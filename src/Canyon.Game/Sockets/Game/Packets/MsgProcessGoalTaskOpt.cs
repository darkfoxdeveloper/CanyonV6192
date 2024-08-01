using Canyon.Game.Services.Managers;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgProcessGoalTaskOpt : MsgBase<Client>
    {
        public enum Action : byte
        {
            Display,
            ClaimStageReward,
            ClaimReward
        }

        public Action Mode { get; set; }
        public ushort Data { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Mode = (Action)reader.ReadByte();
            Data = reader.ReadUInt16();
            ushort test = reader.ReadUInt16();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgProcessGoalTaskOpt);
            writer.Write((byte)Mode);
            writer.Write(Data);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            switch (Mode)
            {
                case Action.Display:
                    {
                        await ProcessGoalManager.SubmitGoalsAsync(client.Character, Data);
                        break;
                    }

                case Action.ClaimStageReward:
                    {
                        if (await ProcessGoalManager.ClaimStageRewardAsync(client.Character, Data))
                        {
                            await client.SendAsync(this);
                        }
                        break;
                    }
            }
        }
    }
}
