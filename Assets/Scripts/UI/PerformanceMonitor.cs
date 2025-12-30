// PerformanceMonitor.cs - 性能监控器
// 收集和统计游戏运行时的各项性能指标
// 无Unity依赖的核心逻辑，可被UI层调用
// 优化：减少foreach迭代器分配

using System;
using System.Collections.Generic;

namespace MobaCombatCore.UI
{
    /// <summary>
    /// 性能数据快照
    /// </summary>
    public struct PerformanceSnapshot
    {
        // 渲染性能
        public float RenderFPS;
        public float RenderFrameTime;

        // 逻辑性能
        public float LogicFPS;
        public float LogicFrameTime;
        public float AverageLogicFrameTime;
        public int CurrentLogicFrame;

        // 实体统计
        public int TotalEntities;
        public int ActiveEntities;
        public int ProjectileCount;
        public int HeroCount;
        public int DummyCount;

        // 内存统计
        public long TotalMemoryMB;
        public long GCAllocPerFrame;

        // 网络统计
        public int SimulatedDelayMs;
        public int RollbackCount;
        public int InputBufferSize;

        // 碰撞统计
        public int CollisionChecksPerFrame;
        public float CollisionTimeMs;

        // 时间戳
        public long Timestamp;
    }

    /// <summary>
    /// 性能监控器 - 收集和统计性能数据
    /// </summary>
    public class PerformanceMonitor
    {
        #region 单例

        private static PerformanceMonitor _instance;
        public static PerformanceMonitor Instance => _instance ??= new PerformanceMonitor();

        #endregion

        #region 配置

        /// <summary>
        /// 采样窗口大小（用于计算平均值）
        /// </summary>
        public int SampleWindowSize { get; set; } = 60;

        /// <summary>
        /// 是否启用监控
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        #endregion

        #region 渲染性能

        /// <summary>
        /// 使用固定大小数组替代Queue，避免迭代器分配
        /// </summary>
        private float[] _renderFrameTimesArray;
        private int _renderFrameTimesIndex;
        private int _renderFrameTimesCount;
        private float _lastRenderFrameTime;
        private int _renderFrameCount;
        private float _renderFPSAccumulator;
        private float _currentRenderFPS;

        /// <summary>
        /// 当前渲染FPS
        /// </summary>
        public float RenderFPS => _currentRenderFPS;

        /// <summary>
        /// 当前渲染帧时间（毫秒）
        /// </summary>
        public float RenderFrameTimeMs => _lastRenderFrameTime * 1000f;

        /// <summary>
        /// 记录渲染帧
        /// 优化：使用环形缓冲区替代Queue
        /// </summary>
        public void RecordRenderFrame(float deltaTime)
        {
            if (!IsEnabled) return;
            
            // 延迟初始化数组
            if (_renderFrameTimesArray == null)
            {
                _renderFrameTimesArray = new float[SampleWindowSize];
            }

            _lastRenderFrameTime = deltaTime;
            
            // 环形缓冲区写入
            _renderFrameTimesArray[_renderFrameTimesIndex] = deltaTime;
            _renderFrameTimesIndex = (_renderFrameTimesIndex + 1) % SampleWindowSize;
            if (_renderFrameTimesCount < SampleWindowSize)
            {
                _renderFrameTimesCount++;
            }

            // 计算FPS
            _renderFPSAccumulator += deltaTime;
            _renderFrameCount++;

            if (_renderFPSAccumulator >= 0.5f) // 每0.5秒更新一次FPS
            {
                _currentRenderFPS = _renderFrameCount / _renderFPSAccumulator;
                _renderFrameCount = 0;
                _renderFPSAccumulator = 0f;
            }
        }

        #endregion

        #region 逻辑性能

        /// <summary>
        /// 使用固定大小数组替代Queue，避免迭代器分配
        /// </summary>
        private float[] _logicFrameTimesArray;
        private int _logicFrameTimesIndex;
        private int _logicFrameTimesCount;
        private float _lastLogicFrameTime;
        private float _averageLogicFrameTime;
        private int _currentLogicFrame;
        private int _logicFrameRate = 30;

        /// <summary>
        /// 逻辑帧率
        /// </summary>
        public float LogicFPS => _logicFrameRate;

        /// <summary>
        /// 上一逻辑帧耗时（毫秒）
        /// </summary>
        public float LogicFrameTimeMs => _lastLogicFrameTime;

        /// <summary>
        /// 平均逻辑帧耗时（毫秒）
        /// </summary>
        public float AverageLogicFrameTimeMs => _averageLogicFrameTime;

        /// <summary>
        /// 当前逻辑帧号
        /// </summary>
        public int CurrentLogicFrame => _currentLogicFrame;

        /// <summary>
        /// 记录逻辑帧
        /// 优化：使用环形缓冲区替代Queue，使用for循环计算平均值
        /// </summary>
        public void RecordLogicFrame(int frameNumber, float frameTimeMs)
        {
            if (!IsEnabled) return;
            
            // 延迟初始化数组
            if (_logicFrameTimesArray == null)
            {
                _logicFrameTimesArray = new float[SampleWindowSize];
            }

            _currentLogicFrame = frameNumber;
            _lastLogicFrameTime = frameTimeMs;
            
            // 环形缓冲区写入
            _logicFrameTimesArray[_logicFrameTimesIndex] = frameTimeMs;
            _logicFrameTimesIndex = (_logicFrameTimesIndex + 1) % SampleWindowSize;
            if (_logicFrameTimesCount < SampleWindowSize)
            {
                _logicFrameTimesCount++;
            }

            // 计算平均值 - 使用for循环避免迭代器分配
            float sum = 0f;
            for (int i = 0; i < _logicFrameTimesCount; i++)
            {
                sum += _logicFrameTimesArray[i];
            }
            _averageLogicFrameTime = _logicFrameTimesCount > 0 ? sum / _logicFrameTimesCount : 0f;
        }

        /// <summary>
        /// 设置逻辑帧率
        /// </summary>
        public void SetLogicFrameRate(int fps)
        {
            _logicFrameRate = fps;
        }

        #endregion

        #region 实体统计

        private int _totalEntities;
        private int _activeEntities;
        private int _projectileCount;
        private int _heroCount;
        private int _dummyCount;

        public int TotalEntities => _totalEntities;
        public int ActiveEntities => _activeEntities;
        public int ProjectileCount => _projectileCount;
        public int HeroCount => _heroCount;
        public int DummyCount => _dummyCount;

        /// <summary>
        /// 更新实体统计
        /// </summary>
        public void UpdateEntityStats(int total, int active, int projectiles, int heroes, int dummies)
        {
            if (!IsEnabled) return;

            _totalEntities = total;
            _activeEntities = active;
            _projectileCount = projectiles;
            _heroCount = heroes;
            _dummyCount = dummies;
        }

        #endregion

        #region 内存统计

        private long _totalMemoryMB;
        private long _gcAllocPerFrame;
        private long _lastGCAlloc;

        public long TotalMemoryMB => _totalMemoryMB;
        public long GCAllocPerFrame => _gcAllocPerFrame;

        /// <summary>
        /// 更新内存统计
        /// </summary>
        public void UpdateMemoryStats(long totalMemoryBytes, long gcAllocBytes)
        {
            if (!IsEnabled) return;

            _totalMemoryMB = totalMemoryBytes / (1024 * 1024);
            _gcAllocPerFrame = gcAllocBytes - _lastGCAlloc;
            _lastGCAlloc = gcAllocBytes;
        }

        #endregion

        #region 网络统计

        private int _simulatedDelayMs;
        private int _rollbackCount;
        private int _inputBufferSize;

        public int SimulatedDelayMs => _simulatedDelayMs;
        public int RollbackCount => _rollbackCount;
        public int InputBufferSize => _inputBufferSize;

        /// <summary>
        /// 更新网络统计
        /// </summary>
        public void UpdateNetworkStats(int delayMs, int rollbacks, int bufferSize)
        {
            if (!IsEnabled) return;

            _simulatedDelayMs = delayMs;
            _rollbackCount = rollbacks;
            _inputBufferSize = bufferSize;
        }

        #endregion

        #region 碰撞统计

        private int _collisionChecksPerFrame;
        private float _collisionTimeMs;

        public int CollisionChecksPerFrame => _collisionChecksPerFrame;
        public float CollisionTimeMs => _collisionTimeMs;

        /// <summary>
        /// 更新碰撞统计
        /// </summary>
        public void UpdateCollisionStats(int checks, float timeMs)
        {
            if (!IsEnabled) return;

            _collisionChecksPerFrame = checks;
            _collisionTimeMs = timeMs;
        }

        #endregion

        #region 快照

        /// <summary>
        /// 获取当前性能快照
        /// </summary>
        public PerformanceSnapshot GetSnapshot()
        {
            return new PerformanceSnapshot
            {
                RenderFPS = _currentRenderFPS,
                RenderFrameTime = _lastRenderFrameTime * 1000f,
                LogicFPS = _logicFrameRate,
                LogicFrameTime = _lastLogicFrameTime,
                AverageLogicFrameTime = _averageLogicFrameTime,
                CurrentLogicFrame = _currentLogicFrame,
                TotalEntities = _totalEntities,
                ActiveEntities = _activeEntities,
                ProjectileCount = _projectileCount,
                HeroCount = _heroCount,
                DummyCount = _dummyCount,
                TotalMemoryMB = _totalMemoryMB,
                GCAllocPerFrame = _gcAllocPerFrame,
                SimulatedDelayMs = _simulatedDelayMs,
                RollbackCount = _rollbackCount,
                InputBufferSize = _inputBufferSize,
                CollisionChecksPerFrame = _collisionChecksPerFrame,
                CollisionTimeMs = _collisionTimeMs,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        #endregion

        #region 重置

        /// <summary>
        /// 重置所有统计数据
        /// </summary>
        public void Reset()
        {
            // 重置环形缓冲区
            _renderFrameTimesIndex = 0;
            _renderFrameTimesCount = 0;
            _logicFrameTimesIndex = 0;
            _logicFrameTimesCount = 0;
            
            // 清空数组内容（可选，但有助于调试）
            if (_renderFrameTimesArray != null)
            {
                System.Array.Clear(_renderFrameTimesArray, 0, _renderFrameTimesArray.Length);
            }
            if (_logicFrameTimesArray != null)
            {
                System.Array.Clear(_logicFrameTimesArray, 0, _logicFrameTimesArray.Length);
            }
            
            _renderFrameCount = 0;
            _renderFPSAccumulator = 0f;
            _currentRenderFPS = 0f;
            _lastRenderFrameTime = 0f;
            _lastLogicFrameTime = 0f;
            _averageLogicFrameTime = 0f;
            _currentLogicFrame = 0;
            _totalEntities = 0;
            _activeEntities = 0;
            _projectileCount = 0;
            _heroCount = 0;
            _dummyCount = 0;
            _totalMemoryMB = 0;
            _gcAllocPerFrame = 0;
            _lastGCAlloc = 0;
            _simulatedDelayMs = 0;
            _rollbackCount = 0;
            _inputBufferSize = 0;
            _collisionChecksPerFrame = 0;
            _collisionTimeMs = 0f;
        }

        #endregion
    }
}
