using Canyon.Database.Entities;
using Canyon.Game.Database;

namespace Canyon.Game.States.Auction
{
    public class AuctionBid
    {
        private readonly DbAuctionAskBuy askBuy;
        public AuctionBid(DbAuctionAskBuy askBuy, string bidderName)
        {
            this.askBuy = askBuy;
            BidderName = bidderName;
        }

        public uint BidderId => askBuy.Buyer;
        public string BidderName { get; init; }
        public uint Price
        {
            get => askBuy.Price;
            set => askBuy.Price = value;
        }

        public Task SaveAsync()
        {
            return ServerDbContext.SaveAsync(askBuy);
        }

        public Task DeleteAsync()
        {
            return ServerDbContext.DeleteAsync(askBuy);
        }
    }
}
