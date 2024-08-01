using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;

namespace Canyon.Game.States.NPCs
{
    public sealed class Npc : BaseNpc
    {
        private readonly DbNpc m_dbNpc;

        public Npc(DbNpc npc)
            : base(npc.Id)
        {
            m_dbNpc = npc;

            idMap = npc.Mapid;
            X = npc.Cellx;
            Y = npc.Celly;

            Name = npc.Name;
        }

        #region Type

        public override ushort Type => m_dbNpc.Type;

        public override NpcSort Sort => (NpcSort)m_dbNpc.Sort;

        public override uint OwnerIdentity
        {
            get => m_dbNpc.Ownerid;
            set => m_dbNpc.Ownerid = value;
        }

        #endregion

        #region Map and Position

        public override async Task<bool> ChangePosAsync(uint idMap, ushort x, ushort y)
        {
            if (await base.ChangePosAsync(idMap, x, y))
            {
                m_dbNpc.Mapid = idMap;
                m_dbNpc.Celly = y;
                m_dbNpc.Cellx = x;
                await SaveAsync();
                return true;
            }

            return false;
        }

        #endregion

        #region Task and Data

        public override uint Task0 => m_dbNpc.Task0;
        public override uint Task1 => m_dbNpc.Task1;
        public override uint Task2 => m_dbNpc.Task2;
        public override uint Task3 => m_dbNpc.Task3;
        public override uint Task4 => m_dbNpc.Task4;
        public override uint Task5 => m_dbNpc.Task5;
        public override uint Task6 => m_dbNpc.Task6;
        public override uint Task7 => m_dbNpc.Task7;

        public override int Data0
        {
            get => m_dbNpc.Data0;
            set => m_dbNpc.Data0 = value;
        }

        public override int Data1
        {
            get => m_dbNpc.Data1;
            set => m_dbNpc.Data1 = value;
        }

        public override int Data2
        {
            get => m_dbNpc.Data2;
            set => m_dbNpc.Data2 = value;
        }

        public override int Data3
        {
            get => m_dbNpc.Data3;
            set => m_dbNpc.Data3 = value;
        }

        public override string DataStr
        {
            get => m_dbNpc.Datastr;
            set => m_dbNpc.Datastr = value;
        }

        #endregion

        #region Socket

        public override async Task SendSpawnToAsync(Character player)
        {
            await player.SendAsync(new MsgNpcInfo
            {
                Identity = Identity,
                Lookface = (ushort)m_dbNpc.Lookface,
                Sort = m_dbNpc.Sort,
                PosX = X,
                PosY = Y,
                Name = string.Empty,
                NpcType = m_dbNpc.Type
            });
        }

        #endregion

        #region Database

        public override async Task<bool> SaveAsync()
        {
            return await ServerDbContext.SaveAsync(m_dbNpc);
        }

        public override async Task<bool> DeleteAsync()
        {
            return await ServerDbContext.DeleteAsync(m_dbNpc);
        }

        #endregion
    }
}
