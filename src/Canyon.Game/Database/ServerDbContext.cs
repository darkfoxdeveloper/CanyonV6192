using Canyon.Database;
using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace Canyon.Game.Database
{
    /// <summary>
    ///     Server database client context implemented using Entity Framework Core, an open
    ///     source object-relational mapping framework for ADO.NET. Substitutes in MySQL
    ///     support through a third-party framework provided by Pomelo Foundation.
    /// </summary>
    public class ServerDbContext : AbstractDbContext
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<ServerDbContext>();

        public virtual DbSet<DbCharacter> Characters { get; set; }
        public virtual DbSet<DbMap> Maps { get; set; }
        public virtual DbSet<DbDynamap> DynaMaps { get; set; }
        public virtual DbSet<DbPassway> Passways { get; set; }
        public virtual DbSet<DbPortal> Portals { get; set; }
        public virtual DbSet<DbRegion> Regions { get; set; }
        public virtual DbSet<DbSuperman> Superman { get; set; }
        public virtual DbSet<DbPointAllot> PointAllots { get; set; }
        public virtual DbSet<DbLevelExperience> LevelExperiences { get; set; }
        public virtual DbSet<DbRebirth> Rebirths { get; set; }
        public virtual DbSet<DbMagictypeOp> MagictypeOperations { get; set; }
        public virtual DbSet<DbStatus> Status { get; set; }
        public virtual DbSet<DbMessageLog> MessageLogs { get; set; }
        public virtual DbSet<DbGameLoginRecord> GameLoginRecords { get; set; }
        public virtual DbSet<DbItem> Items { get; set; }
        public virtual DbSet<DbItemtype> Itemtypes { get; set; }
        public virtual DbSet<DbItemAddition> ItemAdditions { get; set; }
        public virtual DbSet<DbItemStatus> ItemStatus { get; set; }
        public virtual DbSet<DbItemDrop> ItemDrops { get; set; }
        public virtual DbSet<DbItemPickUp> ItemPickUps { get; set; }
        public virtual DbSet<DbWeaponSkill> WeaponSkills { get; set; }
        public virtual DbSet<DbGoods> Goods { get; set; }
        public virtual DbSet<DbAction> Actions { get; set; }
        public virtual DbSet<DbTask> Tasks { get; set; }
        public virtual DbSet<DbNpc> Npcs { get; set; }
        public virtual DbSet<DbDynanpc> DynamicNpcs { get; set; }
        public virtual DbSet<DbStatistic> Statistics { get; set; }
        public virtual DbSet<DbStatisticDaily> DailyStatistics { get; set; }
        public virtual DbSet<DbTaskDetail> TaskDetails { get; set; }
        public virtual DbSet<DbLottery> Lottery { get; set; }
        public virtual DbSet<DbConfig> Configs { get; set; }
        public virtual DbSet<DbPeerage> Peerage { get; set; }
        public virtual DbSet<DbSyndicate> Syndicates { get; set; }
        public virtual DbSet<DbSyndicateAttr> SyndicatesAttr { get; set; }
        public virtual DbSet<DbSyndicateMemberHistory> SyndicateMemberHistories { get; set; }
        public virtual DbSet<DbTotemAdd> TotemAdds { get; set; }
        public virtual DbSet<DbFamily> Families { get; set; }
        public virtual DbSet<DbFamilyAttr> FamilyAttrs { get; set; }
        public virtual DbSet<DbFamilyBattleEffectShareLimit> FamilyBattleEffectShareLimits { get; set; }
        public virtual DbSet<DbTrade> Trade { get; set; }
        public virtual DbSet<DbTradeItem> TradeItem { get; set; }
        public virtual DbSet<DbArenic> Arenics { get; set; }
        public virtual DbSet<DbArenicHonor> ArenicHonors { get; set; }
        public virtual DbSet<DbTutor> Tutor { get; set; }
        public virtual DbSet<DbTutorAccess> TutorAccess { get; set; }
        public virtual DbSet<DbTutorBattleLimitType> TutorBattleLimitTypes { get; set; }
        public virtual DbSet<DbTutorContribution> TutorContributions { get; set; }
        public virtual DbSet<DbTutorType> TutorTypes { get; set; }
        public virtual DbSet<DbFriend> Friends { get; set; }
        public virtual DbSet<DbEnemy> Enemies { get; set; }
        public virtual DbSet<DbBusiness> Business { get; set; }
        public virtual DbSet<DbFlower> Flowers { get; set; }
        public virtual DbSet<DbNewbieInfo> NewbieInfo { get; set; }
        public virtual DbSet<DbPigeon> Pigeons { get; set; }
        public virtual DbSet<DbPigeonQueue> PigeonQueues { get; set; }
        public virtual DbSet<DbAuction> Auctions { get; set; }
        public virtual DbSet<DbAuctionAskBuy> AuctionAskBuys { get; set; }
        public virtual DbSet<DbAstProfLevel> AstProfLevels { get; set; }
        public virtual DbSet<DbAstProfPromoteCondition> AstProfPromoteConditions { get; set; }
        public virtual DbSet<DbAstProfInaugurationCondition> AstProfInaugurationConditions { get; set; }
        public virtual DbSet<DbMail> Mails { get; set; }
        public virtual DbSet<DbUserTitle> UserTitles { get; set; }
        public virtual DbSet<DbFatePlayer> FatePlayers { get; set; }
        public virtual DbSet<DbFateProtect> FateProtects { get; set; }
        public virtual DbSet<DbFateRand> FateRands { get; set; }
        public virtual DbSet<DbFateRank> FateRanks { get; set; }
        public virtual DbSet<DbFateRule> FateRules { get; set; }
        public virtual DbSet<DbInitFateAttrib> InitFateAttribs { get; set; }
        public virtual DbSet<DbJiangHuAttribRand> JiangHuAttribRands { get; set; }
        public virtual DbSet<DbJiangHuCaltivateCondition> JiangHuCaltivateConditions { get; set; }
        public virtual DbSet<DbJiangHuCaltivateTimes> JiangHuCaltivateTimes { get; set; }
        public virtual DbSet<DbJiangHuPlayer> JiangHuPlayers { get; set; }
        public virtual DbSet<DbJiangHuPlayerPower> JiangHuPlayerPowers { get; set; }
        public virtual DbSet<DbJiangHuPowerEffect> JiangHuPowerEffects { get; set; }
        public virtual DbSet<DbJiangHuQualityRand> JiangHuQualityRands { get; set; }
        public virtual DbSet<DbMineCtrl> MineRates { get; set; }
        public virtual DbSet<DbMagictype> Magictype { get; set; }
        public virtual DbSet<DbMagic> Magic { get; set; }
        public virtual DbSet<DbDisdain> Disdains { get; set; }
        public virtual DbSet<DbMonstertype> Monstertype { get; set; }
        public virtual DbSet<DbMonsterKill> MonsterKills { get; set; }
        public virtual DbSet<DbMonsterTypeMagic> MonsterTypeMagic { get; set; }
        public virtual DbSet<DbDetainedItem> DetainedItems { get; set; }
        public virtual DbSet<DbQuiz> Quiz { get; set; }
        public virtual DbSet<DbAchievement> Achievements { get; set; }
        public virtual DbSet<DbAchievementType> AchievementTypes { get; set; }
        public virtual DbSet<DbMeedRecord> MeedRecords { get; set; }
        public virtual DbSet<DbSetMeed> SetMeeds { get; set; }
        public virtual DbSet<DbSynCompeteRank> SynCompeteRanks { get; set; }
        public virtual DbSet<DbSuperFlag> SuperFlags { get; set; }
        public virtual DbSet<DbSynAdvertisingInfo> SynAdvertisingInfos { get; set; }
        public virtual DbSet<DbPkStatistic> PkStatistics { get; set; }
        public virtual DbSet<DbVipTransPoint> VipTransPoints { get; set; }
        public virtual DbSet<DbVipMineTime> VipMineTime { get; set; }
        public virtual DbSet<DbGoldenLeagueData> GoldenLeagueDatas { get; set; }
        public virtual DbSet<DbDailyReset> DailyResets { get; set; }
        public virtual DbSet<DbDynaRankRec> DynaRankRecs { get; set; }
        public virtual DbSet<DbTrap> Traps { get; set; }
        public virtual DbSet<DbTrapType> TrapsType { get; set; }
        public virtual DbSet<DbDynaGlobalData> DynaGlobalDatas { get; set; }
        public virtual DbSet<DbActivityRewardType> ActivityRewardTypes { get; set; }
        public virtual DbSet<DbActivityTaskType> ActivityTaskTypes { get; set; }
        public virtual DbSet<DbActivityUserTask> ActivityUserTasks { get; set; }
        public virtual DbSet<DbProcessGoal> ProcessGoals { get; set; }
        public virtual DbSet<DbProcessTask> ProcessTasks { get; set; }
        public virtual DbSet<DbChangeNameBackup> ChangeNameBackups { get; set; }
        public virtual DbSet<DbInstanceType> InstanceTypes { get; set; }
        public virtual DbSet<DbInstanceEnterCondition> InstanceEnterConditions { get; set; }
        public virtual DbSet<DbInnerStrenghtPlayer> InnerStrenghtPlayers { get; set; }
        public virtual DbSet<DbInnerStrenghtSecret> InnerStrenghtSecrets { get; set; }
        public virtual DbSet<DbInnerStrenghtSecretType> InnerStrenghtSecretTypes { get; set; }
        public virtual DbSet<DbInnerStrenghtTypeInfo> InnerStrenghtTypeInfos { get; set; }
        public virtual DbSet<DbInnerStrenghtTypeLevInfo> InnerStrenghtTypeLevInfos { get; set; }
        public virtual DbSet<DbInnerStrengthRand> InnerStrengthRands { get; set; }
        public virtual DbSet<DbPkInfo> PkInfos { get; set; }
        public virtual DbSet<DbAwardConfig> AwardConfigs { get; set; }
        public virtual DbSet<DbPetPoint> PetPoints { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DbTrade>().Property(x => x.Type).HasConversion<uint>();
            modelBuilder.Entity<DbFateRand>(e => e.HasNoKey());
            modelBuilder.Entity<DbSuperman>().Property(x => x.UserIdentity).ValueGeneratedNever();
            modelBuilder.Entity<DbAchievement>().Property(x => x.UserIdentity).ValueGeneratedNever();
            modelBuilder.Entity<DbVipMineTime>().Property(x => x.UserId).ValueGeneratedNever();
            modelBuilder.Entity<DbSyndicateAttr>().Property(x => x.UserIdentity).ValueGeneratedNever();
            modelBuilder.Entity<DbFamilyAttr>().Property(x => x.UserIdentity).ValueGeneratedNever();
            modelBuilder.Entity<DbInstanceEnterCondition>().Property(x => x.InstanceId).ValueGeneratedNever();
            modelBuilder.Entity<DbInnerStrengthRand>(e => e.HasNoKey());
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            base.OnConfiguring(options);
        }

        public static bool Ping()
        {
            using var ctx = new ServerDbContext();
            try
            {
                return ctx.Database.CanConnect();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "{Message}", ex.Message);
                return false;
            }
        }

        public static async Task<bool> CreateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                await using var serverDbContext = new ServerDbContext();
                serverDbContext.Add<T>(entity);
                await serverDbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "[{}] CreateAsync has throw: {ExceptionMessage}", typeof(T).FullName, ex.Message);
                return false;
            }
        }

        public async static Task<bool> CreateRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var serverDbContext = new ServerDbContext();
                foreach (var entity in entities)
                {
                    serverDbContext.Add(entity);
                }
                await serverDbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "[{}] CreateRangeAsync has throw: {ExceptionMessage}", typeof(T).FullName, ex.Message);
                return false;
            }
        }

        public static async Task<bool> SaveAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var serverDbContext = new ServerDbContext();
                serverDbContext.Update(entity);
                await serverDbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "[{}] SaveAsync has throw: {ExceptionMessage}", typeof(T).FullName, ex.Message);
                return false;
            }
        }

        public static async Task<bool> SaveRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var serverDbContext = new ServerDbContext();
                foreach (var entity in entities)
                {
                    serverDbContext.Update(entity);
                }
                await serverDbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "[{}] SaveRangeAsync has throw: {ExceptionMessage}", typeof(T).FullName, ex.Message);
                return false;
            }
        }

        public static async Task<bool> DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                await using var serverDbContext = new ServerDbContext();
                serverDbContext.Remove(entity);
                await serverDbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "[{}] DeleteAsync has throw: {ExceptionMessage}", typeof(T).FullName, ex.Message);
                return false;
            }
        }

        public static async Task<bool> DeleteRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
            where T : class
        {
            try
            {
                await using var serverDbContext = new ServerDbContext();
                foreach (var entity in entities)
                {
                    serverDbContext.Remove(entity);
                }
                await serverDbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "[{}] DeleteRangeAsync has throw: {ExceptionMessage}", typeof(T).FullName, ex.Message);
                return false;
            }
        }

        public static async Task<string> ScalarAsync(string query)
        {
            await using var db = new ServerDbContext();
            DbConnection connection = db.Database.GetDbConnection();
            ConnectionState state = connection.State;

            string result;
            try
            {
                if ((state & ConnectionState.Open) == 0)
                {
                    await connection.OpenAsync();
                }

                DbCommand cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;

                result = (await cmd.ExecuteScalarAsync())?.ToString();
            }
            finally
            {
                if (state != ConnectionState.Closed)
                {
                    await connection.CloseAsync();
                }
            }

            return result;
        }

        public static async Task<DataTable> SelectAsync(string query)
        {
            await using var db = new ServerDbContext();
            var result = new DataTable();
            DbConnection connection = db.Database.GetDbConnection();
            ConnectionState state = connection.State;

            try
            {
                if (state != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                DbCommand command = connection.CreateCommand();
                command.CommandText = query;
                command.CommandType = CommandType.Text;

                await using DbDataReader reader = await command.ExecuteReaderAsync();
                result.Load(reader);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (state != ConnectionState.Closed)
                {
                    await connection.CloseAsync();
                }
            }

            return result;
        }
    }
}
