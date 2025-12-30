// DamageCalculator.cs - 伤害计算器
// 用于计算所有类型的伤害，包括物理、魔法、真实伤害
// 纯静态方法，无状态，基于定点数保证确定性

using System;
using MobaCombatCore.Core.Math;
using MobaCombatCore.Gameplay.Entity;

namespace MobaCombatCore.Gameplay.Combat
{
    /// <summary>
    /// 伤害类型
    /// </summary>
    public enum DamageType
    {
        Physical, // 物理伤害
        Magical,  // 魔法伤害
        True      // 真实伤害
    }

    /// <summary>
    /// 伤害信息
    /// </summary>
    public struct DamageInfo
    {
        public int BaseDamage;
        public DamageType Type;
        public BaseEntity Source;
        public BaseEntity Target;
    }

    /// <summary>
    /// 伤害计算器
    /// </summary>
    public static class DamageCalculator
    {
        // 护甲/魔抗减伤系数
        private static readonly Fixed64 K = new Fixed64(100);

        /// <summary>
        /// 计算最终伤害
        /// </summary>
        public static int CalculateDamage(DamageInfo info)
        {
            if (info.Target == null || !info.Target.IsAlive)
            {
                return 0;
            }

            int finalDamage;

            switch (info.Type)
            {
                case DamageType.Physical:
                    finalDamage = CalculatePhysicalDamage(info.BaseDamage, info.Target.Stats.Armor);
                    break;
                case DamageType.Magical:
                    finalDamage = CalculateMagicalDamage(info.BaseDamage, info.Target.Stats.MagicResist);
                    break;
                case DamageType.True:
                    finalDamage = info.BaseDamage;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // 最小伤害为1
            return System.Math.Max(1, finalDamage);
        }

        /// <summary>
        /// 计算物理伤害
        /// 伤害减免 = 护甲 / (100 + 护甲)
        /// 最终伤害 = 基础伤害 * (1 - 伤害减免)
        /// </summary>
        public static int CalculatePhysicalDamage(int baseDamage, int armor)
        {
            if (armor >= 0)
            {
                Fixed64 reduction = Fixed64.FromInt(armor) / (K + armor);
                Fixed64 finalDamage = Fixed64.FromInt(baseDamage) * (Fixed64.One - reduction);
                return finalDamage.ToInt();
            }
            else
            {
                // 负护甲时，伤害增加
                // 伤害增加 = 1 - (100 / (100 - 护甲))
                Fixed64 multiplier = Fixed64.One - (K / (K - armor));
                Fixed64 finalDamage = Fixed64.FromInt(baseDamage) * (Fixed64.One + multiplier);
                return finalDamage.ToInt();
            }
        }

        /// <summary>
        /// 计算魔法伤害
        /// 伤害减免 = 魔抗 / (100 + 魔抗)
        /// 最终伤害 = 基础伤害 * (1 - 伤害减免)
        /// </summary>
        public static int CalculateMagicalDamage(int baseDamage, int magicResist)
        {
            if (magicResist >= 0)
            {
                Fixed64 reduction = Fixed64.FromInt(magicResist) / (K + magicResist);
                Fixed64 finalDamage = Fixed64.FromInt(baseDamage) * (Fixed64.One - reduction);
                return finalDamage.ToInt();
            }
            else
            {
                // 负魔抗时，伤害增加
                Fixed64 multiplier = Fixed64.One - (K / (K - magicResist));
                Fixed64 finalDamage = Fixed64.FromInt(baseDamage) * (Fixed64.One + multiplier);
                return finalDamage.ToInt();
            }
        }

        /// <summary>
        /// 计算普攻伤害
        /// </summary>
        public static int CalculateAutoAttackDamage(BaseEntity attacker, BaseEntity target)
        {
            if (attacker == null || target == null)
            {
                return 0;
            }

            int baseDamage = attacker.Stats.AttackDamage;
            return CalculatePhysicalDamage(baseDamage, target.Stats.Armor);
        }

        /// <summary>
        /// 计算暴击伤害
        /// </summary>
        public static int CalculateCriticalDamage(int baseDamage, Fixed64 critMultiplier)
        {
            return (Fixed64.FromInt(baseDamage) * critMultiplier).ToInt();
        }
    }
}
