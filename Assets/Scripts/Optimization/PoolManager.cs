
// PoolManager.cs
// 对象池管理器，提供一个全局访问点来管理所有类型的对象池
// 使用单例模式

using System.Collections.Generic;
using MobaCombatCore.Gameplay.Entity;
using MobaCombatCore.Optimization;

namespace MobaCombatCore.Glue
{
    public class PoolManager
    {
        private static PoolManager _instance;
        public static PoolManager Instance => _instance ?? (_instance = new PoolManager());

        private readonly Dictionary<System.Type, object> _pools = new Dictionary<System.Type, object>();

        private PoolManager() { }

        public ObjectPool<T> GetPool<T>() where T : class, IPoolable, new()
        {
            if (!_pools.TryGetValue(typeof(T), out var pool))
            {
                pool = new ObjectPool<T>();
                _pools[typeof(T)] = pool;
            }
            return (ObjectPool<T>)pool;
        }

        public T Get<T>() where T : class, IPoolable, new()
        {
            return GetPool<T>().Get();
        }

        public void Return<T>(T obj) where T : class, IPoolable, new()
        {
            if (obj != null)
            {
                GetPool<T>().Return(obj);
            }
        }

        public void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                // 使用反射调用Clear方法
                var clearMethod = pool.GetType().GetMethod("Clear");
                clearMethod?.Invoke(pool, null);
            }
            _pools.Clear();
        }

        // 为常用类型提供便捷访问
        public ObjectPool<ProjectileEntity> ProjectilePool => GetPool<ProjectileEntity>();
    }
}
