using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class LevelExperienceRepository
    {
        public static async Task<List<DbLevelExperience>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return await db.LevelExperiences.ToListAsync();
        }
    }
}
