using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.States.User;

namespace Canyon.Game.States
{
    public class PkStatisticInfo
    {
        private readonly DbPkStatistic pkStatistic;

        public PkStatisticInfo(DbPkStatistic pkStatistic)
        {
            this.pkStatistic = pkStatistic;
        }

        public PkStatisticInfo(Character user, Character target)
        {
            pkStatistic = new DbPkStatistic
            {
                KillerId = user.Identity,
                TargetId = target.Identity
            };
            TargetName = target.Name;
        }

        public async Task<bool> InitializeAsync()
        {
            DbCharacter user = await CharacterRepository.FindByIdentityAsync(TargetId);
            if (user == null)
            {
                return false;
            }

            TargetName = user.Name;
            return true;
        }

        public uint TargetId => pkStatistic.TargetId;
        public string TargetName { get; private set; }

        public uint PkTime 
        {
            get => pkStatistic.PkTime;
            set => pkStatistic.PkTime = value;
        }

        public uint MapId 
        {
            get => pkStatistic.MapId;
            set => pkStatistic.MapId = value;
        }

        public ushort TargetBattleEffect 
        {
            get => pkStatistic.TargetBattleEffect;
            set => pkStatistic.TargetBattleEffect = value;
        }

        public uint KillTimes 
        {
            get => pkStatistic.KillTimes;
            set => pkStatistic.KillTimes = value;
        }

        public Task SaveAsync()
        {
            return ServerDbContext.SaveAsync(pkStatistic);
        }
    }
}
