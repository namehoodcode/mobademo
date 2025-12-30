// ProjectileEntity.cs - 弹道实体
// 用于火球术等飞行道具
// 继承自BaseEntity，拥有独立的移动和碰撞逻辑

using System;
using MobaCombatCore.Core.Math;
using MobaCombatCore.Core.Physics;
using MobaCombatCore.Glue;
using MobaCombatCore.Glue.Services;
using MobaCombatCore.Optimization;

namespace MobaCombatCore.Gameplay.Entity
{
    /// <summary>
    /// 弹道配置
    /// </summary>
    public struct ProjectileConfig
    {
        /// <summary>
        /// 弹道速度
        /// </summary>
        public Fixed64 Speed;

        /// <summary>
        /// 最大飞行距离
        /// </summary>
        public Fixed64 MaxDistance;

        /// <summary>
        /// 碰撞半径
        /// </summary>
        public Fixed64 CollisionRadius;

        /// <summary>
        /// 弹道生命周期（帧）
        /// </summary>
        public int LifetimeFrames;

        /// <summary>
        /// 命中后是否销毁
        /// </summary>
        public bool DestroyOnHit;

        /// <summary>
        // 是否是跟踪弹
        /// </summary>
        public bool IsHoming;

        /// <summary>
        /// 默认配置
        /// </summary>
        public static ProjectileConfig Default => new ProjectileConfig
        {
            Speed = new Fixed64(10),
            MaxDistance = new Fixed64(20),
            CollisionRadius = Fixed64.FromFloat(0.3f),
            LifetimeFrames = 90, // 3秒 @ 30fps
            DestroyOnHit = true,
            IsHoming = false,
        };
    }

    /// <summary>
    /// 弹道实体
    /// </summary>
    public class ProjectileEntity : BaseEntity, IPoolable
    {
        private const string LOG_TAG = "ProjectileEntity";
        
        /// <summary>
        /// 弹道配置
        /// </summary>
        public ProjectileConfig Config;

        /// <summary>
        /// 起始位置
        /// </summary>
        public FixedVector3 StartPosition { get; private set; }

        /// <summary>
        /// 飞行方向
        /// </summary>
        public FixedVector3 Direction { get; private set; }

        /// <summary>
        /// 已经飞行的距离
        /// </summary>
        public Fixed64 DistanceTraveled { get; private set; }

        /// <summary>
        /// 技能来源实体
        /// </summary>
        public BaseEntity Caster { get; private set; }

        /// <summary>
        /// 目标实体（用于跟踪弹）
        /// </summary>
        public BaseEntity Target { get; set; }

        /// <summary>
        /// 伤害值
        /// </summary>
        public int Damage { get; set; }

        /// <summary>
        /// 碰撞体
        /// </summary>
        public Circle Collider;

        /// <summary>
        /// 命中事件
        /// </summary>
        public event Action<ProjectileEntity, BaseEntity> OnHit;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ProjectileEntity()
        {
            Type = EntityType.Projectile;
            Config = ProjectileConfig.Default;
            Stats.CollisionRadius = Config.CollisionRadius;
        }

        /// <summary>
        /// 初始化弹道
        /// </summary>
        public void Launch(BaseEntity caster, FixedVector3 startPosition, FixedVector3 direction,
            BaseEntity target = null, int damage = 0)
        {
            Caster = caster;
            OwnerId = caster.OwnerId;
            TeamId = caster.TeamId;

            StartPosition = startPosition;
            Position = startPosition;
            Direction = direction.Normalized2D;

            Target = target;
            Damage = damage;

            DistanceTraveled = Fixed64.Zero;
            Stats.CollisionRadius = Config.CollisionRadius;

            // 设置速度
            Velocity = Direction * Config.Speed;

            // 初始化碰撞体
            Collider = new Circle(Position, Stats.CollisionRadius, EntityId, 0, TeamId);
            
            GameLog.Info(LOG_TAG, "Launch",
                $"弹道发射 - ID:{EntityId}, 施法者:{caster?.Name}, 起始位置:({startPosition.X.ToFloat():F2}, {startPosition.Z.ToFloat():F2}), " +
                $"方向:({Direction.X.ToFloat():F2}, {Direction.Z.ToFloat():F2}), 速度:{Config.Speed.ToFloat():F2}, 伤害:{damage}");
        }

        /// <summary>
        /// 逻辑更新
        /// </summary>
        public override void LogicUpdate(int frameNumber, Fixed64 deltaTime)
        {
            if (!IsActive || IsDestroyed) return;

            base.LogicUpdate(frameNumber, deltaTime);

            // 更新移动
            UpdateMovement(deltaTime);

            // 更新碰撞体位置
            Collider.SetCenter(Position);

            // 检查生命周期
            if (AliveFrames > Config.LifetimeFrames)
            {
                GameLog.Debug(LOG_TAG, "LogicUpdate",
                    $"弹道{EntityId}生命周期结束 - 存活帧数:{AliveFrames}, 最大帧数:{Config.LifetimeFrames}");
                Destroy();
                return;
            }

            // 检查最大距离
            if (DistanceTraveled >= Config.MaxDistance)
            {
                GameLog.Debug(LOG_TAG, "LogicUpdate",
                    $"弹道{EntityId}达到最大距离 - 已飞行:{DistanceTraveled.ToFloat():F2}, 最大距离:{Config.MaxDistance.ToFloat():F2}");
                Destroy();
                return;
            }
        }

        /// <summary>
        /// 更新移动逻辑
        /// </summary>
        protected override void UpdateMovement(Fixed64 deltaTime)
        {
            // 如果是跟踪弹，更新方向
            if (Config.IsHoming && Target != null && Target.IsAlive)
            {
                Direction = (Target.Position - Position).Normalized2D;
                Velocity = Direction * Config.Speed;
            }

            // 计算移动距离
            FixedVector3 moveDelta = Velocity * deltaTime;
            Position += moveDelta;
            DistanceTraveled += moveDelta.Magnitude2D;
        }

        /// <summary>
        /// 处理碰撞
        /// </summary>
        public void HandleCollision(BaseEntity hitEntity)
        {
            if (IsDestroyed) return;

            // 不能命中自己或友军
            if (hitEntity.EntityId == Caster.EntityId || hitEntity.TeamId == TeamId)
            {
                return;
            }

            // 不能命中其他弹道
            if (hitEntity is ProjectileEntity)
            {
                return;
            }

            GameLog.Info(LOG_TAG, "HandleCollision",
                $"弹道{EntityId}命中目标 - 目标:{hitEntity.Name}(ID:{hitEntity.EntityId}), " +
                $"目标位置:({hitEntity.Position.X.ToFloat():F2}, {hitEntity.Position.Z.ToFloat():F2}), " +
                $"目标血量:{hitEntity.Stats.CurrentHealth}/{hitEntity.Stats.MaxHealth}");

            // 触发命中事件
            OnHit?.Invoke(this, hitEntity);

            // 造成伤害
            if (Damage > 0)
            {
                GameLog.Info(LOG_TAG, "HandleCollision",
                    $"弹道{EntityId}对{hitEntity.Name}造成{Damage}点伤害");
                hitEntity.TakeDamage(Damage, Caster);
                GameLog.Info(LOG_TAG, "HandleCollision",
                    $"目标{hitEntity.Name}剩余血量:{hitEntity.Stats.CurrentHealth}/{hitEntity.Stats.MaxHealth}");
            }

            // 命中后销毁
            if (Config.DestroyOnHit)
            {
                GameLog.Debug(LOG_TAG, "HandleCollision", $"弹道{EntityId}命中后销毁");
                Destroy();
            }
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();

            // 归还到对象池而不是真正销毁
            PoolManager.Instance.Return(this);
        }

        public bool IsInPool { get; set; }

        public void OnGetFromPool()
        {
            // 从池中取出时重置状态
            IsDestroyed = false;
            IsDestroyPending = false;  // 重要：重置销毁待处理标志
            // 重新激活实体状态
            State = EntityState.Active | EntityState.Alive;
            ResetAliveFrames();
            OnHit = null;
        }

        public void OnReturnToPool()
        {
            // 归还到池中时的清理工作
            Caster = null;
            Target = null;
            // 移除激活状态
            RemoveState(EntityState.Active);
        }
    }
}
