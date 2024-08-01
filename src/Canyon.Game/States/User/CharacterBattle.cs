using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Events;
using Canyon.Game.States.Items;
using Canyon.Game.States.Magics;
using Canyon.Game.States.NeiGong;
using Canyon.Game.States.NPCs;
using Canyon.Game.States.World;
using Canyon.Shared.Mathematics;
using Canyon.World.Enums;
using System.Collections.Concurrent;
using System.Drawing;
using static Canyon.Game.Sockets.Game.Packets.MsgAction;
using static Canyon.Game.Sockets.Game.Packets.MsgInteract;
using static Canyon.Game.States.Magics.Magic;

namespace Canyon.Game.States.User
{
    public partial class Character
    {
        public int TotemBattlePower => Syndicate?.GetSharedBattlePower(SyndicateRank) ?? 0;

        public Item Headgear => UserPackage[Item.ItemPosition.Headwear];
        public Item Necklace => UserPackage[Item.ItemPosition.Necklace];
        public Item Ring => UserPackage[Item.ItemPosition.Ring];
        public Item RightHand => UserPackage[Item.ItemPosition.RightHand];
        public Item LeftHand => UserPackage[Item.ItemPosition.LeftHand];
        public Item Armor => UserPackage[Item.ItemPosition.Armor];
        public Item Boots => UserPackage[Item.ItemPosition.Boots];
        public Item Garment => UserPackage[Item.ItemPosition.Garment];
        public Item Mount => UserPackage[Item.ItemPosition.Steed];
        public Item Wings => UserPackage[Item.ItemPosition.Wing];

        public override bool IsBowman => RightHand?.IsBow() == true;
        public bool IsAssassin => RightHand?.IsAssassinKnife() == true;

        #region Attributes

        public override int BattlePower
        {
            get
            {
                int result = Level + Math.Min((byte)2, Metempsychosis) * 5 + (int)NobilityRank;
                result += TotemBattlePower;
                result += Math.Max(FamilyBattlePower, Guide?.SharedBattlePower ?? 0);
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.BattlePower ?? 0;
                }
                return result;
            }
        }

        public int PureBattlePower
        {
            get
            {
                int result = Level + Math.Min((byte)2, Metempsychosis) * 5 + (int)NobilityRank;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.BattlePower ?? 0;
                }
                return result;
            }
        }

        public override int MinAttack
        {
            get
            {
                if (Transformation != null)
                {
                    return Transformation.MinAttack;
                }

                int result = Strength;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                    {
                        continue;
                    }

                    if (pos == Item.ItemPosition.LeftHand)
                    {
                        result += (UserPackage[pos]?.MinAttack ?? 0) / 2;
                    }
                    else
                    {
                        result += UserPackage[pos]?.MinAttack ?? 0;
                    }
                }
                result += AstProf.GetPower(AstProfType.Performer);
                result += Fate.Attack;
                result += JiangHu.Attack;
                result += InnerStrength.Attack;
                return result;
            }
        }

        public override int MaxAttack
        {
            get
            {
                if (Transformation != null)
                {
                    return Transformation.MaxAttack;
                }

                int result = Strength;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                    {
                        continue;
                    }

                    if (pos == Item.ItemPosition.LeftHand)
                    {
                        result += (UserPackage[pos]?.MaxAttack ?? 0) / 2;
                    }
                    else
                    {
                        result += UserPackage[pos]?.MaxAttack ?? 0;
                    }
                }

                result += AstProf.GetPower(AstProfType.Performer);
                result += Fate.Attack;
                result += JiangHu.Attack;
                result += InnerStrength.Attack;
                return result;
            }
        }

        public override int MagicAttack
        {
            get
            {
                if (Transformation != null)
                {
                    return Transformation.MaxAttack;
                }

                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                    {
                        continue;
                    }

                    result += UserPackage[pos]?.MagicAttack ?? 0;
                }

                result += AstProf.GetPower(AstProfType.Performer);
                result += Fate.MagicAttack;
                result += JiangHu.MagicAttack;
                result += InnerStrength.MagicAttack;
                return result;
            }
        }

        public override int Defense
        {
            get
            {
                if (Transformation != null)
                {
                    return Transformation.Defense;
                }

                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                    {
                        continue;
                    }

                    result += UserPackage[pos]?.Defense ?? 0;
                }
                result += JiangHu.Defense;
                result += InnerStrength.Defense;
                return result;
            }
        }

        public override int MagicDefense
        {
            get
            {
                if (Transformation != null)
                {
                    return Transformation.MagicDefense;
                }

                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                    {
                        continue;
                    }

                    result += UserPackage[pos]?.MagicDefense ?? 0;
                }

                result += Fate.MagicDefense;
                result += JiangHu.MagicDefense;
                result += InnerStrength.MagicDefense;
                return result;
            }
        }

        public override int MagicDefenseBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.MagicDefenseBonus ?? 0;
                }
                return result;
            }
        }

        public override int Dodge
        {
            get
            {
                if (Transformation != null)
                {
                    return (int)Transformation.Dodge;
                }

                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.Dodge ?? 0;
                }
                return result;
            }
        }

        public override int Blessing
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.Blessing ?? 0;
                }
                return result;
            }
        }

        public override int AddFinalAttack
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin;
                    pos <= Item.ItemPosition.EquipmentEnd;
                    pos++)
                {
                    result += UserPackage[pos]?.AddFinalDamage ?? 0;
                }

                result += Fate.FinalDamage;
                result += JiangHu.FinalDamage;
                result += InnerStrength.FinalPhysicalDamage;

                IStatus status = QueryStatus(StatusSet.BUFF_FINAL_PDAMAGE + 1);
                if (status != null)
                {
                    result += status.Power;
                }

                return result;
            }
        }

        public override int AddFinalMAttack
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin;
                    pos <= Item.ItemPosition.EquipmentEnd;
                    pos++)
                {
                    result += UserPackage[pos]?.AddFinalMagicDamage ?? 0;
                }

                result += Fate.FinalMagicDamage;
                result += JiangHu.FinalMagicDamage;
                result += InnerStrength.FinalMagicalDamage;

                IStatus status = QueryStatus(StatusSet.BUFF_FINAL_MDAMAGE + 1);
                if (status != null)
                {
                    result += status.Power;
                }

                return result;
            }
        }

        public override int AddFinalDefense
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin;
                    pos <= Item.ItemPosition.EquipmentEnd;
                    pos++)
                {
                    result += UserPackage[pos]?.AddFinalDefense ?? 0;
                }

                result += Fate.FinalDefense;
                result += JiangHu.FinalDefense;
                result += InnerStrength.FinalPhysicalDefense;

                IStatus status = QueryStatus(StatusSet.BUFF_FINAL_PDMGREDUCTION + 1);
                if (status != null)
                {
                    result += status.Power;
                }

                return result;
            }
        }

        public override int AddFinalMDefense
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.AddFinalMagicDefense ?? 0;
                }
                result += Fate.FinalMagicDefense;
                result += JiangHu.FinalMagicDefense;
                result += InnerStrength.FinalMagicalDefense;

                IStatus status = QueryStatus(StatusSet.BUFF_FINAL_MDMGREDUCTION + 1);
                if (status != null)
                {
                    result += status.Power;
                }

                return result;
            }
        }

        public override int CriticalStrike
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.CriticalStrike ?? 0;
                }
                result += AstProf.GetPower(AstProfType.MartialArtist);
                result += Fate.CriticalStrike * 10;
                result += JiangHu.CriticalStrike * 10;
                result += InnerStrength.CriticalStrike;
                var tyrantAura = QueryStatus(StatusSet.TYRANT_AURA);
                if (tyrantAura != null)
                {
                    result += tyrantAura.Power * 100;
                }
                IStatus status = QueryStatus(StatusSet.BUFF_PSTRIKE + 1);
                if (status != null)
                {
                    result += status.Power;
                }
                return result;
            }
        }

        public override int SkillCriticalStrike

        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.SkillCriticalStrike ?? 0;
                }
                result += AstProf.GetPower(AstProfType.Warlock);
                result += Fate.SkillCriticalStrike * 10;
                result += JiangHu.SkillCriticalStrike * 10;
                result += InnerStrength.SkillCriticalStrike;
                var tyrantAura = QueryStatus(StatusSet.TYRANT_AURA);
                if (tyrantAura != null)
                {
                    result += tyrantAura.Power * 100;
                }
                IStatus status = QueryStatus(StatusSet.BUFF_MSTRIKE + 1);
                if (status != null)
                {
                    result += status.Power;
                }
                return result;
            }
        }

        public override int Immunity
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.Immunity ?? 0;
                }
                result += AstProf.GetPower(AstProfType.ChiMaster);
                result += Fate.Immunity * 10;
                result += JiangHu.Immunity * 10;
                result += InnerStrength.Immunity;
                var fendAura = QueryStatus(StatusSet.FEND_AURA);
                if (fendAura != null)
                {
                    result += fendAura.Power * 100;
                }
                IStatus status = QueryStatus(StatusSet.BUFF_IMMUNITY + 1);
                if (status != null)
                {
                    result += status.Power;
                }
                return result;
            }
        }

        public override int Penetration
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.Penetration ?? 0;
                }
                result += AstProf.GetPower(AstProfType.Sage);
                return result;
            }
        }

        public override int Breakthrough
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.Breakthrough ?? 0;
                }
                result += Fate.Breakthrough;
                result += JiangHu.Breakthrough;
                result += InnerStrength.Breakthrough;
                IStatus status = QueryStatus(StatusSet.BUFF_BREAK + 1);
                if (status != null)
                {
                    result += status.Power;
                }
                return result;
            }
        }

        public override int Counteraction
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.Counteraction ?? 0;
                }
                result += Fate.Counteraction;
                result += JiangHu.Counteraction;
                IStatus status = QueryStatus(StatusSet.BUFF_COUNTERACTION + 1);
                if (status != null)
                {
                    result += status.Power;
                }
                return result;
            }
        }

        public override int Block
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.Block ?? 0;
                }
                return result;
            }
        }

        public override int Detoxication
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.Detoxication ?? 0;
                }
                result += AstProf.GetPower(AstProfType.Apothecary);
                return Math.Min(100, Math.Max(0, result));
            }
        }

        public override int FireResistance
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.FireResistance ?? 0;
                }
                var aura = QueryStatus(StatusSet.FIRE_AURA);
                if (aura != null)
                {
                    result += aura.Power;
                }
                return result;
            }
        }

        public override int WaterResistance
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.WaterResistance ?? 0;
                }
                var aura = QueryStatus(StatusSet.WATER_AURA);
                if (aura != null)
                {
                    result += aura.Power;
                }
                return result;
            }
        }

        public override int WoodResistance
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.WoodResistance ?? 0;
                }
                var aura = QueryStatus(StatusSet.WOOD_AURA);
                if (aura != null)
                {
                    result += aura.Power;
                }
                return result;
            }
        }

        public override int EarthResistance
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.EarthResistance ?? 0;
                }
                var aura = QueryStatus(StatusSet.EARTH_AURA);
                if (aura != null)
                {
                    result += aura.Power;
                }
                return result;
            }
        }

        public override int MetalResistance
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.MetalResistance ?? 0;
                }
                var aura = QueryStatus(StatusSet.METAL_AURA);
                if (aura != null)
                {
                    result += aura.Power;
                }
                return result;
            }
        }

        public override int AttackSpeed { get; } = 1000;

        public int Agility
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.Agility ?? 0;
                }
                return result;
            }
        }

        public override int Accuracy
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.Accuracy ?? 0;
                }
                return result;
            }
        }

        public int DragonGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    Item item = UserPackage[pos];
                    if (item != null)
                    {
                        result += item.DragonGemEffect;
                    }
                }
                return result;
            }
        }

        public int PhoenixGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.PhoenixGemEffect ?? 0;
                }
                return result;
            }
        }

        public int VioletGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.VioletGemEffect ?? 0;
                }
                return result;
            }
        }

        public int MoonGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.MoonGemEffect ?? 0;
                }
                return result;
            }
        }

        public int RainbowGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.RainbowGemEffect ?? 0;
                }
                return result;
            }
        }

        public int FuryGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.FuryGemEffect ?? 0;
                }
                return result;
            }
        }

        public int TortoiseGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.TortoiseGemEffect ?? 0;
                }
                return Math.Min(48, result);
            }
        }

        #endregion

        #region AstProf (Sub-classes)

        public ulong AstProfRanks
        {
            get => character.AstProfRank;
            set => character.AstProfRank = value;
        }

        public AstProfType AstProfType
        {
            get => (AstProfType)character.AstProfCurrent;
            set => character.AstProfCurrent = (byte)value;
        }

        public SubClass AstProf { get; set; }

        #endregion

        #region Fate (Chi)

        public Fate Fate { get; set; }

        #endregion

        #region Jiang Hu

        public JiangHu JiangHu { get; set; }

        #endregion

        #region Transformation

        private TimeOut transformationTimer = new();

        public Transformation Transformation { get; protected set; }

        public async Task<bool> TransformAsync(uint dwLook, int nKeepSecs, bool bSynchro)
        {
            var bBack = false;

            if (Transformation != null)
            {
                await ClearTransformationAsync();
                bBack = true;
            }

            DbMonstertype pType = RoleManager.GetMonstertype(dwLook);
            if (pType == null)
            {
                return false;
            }

            var pTransform = new Transformation(this);
            if (pTransform.Create(pType))
            {
                Transformation = pTransform;
                TransformationMesh = (ushort)pTransform.Lookface;
                await SetAttributesAsync(ClientUpdateType.Mesh, Mesh);
                Life = MaxLife;
                transformationTimer = new TimeOut(nKeepSecs);
                transformationTimer.Startup(nKeepSecs);
                if (bSynchro)
                {
                    await SynchroTransformAsync();
                }
            }
            else
            {
                pTransform = null;
            }

            if (bBack)
            {
                await SynchroTransformAsync();
            }

            return true;
        }

        public async Task ClearTransformationAsync()
        {
            TransformationMesh = 0;
            Transformation = null;
            transformationTimer.Clear();

            await SynchroTransformAsync();
            await MagicData.AbortMagicAsync(true);
            BattleSystem.ResetBattle();
        }

        public async Task<bool> SynchroTransformAsync()
        {
            await SynchroAttributesAsync(ClientUpdateType.Mesh, Mesh, true);
            if (TransformationMesh != 98 && TransformationMesh != 99 && IsAlive)
            {
                Life = MaxLife;
                await SynchroAttributesAsync(ClientUpdateType.MaxHitpoints, MaxLife);
                await SynchroAttributesAsync(ClientUpdateType.Hitpoints, Life, false);
            }
            return true;
        }

        public async Task SetGhostAsync()
        {
            if (IsAlive)
            {
                return;
            }

            ushort trans = 98;
            if (Gender == 2)
            {
                trans = 99;
            }

            TransformationMesh = trans;
            await SynchroTransformAsync();
        }

        #endregion

        #region Battle

        private readonly TimeOut ghostTimer = new();

        public override int AdjustWeaponDamage(int damage, Role attacker)
        {
            if (attacker is Character attackerTarget)
            {
                int defense2 = Calculations.DEFAULT_DEFENCE2;
                if (Metempsychosis >= 2 && attackerTarget.Metempsychosis < 2)
                {
                    defense2 = 5000;
                }
                else if (Metempsychosis >= 2 && ProfessionLevel >= 3)
                {
                    defense2 = 7000;
                }
                return Calculations.MulDiv(damage, defense2, Calculations.DEFAULT_DEFENCE2);
            }
            return damage;
        }

        public override bool SetAttackTarget(Role target)
        {
            if (target == null)
            {
                BattleSystem.ResetBattle();
                return false;
            }

            if (!target.IsAttackable(this))
            {
                BattleSystem.ResetBattle();
                return false;
            }

            if (target.IsWing && !IsWing && !IsBowman)
            {
                BattleSystem.ResetBattle();
                return false;
            }

            if (IsBowman && !IsArrowPass(target.X, target.Y, 60))
            {
                return false;
            }

            if (QueryStatus(StatusSet.FATAL_STRIKE) != null)
            {
                if (GetDistance(target) > Screen.VIEW_SIZE)
                {
                    return false;
                }
            }
            else
            {
                if (GetDistance(target) > GetAttackRange(target.SizeAddition))
                {
                    BattleSystem.ResetBattle();
                    return false;
                }
            }

            var currentEvent = GetCurrentEvent();
            if (currentEvent != null && !currentEvent.IsAttackEnable(this))
            {
                return false;
            }
            return true;
        }

        public int GetInterAtkRate()
        {
            int nRate = USER_ATTACK_SPEED;
            int nRateR = 0, nRateL = 0;

            if (UserPackage[Item.ItemPosition.RightHand] != null)
            {
                nRateR = UserPackage[Item.ItemPosition.RightHand].Itemtype.AtkSpeed;
            }

            if (UserPackage[Item.ItemPosition.LeftHand] != null &&
                !UserPackage[Item.ItemPosition.LeftHand].IsArrowSort())
            {
                nRateL = UserPackage[Item.ItemPosition.LeftHand].Itemtype.AtkSpeed;
            }

            if (nRateR > 0 && nRateL > 0)
            {
                nRate = (nRateR + nRateL) / 2;
            }
            else if (nRateR > 0)
            {
                nRate = nRateR;
            }
            else if (nRateL > 0)
            {
                nRate = nRateL;
            }

            if (QueryStatus(StatusSet.CYCLONE) != null)
            {
                nRate = Calculations.CutTrail(0, Calculations.AdjustData(nRate, QueryStatus(StatusSet.CYCLONE).Power));
            }
            else if (QueryStatus(StatusSet.SUPER_CYCLONE) != null)
            {
                nRate = Calculations.CutTrail(0, Calculations.AdjustData(nRate, QueryStatus(StatusSet.SUPER_CYCLONE).Power));
            }

            return Math.Max(400, nRate);
        }

        public override int GetAttackRange(int sizeAdd)
        {
            int range = 1, rangeLeft = 0, rangeRight = 0;
            if (UserPackage[Item.ItemPosition.RightHand] != null && UserPackage[Item.ItemPosition.RightHand].IsWeapon())
            {
                rangeRight = UserPackage[Item.ItemPosition.RightHand].AttackRange;
            }

            if (UserPackage[Item.ItemPosition.LeftHand] != null && UserPackage[Item.ItemPosition.LeftHand].IsWeapon())
            {
                rangeLeft = UserPackage[Item.ItemPosition.LeftHand].AttackRange;
            }

            if (rangeRight > 0 && rangeLeft > 0)
            {
                range = (rangeRight + rangeLeft) / 2;
            }
            else if (rangeRight > 0)
            {
                range = rangeRight;
            }
            else if (rangeLeft > 0)
            {
                range = rangeLeft;
            }

            range += (SizeAddition + sizeAdd + 1) / 2 + 1;
            if (IsBowman)
            {
                range += 3;
            }
            return range;
        }

        public async Task<bool> AutoSkillAttackAsync(Role target)
        {
            if (Transformation != null)
            {
                return false;
            }

            foreach (Magic magic in MagicData.Magics.Values.Where(x => x.AutoActive.HasFlag(MagicData.AutoActive.OnAttack)))
            {
                double percent = magic.Percent;
                if (magic.Sort == MagicData.MagicSort.Tripleattack && target is Character)
                {
                    percent = 33.33d;
                }
                if ((magic.WeaponSubtype == 0
                        || CheckWeaponSubType(magic.WeaponSubtype, magic.UseItemNum))
                    && await ChanceCalcAsync(percent))
                {
                    return await ProcessMagicAttackAsync(magic.Type, target.Identity, target.X, target.Y,
                                                         (uint)MagicData.AutoActive.OnAttack);
                }
            }

            return false;
        }

        public void AddSynWarScore(DynamicNpc npc, int score)
        {
            if (npc == null || score == 0)
            {
                return;
            }

            if (Syndicate == null || npc.OwnerIdentity == SyndicateIdentity)
            {
                return;
            }

            npc.AddSynWarScore(Syndicate, score);
            return;
        }

        public async Task SendGemEffectAsync()
        {
            var setGem = new List<Item.SocketGem>();

            for (var pos = Item.ItemPosition.EquipmentBegin; pos < Item.ItemPosition.EquipmentEnd; pos++)
            {
                Item item = UserPackage[pos];
                if (item == null)
                {
                    continue;
                }

                setGem.Add(item.SocketOne);
                if (item.SocketTwo != Item.SocketGem.NoSocket)
                {
                    setGem.Add(item.SocketTwo);
                }
            }

            int nGems = setGem.Count;
            if (nGems <= 0)
            {
                return;
            }

            var strEffect = "";
            switch (setGem[await NextAsync(0, nGems)])
            {
                case Item.SocketGem.SuperPhoenixGem:
                    strEffect = "phoenix";
                    break;
                case Item.SocketGem.SuperDragonGem:
                    strEffect = "goldendragon";
                    break;
                case Item.SocketGem.SuperFuryGem:
                    strEffect = "fastflash";
                    break;
                case Item.SocketGem.SuperRainbowGem:
                    strEffect = "rainbow";
                    break;
                case Item.SocketGem.SuperKylinGem:
                    strEffect = "goldenkylin";
                    break;
                case Item.SocketGem.SuperVioletGem:
                    strEffect = "purpleray";
                    break;
                case Item.SocketGem.SuperMoonGem:
                    strEffect = "moon";
                    break;
            }

            await SendEffectAsync(strEffect, true);
        }

        public async Task SendWeaponMagic2Async(Role pTarget = null)
        {
            Item item = null;

            if (UserPackage[Item.ItemPosition.RightHand] != null &&
                UserPackage[Item.ItemPosition.RightHand].Effect != Item.ItemEffect.None)
            {
                item = UserPackage[Item.ItemPosition.RightHand];
            }

            if (UserPackage[Item.ItemPosition.LeftHand] != null &&
                UserPackage[Item.ItemPosition.LeftHand].Effect != Item.ItemEffect.None)
            {
                if (item != null && await ChanceCalcAsync(50f) || item == null)
                {
                    item = UserPackage[Item.ItemPosition.LeftHand];
                }
            }

            if (item != null)
            {
                switch (item.Effect)
                {
                    case Item.ItemEffect.Life:
                        {
                            if (!await ChanceCalcAsync(20))
                            {
                                return;
                            }

                            await AddAttributesAsync(ClientUpdateType.Hitpoints, 310);
                            var msg = new MsgMagicEffect
                            {
                                AttackerIdentity = Identity,
                                MagicIdentity = 1005
                            };
                            msg.Append(Identity, 310, false);
                            await BroadcastRoomMsgAsync(msg, true);
                            break;
                        }

                    case Item.ItemEffect.Mana:
                        {
                            if (!await ChanceCalcAsync(20))
                            {
                                return;
                            }

                            await AddAttributesAsync(ClientUpdateType.Mana, 310);
                            var msg = new MsgMagicEffect
                            {
                                AttackerIdentity = Identity,
                                MagicIdentity = 1195
                            };
                            msg.Append(Identity, 310, false);
                            await BroadcastRoomMsgAsync(msg, true);
                            break;
                        }

                    case Item.ItemEffect.Poison:
                        {
                            if (pTarget == null)
                            {
                                return;
                            }

                            if (!await ChanceCalcAsync(7f))
                            {
                                return;
                            }

                            var msg = new MsgMagicEffect
                            {
                                AttackerIdentity = Identity,
                                MagicIdentity = 1320
                            };
                            msg.Append(pTarget.Identity, 310, true);
                            await BroadcastRoomMsgAsync(msg, true);

                            await pTarget.AttachStatusAsync(this, StatusSet.POISONED, 310, POISONDAMAGE_INTERVAL, 20);

                            var result = await AttackAsync(pTarget);
                            int nTargetLifeLost = result.Damage;

                            await SendDamageMsgAsync(pTarget.Identity, nTargetLifeLost, InteractionEffect.None);

                            if (!pTarget.IsAlive)
                            {
                                var dwDieWay = 1;
                                if (nTargetLifeLost > pTarget.MaxLife / 3)
                                {
                                    dwDieWay = 2;
                                }

                                await KillAsync(pTarget, IsBowman ? 5 : (uint)dwDieWay);
                            }

                            break;
                        }
                }
            }
        }

        public async Task SendGemEffect2Async()
        {
            var setGem = new List<int>();

            for (var pos = Item.ItemPosition.EquipmentBegin; pos < Item.ItemPosition.EquipmentEnd; pos++)
            {
                Item item = UserPackage[pos];
                if (item == null)
                {
                    continue;
                }

                if (item.Blessing > 0)
                {
                    setGem.Add(item.Blessing);
                }
            }

            int nGems = setGem.Count;
            if (nGems <= 0)
            {
                return;
            }

            var strEffect = "";
            switch (setGem[await NextAsync(0, nGems)])
            {
                case 1:
                    strEffect = "Aegis1";
                    break;
                case 3:
                    strEffect = "Aegis2";
                    break;
                case 5:
                    strEffect = "Aegis3";
                    break;
                case 7:
                    strEffect = "Aegis4";
                    break;
            }

            await SendEffectAsync(strEffect, true);
        }

        public async Task<long> CalcExpLostOfDeathAsync(Role killer)
        {
            if (killer is not Character)
            {
                return 0;
            }

            var param = 50;
            if (QueryStatus(StatusSet.RED_NAME) != null)
            {
                param = 20;
            }
            else if (QueryStatus(StatusSet.BLACK_NAME) != null)
            {
                param = 10;
            }

            var expLost = (int)((long)Experience / param);

            if (SyndicateIdentity != 0)
            {
                const int moneyCostPerExp = 100;

                var decPercent = 0;
                var expPayBySyn = 0;
                if (Syndicate.Money > 0)
                {
                    int fundLost = Calculations.MulDiv(expLost, decPercent, 100 * moneyCostPerExp);
                    if (fundLost > Syndicate.Money)
                    {
                        fundLost = (int)Syndicate.Money;
                    }

                    Syndicate.Money -= fundLost;

                    expPayBySyn = fundLost * moneyCostPerExp;
                    expLost -= expPayBySyn;
                }

                if (expPayBySyn > 0)
                {
                    await SendAsync(string.Format(StrExpLostBySynFund, expPayBySyn));
                }
            }

            if (expLost > 0 && VipLevel >= 3)
            {
                expLost /= 2;
            }

            return Math.Max(0, expLost);
        }

        public override bool IsImmunity(Role target)
        {
            if (base.IsImmunity(target))
            {
                return true;
            }

            if (IsArenicWitness())
            {
                return true;
            }

            if (target is Character targetUser)
            {
                switch (PkMode)
                {
                    case PkModeType.Capture:
                        {
                            return !targetUser.IsEvil();
                        }

                    case PkModeType.Peace:
                        {
                            return true;
                        }

                    case PkModeType.FreePk:
                        {
                            return false;
                        }

                    case PkModeType.Team:
                        {
                            if (IsFriend(targetUser.Identity))
                            {
                                return true;
                            }

                            if (Syndicate != null)
                            {
                                if (Syndicate.QueryMember(targetUser.Identity) != null)
                                {
                                    return true;
                                }

                                if (Syndicate.UserIsAlly(targetUser.Identity))
                                {
                                    return true;
                                }
                            }

                            if (Map.IsFamilyMap() && Family != null)
                            {
                                if (Family.GetMember(targetUser.Identity) != null)
                                {
                                    return true;
                                }

                                if (targetUser.Family != null && Family.IsAlly(targetUser.Family.Identity))
                                {
                                    return true;
                                }
                            }

                            if (Team?.IsMember(targetUser.Identity) == true)
                            {
                                return true;
                            }

                            return false;
                        }

                    case PkModeType.Revenge:
                        {
                            if (IsEnemy(targetUser.Identity))
                            {
                                return false;
                            }

                            return true;
                        }

                    case PkModeType.Syndicate:
                        {
                            if (Syndicate != null && Syndicate.IsEnemy(targetUser.SyndicateIdentity))
                            {
                                return false;
                            }

                            return true;
                        }

                    case PkModeType.JiangHu:
                        {
                            if (!targetUser.JiangHu.IsActive)
                            {
                                return true;
                            }

                            if (JiangPkImmunity.HasFlag(JiangPkMode.NotHitFriends) && IsFriend(targetUser.Identity))
                            {
                                return true;
                            }

                            if (Syndicate != null && targetUser.Syndicate != null)
                            {
                                if (JiangPkImmunity.HasFlag(JiangPkMode.NotHitGuildMembers) 
                                    && SyndicateIdentity == targetUser.SyndicateIdentity)
                                {
                                    return true;
                                }

                                if (JiangPkImmunity.HasFlag(JiangPkMode.NotHitAlliedGuild) 
                                    && Syndicate.IsAlly(targetUser.SyndicateIdentity))
                                {
                                    return true;
                                }
                            }

                            if (Family != null && targetUser.Family != null)
                            {
                                if (JiangPkImmunity.HasFlag(JiangPkMode.NotHitClanMembers)
                                    && FamilyIdentity == targetUser.FamilyIdentity)
                                {
                                    return true;
                                }

                                if (JiangPkImmunity.HasFlag(JiangPkMode.NoHitAlliesClan)
                                    && Family.IsAlly(targetUser.FamilyIdentity))
                                {
                                    return true;
                                }
                            }

                            return false;
                        }
                }
            }
            else if (target is Monster monster)
            {
                switch (PkMode)
                {
                    case PkModeType.Peace:
                        {
                            if (monster.IsGuard()
                                || monster.IsPkKiller()
                                || monster.IsCallPet())
                            {
                                return true;
                            }
                            return false;
                        }

                    case PkModeType.Team:
                        {
                            if (monster.IsGuard()
                                || monster.IsPkKiller())
                            {
                                return true;
                            }

                            if (monster.IsCallPet())
                            {
                                if (Team != null)
                                {
                                    return Team.IsMember(monster.OwnerIdentity);
                                }

                                if (Syndicate != null)
                                {
                                    return Syndicate.QueryMember(monster.OwnerIdentity) != null || Syndicate.UserIsAlly(monster.OwnerIdentity);
                                }
                            }
                            return false;
                        }

                    case PkModeType.Capture:
                    case PkModeType.Revenge:
                    case PkModeType.JiangHu:
                        {
                            if (monster.IsGuard()
                                || monster.IsPkKiller())
                            {
                                return true;
                            }

                            if (monster.IsCallPet())
                            {
                                Character owner = Map.QueryRole<Character>(monster.OwnerIdentity);
                                return owner != null && owner.IsCrime();
                            }

                            return false;
                        }
                    case PkModeType.FreePk:
                        {
                            return false;
                        }
                }
            }
            else if (target is DynamicNpc)
            {
                return false;
            }
            return true;
        }

        public override bool IsAttackable(Role attacker)
        {
            if (protectionTimer.IsActive() && !protectionTimer.IsTimeOut())
            {
                return false;
            }

            if (attacker.IsPlayer() && Map.IsPkDisable())
            {
                return false;
            }

            if (Map.QueryRegion(RegionType.PkProtected, X, Y) || Map.QueryRegion(RegionType.FlagProtection, X, Y))
            {
                return false;
            }

            var currentEvent = GetCurrentEvent();
            if (currentEvent != null && !currentEvent.IsAttackEnable(this))
            {
                return false;
            }

            if (attacker is Character atkUser)
            {
                if (!atkUser.IsArenicWitness() && IsArenicWitness())
                {
                    return false;
                }

                if (atkUser.IsArenicWitness())
                {
                    return false;
                }
            }

            return IsAlive;
        }

        public override async Task<BattleSystem.AttackResult> AttackAsync(Role target)
        {
            if (target == null)
            {
                return new(0, 0, InteractionEffect.None);
            }

            var currentEvent = GetCurrentEvent();
            if (currentEvent != null)
            {
                await currentEvent.OnAttackAsync(this);
            }
            return await BattleSystem.CalcPowerAsync(MagicType.None, this, target);
        }

        public override async Task<bool> BeAttackAsync(MagicType magicType, Role attacker, int power, bool reflectEnable)
        {
            if (attacker == null)
            {
                return false;
            }

            StopMining();

            if (IsLucky && await ChanceCalcAsync(1, 100))
            {
                await SendEffectAsync("LuckyGuy", true);
                power /= 10;
            }

            if ((PreviousProfession == 25 || FirstProfession == 25) && reflectEnable &&
                await ChanceCalcAsync(5, 100))
            {
                power = Math.Min(1700, power);

                await attacker.BeAttackAsync(magicType, this, power, false);
                await BroadcastRoomMsgAsync(new MsgInteract
                {
                    Action = MsgInteractType.ReflectMagic,
                    Data = power,
                    PosX = X,
                    PosY = Y,
                    SenderIdentity = Identity,
                    TargetIdentity = attacker.Identity
                }, true);

                if (!attacker.IsAlive)
                {
                    await attacker.BeKillAsync(null);
                }
                return true;
            }

            if (AwaitingProgressBar != null && !AwaitingProgressBar.Completed)
            {
                await GameAction.ExecuteActionAsync(AwaitingProgressBar.IdNextFail, this, null, null, string.Empty);
                AwaitingProgressBar = null;

                await SendAsync(new MsgAction
                {
                    Action = ActionType.ProgressBar,
                    Identity = Identity,
                    Command = 0,
                    Direction = 1,
                    MapColor = 0,
                    Strings = new List<string>
                    {
                        "Error"
                    }
                });
            }

            foreach (Magic magic in MagicData.Magics.Values.Where(x => x.AutoActive.HasFlag(MagicData.AutoActive.OnBeAttack)))
            {
                double percent = magic.Percent;
                if ((magic.WeaponSubtype == 0
                        || CheckWeaponSubType(magic.WeaponSubtype, magic.UseItemNum))
                    && await ChanceCalcAsync(percent))
                {
                    await ProcessMagicAttackAsync(magic.Type, attacker.Identity, attacker.X, attacker.Y, (uint)magic.AutoActive);
                }
            }

            IStatus azureShield = QueryStatus(StatusSet.AZURE_SHIELD);
            if (azureShield != null
                && reflectEnable)
            {
                int reduceShield = Math.Min(power, azureShield.Power);
                int newPower = Math.Max(0, azureShield.Power - power);
                power -= reduceShield;

                if (newPower > 0)
                {
                    await azureShield.ChangeDataAsync(newPower, azureShield.RemainingTime, azureShield.RemainingTimes);
                }
                else
                {
                    await DetachStatusAsync(StatusSet.AZURE_SHIELD);
                }

                await BroadcastRoomMsgAsync(new MsgInteract
                {
                    SenderIdentity = Identity,
                    TargetIdentity = Identity,
                    PosX = X,
                    PosY = Y,
                    Action = MsgInteractType.AzureDmg,
                    Data = reduceShield
                }, true);
            }

            var currentEvent = GetCurrentEvent();
            if (currentEvent != null)
            {
                await currentEvent.OnBeAttackAsync(attacker, this, (int)Math.Min(Life, power));
            }

            if (power > 0)
            {
                await AddAttributesAsync(ClientUpdateType.Hitpoints, power * -1);
                await BroadcastTeamLifeAsync();
            }

            if (IsAlive && await ChanceCalcAsync(5))
            {
                await SendGemEffect2Async();
            }

            if (!Map.IsTrainingMap() && await ChanceCalcAsync(5))
            {
                await DecEquipmentDurabilityAsync(true, (int)magicType, (ushort)(power > MaxLife / 4 ? 10 : 1));
            }

            if (Action == EntityAction.Sit)
            {
                await SetAttributesAsync(ClientUpdateType.Stamina, (ulong)(Energy / 2));
            }
            return true;
        }

        public override async Task KillAsync(Role target, uint dieWay)
        {
            if (target == null)
            {
                return;
            }


            var currentEvent = GetCurrentEvent();
            if (currentEvent != null)
            {
                await currentEvent.OnKillAsync(this, target, MagicData.QueryMagic);
            }

            if (target is Character targetUser)
            {
                await BroadcastRoomMsgAsync(new MsgInteract
                {
                    Action = MsgInteractType.Kill,
                    SenderIdentity = Identity,
                    TargetIdentity = target.Identity,
                    PosX = target.X,
                    PosY = target.Y,
                    Data = (int)dieWay
                }, true);

                bool jiangHuKill = PkMode == PkModeType.JiangHu && targetUser.JiangHu.IsActive;
                if (MagicData.QueryMagic != null && MagicData.QueryMagic.Sort != MagicData.MagicSort.Activateswitch)
                {
                    if (!jiangHuKill)
                    {
                        await ProcessPkAsync(targetUser);
                    }
                }

                if (jiangHuKill && JiangHu.Talent <= targetUser.JiangHu.Talent)
                {
                    await JiangHu.AwardTalentAsync();
                    await targetUser.JiangHu.SpendTalentAsync();
                }

                LuaScriptManager.Run(this, null, null, string.Empty, $"Event_Kill_User()");
                await GameAction.ExecuteActionAsync(USER_KILL_ACTION, this, target, null, string.Empty);
            }
            else if (target is Monster monster)
            {
                await AddXpAsync(1);

                if (QueryStatus(StatusSet.CYCLONE) != null || QueryStatus(StatusSet.SUPERMAN) != null)
                {
                    KoCount += 1;
                    IStatus status = QueryStatus(StatusSet.CYCLONE) ?? QueryStatus(StatusSet.SUPERMAN);
                    status?.IncTime(700, 30000);
                }

                if (QueryStatus(StatusSet.OBLIVION) != null)
                {
                    oblivionMonsterCount += 1;
                    if (oblivionMonsterCount >= OBLIVION_MAX_COUNT)
                    {
                        await DetachStatusAsync(StatusSet.OBLIVION);
                    }
                }

                //await KillMonsterAsync(monster.Type);
            }

            await target.BeKillAsync(this);
        }

        public override async Task BeKillAsync(Role attacker)
        {
            if (QueryStatus(StatusSet.GHOST) != null)
            {
                return;
            }

            BattleSystem.ResetBattle();
            if (MagicData.QueryMagic != null)
            {
                await MagicData.AbortMagicAsync(false);
            }

            await ClearTransformationAsync();

            if (QueryStatus(StatusSet.CYCLONE) != null || QueryStatus(StatusSet.SUPERMAN) != null)
            {
                await FinishXpAsync();
            }

            await SetAttributesAsync(ClientUpdateType.Mesh, Mesh);

            await DetachStatusAsync(StatusSet.CRIME).ConfigureAwait(true);
            await DetachAllStatusAsync().ConfigureAwait(true);
            await RemoveDeadMarkAsync();

            if (Scapegoat)
            {
                await SetScapegoatAsync(false);
            }

            await AttachStatusAsync(this, StatusSet.DEAD, 0, int.MaxValue, 0).ConfigureAwait(true);
            await AttachStatusAsync(this, StatusSet.GHOST, 0, int.MaxValue, 0).ConfigureAwait(true);

            await ClsXpValAsync();

            if (Team != null)
            {
                await Team.ProcessAuraAsync();
            }

            ghostTimer.Startup(3);
            reviveTimer.Startup(20);

            await KillCallPetAsync();

            if (attacker?.IsCallPet() == true && attacker.OwnerIdentity != 0)
            {
                Character owner = RoleManager.GetUser(attacker.OwnerIdentity);
                if (owner != null)
                {
                    await owner.SetCrimeStatusAsync(30);
                }
            }

            await GameAction.ExecuteActionAsync(USER_DIE_ACTION, this, attacker, null, string.Empty);

            var currentEvent = GetCurrentEvent();
            if (currentEvent != null)
            {
                // TODO add validation if keep original behavior
                await currentEvent.OnBeKillAsync(attacker, this, MagicData.QueryMagic);
                return;
            }

            uint idMap = 0;
            var posTarget = new Point();
            if (Map.GetRebornMap(ref idMap, ref posTarget))
            {
                await SavePositionAsync(idMap, (ushort)posTarget.X, (ushort)posTarget.Y);
            }

            if (Map.IsPkField() || Map.IsSynMap())
            {
                if (!Map.IsNewbieProtect())
                {
                    await UserPackage.RandDropItemAsync(1, 30);
                }

                if (Map.IsSynMap() && !Map.IsWarTime())
                {
                    await SavePositionAsync(1002, 300, 278);
                }

                return;
            }

            if (Map.IsPrisionMap())
            {
                if (!Map.IsNewbieProtect())
                {
                    int nChance = Math.Min(90, 20 + PkPoints / 2);
                    await UserPackage.RandDropItemAsync(3, nChance);
                }

                return;
            }

            if (attacker == null)
            {
                return;
            }

            if (attacker is Character atkJiangHu && JiangHu.HasJiangHu)
            {
                if (atkJiangHu.PkMode == PkModeType.JiangHu && JiangHu.IsActive)
                {
                    return;
                }
            }

            if (attacker is Character atkrUser && !Map.IsPkField() && !Map.IsSynMap() && !Map.IsFamilyMap())
            {
                if (!IsNewbie() || QueryStatus(StatusSet.RED_NAME) != null || IsEvil())
                {
                    int nChance;
                    if (PkPoints < 30)
                    {
                        nChance = 10 + await NextAsync(40);
                    }
                    else if (PkPoints < 100)
                    {
                        nChance = 50 + await NextAsync(50);
                    }
                    else
                    {
                        nChance = 100;
                    }

                    if (VipLevel < 6)
                    {
                        int nItems = UserPackage.InventoryCount;
                        int nDropItem = Level < 26 ? 0 : nItems * nChance / 100;
                        await UserPackage.RandDropItemAsync(nDropItem);

                        uint moneyDropped;
                        if (QueryStatus(StatusSet.RED_NAME) != null)                                   // Red name drop
                        {
                            moneyDropped = (uint)(Silvers * (ulong)(await NextAsync(35) + 25) / 100); // 25-60%
                        }
                        else if (QueryStatus(StatusSet.BLACK_NAME) != null)                            // Black name drop
                        {
                            moneyDropped = (uint)(Silvers * (ulong)(await NextAsync(40) + 40) / 100); // 40-80%
                        }
                        else                                                                           // normal drop
                        {
                            moneyDropped = (uint)(Silvers * (ulong)(await NextAsync(30) + 10) / 100); // 10-40%
                        }

                        if (moneyDropped > 0)
                        {
                            await DropSilverAsync(Math.Min(moneyDropped, 10_000_000));
                        }
                    }
                }

                if (attacker.Identity != Identity)
                {
                    await CreateEnemyAsync(atkrUser);

                    if (!IsBlessed)
                    {
                        long expLost = await CalcExpLostOfDeathAsync(attacker);
                        if (expLost > 0)
                        {
                            await AddAttributesAsync(ClientUpdateType.Experience, expLost * -1);
                        }
                    }

                    if (!atkrUser.IsBlessed && IsBlessed && !Map.IsArenicMapInGeneral())
                    {
                        if (atkrUser.QueryStatus(StatusSet.CURSED) != null)
                        {
                            IStatus status = atkrUser.QueryStatus(StatusSet.CURSED);
                            status.IncTime(300000, 60 * 5 * 12 * 1000);
                            await atkrUser.SynchroAttributesAsync(ClientUpdateType.CursedTimer,
                                                                  (ulong)status.RemainingTime);
                        }
                        else
                        {
                            await atkrUser.AttachStatusAsync(this, StatusSet.CURSED, 0, 300, 0);
                        }
                    }

                    int detainAmount = 0;
                    if (PkPoints >= 1000)
                    {
                        detainAmount = 3;
                    }
                    else if (PkPoints >= 300)
                    {
                        detainAmount = 2;
                    }
                    else if (PkPoints >= 100 || (PkPoints >= 30 && await ChanceCalcAsync(50, 100)))
                    {
                        detainAmount = 1;
                    }

                    for (int i = 0; i < detainAmount; i++)
                    {
                        if (!await ItemManager.DetainItemAsync(this, atkrUser))
                        {
                            break;
                        }
                    }
                }

                if (PkPoints >= 100)
                {
                    await SavePositionAsync(6000, 31, 72);
                    await FlyMapAsync(6000, 31, 72);
                    await BroadcastWorldMsgAsync(string.Format(StrGoToJail, attacker.Name, Name), TalkChannel.Talk, Color.White);
                }
            }
            else if (attacker is Monster monster)
            {
                if (!IsNewbie() || QueryStatus(StatusSet.RED_NAME) != null || IsEvil())
                {
                    var dropMoney = (uint)await NextAsync((int)(Silvers / 3));
                    if (dropMoney > 0)
                    {
                        await DropSilverAsync(Math.Min(dropMoney, 10_000_000));
                    }

                    var chance = 33;
                    if (Level < 10)
                    {
                        chance = 5;
                    }

                    await UserPackage.RandDropItemAsync(0, chance);
                }

                if (monster.IsGuard() && PkPoints > 99)
                {
                    await SavePositionAsync(6000, 31, 72);
                    await FlyMapAsync(6000, 31, 72);
                    await BroadcastWorldMsgAsync(string.Format(StrGoToJail, attacker.Name, Name), TalkChannel.Talk, Color.White);
                }
            }

            if (IsAutoHangUp && !Flag.HasFlag(PrivilegeFlag.FirstCreditClaimed))
            {
                var param = 50;
                if (QueryStatus(StatusSet.RED_NAME) != null)
                {
                    param = 20;
                }
                else if (QueryStatus(StatusSet.BLACK_NAME) != null)
                {
                    param = 10;
                }

                long expLost = ((long)AutoHangUpExperience / param);
                if (expLost > 0)
                {
                    AutoHangUpExperience = Math.Max(0, AutoHangUpExperience - (ulong)expLost);
                    await SendAsync(new MsgHangUp
                    {
                        Action = MsgHangUp.HangUpMode.KilledNoBlessing,
                        Experience = (ulong)expLost,
                        KillerName = attacker.Name
                    });
                }
            }
        }

        #endregion

        #region Monster Kills

        private ConcurrentDictionary<uint, DbMonsterKill> m_monsterKills = new();

        public async Task LoadMonsterKillsAsync()
        {
            m_monsterKills = new ConcurrentDictionary<uint, DbMonsterKill>((await MonsterKillRepository.GetAsync(Identity)).ToDictionary(x => x.Monster));
        }

        public Task KillMonsterAsync(uint type)
        {
            if (!m_monsterKills.TryGetValue(type, out DbMonsterKill value))
            {
                m_monsterKills.TryAdd(type, value = new DbMonsterKill
                {
                    CreatedAt = DateTime.Now,
                    UserIdentity = Identity,
                    Monster = type
                });
            }

            value.Amount += 1;
            return Task.CompletedTask;
        }

        #endregion

        #region Crime

        public async Task ProcessPkAsync(Character target)
        {
            if (!Map.IsPkField() && !Map.IsCallNewbieDisable() && !Map.IsSynMap() && !Map.IsPrisionMap())
            {
                if (!target.IsEvil())
                {
                    var nAddPk = 10;
                    if (target.IsNewbie() && !IsNewbie())
                    {
                        nAddPk = 20;
                    }
                    else
                    {
                        if (Syndicate?.IsEnemy(target.SyndicateIdentity) == true)
                        {
                            nAddPk = 3;
                        }
                        else if (IsEnemy(target.Identity))
                        {
                            nAddPk = 5;
                        }

                        if (target.PkPoints > 29)
                        {
                            nAddPk /= 2;
                        }
                    }

                    await AddAttributesAsync(ClientUpdateType.PkPoints, nAddPk);

                    await SetCrimeStatusAsync(90);

                    if (PkPoints > 29)
                    {
                        await SendAsync(StrKillingTooMuch);
                    }
                }

                int deltaLevel = Level - target.Level;
                var synPkPoints = 10;
                if (deltaLevel > 30)
                {
                    synPkPoints = 1;
                }
                else if (deltaLevel > 20)
                {
                    synPkPoints = 2;
                }
                else if (deltaLevel > 10)
                {
                    synPkPoints = 3;
                }
                else if (deltaLevel > 0)
                {
                    synPkPoints = 5;
                }

                if (SyndicateIdentity != 0)
                {
                    SyndicateMember.PkDonation += synPkPoints;
                    SyndicateMember.PkTotalDonation += synPkPoints;
                    await SyndicateMember.SaveAsync().ConfigureAwait(false);
                }

                if (target.SyndicateIdentity != 0)
                {
                    target.SyndicateMember.PkDonation -= synPkPoints;
                    await target.SyndicateMember.SaveAsync().ConfigureAwait(false);
                }

                if (SyndicateIdentity != 0 && target.SyndicateIdentity != 0)
                {
                    if (SyndicateIdentity == target.SyndicateIdentity)
                    {
                        await Syndicate.SendAsync(
                            string.Format(StrSyndicateSameKill, SyndicateRankName, Name,
                                          target.SyndicateRankName, target.Name, Map.Name), 0, Color.White);
                    }
                    else
                    {
                        await Syndicate.SendAsync(string.Format(StrSyndicateKill, SyndicateRankName, Name,
                                                                target.Name, target.SyndicateRankName,
                                                                target.SyndicateName, Map.Name));
                        await target.Syndicate.SendAsync(string.Format(StrSyndicateBeKill, Name,
                                                                       SyndicateRankName, SyndicateName,
                                                                       target.SyndicateRankName, target.Name,
                                                                       Map.Name));
                    }
                }
            }
        }

        public override async Task<bool> CheckCrimeAsync(Role target)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            if (target.Identity == Identity)
            {
                return false;
            }

            if (target is Character targetUser)
            {

                if (PkMode == PkModeType.JiangHu
                    && targetUser.JiangHu.IsActive)
                {
                    return false;
                }

            }

            if (!target.IsEvil() && !target.IsMonster() && target is not DynamicNpc)
            {
                if (!Map.IsTrainingMap() 
                    && !Map.IsPrisionMap()
                    && !Map.IsFamilyMap()
                    && !Map.IsCallNewbieDisable()
                    && !Map.IsPkField()
                    && !Map.IsSynMap()
                    && !Map.IsFamilyMap())
                {
                    await SetCrimeStatusAsync(30);
                }

                return true;
            }

            if (target is Monster mob && (mob.IsGuard() || mob.IsPkKiller()))
            {
                await SetCrimeStatusAsync(30);
                return true;
            }

            return false;
        }

        #endregion

        #region Revive

        private readonly TimeOut reviveTimer = new();
        private readonly TimeOut protectionTimer = new();
        private bool reviveLeaveMap;

        public bool CanRevive()
        {
            return !IsAlive && reviveTimer.IsTimeOut();
        }

        public async Task RebornAsync(bool chgMap, bool isSpell = false)
        {
            if (IsAlive || !CanRevive() && !isSpell)
            {
                if (QueryStatus(StatusSet.GHOST) != null)
                {
                    await DetachStatusAsync(StatusSet.GHOST);
                }

                if (QueryStatus(StatusSet.DEAD) != null)
                {
                    await DetachStatusAsync(StatusSet.DEAD);
                }

                if (TransformationMesh == 98 || TransformationMesh == 99)
                {
                    await ClearTransformationAsync();
                }

                return;
            }

            reviveTimer.Clear();
            BattleSystem.ResetBattle();

            await ClearTransformationAsync();
            await DetachStatusAsync(StatusSet.GHOST);
            await DetachStatusAsync(StatusSet.DEAD);

            await SetAttributesAsync(ClientUpdateType.Stamina, DEFAULT_USER_ENERGY);
            await SetAttributesAsync(ClientUpdateType.Hitpoints, MaxLife);
            await SetAttributesAsync(ClientUpdateType.Mana, MaxMana);
            await SetXpAsync(0);

            reviveLeaveMap = true;

            bool reviveOverriden = false;
            var currentEvent = GetCurrentEvent();
            if (currentEvent != null && await currentEvent.OnReviveAsync(this, !isSpell))
            {
                reviveOverriden = true;
            }

            if (reviveOverriden)
            {
                // do nothing, event already teleported user (or not)
            }
            else if (chgMap || !IsBlessed && !isSpell)
            {
                reviveLeaveMap = false;
                await FlyMapAsync(character.MapID, character.X, character.Y);
            }
            else
            {
                if (!isSpell && (Map.IsPrisionMap()
                                 || Map.IsPkField()
                                 || Map.IsCallNewbieDisable()
                || Map.IsSynMap()))
                {
                    await FlyMapAsync(character.MapID, character.X, character.Y);
                }
                else
                {
                    await FlyMapAsync(idMap, currentX, currentY);
                }
            }

            protectionTimer.Startup(CHGMAP_LOCK_SECS);
        }

        #endregion

        #region Pk Statistic

        public PkStatistic PkStatistic { get; init; }

        #endregion

        #region Oblivion

        private const int OBLIVION_MAX_COUNT = 32;
        private const double OBLIVION_BONUS_EXP_RATE = 1.5d;
        private long oblivionExperience;
        private int oblivionMonsterCount;

        public Task AwardOblivionExperienceAsync()
        {
            if (oblivionExperience == 0)
            {
                return Task.CompletedTask;
            }

            long tempOblivionexp = oblivionExperience;
            int tempKoCount = oblivionMonsterCount;
            oblivionMonsterCount = 0;
            oblivionExperience = 0;
            if (tempKoCount == OBLIVION_MAX_COUNT)
            {
                tempOblivionexp = (long)(tempOblivionexp * OBLIVION_BONUS_EXP_RATE);
            }
            return AwardExperienceAsync(tempOblivionexp);
        }

        #endregion

        #region Inner Strength

        public InnerStrength InnerStrength { get; init; }

        public uint CultureValue
        {
            get => character.CultureValue;
            set => character.CultureValue = value;
        }

        public async Task<bool> ChangeCultureAsync(int amount)
        {
            if (amount > 0)
            {
                await AwardCultureAsync(amount);
                return true;
            }

            if (amount < 0)
            {
                return await SpendCultureAsync(amount * -1);
            }

            return false;
        }

        public async Task AwardCultureAsync(int amount)
        {
            CultureValue = (uint)(CultureValue + amount);
            await SaveAsync();
            await SendAsync(new MsgUserAttrib(Identity, ClientUpdateType.InnerPowerPotency, CultureValue));
        }

        public async Task<bool> SpendCultureAsync(int amount)
        {
            if (amount > CultureValue)
            {
                return false;
            }

            CultureValue = (uint)(CultureValue - amount);
            await SaveAsync();
            await SendAsync(new MsgUserAttrib(Identity, ClientUpdateType.InnerPowerPotency, CultureValue));
            return true;
        }

        #endregion
    }
}
