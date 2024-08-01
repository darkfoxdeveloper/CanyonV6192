using Canyon.Game.Services.Managers;
using Canyon.Game.States;
using Canyon.Game.States.NPCs;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgNpc : MsgBase<Client>
    {
        public int Timestamp { get; set; }
        public uint Identity { get; set; }
        public uint Data { get; set; }
        public NpcActionType RequestType { get; set; }
        public ushort Event { get; set; }

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
            Timestamp = reader.ReadInt32();
            Identity = reader.ReadUInt32();
            Data = reader.ReadUInt32();
            RequestType = (NpcActionType)reader.ReadUInt16();
            Event = reader.ReadUInt16();
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
            writer.Write((ushort)PacketType.MsgNpc);
            writer.Write(Timestamp);
            writer.Write(Identity);
            writer.Write(Data);
            writer.Write((ushort)RequestType);
            writer.Write(Event);
            return writer.ToArray();
        }

        public enum NpcActionType : ushort
        {
            Activate = 0,
            AddNpc = 1,
            LeaveMap = 2,
            DeleteNpc = 3,
            ChangePosition = 4,
            LayNpc = 5,

            CancelInteraction = 255
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (RequestType)
            {
                case NpcActionType.Activate:
                    {
                        user.ClearTaskId();
                        Role role = user.Map.QueryRole(Identity);
                        if (role == null)
                        {
                            role = RoleManager.GetRole(Identity);
                        }
                        if (role is BaseNpc npc
                            && ((role.MapIdentity == user.MapIdentity || role.Map.Identity == user.Map.BaseMapId)
                                && role.GetDistance(user) <= 18
                                || role.MapIdentity == 5000))
                        {
                            user.InteractingNpc = npc.Identity;
                            await npc.ActivateNpc(user);
                        }

                        break;
                    }

                case NpcActionType.CancelInteraction:
                    {
                        user.CancelInteraction();
                        break;
                    }
            }
        }
    }
}
