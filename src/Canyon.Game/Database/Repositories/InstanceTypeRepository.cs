using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class InstanceTypeRepository
    {
        public static DbInstanceType Get(uint instanceType)
        {
            using var ctx = new ServerDbContext();
            return ctx.InstanceTypes
                //.Include(x => x.EnterCondition)
                .FirstOrDefault(x => x.Id == instanceType);
        }
    }
}
