using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Syndicates;
using System.Drawing;

namespace Canyon.Game.States.User
{
    public partial class Character
    {
        public Syndicate Syndicate { get; set; }
        public SyndicateMember SyndicateMember => Syndicate?.QueryMember(Identity);
        public ushort SyndicateIdentity => Syndicate?.Identity ?? 0;
        public string SyndicateName => Syndicate?.Name ?? StrNone;

        public SyndicateMember.SyndicateRank SyndicateRank =>
            SyndicateMember?.Rank ?? SyndicateMember.SyndicateRank.None;

        public string SyndicateRankName => SyndicateMember?.RankName ?? StrNone;

        public async Task<bool> CreateSyndicateAsync(string name, int price = 1000000)
        {
            if (Syndicate != null)
            {
                await SendAsync(StrSynAlreadyJoined);
                return false;
            }

            if (name.Length > 15)
            {
                return false;
            }

            if (!RoleManager.IsValidName(name))
            {
                return false;
            }

            if (SyndicateManager.GetSyndicate(name) != null)
            {
                await SendAsync(StrSynNameInUse);
                return false;
            }

            if (!await SpendMoneyAsync(price))
            {
                await SendAsync(StrNotEnoughMoney);
                return false;
            }

            Syndicate = new Syndicate();
            if (!await Syndicate.CreateAsync(name, price, this))
            {
                Syndicate = null;
                await AwardMoneyAsync(price);
                return false;
            }

            if (!SyndicateManager.AddSyndicate(Syndicate))
            {
                await Syndicate.DeleteAsync();
                Syndicate = null;
                await AwardMoneyAsync(price);
                return false;
            }

            await BroadcastWorldMsgAsync(string.Format(StrSynCreate, Name, name), TalkChannel.Talk,
                                                Color.White);
            await SendSyndicateAsync();
            await Screen.SynchroScreenAsync();
            await Syndicate.BroadcastNameAsync();
            return true;
        }

        public async Task<bool> ChangeSyndicateNameAsync(string newName)
        {
            if (Syndicate == null)
            {
                return false;
            }

            if (newName.Length > 15)
            {
                return false;
            }

            if (!RoleManager.IsValidName(newName))
            {
                return false;
            }

            if (SyndicateManager.GetSyndicate(newName) != null)
            {
                await SendAsync(StrSynNameInUse);
                return false;
            }

            await Syndicate.ChangeNameAsync(newName);
            return true;
        }

        public async Task<bool> DisbandSyndicateAsync()
        {
            if (SyndicateIdentity == 0)
            {
                return false;
            }

            if (Syndicate.Leader.UserIdentity != Identity)
            {
                return false;
            }

            if (Syndicate.MemberCount > 1)
            {
                await SendAsync(StrSynNoDisband);
                return false;
            }

            return await Syndicate.DisbandAsync(this);
        }

        public async Task SendSyndicateAsync()
        {
            if (Syndicate != null)
            {
                await SendAsync(new MsgSyndicateAttributeInfo
                {
                    Identity = SyndicateIdentity,
                    Rank = (int)SyndicateRank,
                    MemberAmount = Syndicate.MemberCount,
                    Funds = Syndicate.Money,
                    PlayerDonation = SyndicateMember.Silvers,
                    LeaderName = Syndicate.Leader?.UserName ?? StrNone,
                    ConditionLevel = Syndicate.LevelRequirement,
                    ConditionMetempsychosis = Syndicate.MetempsychosisRequirement,
                    ConditionProfession = (int)Syndicate.ProfessionRequirement,
                    ConquerPointsFunds = Syndicate.ConquerPoints,
                    PositionExpiration = uint.Parse(SyndicateMember.PositionExpiration?.ToString("yyyyMMdd") ?? "0"),
                    EnrollmentDate = uint.Parse(SyndicateMember.JoinDate.ToString("yyyyMMdd")),
                    Level = Syndicate.Level
                });
                await SendAsync(new MsgSyndicate
                {
                    Mode = MsgSyndicate.SyndicateRequest.Bulletin,
                    Strings = new List<string> { Syndicate.Announce },
                    Identity = uint.Parse(Syndicate.AnnounceDate.ToString("yyyyMMdd"))
                });
                await Syndicate.SendAsync(this);
                await SynchroAttributesAsync(ClientUpdateType.TotemPoleBattlePower, (ulong)TotemBattlePower);
            }
            else
            {
                await SendAsync(new MsgSyndicateAttributeInfo
                {
                    Rank = (int)SyndicateMember.SyndicateRank.None
                });
            }
        }
    }
}
