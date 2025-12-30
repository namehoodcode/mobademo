// BaseEntity.cs - 基础实体类
// 游戏逻辑层的实体基类，无Unity依赖
// 所有游戏对象（英雄、弹道、木桩等）都继承自此类

using System;
using MobaCombatCore.Core.Math;
using MobaCombatCore.DebugTools;
using UnityEngine;

namespace MobaCombatCore.Gameplay.Entity
{
    /// <summary>
    /// 实体类型
    /// </summary>
    public enum EntityType
    {
        None = 0,
        Hero = 1,
        Projectile = 2,
        Dummy = 3,      // 木桩
        Minion = 4,     // 小兵
        Tower = 5,      // 防御塔
        Monster = 6     // 野怪
    }

    /// <summary>
    /// 实体状态
    /// </summary>
    [Flags]
    public enum EntityState
    {
        None = 0,
        Active = 1 << 0,        // 激活
        Alive = 1 << 1,         // 存活
        Invincible = 1 << 2,    // 无敌
        Stunned = 1 << 3,       // 眩晕
        Silenced = 1 << 4,      // 沉默
        Rooted = 1 << 5,        // 定身
        Invisible = 1 << 6,     // 隐身
        Untargetable = 1 << 7   // 不可选中
    }

    /// <summary>
    /// 实体属性
    /// </summary>
    public struct EntityStats
    {
        /// <summary>
        /// 最大生命值
        /// </summary>
        public int MaxHealth;

        /// <summary>
        /// 当前生命值
        /// </summary>
        public int CurrentHealth;

        /// <summary>
        /// 最大法力值
        /// </summary>
        public int MaxMana;

        /// <summary>
        /// 当前法力值
        /// </summary>
        public int CurrentMana;

        /// <summary>
        /// 攻击力
        /// </summary>
        public int AttackDamage;

        /// <summary>
        /// 法术强度
        /// </summary>
        public int AbilityPower;

        /// <summary>
        /// 护甲
        /// </summary>
        public int Armor;

        /// <summary>
        /// 魔抗
        /// </summary>
        public int MagicResist;

        /// <summary>
        /// 移动速度（定点数）
        /// </summary>
        public Fixed64 MoveSpeed;

        /// <summary>
        /// 攻击速度（定点数）
        /// </summary>
        public Fixed64 AttackSpeed;

        /// <summary>
        /// 攻击范围（定点数）
        /// </summary>
        public Fixed64 AttackRange;

        /// <summary>
        /// 碰撞半径（定点数）
        /// </summary>
        public Fixed64 CollisionRadius;

        /// <summary>
        /// 默认属性
        /// </summary>
        public static EntityStats Default => new EntityStats
        {
            MaxHealth = 1000,
            CurrentHealth = 1000,
            MaxMana = 500,
            CurrentMana = 500,
            AttackDamage = 50,
            AbilityPower = 0,
            Armor = 20,
            MagicResist = 20,
            MoveSpeed = Fixed64.FromInt(5),      // 移动速度 5 单位/秒
            AttackSpeed = Fixed64.One,
            AttackRange = Fixed64.FromInt(2),    // 攻击范围 2 单位
            CollisionRadius = Fixed64.Half       // 碰撞半径 0.5 单位
        };

        /// <summary>
        /// 生命值百分比
        /// </summary>
        public float HealthPercent => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;

        /// <summary>
        /// 法力值百分比
        /// </summary>
        public float ManaPercent => MaxMana > 0 ? (float)CurrentMana / MaxMana : 0f;
    }

    /// <summary>
    /// 基础实体类 - 所有游戏对象的基类
    /// </summary>
    public class BaseEntity
    {
        #region 基础属性

        /// <summary>
        /// 实体唯一ID
        /// </summary>
        public int EntityId { get; private set; }

        /// <summary>
        /// 实体类型
        /// </summary>
        public EntityType Type { get; protected set; }

        /// <summary>
        /// 实体状态标志
        /// </summary>
        public EntityState State { get; set; }

        /// <summary>
        /// 所属玩家ID（-1表示中立）
        /// </summary>
        public int OwnerId { get; set; }

        /// <summary>
        /// 队伍ID
        /// </summary>
        public int TeamId { get; set; }

        /// <summary>
        /// 实体名称
        /// </summary>
        public string Name { get; set; }

        #endregion

        #region 位置与移动

        /// <summary>
        /// 当前位置（逻辑层）
        /// </summary>
        public FixedVector3 Position { get; set; }

        /// <summary>
        /// 上一帧位置（用于插值）
        /// </summary>
        public FixedVector3 PreviousPosition { get; private set; }

        /// <summary>
        /// 当前速度
        /// </summary>
        public FixedVector3 Velocity { get; set; }

        /// <summary>
        /// 朝向（Y轴旋转角度，弧度）
        /// </summary>
        public Fixed64 Rotation { get; set; }

        /// <summary>
        /// 目标位置（移动目标）
        /// </summary>
        public FixedVector3 TargetPosition { get; set; }

        /// <summary>
        /// 是否正在移动
        /// </summary>
        public bool IsMoving { get; set; }

        #endregion

        #region 属性

        /// <summary>
        /// 实体属性
        /// </summary>
        public EntityStats Stats;

        /// <summary>
        /// 基础属性（不受Buff影响）
        /// </summary>
        public EntityStats BaseStats;

        #endregion

        #region 生命周期

        /// <summary>
        /// 创建时间（帧号）
        /// </summary>
        public int CreatedFrame { get; private set; }

        /// <summary>
        /// 存活时间（帧数）
        /// </summary>
        public int AliveFrames { get; protected set; }

        /// <summary>
        /// 是否已销毁
        /// </summary>
        public bool IsDestroyed { get; protected set; }

        /// <summary>
        /// 是否已加入销毁队列
        /// </summary>
        public bool IsDestroyPending { get; set; }


        #endregion

        #region 事件

        /// <summary>
        /// 实体创建事件
        /// </summary>
        public event Action<BaseEntity> OnCreated;

        /// <summary>
        /// 实体销毁事件
        /// </summary>
        public event Action<BaseEntity> OnDestroyed;

        /// <summary>
        /// 位置变化事件
        /// </summary>
        public event Action<BaseEntity, FixedVector3, FixedVector3> OnPositionChanged;

        /// <summary>
        /// 受到伤害事件
        /// </summary>
        public event Action<BaseEntity, int, BaseEntity> OnDamageTaken;

        /// <summary>
        /// 死亡事件
        /// </summary>
        public event Action<BaseEntity, BaseEntity> OnDeath;

        #endregion

        /// <summary>
        /// 静态ID计数器
        /// </summary>
        private static int _nextEntityId = 1;

        /// <summary>
        /// 构造函数
        /// </summary>
        public BaseEntity()
        {
            EntityId = _nextEntityId++;
            Type = EntityType.None;
            State = EntityState.Active | EntityState.Alive;
            OwnerId = -1;
            TeamId = 0;
            Name = $"Entity_{EntityId}";

            Position = FixedVector3.Zero;
            PreviousPosition = FixedVector3.Zero;
            Velocity = FixedVector3.Zero;
            Rotation = Fixed64.Zero;
            TargetPosition = FixedVector3.Zero;
            IsMoving = false;

            Stats = EntityStats.Default;
            BaseStats = EntityStats.Default;

            CreatedFrame = 0;
            AliveFrames = 0;
            IsDestroyed = false;
        }

        /// <summary>
        /// 重置ID计数器（用于游戏重置）
        /// </summary>
        public static void ResetIdCounter()
        {
            _nextEntityId = 1;
        }

        /// <summary>
        /// 重置存活帧数（用于对象池复用）
        /// </summary>
        protected void ResetAliveFrames()
        {
            AliveFrames = 0;
        }

        #region 状态检查

        /// <summary>
        /// 检查是否有指定状态
        /// </summary>
        public bool HasState(EntityState state)
        {
            return (State & state) != 0;
        }

        /// <summary>
        /// 添加状态
        /// </summary>
        public void AddState(EntityState state)
        {
            State |= state;
        }

        /// <summary>
        /// 移除状态
        /// </summary>
        public void RemoveState(EntityState state)
        {
            State &= ~state;
        }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive => HasState(EntityState.Active);

        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive => HasState(EntityState.Alive);

        /// <summary>
        /// 是否可以移动
        /// </summary>
        public bool CanMove => IsActive && IsAlive && !HasState(EntityState.Stunned) && !HasState(EntityState.Rooted);

        /// <summary>
        /// 是否可以施法
        /// </summary>
        public bool CanCast => IsActive && IsAlive && !HasState(EntityState.Stunned) && !HasState(EntityState.Silenced);

        /// <summary>
        /// 是否可以被选中
        /// </summary>
        public bool CanBeTargeted => IsActive && IsAlive && !HasState(EntityState.Untargetable);

        #endregion

        #region 生命周期方法

        /// <summary>
        /// 初始化（在创建后调用）
        /// </summary>
        public virtual void Initialize(int currentFrame)
        {
            CreatedFrame = currentFrame;
            AliveFrames = 0;
            PreviousPosition = Position;
            OnCreated?.Invoke(this);
            GizmosDrawer.Instance?.RegisterEntity(this);
        }

        /// <summary>
        /// 逻辑更新（每逻辑帧调用）
        /// </summary>
        public virtual void LogicUpdate(int frameNumber, Fixed64 deltaTime)
        {
            if (!IsActive || IsDestroyed) return;

            // 保存上一帧位置
            PreviousPosition = Position;

            // 更新移动
            if (IsMoving && CanMove)
            {
                UpdateMovement(deltaTime);
            }

            // 更新存活时间
            AliveFrames++;
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        public virtual void Destroy()
        {
            if (IsDestroyed) return;

            IsDestroyed = true;
            RemoveState(EntityState.Active);
            RemoveState(EntityState.Alive);

            OnDestroyed?.Invoke(this);
            GizmosDrawer.Instance?.UnregisterEntity(this);
        }

        #endregion

        #region 移动

        /// <summary>
        /// 移动到目标位置
        /// </summary>
        public virtual void MoveTo(FixedVector3 target)
        {
            if (!CanMove) return;

            TargetPosition = target;
            IsMoving = true;

            // 计算朝向
            var direction = (target - Position).Normalized2D;
            if (direction.SqrMagnitude2D > Fixed64.Epsilon)
            {
                Rotation = FixedMath.Atan2(direction.X, direction.Z);
            }
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public virtual void StopMovement()
        {
            IsMoving = false;
            Velocity = FixedVector3.Zero;
        }

        /// <summary>
        /// 更新移动
        /// </summary>
        protected virtual void UpdateMovement(Fixed64 deltaTime)
        {
            var direction = (TargetPosition - Position).Normalized2D;
            var distance = FixedVector3.Distance2D(Position, TargetPosition);

            // 计算本帧移动距离
            var moveDistance = Stats.MoveSpeed * deltaTime;

            if (distance <= moveDistance)
            {
                // 到达目标
                Position = TargetPosition;
                StopMovement();
            }
            else
            {
                // 继续移动
                Velocity = direction * Stats.MoveSpeed;
                Position += Velocity * deltaTime;

                // 更新朝向
                if (direction.SqrMagnitude2D > Fixed64.Epsilon)
                {
                    Rotation = FixedMath.Atan2(direction.X, direction.Z);
                }
            }

            // 触发位置变化事件
            if (Position != PreviousPosition)
            {
                OnPositionChanged?.Invoke(this, PreviousPosition, Position);
            }
        }

        /// <summary>
        /// 直接设置位置（瞬移）
        /// </summary>
        public virtual void SetPosition(FixedVector3 newPosition)
        {
            var oldPosition = Position;
            Position = newPosition;
            OnPositionChanged?.Invoke(this, oldPosition, newPosition);
        }

        #endregion

        #region 战斗

        /// <summary>
        /// 受到伤害
        /// </summary>
        public virtual void TakeDamage(int damage, BaseEntity source = null)
        {
            if (!IsAlive || HasState(EntityState.Invincible)) return;

            // 应用伤害
            Stats.CurrentHealth -= damage;
            OnDamageTaken?.Invoke(this, damage, source);

            // 检查死亡
            if (Stats.CurrentHealth <= 0)
            {
                Stats.CurrentHealth = 0;
                Die(source);
            }
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public virtual void Heal(int amount)
        {
            if (!IsAlive) return;

            Stats.CurrentHealth += amount;
            if (Stats.CurrentHealth > Stats.MaxHealth)
            {
                Stats.CurrentHealth = Stats.MaxHealth;
            }
        }

        /// <summary>
        /// 死亡
        /// </summary>
        protected virtual void Die(BaseEntity killer = null)
        {
            RemoveState(EntityState.Alive);
            StopMovement();
            OnDeath?.Invoke(this, killer);
        }

        /// <summary>
        /// 复活
        /// </summary>
        public virtual void Revive(int healthPercent = 100)
        {
            if (IsAlive) return;

            AddState(EntityState.Alive);
            Stats.CurrentHealth = Stats.MaxHealth * healthPercent / 100;
            Stats.CurrentMana = Stats.MaxMana;
        }

        #endregion
    }
}
