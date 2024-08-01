using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class MailRepository
    {
        public static async Task<List<DbMail>> GetAsync(uint idUser)
        {
            await using var serverDbContext = new ServerDbContext();
            return await serverDbContext.Mails.Where(x => x.ReceiverId == idUser).ToListAsync();
        }
    }
}
