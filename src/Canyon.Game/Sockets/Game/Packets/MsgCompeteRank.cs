using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgCompeteRank : MsgBase<Client>
    {
        public enum Action
        {
            BestTime,
            EndTime,
            AddRecord,
            CloseRecords
        }

        public Action Mode { get; set; }
        public int Rank { get; set; }
        public string Name { get; set; }
        public int Param { get; set; }
        public int Data { get; set; }
        public int Time { get; set; }
        public int Prize { get; set; }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgCompeteRank);
            writer.Write((int)Mode);
            writer.Write(Rank);
            if (Mode != Action.AddRecord)
            {
                writer.Write(Param);
                writer.Write(Data);
                writer.Write(0);
                writer.Write(0);
            }
            else
            {
                writer.Write(Name, 16);
            }
            writer.Write(Time);
            writer.Write(Prize);
            return writer.ToArray();
        }
    }
}
