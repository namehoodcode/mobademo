// Snapshot.cs - 状态快照系统
// 用于帧同步系统，保存和恢复游戏状态（用于回滚）
// 无Unity依赖，保证确定性

using System;
using System.Collections.Generic;
using MobaCombatCore.Core.Math;

namespace MobaCombatCore.Core.Lockstep
{
    /// <summary>
    /// 实体状态快照
    /// </summary>
    public struct EntitySnapshot
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        public int EntityId;

        /// <summary>
        /// 实体类型
        /// </summary>
        public int EntityType;

        /// <summary>
        /// 位置
        /// </summary>
        public FixedVector3 Position;

        /// <summary>
        /// 旋转（欧拉角Y轴）
        /// </summary>
        public Fixed64 Rotation;

        /// <summary>
        /// 速度
        /// </summary>
        public FixedVector3 Velocity;

        /// <summary>
        /// 当前生命值
        /// </summary>
        public int CurrentHealth;

        /// <summary>
        /// 最大生命值
        /// </summary>
        public int MaxHealth;

        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive;

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive;

        /// <summary>
        /// 所属玩家ID
        /// </summary>
        public int OwnerId;

        /// <summary>
        /// 自定义数据（用于扩展）
        /// </summary>
        public long CustomData1;
        public long CustomData2;
        public long CustomData3;
        public long CustomData4;

        /// <summary>
        /// 计算快照哈希
        /// </summary>
        public long GetHash()
        {
            unchecked
            {
                long hash = 17;
                hash = hash * 31 + EntityId;
                hash = hash * 31 + EntityType;
                hash = hash * 31 + Position.GetHashCode();
                hash = hash * 31 + Rotation.GetHashCode();
                hash = hash * 31 + Velocity.GetHashCode();
                hash = hash * 31 + CurrentHealth;
                hash = hash * 31 + MaxHealth;
                hash = hash * 31 + (IsAlive ? 1 : 0);
                hash = hash * 31 + (IsActive ? 1 : 0);
                hash = hash * 31 + OwnerId;
                return hash;
            }
        }
    }

    /// <summary>
    /// 游戏状态快照 - 包含某一帧的完整游戏状态
    /// </summary>
    public class GameSnapshot
    {
        /// <summary>
        /// 快照对应的帧号
        /// </summary>
        public int FrameNumber { get; private set; }

        /// <summary>
        /// 快照创建时间戳
        /// </summary>
        public long Timestamp { get; private set; }

        /// <summary>
        /// 所有实体的快照
        /// </summary>
        public List<EntitySnapshot> EntitySnapshots { get; private set; }

        /// <summary>
        /// 随机数生成器状态
        /// </summary>
        public long RandomState { get; set; }

        /// <summary>
        /// 游戏时间（逻辑帧数）
        /// </summary>
        public int GameTime { get; set; }

        /// <summary>
        /// 状态哈希（用于同步校验）
        /// </summary>
        public long StateHash { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public GameSnapshot(int frameNumber)
        {
            FrameNumber = frameNumber;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            EntitySnapshots = new List<EntitySnapshot>();
            RandomState = 0;
            GameTime = frameNumber;
            StateHash = 0;
        }

        /// <summary>
        /// 添加实体快照
        /// </summary>
        public void AddEntitySnapshot(EntitySnapshot snapshot)
        {
            EntitySnapshots.Add(snapshot);
        }

        /// <summary>
        /// 获取指定实体的快照
        /// </summary>
        public EntitySnapshot? GetEntitySnapshot(int entityId)
        {
            foreach (var snapshot in EntitySnapshots)
            {
                if (snapshot.EntityId == entityId)
                {
                    return snapshot;
                }
            }
            return null;
        }

        /// <summary>
        /// 计算并更新状态哈希
        /// </summary>
        public void CalculateHash()
        {
            unchecked
            {
                long hash = 17;
                hash = hash * 31 + FrameNumber;
                hash = hash * 31 + RandomState;
                hash = hash * 31 + GameTime;

                foreach (var entitySnapshot in EntitySnapshots)
                {
                    hash = hash * 31 + entitySnapshot.GetHash();
                }

                StateHash = hash;
            }
        }

        /// <summary>
        /// 清空快照
        /// </summary>
        public void Clear()
        {
            EntitySnapshots.Clear();
            RandomState = 0;
            StateHash = 0;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        public GameSnapshot Clone()
        {
            var clone = new GameSnapshot(FrameNumber);
            clone.Timestamp = Timestamp;
            clone.RandomState = RandomState;
            clone.GameTime = GameTime;
            clone.StateHash = StateHash;

            foreach (var entitySnapshot in EntitySnapshots)
            {
                clone.EntitySnapshots.Add(entitySnapshot);
            }

            return clone;
        }

        public override string ToString()
        {
            return $"Snapshot[Frame:{FrameNumber}] Entities:{EntitySnapshots.Count} Hash:{StateHash:X16}";
        }
    }

    /// <summary>
    /// 快照管理器 - 管理快照的创建、存储和恢复
    /// </summary>
    public class SnapshotManager
    {
        /// <summary>
        /// 快照存储（按帧号索引）
        /// </summary>
        private readonly Dictionary<int, GameSnapshot> _snapshots;

        /// <summary>
        /// 最大保存的快照数量
        /// </summary>
        private readonly int _maxSnapshots;

        /// <summary>
        /// 关键帧间隔
        /// </summary>
        private readonly int _keyFrameInterval;

        /// <summary>
        /// 当前保存的快照数量
        /// </summary>
        public int SnapshotCount => _snapshots.Count;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxSnapshots">最大快照数量</param>
        /// <param name="keyFrameInterval">关键帧间隔</param>
        public SnapshotManager(int maxSnapshots = 30, int keyFrameInterval = 10)
        {
            _maxSnapshots = maxSnapshots;
            _keyFrameInterval = keyFrameInterval;
            _snapshots = new Dictionary<int, GameSnapshot>();
        }

        /// <summary>
        /// 检查是否应该保存快照
        /// </summary>
        public bool ShouldSaveSnapshot(int frameNumber)
        {
            return frameNumber % _keyFrameInterval == 0;
        }

        /// <summary>
        /// 保存快照
        /// </summary>
        public void SaveSnapshot(GameSnapshot snapshot)
        {
            if (snapshot == null) return;

            snapshot.CalculateHash();
            _snapshots[snapshot.FrameNumber] = snapshot;

            // 清理过旧的快照
            CleanupOldSnapshots();
        }

        /// <summary>
        /// 获取指定帧的快照
        /// </summary>
        public GameSnapshot GetSnapshot(int frameNumber)
        {
            if (_snapshots.TryGetValue(frameNumber, out var snapshot))
            {
                return snapshot;
            }
            return null;
        }

        /// <summary>
        /// 获取最近的快照（小于等于指定帧号）
        /// </summary>
        public GameSnapshot GetNearestSnapshot(int frameNumber)
        {
            GameSnapshot nearest = null;
            int nearestFrame = -1;

            foreach (var kvp in _snapshots)
            {
                if (kvp.Key <= frameNumber && kvp.Key > nearestFrame)
                {
                    nearestFrame = kvp.Key;
                    nearest = kvp.Value;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 删除指定帧之后的所有快照
        /// </summary>
        public void RemoveSnapshotsAfter(int frameNumber)
        {
            var framesToRemove = new List<int>();

            foreach (var kvp in _snapshots)
            {
                if (kvp.Key > frameNumber)
                {
                    framesToRemove.Add(kvp.Key);
                }
            }

            foreach (var frame in framesToRemove)
            {
                _snapshots.Remove(frame);
            }
        }

        /// <summary>
        /// 清空所有快照
        /// </summary>
        public void Clear()
        {
            _snapshots.Clear();
        }

        /// <summary>
        /// 清理过旧的快照
        /// </summary>
        private void CleanupOldSnapshots()
        {
            if (_snapshots.Count <= _maxSnapshots) return;

            // 找到最旧的快照并删除
            int oldestFrame = int.MaxValue;
            foreach (var kvp in _snapshots)
            {
                if (kvp.Key < oldestFrame)
                {
                    oldestFrame = kvp.Key;
                }
            }

            if (oldestFrame != int.MaxValue)
            {
                _snapshots.Remove(oldestFrame);
            }
        }

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            int minFrame = int.MaxValue;
            int maxFrame = int.MinValue;

            foreach (var kvp in _snapshots)
            {
                if (kvp.Key < minFrame) minFrame = kvp.Key;
                if (kvp.Key > maxFrame) maxFrame = kvp.Key;
            }

            if (_snapshots.Count == 0)
            {
                return "SnapshotManager: Empty";
            }

            return $"SnapshotManager: Count={_snapshots.Count}, Range=[{minFrame}-{maxFrame}]";
        }
    }
}
