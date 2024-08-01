﻿using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class ConfigRepository
    {
        public static async Task<IList<DbConfig>> GetAsync(int type)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Configs.Where(x => x.Type == type).ToListAsync();
        }

        public static async Task<List<DbConfig>> GetAsync(Func<DbConfig, bool> predicate)
        {
            await using var ctx = new ServerDbContext();
            return ctx.Configs.Where(predicate).ToList();
        }
    }
}
