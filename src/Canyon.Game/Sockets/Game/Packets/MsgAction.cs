using Canyon.Database.Entities;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Ai.Packets;
using Canyon.Game.States;
using Canyon.Game.States.Events.Mount;
using Canyon.Game.States.Items;
using Canyon.Game.States.Magics;
using Canyon.Game.States.Relationship;
using Canyon.Game.States.User;
using Canyon.Game.States.World;
using Canyon.Network.Packets;
using Canyon.Network.Packets.Ai;
using Newtonsoft.Json;
using System.Drawing;
using static Canyon.Game.States.Role;
using static Canyon.Game.States.User.Character;

namespace Canyon.Game.Sockets.Game.Packets
{
    /// <remarks>Packet Type 1010</remarks>
    /// <summary>
    ///     Message containing a general action being performed by the client. Commonly used
    ///     as a request-response protocol for question and answer like exchanges. For example,
    ///     walk requests are responded to with an answer as to if the step is legal or not.
    /// </summary>
    public sealed class MsgAction : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAction>();

        public MsgAction()
        {
            Timestamp = (uint)Environment.TickCount;
        }

        // Packet Properties
        public int Unknown { get; set; }
        public uint Timestamp { get; set; }
        public uint Identity { get; set; }
        public uint Data { get; set; }
        public uint Command { get; set; }

        public ushort CommandX
        {
            get => (ushort)(Command - (CommandY << 16));
            set => Command = (uint)(CommandY << 16 | value);
        }

        public ushort CommandY
        {
            get => (ushort)(Command >> 16);
            set => Command = (uint)(value << 16) | Command;
        }

        public uint Argument { get; set; }

        public ushort ArgumentX
        {
            get => (ushort)(Argument - (ArgumentY << 16));
            set => Argument = (uint)(ArgumentY << 16 | value);
        }

        public ushort ArgumentY
        {
            get => (ushort)(Argument >> 16);
            set => Argument = (uint)(value << 16) | Argument;
        }
        public ushort Direction { get; set; }
        public ActionType Action { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public uint Map { get; set; }
        public uint MapColor { get; set; }
        public byte Sprint { get; set; }
        public List<string> Strings { get; init; } = new();

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
            Unknown = reader.ReadInt32(); // 4
            Identity = reader.ReadUInt32(); // 8
            Command = reader.ReadUInt32(); // 12
            Argument = reader.ReadUInt32(); // 16
            Timestamp = reader.ReadUInt32(); // 20
            Action = (ActionType)reader.ReadUInt16(); // 24
            Direction = reader.ReadUInt16(); // 26
            X = reader.ReadUInt16(); // 28
            Y = reader.ReadUInt16(); // 30
            Map = reader.ReadUInt32(); // 32
            MapColor = reader.ReadUInt32(); // 36
            Sprint = reader.ReadByte(); // 40
            Strings.AddRange(reader.ReadStrings()); // 41
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
            writer.Write((ushort)PacketType.MsgAction);
            writer.Write(Environment.TickCount); // 4
            writer.Write(Identity); // 8
            writer.Write(Command); // 12
            writer.Write(Argument); // 16
            writer.Write(Timestamp); // 20
            writer.Write((ushort)Action); // 24
            writer.Write(Direction); // 26
            writer.Write(X); // 28
            writer.Write(Y); // 30
            writer.Write(Map); // 32
            writer.Write(MapColor); // 36
            writer.Write(Sprint); // 40
            writer.Write(Strings); // 41
            return writer.ToArray();
        }

        /// <summary>
        ///     Defines actions that may be requested by the user, or given to by the server.
        ///     Allows for action handling as a packet subtype. Enums should be named by the
        ///     action they provide to a system in the context of the player actor.
        /// </summary>
        public enum ActionType
        {
            LoginSpawn = 74,
            LoginInventory,
            LoginRelationships,
            LoginProficiencies,
            LoginSpells,
            CharacterDirection,
            CharacterEmote = 81,
            MapPortal = 85,
            MapTeleport,
            CharacterLevelUp = 92,
            SpellAbortXp,
            CharacterRevive,
            CharacterDelete,
            CharacterPkMode,
            LoginGuild,
            MapMine = 99,
            MapTeamLeaderStar = 101,
            MapQuery,
            AbortMagic = 103,
            MapArgb = 104,
            MapTeamMemberStar = 106,
            Kickback = 108,
            SpellRemove,
            ProficiencyRemove,
            BoothSpawn,
            BoothSuspend,
            BoothResume,
            BoothLeave,
            ClientCommand = 116,
            CharacterObservation,
            SpellAbortTransform,
            SpellAbortFlight = 120,
            MapGold,
            RelationshipsEnemy = 123,
            ClientDialog = 126,
            MapEffect = 134,
            RemoveEntity = 135,
            MapJump = 137,
            CharacterDead = 145,
            SyncScreen = 146,
            RelationshipsFriend = 148,
            CharacterAvatar = 151,
            QueryTradeBuddy = 152,
            ItemDetained = 153,
            ItemDetainedEx = 155,
            NinjaStep = 156,
            Countdown = 159,
            OpenShop = 160,
            Away = 161,
            PathFinding = 162,
            MonsterTrap = 163,
            ProgressBar = 164,
            BulletinInviteTrans = 166,
            MouseSetFace = 171,
            MouseProcess = 172,
            MouseCancel = 173,
            MouseResetFace = 174,
            MouseResetClick = 175,
            ShowType = 178,
            LoginComplete = 251,
            UpgradeMagicSkill = 252,
            UpgradeWeaponSkill = 253,
            AwardFirstCredit = 255,
            InventorySash = 256,
            FriendObservation = 310,
            StartRaceTrack = 401,
            EndRaceTrack = 402,
            UserAttribInfo = 408,
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            Role role = RoleManager.GetRole(Identity);
            Character targetUser = RoleManager.GetUser(Command);

            switch (Action)
            {
                case ActionType.CharacterDirection:   // 79
                case ActionType.CharacterEmote:       // 81
                case ActionType.CharacterObservation: // 117
                case ActionType.FriendObservation:    // 310
                    {
                        if (user.Identity == Identity)
                        {
                            user.BattleSystem.ResetBattle();
                            await user.MagicData.AbortMagicAsync(true);
                        }
                        break;
                    }
            }

            switch (Action)
            {
                case ActionType.LoginSpawn: // 74
                    {
                        if (user == null)
                        {
                            return;
                        }

                        Identity = user.Identity;

                        if (user.IsOfflineTraining)
                        {
                            user.MapIdentity = 601;
                            user.X = 61;
                            user.Y = 54;
                        }

                        if (user.MapIdentity == 5000 && !user.IsGm())
                        {
                            user.MapIdentity = 6002;
                            user.X = 61;
                            user.Y = 54;
                            await user.SavePositionAsync(6002, 61, 54);
                        }
#if !DEBUG
                        else if (user.IsGm())
                        {
                            user.MapIdentity = 5000;
                            user.X = 37;
                            user.Y = 73;
                        }
#endif                   

                        GameMap targetMap = MapManager.GetMap(user.MapIdentity);
                        if (targetMap == null)
                        {
                            await user.SavePositionAsync(1002, 300, 278);
                            client.Disconnect();
                            return;
                        }

                        Command = targetMap.MapDoc;
                        X = user.X;
                        Y = user.Y;
                        Map = targetMap.Identity;

                        async Task enterMapPartitionTask()
                        {
                            await user.EnterMapAsync();
                            await GameAction.ExecuteActionAsync(1000000, user, null, null, "");
                            await user.SendAsync(this);
                            await user.SendAsync(new MsgHangUp
                            {
                                Param = 341
                            });
                            if (user.Life == 0)
                            {
                                await user.SetAttributesAsync(ClientUpdateType.Hitpoints, 10);
                            }
                        }

                        Kernel.Services.Processor.Queue(targetMap.Partition, enterMapPartitionTask); // sends the current player from Partition 0 the proper partition
                        break;
                    }

                case ActionType.LoginInventory: // 75
                    {
                        await user.SynchroAttributesAsync(ClientUpdateType.CurrentSashSlots, user.SashSlots);
                        await user.SynchroAttributesAsync(ClientUpdateType.MaximumSashSlots, MAXIMUM_SASH_SLOTS);
                        await user.UserPackage.SendAsync();
                        await user.SendDetainRewardAsync();
                        await user.SendDetainedEquipmentAsync();
                        await user.SendAsync(this);
                        break;
                    }

                case ActionType.LoginRelationships: // 76
                    {
                        await user.PkStatistic.InitializeAsync();

                        await user.LoadRelationshipAsync();
                        await user.SendAllFriendAsync();
                        await user.SendAllEnemiesAsync();

                        if (user.MateIdentity != 0)
                        {
                            Character mate = RoleManager.GetUser(user.MateIdentity);
                            if (mate != null)
                            {
                                await mate.SendAsync(user.Gender == 1
                                                         ? StrMaleMateLogin
                                                         : StrFemaleMateLogin);
                            }
                        }

                        await user.LoadGuideAsync();
                        await user.LoadTradePartnerAsync();
                        await user.LoadGoldenLeagueDataAsync();
                        await user.SendAsync(this);
                        break;
                    }

                case ActionType.LoginProficiencies: // 77
                    {
                        await user.WeaponSkill.InitializeAsync();
                        await user.WeaponSkill.SendAsync();
                        await user.AstProf.InitializeAsync();
                        await user.Fate.InitializeAsync();
                        await user.JiangHu.InitializeAsync();
                        await user.InnerStrength.InitializeAsync();
                        await user.Achievements.InitializeAsync();
                        await user.SendAsync(this);
                        break;
                    }

                case ActionType.LoginSpells: // 78
                    {
                        await user.MagicData.InitializeAsync();
                        await user.LoadMonsterKillsAsync();
                        if (user.IsGm() && !user.MagicData.CheckType(3321))
                        {
                            await user.MagicData.CreateAsync(3321, 0);
                        }
                        await user.SendAsync(this);
                        break;
                    }

                case ActionType.CharacterDirection: // 79
                    {
                        await user.SetDirectionAsync((FacingDirection)(Direction % 8), false);
                        await user.BroadcastRoomMsgAsync(this, true);
                        break;
                    }

                case ActionType.CharacterEmote: // 81
                    {
                        if (user != null && user.Identity == Identity)
                        {
                            await role.SetActionAsync((EntityAction)Command, false);

                            if ((EntityAction)Command == EntityAction.Cool && user.IsCoolEnable())
                            {
                                if (user.IsFullSuper())
                                {
                                    Argument = Command |= (uint)(user.Profession * 0x00010000 + 0x01000000);
                                }
                                else if (user.IsFullUnique())
                                {
                                    Argument = Command |= (uint)(user.Profession * 0x010000);
                                }
                            }

                            await role.BroadcastRoomMsgAsync(this, user?.Identity == Identity);
                        }
                        break;
                    }

                case ActionType.MapPortal: // 85
                    {
                        uint idMap = 0;
                        var tgtPos = new Point();
                        Point sourcePos;
                        bool result;
                        logger.LogDebug($"MapPortal: {Command} {CommandX} {CommandY} {MapColor}");
                        if (Command == 0)
                        {
                            sourcePos = new Point(user.X, user.Y);
                            result = user.Map.GetPassageById((int)MapColor, ref idMap, ref tgtPos, ref sourcePos);
                        }
                        else
                        {
                            sourcePos = new Point(CommandX, CommandY);
                            result = user.Map.GetPassageMap(ref idMap, ref tgtPos, ref sourcePos);
                        }

                        if (!result)
                        {
                            user.Map.GetRebornMap(ref idMap, ref tgtPos);
                        }

                        GameMap targetMap = MapManager.GetMap(idMap);
                        if (targetMap.IsRecordDisable())
                        {
                            await user.SavePositionAsync(user.RecordMapIdentity, user.RecordMapX, user.RecordMapY);
                        }
                        await user.FlyMapAsync(idMap, tgtPos.X, tgtPos.Y);
                        break;
                    }

                case ActionType.SpellAbortXp: // 93
                    {
                        if (user.QueryStatus(StatusSet.START_XP) != null)
                        {
                            await user.DetachStatusAsync(StatusSet.START_XP);
                        }

                        break;
                    }

                case ActionType.CharacterRevive: // 94
                    {
                        if (user == null)
                        {
                            return;
                        }

                        if (user.IsAlive || !user.CanRevive())
                        {
                            return;
                        }

                        await user.RebornAsync(Command == 0);
                        break;
                    }

                case ActionType.CharacterDelete: // 95
                    {
                        if (user == null)
                        {
                            return;
                        }

                        if (user.SecondaryPassword != Command)
                        {
                            await user.SendMenuMessageAsync(StrDeleteInvalidPassword);
                            return;
                        }


                        if (await user.DeleteCharacterAsync())
                        {
                            await RoleManager.KickOutAsync(user.Identity, "DELETED");
                        }

                        break;
                    }

                case ActionType.CharacterPkMode: // 96
                    {
                        if (!Enum.IsDefined(typeof(PkModeType), (int)Command))
                        {
                            return;
                        }

                        await user.SetPkModeAsync((PkModeType)Command);
                        break;
                    }

                case ActionType.LoginGuild: // 97
                    {
                        try
                        {
                            user.Syndicate = SyndicateManager.FindByUser(user.Identity);
                            await user.SendSyndicateAsync();
                            if (user.Syndicate != null)
                            {
                                await user.Syndicate.SendRelationAsync(user);
                            }

                            await user.LoadFamilyAsync();
                        }
                        catch (Exception ex)
                        {
                            await user.SendAsync($"[DEBUG] An error occured when loading guilds. {ex.Message}");
                            logger.LogError(ex, $"Error when loading syndicates: {ex.Message}");
                        }
                        await user.SendAsync(this);
                        break;
                    }

                case ActionType.MapMine: // 99
                    {
                        if (user == null)
                        {
                            return;
                        }

                        if (!user.IsAlive)
                        {
                            await user.SendAsync(StrDead);
                            return;
                        }

                        if (!user.Map.IsMineField())
                        {
                            await user.SendAsync(StrNoMine);
                            return;
                        }

                        user.StartMining();
                        break;
                    }

                case ActionType.MapTeamLeaderStar: // 101
                    {
                        if (user == null)
                        {
                            return;
                        }

                        if (user.Team == null || user.Team.Leader.MapIdentity != user.MapIdentity)
                        {
                            return;
                        }

                        targetUser = user.Team.Leader;
                        CommandX = targetUser.X;
                        CommandY = targetUser.Y;
                        await user.SendAsync(this);
                        break;
                    }

                case ActionType.MapQuery: // 102
                    {
                        Role targetRole = RoleManager.GetRole(Command);
                        if (targetRole != null)
                        {
                            await targetRole.SendSpawnToAsync(user);
                        }

                        break;
                    }

                case ActionType.MapTeamMemberStar: // 106
                    {
                        if (user == null)
                        {
                            return;
                        }

                        if (user.Team == null || targetUser == null || !user.Team.IsMember(targetUser.Identity) ||
                            targetUser.MapIdentity != user.MapIdentity)
                        {
                            return;
                        }

                        Command = targetUser.MapIdentity;
                        X = targetUser.X;
                        Y = targetUser.Y;
                        await user.SendAsync(this);
                        break;
                    }

                case ActionType.BoothSpawn: // 111
                    {
                        if (user == null)
                        {
                            return;
                        }

                        if (await user.CreateBoothAsync())
                        {
                            Command = user.Booth.Identity;
                            X = user.Booth.X;
                            Y = user.Booth.Y;
                            await user.SendAsync(this);
                        }

                        break;
                    }

                case ActionType.BoothLeave: // 114
                    {
                        if (user == null)
                        {
                            return;
                        }

                        await user.DestroyBoothAsync();
                        await user.Screen.SynchroScreenAsync();
                        break;
                    }

                case ActionType.CharacterObservation: // 117
                    {
                        if (user == null)
                        {
                            return;
                        }

                        targetUser = RoleManager.GetUser(Command);
                        if (targetUser == null)
                        {
                            return;
                        }

                        for (var pos = Item.ItemPosition.EquipmentBegin;
                             pos <= Item.ItemPosition.EquipmentEnd;
                             pos++)
                        {
                            if (targetUser.UserPackage[pos] != null)
                            {
                                Item item = targetUser.UserPackage[pos];
                                await user.SendAsync(new MsgItemInfoEx(item, MsgItemInfoEx.ViewMode.ViewEquipment));
                                if (item.Quench != null)
                                {
                                    await item.Quench.SendToAsync(user);
                                }
                            }
                        }
                        await user.SendAsync(new MsgPlayerAttribInfo(targetUser));
                        break;
                    }

                case ActionType.SpellAbortTransform:
                    {
                        if (user.Transformation != null)
                        {
                            await user.ClearTransformationAsync();
                        }

                        break;
                    }

                case ActionType.SpellAbortFlight: // 120
                    {
                        if (user.QueryStatus(StatusSet.FLY) != null)
                        {
                            await user.DetachStatusAsync(StatusSet.FLY);
                        }

                        break;
                    }

                case ActionType.RelationshipsEnemy: // 123
                    {
                        if (user == null)
                        {
                            return;
                        }

                        Enemy fetchEnemy = user.GetEnemy(Command);
                        if (fetchEnemy == null)
                        {
                            await user.SendAsync(this);
                            return;
                        }

                        await fetchEnemy.SendInfoAsync();
                        break;
                    }

                case ActionType.MapJump: // 137
                    {
                        if (role == null)
                        {
                            return;
                        }

                        if (!role.IsAlive)
                        {
                            if (role is Character player)
                            {
                                await player.KickbackAsync();
                                await player.SendAsync(StrDead);
                            }
                            return;
                        }

                        if (role.Map.IsRaceTrack())
                        {
                            if (role is Character player)
                            {
                                await player.KickbackAsync();
                            }
                            return;
                        }

                        ushort newX = (ushort)Command;
                        ushort newY = (ushort)(Command >> 16);

                        if (Identity == user.Identity)
                        {
                            if (!user.IsAlive)
                            {
                                await user.SendAsync(StrDead, TalkChannel.System, Color.Red);
                                return;
                            }

                            if (user.GetDistance(newX, newY) >= 2 * Screen.VIEW_SIZE)
                            {
                                await user.SendAsync(StrInvalidMsg, TalkChannel.System, Color.Red);
                                await RoleManager.KickOutAsync(user.Identity, "big jump");
                                return;
                            }
                        }

                        ArgumentX = user.X;
                        ArgumentY = user.Y;

                        await user.ProcessOnMoveAsync();
                        bool result = await role.JumpPosAsync(newX, newY);

                        Character couple;
                        if (result
                            && user.HasCoupleInteraction()
                            && user.HasCoupleInteractionStarted()
                            && (couple = user.GetCoupleInteractionTarget()) != null)
                        {
                            await couple.ProcessOnMoveAsync();
                            couple.X = user.X;
                            couple.Y = user.Y;
                            await couple.ProcessAfterMoveAsync();

                            await user.SetDirectionAsync((FacingDirection)(Direction % 8), false);
                            await user.SendAsync(this);
                            await user.ProcessAfterMoveAsync();
                            await BroadcastNpcMsgAsync(new MsgAiAction
                            {
                                Action = AiActionType.Jump,
                                Identity = user.Identity,
                                X = user.X,
                                Y = user.Y,
                                Direction = (int)user.Direction
                            });

                            Identity = couple.Identity;
                            await couple.SetDirectionAsync((FacingDirection)(Direction % 8), false);
                            await couple.SendAsync(this);
                            await couple.ProcessAfterMoveAsync();
                            await BroadcastNpcMsgAsync(new MsgAiAction
                            {
                                Action = AiActionType.Jump,
                                Identity = couple.Identity,
                                X = couple.X,
                                Y = couple.Y,
                                Direction = (int)couple.Direction
                            });

                            MsgSyncAction msg = new()
                            {
                                Action = SyncAction.Jump,
                                X = user.X,
                                Y = user.Y
                            };
                            msg.Targets.Add(user.Identity);
                            msg.Targets.Add(couple.Identity);
                            await user.SendAsync(msg);
                            await user.Screen.UpdateAsync(msg);
                            await couple.Screen.UpdateAsync();
                        }
                        else
                        {
                            X = user.X;
                            Y = user.Y;

                            await role.SetDirectionAsync((FacingDirection)(Direction % 8), false);
                            if (role is Character roleUser)
                            {
                                await role.SendAsync(this);
                                await roleUser.Screen.UpdateAsync(this);
                            }
                            else
                            {
                                await role.BroadcastRoomMsgAsync(this, true);
                            }
                            await role.ProcessAfterMoveAsync();
                            await BroadcastNpcMsgAsync(new MsgAiAction
                            {
                                Action = AiActionType.Jump,
                                Identity = user.Identity,
                                X = user.X,
                                Y = user.Y,
                                Direction = (int)user.Direction
                            });
                        }
                        break;
                    }

                case ActionType.RelationshipsFriend: // 140
                    {
                        if (user == null)
                        {
                            return;
                        }

                        Friend fetchFriend = user.GetFriend(Command);
                        if (fetchFriend == null)
                        {
                            await user.SendAsync(this);
                            return;
                        }

                        await fetchFriend.SendInfoAsync();
                        break;
                    }

                case ActionType.QueryTradeBuddy: // 143
                    {
                        if (user == null)
                        {
                            return;
                        }

                        TradePartner partner = user.GetTradePartner(Command);
                        if (partner == null)
                        {
                            await user.SendAsync(this);
                            return;
                        }

                        await partner.SendInfoAsync();
                        break;
                    }

                case ActionType.CharacterDead: // 145
                    {
                        if (user == null)
                        {
                            return;
                        }

                        if (user.IsAlive)
                        {
                            return;
                        }

                        await user.SetGhostAsync();
                        break;
                    }

                case ActionType.SyncScreen: // 146
                    {
                        await user.Screen.SynchroScreenAsync();
                        break;
                    }

                case ActionType.CharacterAvatar:
                    {
                        if (user.Gender == 1 && Command >= 200 || user.Gender == 2 && Command < 200)
                        {
                            return;
                        }

                        user.Avatar = (ushort)Command;
                        await user.BroadcastRoomMsgAsync(this, true);
                        await user.SaveAsync();
                        break;
                    }

                case ActionType.ItemDetained:
                    {
                        await user.SendAsync(new MsgAction
                        {
                            Action = ActionType.ClientDialog,
                            X = user.X,
                            Y = user.Y,
                            Identity = user.Identity,
                            Data = 336
                        });

                        await user.SendAsync(new MsgAction
                        {
                            Action = ActionType.ClientDialog,
                            X = user.X,
                            Y = user.Y,
                            Identity = user.Identity,
                            Data = 337
                        });

                        await user.SendAsync(this);
                        break;
                    }

                case ActionType.Away: // 161
                    {
                        if (user == null)
                        {
                            return;
                        }

                        user.IsAway = Command != 0;

                        if (user.IsAway && user.Action != EntityAction.Sit)
                        {
                            await user.SetActionAsync(EntityAction.Sit, true);

                            if (user.Trade != null)
                            {
                                await user.Trade.SendCloseAsync();
                            }

                        }
                        else if (!user.IsAway && user.Action == EntityAction.Sit)
                        {
                            await user.SetActionAsync(EntityAction.Stand, true);
                        }

                        await user.BroadcastRoomMsgAsync(this, true);
                        break;
                    }

                case ActionType.MonsterTrap: // 163
                    {
                        if (user == null)
                        {
                            return;
                        }

                        if (user.Map.IsRaceTrack())
                        {
                            Monster raceItem = RoleManager.FindRole<Monster>(Command);
                            if (raceItem != null)
                            {
                                await GameAction.ExecuteActionAsync(raceItem.ActionId, user, raceItem, null, string.Empty);
                                await raceItem.LeaveMapAsync();
                            }
                        }

                        break;
                    }

                case ActionType.BulletinInviteTrans:
                    {
                        await EventManager.BulletinInvitationAsync(user, Command);
                        break;
                    }

                case ActionType.MouseProcess: // 172
                    {
                        if (user == null || user.InteractingMouseAction == 0)
                        {
                            return;
                        }

                        Role target = RoleManager.FindRole<Role>(Command);
                        await GameAction.ExecuteActionAsync(user.InteractingMouseAction, user, target, null, string.Empty);
                        break;
                    }

                case ActionType.MouseCancel:
                    {
                        if (user == null || user.InteractingMouseAction == 0)
                        {
                            return;
                        }

                        user.InteractingMouseAction = 0;
                        await user.SendAsync(new MsgAction
                        {
                            Action = ActionType.MouseResetFace,
                            X = user.X,
                            Y = user.Y,
                            Identity = user.Identity,
                        });
                        break;
                    }

                case ActionType.ShowType: // 178
                    {
                        Identity = user.Identity;
                        user.CurrentLayout = (byte)Command;
                        await user.SaveAsync();
                        await user.BroadcastRoomMsgAsync(this, true);
                        break;
                    }

                case ActionType.LoginComplete: // 251
                    {
                        await BroadcastNpcMsgAsync(new MsgAiPlayerLogin(user));

                        await user.DoDailyResetAsync(true);

                        if (user.SendFlowerTime == 0 || int.Parse(DateTime.Now.ToString("yyyyMMdd")) > user.SendFlowerTime)
                        {
                            await user.SendAsync(new MsgFlower
                            {
                                Mode = user.Gender == 1 ? MsgFlower.RequestMode.QueryFlower : MsgFlower.RequestMode.QueryGift,
                                RedRoses = 1
                            });
                        }

                        await user.SynchroAttributesAsync(ClientUpdateType.Hitpoints, user.Life);
                        await user.SynchroAttributesAsync(ClientUpdateType.Mana, user.Mana);
                        
                        await user.CheckPkStatusAsync();
                        await user.SendNobilityInfoAsync();
                        await user.LoadStatusAsync();
                        user.LoadExperienceData();
                        await user.SendMultipleExpAsync();
                        await user.SendBlessAsync();
                        await user.SendLuckAsync();
                        await user.SendMerchantAsync();
                        await user.MailBox.InitializeAsync();
                        await user.Screen.SynchroScreenAsync();
                        await user.LoadTitlesAsync();

                        await PigeonManager.SendToUserAsync(user);

                        if (!user.IsUnlocked())
                        {
                            await user.SendAsync(new Msg2ndPsw
                            {
                                Password = 0x1,
                                Action = Msg2ndPsw.PasswordRequestType.CorrectPassword
                            });
                        }

                        await user.InitializeActivityTasksAsync();
                        await ProcessGoalManager.ProcessUserCurrentGoalsAsync(user);

                        await user.UpdateTaskActivityAsync(ActivityManager.ActivityType.LoginTheGame);
                        if (user.VipLevel > 0)
                        {
                            await user.UpdateTaskActivityAsync(ActivityManager.ActivityType.VipActiveness);
                        }

                        if (user.Metempsychosis >= 2 && user.Level >= 30 && !user.JiangHu.HasJiangHu)
                        {
                            await user.SendAsync(new MsgOwnKongfuBase
                            {
                                Mode = MsgOwnKongfuBase.KongfuBaseMode.IconBar
                            });
                        }

                        if (user.Name.Contains("[Z"))
                        {
                            await user.SendAsync(new MsgChangeName
                            {
                                Action = MsgChangeName.ChangeNameAction.NameError
                            });
                        }

                        if (user.JiangHu.HasJiangHu && user.JiangHu.IsActive)
                        {
                            await user.JiangHu.SendStatusAsync();
                        }

#if !DEBUG
                        if (user.IsGm())
                        {
                            await user.TransformAsync(3321, int.MaxValue, true);
                        }
#endif

                        await user.SendAsync(this);
                        break;
                    }

                case ActionType.FriendObservation: // 310
                    {
                        targetUser = RoleManager.GetUser(Command);
                        if (targetUser == null)
                        {
                            return;
                        }

                        await targetUser.SendWindowToAsync(user);
                        await targetUser.SendAsync(string.Format(StrObservingEquipment, user.Name), TalkChannel.Talk);
                        break;
                    }

                case ActionType.UpgradeMagicSkill: // 252
                    {
                        if (!user.IsUnlocked())
                        {
                            await user.SendSecondaryPasswordInterfaceAsync();
                            return;
                        }

                        const int minCps = 1;
                        Magic skill = user.MagicData[(ushort)Command];
                        if (skill == null)
                        {
                            return;
                        }

                        if (skill.Level >= skill.MaxLevel)
                        {
                            return;
                        }

                        int amount = Math.Max(minCps, (int)((1 - skill.Experience / (double)skill.NeedExp) * skill.EmoneyCost));
                        if (!await user.SpendConquerPointsAsync(amount, true, true))
                        {
                            return;
                        }

                        await skill.ChangeLevelAsync((byte)(skill.Level + 1));
                        skill.Experience = 0;

                        await skill.SendAsync();
                        await skill.SaveAsync();
                        await user.SendAsync(this);
                        break;
                    }


                case ActionType.UpgradeWeaponSkill: // 253
                    {
                        if (!user.IsUnlocked())
                        {
                            await user.SendSecondaryPasswordInterfaceAsync();
                            return;
                        }

                        DbWeaponSkill ws = user.WeaponSkill[(ushort)Command];
                        if (ws == null)
                        {
                            return;
                        }

                        if (ws.Level >= MAX_WEAPONSKILLLEVEL)
                        {
                            return;
                        }

                        int cost = (int)Math.Ceiling(WeaponSkill.UpgradeCost[ws.Level] *
                                                     (1 - ws.Experience /
                                                      (double)WeaponSkill.RequiredExperience[ws.Level]));

                        if (!await user.SpendConquerPointsAsync(cost, true, true))
                        {
                            return;
                        }

                        ws.Level += 1;
                        ws.Experience = 0;

                        await user.WeaponSkill.SendAsync(ws);
                        await user.WeaponSkill.SaveAsync(ws);
                        await user.SendAsync(this);
                        break;
                    }

                case ActionType.AwardFirstCredit:
                    {
                        await user.ClaimFirstCreditGiftAsync();
                        break;
                    }

                case ActionType.InventorySash: // 256
                    {
                        await user.AddSashSpaceAsync(1);
                        break;
                    }

                case ActionType.EndRaceTrack: // 402
                    {
                        HorseRacing horseRacing = EventManager.GetEvent<HorseRacing>();
                        if (horseRacing == null)
                        {
                            return;
                        }

                        await user.SendAsync(this);
                        await horseRacing.CrossFinishLineAsync(user);
                        break;
                    }

                case ActionType.UserAttribInfo:
                    {
                        await client.SendAsync(new MsgPlayerAttribInfo(client.Character));
                        break;
                    }

                default:
                    {
                        logger.LogWarning("Action [{Action}] is not being handled.\n{Dump}\n{Json}", Action, PacketDump.Hex(Encode()), JsonConvert.SerializeObject(this));
                        break;
                    }
            }
        }
    }
}
