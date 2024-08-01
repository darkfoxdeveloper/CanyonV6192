using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Syndicates;
using Canyon.Game.States.User;
using System.Collections.Concurrent;
using static Canyon.Game.States.Syndicates.Syndicate;

namespace Canyon.Game.Services.Managers
{
    public class SyndicateManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<SyndicateManager>();

        private static readonly TimeOut syndicateAdvertiseCheckTimer = new();
        private static readonly ConcurrentDictionary<ushort, Syndicate> syndicates = new();
        private static readonly ConcurrentDictionary<uint, DbSynAdvertisingInfo> synAdvertisingInfos = new();
        public static async Task<bool> InitializeAsync()
        {
            logger.LogInformation("Initializating syndicate data");

            syndicateAdvertiseCheckTimer.Startup(60);

            List<DbSyndicate> dbSyndicates = await SyndicateRepository.GetAsync();
            foreach (DbSyndicate dbSyn in dbSyndicates)
            {
                var syn = new Syndicate();
                if (!await syn.CreateAsync(dbSyn))
                {
                    continue;
                }

                syndicates.TryAdd(syn.Identity, syn);
            }

            foreach (Syndicate syndicate in syndicates.Values)
            {
                syndicate.LoadRelation();
            }

            var advertisings = await SyndicateRepository.GetAdvertiseAsync();
            foreach (var adv in advertisings)
            {
                synAdvertisingInfos.TryAdd(adv.IdSyn, adv);
            }

            return true;
        }

        public static bool AddSyndicate(Syndicate syn)
        {
            return syndicates.TryAdd(syn.Identity, syn);
        }

        public static Syndicate GetSyndicate(int idSyndicate)
        {
            return syndicates.TryGetValue((ushort)idSyndicate, out Syndicate syn) ? syn : null;
        }

        public static Syndicate GetSyndicate(string name)
        {
            return syndicates.Values.FirstOrDefault(
                x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        ///     Find the syndicate a user is in.
        /// </summary>
        public static Syndicate FindByUser(uint idUser)
        {
            return syndicates.Values.FirstOrDefault(x => x.QueryMember(idUser) != null);
        }

        public static Syndicate GetSyndicate(uint ownerIdentity)
        {
            return GetSyndicate((ushort)ownerIdentity);
        }

        public static bool HasSyndicateAdvertise(uint idSyn)
        {
            return synAdvertisingInfos.ContainsKey(idSyn);
        }

        public static async Task JoinByAdvertisingAsync(Character user, ushort syndicateIdentity)
        {
            if (user.Syndicate != null)
            {
                return;
            }

            if (!synAdvertisingInfos.TryGetValue(syndicateIdentity, out var advertising))
            {
                return;
            }

            if (advertising.ConditionMetem > user.Metempsychosis)
            {
                return;
            }

            if (advertising.ConditionLevel > user.Level)
            {
                return;
            }

            ProfessionPermission denyProfession = (ProfessionPermission)advertising.ConditionProf;
            if (user.ProfessionSort == 10 && denyProfession.HasFlag(ProfessionPermission.Trojan))
            {
                await user.SendAsync(StrSynRecruitNotAllowProfession);
                return;
            }
            else if (user.ProfessionSort == 20 && denyProfession.HasFlag(ProfessionPermission.Warrior))
            {
                await user.SendAsync(StrSynRecruitNotAllowProfession);
                return;
            }
            else if (user.ProfessionSort == 40 && denyProfession.HasFlag(ProfessionPermission.Archer))
            {
                await user.SendAsync(StrSynRecruitNotAllowProfession);
                return;
            }
            else if (user.ProfessionSort == 50 && denyProfession.HasFlag(ProfessionPermission.Ninja))
            {
                await user.SendAsync(StrSynRecruitNotAllowProfession);
                return;
            }
            else if (user.ProfessionSort == 60 && denyProfession.HasFlag(ProfessionPermission.Monk))
            {
                await user.SendAsync(StrSynRecruitNotAllowProfession);
                return;
            }
            else if (user.ProfessionSort == 70 && denyProfession.HasFlag(ProfessionPermission.Pirate))
            {
                await user.SendAsync(StrSynRecruitNotAllowProfession);
                return;
            }
            else if (user.ProfessionSort == 100 && denyProfession.HasFlag(ProfessionPermission.Taoist))
            {
                await user.SendAsync(StrSynRecruitNotAllowProfession);
                return;
            }

            Syndicate targetSyndicate = GetSyndicate(syndicateIdentity);
            if (targetSyndicate == null)
            {
                return;
            }

            await targetSyndicate.AppendMemberAsync(user, null, Syndicate.JoinMode.Recruitment);
        }

        public static async Task SubmitEditAdvertiseScreenAsync(Character user)
        {
            if (user.Syndicate == null || user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                return;
            }

            if (!synAdvertisingInfos.TryGetValue(user.SyndicateIdentity, out var adv))
            {
                return;
            }

            await user.SendAsync(new MsgSynRecuitAdvertising()
            {
                Identity = user.SyndicateIdentity,
                Description = adv.Content[..Math.Min(255, adv.Content.Length)],
                Silvers = adv.Expense,
                AutoRecruit = adv.AutoRecruit != 0,
                RequiredLevel = adv.ConditionLevel,
                RequiredMetempsychosis = adv.ConditionMetem,
                ForbidGender = adv.ConditionSex,
                ForbidProfession = adv.ConditionProf
            });
        }

        public static async Task PublishAdvertisingAsync(Character user, 
            long money,
            string description, 
            int requiredLevel, 
            int requiredMetempsychosis, 
            int requiredProfession,
            int conditionBattle, // not sure
            int conditionSex,
            bool autoJoin)
        {
            if (user.Syndicate == null)
            {
                return;
            }

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                return;
            }

            if (synAdvertisingInfos.TryGetValue(user.SyndicateIdentity, out _))
            {
                await ReplaceAdvertisingAsync(user, money, description, requiredLevel, requiredMetempsychosis, requiredProfession, conditionBattle, conditionSex, autoJoin);
                return;
            }

            requiredLevel = Math.Max(1, Math.Min(ExperienceManager.GetLevelLimit(), requiredLevel));
            requiredMetempsychosis = Math.Max(0, Math.Min(2, requiredMetempsychosis));

            if (user.Syndicate.Money < money)
            {
                return;
            }

            if (user.Syndicate.Money < Syndicate.SYNDICATE_ACTION_COST)
            {
                return;
            }

            DbSynAdvertisingInfo dbInfo = new()
            {
                IdSyn = user.SyndicateIdentity,
                Content = description,
                Expense = (uint)money,
                AutoRecruit = (byte)(autoJoin ? 1 : 0),
                ConditionLevel = (byte)requiredLevel,
                ConditionMetem = (byte)requiredMetempsychosis,
                ConditionProf = (ushort)requiredProfession,
                ConditionBattle = (ushort)conditionBattle,
                ConditionSex = (byte)conditionSex,
                EndDate = (uint)UnixTimestamp.FromDateTime(DateTime.Now.AddDays(7))
            };

            if (await ServerDbContext.SaveAsync(dbInfo) 
                && synAdvertisingInfos.TryAdd(user.SyndicateIdentity, dbInfo))
            {
                user.Syndicate.Money -= money;
                await user.Syndicate.SaveAsync();
            }

            await user.SendAsync(new MsgSynRecruitAdvertisingList());
        }

        public static async Task ReplaceAdvertisingAsync(Character user,
            long money,
            string description,
            int requiredLevel,
            int requiredMetempsychosis,
            int requiredProfession,
            int conditionBattle, // not sure
            int conditionSex,
            bool autoJoin)
        {
            if (user.Syndicate == null)
            {
                return;
            }

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                return;
            }

            if (!synAdvertisingInfos.TryGetValue(user.SyndicateIdentity, out var advertise))
            {
                return;
            }

            if (money < advertise.Expense)
            {
                return;
            }

            requiredLevel = Math.Max(1, Math.Min(ExperienceManager.GetLevelLimit(), requiredLevel));
            requiredMetempsychosis = Math.Max(0, Math.Min(2, requiredMetempsychosis));

            if (user.Syndicate.Money < money)
            {
                return;
            }

            if (user.Syndicate.Money < Syndicate.SYNDICATE_ACTION_COST)
            {
                return;
            }

            bool spendMoney = money != advertise.Expense;
            if (spendMoney)
            {
                advertise.Expense = (uint)money;
                advertise.EndDate = (uint)UnixTimestamp.FromDateTime(DateTime.Now.AddDays(7));
            }

            advertise.Content = description;
            advertise.AutoRecruit = (byte)(autoJoin ? 1 : 0);
            advertise.ConditionLevel = (byte)requiredLevel;
            advertise.ConditionMetem = (byte)requiredMetempsychosis;
            advertise.ConditionProf = (ushort)requiredProfession;
            advertise.ConditionBattle = (ushort)conditionBattle;
            advertise.ConditionSex = (byte)conditionSex;

            if (await ServerDbContext.SaveAsync(advertise))
            {
                if (spendMoney)
                {
                    user.Syndicate.Money -= money;
                    await user.Syndicate.SaveAsync();
                }
            }
        }

        public static async Task SubmitAdvertisingListAsync(Character user, int startIndex) 
        {
            const int ipp = 4;
            MsgSynRecruitAdvertisingList msg = new()
            {
                StartIndex = startIndex,
                TotalRecords = synAdvertisingInfos.Count,
                CurrentPageIndex = 0
            };
            foreach (var adv in synAdvertisingInfos.Values
                .OrderByDescending(x => x.Expense)
                .Skip(startIndex)
                .Take(ipp))
            {
                Syndicate syn = GetSyndicate(adv.IdSyn);
                string synName = syn?.Name ?? $"Error{adv.IdSyn}";
                string leaderName = syn?.Leader?.UserName ?? "Error";
                int memberCount = syn?.MemberCount ?? 0;
                long money = syn?.Money ?? 0;
                int level = syn?.Level ?? 1;

                msg.Advertisings.Add(new MsgSynRecruitAdvertisingList.AdvertisingStruct
                {
                    Identity = adv.IdSyn,
                    Description = adv.Content,
                    Name = synName,
                    LeaderName = leaderName,
                    Count = memberCount,
                    Funds = money,
                    Level = level,
                    AutoJoin = adv.AutoRecruit != 0,
                    DenyProfession = 0
                });
                
                if (msg.Advertisings.Count >= 2)
                {
                    await user.SendAsync(msg);
                    msg.Advertisings.Clear();
                    msg.CurrentPageIndex++;
                }
            }

            if (msg.Advertisings.Count > 0)
            {
                await user.SendAsync(msg);
            }
        }

        public static async Task OnTimerAsync()
        {
            if (syndicateAdvertiseCheckTimer.ToNextTime())
            {
                foreach (var adv in synAdvertisingInfos.Where(x => x.Value.EndDate < UnixTimestamp.Now))
                {
                    synAdvertisingInfos.TryRemove(adv.Key, out _);
                }
            }
        }
    }
}
