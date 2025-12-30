// Skill_Blink.cs - 闪现技能逻辑
// 位移型技能的典型实现

using MobaCombatCore.Core.Math;
using MobaCombatCore.Gameplay.Entity;

namespace MobaCombatCore.Gameplay.Skill.SkillTypes
{
    public class Skill_Blink : SkillLogicBase
    {
        public override int SkillId => 2; // 假设闪现ID为2

        /// <summary>
        /// 验证
        /// </summary>
        public override bool Validate(ref SkillContext context)
        {
            if (!base.Validate(ref context)) return false;

            // 闪现是方向型技能
            if (context.SkillData.targetType == SkillTargetType.Direction)
            {
                var direction = GetCastDirection(ref context);
                if (direction.SqrMagnitude2D <= Fixed64.Epsilon)
                {
                    return false; // 无效方向
                }
            }
            // 闪现也可以是点对点
            else if (context.SkillData.targetType == SkillTargetType.Ground)
            {
                // 距离检查在执行时做
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

            // 1. 获取目标位置
            FixedVector3 targetPosition;
            var direction = GetCastDirection(ref context);

            if (skillData.targetType == SkillTargetType.Ground)
            {
                // 指向性闪现
                var distance = FixedVector3.Distance2D(caster.Position, context.TargetPosition);
                if (distance > skillData.GetBlinkDistanceFixed())
                {
                    // 如果目标点超出最大距离，则闪现到最大距离处
                    targetPosition = caster.Position + direction * skillData.GetBlinkDistanceFixed();
                }
                else
                {
                    targetPosition = context.TargetPosition;
                }
            }
            else // Direction
            {
                // 方向性闪现
                targetPosition = caster.Position + direction * skillData.GetBlinkDistanceFixed();
            }

            // 2. TODO: 检查目标位置是否合法（例如，是否在墙体或障碍物内）
            // 在本项目中，我们假设所有位置都是合法的

            // 3. 直接修改施法者位置
            caster.SetPosition(targetPosition);

            // 4. 更新施法者朝向
            if (direction.SqrMagnitude2D > Fixed64.Epsilon)
            {
                caster.Rotation = FixedMath.Atan2(direction.X, direction.Z);
            }

            // 5. 返回成功结果
            return SkillResult.Succeeded;
        }
    }
}
