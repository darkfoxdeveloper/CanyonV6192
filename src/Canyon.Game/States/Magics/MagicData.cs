using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.NPCs;
using Canyon.Game.States.User;
using Canyon.Shared.Mathematics;
using System.Collections.Concurrent;
using static Canyon.Game.Sockets.Game.Packets.MsgAction;

namespace Canyon.Game.States.Magics
{
    public sealed partial class MagicData
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MagicData>();

        private readonly Role role;
        private readonly ConcurrentDictionary<uint, Magic> magics = new();

        public MagicData(Role role)
        {
            this.role = role;
            delayTimer.Update();
            magicDelayTimer.Update();
        }

        public ConcurrentDictionary<uint, Magic> Magics => magics;

        public async Task<bool> InitializeAsync()
        {
            if (role is Character user)
            {
                var userMagics = await MagicRepository.GetAsync(user.Identity);
                foreach (var dbMagic in userMagics
                    .OrderBy(x => x.Type)
                    .ThenBy(x => x.Unlearn)
                    .ThenByDescending(x => x.Level))
                {
                    var magic = new Magic(role);
                    if (!magic.Create(dbMagic))
                    {
                        logger.LogError($"Could not load magic [{dbMagic.Id}] for user {user.Identity}");
                        continue;
                    }

                    if (!magics.TryAdd(magic.Type, magic))
                    {
                        await magic.DeleteAsync();
                    }
                    else
                    {
                        if (!magic.Unlearn)
                        {
                            await magic.SendAsync();
                        }
                    }
                }
            }

            return true;
        }

        public async Task<bool> CreateAsync(ushort type, byte level)
        {
            if (this[type] != null)
            {
                Magic old = this[type];
                old.Unlearn = false;

                if (role is Character)
                {
                    await old.SaveAsync();
                    await old.SendAsync();
                }
                return true;
            }

            var magic = new Magic(role);
            if (await magic.CreateAsync(type, level))
            {
                return magics.TryAdd(type, magic);
            }
            return false;
        }

        #region Experience

        public async Task<bool> AwardExpOfLifeAsync(Role target, int lifeLost, Magic magic = null,
                                                    bool magicRecruit = false)
        {
            if (role is not Character owner)
            {
                owner = role.IsCallPet() ? RoleManager.GetUser(role.OwnerIdentity) : null;
            }

            if (owner != null && ((target is Monster mob && mob.SpeciesType == 0 && !mob.IsGuard()) || target is DynamicNpc dynamicNpc && dynamicNpc.IsGoal()))
            {
                int exp = lifeLost;
                long battleExp = owner.AdjustExperience(target, lifeLost, false);

                if (!target.IsAlive && !magicRecruit)
                {
                    var nBonusExp = (int)(target.MaxLife * (5 / 100));
                    battleExp += nBonusExp;
                    if (!owner.Map.IsTrainingMap() && nBonusExp > 0)
                    {
                        await owner.SendAsync(string.Format(Language.StrKillingExperience, nBonusExp));
                    }
                }

                await AwardExpAsync(0, (int)battleExp, exp, magic);
            }

            return true;
        }

        public async Task<bool> AwardExpAsync(int type, long battleExp, long experience, Magic magic = null)
        {
            if (magic == null)
            {
                return await AwardExpAsync(battleExp, experience, true, QueryMagic);
            }

            return await AwardExpAsync(battleExp, experience, true, magic);
        }

        public async Task<bool> AwardExpAsync(long battleExp, long experience, bool ignoreFlag, Magic magic = null)
        {
            if (battleExp <= 0 && experience == 0)
            {
                return false;
            }

            magic ??= QueryMagic;

            if (role.Map.IsTrainingMap())
            {
                if (battleExp > 0)
                {
                    if (role.IsBowman)
                    {
                        battleExp /= 2;
                    }

                    battleExp = Calculations.CutTrail(1, Calculations.MulDiv(battleExp, 10, 100));
                }
            }

            if (battleExp > 0 && role is Character user)
            {
                await user.AwardBattleExpAsync(battleExp, true);
            }

            if (magic == null)
            {
                return false;
            }

            if (!CheckAwardExpEnable(magic))
            {
                return false;
            }

            if (role.Map.Identity == TC_PK_ARENA_ID)
            {
                return true;
            }

            if (role.Map.IsTrainingMap() && magic.AutoActive == 0 && autoAttackCount > 0 &&
                autoAttackCount % 10 != 0)
            {
                return true;
            }

            if (magic.NeedExp > 0
                && ((int)magic.AutoActive & 16) == 0
                || ignoreFlag)
            {
                if (role is Character owner)
                {
                    experience = (int)(experience * (1 + owner.MoonGemBonus / 100d));
                }

                magic.Experience += (uint)experience;

                //if ((pMagic.AutoActive & 8) == 0)
                await magic.SendAsync();

                await UpLevelMagicAsync(true, magic);
                await magic.SaveAsync();
                return true;
            }

            if (magic.NeedExp == 0
                && magic.Target == 4)
            {
                if (role is Character owner)
                {
                    experience = (int)(experience * (1 + owner.MoonGemBonus / 100d));
                }

                magic.Experience += (uint)experience;

                //if ((pMagic.AutoActive & 8) == 0)
                await magic.SendAsync();
                await UpLevelMagicAsync(true, magic);

                await magic.SaveAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> UpLevelMagicAsync(bool synchro, Magic magic)
        {
            if (magic == null)
            {
                return false;
            }

            int needExp = magic.NeedExp;

            if (!(needExp > 0
                  && (magic.Experience >= needExp
                      || magic.OldLevel > 0
                      && magic.Level >= magic.OldLevel / 2
                      && magic.Level < magic.OldLevel)))
            {
                return false;
            }

            var newLevel = (ushort)(magic.Level + 1);
            magic.Experience = 0;
            await magic.ChangeLevelAsync((byte)newLevel);
            if (synchro)
            {
                await magic.SendAsync();
            }

            return true;
        }

        public async Task<bool> UpLevelByTaskAsync(ushort type)
        {
            Magic magic;
            if (!magics.TryGetValue(type, out magic))
            {
                return false;
            }

            var newLevel = (byte)(magic.Level + 1);
            if (!await magic.ChangeLevelAsync(newLevel))
            {
                return false;
            }

            magic.Experience = 0;
            await magic.SendAsync();
            await magic.SaveAsync();
            return true;
        }

        public bool CheckAwardExpEnable(Magic magic)
        {
            if (magic == null)
            {
                return false;
            }

            return role.Level >= magic.NeedLevel
                   && magic.NeedExp > 0
                   && role.MapIdentity != 1005;
        }

        public bool CheckType(ushort type)
        {
            return magics.TryGetValue(type, out var magic) && !magic.Unlearn;
        }

        public bool CheckLevel(ushort type, ushort level)
        {
            return magics.TryGetValue(type, out var magic) && magic.Level == level && !magic.Unlearn;
        }

        public async Task<bool> ResetSkillAsync(ushort type)
        {
            Magic magic = this[type];
            if (magic == null)
            {
                return false;
            }

            magic.OldLevel = magic.Level;
            await magic.ChangeLevelAsync(0);
            magic.Experience = 0;
            magic.Unlearn = false;
            await magic.SaveAsync();
            await magic.SendAsync();
            return true;
        }

        public async Task<bool> UnlearnMagicAsync(ushort type, bool drop)
        {
            Magic magic = this[type];
            if (magic == null)
            {
                return false;
            }

            if (drop)
            {
                magics.TryRemove(type, out _);
                await magic.DeleteAsync();
            }
            else
            {
                magic.OldLevel = magic.Level;
                await magic.ChangeLevelAsync(0);
                magic.Experience = 0;
                magic.Unlearn = true;
                await magic.SaveAsync();
            }

            await role.SendAsync(new MsgAction
            {
                Identity = role.Identity,
                Command = type,
                Action = ActionType.SpellRemove
            });
            return true;
        }

        #endregion

        #region Query Magic

        public Magic QueryMagic => magics.TryGetValue(useMagicType, out Magic magic) ? magic : null;

        public Magic this[ushort nType] => magics.TryGetValue(nType, out Magic ret) ? ret : null;

        #endregion

        [Flags]
        public enum AutoActive
        {
            None = 0,
            Kill = 0x1,
            OnAttack = 0x4,
            OnBeAttack = 0x8,
            AfterSkill = 0x40
        }

        public enum MagicSort
        {
            Attack = 1,
            Recruit = 2, // support auto active.
            Cross = 3,
            Fan = 4, // support auto active(random).
            Bomb = 5,
            Attachstatus = 6,
            Detachstatus = 7,
            Square = 8,
            Jumpattack = 9,   // move, a-lock
            Randomtrans = 10, // move, a-lock
            Dispatchxp = 11,
            Collide = 12,   // move, a-lock & b-synchro
            Serialcut = 13, // auto active only.
            Line = 14,      // support auto active(random).
            Atkrange = 15,  // auto active only, forever active.
            Atkstatus = 16, // support auto active, random active.
            Callteammember = 17,
            Recordtransspell = 18,
            Transform = 19,
            Addmana = 20, // support self target only.
            Laytrap = 21,
            Dance = 22,       // ÌøÎè(only use for client)
            Callpet = 23,     // ÕÙ»½ÊÞ
            Vampire = 24,     // ÎüÑª£¬power is percent award. use for call pet
            Instead = 25,     // ÌæÉí. use for call pet
            Declife = 26,     // ¿ÛÑª(µ±Ç°ÑªµÄ±ÈÀý)
            Groundsting = 27, // µØ´Ì,
            Vortex = 28,
            Activateswitch = 29,
            Spook = 30,
            Warcry = 31,
            Riding = 32,
            AttachstatusArea = 34,
            FanStatus = 35, // fuck tq i dont know what name to use _|_
            BombStatus = 36,
            ChainXp = 37,
            Knockback = 38,
            Dashwhirl = 40,
            Perseverance = 41,
            Selfdetach = 46,
            Detachbadstatus = 47,
            CloseLine = 48,
            Compassion = 50,
            Teamflag = 51,
            Increaseblock = 52,
            Oblivion = 53,
            Stunbomb = 54,
            Tripleattack = 55,
            ScurvyBomb,
            CannonBarrage,
            BlackSpot,
            AdrenalineRush = 59,
            GaleBomb,
            Dashdeadmark = 61,
            KrakensRevenge,
            BlackbeardsRage,
            Mountwhirl = 64,
            Targetdrag = 65,
            KineticSpark = 67,
            Assassinvortex = 68,
            Blisteringwave = 69,
            BreathFocus = 70,
            FatalCross = 71,
            FatalSpin = 73
        }

        public const int MAX_TARGET_NUM = 25;
        public const int TC_PK_ARENA_ID = 1005;
        public const int PURE_TROJAN_ID = 10315;
        public const int PURE_WARRIOR_ID = 10311;
        public const int PURE_ARCHER_ID = 10313;
        public const int PURE_NINJA_ID = 6003;
        public const int PURE_MONK_ID = 10405;
        public const int PURE_PIRATE_ID = 11040;
        public const int PURE_WATER_ID = 30000;
        public const int PURE_FIRE_ID = 10310;

        public const int MAGICDAMAGE_ALT = 26;
        public const int AUTOLEVELUP_EXP = -1;
        public const int DISABLELEVELUP_EXP = 0;
        public const int AUTOMAGICLEVEL_PER_USERLEVEL = 10;
        public const int USERLEVELS_PER_MAGICLEVEL = 10;

        public const int KILLBONUS_PERCENT = 5;
        public const int HAVETUTORBONUS_PERCENT = 10;
        public const int WITHTUTORBONUS_PERCENT = 20;

        public const int MAGIC_DELAY = 1000; // DELAY
        public const int MAGIC_DELAY_MIN = 500; // DELAY
        public const int MAGIC_DECDELAY_PER_LEVEL = 100;
        public const int RANDOMTRANS_TRY_TIMES = 10;
        public const int DISPATCHXP_NUMBER = 20;
        public const int COLLIDE_POWER_PERCENT = 80;
        public const int COLLIDE_SHIELD_DURABILITY = 3;
        public const int LINE_WEAPON_DURABILITY = 2;
        public const int MAX_SERIALCUTSIZE = 10;
        public const int AWARDEXP_BY_TIMES = 1;
        public const int AUTO_MAGIC_DELAY_PERCENT = 150;
        public const int BOW_SUBTYPE = 500;
        public const ushort POISON_MAGIC_TYPE = 10010;
        public const int DEFAULT_MAGIC_FAN = 120;
        public const int STUDENTBONUS_PERCENT = 5;

        public const int MAGIC_KO_LIFE_PERCENT = 15;
        public const int MAGIC_ESCAPE_LIFE_PERCENT = 15;
    }
}
