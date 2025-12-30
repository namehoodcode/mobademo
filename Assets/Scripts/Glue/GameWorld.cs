// GameWorld.cs - 游戏世界状态
// 详细设计见 Architecture.md - Glue层架构
//
// 设计原则：GameWorld 是纯数据容器，提供只读访问
// 所有实体的增删操作必须通过 EntityManager 进行

using System.Collections.Generic;
using System.Linq;
using MobaCombatCore.Core.Math;
using MobaCombatCore.Gameplay.Entity;

namespace MobaCombatCore.Glue
{
    /// <summary>
    /// 游戏世界 - 持有所有游戏状态的只读容器
    /// 注意：实体的增删操作请使用 EntityManager
    /// </summary>
    public class GameWorld
    {
        /// <summary>
        /// 当前游戏世界实例（供外部代码访问数据）
        /// 符合设计原则："需要数据去GameWorld"
        /// </summary>
        public static GameWorld Current { get; private set; }

        // 内部可修改的列表
        private readonly List<BaseEntity> _allEntities = new List<BaseEntity>();
        private readonly List<ProjectileEntity> _projectiles = new List<ProjectileEntity>();
        private readonly Dictionary<int, BaseEntity> _entityById = new Dictionary<int, BaseEntity>();

        /// <summary>
        /// 当前帧号
        /// </summary>
        public int CurrentFrame { get; set; }
        
        /// <summary>
        /// 所有实体（只读访问）
        /// </summary>
        public IReadOnlyList<BaseEntity> AllEntities => _allEntities;
        
        /// <summary>
        /// 所有弹道实体（只读访问）
        /// </summary>
        public IReadOnlyList<ProjectileEntity> Projectiles => _projectiles;
        
        /// <summary>
        /// 实体ID索引（只读访问）
        /// </summary>
        public IReadOnlyDictionary<int, BaseEntity> EntityById => _entityById;
        
        /// <summary>
        /// 随机数生成器
        /// </summary>
        public FixedRandom Random { get; }

        public GameWorld(int seed = 0)
        {
            Random = new FixedRandom(seed);
            Current = this;
        }

        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        public BaseEntity GetEntity(int entityId)
        {
            _entityById.TryGetValue(entityId, out var entity);
            return entity;
        }

        /// <summary>
        /// 根据玩家ID获取其控制的实体
        /// </summary>
        public BaseEntity GetEntityByOwnerId(int ownerId)
        {
            foreach (var entity in _allEntities)
            {
                if (entity.OwnerId == ownerId)
                {
                    return entity;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取玩家英雄（OwnerId为0的英雄实体）
        /// </summary>
        public HeroEntity GetPlayerHero()
        {
            return _allEntities.OfType<HeroEntity>().FirstOrDefault(h => h.OwnerId == 0);
        }

        #region 内部方法 - 仅供 EntityManager 调用

        /// <summary>
        /// 添加实体（内部方法，请使用 EntityManager.AddEntity）
        /// </summary>
        internal void AddEntityInternal(BaseEntity entity)
        {
            if (_entityById.ContainsKey(entity.EntityId)) return;

            _allEntities.Add(entity);
            _entityById[entity.EntityId] = entity;

            if (entity is ProjectileEntity projectile)
            {
                _projectiles.Add(projectile);
            }
        }

        /// <summary>
        /// 移除实体（内部方法，请使用 EntityManager.DestroyEntity）
        /// </summary>
        internal void RemoveEntityInternal(BaseEntity entity)
        {
            if (!_entityById.ContainsKey(entity.EntityId)) return;

            _allEntities.Remove(entity);
            _entityById.Remove(entity.EntityId);

            if (entity is ProjectileEntity projectile)
            {
                _projectiles.Remove(projectile);
            }
        }

        /// <summary>
        /// 清空所有实体（内部方法，请使用 EntityManager.ClearAllEntities）
        /// </summary>
        internal void ClearInternal()
        {
            _allEntities.Clear();
            _projectiles.Clear();
            _entityById.Clear();
            CurrentFrame = 0;
        }

        #endregion
    }
}
