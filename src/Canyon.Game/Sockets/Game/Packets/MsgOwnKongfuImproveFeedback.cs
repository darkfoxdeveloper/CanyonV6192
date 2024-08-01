using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgOwnKongfuImproveFeedback : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgOwnKongfuImproveFeedback>();

        public int FreeCourse { get; set; }
        public byte High { get; set; }
        public KongFuImproveFeedbackMode Mode { get; set; }
        public byte Star { get; set; }
        public byte Stage { get; set; }
        public ushort Attribute { get; set; }
        public byte FreeCourseUsedToday { get; set; }
        public int PaidRounds { get; set; }

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            FreeCourse = reader.ReadInt32(); // 4
            High = reader.ReadByte(); // 8
            Mode = (KongFuImproveFeedbackMode)reader.ReadByte(); // 9
            Star = reader.ReadByte(); // 10
            Stage = reader.ReadByte(); // 11
            Attribute = reader.ReadUInt16(); // 12
            FreeCourseUsedToday = reader.ReadByte(); // 14
            PaidRounds = reader.ReadInt32(); // 15
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgOwnKongfuImproveFeedback);
            writer.Write(FreeCourse);
            writer.Write(High);
            writer.Write((byte)Mode);
            writer.Write(Star);
            writer.Write(Stage);
            writer.Write(Attribute);
            writer.Write(FreeCourseUsedToday);
            writer.Write(PaidRounds);
            return writer.ToArray();
        }

        public enum KongFuImproveFeedbackMode
        {
            FreeCourse,
            PaidCourse,
            FavouredTraining
        }

        public override async Task ProcessAsync(Client client)
        {
            if (Stage == 0 || Stage > 9)
            {
                return;
            }

            if (Star == 0 || Star > 9)
            {
                return;
            }

            Character user = client.Character;

            switch (Mode)
            {
                case KongFuImproveFeedbackMode.FreeCourse:
                case KongFuImproveFeedbackMode.PaidCourse:
                case KongFuImproveFeedbackMode.FavouredTraining:
                    {
                        if (!user.IsUnlocked())
                        {
                            await user.SendSecondaryPasswordInterfaceAsync();
                            return;
                        }

                        await user.JiangHu.StudyAsync(Stage, Star, High, Mode);
                        break;
                    }
            }
        }
    }
}
