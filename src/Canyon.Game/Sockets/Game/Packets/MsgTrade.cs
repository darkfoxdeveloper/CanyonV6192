using Canyon.Game.States;
using Canyon.Game.States.User;
using Canyon.Game.States.World;
using Canyon.Network.Packets;
using static Canyon.Game.States.User.Character;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgTrade : MsgBase<Client>
    {
        public uint Data { get; set; }
        public int Param { get; set; }
        public TradeAction Action { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Data = reader.ReadUInt32();
            Param = reader.ReadInt32();
            Action = (TradeAction)reader.ReadUInt16();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgTrade);
            writer.Write(Data);
            writer.Write(Param);
            writer.Write((uint)Action);
            return writer.ToArray();
        }

        public enum TradeAction
        {
            Apply = 1,
            Quit,
            Open,
            Success,
            Fail,
            AddItem,
            AddMoney,
            ShowMoney,
            Accept = 10,
            AddItemFail,
            ShowConquerPoints,
            AddConquerPoints,
            SuspiciousTradeNotify = 15,
            SuspiciousTradeAgree,
            Timeout = 17
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            Character target = null;

            switch (Action)
            {
                case TradeAction.Apply:
                    {
                        if (Data == 0)
                        {
                            return;
                        }

                        target = user.QueryRole(Data) as Character;
                        if (target == null || target.GetDistance(user) > Screen.VIEW_SIZE)
                        {
                            await user.SendAsync(Language.StrTargetNotInRange);
                            return;
                        }

                        if (user.Trade != null)
                        {
                            await user.SendAsync(Language.StrTradeYouAlreadyTrade);
                            return;
                        }

                        if (target.Trade != null)
                        {
                            await user.SendAsync(Language.StrTradeTargetAlreadyTrade);
                            return;
                        }

                        if (target.QueryRequest(RequestType.Trade) == user.Identity)
                        {
                            target.PopRequest(RequestType.Trade);
                            user.Trade = target.Trade = new Trade(target, user);
                            await user.SendAsync(new MsgTrade { Action = TradeAction.Open, Data = target.Identity });
                            await target.SendAsync(new MsgTrade { Action = TradeAction.Open, Data = user.Identity });
                            return;
                        }

                        Data = user.Identity;
                        await target.SendAsync(this);
                        await target.SendRelationAsync(user);
                        user.SetRequest(RequestType.Trade, target.Identity);
                        await user.SendAsync(Language.StrTradeRequestSent);
                        break;
                    }

                case TradeAction.Quit:
                    {
                        if (user.Trade != null)
                        {
                            await user.Trade.SendCloseAsync();
                        }

                        break;
                    }

                case TradeAction.AddItem:
                    {
                        if (user.Trade != null)
                        {
                            await user.Trade.AddItemAsync(Data, user);
                        }

                        break;
                    }

                case TradeAction.AddMoney:
                    {
                        if (user.Trade != null)
                        {
                            await user.Trade.AddMoneyAsync(Data, user);
                        }

                        break;
                    }

                case TradeAction.Accept:
                    {
                        if (user.Trade != null)
                        {
                            await user.Trade.AcceptAsync(user.Identity);
                        }

                        break;
                    }

                case TradeAction.AddConquerPoints:
                    {
                        if (user.Trade != null)
                        {
                            await user.Trade.AddEmoneyAsync(Data, user);
                        }

                        break;
                    }
            }
        }
    }
}
