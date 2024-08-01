using Canyon.Game.Services.Managers;
using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgLottery : MsgBase<Client>
    {
        public LotteryRequest Action { get; set; }
        public byte Unknown6 { get; set; }
        public byte SocketOne { get; set; }
        public byte SocketTwo { get; set; }
        public byte Addition { get; set; }
        public byte Color { get; set; }
        public byte UsedChances { get; set; }
        public uint ItemType { get; set; } // or chances
        public int Unknown16 { get; set; }
        public int Unknown20 { get; set; }

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (LotteryRequest)reader.ReadUInt16();
            Unknown6 = reader.ReadByte();
            SocketOne = reader.ReadByte();
            SocketTwo = reader.ReadByte();
            Addition = reader.ReadByte();
            Color = reader.ReadByte();
            UsedChances = reader.ReadByte();
            ItemType = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgLottery);
            writer.Write((ushort)Action);
            writer.Write(Unknown6);
            writer.Write(SocketOne);
            writer.Write(SocketTwo);
            writer.Write(Addition);
            writer.Write(Color);
            writer.Write(UsedChances);
            writer.Write(ItemType);
            return writer.ToArray();
        }

        public enum LotteryRequest : ushort
        {
            Accept = 0,
            AddJade = 1,
            Continue = 2,
            Show = 259
        }

        public override async Task ProcessAsync(Client client)
        {
            /**
             * Statistics for Lottery
             * idEvent: 22
             *
             * idType: 0
             *  How many times the character has entered the lottery.
             * idType: 1
             *  How many times the user has tried before Accept.
             * idType: 2
             *  If the user has accepted the last prize.
             *
             * Rules:
             * If idType(2) == 0 and the user is trying to open another box then the user will receive the latest prize. The same if disconnecting.
             *
             */

            Character user = client.Character;

            switch (Action)
            {
                case LotteryRequest.Accept:
                    {
                        await client.Character.AcceptLotteryPrizeAsync();
                        break;
                    }

                case LotteryRequest.AddJade:
                    {
                        await client.Character.LotteryTryAgainAsync();
                        break;
                    }

                case LotteryRequest.Continue:
                    {
                        if (user.Statistic.GetValue(22) >= LotteryManager.GetMaxAttempts(user))
                        {
                            return;
                        }

                        if (!await user.UserPackage.SpendItemAsync(Item.SMALL_LOTTERY_TICKET, 3))
                        {
                            await user.SendAsync(StrEmbedNoRequiredItem);
                            return;
                        }

                        await LotteryManager.QueryPrizeAsync(user, user.LotteryLastColor, false);
                        await user.UpdateTaskActivityAsync(ActivityManager.ActivityType.Lottery);
                        break;
                    }
            }
        }
    }
}
