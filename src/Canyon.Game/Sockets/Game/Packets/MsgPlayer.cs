using Canyon.Game.States;
using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgPlayer : MsgBase<Client>
    {
        public MsgPlayer(Character user, Character target = null, ushort x = 0, ushort y = 0)
        {
            Identity = user.Identity;
            Mesh = user.Mesh;

            MapX = x == 0 ? user.X : x;
            MapY = y == 0 ? user.Y : y;

            Status = user.StatusFlag1;
            Status2 = user.StatusFlag2;
            Status3 = user.StatusFlag3;

            Hairstyle = user.Hairstyle;
            Direction = (byte)user.Direction;
            Pose = (byte)user.Action;
            Metempsychosis = Math.Min((byte)2, user.Metempsychosis);
            Level = user.Level;

            CurrentProfession = user.Profession;
            LastProfession = user.PreviousProfession;
            FirstProfession = user.FirstProfession;

            SyndicateIdentity = user.SyndicateIdentity;
            SyndicatePosition = (ushort)user.SyndicateRank;
            TotemBattlePower = user.TotemBattlePower;

            NobilityRank = (uint)user.NobilityRank;

            Helmet = user.Headgear?.Type ?? 0;
            HelmetColor = (ushort)(user.Headgear?.Color ?? Item.ItemColor.None);
            HelmetArtifact = user.Headgear?.Quench?.CurrentArtifact?.ItemType.Type ?? 0;
            RightHand = user.RightHand?.Type ?? 0;
            RightHandArtifact = user.RightHand?.Quench?.CurrentArtifact?.ItemType.Type ?? 0;
            RightAccessory = user.UserPackage[Item.ItemPosition.RightHandAccessory]?.Type ?? 0;
            LeftHand = user.LeftHand?.Type ?? 0;
            LeftHandColor = (ushort)(user.LeftHand?.Color ?? Item.ItemColor.None);
            LeftHandArtifact = user.LeftHand?.Quench?.CurrentArtifact?.ItemType.Type ?? 0;
            LeftAccessory = user.UserPackage[Item.ItemPosition.LeftHandAccessory]?.Type ?? 0;
            Armor = user.Armor?.Type ?? 0;
            ArmorColor = (ushort)(user.Armor?.Color ?? Item.ItemColor.None);
            ArmorArtifact = user.Armor?.Quench?.CurrentArtifact?.ItemType.Type ?? 0;
            Garment = user.Garment?.Type ?? 0;

            Wings = user.Wings?.Type ?? 0;
            WingsAddition = user.Wings?.Plus ?? 0;
            WingsCompositionProgress = user.Wings?.CompositionProgress ?? 0;

            Mount = user.Mount?.Type ?? 0;
            MountExperience = user.Mount?.CompositionProgress ?? 0;
            MountAddition = user.Mount?.Plus ?? 0;
            MountColor = user.Mount?.SocketProgress ?? 0;
            MountArmor = user.UserPackage[Item.ItemPosition.SteedArmor]?.Type ?? 0;

            FlowerRanking = user.FlowerCharm;
            QuizPoints = user.QuizPoints;
            UserTitle = user.TitleSelect;

            if (target != null)
            {
                EnlightenPoints = (ushort)(target.CanBeEnlightened(user) ? user.EnlightenPoints : 0);
                CanBeEnlightened = user.CanBeEnlightened(target);
            }

            if (!WindowSpawn)
            {
                IsArenaWitness = user.IsArenicWitness();
            }

            Away = user.IsAway;

            SharedBattlePower = (uint)(user.Guide?.SharedBattlePower ?? 0);
            NationalityFlag = (int)user.Nationality;

            FamilyIdentity = user.FamilyIdentity;
            FamilyRank = (uint)user.FamilyPosition;
            FamilyBattlePower = user.FamilyBattlePower;

            CurrentAstProf = (byte)user.AstProfType;
            AstProfRank = user.AstProfRanks;

            CurrentLayout = user.CurrentLayout;

            if (user.JiangHu.HasJiangHu)
            {
                KongFuActive = user.JiangHu.IsActive;
                TalentPoints = (byte)(user.JiangHu.Talent + 1);
            }

            BattlePower = user.BattlePower;

            Name = user.Name;
            FamilyName = user.FamilyName;
        }

        public MsgPlayer(Monster monster, ushort x = 0, ushort y = 0)
        {
            Identity = monster.Identity;
            Mesh = monster.Mesh;

            MapX = x == 0 ? monster.X : x;
            MapY = y == 0 ? monster.Y : y;

            Status = monster.StatusFlag1;
            Status2 = monster.StatusFlag2;
            Status3 = monster.StatusFlag3;

            Direction = (byte)monster.Direction;
            Pose = (byte)monster.Action;

            IsRacePotion = monster.Map.IsRaceTrack();
            SpeciesType = monster.SpeciesType;
            MonsterLevel = monster.Level;
            MonsterLife = monster.Life;

            Name = monster.Name;
            FamilyName = "";
        }

        public uint Identity { get; set; }
        public uint Mesh { get; set; }

        #region Union

        #region Struct

        public uint SyndicateIdentity { get; set; }
        public uint SyndicatePosition { get; set; }

        #endregion

        public uint OwnerIdentity { get; set; }

        #endregion

        #region Union

        public ulong Status { get; set; }

        #region Struct

        public ushort StatuaryLife { get; set; }
        public ushort StatuaryFrame { get; set; }

        #endregion

        #endregion

        public ulong Status2 { get; set; }
        public ulong Status3 { get; set; }

        public ushort CurrentLayout { get; set; }

        public uint Garment { get; set; }
        public uint Helmet { get; set; }
        public uint Armor { get; set; }
        public uint RightHand { get; set; }
        public uint LeftHand { get; set; }
        public uint Mount { get; set; }
        public uint MountArmor { get; set; }
        public uint RightAccessory { get; set; }
        public uint LeftAccessory { get; set; }

        public uint Wings { get; set; }
        public byte WingsAddition { get; set; }
        public uint WingsCompositionProgress { get; set; }

        public uint MonsterLife { get; set; }
        public ushort MonsterLevel { get; set; }

        public ushort MapX { get; set; }
        public ushort MapY { get; set; }
        public ushort Hairstyle { get; set; }
        public byte Direction { get; set; }
        public byte Pose { get; set; }
        public ushort Metempsychosis { get; set; }
        public ushort Level { get; set; }
        public bool WindowSpawn { get; set; }
        public bool Away { get; set; }
        public uint SharedBattlePower { get; set; }
        public uint FlowerRanking { get; set; }

        public uint NobilityRank { get; set; }

        public ushort Padding2 { get; set; }

        public ushort HelmetColor { get; set; }
        public ushort ArmorColor { get; set; }
        public ushort LeftHandColor { get; set; }
        public uint QuizPoints { get; set; }

        public byte MountAddition { get; set; }
        public uint MountExperience { get; set; }
        public uint MountColor { get; set; }
        public ushort EnlightenPoints { get; set; }
        public bool CanBeEnlightened { get; set; }

        public byte SpeciesType
        {
            get => (byte)FamilyIdentity;
            set => FamilyIdentity = value;
        }

        public uint FamilyIdentity { get; set; }
        public uint FamilyRank { get; set; }
        public int FamilyBattlePower { get; set; }

        public uint UserTitle { get; set; }
        public int TotemBattlePower { get; set; }
        public bool IsArenaWitness { get; set; }
        public bool IsRacePotion { get; set; }

        public uint HelmetArtifact { get; set; }
        public uint ArmorArtifact { get; set; }
        public uint LeftHandArtifact { get; set; }
        public uint RightHandArtifact { get; set; }

        public byte CurrentAstProf { get; set; }
        public ulong AstProfRank { get; set; }
        public ushort FirstProfession { get; set; }
        public ushort LastProfession { get; set; }
        public ushort CurrentProfession { get; set; }

        public int NationalityFlag { get; set; }
        public int BattlePower { get; set; }

        public byte TalentPoints { get; set; }
        public bool KongFuActive { get; set; }

        public byte SkillSoul { get; set; }

        public string Name { get; set; }
        public string FamilyName { get; set; }
        public string ServerName { get; set; }


        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgPlayer);
            writer.Write(0); // 4
            writer.Write(Mesh); // 8
            writer.Write(Identity); // 12

            if (OwnerIdentity > 0)
            {
                writer.Write(OwnerIdentity); // 16
                writer.Write(0); // 20
            }
            else
            {
                writer.Write(SyndicateIdentity); // 16
                writer.Write(SyndicatePosition); // 20
            }

            writer.Write((ushort)0); // 24

            if (StatuaryLife > 0)
            {
                writer.Write(StatuaryLife); // 26
                writer.Write(StatuaryFrame); // 28
                writer.Write(0u); // 30
            }
            else
            {
                writer.Write(Status); // 26
            }

            writer.Write(Status2); // 34
            writer.Write(Status3); // 42

            writer.Write(CurrentLayout); // 50

            writer.Write(Helmet); // 52
            writer.Write(Garment); // 56
            writer.Write(Armor); // 60
            writer.Write(LeftHand); // 64
            writer.Write(RightHand); // 68
            writer.Write(LeftAccessory); // 72
            writer.Write(RightAccessory); // 76
            writer.Write(Mount); // 80
            writer.Write(MountArmor); // 84
            writer.Write(Wings); // 88
            writer.Write(WingsAddition); // 92
            writer.Write(WingsCompositionProgress); // 93
            writer.Write(new byte[6]); // 97
            writer.Write(MonsterLife); // 103
            writer.Write((ushort)0); // 107
            writer.Write(MonsterLevel); // 109
            writer.Write(MapX); // 111
            writer.Write(MapY); // 113
            writer.Write(Hairstyle); // 115
            writer.Write(Direction); // 117
            writer.Write(Pose); // 118
            writer.BaseStream.Seek(6, SeekOrigin.Current); // 119
            writer.Write((byte)Metempsychosis); // 125
            writer.Write(Level); // 126
            writer.Write(WindowSpawn); // 128
            writer.Write(Away); // 129
            writer.Write(SharedBattlePower); // 130
            writer.Write(0); // 134
            writer.Write(0); // 138
            writer.Write(0); // 142
            writer.Write(FlowerRanking); // 146
            writer.Write(NobilityRank); // 150
            writer.Write(ArmorColor); // 154
            writer.Write(LeftHandColor); // 156
            writer.Write(HelmetColor); // 158
            writer.Write(QuizPoints); // 160
            writer.Write((ushort)MountAddition); // 164
            writer.Write(MountExperience); // 166
            writer.Write(MountColor); // 170
            writer.Write((byte)0); // 174 Merit?
            writer.Write(EnlightenPoints); // 175
            writer.Write(0); // 177 Inner Strength Score
            writer.Write((byte)0);              // 181
            writer.Write(CanBeEnlightened ? 1 : 0); // 182
            writer.Write(FamilyIdentity); // 186
            writer.Write(FamilyRank); // 190
            writer.Write(FamilyBattlePower); // 194
            writer.Write((ushort)UserTitle); // 198
            writer.Write(0); // 200 poker table seat?
            writer.Write((byte)0); // 204 poker table id?
            writer.Write((ushort)TotemBattlePower); // 205
            writer.Write((ushort)0); // 207
            writer.Write(IsArenaWitness); // 209
            writer.Write(IsRacePotion); // 210
            writer.Write((ushort)0); // 211
            writer.Write(HelmetArtifact); // 213
            writer.Write(ArmorArtifact); // 217
            writer.Write(LeftHandArtifact); // 221
            writer.Write(RightHandArtifact); // 225
            writer.Write(CurrentAstProf); // 229
            writer.Write(AstProfRank); // 230 AST PROF LEVEL INFO
            writer.Write(FirstProfession); // 238
            writer.Write(LastProfession); // 240
            writer.Write(CurrentProfession); // 242
            writer.Write(NationalityFlag); // 244
            writer.Write((ushort)0); // 248 TeamID
            writer.Write(BattlePower); // 250
            writer.Write(TalentPoints); // 254
            writer.Write(KongFuActive); // 255
            writer.Write((byte)0); // 256
            writer.Write(0); // 257 owner ID?
            writer.Write((byte)0); // 261
            writer.Write(0); // 262 Skill Soul2?
            //writer.Write((ushort)0); // 255 associate id 9 for guard, 2 for clone 255
            writer.Write(0); // 266
            writer.Write(new byte[14]); // 270
            writer.Write(new List<string> // 284
            {
                Name,
                "",
                FamilyName
            });
            return writer.ToArray();
        }
    }
}
