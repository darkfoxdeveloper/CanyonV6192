using Canyon.Database;
using Canyon.Login.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Login.Database
{
    public class ServerDbContext : AbstractDbContext
    {
        public virtual DbSet<GameAccount> GameAccounts { get; set; }
        public virtual DbSet<GameAccountAuthority> GameAccountsAuthority { get; set; }
        public virtual DbSet<GameAccountVip> GameAccountVips { get; set; }
        public virtual DbSet<RealmData> RealmDatas { get; set; }
    }
}