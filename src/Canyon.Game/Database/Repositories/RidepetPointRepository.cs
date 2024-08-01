using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Canyon.Game.Database.Repositories
{
    public static class RidepetPointRepository
    {
        public static async Task<List<DbPetPoint>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.PetPoints.ToListAsync();
        }
    }
}
