using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Services.Managers;
using Canyon.Game.Services.Processors;
using Canyon.Game.Sockets.Ai.Packets;
using Canyon.Game.States;
using Canyon.Game.States.Events;
using Canyon.Game.States.Events.Elite;
using Canyon.Game.States.Items;
using Canyon.Game.States.Magics;
using Canyon.Game.States.MessageBoxes;
using Canyon.Game.States.NPCs;
using Canyon.Game.States.Syndicates;
using Canyon.Game.States.User;
using Canyon.Game.States.World;
using Canyon.Network.Packets;
using System.Drawing;

namespace Canyon.Game.Sockets.Game.Packets
{
    /// <remarks>Packet Type 1004</remarks>
    /// <summary>
    ///     Message defining a chat message from one player to the other, or from the system
    ///     to a player. Used for all chat systems in the game, including messages outside of
    ///     the game world state, such as during character creation or to tell the client to
    ///     continue logging in after connect.
    /// </summary>
    public sealed class MsgTalk : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgTalk>();

        public const uint SystemLookface = 2962001;

        public MsgTalk()
        {
        }

        /// <summary>
        ///     Instantiates a new instance of <see cref="MsgTalk" /> using the recipient's
        ///     character ID, a destination channel, and text to display. By default, sends
        ///     from "SYSTEM" to "ALLUSERS".
        /// </summary>
        /// <param name="characterID">Character's identifier</param>
        /// <param name="channel">Destination channel to send the text on</param>
        /// <param name="text">Text to be displayed in the client</param>
        public MsgTalk(uint characterID, TalkChannel channel, string text)
        {
            Timestamp = Environment.TickCount;
            Color = Color.White;
            Channel = channel;
            Style = TalkStyle.Normal;
            CharacterID = characterID;
            SenderMesh = SystemLookface;
            SenderName = SYSTEM;
            RecipientName = ALLUSERS;
            Suffix = string.Empty;
            Message = text;
        }

        /// <summary>
        ///     Instantiates a new instance of <see cref="MsgTalk" /> using the recipient's
        ///     character ID, a destination channel, a text color, and text to display. By
        ///     default, sends from "SYSTEM" to "ALLUSERS".
        /// </summary>
        /// <param name="characterID">Character's identifier</param>
        /// <param name="channel">Destination channel to send the text on</param>
        /// <param name="color">Color text is to be displayed in</param>
        /// <param name="text">Text to be displayed in the client</param>
        public MsgTalk(uint characterID, TalkChannel channel, Color color, string text)
        {
            Timestamp = Environment.TickCount;
            Color = color;
            Channel = channel;
            Style = TalkStyle.Normal;
            CharacterID = characterID;
            SenderMesh = SystemLookface;
            SenderName = SYSTEM;
            RecipientName = ALLUSERS;
            Suffix = string.Empty;
            Message = text;
        }

        /// <summary>
        ///     Instantiates a new instance of <see cref="MsgTalk" /> using the recipient's
        ///     character ID, a destination channel, a text color, sender and recipient's name,
        ///     and text to display.
        /// </summary>
        /// <param name="characterID">Character's identifier</param>
        /// <param name="channel">Destination channel to send the text on</param>
        /// <param name="color">Color text is to be displayed in</param>
        /// <param name="recipient">Name the message displays it is to</param>
        /// <param name="sender">Name the message displays it is from</param>
        /// <param name="text">Text to be displayed in the client</param>
        public MsgTalk(uint characterID, TalkChannel channel, Color color,
                       string recipient, string sender, string text)
        {
            Timestamp = Environment.TickCount;
            Color = color;
            Channel = channel;
            Style = TalkStyle.Normal;
            CharacterID = characterID;
            SenderMesh = SystemLookface;
            SenderName = sender;
            RecipientName = recipient;
            Suffix = string.Empty;
            Message = text;
        }

        // Packet Properties
        public int Timestamp { get; set; }
        public Color Color { get; set; }
        public TalkChannel Channel { get; set; }
        public TalkStyle Style { get; set; }
        public uint CharacterID { get; set; }
        public uint RecipientMesh { get; set; }
        public string RecipientName { get; set; }
        public uint SenderMesh { get; set; }
        public string SenderName { get; set; }
        public string Suffix { get; set; }
        public string Message { get; set; }

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
            Timestamp = reader.ReadInt32();
            Color = Color.FromArgb(reader.ReadInt32());
            Channel = (TalkChannel)reader.ReadUInt16();
            Style = (TalkStyle)reader.ReadUInt16();
            CharacterID = reader.ReadUInt32();
            RecipientMesh = reader.ReadUInt32();
            SenderMesh = reader.ReadUInt32();
            byte[] unknown = reader.ReadBytes(5);
            List<string> strings = reader.ReadStrings();
            if (strings.Count > 3)
            {
                SenderName = strings[0];
                RecipientName = strings[1];
                Suffix = strings[2];
                Message = strings[3];
            }
        }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgTalk);
            writer.Write(Timestamp); // 4
            writer.Write(Color.FromArgb(0, Color).ToArgb()); // 8
            writer.Write((ushort)Channel); // 12
            writer.Write((ushort)Style); // 14
            writer.Write(CharacterID); // 20
            writer.Write(RecipientMesh); // 24
            writer.Write(SenderMesh); // 28
            writer.Write(new byte[5]); // 16
            writer.Write(new List<string> // 32
            {
                SenderName,
                RecipientName,
                Suffix,
                Message
            });
            return writer.ToArray();
        }

        // Static messages
        public const string SYSTEM = "SYSTEM";
        public const string ALLUSERS = "ALLUSERS";

        public static MsgTalk LoginOk { get; } = new(0, TalkChannel.Login, "ANSWER_OK");
        public static MsgTalk LoginInvalid { get; } = new(0, TalkChannel.Login, "Invalid login");
        public static MsgTalk LoginNewRole { get; } = new(0, TalkChannel.Login, "NEW_ROLE");
        public static MsgTalk RegisterOk { get; } = new(0, TalkChannel.Register, "ANSWER_OK");
        public static MsgTalk RegisterInvalid { get; } = new(0, TalkChannel.Register, "Invalid character.");
        public static MsgTalk RegisterInvalidBody { get; } = new(0, TalkChannel.Register, "Invalid character body.");
        public static MsgTalk RegisterInvalidProfession { get; } = new(0, TalkChannel.Register, "Invalid character profession.");
        public static MsgTalk RegisterNameTaken { get; } = new(0, TalkChannel.Register, "Character name taken");
        public static MsgTalk RegisterTryAgain { get; } = new(0, TalkChannel.Register, "Error, please try later");

        public override async Task ProcessAsync(Client client)
        {
            Character sender = client.Character;
            Character target = RoleManager.GetUser(RecipientName);

            if (sender.Name != SenderName)
            {
#if DEBUG
                if (sender.IsGm())
                {
                    await sender.SendAsync("Invalid sender name????");
                }
#endif
                return;
            }

            await ServerDbContext.SaveAsync(new DbMessageLog
            {
                SenderIdentity = sender.Identity,
                SenderName = sender.Name,
                TargetIdentity = target?.Identity ?? 0,
                TargetName = target?.Name ?? RecipientName,
                Channel = (ushort)Channel,
                Message = Message,
                Time = DateTime.Now
            });

            if (await ProcessCommandAsync(Message, sender))
            {
                logger.LogInformation($"[{sender.Identity}] {sender.Name} Command executed: {Message}");
                return;
            }

            switch (Channel)
            {
                case TalkChannel.Talk:
                    {
                        if (!sender.IsAlive)
                        {
                            return;
                        }

                        await sender.BroadcastRoomMsgAsync(this, false);
                        break;
                    }

                case TalkChannel.Whisper:
                    {
                        if (target == null)
                        {
                            await sender.SendAsync(StrTargetNotOnline, TalkChannel.Talk, Color.White);

                            return;
                        }

                        SenderMesh = sender.Mesh;
                        RecipientMesh = target.Mesh;
                        await target.SendAsync(this);
                        break;
                    }

                case TalkChannel.Team:
                    {
                        if (sender.Team != null)
                        {
                            await sender.Team.SendAsync(this, sender.Identity);
                        }
                        break;
                    }

                case TalkChannel.Friend:
                    {
                        await sender.SendToFriendsAsync(this);
                        break;
                    }

                case TalkChannel.Guild:
                    {
                        if (sender.SyndicateIdentity == 0)
                        {
                            return;
                        }

                        await sender.Syndicate.SendAsync(this, sender.Identity);
                        break;
                    }

                case TalkChannel.Family:
                    {
                        if (sender.FamilyIdentity == 0)
                        {
                            return;
                        }

                        await sender.Family.SendAsync(this, sender.Identity);
                        break;
                    }

                case TalkChannel.Ally:
                    {
                        if (sender.SyndicateIdentity == 0)
                        {
                            return;
                        }

                        await sender.Syndicate.SendAsync(this, sender.Identity);
                        await sender.Syndicate.BroadcastToAlliesAsync(this);
                        break;
                    }

                case TalkChannel.Ghost:
                    {
                        if (sender.IsAlive)
                        {
                            return;
                        }

                        await sender.BroadcastRoomMsgAsync(this, false);
                        break;
                    }

                case TalkChannel.World:
                    {
                        if (!sender.CanUseWorldChat())
                        {
                            return;
                        }

                        await BroadcastWorldMsgAsync(this, sender.Identity);
                        break;
                    }

                case TalkChannel.Announce:
                    {
                        if (sender.SyndicateIdentity == 0 ||
                            sender.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        {
                            return;
                        }

                        sender.Syndicate.Announce = Message[..Math.Min(127, Message.Length)];
                        sender.Syndicate.AnnounceDate = DateTime.Now;
                        await sender.Syndicate.SaveAsync();
                        break;
                    }

                case TalkChannel.Bbs:
                case TalkChannel.GuildBoard:
                case TalkChannel.FriendBoard:
                case TalkChannel.OthersBoard:
                case TalkChannel.TeamBoard:
                case TalkChannel.TradeBoard:
                    {
                        MessageBoard.AddMessage(sender, Message, Channel);
                        break;
                    }

                default:
                    {
                        logger.LogInformation("MsgTalk channel not handled!! {CHN}", Channel);
                        break;
                    }
            }
        }

        private async Task<bool> ProcessCommandAsync(string fullCmd, Character user)
        {
            if (fullCmd.StartsWith("#") && user.Gender == 2 && fullCmd.Length > 7)
            {
                // let's suppose that the user is with flower charm
                fullCmd = fullCmd.Substring(3, fullCmd.Length - 6);
            }

            if (fullCmd[0] != '/')
            {
                return false;
            }

            string[] splitCmd = fullCmd.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            string command = splitCmd[0];
            string param = "";
            if (splitCmd.Length > 1)
            {
                param = splitCmd[1];
            }

            if (user.IsPm())
            {
                switch (command.ToLower())
                {
                    case "/pro":
                        {
                            if (byte.TryParse(param, out byte proProf))
                            {
                                await user.SetAttributesAsync(ClientUpdateType.Class, proProf);
                            }

                            return true;
                        }

                    case "/life":
                        {
                            await user.SetAttributesAsync(ClientUpdateType.Hitpoints, user.MaxLife);
                            return true;
                        }

                    case "/mana":
                        {
                            await user.SetAttributesAsync(ClientUpdateType.Mana, user.MaxMana);
                            return true;
                        }

                    case "/superman":
                        {
                            await user.SetAttributesAsync(ClientUpdateType.Strength, 176);
                            await user.SetAttributesAsync(ClientUpdateType.Agility, 256);
                            await user.SetAttributesAsync(ClientUpdateType.Vitality, 110);
                            await user.SetAttributesAsync(ClientUpdateType.Spirit, 125);

                            return true;
                        }

                    case "/status":
                        {
                            string[] p = param.Split(' ');

                            if (int.TryParse(p[0], out int flag))
                            {
                                if (p.Length < 2 || !int.TryParse(p[1], out int time))
                                {
                                    time = 10;
                                }
                                await user.AttachStatusAsync(user, flag, 0, time, 0);
                            }
                            return true;
                        }

                    case "/xp":
                        {
                            await user.AddXpAsync(100);
                            await user.BurstXpAsync();
                            return true;
                        }

                    case "/sp":
                        {
                            await user.SetAttributesAsync(ClientUpdateType.Stamina, user.MaxEnergy);
                            return true;
                        }

                    case "/rangedrop":
                        {
                            string[] splitParams = param.Split(" ");
                            if (splitParams.Length < 4)
                            {
                                await user.SendAsync("Command requires arguments! /rangedrop itemtype range num secs");
                                return true;
                            }

                            if (!uint.TryParse(splitParams[0], out var itemType))
                            {
                                await user.SendAsync("Invalid itemtype value. Must be a valid number.");
                                return true;
                            }

                            DbItemtype dbItemtype = ItemManager.GetItemtype(itemType);
                            if (dbItemtype == null)
                            {
                                await user.SendAsync("Invalid itemtype value. Item does not exist.");
                                return true;
                            }

                            if (!uint.TryParse(splitParams[1], out var range) || range > 100)
                            {
                                await user.SendAsync("Invalid range value. Must be a number below 100.");
                                return true;
                            }

                            if (!uint.TryParse(splitParams[2], out var num) || num > 100)
                            {
                                await user.SendAsync("Invalid num value. Must be a number below 100.");
                                return true;
                            }

                            if (!uint.TryParse(splitParams[3], out var secs))
                            {
                                await user.SendAsync("Invalid seconds value. Must be a number.");
                                return true;
                            }

                            uint mapId = user.MapIdentity;
                            int x = (int)(user.X - range);
                            int y = (int)(user.Y - range);

                            const string format = "Map_DropMultiItems({0},{1},{2},{3},{4},{5},{6},{7})";
                            string luaScript = string.Format(format, mapId, itemType, x, y, range * 2, range * 2, num, secs);
                            LuaScriptManager.Run(luaScript);
                            return true;
                        }

                    case "/awarditem":
                        {
                            string[] splitParam = param.Split(' ');
                            if (!uint.TryParse(splitParam[0], out uint idAwardItem))
                            {
                                return true;
                            }

                            int count;
                            if (splitParam.Length < 2 || !int.TryParse(splitParam[1], out count))
                            {
                                count = 1;
                            }

                            DbItemtype itemtype = ItemManager.GetItemtype(idAwardItem);
                            if (itemtype == null)
                            {
                                await user.SendAsync($"[AwardItem] Itemtype {idAwardItem} not found");
                                return true;
                            }

                            for (int i = 0; i < count; i++)
                            {
                                await user.UserPackage.AwardItemAsync(idAwardItem);
                            }
                            await user.SendAsync($"[AwardItem] {itemtype.Name} award success!");
                            return true;
                        }

                    case "/awardmoney":
                        {
                            if (int.TryParse(param, out int moneyAmount))
                            {
                                await user.AwardMoneyAsync(moneyAmount);
                            }

                            return true;
                        }

                    case "/awardemoney":
                        {
                            if (int.TryParse(param, out int emoneyAmount))
                            {
                                await user.AwardConquerPointsAsync(emoneyAmount);
                            }

                            return true;
                        }

                    case "/awardemoneymono":
                        {
                            if (int.TryParse(param, out int emoneyAmount))
                            {
                                await user.AwardBoundConquerPointsAsync(emoneyAmount);
                            }

                            return true;
                        }

                    case "/awardwskill":
                        {
                            byte level = 1;

                            string[] awardwskill = param.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                            if (!ushort.TryParse(awardwskill[0], out ushort type))
                            {
                                return true;
                            }

                            if (awardwskill.Length > 1 && !byte.TryParse(awardwskill[1], out level))
                            {
                                return true;
                            }

                            if (user.WeaponSkill[type] == null)
                            {
                                await user.WeaponSkill.CreateAsync(type, level);
                            }
                            else
                            {
                                user.WeaponSkill[type].Level = level;
                                await user.WeaponSkill.SaveAsync(user.WeaponSkill[type]);
                                await user.WeaponSkill.SendAsync(user.WeaponSkill[type]);
                            }

                            return true;
                        }

                    case "/awardmagic":
                    case "/awardskill":
                        {
                            byte skillLevel = 0;
                            string[] awardSkill = param.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                            if (!ushort.TryParse(awardSkill[0], out ushort skillType))
                            {
                                return true;
                            }

                            if (awardSkill.Length > 1 && !byte.TryParse(awardSkill[1], out skillLevel))
                            {
                                return true;
                            }

                            Magic magic;
                            if (user.MagicData.CheckType(skillType))
                            {
                                magic = user.MagicData[skillType];
                                if (await magic.ChangeLevelAsync(skillLevel))
                                {
                                    await magic.SaveAsync();
                                    await magic.SendAsync();
                                }
                            }
                            else
                            {
                                if (!await user.MagicData.CreateAsync(skillType, skillLevel))
                                {
                                    await user.SendAsync("[Award Skill] Could not create skill!");
                                }
                            }

                            return true;
                        }

                    case "/awardridingpoint":
                        {
                            if (int.TryParse(param, out int ridingPoints))
                            {
                                await user.AwardHorseRacePointsAsync(ridingPoints);
                            }

                            return true;
                        }

                    case "/awardglp":
                        {
                            if (int.TryParse(param, out int goldenLeaguePoints))
                            {
                                await user.AwardGoldenLeaguePointsAsync(goldenLeaguePoints);
                            }
                            return true;
                        }

                    case "/setmetempsychosis":
                        {
                            string[] p = param.Split(' ');
                            if (p.Length == 1)
                            {
                                await user.SetAttributesAsync(ClientUpdateType.Class, uint.Parse(p[0]));
                                await user.SetAttributesAsync(ClientUpdateType.Reborn, 0);
                            }
                            else if (p.Length == 2)
                            {
                                await user.SetAttributesAsync(ClientUpdateType.Class, uint.Parse(p[1]));
                                await user.SetAttributesAsync(ClientUpdateType.FirstProfession, uint.Parse(p[0]));
                                await user.SetAttributesAsync(ClientUpdateType.Reborn, 1);
                            }
                            else if (p.Length == 3)
                            {
                                await user.SetAttributesAsync(ClientUpdateType.Class, uint.Parse(p[2]));
                                await user.SetAttributesAsync(ClientUpdateType.FirstProfession, uint.Parse(p[0]));
                                await user.SetAttributesAsync(ClientUpdateType.PreviousProfession, uint.Parse(p[1]));
                                await user.SetAttributesAsync(ClientUpdateType.Reborn, 2);
                                await user.SaveAsync();
                            }
                            return true;
                        }

                    case "/targetsetmetempsychosis":
                        {
                            string[] p = param.Split(' ');
                            Character target = RoleManager.GetUser(p[0]);
                            if (target == null)
                            {
                                await user.SendAsync("[PM] target not found.");
                                return true;
                            }

                            if (p.Length == 2)
                            {
                                await target.SetAttributesAsync(ClientUpdateType.Class, uint.Parse(p[1]));
                                await target.SetAttributesAsync(ClientUpdateType.Reborn, 0);
                            }
                            else if (p.Length == 3)
                            {
                                await target.SetAttributesAsync(ClientUpdateType.Class, uint.Parse(p[2]));
                                await target.SetAttributesAsync(ClientUpdateType.FirstProfession, uint.Parse(p[1]));
                                await target.SetAttributesAsync(ClientUpdateType.Reborn, 1);
                            }
                            else if (p.Length == 4)
                            {
                                await target.SetAttributesAsync(ClientUpdateType.Class, uint.Parse(p[3]));
                                await target.SetAttributesAsync(ClientUpdateType.FirstProfession, uint.Parse(p[1]));
                                await target.SetAttributesAsync(ClientUpdateType.PreviousProfession, uint.Parse(p[2]));
                                await target.SetAttributesAsync(ClientUpdateType.Reborn, 2);
                                await target.SaveAsync();
                            }
                            return true;
                        }

                    case "/awardallskill":
                        {
                            byte profession = (byte)(user.Profession / 10 * 10 + 1);
                            if (user.Profession > 100)
                            {
                                profession++;
                            }
                            List<ushort> magicTypes = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.FirstLifeSkills, profession);
                            if (magicTypes != null)
                            {
                                foreach (var magicOp in magicTypes)
                                {
                                    await user.MagicData.CreateAsync(magicOp, 0);
                                }
                            }
                            return true;
                        }

                    case "/dropallskills":
                        {
                            foreach (var magic in user.MagicData.Magics.Values)
                            {
                                await user.MagicData.UnlearnMagicAsync(magic.Type, true);
                            }
                            return true;
                        }

                    case "/querynpcs":
                        {
                            foreach (var npc in user.Map.Query9BlocksByPos(user.X, user.Y).Where(x => x is BaseNpc)
                                .Cast<BaseNpc>())
                            {
                                await user.SendAsync($"NPC[{npc.Identity}]:{npc.Name}({npc.X},{npc.Y})", TalkChannel.Talk);
                            }
                            return true;
                        }

                    case "/movenpc":
                        {
                            string[] moveNpcParams = param.Trim().Split(new[] { " " }, 4, StringSplitOptions.RemoveEmptyEntries);

                            if (moveNpcParams.Length < 4)
                            {
                                await user.SendAsync("Move NPC cmd must have: npcid mapid targetx targety");
                                return true;
                            }

                            if (!uint.TryParse(moveNpcParams[0], out var idNpc)
                                || !uint.TryParse(moveNpcParams[1], out var idMap)
                                || !ushort.TryParse(moveNpcParams[2], out var mapX)
                                || !ushort.TryParse(moveNpcParams[3], out var mapY))
                            {
                                return true;
                            }

                            BaseNpc npc = RoleManager.FindRole<BaseNpc>(idNpc);
                            if (npc == null)
                            {
                                await user.SendAsync($"Object {idNpc} is not of type npc");
                                return true;
                            }

                            GameMap map = MapManager.GetMap(idMap);
                            if (map == null)
                            {
                                return true;
                            }

                            if (!map.IsValidPoint(mapX, mapY))
                            {
                                return true;
                            }

                            await npc.ChangePosAsync(idMap, mapX, mapY);
                            return true;
                        }

                    case "/reloadactionall":
                        {
                            await EventManager.LoadActionsAsync();
                            await user.SendAsync("Reload action all!!!");
                            return true;
                        }

                    case "/action":
                        {
                            if (uint.TryParse(param, out uint idAction))
                            {
                                await GameAction.ExecuteActionAsync(idAction, user, null, null, string.Empty);
                            }
                            return true;
                        }

                    case "/uplev":
                        {
                            if (byte.TryParse(param, out byte uplevValue))
                            {
                                await user.AwardLevelAsync(uplevValue);
                            }

                            return true;
                        }

                    case "/awardbattlexp":
                        {
                            if (!long.TryParse(param, out var exp))
                            {
                                return true;
                            }

                            await user.AwardBattleExpAsync(exp, true);
                            return true;
                        }

                    case "/awardexpball":
                        {
                            if (user.Map != null && user.Map.IsNoExpMap())
                            {
                                await user.SendAsync($"Cannot award experience in no-exp map.");
                                return true;
                            }

                            if (!int.TryParse(param, out var amount))
                            {
                                return true;
                            }

                            var exp = user.CalculateExpBall(Math.Max(1, amount) * Role.EXPBALL_AMOUNT);
                            await user.AwardExperienceAsync(exp);
                            return true;
                        }

                    case "/awardcultivation":
                        {
                            if (int.TryParse(param, out int amount))
                            {
                                await user.AwardCultivationAsync(amount);
                            }
                            return true;
                        }

                    case "/awardstrength":
                        {
                            if (int.TryParse(param, out int amount))
                            {
                                await user.AwardStrengthValueAsync(amount);
                            }
                            return true;
                        }

                    case "/awardculture":
                        {
                            if (int.TryParse(param, out int amount))
                            {
                                await user.AwardCultureAsync(amount);
                            }
                            return true;
                        }

                    case "/vip":
                        {
                            if (byte.TryParse(param, out byte vip))
                            {
                                await user.SetAttributesAsync(ClientUpdateType.VipLevel, vip);
                            }

                            return true;
                        }

                    case "/ctf":
                        {
                            CaptureTheFlag captureTheFlag = EventManager.GetEvent<CaptureTheFlag>();
                            if (captureTheFlag != null)
                            {
                                await captureTheFlag.PrepareEventAsync();
                                await BroadcastWorldMsgAsync($"{user.Name} has forced Capture The Flag to start!!!", TalkChannel.Talk);
                            }
                            return true;
                        }

                    case "/endctf":
                        {
                            CaptureTheFlag captureTheFlag = EventManager.GetEvent<CaptureTheFlag>();
                            if (captureTheFlag != null)
                            {
                                await captureTheFlag.EndEventAsync();
                                await BroadcastWorldMsgAsync($"{user.Name} has forced Capture The Flag to end!!!", TalkChannel.Talk);
                            }
                            return true;
                        }

                    case "/creategen":
                        {
                            await user.SendAsync(
                                "Attention, use this command only on localhost tests or the generator thread may crash.");
                            // mobid mapid mapx mapy boundcx boundcy maxnpc rest maxpergen
                            string[] szComs = param.Split(' ');
                            if (szComs.Length < 9)
                            {
                                await user.SendAsync(
                                    "/creategen mobid mapid mapx mapy boundcx boundcy maxnpc rest maxpergen timerBegin timerEnd");
                                return true;
                            }

                            ushort idMob = ushort.Parse(szComs[0]);
                            uint idMap = uint.Parse(szComs[1]);
                            ushort mapX = ushort.Parse(szComs[2]);
                            ushort mapY = ushort.Parse(szComs[3]);
                            ushort boundcx = ushort.Parse(szComs[4]);
                            ushort boundcy = ushort.Parse(szComs[5]);
                            ushort maxNpc = ushort.Parse(szComs[6]);
                            ushort restSecs = ushort.Parse(szComs[7]);
                            ushort maxPerGen = ushort.Parse(szComs[8]);
                            int timerBegin = int.Parse(szComs[9]);
                            int timerEnd = int.Parse(szComs[10]);

                            if (idMap == 0)
                            {
                                idMap = user.MapIdentity;
                            }

                            if (mapX == 0 || mapY == 0)
                            {
                                mapX = user.X;
                                mapY = user.Y;
                            }

                            var newGen = new DbGenerator
                            {
                                Mapid = idMap,
                                Npctype = idMob,
                                BoundX = mapX,
                                BoundY = mapY,
                                BoundCx = boundcx,
                                BoundCy = boundcy,
                                MaxNpc = maxNpc,
                                RestSecs = restSecs,
                                MaxPerGen = maxPerGen,
                                BornX = 0,
                                BornY = 0,
                                TimerBegin = timerBegin,
                                TimerEnd = timerEnd
                            };

                            await BroadcastNpcMsgAsync(new MsgAiGeneratorManage(newGen));
                            return true;
                        }

                    case "/querymonster":
                        {
                            List<Monster> monsters;
                            if (uint.TryParse(param, out var idMonster))
                            {
                                monsters = user.Map.QueryRoles(x => x is Monster m && m.Type == idMonster).Cast<Monster>().ToList();
                            }
                            else
                            {
                                monsters = user.Map.QueryRoles(x => x is Monster m && m.Name.Equals(param)).Cast<Monster>().ToList();
                            }

                            if (monsters.Count == 0)
                            {
                                await user.SendAsync($"No monsters found!", TalkChannel.Talk);
                                return true;
                            }

                            var first = monsters.OrderBy(x => x.GetDistance(user)).FirstOrDefault();
                            await user.SendAsync($"{monsters.Count} monsters found! First: {(first != null ? $"{first.X},{first.Y}" : "None")}", TalkChannel.Talk);
                            return true;
                        }

                    case "/kickoutall":
                        {
                            await RoleManager.KickOutAllAsync(param, true);
                            return true;
                        }

                    case "/deletemagics":
                        {
                            foreach (var magic in user.MagicData.Magics.Values)
                            {
                                await user.MagicData.UnlearnMagicAsync(magic.Type, true);
                            }
                            return true;
                        }

                    case "/fixmyrebornskills":
                        {
                            if (user.Metempsychosis < 2)
                            {
                                await user.SendAsync("You are not second rebirth to use this command.", TalkChannel.Talk, Color.White);
                                return true;
                            }

                            byte[] professions = new byte[3];
                            professions[0] = (byte)(user.FirstProfession / 10 * 10 + 1);
                            if (professions[0] > 100)
                            {
                                professions[0]++;
                            }
                            professions[1] = (byte)(user.PreviousProfession / 10 * 10 + 1);
                            if (professions[1] > 100)
                            {
                                professions[1]++;
                            }
                            professions[2] = (byte)(user.Profession / 10 * 10 + 1);
                            if (professions[2] > 100)
                            {
                                professions[2]++;
                            }

                            foreach (var magic in user.MagicData.Magics.Values)
                            {
                                await user.MagicData.UnlearnMagicAsync(magic.Type, true);
                            }

                            for (int i = 0; i < 3; i++)
                            {
                                foreach (var skills in ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.FirstLifeSkills, professions[i]))
                                {
                                    await user.MagicData.CreateAsync(skills, 0);
                                }

                                if (i == 0)
                                {
                                    continue;
                                }

                                int currentProfession = professions[i] / 10 * 10 + 1;
                                if (currentProfession >= 100)
                                {
                                    currentProfession++;
                                }
                                int previousProfession = professions[i - 1] / 10 * 10 + 5;

                                var removeSkills = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.RemoveOnRebirth, previousProfession, currentProfession, i);
                                foreach (var skills in removeSkills)
                                {
                                    await user.MagicData.UnlearnMagicAsync(skills, true);
                                }

                                var resetSkills = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.ResetOnRebirth, previousProfession, currentProfession, i);
                                foreach (var skills in resetSkills)
                                {
                                    await user.MagicData.ResetSkillAsync(skills);
                                }

                                var learnSkills = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.LearnAfterRebirth, previousProfession, currentProfession, i);
                                foreach (var skills in learnSkills)
                                {
                                    await user.MagicData.CreateAsync(skills, 0);
                                }
                            }

                            foreach (var magic in user.MagicData.Magics.Values)
                            {
                                if (magic.Level >= magic.MaxLevel)
                                {
                                    continue;
                                }

                                await magic.ChangeLevelAsync(magic.MaxLevel);
                                await magic.SendAsync();
                            }

                            return true;
                        }

                    case "/setguildwar":
                        {
                            string[] splitParams = param.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                            if (splitParams.Length < 2)
                            {
                                return true;
                            }

                            uint startTime, endTime;
                            if (!uint.TryParse(splitParams[0], out startTime)
                                || !uint.TryParse(splitParams[1], out endTime))
                            {
                                return true;
                            }

                            GameMap map = MapManager.GetMap(1038);
                            if (map == null)
                            {
                                return true;
                            }

                            DynamicNpc dynamicNpc = map.QueryRole<DynamicNpc>(810);
                            if (dynamicNpc == null)
                            {
                                return true;
                            }

                            // 84 99
                            dynamicNpc.SetData("data1", (int)startTime);
                            dynamicNpc.SetData("data2", (int)endTime);
                            await dynamicNpc.SaveAsync();
                            return true;
                        }

                    case "/testmsgshutdown":
                        {
                            using var writer = new PacketWriter();
                            writer.Write((ushort)1350);
                            writer.Write(0);
                            writer.Write(0xA);
                            await user.SendAsync(writer.ToArray());
                            return true;
                        }

                    case "/testtrapmesh":
                        {
                            const int range = 24;
                            uint itemtype = 1;
                            int i = 0;
                            for (int x = 0; x < range; x++)
                            {
                                for (int y = 0; y < range; y++)
                                {
                                    MsgMapItem msg = new MsgMapItem
                                    {
                                        Identity = (uint)(990000 + i),
                                        Itemtype = itemtype++,
                                        MapX = (ushort)(user.X - (range / 2) + x),
                                        MapY = (ushort)(user.Y - (range / 2) + y),
                                        Mode = MsgMapItem.DropType.SynchroTrap
                                    };
                                    await user.SendAsync(msg);
                                }
                            }

                            await user.SendAsync($"Last trap mesh: {itemtype}", TalkChannel.Talk);
                            return true;
                        }

                    case "/testeffect":
                        {
                            string[] splitParams = param.Split(' ');
                            int status = int.Parse(splitParams[0]);
                            if (splitParams.Length < 2 || !int.TryParse(splitParams[1], out var power))
                            {
                                power = 50;
                            }
                            if (splitParams.Length < 1 || !int.TryParse(splitParams[2], out var time))
                            {
                                time = 5;
                            }
                            await user.AttachStatusAsync(status, power, time, 0);
                            return true;
                        }

                    case "/startlinepk":
                        {
                            var linePk = EventManager.GetEvent<LineSkillPK>();
                            if (linePk == null)
                            {
                                await user.SendAsync("Event not found.");
                                return true;
                            }

                            linePk.ForceStartup();
                            return true;
                        }

                    case "/endlinepk":
                        {
                            var linePk = EventManager.GetEvent<LineSkillPK>();
                            if (linePk == null)
                            {
                                await user.SendAsync("Event not found.");
                                return true;
                            }

                            linePk.ForceEnd();
                            return true;
                        }

                    case "/syncompete":
                        {
                            // packet 1062 test (MsgSynCompete)
                            using PacketWriter writer = new PacketWriter();
                            writer.Write(1062);
                            /*
                             * Offset 4: Action
                             * Type: 0 ranking?
                             * Type: 1
                             * Type: 2 Set Hero merit
                             */
                            writer.Write(0);
                            /*
                             * Offset 8: Count
                             */
                            writer.Write(1);

                            // Apparently some offsets jump
                            writer.Write(0);
                            //writer.Write(0);
                            //writer.Write(0);
                            //writer.Write(0);

                            // struct has 48(0x30) bytes and starts at 28(0x1C)
                            writer.Write("Um~string~grande", 0x24);
                            writer.Write(1); // Ranking
                            writer.Write(12); // SynID???
                            writer.Write(0); // Unknown!

                            await user.SendAsync(writer.ToArray());
                            return true;
                        }

                    case "/hangup":
                        {
                            await user.SendAsync(new MsgHangUp
                            {
                                Action = (MsgHangUp.HangUpMode)4,
                                Experience = 0,
                                KillerName = user.Name
                            });
                            return true;
                        }

                    case "/testjump":
                        {
                            GameMap map = user.Map;
                            string[] splitParams = param.Split(' ');
                            if (splitParams.Length < 2)
                            {
                                await user.SendAsync($"/testjump x y");
                                return true;
                            }

                            ushort x = ushort.Parse(splitParams[0]);
                            ushort y = ushort.Parse(splitParams[1]);

                            if (user.IsJumpPass(x, y, Role.MAX_JUMP_ALTITUDE))
                            {
                                await user.SendAsync($"Jump enable");
                            }
                            else
                            {
                                await user.SendAsync($"Jump disable");
                            }
                            return true;
                        }

                    case "/synclevel":
                        {
                            await user.SynchroAttributesAsync(ClientUpdateType.Level, 0, user.Level);
                            return true;
                        }

                    case "/reloadlua":
                        {
                            if (int.TryParse(param, out var luaId))
                            {
                                LuaScriptManager.Reload(luaId);
                            }
                            return true;
                        }

                    case "/executelua":
                        {
                            LuaScriptManager.Run(user, null, null, string.Empty, param);
                            return true;
                        }

#if DEBUG
                    case "/awarditemfull":
                        {
                            if (!uint.TryParse(param, out uint idAwardItem))
                            {
                                return true;
                            }

                            DbItemtype itemtype = ItemManager.GetItemtype(idAwardItem);
                            if (itemtype == null)
                            {
                                await user.SendAsync($"[AwardItem] Itemtype {idAwardItem} not found");
                                return true;
                            }

                            Item item = new(user);
                            if (!await item.CreateAsync(itemtype, Item.ItemPosition.Inventory, true))
                            {
                                return true;
                            }

                            if (item.IsCountable())
                            {
                                item.AccumulateNum = Math.Max(1, item.AccumulateNum);
                            }

                            if (item.IsActivable())
                            {
                                await item.ActivateAsync();
                            }

                            if (item.IsEquipment())
                            {
                                if (item.IsGourd() || item.IsGarment() || item.IsAccessory() || item.IsMountArmor())
                                {
                                    item.ReduceDamage = 1;
                                }
                                else
                                {
                                    item.ChangeAddition(12);
                                    item.ReduceDamage = 7;
                                    item.Enchantment = 255;
                                }
                                
                                switch (item.GetItemSubType())
                                {
                                    case 421:
                                    case 620:
                                    case 134:
                                    case 152:
                                        {
                                            item.SocketOne = Item.SocketGem.SuperPhoenixGem;
                                            item.SocketTwo = Item.SocketGem.SuperPhoenixGem;
                                            break;
                                        }

                                    case 201:
                                        {
                                            item.SocketOne = Item.SocketGem.SuperThunderGem;
                                            item.SocketTwo = Item.SocketGem.SuperThunderGem;
                                            break;
                                        }

                                    case 202:
                                        {
                                            item.SocketOne = Item.SocketGem.SuperGloryGem;
                                            item.SocketTwo = Item.SocketGem.SuperGloryGem;
                                            break;
                                        }
                                    case 203:
                                        {
                                            break;
                                        }
                                    case 204:
                                        {
                                            item.SocketOne = Item.SocketGem.SuperThunderGem;
                                            item.SocketTwo = Item.SocketGem.SuperGloryGem;
                                            break;
                                        }

                                    case 300:
                                        {
                                            break;
                                        }

                                    default:
                                        {
                                            if (user.ProfessionSort >= 10)
                                            {
                                                item.SocketOne = Item.SocketGem.SuperTortoiseGem;
                                                item.SocketTwo = Item.SocketGem.SuperTortoiseGem;
                                            }
                                            else
                                            {
                                                item.SocketOne = Item.SocketGem.SuperDragonGem;
                                                item.SocketTwo = Item.SocketGem.SuperDragonGem;
                                            }
                                            break;
                                        }
                                }
                            }
                            else if (item.IsMount())
                            {
                                item.ChangeAddition(12);
                            }

                            await user.UserPackage.AddItemAsync(item, false);
                            return true;
                        }

                    case "/elitepk":
                        {
                            ElitePkTournament elitePkTournament = EventManager.GetEvent<ElitePkTournament>();
                            if (elitePkTournament == null)
                            {
                                await EventManager.RegisterEventAsync(new ElitePkTournament());
                                await user.SendAsync("Event is not registered! Registering now... Please, run the command again");
                                return true;
                            }

                            if (param.StartsWith("start"))
                            {
                                string[] splitParams = param.Split(' ');
                                if (splitParams.Length != 5)
                                {
                                    await user.SendAsync("Command must be: /elitepk start amount0 amount1 amount2 amount3");
                                    return true;
                                }

                                if (elitePkTournament.IsActive)
                                {
                                    await user.SendAsync("Elite PK Tournament is already running");
                                    return true;
                                }

                                await elitePkTournament.EmulateEventAsync(splitParams.Skip(1).Select(int.Parse).ToArray());
                            }
                            else if (param.Equals("step"))
                            {
                                await elitePkTournament.StepAsync(3);
                            }
                            else if (param.Equals("stop"))
                            {
                                await elitePkTournament.StopAsync();
                            }
                            return true;
                        }

                    case "/msgpkelitematchinfo":
                        {
                            MsgPkEliteMatchInfo msg = new MsgPkEliteMatchInfo
                            {
                                Group = 3,
                                Mode = MsgPkEliteMatchInfo.ElitePkMatchType.RequestInformation,
                                Gui = MsgPkEliteMatchInfo.ElitePkGuiType.Top8Qualifier,
                                TimeLeft = 1
                            };
                            await user.SendAsync(msg);

                            msg = new()
                            {
                                Group = 3,
                                Gui = MsgPkEliteMatchInfo.ElitePkGuiType.Top8Qualifier,
                                Mode = MsgPkEliteMatchInfo.ElitePkMatchType.MainPage
                            };
                            await user.SendAsync(msg);

                            msg.Mode = MsgPkEliteMatchInfo.ElitePkMatchType.GuiUpdate;
                            msg.TimeLeft = 60;
                            await user.SendAsync(msg);

                            msg.Mode = MsgPkEliteMatchInfo.ElitePkMatchType.UpdateList;
                            msg.TotalMatches = 1;
                            msg.TimeLeft = 60;

                            msg.Matches.Add(new MsgPkEliteMatchInfo.MatchInfo
                            {
                                MatchIdentity = 10001,
                                Index = 0,
                                Status = States.Events.Tournament.BaseTournamentMatch<ElitePkParticipant, Character>.MatchStatus.AcceptingWagers,
                                ContestantInfos = new List<MsgPkEliteMatchInfo.MatchContestantInfo>
                                {
                                    new MsgPkEliteMatchInfo.MatchContestantInfo
                                    {
                                        Flag = States.Events.Tournament.BaseTournamentMatch<ElitePkParticipant, Character>.ContestantFlag.Waiting,
                                        Identity = 10_000_003,
                                        Mesh = 1003,
                                        Name = "Teste3",
                                        ServerId = 0,
                                        Winner = false
                                    },
                                    new MsgPkEliteMatchInfo.MatchContestantInfo
                                    {
                                        Flag = States.Events.Tournament.BaseTournamentMatch<ElitePkParticipant, Character>.ContestantFlag.Waiting,
                                        Identity = 10_000_001,
                                        Mesh = 1003,
                                        Name = "Teste1",
                                        ServerId = 0,
                                        Winner = false
                                    },
                                    new MsgPkEliteMatchInfo.MatchContestantInfo
                                    {
                                        Flag = States.Events.Tournament.BaseTournamentMatch<ElitePkParticipant, Character>.ContestantFlag.Waiting,
                                        Identity = 10_000_002,
                                        Mesh = 1003,
                                        Name = "Teste2",
                                        ServerId = 0,
                                        Winner = false
                                    }
                                }
                            });

                            await user.SendAsync(msg);
                            return true;
                        }

                    case "/testupdate":
                        {
                            string[] splitParams = param.Split(' ');
                            if (splitParams.Length < 2)
                            {
                                return true;
                            }

                            if (!int.TryParse(splitParams[0], out var action)
                                || !uint.TryParse(splitParams[1], out var data))
                            {
                                return true;
                            }

                            await user.SynchroAttributesAsync((ClientUpdateType) action, data);
                            return true;
                        }

                    case "/racetrackprop":
                        {
                            await user.SendAsync(new MsgRaceTrackProp
                            {
                                Index = 1,
                                Amount = 1,
                                PotionType = States.Events.Mount.HorseRacing.ItemType.DizzyHammer,
                                Data = 0
                            });
                            return true;
                        }

                    case "/competerank":
                        {
                            await user.SendAsync(new MsgCompeteRank
                            {
                                Mode = MsgCompeteRank.Action.EndTime,
                                Rank = 1,
                                Param = 123465,
                                Data = 1000,
                                Time = 123465,
                                Prize = 1000
                            });
                            return true;
                        }

                    case "/tradetest":
                        {
                            await user.SendAsync(new MsgTrade
                            {
                                Action = MsgTrade.TradeAction.SuspiciousTradeNotify
                            });
                            return true;
                        }

                    case "/tradebuddy":
                        {
                            await user.SendAsync(new MsgTradeBuddy
                            {
                                Action = (MsgTradeBuddy.TradeBuddyAction)int.Parse(param),
                                HoursLeft = 23,
                                Identity = 1000012,
                                IsOnline = true,
                                Name = "Pirate[PM]"
                            });
                            return true;
                        }
#endif
                }
            }

            if (user.IsGm())
            {
                switch (command.ToLower())
                {
                    case "/bring":
                        {
                            if (user.MapIdentity == 5000)
                            {
                                await user.SendAsync("You cannot bring players to GM area.");
                                return true;
                            }

                            Character bringTarget;
                            if (uint.TryParse(param, out uint idFindTarget))
                            {
                                bringTarget = RoleManager.GetUser(idFindTarget);
                            }
                            else
                            {
                                bringTarget = RoleManager.GetUser(param);
                            }

                            if (bringTarget == null)
                            {
                                await user.SendAsync("Target not found");
                                return true;
                            }

                            if ((bringTarget.MapIdentity == 6002 || bringTarget.MapIdentity == 6010) && !user.IsPm())
                            {
                                await user.SendAsync("You cannot move players from jail.");
                                return true;
                            }

                            await bringTarget.FlyMapAsync(user.MapIdentity, user.X, user.Y);
                            return true;
                        }
                    case "/cmd":
                        {
                            string[] cmdParams = param.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                            string subCmd = cmdParams[0];

                            if (command.Length > 1)
                            {
                                string subParam = cmdParams[1];

                                switch (subCmd.ToLower())
                                {
                                    case "broadcast":
                                        await BroadcastWorldMsgAsync(subParam, TalkChannel.Center,
                                            Color.White);
                                        break;

                                    case "gmmsg":
                                        await BroadcastWorldMsgAsync($"{user.Name} says: {subParam}",
                                            TalkChannel.Center, Color.White);
                                        break;

                                    case "player":
                                        if (subParam.Equals("all", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            await user.SendAsync(
                                                $"Players Online: {RoleManager.OnlinePlayers}, Distinct: {RoleManager.OnlineUniquePlayers} (max: {RoleManager.MaxOnlinePlayers})",
                                                TalkChannel.TopLeft, Color.White);
                                        }
                                        else if (subParam.Equals("map", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            await user.SendAsync(
                                                $"Map Online Players: {user.Map.PlayerCount} ({user.Map.Name})",
                                                TalkChannel.TopLeft, Color.White);
                                        }

                                        break;
                                }

                                return true;
                            }

                            return true;
                        }
                    case "/chgmap":
                        {
                            string[] chgMapParams = param.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                            if (chgMapParams.Length < 3)
                            {
                                return true;
                            }

                            if (uint.TryParse(chgMapParams[0], out uint chgMapId)
                                && ushort.TryParse(chgMapParams[1], out ushort chgMapX)
                                && ushort.TryParse(chgMapParams[2], out ushort chgMapY))
                            {
                                await user.FlyMapAsync(chgMapId, chgMapX, chgMapY);
                            }

                            return true;
                        }

                    case "/openui":
                        {
                            if (uint.TryParse(param, out uint ui))
                            {
                                await user.SendAsync(new MsgAction
                                {
                                    Action = MsgAction.ActionType.ClientCommand,
                                    Identity = user.Identity,
                                    Command = ui,
                                    ArgumentX = user.X,
                                    ArgumentY = user.Y
                                });
                            }
                            return true;
                        }

                    case "/openwindow":
                        {
                            if (uint.TryParse(param, out uint window))
                            {
                                await user.SendAsync(new MsgAction
                                {
                                    Action = MsgAction.ActionType.ClientDialog,
                                    Identity = user.Identity,
                                    Command = window,
                                    ArgumentX = user.X,
                                    ArgumentY = user.Y
                                });
                            }

                            return true;
                        }

                    case "/kickout":
                        {
                            Character findTarget;
                            if (uint.TryParse(param, out uint idFindTarget))
                            {
                                findTarget = RoleManager.GetUser(idFindTarget);
                            }
                            else
                            {
                                findTarget = RoleManager.GetUser(param);
                            }

                            if (findTarget == null)
                            {
                                await user.SendAsync("Target not found");
                                return true;
                            }

                            try
                            {
                                findTarget.Client.Disconnect();
                            }
                            catch (Exception ex)
                            {
                                logger.LogCritical(ex, "Error on kickout", ex.Message);
                                Kernel.Services.Processor.Queue(ServerProcessor.NO_MAP_GROUP, () =>
                                {
                                    RoleManager.ForceLogoutUser(findTarget.Identity);
                                    return Task.CompletedTask;
                                });
                            }

                            return true;
                        }

                    case "/find":
                        {
                            Character findTarget = RoleManager.GetUser(param);
                            if (findTarget == null && uint.TryParse(param, out uint idFindTarget))
                            {
                                findTarget = RoleManager.GetUser(idFindTarget);
                            }

                            if (findTarget == null)
                            {
                                await user.SendAsync("Target not found");
                                return true;
                            }

                            await user.FlyMapAsync(findTarget.MapIdentity, findTarget.X, findTarget.Y);
                            return true;
                        }

                    case "/bot":
                        {
                            string[] myParams = param.Split(new[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);

                            if (myParams.Length < 2)
                            {
                                await user.SendAsync("/bot [target_name] [reason]", TalkChannel.Talk);
                                return true;
                            }

                            Character target = RoleManager.GetUser(myParams[0]);
                            if (target != null)
                            {
                                await target.SendAsync(StrBotjail);
                                await target.FlyMapAsync(6002, 28, 74);
                                await target.SaveAsync();
                                await target.SendAsync(new MsgCheatingProgram(target.Identity, myParams[1]));
                            }
                            return true;
                        }

                    case "/macro":
                        {
                            string[] myParams = param.Split(new[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);

                            if (myParams.Length < 2)
                            {
                                await user.SendAsync("/macro [target_name] [reason]", TalkChannel.Talk);
                                return true;
                            }

                            Character target = RoleManager.GetUser(myParams[0]);
                            if (target != null)
                            {
                                await target.SendAsync(StrMacrojail);
                                await target.FlyMapAsync(6010, 28, 74);
                                await target.SaveAsync();
                                await target.SendAsync(new MsgCheatingProgram(target.Identity, myParams[1]));
                            }
                            return true;
                        }

                    case "/fly":
                        {
                            string[] chgMapParams = param.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                            if (chgMapParams.Length < 2)
                            {
                                return true;
                            }

                            int x = int.Parse(chgMapParams[0]);
                            int y = int.Parse(chgMapParams[1]);

                            if (!user.Map.IsStandEnable(x, y))
                            {
                                await user.SendAsync(StrInvalidCoordinate);
                                return true;
                            }

                            var error = false;
                            List<Role> roleSet = user.Map.Query9Blocks(x, y);
                            foreach (Role role in roleSet)
                            {
                                if (role is BaseNpc npc
                                    && role.X == x && role.Y == y)
                                {
                                    error = true;
                                    break;
                                }
                            }

                            if (!error)
                            {
                                await user.FlyMapAsync(0, x, y);
                            }
                            else
                            {
                                await user.SendAsync(StrInvalidCoordinate);
                            }

                            return true;
                        }

                    case "/flymap":
                        {
                            uint idMap = uint.Parse(param);

                            GameMap map = MapManager.GetMap(idMap);
                            if (map == null)
                            {
                                await user.SendAsync($"Invalid map /flymap {param}");
                                logger.LogError($"Invalid map /flymap {param}");
                                return true;
                            }

                            var pos = await map.QueryRandomPositionAsync(0, 0, map.Width);
                            if (pos != default)
                            {
                                await user.FlyMapAsync(idMap, pos.X, pos.Y);
                            }
                            return true;
                        }

                    case "/stopevent":
                        {
                            if (Enum.TryParse<GameEvent.EventType>(param, true, out var result))
                            {
                                GameEvent evt = EventManager.GetEvent(result);
                                if (evt != null)
                                {
                                    EventManager.RemoveEvent(result);
                                    await BroadcastWorldMsgAsync($"Event {result} has been stopped.", TalkChannel.Talk, Color.White);
                                }
                            }
                            return true;
                        }

                    case "/activegenerators":
                        {
                            List<uint> generators = new();
                            foreach (var mob in user.Screen.Roles.Values.Where(x => x is Monster).Cast<Monster>())
                            {
                                if (!generators.Contains(mob.GeneratorId))
                                {
                                    generators.Add(mob.GeneratorId);
                                }
                            }

                            foreach (var generatorId in generators)
                            {
                                await user.SendAsync($"Generator ID: {generatorId}", TalkChannel.Talk, System.Drawing.Color.White);
                            }
                            return true;
                        }
                }
            }

            switch (command.ToLower())
            {
                case "/dc":
                case "/discnonect":
                    {
                        user.Client.Disconnect();
                        return true;
                    }

                case "/clearinventory":
                    {
                        if (user.MessageBox != null)
                        {
                            await user.SendAsync(StrClearInventoryCloseBoxes);
                            return true;
                        }

                        user.MessageBox = new CleanInventoryMessageBox(user);
                        await user.MessageBox.SendAsync();
                        return true;
                    }

                case "/pos":
                    {
                        await user.SendAsync($"[{user.MapIdentity}] {user.Map?.Name} {user.X} {user.Y}");
                        return true;
                    }

                case "/bp":
                    {
                        await user.SendAsync($"Current Battle Power: {user.BattlePower}, Raw Battle Power: {user.PureBattlePower}");
                        return true;
                    }

                case "/sync":
                    {
                        await user.KickbackAsync();
                        await user.Screen.SynchroScreenAsync();
                        return true;
                    }
            }

            return false;
        }
    }

    /// <summary>
    ///     Enumeration for defining the channel text is printed to. Can also print to
    ///     separate states of the client such as character registration, and can be
    ///     used to change the state of the client or deny a login.
    /// </summary>
    public enum TalkChannel : ushort
    {
        Talk = 2000,
        Whisper,
        Action,
        Team,
        Guild,
        Family = 2006,
        System,
        Yell,
        Friend,
        Center = 2011,
        TopLeft,
        Ghost,
        Service,
        Tip,
        World = 2021,
        Qualifier = 2022,
        Ally = 2025,
        Register = 2100,
        Login,
        Shop,
        Vendor = 2104,
        Website,
        GuildWarRight1 = 2108,
        GuildWarRight2,
        Offline,
        Announce,
        MessageBox,
        TradeBoard = 2201,
        FriendBoard,
        TeamBoard,
        GuildBoard,
        OthersBoard,
        Bbs,
        Broadcast = 2500,
        Monster = 2600
    }

    /// <summary>
    ///     Enumeration type for controlling how text is stylized in the client's chat
    ///     area. By default, text appears and fades overtime. This can be overridden
    ///     with multiple styles, hard-coded into the client.
    /// </summary>
    [Flags]
    public enum TalkStyle : ushort
    {
        Normal = 0,
        Scroll = 1 << 0,
        Flash = 1 << 1,
        Blast = 1 << 2
    }
}
