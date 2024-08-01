#if DEBUG
//#define DEBUG_PM_ONLY
//#define DEBUG_FULL_ITEM
#endif

using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.States.Items;
using Canyon.Game.States.Transfer;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using System.Runtime.Caching;
using static Canyon.Game.Sockets.Game.Packets.MsgTalk;
using static Canyon.Game.States.User.Character;

namespace Canyon.Game.Sockets.Game.Packets
{
    /// <remarks>Packet Type 1001</remarks>
    /// <summary>
    ///     Message containing character creation details, such as the new character's name,
    ///     body size, and profession. The character name should be verified, and may be
    ///     rejected by the server if a character by that name already exists.
    /// </summary>
    public sealed class MsgRegister : MsgBase<Client>
    {
        private static readonly ushort[] StartX = { 300, 293, 309, 298, 322, 334, 309 };
        private static readonly ushort[] StartY = { 278, 294, 284, 265, 265, 278, 296 };

        // Registration constants
        private static readonly byte[] Hairstyles =
        {
            10, 11, 13, 14, 15, 24, 30, 35, 37, 38, 39, 40
        };

        public int Cancel { get; set; }
        public string Username { get; set; }
        public string CharacterName { get; set; }
        public string MaskedPassword { get; set; }
        public string UnknownString { get; set; }
        public ushort Mesh { get; set; }
        public ushort Class { get; set; }
        public uint UnknownInt { get; set; }
        public uint Token { get; set; }

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Cancel = reader.ReadInt32(); // 4
            Username = reader.ReadString(16); // 8
            CharacterName = reader.ReadString(16); // 24
            MaskedPassword = reader.ReadString(16); // 40
            UnknownString = reader.ReadString(16); // 56
            Mesh = reader.ReadUInt16(); // 72
            Class = reader.ReadUInt16(); // 74
            Token = reader.ReadUInt32(); // 76
            UnknownInt = reader.ReadUInt32(); // 80
        }

        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            // Validate that the player has access to character creation
            if (client.Creation == null || Token != client.Creation.Token ||
                !Kernel.Registration.Contains(Token))
            {
                await client.SendAsync(RegisterInvalid);
                client.Disconnect();
                return;
            }

            if (Cancel != 0)
            {
                client.Disconnect();
                return;
            }

            // Check character name availability
            if (await CharacterRepository.ExistsAsync(CharacterName))
            {
                await client.SendAsync(RegisterNameTaken);
                return;
            }

            if (!RoleManager.IsValidName(CharacterName))
            {
                await client.SendAsync(RegisterInvalid);
                return;
            }

            BaseClassType baseClass;
            switch (Class)
            {
                case 0:
                case 1:
                    {
                        baseClass = BaseClassType.Taoist;
                        break;
                    }
                case 2:
                case 3:
                    {
                        baseClass = BaseClassType.Trojan;
                        break;
                    }
                case 4:
                case 5:
                    {
                        baseClass = BaseClassType.Archer;
                        break;
                    }
                case 6:
                case 7:
                    {
                        baseClass = BaseClassType.Warrior;
                        break;
                    }
                case 8:
                case 9:
                    {
                        baseClass = BaseClassType.Ninja;
                        break;
                    }
                case 10: 
                case 11:
                    {
                        baseClass = BaseClassType.Monk;
                        break;
                    }
                case 12:
                case 13:
                    {
                        baseClass = BaseClassType.Pirate;
                        break;
                    }
#if DEBUG
                case 14:
                case 15:
                    {
                        baseClass = BaseClassType.DragonWarrior;
                        break;
                    }
#endif
                default:
                    {
                        await client.SendAsync(RegisterInvalidProfession);
                        return;
                    }
            }

            // Validate character creation input
            if (!Enum.IsDefined(typeof(BodyType), Mesh))
            {
                await client.SendAsync(RegisterInvalidBody);
                return;
            }

            DbPointAllot allot = ExperienceManager.GetPointAllot((ushort)((int)baseClass / 10), 1) ?? new DbPointAllot
            {
                Strength = 4,
                Agility = 6,
                Vitality = 12,
                Spirit = 0
            };

#if DEBUG_PM_ONLY
            if (CharacterName.Length + 4 > 15)
            {
                CharacterName = CharacterName[..11];
            }
            CharacterName += "[PM]";
#endif

            int posIdx = await NextAsync(StartX.Length) % StartX.Length;

            // Create the character
            var character = new DbCharacter
            {
                AccountIdentity = client.Creation.AccountID,
                Name = CharacterName,
                Mate = 0,
                Mesh = Mesh,

                MapID = 1002,
                X = StartX[posIdx],
                Y = StartY[posIdx],

                Profession = (byte)baseClass,
                Level = 1,
                AutoAllot = 1,
                Silver = 10_000,

                Strength = allot.Strength,
                Agility = allot.Agility,
                Vitality = allot.Vitality,
                Spirit = allot.Spirit,
                HealthPoints =
                    (ushort)(allot.Strength * 3
                              + allot.Agility * 3
                              + allot.Spirit * 3
                              + allot.Vitality * 24),
                ManaPoints = (ushort)(allot.Spirit * 5),

                FirstLogin = (uint)UnixTimestamp.Now,
                HeavenBlessing = (uint)UnixTimestamp.FromDateTime(DateTime.Now.AddDays(30))
            };

            // Generate a random look for the character
            var body = (BodyType)Mesh;
            uint lookFace = 0;
            switch (body)
            {
                case BodyType.AgileFemale:
                case BodyType.MuscularFemale:
                    {
                        switch (baseClass)
                        {
                            case BaseClassType.Ninja: lookFace = 291; break;
                            case BaseClassType.Monk: lookFace = 300; break;
                            case BaseClassType.Pirate: lookFace = 347; break;
                            case BaseClassType.DragonWarrior: lookFace = 355; break;
                            default: lookFace = 201; break;
                        }
                        break;
                    }
                default:
                    {
                        switch (baseClass)
                        {
                            case BaseClassType.Ninja: lookFace = 103; break;
                            case BaseClassType.Monk: lookFace = 109; break;
                            case BaseClassType.Pirate: lookFace = 134; break;
                            case BaseClassType.DragonWarrior: lookFace = 164; break;
                            default: lookFace = 1; break;
                        }
                        break;
                    }
            }

            character.Mesh += lookFace * 10000;
            character.Hairstyle = (ushort)(await NextAsync(3, 9) * 100 + Hairstyles[await NextAsync(0, Hairstyles.Length)]);

            try
            {
                // Save the character and continue with login
                await CharacterRepository.CreateAsync(character);
                Kernel.Registration.Remove(client.Creation.Token);

                await GenerateInitialEquipmentAsync(character);
#if DEBUG_FULL_ITEM
                await GenerateFullUserAsync(character);
#endif

                var args = new TransferAuthArgs
                {
                    AccountID = client.AccountIdentity,
                    AuthorityID = client.AuthorityLevel,
                    IPAddress = client.IpAddress
                };
                // Store in the login cache with an absolute timeout
                var timeoutPolicy = new CacheItemPolicy { AbsoluteExpiration = DateTime.Now.AddSeconds(10) };
                Kernel.Logins.Set(Token.ToString(), args, timeoutPolicy);
            }
            catch
            {
                await client.SendAsync(RegisterTryAgain);
                return;
            }

            await client.SendAsync(RegisterOk);
        }

#if STRESS_TEST
        public static async Task<DbCharacter> CreateBotAccountAsync(uint newUserId)
        {
            byte profession = 45;
            int rate;// = await NextAsync(90);
            //if (rate < 10)
            //{
            //    profession = 15;
            //}
            //else if (rate < 20)
            //{
            //    profession = 25;
            //}
            //else if (rate < 30)
            //{
            //    profession = 45;
            //}
            //else if (rate < 40)
            //{
            //    profession = 55;
            //}
            //else if (rate < 50)
            //{
            //    profession = 65;
            //}
            //else if (rate < 60)
            //{
            //    profession = 75;
            //}
            //else if (rate < 70)
            //{
            //    profession = 85;
            //}
            //else if (rate < 80)
            //{
            //    profession = 135;
            //}
            //else
            //{
            //    profession = 145;
            //}

            rate = await NextAsync(100);
            uint mesh;
            if (rate < 25)
            {
                mesh = 11003;
            }
            else if (rate < 50)
            {
                mesh = 11004;
            }
            else if (rate < 75)
            {
                mesh = 2012001;
            }
            else
            {
                mesh = 2012002;
            }

            rate = await NextAsync(180);
            GameMap gameMap;
            uint mapId;
            Point pos;
            if (rate < 20)
            {
                gameMap = MapManager.GetMap(1002);
            }
            else if (rate < 40)
            {
                gameMap = MapManager.GetMap(1011);
            }
            else if (rate < 60)
            {
                gameMap = MapManager.GetMap(1020);
            }
            else if (rate < 80)
            {
                gameMap = MapManager.GetMap(1000);
            }
            else if (rate < 100)
            {
                gameMap = MapManager.GetMap(1015);
            }
            else if (rate < 120)
            {
                gameMap = MapManager.GetMap(1075);
            }
            else if (rate < 140)
            {
                gameMap = MapManager.GetMap(1926);
            }
            else if (rate < 160)
            {
                gameMap = MapManager.GetMap(1927);
            }
            else 
            {
                gameMap = MapManager.GetMap(1999);
            }

            mapId = gameMap.Identity;
            pos = await gameMap.QueryRandomPositionAsync();

            // Create the character
            var character = new DbCharacter
            {
                AccountIdentity = newUserId,
                Name = $"BOT[{newUserId%1000000}]",
                Mate = 0,
                Mesh = mesh,

                MapID = gameMap.Identity,
                X = (ushort)pos.X,
                Y = (ushort)pos.Y,

                Profession = (byte)profession,
                Level = 140,
                AutoAllot = 1,
                Silver = 10_000,

                Strength = 65535,
                Agility = 65535,
                Vitality = 65535,
                Spirit = 300,
                HealthPoints =
                    (uint)(65535 * 3
                              + 65535 * 3
                              + 65535 * 3
                              + 65535 * 24),
                ManaPoints = (ushort)(300 * 5),

                FirstLogin = (uint)UnixTimestamp.Now,
                HeavenBlessing = (uint)UnixTimestamp.FromDateTime(DateTime.Now.AddDays(30))
            };
            character.Hairstyle = (ushort)(await NextAsync(3, 9) * 100 + Hairstyles[await NextAsync(0, Hairstyles.Length)]);

            await ServerDbContext.CreateAsync(character);
            await GenerateFullUserAsync(character);
            return character;
        }
#endif

#if DEBUG_FULL_ITEM
        private static async Task GenerateFullUserAsync(DbCharacter user)
        {
            if (!user.Name.StartsWith("BOT[", StringComparison.InvariantCultureIgnoreCase))
            {
                user.Level = 140;
                //user.Rebirths = 2;
                //user.Profession += 5;
                user.AttributePoints = 60;
            }

            user.Silver = 10_000_000_000;
            user.ConquerPoints = 10_000_000;
            user.ConquerPointsBound = 10_000_000;

            user.Cultivation = 10_000_000;
            user.RidePetPoint = 10_000_000;
            user.ChestPackageSize = 300;
            user.Flag = 255;
            user.StrengthValue = 10_000_000;

            DbPointAllot allot = ExperienceManager.GetPointAllot((ushort)((int)user.Profession / 10), 120) ?? new DbPointAllot
            {
                Strength = 4,
                Agility = 6,
                Vitality = 12,
                Spirit = 0
            };

            user.Strength = allot.Strength;
            user.Agility = allot.Agility;
            user.Vitality = allot.Vitality;
            user.Spirit = allot.Spirit;
            user.HealthPoints = (ushort)(allot.Strength * 3
                          + allot.Agility * 3
                          + allot.Spirit * 3
                          + allot.Vitality * 24);
            user.ManaPoints = (ushort)(allot.Spirit * 5);

            ushort[] set1Hand = { 410, 420, 421, 430, 440, 450, 460, 480, 481, 490 };
            ushort[] set2Hand = { 510, 511, 530, 540, 560, 561, 580 };

            List<DbWeaponSkill> weaponSkills = new();
            DbFatePlayer fatePlayer = new DbFatePlayer
            {
                PlayerId = user.Identity
            };
            DbJiangHuPlayer jiangHuPlayer = new DbJiangHuPlayer 
            { 
                PlayerId = user.Identity,
                Name = user.Name,
                GenuineQiLevel = JiangHuManager.MAX_TALENT,
                FreeCaltivateParam = (uint)(JiangHuManager.POINTS_TO_COURSE * JiangHuManager.MAX_FREE_COURSE)
            };
            List<DbJiangHuPlayerPower> jiangHuPower = new();
            switch (user.Profession / 10)
            {
                case 1:
                    {
                        //user.FirstProfession = 135;
                        //user.PreviousProfession = 25;

                        await CreateItemAsync(117309, user.Identity, Item.ItemPosition.Headwear, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(120269, user.Identity, Item.ItemPosition.Necklace, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(150269, user.Identity, Item.ItemPosition.Ring, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(614439, user.Identity, Item.ItemPosition.RightHand, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(614439, user.Identity, Item.ItemPosition.LeftHand, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(130309, user.Identity, Item.ItemPosition.Armor, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(160249, user.Identity, Item.ItemPosition.Boots, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(118309, user.Identity, Item.ItemPosition.Inventory, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        
                        foreach (var oneHand in set1Hand)
                        {
                            await CreateItemAsync((uint)(oneHand * 1000 + 439), user.Identity, Item.ItemPosition.Inventory, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                            weaponSkills.Add(new DbWeaponSkill
                            {
                                Type = oneHand,
                                Level = 20,
                                OwnerIdentity = user.Identity
                            });
                        }

                        fatePlayer.Fate1Attrib1 = 10200;
                        fatePlayer.Fate1Attrib2 = 30200;
                        fatePlayer.Fate1Attrib3 = 63500;
                        fatePlayer.Fate1Attrib4 = 72000;

                        fatePlayer.Fate2Attrib1 = 10200;
                        fatePlayer.Fate2Attrib2 = 30200;
                        fatePlayer.Fate2Attrib3 = 63500;
                        fatePlayer.Fate2Attrib4 = 72000;

                        fatePlayer.Fate3Attrib1 = 10200;
                        fatePlayer.Fate3Attrib2 = 30200;
                        fatePlayer.Fate3Attrib3 = 63500;
                        fatePlayer.Fate3Attrib4 = 72000;

                        fatePlayer.Fate4Attrib1 = 10200;
                        fatePlayer.Fate4Attrib2 = 30200;
                        fatePlayer.Fate4Attrib3 = 63500;
                        fatePlayer.Fate4Attrib4 = 72000;

                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 1,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 2,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 3,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 4,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 5,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 6,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 7,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 8,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 9,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        break;
                    }

                case 2:
                    {
                        //user.FirstProfession = user.PreviousProfession = user.Profession;

                        await CreateItemAsync(117309, user.Identity, Item.ItemPosition.Headwear, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(120269, user.Identity, Item.ItemPosition.Necklace, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(150269, user.Identity, Item.ItemPosition.Ring, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(561439, user.Identity, Item.ItemPosition.RightHand, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(900309, user.Identity, Item.ItemPosition.LeftHand, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(130309, user.Identity, Item.ItemPosition.Armor, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(160249, user.Identity, Item.ItemPosition.Boots, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);

                        await CreateItemAsync(111309, user.Identity, Item.ItemPosition.Inventory, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);

                        foreach (var twoHand in set2Hand)
                        {
                            if (twoHand == 561)
                            {
                                continue;
                            }

                            await CreateItemAsync((uint)(twoHand * 1000 + 439), user.Identity, Item.ItemPosition.Inventory, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                            weaponSkills.Add(new DbWeaponSkill
                            {
                                Type = twoHand,
                                Level = 20,
                                OwnerIdentity = user.Identity
                            });
                        }

                        fatePlayer.Fate1Attrib1 = 10200;
                        fatePlayer.Fate1Attrib2 = 30200;
                        fatePlayer.Fate1Attrib3 = 63500;
                        fatePlayer.Fate1Attrib4 = 72000;

                        fatePlayer.Fate2Attrib1 = 10200;
                        fatePlayer.Fate2Attrib2 = 30200;
                        fatePlayer.Fate2Attrib3 = 63500;
                        fatePlayer.Fate2Attrib4 = 72000;

                        fatePlayer.Fate3Attrib1 = 10200;
                        fatePlayer.Fate3Attrib2 = 30200;
                        fatePlayer.Fate3Attrib3 = 63500;
                        fatePlayer.Fate3Attrib4 = 72000;

                        fatePlayer.Fate4Attrib1 = 10200;
                        fatePlayer.Fate4Attrib2 = 30200;
                        fatePlayer.Fate4Attrib3 = 63500;
                        fatePlayer.Fate4Attrib4 = 72000;

                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 1,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 2,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 3,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 4,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 5,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 6,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 7,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 8,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 9,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        break;
                    }

                case 4:
                    {
                        //user.FirstProfession = 15;
                        //user.PreviousProfession = 25;

                        await CreateMagicAsync(user.Identity, 8001, 5);

                        await CreateItemAsync(117309, user.Identity, Item.ItemPosition.Headwear, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(120269, user.Identity, Item.ItemPosition.Necklace, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(150269, user.Identity, Item.ItemPosition.Ring, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(500429, user.Identity, Item.ItemPosition.RightHand, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(131309, user.Identity, Item.ItemPosition.Armor, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(160249, user.Identity, Item.ItemPosition.Boots, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);

                        await CreateItemAsync(113309, user.Identity, Item.ItemPosition.Inventory, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(613439, user.Identity, Item.ItemPosition.Inventory, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(613439, user.Identity, Item.ItemPosition.Inventory, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);

                        weaponSkills.Add(new DbWeaponSkill
                        {
                            Type = 500,
                            Level = 20,
                            OwnerIdentity = user.Identity
                        });
                        weaponSkills.Add(new DbWeaponSkill
                        {
                            Type = 613,
                            Level = 20,
                            OwnerIdentity = user.Identity
                        });

                        fatePlayer.Fate1Attrib1 = 10200;
                        fatePlayer.Fate1Attrib2 = 30200;
                        fatePlayer.Fate1Attrib3 = 63500;
                        fatePlayer.Fate1Attrib4 = 72000;

                        fatePlayer.Fate2Attrib1 = 10200;
                        fatePlayer.Fate2Attrib2 = 30200;
                        fatePlayer.Fate2Attrib3 = 63500;
                        fatePlayer.Fate2Attrib4 = 72000;

                        fatePlayer.Fate3Attrib1 = 10200;
                        fatePlayer.Fate3Attrib2 = 30200;
                        fatePlayer.Fate3Attrib3 = 63500;
                        fatePlayer.Fate3Attrib4 = 72000;

                        fatePlayer.Fate4Attrib1 = 10200;
                        fatePlayer.Fate4Attrib2 = 30200;
                        fatePlayer.Fate4Attrib3 = 63500;
                        fatePlayer.Fate4Attrib4 = 72000;

                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 1,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 2,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 3,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 4,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 5,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 6,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 7,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 8,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 9,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        break;
                    }

                case 5:
                    {
                        //user.FirstProfession = user.PreviousProfession = user.Profession;

                        await CreateItemAsync(112309, user.Identity, Item.ItemPosition.Headwear, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(120269, user.Identity, Item.ItemPosition.Necklace, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(150269, user.Identity, Item.ItemPosition.Ring, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(616439, user.Identity, Item.ItemPosition.RightHand, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(616439, user.Identity, Item.ItemPosition.LeftHand, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(135309, user.Identity, Item.ItemPosition.Armor, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(160249, user.Identity, Item.ItemPosition.Boots, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(118309, user.Identity, Item.ItemPosition.Inventory, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);

                        await CreateItemAsync(117309, user.Identity, Item.ItemPosition.Inventory, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(601439, user.Identity, Item.ItemPosition.Inventory, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(601439, user.Identity, Item.ItemPosition.Inventory, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);

                        weaponSkills.Add(new DbWeaponSkill
                        {
                            Type = 601,
                            Level = 20,
                            OwnerIdentity = user.Identity
                        });
                        weaponSkills.Add(new DbWeaponSkill
                        {
                            Type = 616,
                            Level = 20,
                            OwnerIdentity = user.Identity
                        });

                        fatePlayer.Fate1Attrib1 = 10200;
                        fatePlayer.Fate1Attrib2 = 30200;
                        fatePlayer.Fate1Attrib3 = 63500;
                        fatePlayer.Fate1Attrib4 = 72000;

                        fatePlayer.Fate2Attrib1 = 10200;
                        fatePlayer.Fate2Attrib2 = 30200;
                        fatePlayer.Fate2Attrib3 = 63500;
                        fatePlayer.Fate2Attrib4 = 72000;

                        fatePlayer.Fate3Attrib1 = 10200;
                        fatePlayer.Fate3Attrib2 = 30200;
                        fatePlayer.Fate3Attrib3 = 63500;
                        fatePlayer.Fate3Attrib4 = 72000;

                        fatePlayer.Fate4Attrib1 = 10200;
                        fatePlayer.Fate4Attrib2 = 30200;
                        fatePlayer.Fate4Attrib3 = 63500;
                        fatePlayer.Fate4Attrib4 = 72000;

                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 1,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 2,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 3,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 4,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 5,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 6,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 7,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 8,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 9,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        break;
                    }

                case 6:
                    {
                        //user.FirstProfession = user.PreviousProfession = user.Profession;

                        await CreateItemAsync(143309, user.Identity, Item.ItemPosition.Headwear, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(120269, user.Identity, Item.ItemPosition.Necklace, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(151269, user.Identity, Item.ItemPosition.Ring, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(610439, user.Identity, Item.ItemPosition.RightHand, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(610439, user.Identity, Item.ItemPosition.LeftHand, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(136309, user.Identity, Item.ItemPosition.Armor, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(160249, user.Identity, Item.ItemPosition.Boots, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);

                        weaponSkills.Add(new DbWeaponSkill
                        {
                            Type = 610,
                            Level = 20,
                            OwnerIdentity = user.Identity
                        });

                        fatePlayer.Fate1Attrib1 = 10200;
                        fatePlayer.Fate1Attrib2 = 30200;
                        fatePlayer.Fate1Attrib3 = 63500;
                        fatePlayer.Fate1Attrib4 = 72000;

                        fatePlayer.Fate2Attrib1 = 10200;
                        fatePlayer.Fate2Attrib2 = 30200;
                        fatePlayer.Fate2Attrib3 = 63500;
                        fatePlayer.Fate2Attrib4 = 72000;

                        fatePlayer.Fate3Attrib1 = 10200;
                        fatePlayer.Fate3Attrib2 = 30200;
                        fatePlayer.Fate3Attrib3 = 63500;
                        fatePlayer.Fate3Attrib4 = 72000;

                        fatePlayer.Fate4Attrib1 = 10200;
                        fatePlayer.Fate4Attrib2 = 30200;
                        fatePlayer.Fate4Attrib3 = 63500;
                        fatePlayer.Fate4Attrib4 = 72000;

                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 1,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 2,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 3,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 4,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 5,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 6,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 7,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 8,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 9,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        break;
                    }

                case 7:
                    {
                        //user.FirstProfession = 135;
                        //user.PreviousProfession = 25;

                        await CreateItemAsync(144309, user.Identity, Item.ItemPosition.Headwear, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(120269, user.Identity, Item.ItemPosition.Necklace, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(150269, user.Identity, Item.ItemPosition.Ring, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(611439, user.Identity, Item.ItemPosition.RightHand, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(612439, user.Identity, Item.ItemPosition.LeftHand, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(139309, user.Identity, Item.ItemPosition.Armor, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(160249, user.Identity, Item.ItemPosition.Boots, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);

                        weaponSkills.Add(new DbWeaponSkill
                        {
                            Type = 611,
                            Level = 20,
                            OwnerIdentity = user.Identity
                        });
                        weaponSkills.Add(new DbWeaponSkill
                        {
                            Type = 612,
                            Level = 20,
                            OwnerIdentity = user.Identity
                        });

                        fatePlayer.Fate1Attrib1 = 10200;
                        fatePlayer.Fate1Attrib2 = 30200;
                        fatePlayer.Fate1Attrib3 = 63500;
                        fatePlayer.Fate1Attrib4 = 72000;

                        fatePlayer.Fate2Attrib1 = 10200;
                        fatePlayer.Fate2Attrib2 = 30200;
                        fatePlayer.Fate2Attrib3 = 63500;
                        fatePlayer.Fate2Attrib4 = 72000;

                        fatePlayer.Fate3Attrib1 = 10200;
                        fatePlayer.Fate3Attrib2 = 30200;
                        fatePlayer.Fate3Attrib3 = 63500;
                        fatePlayer.Fate3Attrib4 = 72000;

                        fatePlayer.Fate4Attrib1 = 10200;
                        fatePlayer.Fate4Attrib2 = 30200;
                        fatePlayer.Fate4Attrib3 = 63500;
                        fatePlayer.Fate4Attrib4 = 72000;

                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 1,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 2,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 3,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 4,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 5,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 6,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 7,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 8,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 9,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        break;
                    }

                case 8:
                    {
                        //user.FirstProfession = user.PreviousProfession = user.Profession;

                        await CreateItemAsync(148309, user.Identity, Item.ItemPosition.Headwear, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(120269, user.Identity, Item.ItemPosition.Necklace, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(150269, user.Identity, Item.ItemPosition.Ring, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(617439, user.Identity, Item.ItemPosition.RightHand, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(617439, user.Identity, Item.ItemPosition.LeftHand, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(138309, user.Identity, Item.ItemPosition.Armor, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);
                        await CreateItemAsync(160249, user.Identity, Item.ItemPosition.Boots, 12, Item.SocketGem.SuperDragonGem, Item.SocketGem.SuperDragonGem, 255, 7, true);

                        weaponSkills.Add(new DbWeaponSkill
                        {
                            Type = 617,
                            Level = 20,
                            OwnerIdentity = user.Identity
                        });

                        fatePlayer.Fate1Attrib1 = 10200;
                        fatePlayer.Fate1Attrib2 = 30200;
                        fatePlayer.Fate1Attrib3 = 63500;
                        fatePlayer.Fate1Attrib4 = 72000;

                        fatePlayer.Fate2Attrib1 = 10200;
                        fatePlayer.Fate2Attrib2 = 30200;
                        fatePlayer.Fate2Attrib3 = 63500;
                        fatePlayer.Fate2Attrib4 = 72000;

                        fatePlayer.Fate3Attrib1 = 10200;
                        fatePlayer.Fate3Attrib2 = 30200;
                        fatePlayer.Fate3Attrib3 = 63500;
                        fatePlayer.Fate3Attrib4 = 72000;

                        fatePlayer.Fate4Attrib1 = 10200;
                        fatePlayer.Fate4Attrib2 = 30200;
                        fatePlayer.Fate4Attrib3 = 63500;
                        fatePlayer.Fate4Attrib4 = 72000;

                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 1,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 2,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 3,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 4,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 5,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 6,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 7,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Attack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 8,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.CriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 9,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        break;
                    }

                case 10:
                    {
                        // ? profession define ?
                        await CreateItemAsync(114309, user.Identity, Item.ItemPosition.Headwear, 12, Item.SocketGem.SuperTortoiseGem, Item.SocketGem.SuperTortoiseGem, 255, 7, true);
                        await CreateItemAsync(121269, user.Identity, Item.ItemPosition.Necklace, 12, Item.SocketGem.SuperTortoiseGem, Item.SocketGem.SuperTortoiseGem, 255, 7, true);
                        await CreateItemAsync(152279, user.Identity, Item.ItemPosition.Ring, 12, Item.SocketGem.SuperPhoenixGem, Item.SocketGem.SuperPhoenixGem, 255, 7, true);
                        await CreateItemAsync(620439, user.Identity, Item.ItemPosition.RightHand, 12, Item.SocketGem.SuperPhoenixGem, Item.SocketGem.SuperPhoenixGem, 255, 7, true);
                        await CreateItemAsync(619439, user.Identity, Item.ItemPosition.LeftHand, 12, 0, 0, 0, 0, true);
                        await CreateItemAsync(134309, user.Identity, Item.ItemPosition.Armor, 12, Item.SocketGem.SuperTortoiseGem, Item.SocketGem.SuperTortoiseGem, 255, 7, true);
                        await CreateItemAsync(160249, user.Identity, Item.ItemPosition.Boots, 12, Item.SocketGem.SuperTortoiseGem, Item.SocketGem.SuperTortoiseGem, 255, 7, true);

                        await CreateItemAsync(421439, user.Identity, Item.ItemPosition.Inventory, 12, Item.SocketGem.SuperPhoenixGem, Item.SocketGem.SuperPhoenixGem, 255, 7, true);

                        fatePlayer.Fate1Attrib1 = 110300;
                        fatePlayer.Fate1Attrib2 = 30200;
                        fatePlayer.Fate1Attrib3 = 63500;
                        fatePlayer.Fate1Attrib4 = 82500;

                        fatePlayer.Fate2Attrib1 = 110300;
                        fatePlayer.Fate2Attrib2 = 30200;
                        fatePlayer.Fate2Attrib3 = 63500;
                        fatePlayer.Fate2Attrib4 = 82500;

                        fatePlayer.Fate3Attrib1 = 110300;
                        fatePlayer.Fate3Attrib2 = 30200;
                        fatePlayer.Fate3Attrib3 = 63500;
                        fatePlayer.Fate3Attrib4 = 82500;

                        fatePlayer.Fate4Attrib1 = 110300;
                        fatePlayer.Fate4Attrib2 = 30200;
                        fatePlayer.Fate4Attrib3 = 63500;
                        fatePlayer.Fate4Attrib4 = 82500;

                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 1,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Counteraction,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 2,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 3,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 4,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 5,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 6,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.Immunity,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 7,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MagicAttack,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 8,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.SkillCriticalStrike,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        jiangHuPower.Add(new DbJiangHuPlayerPower
                        {
                            PlayerId = user.Identity,
                            Level = 9,
                            Type1 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type2 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type3 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type4 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type5 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type6 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type7 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type8 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Type9 = (byte)JiangHuManager.JiangHuAttrType.MaxLife,
                            Quality1 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality2 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality3 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality4 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality5 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality6 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality7 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality8 = (byte)JiangHuManager.JiangHuQuality.Epic,
                            Quality9 = (byte)JiangHuManager.JiangHuQuality.Epic
                        });
                        break;
                    }
            }

            await CreateItemAsync(2100025, user.Identity, Item.ItemPosition.Gourd, 0, 0, 0, 0, 1, true);
            await CreateItemAsync(300000, user.Identity, Item.ItemPosition.Steed, 12, 0, 0, 0, 0, true);
            await CreateItemAsync(201009, user.Identity, Item.ItemPosition.AttackTalisman, 12, Item.SocketGem.SuperThunderGem, Item.SocketGem.SuperThunderGem, 0, 1, true);
            await CreateItemAsync(202009, user.Identity, Item.ItemPosition.DefenceTalisman, 12, Item.SocketGem.SuperGloryGem, Item.SocketGem.SuperGloryGem, 0, 1, true);
            await CreateItemAsync(203009, user.Identity, Item.ItemPosition.Crop, 12, Item.SocketGem.NoSocket, Item.SocketGem.NoSocket, 0, 1, true);
            await CreateItemAsync(204009, user.Identity, Item.ItemPosition.Wing, 12, Item.SocketGem.SuperThunderGem, Item.SocketGem.SuperGloryGem, 0, 1, true);

            await ServerDbContext.SaveAsync(user);
            await ServerDbContext.SaveRangeAsync(weaponSkills);
            await ServerDbContext.SaveAsync(fatePlayer);
            await ServerDbContext.SaveAsync(jiangHuPlayer);
            await ServerDbContext.SaveRangeAsync(jiangHuPower);
        }
#endif

        private static async Task GenerateInitialEquipmentAsync(DbCharacter user)
        {
            DbNewbieInfo info = await NewbieInfoRepository.GetAsync((uint)(user.Profession / 10 * 10));
            if (info == null)
            {
                return;
            }

            if (info.LeftHand != 0)
            {
                await CreateItemAsync(info.LeftHand, user.Identity, Item.ItemPosition.LeftHand);
            }

            if (info.RightHand != 0)
            {
                await CreateItemAsync(info.RightHand, user.Identity, Item.ItemPosition.RightHand);
            }

            if (info.Shoes != 0)
            {
                await CreateItemAsync(info.Shoes, user.Identity, Item.ItemPosition.Boots);
            }

            if (info.Headgear != 0)
            {
                await CreateItemAsync(info.Headgear, user.Identity, Item.ItemPosition.Headwear);
            }

            if (info.Necklace != 0)
            {
                await CreateItemAsync(info.Necklace, user.Identity, Item.ItemPosition.Necklace);
            }

            if (info.Armor != 0)
            {
                await CreateItemAsync(info.Armor, user.Identity, Item.ItemPosition.Armor);
            }

            if (info.Ring != 0)
            {
                await CreateItemAsync(info.Ring, user.Identity, Item.ItemPosition.Ring);
            }

            if (info.Item0 != 0)
            {
                for (var i = 0; i < info.Number0; i++)
                {
                    await CreateItemAsync(info.Item0, user.Identity, Item.ItemPosition.Inventory);
                }
            }

            if (info.Item1 != 0)
            {
                for (var i = 0; i < info.Number1; i++)
                {
                    await CreateItemAsync(info.Item1, user.Identity, Item.ItemPosition.Inventory);
                }
            }

            if (info.Item2 != 0)
            {
                for (var i = 0; i < info.Number2; i++)
                {
                    await CreateItemAsync(info.Item2, user.Identity, Item.ItemPosition.Inventory);
                }
            }

            if (info.Item3 != 0)
            {
                for (var i = 0; i < info.Number3; i++)
                {
                    await CreateItemAsync(info.Item3, user.Identity, Item.ItemPosition.Inventory);
                }
            }

            if (info.Item4 != 0)
            {
                for (var i = 0; i < info.Number4; i++)
                {
                    await CreateItemAsync(info.Item4, user.Identity, Item.ItemPosition.Inventory);
                }
            }

            if (info.Item5 != 0)
            {
                for (var i = 0; i < info.Number5; i++)
                {
                    await CreateItemAsync(info.Item5, user.Identity, Item.ItemPosition.Inventory);
                }
            }

            if (info.Item6 != 0)
            {
                for (var i = 0; i < info.Number6; i++)
                {
                    await CreateItemAsync(info.Item6, user.Identity, Item.ItemPosition.Inventory);
                }
            }

            if (info.Item7 != 0)
            {
                for (var i = 0; i < info.Number7; i++)
                {
                    await CreateItemAsync(info.Item7, user.Identity, Item.ItemPosition.Inventory);
                }
            }

            if (info.Item8 != 0)
            {
                for (var i = 0; i < info.Number8; i++)
                {
                    await CreateItemAsync(info.Item8, user.Identity, Item.ItemPosition.Inventory);
                }
            }

            if (info.Item9 != 0)
            {
                for (var i = 0; i < info.Number9; i++)
                {
                    await CreateItemAsync(info.Item9, user.Identity, Item.ItemPosition.Inventory);
                }
            }

            if (info.Magic0 != 0)
                await CreateMagicAsync(user.Identity, (ushort)info.Magic0);
            if (info.Magic1 != 0)
                await CreateMagicAsync(user.Identity, (ushort)info.Magic1);
            if (info.Magic2 != 0)
                await CreateMagicAsync(user.Identity, (ushort)info.Magic2);
            if (info.Magic3 != 0)
                await CreateMagicAsync(user.Identity, (ushort)info.Magic3);
        }

        private static async Task CreateItemAsync(uint type, uint idOwner, Item.ItemPosition position, byte add = 0,
                                           Item.SocketGem gem1 = Item.SocketGem.NoSocket,
                                           Item.SocketGem gem2 = Item.SocketGem.NoSocket,
                                           byte enchant = 0, byte reduceDmg = 0, bool monopoly = false)
        {
            DbItem item = Item.CreateEntity(type);
            if (item == null)
            {
                return;
            }

            item.Position = (byte)position;
            item.PlayerId = idOwner;
            item.AddLife = enchant;
            item.ReduceDmg = reduceDmg;
            item.Magic3 = add;
            item.Gem1 = (byte)gem1;
            item.Gem2 = (byte)gem2;
            item.Monopoly = (byte)(monopoly ? 3 : 0);
            await ServerDbContext.SaveAsync(item);
        }

        private static Task CreateMagicAsync(uint idOwner, ushort type, byte level = 0)
        {
            return ServerDbContext.SaveAsync(new DbMagic
            {
                Type = type,
                Level = level,
                OwnerId = idOwner
            });
        }
    }
}
