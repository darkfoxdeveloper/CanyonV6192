using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class PkInfoRepository
    {
        public enum PkInfoType
        {
            None,
            ElitePk,
            TeamPk,
            SkillTeamPk
        }

        public static async Task<DbPkInfo> GetPkInfoAsync(PkInfoType type, int subType)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.PkInfos.FirstOrDefaultAsync(x => x.Type == (int)type && x.Subtype == subType);
        }
    }
}
