using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class TutorContributionRepository
    {
        public static async Task<List<DbTutorContribution>> GetStudentsAsync(uint idGuide)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.TutorContributions
                            .Where(x => x.TutorIdentity == idGuide)
                            .ToListAsync();
        }

        public static async Task<DbTutorContribution> GetGuideAsync(uint idStudent)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.TutorContributions
                            .FirstOrDefaultAsync(x => x.StudentIdentity == idStudent);
        }
    }
}
