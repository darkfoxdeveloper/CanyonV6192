using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgUserAttrib : MsgBase<Client>
    {
        private readonly List<UserAttribute> Attributes = new();

        public MsgUserAttrib()
        {
        }

        public MsgUserAttrib(uint idRole, ClientUpdateType type, ulong value)
        {
            Identity = idRole;
            Amount++;
            Attributes.Add(new UserAttribute((uint)type, value));
        }

        public MsgUserAttrib(uint idRole, ClientUpdateType type, uint value0, uint value1)
        {
            Identity = idRole;
            Amount++;
            Attributes.Add(new UserAttribute((uint)type, value0, value1));
        }

        public MsgUserAttrib(uint idRole, ClientUpdateType type, ulong value, ulong value2)
        {
            Identity = idRole;
            Amount++;
            Attributes.Add(new UserAttribute((uint)type, value, value2));
        }

        public MsgUserAttrib(uint idRole, ClientUpdateType type, ulong value, ulong value2, ulong value3)
        {
            Identity = idRole;
            Amount++;
            Attributes.Add(new UserAttribute((uint)type, value, value2, value3));
        }

        public MsgUserAttrib(uint idRole, ClientUpdateType type, uint value0, uint value1, uint value3, uint value4)
        {
            Identity = idRole;
            Amount++;
            Attributes.Add(new UserAttribute((uint)type, value0, value1, value3, value4));
        }

        public MsgUserAttrib(uint idRole, ClientUpdateType type, uint value0, uint value1, uint value3, uint value4, uint value5, uint value6)
        {
            Identity = idRole;
            Amount++;
            Attributes.Add(new UserAttribute((uint)type, value0, value1, value3, value4, value5, value6));
        }

        public int Timestamp { get; set; }
        public uint Identity { get; set; }
        public int Amount { get; set; }

        public List<UserAttribute> GetAttributes()
        {
            return Attributes.ToList();
        }

        public void Append(ClientUpdateType type, ulong data)
        {
            Amount++;
            Attributes.Add(new UserAttribute((uint)type, data));
        }

        public void Append(ClientUpdateType type, ulong data, ulong data2)
        {
            Amount++;
            Attributes.Add(new UserAttribute((uint)type, data, data2));
        }

        public void Append(ClientUpdateType type, uint data, uint data2)
        {
            Amount++;
            Attributes.Add(new UserAttribute((uint)type, data, data2));
        }

        public void Append(ClientUpdateType type, ulong data, ulong data2, ulong data3)
        {
            Amount++;
            Attributes.Add(new UserAttribute((uint)type, data, data2, data3));
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgUserAttrib);
            writer.Write(Environment.TickCount);
            writer.Write(Identity);
            Amount = Attributes.Count;
            writer.Write(Amount);
            for (var i = 0; i < Amount; i++)
            {
                writer.Write(Attributes[i].Type);
                writer.Write(Attributes[i].Data);
                writer.Write(Attributes[i].Data2);
                writer.Write(Attributes[i].Data3);
            }

            return writer.ToArray();
        }

        public readonly struct UserAttribute
        {
            public UserAttribute(uint type, ulong data)
            {
                Type = type;
                Data = data;
                Data2 = 0;
                Data3 = 0;
            }

            public UserAttribute(uint type, ulong data, ulong data2)
            {
                Type = type;
                Data = data;
                Data2 = data2;
                Data3 = 0;
            }

            public UserAttribute(uint type, ulong data, ulong data2, ulong data3)
            {
                Type = type;
                Data = data;
                Data2 = data2;
                Data3 = data3;
            }

            public UserAttribute(uint type, uint left, uint right)
            {
                Type = type;
                Data = ((ulong)left << 32) | right;
                Data2 = 0;
                Data3 = 0;
            }

            public UserAttribute(uint type, uint left, uint right, uint left2, uint right2)
            {
                Type = type;
                Data = ((ulong)left << 32) | right;
                Data2 = ((ulong)left2 << 32) | right2;
                Data3 = 0;
            }

            public UserAttribute(uint type, uint left, uint right, uint left2, uint right2, uint left3, uint right3)
            {
                Type = type;
                Data = ((ulong)left << 32) | right;
                Data2 = ((ulong)left2 << 32) | right2;
                Data3 = ((ulong)left3 << 32) | right3;
            }

            public readonly uint Type;
            public readonly ulong Data;
            public readonly ulong Data2;
            public readonly ulong Data3;
        }
    }

    public enum ClientUpdateType
    {
        Hitpoints = 0,
        MaxHitpoints = 1,
        Mana = 2,
        MaxMana = 3,
        Money = 4,
        Experience = 5,
        PkPoints = 6,
        Class = 7,
        Stamina = 8,
        Atributes = 10,
        Mesh,
        Level,
        Spirit,
        Vitality,
        Strength,
        Agility,
        HeavensBlessing,
        MultipleExpTimer,
        CursedTimer = 20,
        Reborn = 22,
        StatusFlag = 25,
        HairStyle = 26,
        XpCircle = 27,
        LuckyTimeTimer = 28,
        ConquerPoints = 29,
        OnlineTraining = 31,
        ExtraBattlePower = 36,
        Merchant = 38,
        VipLevel = 39,
        QuizPoints = 40,
        EnlightenPoints = 41,
        FamilySharedBattlePower = 42,
        TotemPoleBattlePower = 44,
        BoundConquerPoints = 45,
        RidePetPoint = 47,
        AzureShield = 49,
        PreviousProfession = 50,
        FirstProfession = 51,
        SoulShackleTimer = 54,
        Fatigue = 55,
        PhysicalCritPct = 59,
        MagicCritPct = 60,
        Immunity = 61,
        Break = 62,
        Counteraction = 63,
        HpMod = 64,
        PhDmgMod = 65,
        MAttkMod = 67,
        PhDmgTakenMod = 67,
        MaDmgTakenMod = 68,
        FinalPhDmgMod = 69,
        FinalMaDmgMod = 70,
        PrivilegeFlag = 71,
        ExpProtection = 73,
        DragonSwing = 75,
        DragonFury = 74,
        InnerPowerPotency = 77,
        AppendIcon = 78,
        CurrentSashSlots = 79,
        MaximumSashSlots = 80,
        ExploitsRank = 82,
        UnionRank = 83,
        Anger = 90,
        XpList = 101,

        Vigor = 10000
    }

    public enum AttrUpdateType : uint
    {
        Accelerated = 52,
        Decelerated = 53,
        Flustered = 54,
        Sprint = 55,
        DivineShield = 57,
        Stun = 58,
        Freeze = 59,
        Dizzy = 60,
        AzureShield = 93,
        SoulShackle = 111
    }
}
