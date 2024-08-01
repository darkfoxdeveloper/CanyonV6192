using Canyon.Game.Services.Managers;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgMentorPlayer : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgMentorPlayer>();

        public int Timestamp { get; set; }
        public int Action { get; set; }
        public uint SenderId { get; set; }
        public uint TargetId { get; set; }
        public int Unknown2 { get; set; }
        public int Unknown3 { get; set; }
        public int Unknown4 { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Timestamp = reader.ReadInt32();
            Action = reader.ReadInt32();    // 4
            SenderId = reader.ReadUInt32(); // 8 
            TargetId = reader.ReadUInt32(); // 12
            Unknown2 = reader.ReadInt32();  // 16
            Unknown3 = reader.ReadInt32();  // 20
            Unknown4 = reader.ReadInt32();  // 24
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgMentorPlayer);
            writer.Write(Timestamp = Environment.TickCount);
            writer.Write(Action);
            writer.Write(SenderId);
            writer.Write(TargetId);
            writer.Write(Unknown2);
            writer.Write(Unknown3);
            writer.Write(Unknown4);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character sender = RoleManager.GetUser(SenderId);
            Character target = RoleManager.GetUser(TargetId);

            if (sender == null || client.Character.Identity != sender.Identity || target == null)
            {
                return;
            }

            if (Action == 0) // Action 0 Enlight
            {
                await sender.EnlightenPlayerAsync(target);

                await target.BroadcastRoomMsgAsync(new MsgMentorPlayer
                {
                    SenderId = sender.Identity,
                    TargetId = target.Identity
                }, true);
            }
            else if (Action == 1) // Worship
            {
                // do nothing?
            }
            else
            {
                logger.LogWarning($"Unhandled Action {Action} for MsgMentorPlayer\n{PacketDump.Hex(Encode())}");
            }
        }
    }
}
