using Canyon.Login.Database;
using Canyon.Login.Database.Entities;

namespace Canyon.Login.Repositories
{
    public static class VipRepository
    {
        public static GameAccountVip GetAccountVip(uint idAccount)
        {
            using var ctx = new ServerDbContext();
            return ctx.GameAccountVips.Where(x => x.GameAccountId == idAccount && x.StartDate < DateTime.Now && x.EndDate > DateTime.Now)
                .FirstOrDefault();
        }
    }
}
