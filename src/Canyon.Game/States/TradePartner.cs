using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;

namespace Canyon.Game.States
{
    public sealed class TradePartner
    {
        private readonly DbBusiness dbBusiness;

        public TradePartner(Character owner, DbBusiness business = null)
        {
            Owner = owner;
            if (business != null)
            {
                dbBusiness = business;
            }
        }

        public Character Owner { get; }

        public Character Target => RoleManager.GetUser(dbBusiness.UserId == Owner.Identity
                                    ? dbBusiness.BusinessId
                                    : dbBusiness.UserId);

        public uint Identity => dbBusiness.UserId == Owner.Identity ? dbBusiness.BusinessId : dbBusiness.UserId;
        public string Name => dbBusiness.UserId == Owner.Identity ? dbBusiness.Business?.Name : dbBusiness.User?.Name;
        public uint Lookface => dbBusiness.UserId == Owner.Identity ? dbBusiness.Business.Mesh : dbBusiness.User.Mesh;
        public ushort Level => dbBusiness.UserId == Owner.Identity ? dbBusiness.Business.Level : dbBusiness.User.Level;

        public int RemainingMinutes => (int)(!IsValid() ? (UnixTimestamp.ToDateTime(dbBusiness.Date) - DateTime.Now).TotalMinutes : 0);

        public bool IsValid()
        {
            return UnixTimestamp.ToDateTime(dbBusiness.Date) < DateTime.Now;
        }

        public Task NotifyAsync()
        {
            if (IsValid())
            {
                return Task.CompletedTask;
            }

            return Owner.SendAsync(new MsgTradeBuddy
            {
                Action = MsgTradeBuddy.TradeBuddyAction.AwaitingPartnersList,
                Identity = Lookface,
                Name = Owner.Name,
                IsOnline = true
            });
        }

        public Task SendAsync()
        {
            return Owner.SendAsync(new MsgTradeBuddy
            {
                Name = Name,
                Action = MsgTradeBuddy.TradeBuddyAction.AddPartner,
                IsOnline = Target != null,
                HoursLeft = RemainingMinutes,
                Identity = Identity,
                Level = Level
            });
        }

        public Task SendInfoAsync()
        {
            Character target = Target;
            if (target == null)
            {
                return Task.CompletedTask;
            }

            return Owner.SendAsync(new MsgTradeBuddyInfo
            {
                Identity = Identity,
                Name = target.MateName,
                Level = target.Level,
                Lookface = target.Mesh,
                PkPoints = target.PkPoints,
                Profession = target.Profession,
                Syndicate = target.SyndicateIdentity,
                SyndicatePosition = (int)target.SyndicateRank
            });
        }

        public Task SendRemoveAsync()
        {
            return Owner.SendAsync(new MsgTradeBuddy
            {
                Action = MsgTradeBuddy.TradeBuddyAction.BreakPartnership,
                Identity = Identity,
                IsOnline = true,
                Name = ""
            });
        }

        public Task<bool> DeleteAsync()
        {
            return ServerDbContext.DeleteAsync(dbBusiness);
        }
    }
}
