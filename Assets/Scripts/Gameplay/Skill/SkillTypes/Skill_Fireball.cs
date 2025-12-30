// Skill_Fireball.cs - 火球术技能逻辑
// 弹道型技能的典型实现

using System;
using MobaCombatCore.Core.Math;
using MobaCombatCore.Gameplay.Entity;
using MobaCombatCore.Glue;
using MobaCombatCore.Glue.Services;

namespace MobaCombatCore.Gameplay.Skill.SkillTypes
{
    public class Skill_Fireball : SkillLogicBase
    {
        private const string LOG_TAG = "Skill_Fireball";
        
        public override int SkillId => 1; // 假设火球术ID为1

        /// <summary>
        /// 验证
        /// </summary>
        public override bool Validate(ref SkillContext context)
        {
            GameLog.Debug(LOG_TAG, "Validate",
                $"开始验证火球术 - 施法者:{context.Caster?.Name}, 法力:{context.Caster?.Stats.CurrentMana}/{context.Caster?.Stats.MaxMana}");
            
            if (!base.Validate(ref context))
            {
                GameLog.Warning(LOG_TAG, "Validate", "基础验证失败");
                return false;
            }

            // 方向型技能需要有效方向
            if (context.SkillData.targetType == SkillTargetType.Direction)
            {
                var direction = GetCastDirection(ref context);
                if (direction.SqrMagnitude2D <= Fixed64.Epsilon)
                {
                    GameLog.Warning(LOG_TAG, "Validate", "无效方向，验证失败");
                    return false; // 无效方向
                }
                GameLog.Debug(LOG_TAG, "Validate",
                    $"方向验证通过 - 方向:({direction.X.ToFloat():F2}, {direction.Z.ToFloat():F2})");
            }
            // 检查施法距离
            else if (context.SkillData.targetType == SkillTargetType.Ground)
            {
                if (!IsPositionInRange(ref context))
                {
                    GameLog.Warning(LOG_TAG, "Validate", "目标点太远，验证失败");
                    return false; // 目标点太远
                }
            }

            GameLog.Info(LOG_TAG, "Validate", "火球术验证通过");
            return true;
        }

        /// <summary>
        /// 前摇开始
        /// </summary>
        public override void OnCastStart(ref SkillContext context)
        {
            base.OnCastStart(ref context);
            GameLog.Info(LOG_TAG, "OnCastStart",
                $"火球术前摇开始 - 施法者:{context.Caster?.Name}, 帧号:{context.CurrentFrame}, 消耗法力:{context.SkillData?.manaCost}");
        }

        /// <summary>
        /// 执行
        /// </summary>
        public override SkillResult Execute(ref SkillContext context)
        {
            GameLog.Info(LOG_TAG, "Execute",
                $"火球术执行 - 施法者:{context.Caster?.Name}, 帧号:{context.CurrentFrame}");
            
            // 1. 获取施法方向
            var direction = GetCastDirection(ref context);
            context.Caster.Rotation = FixedMath.Atan2(direction.X, direction.Z);
            
            GameLog.Debug(LOG_TAG, "Execute",
                $"施法方向:({direction.X.ToFloat():F2}, {direction.Z.ToFloat():F2}), 施法者朝向:{context.Caster.Rotation.ToFloat():F2}rad");

            // 2. 从对象池获取弹道实体
            var projectile = PoolManager.Instance.Get<ProjectileEntity>();
            
            GameLog.Debug(LOG_TAG, "Execute",
                $"从对象池获取弹道 - ID:{projectile.EntityId}, IsInPool:{projectile.IsInPool}");

            // 3. 配置弹道
            var speed = context.SkillData.GetProjectileSpeedFixed();
            var maxDistance = context.SkillData.GetProjectileMaxDistanceFixed();
            var collisionRadius = context.SkillData.GetProjectileRadiusFixed();
            var lifetimeFrames = (int)(maxDistance / speed / context.DeltaTime);
            
            projectile.Config = new ProjectileConfig
            {
                Speed = speed,
                MaxDistance = maxDistance,
                CollisionRadius = collisionRadius,
                DestroyOnHit = context.SkillData.destroyOnHit,
                IsHoming = context.SkillData.isHoming,
                LifetimeFrames = lifetimeFrames
            };
            
            GameLog.Debug(LOG_TAG, "Execute",
                $"弹道配置 - 速度:{speed.ToFloat():F2}, 最大距离:{maxDistance.ToFloat():F2}, 碰撞半径:{collisionRadius.ToFloat():F2}, 生命周期:{lifetimeFrames}帧");

            // 4. 计算伤害
            int damage = CalculateDamage(ref context);
            GameLog.Debug(LOG_TAG, "Execute",
                $"伤害计算 - 基础伤害:{context.SkillData.baseDamage}, AP:{context.Caster.Stats.AbilityPower}, AP比例:{context.SkillData.apRatio}, 最终伤害:{damage}");

            // 5. 获取起始位置（例如，从施法者前方1米处发射）
            var startPosition = context.Caster.Position + direction * Fixed64.One;
            GameLog.Debug(LOG_TAG, "Execute",
                $"弹道起始位置:({startPosition.X.ToFloat():F2}, {startPosition.Y.ToFloat():F2}, {startPosition.Z.ToFloat():F2})");

            // 6. 发射弹道
            projectile.Launch(
                caster: context.Caster,
                startPosition: startPosition,
                direction: direction,
                target: context.Target, // 用于跟踪弹
                damage: damage
            );

            projectile.Initialize(context.CurrentFrame);
            
            GameLog.Info(LOG_TAG, "Execute",
                $"火球术发射成功! 弹道ID:{projectile.EntityId}, 伤害:{damage}, 方向:({direction.X.ToFloat():F2}, {direction.Z.ToFloat():F2})");

            // 7. 返回结果
            return new SkillResult
            {
                Success = true,
                Projectile = projectile,
                TotalDamage = damage
            };
        }

        /// <summary>
        /// 技能被打断
        /// </summary>
        public override void OnInterrupted(ref SkillContext context)
        {
            base.OnInterrupted(ref context);
            GameLog.Warning(LOG_TAG, "OnInterrupted",
                $"火球术被打断 - 施法者:{context.Caster?.Name}, 帧号:{context.CurrentFrame}");
        }

        /// <summary>
        /// 技能完成
        /// </summary>
        public override void OnComplete(ref SkillContext context)
        {
            base.OnComplete(ref context);
            GameLog.Info(LOG_TAG, "OnComplete",
                $"火球术施放完成 - 施法者:{context.Caster?.Name}, 帧号:{context.CurrentFrame}");
        }
    }
}
