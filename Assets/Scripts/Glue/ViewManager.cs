// ViewManager.cs - 视图管理器
// 详细设计见 Architecture.md - Glue层架构
// 优化：使用缓存列表避免foreach迭代器GC分配

using System.Collections.Generic;
using MobaCombatCore.DebugTools;
using MobaCombatCore.Gameplay.Entity;
using MobaCombatCore.Glue.Services;
using MobaCombatCore.Presentation;
using UnityEngine;

namespace MobaCombatCore.Glue
{
    /// <summary>
    /// 视图管理器 - 管理逻辑实体与表现层视图的绑定
    /// 优化版本：使用缓存列表避免foreach迭代器GC分配
    /// </summary>
    public class ViewManager
    {
        private const string LOG_TAG = "ViewManager";

        private readonly Dictionary<int, EntityView> _viewsByEntityId = new Dictionary<int, EntityView>();
        private readonly Dictionary<EntityType, GameObject> _prefabs;
        
        /// <summary>
        /// 缓存的视图列表，用于遍历更新，避免Dictionary.Values的迭代器分配
        /// </summary>
        private readonly List<EntityView> _viewsCache = new List<EntityView>(64);
        
        /// <summary>
        /// 标记缓存是否需要更新
        /// </summary>
        private bool _viewsCacheDirty = true;

        public ViewManager(Dictionary<EntityType, GameObject> prefabs)
        {
            _prefabs = prefabs;
            
            GameLog.Info(LOG_TAG, "Constructor", $"初始化完成 - 预制体数量:{prefabs.Count}");
        }

        public EntityView CreateViewForEntity(BaseEntity entity)
        {
            if (!_prefabs.TryGetValue(entity.Type, out var prefab))
            {
                GameLog.Error(LOG_TAG, "CreateViewForEntity",
                    $"未找到实体类型 {entity.Type} 对应的预制体");
                return null;
            }

            var go = Object.Instantiate(prefab, entity.Position.ToUnityVector3(), Quaternion.identity);
            var view = go.GetComponent<EntityView>();
            if (view == null)
            {
                GameLog.Error(LOG_TAG, "CreateViewForEntity",
                    $"类型 {entity.Type} 的预制体上未找到 EntityView 组件");
                Object.Destroy(go);
                return null;
            }

            view.BindEntity(entity);
            _viewsByEntityId[entity.EntityId] = view;
            _viewsCacheDirty = true;  // 标记缓存需要更新

            GameLog.Info(LOG_TAG, "CreateViewForEntity",
                $"创建视图 - 实体ID:{entity.EntityId}, 类型:{entity.Type}, 名称:{entity.Name}");

            return view;
        }

        public void DestroyViewForEntity(BaseEntity entity)
        {
            if (_viewsByEntityId.TryGetValue(entity.EntityId, out var view))
            {
                GameLog.Info(LOG_TAG, "DestroyViewForEntity",
                    $"销毁视图 - 实体ID:{entity.EntityId}");
                    
                _viewsByEntityId.Remove(entity.EntityId);
                _viewsCacheDirty = true;  // 标记缓存需要更新
                if (view != null)
                {
                    Object.Destroy(view.gameObject);
                }
            }
        }

        public EntityView GetView(int entityId)
        {
            _viewsByEntityId.TryGetValue(entityId, out var view);
            return view;
        }

        /// <summary>
        /// 更新所有视图
        /// 优化：使用缓存列表和for循环避免Dictionary迭代器GC分配
        /// </summary>
        public void UpdateAllViews(float interpolationAlpha)
        {
            // 如果缓存脏了，重建缓存
            if (_viewsCacheDirty)
            {
                RebuildViewsCache();
            }
            
            // 使用for循环遍历缓存列表，避免迭代器分配
            int count = _viewsCache.Count;
            for (int i = 0; i < count; i++)
            {
                var view = _viewsCache[i];
                if (view != null)
                {
                    view.OnLogicUpdate(interpolationAlpha);
                }
            }
        }
        
        /// <summary>
        /// 重建视图缓存
        /// </summary>
        private void RebuildViewsCache()
        {
            _viewsCache.Clear();
            foreach (var kvp in _viewsByEntityId)
            {
                _viewsCache.Add(kvp.Value);
            }
            _viewsCacheDirty = false;
        }

        public void ClearAllViews()
        {
            GameLog.Info(LOG_TAG, "ClearAllViews", $"清除所有视图 - 数量:{_viewsByEntityId.Count}");
            
            // 使用缓存列表遍历
            if (_viewsCacheDirty)
            {
                RebuildViewsCache();
            }
            
            int count = _viewsCache.Count;
            for (int i = 0; i < count; i++)
            {
                var view = _viewsCache[i];
                if (view != null)
                {
                    Object.Destroy(view.gameObject);
                }
            }
            _viewsByEntityId.Clear();
            _viewsCache.Clear();
            _viewsCacheDirty = false;
        }
    }
}
