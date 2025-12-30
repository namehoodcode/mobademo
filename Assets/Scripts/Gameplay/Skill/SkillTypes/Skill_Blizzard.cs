// Skill_Blizzard.cs - 暴风雪技能逻辑
// AOE型技能的典型实现
// 这个技能需要与Buff系统和持续性伤害系统交互，暂时先实现范围检测和一次性效果

using System.Collections.Generic;
using MobaCombatCore.Core.Math;
using MobaCombatCore.Core.Physics;
using MobaCombatCore.Gameplay.Entity;

namespace MobaCombatCore.Gameplay.Skill.SkillTypes
{
    public class Skill_Blizzard : SkillLogicBase
    {
        public override int SkillId => 3; // 假设暴风雪ID为3

        // 注意：持续性技能的状态管理比较复杂
        // 简单起见，这里只在Execute时应用一次效果
        // 完整的实现需要一个独立的系统来管理持续性AOE

        /// <summary>
        /// 验证
        /// </summary>
        public override bool Validate(ref SkillContext context)
        {
            if (!base.Validate(ref context)) return false;

            // 暴风雪是地面指向型技能
            if (context.SkillData.targetType != SkillTargetType.Ground)
            {
                return false;
            }

            // 检查施法距离
            if (!IsPositionInRange(ref context))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 执行
        /// </summary>
        public override SkillResult Execute(ref SkillContext context)
        {
            var caster = context.Caster;
            var skillData = context.SkillData;
            var targetPosition = context.TargetPosition;
            var radius = skillData.GetRadiusFixed();

            int hitCount = 0;
            int totalDamage = 0;

            // 1. 定义AOE范围
            var aoeCircle = new Circle(targetPosition, radius);

            // 2. 获取所有实体（需要外部传入或通过EntityManager获取）
            // 这里我们假设有一个方法能获取所有实体
            // List<BaseEntity> allEntities = GetWorldEntities();

            // 3. 遍历所有实体，检查是否在范围内
            // foreach (var entity in allEntities)
            // {
            //    if (entity == null || !entity.IsAlive || !entity.CanBeTargeted) continue;
            //    if (entity.IsEnemy(caster))
            //    {
            //        var entityCollider = new Circle(entity.Position, entity.Stats.CollisionRadius);
            //        if (CollisionDetector.CircleCircle(aoeCircle, entityCollider))
            //        {
            //            // 命中
            //            hitCount++;
            //
            //            // 造成伤害（简化为一次性伤害）
            //            int damage = skillData.tickDamage;
            //            entity.TakeDamage(damage, caster);
            //            totalDamage += damage;
            //
            //            // TODO: 添加减速Buff
            //            // if (skillData.applySlow)
            //            // {
            //            //     var slowBuff = new Buff_Slow(skillData.GetSlowPercentFixed(), skillData.GetSlowDurationFixed());
            //            //     entity.BuffManager.AddBuff(slowBuff);
            //            // }
            //        }
            //    }
            // }
            // 在当前架构下，技能逻辑不应该直接访问全局实体列表。
            // 更好的做法是，在Execute中创建一个"AOE实体"，由这个实体在自己的LogicUpdate中持续检测和造成伤害。
            // 但为了简化，这里暂时留空，表示AOE的逻辑需要更复杂的系统支持。

            // 4. 返回结果
            return new SkillResult
            {
                Success = true,
                HitCount = hitCount,
                TotalDamage = totalDamage
            };
        }
    }
}
