using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;
using System.Collections.Concurrent;
using System.Drawing;
using static Canyon.Game.Sockets.Game.Packets.MsgPeerage;

namespace Canyon.Game.Services.Managers
{
    public class PeerageManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<PeerageManager>();
        private static readonly ConcurrentDictionary<uint, NobilityObject> PeerageSet = new();

        public static async Task InitializeAsync()
        {
            logger.LogInformation("Initializating Peerage Manager");
            List<DbDynaRankRec> dbPeerages = await PeerageRepository.GetAsync();
            foreach (DbDynaRankRec peerage in dbPeerages)
            {
                DbCharacter user = await CharacterRepository.FindByIdentityAsync(peerage.UserId);
                if (user == null)
                {
                    await ServerDbContext.DeleteAsync(peerage);
                    continue;
                }
                PeerageSet.TryAdd(peerage.UserId, new NobilityObject(peerage, user.Name));
            }
        }

        public static async Task DonateAsync(Character user, long amount)
        {
            int oldPosition = GetPosition(user.Identity);
            NobilityRank oldRank = GetRanking(user.Identity);

            amount = Math.Min(amount, 10_000_000_000);

            if (!PeerageSet.TryGetValue(user.Identity, out NobilityObject peerage))
            {
                peerage = new NobilityObject(new DbDynaRankRec
                {
                    UserId = user.Identity,
                    Value = user.NobilityDonation + amount,
                    RankType = 3_000_003
                }, user.Name);

                PeerageSet.TryAdd(user.Identity, peerage);
            }
            else
            {
                peerage.Donation += amount;                
            }

            await peerage.SaveAsync();

            user.NobilityDonation = peerage.Donation;
            await user.SaveAsync();

            NobilityRank rank = GetRanking(user.Identity);
            int position = GetPosition(user.Identity);

            await user.SendNobilityInfoAsync();

            if (position != oldPosition && position < 50)
            {
                foreach (NobilityObject peer in PeerageSet.Values
                    .Where(x => x.Donation > 0)
                    .OrderByDescending(z => z.Donation))
                {
                    Character targetUser = RoleManager.GetUser(peer.UserIdentity);
                    if (targetUser != null)
                    {
                        await targetUser.SendNobilityInfoAsync(true);
                    }
                }
            }

            if (rank != oldRank)
            {
                var message = "";
                switch (rank)
                {
                    case NobilityRank.King:
                        if (user.Gender == 1)
                        {
                            message = string.Format(StrPeeragePromptKing, user.Name,
                                                    ServerConfiguration.Configuration.Realm.Name);
                        }
                        else
                        {
                            message = string.Format(StrPeeragePromptQueen, user.Name,
                                                    ServerConfiguration.Configuration.Realm.Name);
                        }

                        break;
                    case NobilityRank.Prince:
                        if (user.Gender == 1)
                        {
                            message = string.Format(StrPeeragePromptPrince, user.Name,
                                                    ServerConfiguration.Configuration.Realm.Name);
                        }
                        else
                        {
                            message = string.Format(StrPeeragePromptPrincess, user.Name,
                                                    ServerConfiguration.Configuration.Realm.Name);
                        }

                        break;
                    case NobilityRank.Duke:
                        if (user.Gender == 1)
                        {
                            message = string.Format(StrPeeragePromptDuke, user.Name,
                                                    ServerConfiguration.Configuration.Realm.Name);
                        }
                        else
                        {
                            message = string.Format(StrPeeragePromptDuchess, user.Name,
                                                    ServerConfiguration.Configuration.Realm.Name);
                        }

                        break;
                    case NobilityRank.Earl:
                        if (user.Gender == 1)
                        {
                            message = string.Format(StrPeeragePromptEarl, user.Name);
                        }
                        else
                        {
                            message = string.Format(StrPeeragePromptCountess, user.Name);
                        }

                        break;
                    case NobilityRank.Baron:
                        if (user.Gender == 1)
                        {
                            message = string.Format(StrPeeragePromptBaron, user.Name);
                        }
                        else
                        {
                            message = string.Format(StrPeeragePromptBaroness, user.Name);
                        }

                        break;
                    case NobilityRank.Knight:
                        if (user.Gender == 1)
                        {
                            message = string.Format(StrPeeragePromptKnight, user.Name);
                        }
                        else
                        {
                            message = string.Format(StrPeeragePromptLady, user.Name);
                        }

                        break;
                }

                if (user.Team != null)
                {
                    await user.Team.SyncFamilyBattlePowerAsync();
                }

                if (user.ApprenticeCount > 0)
                {
                    await user.SynchroApprenticesSharedBattlePowerAsync();
                }

                await BroadcastWorldMsgAsync(message, TalkChannel.Center, Color.Red);
            }
        }

        public static async Task ChangeNameAsync(Character user, string newName)
        {
            if (PeerageSet.TryGetValue(user.Identity, out var peerage))
            {
                peerage.UserName = newName;
            }
        }

        public static NobilityRank GetRanking(uint idUser)
        {
            int position = GetPosition(idUser);
            if (position >= 0 && position < 3)
            {
                return NobilityRank.King;
            }

            if (position >= 3 && position < 15)
            {
                return NobilityRank.Prince;
            }

            if (position >= 15 && position < 50)
            {
                return NobilityRank.Duke;
            }

            NobilityObject peerageUser = GetUser(idUser);
            ulong donation = 0;
            if (peerageUser != null)
            {
                donation = (ulong)peerageUser.Donation;
            }
            else
            {
                Character user = RoleManager.GetUser(idUser);
                if (user != null)
                {
                    donation = (ulong)user.NobilityDonation;
                }
            }

            if (donation >= 200000000)
            {
                return NobilityRank.Earl;
            }

            if (donation >= 100000000)
            {
                return NobilityRank.Baron;
            }

            if (donation >= 30000000)
            {
                return NobilityRank.Knight;
            }

            return NobilityRank.Serf;
        }

        public static int GetPosition(uint idUser)
        {
            var found = false;
            int idx = -1;

            foreach (NobilityObject peerage in PeerageSet.Values.OrderByDescending(x => x.Donation))
            {
                idx++;
                if (peerage.UserIdentity == idUser)
                {
                    found = true;
                    break;
                }

                if (idx >= 50)
                {
                    break;
                }
            }

            return found ? idx : -1;
        }

        public static async Task SendRankingAsync(Character target, int page)
        {
            if (target == null)
            {
                return;
            }

            const int MAX_PER_PAGE_I = 10;
            const int MAX_PAGES = 5;

            int currentPagesNum = Math.Max(1, Math.Min(PeerageSet.Count / MAX_PER_PAGE_I + 1, MAX_PAGES));
            if (page >= currentPagesNum)
            {
                return;
            }

            var current = 0;
            int min = page * MAX_PER_PAGE_I;
            int max = page * MAX_PER_PAGE_I + MAX_PER_PAGE_I;

            var rank = new List<NobilityStruct>();
            foreach (NobilityObject peerage in PeerageSet.Values.OrderByDescending(x => x.Donation))
            {
                if (current >= MAX_PAGES * MAX_PER_PAGE_I)
                {
                    break;
                }

                if (current < min)
                {
                    current++;
                    continue;
                }

                if (current >= max)
                {
                    break;
                }

                Character peerageUser = RoleManager.GetUser(peerage.UserIdentity);
                uint lookface = peerageUser?.Mesh ?? 0;
                rank.Add(new NobilityStruct
                {
                    Identity = peerage.UserIdentity,
                    Name = peerage.UserName,
                    Donation = (ulong)peerage.Donation,
                    LookFace = lookface,
                    Position = current,
                    Rank = GetRanking(peerage.UserIdentity)
                });

                current++;
            }

            var msg = new MsgPeerage(NobilityAction.List, (ushort)Math.Min(MAX_PER_PAGE_I, rank.Count),
                                     (ushort)currentPagesNum);
            msg.Rank.AddRange(rank);
            await target.SendAsync(msg);
        }

        public static NobilityObject GetUser(uint idUser)
        {
            return PeerageSet.TryGetValue(idUser, out NobilityObject peerage) ? peerage : null;
        }

        public static ulong GetNextRankSilver(NobilityRank rank, ulong donation)
        {
            switch (rank)
            {
                case NobilityRank.Knight: return 30000000 - donation;
                case NobilityRank.Baron: return 100000000 - donation;
                case NobilityRank.Earl: return 200000000 - donation;
                case NobilityRank.Duke: return GetDonation(50) - donation;
                case NobilityRank.Prince: return GetDonation(15) - donation;
                case NobilityRank.King: return GetDonation(3) - donation;
                default: return 0;
            }
        }

        public static ulong GetDonation(int position)
        {
            var ranking = 1;
            ulong donation = 0;
            foreach (NobilityObject peerage in PeerageSet.Values.OrderByDescending(x => x.Donation))
            {
                donation = (ulong)peerage.Donation;
                if (ranking++ == position)
                {
                    break;
                }
            }

            return Math.Max(3000000, donation);
        }

        public static async Task SaveAsync()
        {
            foreach (NobilityObject peerage in PeerageSet.Values)
            {
                await peerage.SaveAsync();
            }
        }

        public class NobilityObject
        {
            private readonly DbDynaRankRec rec;

            public NobilityObject(DbDynaRankRec rec, string userName)
            {
                this.rec = rec;
                UserName = userName;
            }

            public uint UserIdentity => rec.UserId;
            public string UserName { get; set; }
            public long Donation
            {
                get => rec.Value;
                set => rec.Value = value;
            }

            public Task SaveAsync() => ServerDbContext.SaveAsync(rec);
            public Task DeleteAsync() => ServerDbContext.DeleteAsync(rec);
        }
    }
}
