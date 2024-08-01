using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;
using System.Collections.Concurrent;

namespace Canyon.Game.States
{
    public sealed class WeaponSkill
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<WeaponSkill>();

        private readonly Character user;
        private readonly ConcurrentDictionary<ushort, DbWeaponSkill> weaponSkills;

        public WeaponSkill(Character user)
        {
            this.user = user;
            weaponSkills = new ConcurrentDictionary<ushort, DbWeaponSkill>();
        }

        public async Task InitializeAsync()
        {
            foreach (var skill in await WeaponSkillRepository.GetAsync(user.Identity))
            {
                weaponSkills.TryAdd((ushort)skill.Type, skill);
            }
        }

        public DbWeaponSkill this[ushort type] => weaponSkills.TryGetValue(type, out var item) ? item : null;

        public async Task<bool> CreateAsync(ushort type, byte level = 1)
        {
            if (weaponSkills.ContainsKey(type))
            {
                return false;
            }

            DbWeaponSkill skill = new()
            {
                Type = type,
                Experience = 0,
                Level = level,
                OwnerIdentity = user.Identity,
                OldLevel = 0,
                Unlearn = 0
            };

            if (await SaveAsync(skill))
            {
                await user.SendAsync(new MsgWeaponSkill(skill));
                return weaponSkills.TryAdd(type, skill);
            }

            return false;
        }

        public Task<bool> SaveAsync(DbWeaponSkill skill)
        {
            return ServerDbContext.SaveAsync(skill);
        }

        public Task<bool> SaveAllAsync(ServerDbContext ctx)
        {
            if (weaponSkills.Count > 0)
            {
                ctx.WeaponSkills.UpdateRange(weaponSkills.Values.AsEnumerable());
                return Task.FromResult(true);
            }
            return Task.FromResult(true);
        }

        public async Task<bool> UnearnAllAsync()
        {
            foreach (var skill in weaponSkills.Values)
            {
                skill.Unlearn = 1;
                skill.OldLevel = skill.Level;
                skill.Level = 0;
                skill.Experience = 0;

                await user.SendAsync(new MsgAction
                {
                    Action = MsgAction.ActionType.ProficiencyRemove,
                    Identity = user.Identity,
                    Command = skill.Type,
                    Argument = skill.Type
                });
            }
            return true;
        }

        public async Task SendAsync(DbWeaponSkill skill)
        {
            await user.SendAsync(new MsgWeaponSkill(skill));
        }

        public async Task SendAsync()
        {
            foreach (var skill in weaponSkills.Values.Where(x => x.Unlearn == 0))
            {
                await user.SendAsync(new MsgWeaponSkill(skill));
            }
        }

        public static readonly uint[] RequiredExperience = new uint[21]
        {
            0,
            1200,
            68000,
            250000,
            640000,
            1600000,
            4000000,
            10000000,
            22000000,
            40000000,
            90000000,
            95000000,
            142500000,
            213750000,
            320625000,
            480937500,
            721406250,
            1082109375,
            1623164063,
            2100000000,
            0
        };

        public static readonly int[] UpgradeCost =
        {
            0, 27, 27, 27, 27, 27, 54, 81, 135, 162, 270, 324, 324, 324, 324, 375, 548, 799, 1154, 1420
        };
    }
}
