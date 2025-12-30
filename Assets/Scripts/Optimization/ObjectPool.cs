
// ObjectPool.cs
// 通用对象池实现，用于复用高频创建和销毁的对象（如弹道、特效）
// 核心思想：预先创建一定数量的对象，需要时从池中获取，用完后归还池中，避免GC

using System;
using System.Collections.Generic;
using MobaCombatCore.Glue.Services;

namespace MobaCombatCore.Optimization
{
    public interface IPoolable
    {
        void OnGetFromPool();
        void OnReturnToPool();
        bool IsInPool { get; set; }
    }

    public class ObjectPool<T> where T : class, IPoolable, new()
    {
        private const string LOG_TAG = "ObjectPool";

        private readonly Stack<T> _pool = new Stack<T>();
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;
        private readonly int _maxSize;

        public int Count => _pool.Count;
        public int ActiveCount { get; private set; }

        public ObjectPool(Action<T> onGet = null, Action<T> onReturn = null, int initialCapacity = 16, int maxSize = 10000)
        {
            _onGet = onGet;
            _onReturn = onReturn;
            _maxSize = maxSize;

            // 预填充
            for (int i = 0; i < initialCapacity; i++)
            {
                var obj = new T();
                obj.IsInPool = true;
                _pool.Push(obj);
            }
        }

        public T Get()
        {
            T obj;
            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
                GameLog.Debug(LOG_TAG, "Get", $"从池中获取对象 - 类型:{typeof(T).Name}, 当前池大小:{Count}, 活动对象:{ActiveCount + 1}");
            }
            else
            {
                obj = new T();
                GameLog.Warning(LOG_TAG, "Get", $"池已空，创建新对象 - 类型:{typeof(T).Name}, 活动对象:{ActiveCount + 1}");
            }

            obj.IsInPool = false;
            obj.OnGetFromPool();
            _onGet?.Invoke(obj);
            ActiveCount++;
            return obj;
        }

        public void Return(T obj)
        {
            if (obj == null || obj.IsInPool)
            {
                GameLog.Error(LOG_TAG, "Return", $"试图归还空对象或已在池中的对象 - 类型:{typeof(T).Name}");
                return;
            }

            if (_pool.Count >= _maxSize)
            {
                // 池已满，直接丢弃
                GameLog.Warning(LOG_TAG, "Return", $"池已满({_maxSize})，丢弃对象 - 类型:{typeof(T).Name}");
                return;
            }

            obj.IsInPool = true;
            obj.OnReturnToPool();
            _onReturn?.Invoke(obj);
            _pool.Push(obj);
            ActiveCount--;
            GameLog.Debug(LOG_TAG, "Return", $"归还对象到池中 - 类型:{typeof(T).Name}, 当前池大小:{Count}, 活动对象:{ActiveCount}");
        }

        public void Clear()
        {
            _pool.Clear();
            ActiveCount = 0;
        }
    }
}
