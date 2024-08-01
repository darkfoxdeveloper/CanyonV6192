﻿using Canyon.Database.Entities;
using Canyon.Game.Database;
using static Canyon.Game.Services.Managers.InnerStrengthManager;

namespace Canyon.Game.States.NeiGong
{
    public sealed class InnerStrengthPower
    {
        private readonly DbInnerStrenghtPlayer innerStrenghtPlayer;
        private readonly InnerStrengthTypeInfo neiGongInfo;

        public InnerStrengthPower(DbInnerStrenghtPlayer innerStrenghtPlayer)
        {
            this.innerStrenghtPlayer = innerStrenghtPlayer;
            neiGongInfo = QueryTypeInfo((byte)innerStrenghtPlayer.Type);
        }

        public byte SecretType => neiGongInfo.SecretType;

        public int RequiredLevel => neiGongInfo?.RequiredLevel ?? 0;
        public int RequiredNeiGongValue => neiGongInfo?.RequiredNeiGongValue ?? 0;
        public int RequiredPreNeiGong => neiGongInfo?.RequiredPreNeiGong ?? 0;
        public uint RequiredItemType => neiGongInfo?.RequiredItemType ?? 0;

        public int Identity => innerStrenghtPlayer.Type;

        public byte Level
        {
            get => innerStrenghtPlayer.Level;
            set => innerStrenghtPlayer.Level = value;
        }

        public byte MaxLevel => (byte)GetStrenghtMaxLevel((byte) Identity);

        public byte Value
        {
            get => innerStrenghtPlayer.Value;
            set => innerStrenghtPlayer.Value = value;
        }

        public byte FinishValue
        {
            get => innerStrenghtPlayer.FinishValue;
            set => innerStrenghtPlayer.FinishValue = value;
        }

        public bool IsPerfect
        {
            get => innerStrenghtPlayer.Status != 0;
            set => innerStrenghtPlayer.Status = (byte)(value ? 1 : 0);
        }

        public byte AbolishNum
        {
            get => innerStrenghtPlayer?.AbolishNum ?? 0;
            set => innerStrenghtPlayer.AbolishNum = value;
        }

        public int MaxLife 
        {
            get => (int)innerStrenghtPlayer.MaxLife;
            set => innerStrenghtPlayer.MaxLife = (uint)value;
        }
        public int PhysicAttackNew
        {
            get => (int)innerStrenghtPlayer.PhysicAttackNew;
            set => innerStrenghtPlayer.PhysicAttackNew = (uint)value;
        }
        public int MagicAttack
        {
            get => (int)innerStrenghtPlayer.MagicAttack;
            set => innerStrenghtPlayer.MagicAttack = (uint)value;
        }
        public int PhysicDefenseNew
        {
            get => (int)innerStrenghtPlayer.PhysicDefenseNew;
            set => innerStrenghtPlayer.PhysicDefenseNew = (uint)value;
        }
        public int MagicDefense
        {
            get => (int)innerStrenghtPlayer.MagicDefense;
            set => innerStrenghtPlayer.MagicDefense = (uint)value;
        }
        public int FinalPhysicAdd
        {
            get => innerStrenghtPlayer.FinalPhysicAdd;
            set => innerStrenghtPlayer.FinalPhysicAdd = (ushort)value;
        }
        public int FinalMagicAdd
        {
            get => innerStrenghtPlayer.FinalMagicAdd;
            set => innerStrenghtPlayer.FinalMagicAdd = (ushort)value;
        }
        public int FinalPhysicReduce
        {
            get => innerStrenghtPlayer.FinalPhysicReduce;
            set => innerStrenghtPlayer.FinalPhysicReduce = (ushort)value;
        }
        public int FinalMagicReduce
        {
            get => innerStrenghtPlayer.FinalMagicReduce;
            set => innerStrenghtPlayer.FinalMagicReduce = (ushort)value;
        }
        public int PhysicCrit
        {
            get => innerStrenghtPlayer.PhysicCrit;
            set => innerStrenghtPlayer.PhysicCrit = (ushort)value;
        }
        public int MagicCrit
        {
            get => innerStrenghtPlayer.MagicCrit;
            set => innerStrenghtPlayer.MagicCrit = (ushort)value;
        }
        public int DefenseCrit
        {
            get => innerStrenghtPlayer.DefenseCrit;
            set => innerStrenghtPlayer.DefenseCrit = (ushort)value;
        }
        public int SmashRate
        {
            get => innerStrenghtPlayer.SmashRate;
            set => innerStrenghtPlayer.SmashRate = (ushort)value;
        }
        public int FirmDefenseRate
        {
            get => innerStrenghtPlayer.FirmDefenseRate;
            set => innerStrenghtPlayer.FirmDefenseRate = (ushort)value;
        }

        public Task SaveAsync()
        {
            return ServerDbContext.SaveAsync(innerStrenghtPlayer);
        }
    }
}
