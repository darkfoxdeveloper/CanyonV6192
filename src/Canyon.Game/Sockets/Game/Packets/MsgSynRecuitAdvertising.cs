using Canyon.Game.Services.Managers;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using Newtonsoft.Json;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgSynRecuitAdvertising : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgSynRecuitAdvertising>();

        public uint Identity { get; set; }
        public string Description { get; set; } = string.Empty;
        public long Silvers { get; set; }
        public bool AutoRecruit { get; set; }
        public ushort RequiredLevel { get; set; }
        public ushort RequiredMetempsychosis { get; set; }
        public ushort ForbidProfession { get; set; }
        public ushort ForbidGender { get; set; }
        public ushort Unknown { get; set; }

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Description = reader.ReadString(256);
            Silvers = reader.ReadInt64();
            AutoRecruit = reader.ReadUInt16() != 0;
            RequiredLevel = reader.ReadUInt16();
            RequiredMetempsychosis = reader.ReadUInt16();
            ForbidProfession = reader.ReadUInt16();
            ForbidGender = reader.ReadUInt16();
            Unknown = reader.ReadUInt16();
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgSynRecuitAdvertising);
            writer.Write(Identity); // 4
            writer.Write(Description, 256); // 8
            writer.Write(Silvers); // 264
            writer.Write((ushort) (AutoRecruit ? 1 : 0)); // 272 
            writer.Write(RequiredLevel); // 274
            writer.Write(RequiredMetempsychosis); // 276
            writer.Write(ForbidProfession); // 278
            writer.Write(ForbidGender); // 280
            writer.Write(Unknown); // 282
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            //logger.LogWarning("MsgSynRecuitAdvertising\n{Hex}\n{Json}", PacketDump.Hex(Encode()), JsonConvert.SerializeObject(this));
            await SyndicateManager.PublishAdvertisingAsync(client.Character, Silvers, Description, RequiredLevel, RequiredMetempsychosis, ForbidProfession, 0, ForbidGender, AutoRecruit);
        }
    }
}
