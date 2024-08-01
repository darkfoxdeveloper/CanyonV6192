using Canyon.Game.Services.Managers;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using Newtonsoft.Json;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgSynRecruitAdvertisingOpt : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgSynRecruitAdvertisingOpt>();

        public AdvertisingOpt Action { get; set; }
        public uint Identity { get; set; }
        public uint Data { get; set; }

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (AdvertisingOpt)reader.ReadInt32();
            Identity = reader.ReadUInt32();
            Data = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort) PacketType.MsgSynRecruitAdvertisingOpt);
            writer.Write((int) Action);
            writer.Write(Identity);
            writer.Write(Data);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            switch (Action)
            {
                case AdvertisingOpt.Join:
                    {
                        await SyndicateManager.JoinByAdvertisingAsync(client.Character, (ushort)Identity);
                        break;
                    }

                case AdvertisingOpt.Recruit:
                    {
                        if (client.Character.Syndicate == null)
                        {
                            return;
                        }

                        if (SyndicateManager.HasSyndicateAdvertise(client.Character.SyndicateIdentity))
                        {
                            await SyndicateManager.SubmitEditAdvertiseScreenAsync(client.Character);
                        }
                        else
                        {
                            await client.SendAsync(new MsgSynRecuitAdvertising()
                            {
                                Identity = client.Character.SyndicateIdentity
                            });
                        }
                        break;
                    }

                default:
                    {
                        logger.LogWarning("Action [{Action}] is not being handled.\n{Dump}\n{Json}", Action, PacketDump.Hex(Encode()), JsonConvert.SerializeObject(this));
                        break;
                    }
            }
        }

        public enum AdvertisingOpt
        {
            Join = 1,
            Recruit
        }
    }
}
