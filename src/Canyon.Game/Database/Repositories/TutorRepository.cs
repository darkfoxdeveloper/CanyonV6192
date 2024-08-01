using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class TutorRepository
    {
        public static async Task<DbTutor> GetAsync(uint idStudent)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Tutor
                            .FirstOrDefaultAsync(x => x.StudentId == idStudent);
        }

        public static async Task<List<DbTutor>> GetStudentsAsync(uint idTutor)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Tutor
                            .Where(x => x.GuideId == idTutor)
                            .ToListAsync();
        }
    }
}
