using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class QuizRepository
    {
        public static async Task<List<DbQuiz>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Quiz.ToListAsync();
        }
    }
}
