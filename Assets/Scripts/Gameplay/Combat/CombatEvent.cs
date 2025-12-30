// CombatEvent.cs - 战斗事件系统
// 用于解耦战斗逻辑，例如伤害、死亡、技能释放等
// 使用静态事件实现一个简单的全局事件总线

using System;
using MobaCombatCore.Gameplay.Entity;

namespace MobaCombatCore.Gameplay.Combat
{
    /// <summary>
    /// 伤害事件参数
    /// </summary>
    public class DamageEventArgs : EventArgs
    {
        public BaseEntity Source { get; }
        public BaseEntity Target { get; }
        public int DamageAmount { get; }
        public DamageType Type { get; }

        public DamageEventArgs(BaseEntity source, BaseEntity target, int damageAmount, DamageType type)
        {
            Source = source;
            Target = target;
            DamageAmount = damageAmount;
            Type = type;
        }
    }

    /// <summary>
    /// 死亡事件参数
    /// </summary>
    public class DeathEventArgs : EventArgs
    {
        public BaseEntity Killer { get; }
        public BaseEntity Victim { get; }

        public DeathEventArgs(BaseEntity killer, BaseEntity victim)
        {
            Killer = killer;
            Victim = victim;
        }
    }

    /// <summary>
    /// 治疗事件参数
    /// </summary>
    public class HealEventArgs : EventArgs
    {
        public BaseEntity Source { get; }
        public BaseEntity Target { get; }
        public int HealAmount { get; }

        public HealEventArgs(BaseEntity source, BaseEntity target, int healAmount)
        {
            Source = source;
            Target = target;
            HealAmount = healAmount;
        }
    }

    /// <summary>
    /// 技能释放事件参数
    /// </summary>
    public class SkillCastEventArgs : EventArgs
    {
        public BaseEntity Caster { get; }
        public int SkillId { get; }
        // 可根据需要添加更多技能相关信息，如目标、位置等

        public SkillCastEventArgs(BaseEntity caster, int skillId)
        {
            Caster = caster;
            SkillId = skillId;
        }
    }

    /// <summary>
    /// 战斗事件总线
    /// </summary>
    public static class CombatEvent
    {
        /// <summary>
        /// 造成伤害事件
        /// </summary>
        public static event EventHandler<DamageEventArgs> OnDamageDealt;

        /// <summary>
        /// 受到伤害事件
        /// </summary>
        public static event EventHandler<DamageEventArgs> OnDamageTaken;

        /// <summary>
        /// 实体死亡事件
        /// </summary>
        public static event EventHandler<DeathEventArgs> OnEntityDied;

        /// <summary>
        /// 实体复活事件
        /// </summary>
        public static event EventHandler<BaseEntity> OnEntityRevived;

        /// <summary>
        /// 造成治疗事件
        /// </summary>
        public static event EventHandler<HealEventArgs> OnHealDealt;

        /// <summary>
        /// 受到治疗事件
        /// </summary>
        public static event EventHandler<HealEventArgs> OnHealTaken;

        /// <summary>
        /// 技能开始释放事件
        /// </summary>
        public static event EventHandler<SkillCastEventArgs> OnSkillCastStart;

        /// <summary>
        /// 技能命中事件
        /// </summary>
        public static event EventHandler<SkillCastEventArgs> OnSkillHit;

        #region 触发事件方法

        public static void TriggerDamageDealt(BaseEntity source, BaseEntity target, int amount, DamageType type)
        {
            OnDamageDealt?.Invoke(null, new DamageEventArgs(source, target, amount, type));
        }

        public static void TriggerDamageTaken(BaseEntity source, BaseEntity target, int amount, DamageType type)
        {
            OnDamageTaken?.Invoke(null, new DamageEventArgs(source, target, amount, type));
        }

        public static void TriggerEntityDied(BaseEntity killer, BaseEntity victim)
        {
            OnEntityDied?.Invoke(null, new DeathEventArgs(killer, victim));
        }

        public static void TriggerEntityRevived(BaseEntity entity)
        {
            OnEntityRevived?.Invoke(null, entity);
        }

        public static void TriggerHealDealt(BaseEntity source, BaseEntity target, int amount)
        {
            OnHealDealt?.Invoke(null, new HealEventArgs(source, target, amount));
        }

        public static void TriggerHealTaken(BaseEntity source, BaseEntity target, int amount)
        {
            OnHealTaken?.Invoke(null, new HealEventArgs(source, target, amount));
        }

        public static void TriggerSkillCastStart(BaseEntity caster, int skillId)
        {
            OnSkillCastStart?.Invoke(null, new SkillCastEventArgs(caster, skillId));
        }

        public static void TriggerSkillHit(BaseEntity caster, int skillId)
        {
            OnSkillHit?.Invoke(null, new SkillCastEventArgs(caster, skillId));
        }

        #endregion

        /// <summary>
        /// 清理所有事件订阅（用于游戏重置）
        /// </summary>
        public static void ClearAll()
        {
            OnDamageDealt = null;
            OnDamageTaken = null;
            OnEntityDied = null;
            OnEntityRevived = null;
            OnHealDealt = null;
            OnHealTaken = null;
            OnSkillCastStart = null;
            OnSkillHit = null;
        }

        /// <summary>
        /// 绑定实体事件到战斗事件总线
        /// </summary>
        public static void BindEntityEvents(BaseEntity entity)
        {
            entity.OnDamageTaken += (target, amount, source) =>
            {
                TriggerDamageTaken(source, target, amount, DamageType.Physical); // 假设默认为物理伤害
            };
            entity.OnDeath += (victim, killer) =>
            {
                TriggerEntityDied(killer, victim);
            };
            // 可以绑定更多事件
        }

        /// <summary>
        /// 解绑实体事件
        /// </summary>
        public static void UnbindEntityEvents(BaseEntity entity)
        {
            // C#事件解绑比较复杂，需要保存委托实例
            // 在此简化，或者在实体销毁时手动清理
        }
    }
}
