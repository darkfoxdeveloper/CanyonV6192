using Canyon.Database.Entities;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Ai.Packets;
using Canyon.Shared.Managers;

namespace Canyon.Game.States.World
{
    public sealed class InstanceMap : GameMap
    {
        private readonly DbInstanceType instanceType;

        private TimeOut expirationTimer = new();

        public InstanceMap(DbMap map) 
            : base(map)
        {
            throw new NotSupportedException("Instance map should be only created as Dynamic Maps");
        }

        public InstanceMap(DbDynamap map, DbInstanceType instanceType) 
            : base(map)
        {
            this.instanceType = instanceType;
        }

        public uint InstanceType => instanceType.Id;

        public bool HasExpired => expirationTimer.IsTimeOut();

        public override async Task<bool> InitializeAsync()
        {
            if (!await base.InitializeAsync())
            {
                return false;
            }


            expirationTimer.Startup(instanceType.TimeLimit * 60);
            return true;
        }

        public override async Task<bool> RemoveAsync(uint idRole)
        {
            await base.RemoveAsync(idRole);
            if (users.IsEmpty && HasExpired)
            {
                await MapManager.RemoveMapAsync(Identity);
                IdentityManager.Instances.ReturnIdentity(Identity);
            }
            return true;
        }

        public async Task OnTimeOverAsync()
        {
            foreach (var user in users.Values)
            {
                if (!user.IsAlive)
                {
                    await user.RebornAsync(true);
                }
                else
                {
                    await user.FlyMapAsync(instanceType.ReturnMapId, instanceType.ReturnMapX, instanceType.ReturnMapY);
                }
            }
        }

        public override Task SendAddToNpcServerAsync()
        {
            return BroadcastNpcMsgAsync(new MsgAiDynaMap(dynaMapEntity, instanceType.Id, instanceType.MapId));
        }

        public new Task<bool> SaveAsync()
        {
            return Task.FromResult(true);
        }

        public new Task<bool> DeleteAsync()
        {
            return Task.FromResult(true);
        }
    }
}
