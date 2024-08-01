using Canyon.Game.States.User;
using Canyon.Network.Packets;
using static Canyon.Game.States.User.Character;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgOwnKongfuPKSetting : MsgBase<Client>
    {
        public JiangPkMode Mode { get; set; }

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Mode = (JiangPkMode)reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgOwnKongfuPkSetting);
            writer.Write((int)Mode);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            client.Character.JiangPkImmunity = Mode;
        }
    }
}
