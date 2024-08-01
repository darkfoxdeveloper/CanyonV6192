using Canyon.Database.Entities;

namespace Canyon.Game.Database.Repositories
{
    public static class GoodsRepository
    {
        public static async Task<List<DbGoods>> GetAsync(uint idNpc)
        {
            await using ServerDbContext context = new();
            return context.Goods.Where(x => x.OwnerIdentity == idNpc).ToList();
        }
    }
}
