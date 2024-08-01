using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Services.Managers;

namespace Canyon.Game.States.NeiGong
{
    public sealed class InnerStrengthSecret
    {
        private readonly DbInnerStrenghtSecret innerStrenghtSecret;
        private readonly List<InnerStrengthPower> innerPowers = new();

        public InnerStrengthSecret(DbInnerStrenghtSecret innerStrenghtSecret)
        {
            this.innerStrenghtSecret = innerStrenghtSecret;
        }

        public byte SecretType => innerStrenghtSecret.SecretType;

        public bool IsPerfect => innerPowers.Count > 0 && innerPowers.All(x => x.IsPerfect);

        public int TotalValue => innerPowers.Sum(x => x.Value);

        public bool HasBook(int type)
        {
            return innerPowers.Any(x => x.Identity == type);
        }

        public bool AddBook(InnerStrengthPower power)
        {
            if (HasBook(power.Identity))
            {
                return false;
            }
            innerPowers.Add(power);
            return true;
        }

        public InnerStrengthPower GetBook(int type)
        {
            return innerPowers.FirstOrDefault(x => x.Identity == type);
        }

        public List<InnerStrengthPower> GetPowers()
        {
            return new List<InnerStrengthPower>(innerPowers);
        }

        public Dictionary<InnerStrength.InnerStrengthAttrType, int> GetPower()
        {
            Dictionary<InnerStrength.InnerStrengthAttrType, int> powers = new();
            foreach (var power in innerPowers)
            {
                if (power.MaxLife > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.MaxLife, power.MaxLife);
                }
                if (power.PhysicAttackNew > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.Attack, power.PhysicAttackNew);
                }
                if (power.MagicAttack > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.MagicAttack, power.MagicAttack);
                }
                if (power.PhysicDefenseNew > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.Defense, power.PhysicDefenseNew);
                }
                if (power.MagicDefense > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.MagicDefense, power.MagicDefense);
                }
                if (power.FinalPhysicAdd > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.FinalPhysicalDamage, power.FinalPhysicAdd);
                }
                if (power.FinalMagicAdd > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.FinalMagicalDamage, power.FinalMagicAdd);
                }
                if (power.FinalPhysicReduce > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.FinalPhysicalDefense, power.FinalPhysicReduce);
                }
                if (power.FinalMagicReduce > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.FinalMagicalDefense, power.FinalMagicReduce);
                }
                if (power.PhysicCrit > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.CriticalStrike, power.PhysicCrit);
                }
                if (power.MagicCrit > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.SkillCriticalStrike, power.MagicCrit);
                }
                if (power.DefenseCrit > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.Immunity, power.DefenseCrit);
                }
                if (power.SmashRate > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.Breakthrough, power.SmashRate);
                }
                if (power.FirmDefenseRate > 0)
                {
                    AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.Counteraction, power.FirmDefenseRate);
                }
            }

            if (!IsPerfect)
            {
                return powers;
            }

            var secretType = InnerStrengthManager.QuerySecretType(SecretType);
            if (secretType.MaxLife > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.MaxLife, (int)secretType.MaxLife);
            }
            if (secretType.PhysicAttackNew > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.Attack, (int)secretType.PhysicAttackNew);
            }
            if (secretType.MagicAttack > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.MagicAttack, (int)secretType.MagicAttack);
            }
            if (secretType.PhysicDefenseNew > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.Defense, (int)secretType.PhysicDefenseNew);
            }
            if (secretType.MagicDefense > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.MagicDefense, (int)secretType.MagicDefense);
            }
            if (secretType.FinalPhysicAdd > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.FinalPhysicalDamage, secretType.FinalPhysicAdd);
            }
            if (secretType.FinalMagicAdd > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.FinalMagicalDamage, secretType.FinalMagicAdd);
            }
            if (secretType.FinalPhysicReduce > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.FinalPhysicalDefense, secretType.FinalPhysicReduce);
            }
            if (secretType.FinalMagicReduce > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.FinalMagicalDefense, secretType.FinalMagicReduce);
            }
            if (secretType.PhysicCrit > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.CriticalStrike, secretType.PhysicCrit);
            }
            if (secretType.MagicCrit > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.SkillCriticalStrike, secretType.MagicCrit);
            }
            if (secretType.DefenseCrit > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.Immunity, secretType.DefenseCrit);
            }
            if (secretType.SmashRate > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.Breakthrough, secretType.SmashRate);
            }
            if (secretType.FirmDefenseRate > 0)
            {
                AddOrIncrease(powers, InnerStrength.InnerStrengthAttrType.Counteraction, secretType.FirmDefenseRate);
            }
            return powers;
        }

        private void AddOrIncrease(Dictionary<InnerStrength.InnerStrengthAttrType, int> target, InnerStrength.InnerStrengthAttrType type, int power)
        {
            if (target.ContainsKey(type))
            {
                target[type] += power;
            }
            else
            {
                target.Add(type, power);
            }
        }

        public Task SaveAsync()
        {
            return ServerDbContext.SaveAsync(innerStrenghtSecret);
        }
    }
}
