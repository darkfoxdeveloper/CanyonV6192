using Canyon.Game.Services.Managers;
using Canyon.Game.States.Items;
using Canyon.Game.States.Syndicates;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgTotemPole : MsgBase<Client>
    {
        public ActionMode Action { get; set; }
        public int Data1 { get; set; }
        public int Data2 { get; set; }
        public int Data3 { get; set; }
        public int Unknown20 { get; set; }
        public int Unknown24 { get; set; }


        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (ActionMode)reader.ReadUInt32();
            Data1 = reader.ReadInt32();
            Data2 = reader.ReadInt32();
            Data3 = reader.ReadInt32();
            Unknown20 = reader.ReadInt32();
            Unknown24 = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgTotemPole);
            writer.Write((uint)Action);
            writer.Write(Data1);
            writer.Write(Data2);
            writer.Write(Data3);
            writer.Write(Unknown20);
            writer.Write(Unknown24);
            return writer.ToArray();
        }

        public enum ActionMode
        {
            UnlockArsenal,
            InscribeItem,
            UnsubscribeItem,
            Enhance,
            Refresh
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = RoleManager.GetUser(client.Character.Identity);
            if (user == null)
            {
                client.Disconnect();
                return;
            }

            if (user.SyndicateIdentity == 0)
            {
                return;
            }

            switch (Action)
            {
                case ActionMode.UnlockArsenal:
                    {
                        if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader
                            && user.SyndicateRank != SyndicateMember.SyndicateRank.DeputyLeader
                            && user.SyndicateRank != SyndicateMember.SyndicateRank.HonoraryDeputyLeader
                            && user.SyndicateRank != SyndicateMember.SyndicateRank.LeaderSpouse)
                        {
                            return;
                        }

                        Syndicate.TotemPoleType type = (Syndicate.TotemPoleType)Data1;
                        if (type == Syndicate.TotemPoleType.None)
                        {
                            return;
                        }

                        if (user.Syndicate.LastOpenTotem != null)
                        {
                            int now = int.Parse($"{DateTime.Now:yyyyMMdd}");
                            int lastOpenTotem = int.Parse($"{user.Syndicate.LastOpenTotem.Value:yyyyMMdd}");
                            if (lastOpenTotem >= now)
                            {
                                return;
                            }
                        }

                        int price = user.Syndicate.UnlockTotemPolePrice();
                        if (user.Syndicate.Money < price)
                        {
                            return;
                        }

                        if (!await user.Syndicate.OpenTotemPoleAsync(type))
                        {
                            return;
                        }

                        user.Syndicate.Money -= price;
                        await user.Syndicate.SaveAsync();

                        await user.Syndicate.SendTotemPolesAsync(user);
                        await user.SendSyndicateAsync();
                        break;
                    }

                case ActionMode.InscribeItem:
                    {
                        Item item = user.UserPackage[(uint)Data2];
                        if (item == null)
                        {
                            return;
                        }

                        await user.Syndicate.InscribeItemAsync(user, item);
                        break;
                    }

                case ActionMode.UnsubscribeItem:
                    {
                        await user.Syndicate.UnsubscribeItemAsync((uint)Data2, user.Identity);
                        break;
                    }

                case ActionMode.Enhance:
                    {
                        if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        {
                            return;
                        }

                        if (await user.Syndicate.EnhanceTotemPoleAsync((Syndicate.TotemPoleType)Data1, (byte)Data3))
                        {
                            await user.Syndicate.SendTotemPolesAsync(user);
                            await user.SendSyndicateAsync();
                        }

                        break;
                    }

                case ActionMode.Refresh:
                    {
                        await user.Syndicate.SendTotemPolesAsync(user);
                        break;
                    }
            }
        }
    }
}
