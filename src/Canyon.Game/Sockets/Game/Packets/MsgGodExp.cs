using Canyon.Game.Services.Managers;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgGodExp : MsgBase<Client>
    {
        public int Timestamp { get; set; }
        public MsgGodExpAction Action { get; set; }
        public int GodTimeExp { get; set; }
        public int HuntExp { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Timestamp = reader.ReadInt32();
            Action = (MsgGodExpAction)reader.ReadInt32();
            GodTimeExp = reader.ReadInt32();
            HuntExp = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgGodExp);
            writer.Write(Timestamp);
            writer.Write((uint)Action);
            writer.Write(GodTimeExp);
            writer.Write(HuntExp);
            return writer.ToArray();
        }

        public enum MsgGodExpAction
        {
            Query,
            ClaimOnlineTraining,
            ClaimHuntTraining,
            MaximumTrainingExpTimeAlert,
            MaximimBlessExpTimeAlert
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Action)
            {
                case MsgGodExpAction.Query:
                    {
                        break;
                    }

                case MsgGodExpAction.ClaimHuntTraining:
                    {
                        if (user.Map != null && user.Map.IsNoExpMap())
                        {
                            return;
                        }

                        if (user.OnlineTrainingExp > 0)
                        {
                            await user.AwardExperienceAsync(user.CalculateExpBall((int)user.OnlineTrainingExp), true);
                            user.OnlineTrainingExp = 0;
                            await user.SaveAsync();

                            await client.SendAsync(new MsgGodExp
                            {
                                Action = MsgGodExpAction.Query,
                                GodTimeExp = (int)user.GodTimeExp,
                                HuntExp = (int)user.OnlineTrainingExp
                            });
                        }

                        break;
                    }

                case MsgGodExpAction.ClaimOnlineTraining:
                    {
                        if (user.Map != null && user.Map.IsNoExpMap())
                        {
                            return;
                        }

                        if (user.GodTimeExp > 0)
                        {
                            await user.AwardExperienceAsync(user.CalculateExpBall((int)user.GodTimeExp), true);
                            user.GodTimeExp = 0;
                            await user.SaveAsync();

                            await client.SendAsync(new MsgGodExp
                            {
                                Action = MsgGodExpAction.Query,
                                GodTimeExp = (int)user.GodTimeExp,
                                HuntExp = (int)user.OnlineTrainingExp
                            });
                        }

                        break;
                    }
            }

            if (user.Level < ExperienceManager.GetLevelLimit())
            {
                Action = MsgGodExpAction.Query;
                GodTimeExp = (int)user.GodTimeExp * 10_000;
                HuntExp = (int)user.OnlineTrainingExp * 10_000;
                await client.SendAsync(this);

                if (user.GodTimeExp >= 60000)
                {
                    await client.SendAsync(new MsgGodExp
                    {
                        Action = MsgGodExpAction.MaximimBlessExpTimeAlert
                    });
                }
                else if (user.OnlineTrainingExp >= 60000)
                {
                    await client.SendAsync(new MsgGodExp
                    {
                        Action = MsgGodExpAction.MaximumTrainingExpTimeAlert
                    });
                }
            }
        }
    }
}
