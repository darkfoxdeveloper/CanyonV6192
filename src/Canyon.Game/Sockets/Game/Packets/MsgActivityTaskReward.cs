using Canyon.Game.Services.Managers;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgActivityTaskReward : MsgBase<Client>
    {
        public byte RewardGrade { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            RewardGrade = reader.ReadByte();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgActivityTaskReward);
            writer.Write(RewardGrade);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            if (!await ActivityManager.ClaimRewardAsync(client.Character, RewardGrade))
            {
                RewardGrade = 0;
            }
            await client.SendAsync(this);
        }
    }
}
