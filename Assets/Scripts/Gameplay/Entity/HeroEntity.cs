// HeroEntity.cs - 英雄实体类
// 继承自BaseEntity，代表玩家控制的英雄

using MobaCombatCore.Core.Math;
using MobaCombatCore.Gameplay.Skill;
using MobaCombatCore.Glue.Services;

namespace MobaCombatCore.Gameplay.Entity
{
    /// <summary>
    /// 英雄实体 - 玩家控制的角色
    /// </summary>
    public class HeroEntity : BaseEntity
    {
        private const string LOG_TAG = "HeroEntity";
        
        /// <summary>
        /// 技能执行器
        /// </summary>
        public SkillExecutor SkillExecutor { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public HeroEntity()
        {
            Type = EntityType.Hero;
            Name = "Hero";
            
            // 英雄默认属性
            // 注意：Fixed64 的构造函数接收原始值，应使用 FromInt 或 FromFloat 创建实际数值
            Stats = new EntityStats
            {
                MaxHealth = 1000,
                CurrentHealth = 1000,
                MaxMana = 500,
                CurrentMana = 500,
                AttackDamage = 60,
                AbilityPower = 50,
                Armor = 30,
                MagicResist = 25,
                MoveSpeed = Fixed64.FromInt(5),      // 移动速度 5 单位/秒
                AttackSpeed = Fixed64.One,
                AttackRange = Fixed64.FromInt(2),    // 攻击范围 2 单位
                CollisionRadius = Fixed64.Half       // 碰撞半径 0.5 单位
            };
            BaseStats = Stats;
            
            // 创建技能执行器
            SkillExecutor = new SkillExecutor(this);
            
            GameLog.Debug(LOG_TAG, "Constructor", $"英雄实体创建完成，技能执行器已初始化");
        }

        /// <summary>
        /// 设置技能到指定槽位
        /// </summary>
        /// <param name="slotIndex">槽位索引 (0-3 对应 Q/W/E/R)</param>
        /// <param name="skillData">技能配置数据</param>
        /// <param name="skillLogic">技能逻辑实现</param>
        public void SetSkill(int slotIndex, SkillData skillData, ISkillLogic skillLogic)
        {
            SkillExecutor.SetSkill(slotIndex, skillData, skillLogic);
            GameLog.Info(LOG_TAG, "SetSkill",
                $"技能已设置 - 槽位:{slotIndex}, 技能名:{skillData?.skillName ?? "null"}");
        }

        /// <summary>
        /// 释放技能（指定目标位置）
        /// </summary>
        /// <param name="slotIndex">技能槽位</param>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="currentFrame">当前帧号</param>
        /// <param name="deltaTime">逻辑帧间隔</param>
        /// <returns>是否成功释放</returns>
        public bool CastSkill(int slotIndex, FixedVector3 targetPosition, int currentFrame, Fixed64 deltaTime)
        {
            GameLog.Debug(LOG_TAG, "CastSkill",
                $"尝试释放技能 - 槽位:{slotIndex}, 目标位置:({targetPosition.X.ToFloat():F2}, {targetPosition.Z.ToFloat():F2}), 帧号:{currentFrame}");
            
            bool success = SkillExecutor.CastSkillAtPosition(slotIndex, targetPosition, currentFrame, deltaTime);
            
            if (success)
            {
                GameLog.Info(LOG_TAG, "CastSkill", $"技能{slotIndex}释放成功");
            }
            else
            {
                GameLog.Warning(LOG_TAG, "CastSkill",
                    $"技能{slotIndex}释放失败 - CanCast:{CanCast}, 法力:{Stats.CurrentMana}");
            }
            
            return success;
        }

        public void CastSkillForced(int slotIndex, FixedVector3 direction)
        {
            SkillExecutor.CastSkillForced(slotIndex, direction);
        }


        /// <summary>
        /// 逻辑更新
        /// </summary>
        public override void LogicUpdate(int frameNumber, Fixed64 deltaTime)
        {
            base.LogicUpdate(frameNumber, deltaTime);
            
            // 更新技能执行器
            SkillExecutor?.Update(frameNumber, deltaTime);
        }
    }
}