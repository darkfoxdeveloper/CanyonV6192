using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using static Canyon.Game.Sockets.Game.Packets.MsgItemStatus;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgSolidify : MsgBase<Client>
    {
        public enum SolidifyMode
        {
            Refinery,
            Artifact
        }

        public SolidifyMode Action { get; set; }
        public uint MainIdentity { get; set; }
        public int Count { get; set; }
        public List<uint> Consumables { get; } = new List<uint>();


        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (SolidifyMode)reader.ReadInt32();
            MainIdentity = reader.ReadUInt32();
            Count = reader.ReadInt32();
            for (int i = 0; i < Count; i++)
            {
                Consumables.Add(reader.ReadUInt32());
            }
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            Item item = user.UserPackage.FindByIdentity(MainIdentity);

            int pointsSum = 0;
            List<Item> usable = new();
            foreach (var idConsumable in Consumables)
            {
                Item consumable = user.UserPackage[idConsumable];
                if (consumable.Type == Item.PERMANENT_STONE)
                {
                    pointsSum += 10;
                }
                else if (consumable.Type == Item.BIGPERMANENT_STONE)
                {
                    pointsSum += 100;
                }
                else
                {
                    continue;
                }

                usable.Add(consumable);
            }

            switch (Action)
            {
                case SolidifyMode.Refinery:
                    {
                        if (item.Quench.CurrentRefinery == null || item.Quench.CurrentRefinery.IsPermanent)
                        {
                            return;
                        }

                        ItemQuench.QuenchData original = item.Quench.GetOriginalRefinery();
                        int needPoints = ItemQuench.RefinerySolidifyPoints[item.Quench.CurrentRefinery.ItemStatus.Level];
                        if (original != null)
                        {
                            needPoints = Math.Max(10, needPoints - ItemQuench.RefinerySolidifyPoints[original.ItemStatus.Level]);
                        }

                        if (needPoints > pointsSum)
                        {
                            return;
                        }

                        foreach (var consume in usable)
                        {
                            await user.UserPackage.SpendItemAsync(consume);
                        }

                        await item.Quench.StabilizeRefineryAsync();
                        await item.Quench.SendToAsync(user);
                        await user.SendAsync(new MsgItemStatus(item.Quench.CurrentRefinery, ItemStatusType.RefineryStabilizationEffect));
                        break;
                    }

                case SolidifyMode.Artifact:
                    {
                        if (item.Quench.CurrentArtifact == null || item.Quench.CurrentArtifact.IsPermanent)
                        {
                            return;
                        }

                        ItemQuench.QuenchData original = item.Quench.GetOriginalArtifact();
                        int needPoints = ItemQuench.ArtifactSolidifyPoints[item.Quench.CurrentArtifact.ItemStatus.Level];
                        if (original != null)
                        {
                            needPoints = Math.Max(10, needPoints - ItemQuench.ArtifactSolidifyPoints[original.ItemStatus.Level]);
                        }

                        if (needPoints > pointsSum)
                        {
                            return;
                        }

                        foreach (var consume in usable)
                        {
                            await user.UserPackage.SpendItemAsync(consume);
                        }

                        await item.Quench.StabilizeArtifactAsync();
                        await item.Quench.SendToAsync(user);
                        await user.SendAsync(new MsgItemStatus(item.Quench.CurrentArtifact, ItemStatusType.ArtifactStabilizationEffect));
                        break;
                    }
            }
        }
    }
}
