// ISkillLogic.cs - 技能逻辑接口
// 策略模式：不同技能实现不同的逻辑
// 纯逻辑层，无Unity依赖

using MobaCombatCore.Core.Math;
using MobaCombatCore.Gameplay.Entity;

namespace MobaCombatCore.Gameplay.Skill
{
    /// <summary>
    /// 技能上下文 - 包含技能执行所需的所有信息
    /// </summary>
    public struct SkillContext
    {
        /// <summary>
        /// 施法者
        /// </summary>
        public BaseEntity Caster;

        /// <summary>
        /// 目标实体（可为null）
        /// </summary>
        public BaseEntity Target;

        /// <summary>
        /// 目标位置
        /// </summary>
        public FixedVector3 TargetPosition;

        /// <summary>
        /// 施法方向
        /// </summary>
        public FixedVector3 Direction;

        /// <summary>
        /// 技能配置
        /// </summary>
        public SkillData SkillData;

        /// <summary>
        /// 当前帧号
        /// </summary>
        public int CurrentFrame;

        /// <summary>
        /// 逻辑帧间隔
        /// </summary>
        public Fixed64 DeltaTime;
    }

    /// <summary>
    /// 技能执行结果
    /// </summary>
    public struct SkillResult
    {
        /// <summary>
        /// 是否执行成功
        /// </summary>
        public bool Success;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage;

        /// <summary>
        /// 生成的弹道实体（如果有）
        /// </summary>
        public ProjectileEntity Projectile;

        /// <summary>
        /// 命中的目标数量
        /// </summary>
        public int HitCount;

        /// <summary>
        /// 造成的总伤害
        /// </summary>
        public int TotalDamage;

        /// <summary>
        /// 成功结果
        /// </summary>
        public static SkillResult Succeeded => new SkillResult { Success = true };

        /// <summary>
        /// 失败结果
        /// </summary>
        public static SkillResult Failed(string message) => new SkillResult
        {
            Success = false,
            ErrorMessage = message
        };
    }

    /// <summary>
    /// 技能逻辑接口
    /// </summary>
    public interface ISkillLogic
    {
        /// <summary>
        /// 技能ID
        /// </summary>
        int SkillId { get; }

        /// <summary>
        /// 验证技能是否可以释放
        /// </summary>
        /// <param name="context">技能上下文</param>
        /// <returns>是否可以释放</returns>
        bool Validate(ref SkillContext context);

        /// <summary>
        /// 前摇开始时调用
        /// </summary>
        /// <param name="context">技能上下文</param>
        void OnCastStart(ref SkillContext context);

        /// <summary>
        /// 执行技能效果（前摇结束时调用）
        /// </summary>
        /// <param name="context">技能上下文</param>
        /// <returns>执行结果</returns>
        SkillResult Execute(ref SkillContext context);

        /// <summary>
        /// 技能被打断时调用
        /// </summary>
        /// <param name="context">技能上下文</param>
        void OnInterrupted(ref SkillContext context);

        /// <summary>
        /// 技能完成时调用（后摇结束）
        /// </summary>
        /// <param name="context">技能上下文</param>
        void OnComplete(ref SkillContext context);
    }

    /// <summary>
    /// 技能逻辑基类 - 提供默认实现
    /// </summary>
    public abstract class SkillLogicBase : ISkillLogic
    {
        public abstract int SkillId { get; }

        /// <summary>
        /// 验证技能是否可以释放
        /// </summary>
        public virtual bool Validate(ref SkillContext context)
        {
            // 检查施法者
            if (context.Caster == null || !context.Caster.IsAlive)
            {
                return false;
            }

            // 检查是否可以施法
            if (!context.Caster.CanCast)
            {
                return false;
            }

            // 检查法力值
            if (context.SkillData != null && context.Caster.Stats.CurrentMana < context.SkillData.manaCost)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 前摇开始
        /// </summary>
        public virtual void OnCastStart(ref SkillContext context)
        {
            // 默认实现：消耗法力
            if (context.SkillData != null)
            {
                context.Caster.Stats.CurrentMana -= context.SkillData.manaCost;
            }
        }

        /// <summary>
        /// 执行技能
        /// </summary>
        public abstract SkillResult Execute(ref SkillContext context);

        /// <summary>
        /// 技能被打断
        /// </summary>
        public virtual void OnInterrupted(ref SkillContext context)
        {
            // 默认实现：返还部分法力（可选）
        }

        /// <summary>
        /// 技能完成
        /// </summary>
        public virtual void OnComplete(ref SkillContext context)
        {
            // 默认实现：空
        }

        #region 辅助方法

        /// <summary>
        /// 计算伤害
        /// </summary>
        protected int CalculateDamage(ref SkillContext context)
        {
            if (context.SkillData == null) return 0;
            return context.SkillData.CalculateDamage(context.Caster.Stats.AbilityPower);
        }

        /// <summary>
        /// 检查目标是否在范围内
        /// </summary>
        protected bool IsTargetInRange(ref SkillContext context)
        {
            if (context.Target == null || context.SkillData == null) return false;

            var distance = FixedVector3.Distance2D(context.Caster.Position, context.Target.Position);
            return distance <= context.SkillData.GetRangeFixed();
        }

        /// <summary>
        /// 检查位置是否在范围内
        /// </summary>
        protected bool IsPositionInRange(ref SkillContext context)
        {
            if (context.SkillData == null) return false;

            var distance = FixedVector3.Distance2D(context.Caster.Position, context.TargetPosition);
            return distance <= context.SkillData.GetRangeFixed();
        }

        /// <summary>
        /// 获取施法方向
        /// </summary>
        protected FixedVector3 GetCastDirection(ref SkillContext context)
        {
            if (context.Direction.SqrMagnitude2D > Fixed64.Epsilon)
            {
                return context.Direction.Normalized2D;
            }

            if (context.Target != null)
            {
                return (context.Target.Position - context.Caster.Position).Normalized2D;
            }

            if (context.TargetPosition != FixedVector3.Zero)
            {
                return (context.TargetPosition - context.Caster.Position).Normalized2D;
            }

            // 默认使用施法者朝向
            return new FixedVector3(
                FixedMath.Sin(context.Caster.Rotation),
                Fixed64.Zero,
                FixedMath.Cos(context.Caster.Rotation)
            );
        }

        #endregion
    }
}
