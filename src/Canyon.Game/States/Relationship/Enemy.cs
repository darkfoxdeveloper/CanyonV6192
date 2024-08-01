using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;
using static Canyon.Game.Sockets.Game.Packets.MsgPeerage;

namespace Canyon.Game.States.Relationship
{
    public sealed class Enemy
    {
        private DbEnemy enemy;
        private readonly Character owner;

        public Enemy(Character owner)
        {
            this.owner = owner;
        }

        public uint Identity => enemy.TargetIdentity;
        public string Name { get; private set; }
        public bool Online => User != null;
        public Character User => RoleManager.GetUser(Identity);

        public async Task<bool> CreateAsync(Character user)
        {
            enemy = new DbEnemy
            {
                UserIdentity = owner.Identity,
                TargetIdentity = user.Identity,
                Time = (uint)DateTime.Now.ToUnixTimestamp()
            };
            Name = user.Name;
            await SendAsync();
            return await SaveAsync();
        }

        public async Task CreateAsync(DbEnemy enemy)
        {
            this.enemy = enemy;
            Name = (await CharacterRepository.FindByIdentityAsync(enemy.TargetIdentity))?.Name ?? Language.StrNone;
        }

        public async Task SendAsync()
        {
            await owner.SendAsync(new MsgFriend
            {
                Identity = Identity,
                Name = Name,
                Action = MsgFriend.MsgFriendAction.AddEnemy,
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
                IsEnemy = true
            });
        }

        public Task<bool> SaveAsync()
        {
            return ServerDbContext.SaveAsync(enemy);
        }

        public Task<bool> DeleteAsync()
        {
            return ServerDbContext.DeleteAsync(enemy);
        }
    }
}
