using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgAuraGroup : MsgBase<Client>
    {
        public AuraGroupMode Mode { get; set; }
        public uint Identity { get; set; }
        public uint LeaderIdentity { get; set; }
        public uint Count { get; set; }
        public uint Unknown { get; set; }

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            LeaderIdentity = reader.ReadUInt32();
            Count = reader.ReadUInt32();
            Unknown = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgAuraGroup);
            writer.Write((int)Mode);
            writer.Write(Identity);
            writer.Write(LeaderIdentity);
            writer.Write(Count);
            writer.Write(Unknown);
            return writer.ToArray();
        }

        public enum AuraGroupMode : uint
        {
            Leader = 1,
            Teammate = 2
        }

        public override async Task ProcessAsync(Client client)
        {

        }
    }
}
