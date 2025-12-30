// EntityManager.cs - 实体管理器
// 详细设计见 Architecture.md - Glue层架构

using System;
using System.Collections.Generic;
using MobaCombatCore.Core.Math;
using MobaCombatCore.Gameplay.Entity;
using MobaCombatCore.Gameplay.Skill;
using MobaCombatCore.Glue.Services;

namespace MobaCombatCore.Glue
{
    /// <summary>
    /// 实体管理器 - 管理所有游戏实体的生命周期
    /// </summary>
    public class EntityManager
    {
        private const string LOG_TAG = "EntityManager";

        private readonly GameWorld _gameWorld;
        private readonly List<BaseEntity> _entitiesToDestroy = new List<BaseEntity>();
        private readonly List<BaseEntity> _entitiesToAdd = new List<BaseEntity>();
        private bool _isUpdating = false;

        public event Action<BaseEntity> OnEntityCreated;
        public event Action<BaseEntity> OnEntityDestroyed;

        public EntityManager(GameWorld gameWorld)
        {
            _gameWorld = gameWorld;
        }

        public T CreateEntity<T>() where T : BaseEntity, new()
        {
            var entity = new T();

            if (_isUpdating)
            {
                _entitiesToAdd.Add(entity);
            }
            else
            {
                _gameWorld.AddEntityInternal(entity);
            }

            entity.Initialize(_gameWorld.CurrentFrame);
            OnEntityCreated?.Invoke(entity);

            return entity;
        }

        public T CreateEntity<T>(FixedVector3 position) where T : BaseEntity, new()
        {
            var entity = CreateEntity<T>();
            entity.Position = position;
            return entity;
        }

        public ProjectileEntity CreateProjectile(BaseEntity caster, SkillData skillData, FixedVector3 direction)
        {
            // 从对象池获取ProjectileEntity
            var projectile = PoolManager.Instance.Get<ProjectileEntity>();
            projectile.Launch(caster, caster.Position, direction, null, skillData.baseDamage);

            RegisterEntity(projectile);
            OnEntityCreated?.Invoke(projectile);

            return projectile;
        }

        /// <summary>
        /// 注册已创建的实体到游戏世界（用于外部创建的实体，如弹道）
        /// </summary>
        public void RegisterEntity(BaseEntity entity)
        {
            if (entity == null) return;

            if (_isUpdating)
            {
                _entitiesToAdd.Add(entity);
            }
            else
            {
                _gameWorld.AddEntityInternal(entity);
            }
        }


        public void DestroyEntity(BaseEntity entity)
        {
            if (entity == null || entity.IsDestroyed) return;

            if (entity.IsDestroyPending) return;
            entity.IsDestroyPending = true;

            _entitiesToDestroy.Add(entity);
        }

        public void UpdateAllEntities(int frame, Fixed64 deltaTime)
        {
            _isUpdating = true;
            
            // 使用索引遍历，避免foreach期间修改集合的问题
            // 注意：这里只遍历更新开始时已存在的实体
            int entityCount = _gameWorld.AllEntities.Count;
            for (int i = 0; i < entityCount; i++)
            {
                var entity = _gameWorld.AllEntities[i];
                entity.LogicUpdate(frame, deltaTime);
            }
            
            _isUpdating = false;

            // 处理延迟添加的实体
            ProcessPendingAdds();
            
            // 处理延迟销毁的实体
            ProcessPendingDestroys();
        }

        private void ProcessPendingAdds()
        {
            if (_entitiesToAdd.Count > 0)
            {
                GameLog.Debug(LOG_TAG, "ProcessPendingAdds",
                    $"处理延迟添加的实体 - 数量:{_entitiesToAdd.Count}");
                    
                foreach (var entity in _entitiesToAdd)
                {
                    _gameWorld.AddEntityInternal(entity);
                }
                _entitiesToAdd.Clear();
            }
        }

        private void ProcessPendingDestroys()
        {
            if (_entitiesToDestroy.Count > 0)
            {
                foreach (var entity in _entitiesToDestroy)
                {
                    _gameWorld.RemoveEntityInternal(entity);
                    OnEntityDestroyed?.Invoke(entity);

                    // 派发到各自的Destroy，由实体自己决定如何销毁（例如，归还对象池）
                    entity.Destroy();
                }
                _entitiesToDestroy.Clear();
            }
        }

        public void ClearAllEntities()
        {
            _entitiesToDestroy.AddRange(_gameWorld.AllEntities);
            ProcessPendingDestroys();
            _entitiesToAdd.Clear();
            _gameWorld.ClearInternal();
        }
    }
}
