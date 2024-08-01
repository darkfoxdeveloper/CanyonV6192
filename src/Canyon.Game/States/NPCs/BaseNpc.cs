using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Syndicates;
using Canyon.Game.States.User;
using Canyon.Game.States.World;
using System.Drawing;

namespace Canyon.Game.States.NPCs
{
    public abstract class BaseNpc : Role
    {
        #region Constants

        public const int
            NPC_NONE = 0, // Í¨ÓÃNPC
            SHOPKEEPER_NPC = 1, // ÉÌµêNPC
            TASK_NPC = 2, // ÈÎÎñNPC(ÒÑ×÷·Ï£¬½öÓÃÓÚ¼æÈÝ¾ÉÊý¾Ý)
            STORAGE_NPC = 3, // ¼Ä´æ´¦NPC
            TRUNCK_NPC = 4, // Ïä×ÓNPC
            FACE_NPC = 5, // ±äÍ·ÏñNPC
            FORGE_NPC = 6, // ¶ÍÔìNPC		(only use for client)
            EMBED_NPC = 7, // ÏâÇ¶NPC
            STATUARY_NPC = 9, // µñÏñNPC
            SYNFLAG_NPC = 10, // °ïÅÉ±ê¼ÇNPC
            ROLE_PLAYER = 11, // ÆäËûÍæ¼Ò		(only use for client)
            ROLE_HERO = 12, // ×Ô¼º			(only use for client)
            ROLE_MONSTER = 13, // ¹ÖÎï			(only use for client)
            BOOTH_NPC = 14, // °ÚÌ¯NPC		(CBooth class)
            SYNTRANS_NPC = 15, // °ïÅÉ´«ËÍNPC, ¹Ì¶¨ÄÇ¸ö²»ÒªÓÃ´ËÀàÐÍ! (ÓÃÓÚ00:00ÊÕ·Ñ)(LINKIDÎª¹Ì¶¨NPCµÄID£¬ÓëÆäËüÊ¹ÓÃLINKIDµÄ»¥³â)
            ROLE_BOOTH_FLAG_NPC = 16, // Ì¯Î»±êÖ¾NPC	(only use for client)
            ROLE_MOUSE_NPC = 17, // Êó±êÉÏµÄNPC	(only use for client)
            ROLE_MAGICITEM = 18, // ÏÝÚå»ðÇ½		(only use for client)
            ROLE_DICE_NPC = 19, // ÷»×ÓNPC
            ROLE_SHELF_NPC = 20, // ÎïÆ·¼Ü
            WEAPONGOAL_NPC = 21, // ÎäÆ÷°Ð×ÓNPC
            MAGICGOAL_NPC = 22, // Ä§·¨°Ð×ÓNPC
            BOWGOAL_NPC = 23, // ¹­¼ý°Ð×ÓNPC
            ROLE_TARGET_NPC = 24, // °¤´ò£¬²»´¥·¢ÈÎÎñ	(only use for client)
            ROLE_FURNITURE_NPC = 25, // ¼Ò¾ßNPC	(only use for client)
            ROLE_CITY_GATE_NPC = 26, // ³ÇÃÅNPC	(only use for client)
            ROLE_NEIGHBOR_DOOR = 27, // ÁÚ¾ÓµÄÃÅ
            ROLE_CALL_PET = 28, // ÕÙ»½ÊÞ	(only use for client)
            EUDEMON_TRAINPLACE_NPC = 29, // »ÃÊÞÑ±ÑøËù
            AUCTION_NPC = 30, // ÅÄÂòNPC	ÎïÆ·ÁìÈ¡NPC  LW
            ROLE_FAMILY_WAR_FLAG = 31,
            ROLE_CTFBASE_NPC = 46,
            ROLE_3DFURNITURE_NPC = 101, // 3D¼Ò¾ßNPC 
            SYN_NPC_WARLETTER = 110; //Ôö¼ÓÐÂµÄ£Î£Ð£ÃÀàÐÍ¡¡×¨ÃÅÓÃÀ´¡¡ÏÂÕ½ÊéµÄ¡¡°ïÅÉ£Î£Ð£Ã

        [Flags]
        public enum NpcSort
        {
            None = 0,
            Task = 1,           // ÈÎÎñÀà
            Recycle = 2,            // ¿É»ØÊÕÀà
            Scene = 4,          // ³¡¾°Àà(´øµØÍ¼Îï¼þ)
            LinkMap = 8,            // ¹ÒµØÍ¼Àà(LINKIDÎªµØÍ¼ID£¬ÓëÆäËüÊ¹ÓÃLINKIDµÄ»¥³â)
            DieAction = 16,         // ´øËÀÍöÈÎÎñ(LINKIDÎªACTION_ID£¬ÓëÆäËüÊ¹ÓÃLINKIDµÄ»¥³â)
            DelEnable = 32,         // ¿ÉÒÔÊÖ¶¯É¾³ý(²»ÊÇÖ¸Í¨¹ýÈÎÎñ)
            Event = 64,         // ´ø¶¨Ê±ÈÎÎñ, Ê±¼äÔÚdata3ÖÐ£¬¸ñÊ½ÎªMMWWHHMMSS¡£(LINKIDÎªACTION_ID£¬ÓëÆäËüÊ¹ÓÃLINKIDµÄ»¥³â)
            Table = 128,            // ´øÊý¾Ý±íÀàÐÍ

            //		NPCSORT_SHOP		= ,			// ÉÌµêÀà
            //		NPCSORT_DICE		= ,			// ÷»×ÓNPC

            NpcsortUseLinkId = LinkMap | DieAction | Event,
        };

        #endregion

        protected BaseNpc(uint idNpc)
        {
            Identity = idNpc;
        }

        public virtual async Task<bool> InitializeAsync()
        {
            ShopGoods = await GoodsRepository.GetAsync(Identity);
            return true;
        }

        #region Type Identity

        public virtual ushort Type { get; }

        public virtual uint OwnerType { get; set; }

        public virtual NpcSort Sort { get; }

        public virtual int Base { get; }

        #endregion

        #region Position

        /// <remarks>If ran in a different thread, remember to send this action to map queue.</remarks>
        public virtual async Task<bool> ChangePosAsync(uint idMap, ushort x, ushort y)
        {
            GameMap map = MapManager.GetMap(idMap);
            if (map != null)
            {
                if (!map.IsValidPoint(x, y) && idMap != 5000)
                {
                    return false;
                }

                await LeaveMapAsync();
                this.idMap = idMap;
                X = x;
                Y = y;

                Task crossThreadTask()
                {
                    return EnterMapAsync();
                }

                if (map.Partition == Map?.Partition)
                {
                    await crossThreadTask();
                }
                else
                {
                    QueueAction(crossThreadTask);
                }
                return true;
            }
            return false;
        }

        #endregion

        #region Map

        public override async Task EnterMapAsync()
        {
            Map = MapManager.GetMap(MapIdentity);
            if (Map != null)
            {
                await Map.AddAsync(this);
                Map.AddTerrainObject(Identity, X, Y, Mesh);
            }
        }

        public override async Task LeaveMapAsync()
        {
            if (Map != null)
            {
                await Map.RemoveAsync(Identity);
                await Map.DelTerrainObjAsync(Identity);
            }
        }

        #endregion

        #region Task and Data

        public int GetData(string szAttr)
        {
            switch (szAttr.ToLower())
            {
                case "data0": return Data0;
                case "data1": return Data1;
                case "data2": return Data2;
                case "data3": return Data3;
            }
            return 0;
        }

        public bool SetData(string szAttr, int value)
        {
            switch (szAttr.ToLower())
            {
                case "data0": Data0 = value; return true;
                case "data1": Data1 = value; return true;
                case "data2": Data2 = value; return true;
                case "data3": Data3 = value; return true;
            }

            return false;
        }

        public bool AddData(string szAttr, int value)
        {
            switch (szAttr.ToLower())
            {
                case "data0": Data0 += value; return true;
                case "data1": Data1 += value; return true;
                case "data2": Data2 += value; return true;
                case "data3": Data3 += value; return true;
            }

            return false;
        }

        public uint GetTask(string task)
        {
            switch (task.ToLower())
            {
                case "task0": return Task0;
                case "task1": return Task1;
                case "task2": return Task2;
                case "task3": return Task3;
                case "task4": return Task4;
                case "task5": return Task5;
                case "task6": return Task6;
                case "task7": return Task7;
                default: return 0;
            }
        }

        public virtual bool Vending { get; set; }
        public virtual uint Task0 { get; }
        public virtual uint Task1 { get; }
        public virtual uint Task2 { get; }
        public virtual uint Task3 { get; }
        public virtual uint Task4 { get; }
        public virtual uint Task5 { get; }
        public virtual uint Task6 { get; }
        public virtual uint Task7 { get; }

        public virtual int Data0 { get; set; }
        public virtual int Data1 { get; set; }
        public virtual int Data2 { get; set; }
        public virtual int Data3 { get; set; }

        public virtual string DataStr { get; set; }

        #endregion

        #region Functions

        #region Shop

        public List<DbGoods> ShopGoods = new();

        #endregion

        #region Task

        public async Task<bool> ActivateNpc(Character user)
        {
            bool result = false;
            uint task = TestTasks(user);
            if (task != 0)
            {
                result = await GameAction.ExecuteActionAsync(task, user, this, null, "");
            }
            else if (user.IsPm())
            {
                await user.SendAsync($"Unhandled NPC[{Identity}:{Name}]->{Task0},{Task1},{Task2},{Task3},{Task4},{Task5},{Task6},{Task6},{Task7}", TalkChannel.Talk, Color.Red);
            }

            return result;
        }

        private uint TestTasks(Character user)
        {
            for (int i = 0; i < 8; i++)
            {
                DbTask task = EventManager.GetTask(GetTask($"task{i}"));
                if (task != null && user.TestTask(task))
                {
                    return task.IdNext;
                }
            }
            return 0;
        }

        #endregion

        #endregion

        #region Common Checks

        public bool IsLinkNpc()
        {
            return (Sort & NpcSort.LinkMap) != 0;
        }

        public bool IsShopNpc()
        {
            return Type == SHOPKEEPER_NPC;
        }

        public bool IsTaskNpc()
        {
            return Type == TASK_NPC;
        }

        public bool IsStorageNpc()
        {
            return Type == STORAGE_NPC;
        }

        public bool IsUserNpc()
        {
            return OwnerType == 1;
        }

        public bool IsSynNpc()
        {
            return OwnerType == 2;
        }

        public bool IsFamilyNpc()
        {
            return OwnerType == 4;
        }

        public bool IsSynFlag()
        {
            return Type == SYNFLAG_NPC && IsSynNpc();
        }

        public bool IsFlag()
        {
            return Type == 47;
        }

        public bool IsSysTrans()
        {
            return Type == SYNTRANS_NPC;
        }

        public bool IsCtfFlag()
        {
            return Type == ROLE_CTFBASE_NPC && IsSynNpc();
        }

        public bool IsAwardScore()
        {
            return IsSynFlag() || IsCtfFlag();
        }

        public bool IsSynMoneyEmpty()
        {
            if (!IsSynFlag())
            {
                return false;
            }

            Syndicate syn = SyndicateManager.GetSyndicate((int)OwnerIdentity);
            return syn != null && syn.Money <= 0;
        }

        #endregion

        #region Management

        public virtual Task DelNpcAsync()
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Database

        public virtual Task<bool> SaveAsync()
        {
            return Task.FromResult(true);
        }

        public virtual Task<bool> DeleteAsync()
        {
            return Task.FromResult(true);
        }


        #endregion
    }
}
