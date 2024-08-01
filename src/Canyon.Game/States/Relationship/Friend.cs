using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Syndicates;
using Canyon.Game.States.User;
using static Canyon.Game.Sockets.Game.Packets.MsgPeerage;

namespace Canyon.Game.States.Relationship
{
    public sealed class Friend
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<Friend>();

        private DbFriend friend;
        private readonly Character owner;

        public Friend(Character owner)
        {
            this.owner = owner;
        }

        public uint Identity => friend.TargetIdentity;
        public string Name { get; private set; }
        public bool Online => User != null;
        public Character User => RoleManager.GetUser(Identity);

        public bool Create(Character user)
        {
            friend = new DbFriend
            {
                UserIdentity = owner.Identity,
                TargetIdentity = user.Identity
            };
            Name = user.Name;
            return true;
        }

        public async Task CreateAsync(DbFriend friend)
        {
            this.friend = friend;
            Name = (await CharacterRepository.FindByIdentityAsync(friend.TargetIdentity))?.Name ?? Language.StrNone;
        }

        public async Task SendAsync()
        {
            await owner.SendAsync(new MsgFriend
            {
                Identity = Identity,
                Name = Name,
                Action = MsgFriend.MsgFriendAction.AddFriend,
                Online = Online,
                Gender = User?.Gender ?? 0,
                Nobility = (int)(User?.NobilityRank ?? NobilityRank.Serf)
            });
        }

        public async Task SendInfoAsync()
        {
            Character user = User;
            await owner.SendAsync(new MsgFriendInfo
            {
                Identity = Identity,
                PkPoints = user?.PkPoints ?? 0,
                Level = user?.Level ?? 0,
                Mate = user?.MateName ?? StrNone,
                Profession = user?.Profession ?? 0,
                Lookface = user?.Mesh ?? 0,
                SyndicateIdentity = user?.SyndicateIdentity ?? 0,
                SyndicateRank = (ushort)(user?.SyndicateRank ?? SyndicateMember.SyndicateRank.None)
            });
        }

        public Task<bool> SaveAsync()
        {
            return ServerDbContext.SaveAsync(friend);
        }

        public Task<bool> DeleteAsync()
        {
            return ServerDbContext.DeleteAsync(friend);
        }
    }
}
