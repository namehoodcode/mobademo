// DummyEntity.cs - 木桩实体类
// 继承自BaseEntity，代表训练用的木桩目标

using MobaCombatCore.Core.Math;

namespace MobaCombatCore.Gameplay.Entity
{
    /// <summary>
    /// 木桩实体 - 训练用的静止目标
    /// </summary>
    public class DummyEntity : BaseEntity
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public DummyEntity()
        {
            Type = EntityType.Dummy;
            Name = "Dummy";
            
            // 木桩默认属性 - 高血量，不移动
            Stats = new EntityStats
            {
                MaxHealth = 5000,
                CurrentHealth = 5000,
                MaxMana = 0,
                CurrentMana = 0,
                AttackDamage = 0,
                AbilityPower = 0,
                Armor = 0,
                MagicResist = 0,
                MoveSpeed = Fixed64.Zero,  // 木桩不移动
                AttackSpeed = Fixed64.Zero,
                AttackRange = Fixed64.Zero,
                CollisionRadius = Fixed64.One  // 较大的碰撞半径
            };
            BaseStats = Stats;
        }

        /// <summary>
        /// 木桩不能移动
        /// </summary>
        public override void MoveTo(FixedVector3 target)
        {
            // 木桩不响应移动命令
        }
    }
}