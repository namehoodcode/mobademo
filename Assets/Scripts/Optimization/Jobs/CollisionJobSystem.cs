// CollisionJobSystem.cs
// 使用Unity Job System并行计算碰撞检测
// 使用Unity.Mathematics.FixedPoint库进行确定性定点数计算

using System.Collections.Generic;
using MobaCombatCore.Core.Math;
using MobaCombatCore.Gameplay.Entity;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

namespace MobaCombatCore.Optimization.Jobs
{
    /// <summary>
    /// 简化的圆形数据，用于Job System（blittable类型）
    /// 使用Unity.Mathematics.FixedPoint的fp类型确保确定性
    /// </summary>
    public struct CircleDataFp
    {
        public fp CenterX;
        public fp CenterZ;
        public fp Radius;
        public int EntityId;
        public int TeamId;
        public int Enabled; // 1 = enabled, 0 = disabled (使用int代替bool以确保blittable)
    }
    
    /// <summary>
    /// 浮点数版本的圆形数据（用于非确定性场景，性能更好）
    /// </summary>
    public struct CircleData
    {
        public float CenterX;
        public float CenterZ;
        public float Radius;
        public int EntityId;
        public int TeamId;
        public int Enabled;
    }

    /// <summary>
    /// 碰撞结果数据
    /// </summary>
    public struct CollisionHit
    {
        public int ProjectileIndex;
        public int TargetIndex;
        public int ProjectileEntityId;
        public int TargetEntityId;
    }

    /// <summary>
    /// 并行碰撞检测Job（定点数版本 - 确定性）
    /// 使用Unity.Mathematics.FixedPoint确保跨平台一致性
    /// </summary>
    [BurstCompile]
    public struct ParallelCollisionJobFp : IJobParallelFor
    {
        [ReadOnly] public NativeArray<CircleDataFp> Projectiles;
        [ReadOnly] public NativeArray<CircleDataFp> Targets;
        [ReadOnly] public int TargetCount; // 实际目标数量
        
        // 每个弹道最多命中一个目标
        public NativeArray<int> HitTargetIndex; // -1 表示未命中

        public void Execute(int projectileIndex)
        {
            var projectile = Projectiles[projectileIndex];
            if (projectile.Enabled == 0)
            {
                HitTargetIndex[projectileIndex] = -1;
                return;
            }

            for (int i = 0; i < TargetCount; i++)
            {
                var target = Targets[i];
                if (target.Enabled == 0) continue;
                if (target.TeamId == projectile.TeamId) continue; // 跳过友军

                // 圆形碰撞检测（定点数运算）
                fp dx = target.CenterX - projectile.CenterX;
                fp dz = target.CenterZ - projectile.CenterZ;
                fp distSqr = dx * dx + dz * dz;
                fp radiusSum = projectile.Radius + target.Radius;

                if (distSqr <= radiusSum * radiusSum)
                {
                    HitTargetIndex[projectileIndex] = i;
                    return; // 弹道只能命中一个目标
                }
            }

            HitTargetIndex[projectileIndex] = -1;
        }
    }
    
    /// <summary>
    /// 并行碰撞检测Job（浮点数版本 - 高性能）
    /// </summary>
    [BurstCompile]
    public struct ParallelCollisionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<CircleData> Projectiles;
        [ReadOnly] public NativeArray<CircleData> Targets;
        [ReadOnly] public int TargetCount;
        
        public NativeArray<int> HitTargetIndex;

        public void Execute(int projectileIndex)
        {
            var projectile = Projectiles[projectileIndex];
            if (projectile.Enabled == 0)
            {
                HitTargetIndex[projectileIndex] = -1;
                return;
            }

            for (int i = 0; i < TargetCount; i++)
            {
                var target = Targets[i];
                if (target.Enabled == 0) continue;
                if (target.TeamId == projectile.TeamId) continue;

                float dx = target.CenterX - projectile.CenterX;
                float dz = target.CenterZ - projectile.CenterZ;
                float distSqr = dx * dx + dz * dz;
                float radiusSum = projectile.Radius + target.Radius;

                if (distSqr <= radiusSum * radiusSum)
                {
                    HitTargetIndex[projectileIndex] = i;
                    return;
                }
            }

            HitTargetIndex[projectileIndex] = -1;
        }
    }

    /// <summary>
    /// 碰撞检测Job系统管理器
    /// 支持定点数（确定性）和浮点数（高性能）两种模式
    /// </summary>
    public class CollisionJobSystem
    {
        // 定点数版本数据
        private NativeArray<CircleDataFp> _projectileDataFp;
        private NativeArray<CircleDataFp> _targetDataFp;
        
        // 浮点数版本数据
        private NativeArray<CircleData> _projectileData;
        private NativeArray<CircleData> _targetData;
        
        private NativeArray<int> _hitResults;
        
        private bool _isInitialized;
        private int _maxProjectiles;
        private int _maxTargets;
        
        /// <summary>
        /// 是否使用定点数模式（确定性，用于帧同步）
        /// </summary>
        public bool UseDeterministicMode { get; set; } = true;

        public CollisionJobSystem(int maxProjectiles = 512, int maxTargets = 64)
        {
            _maxProjectiles = maxProjectiles;
            _maxTargets = maxTargets;
            
            // 分配定点数版本
            _projectileDataFp = new NativeArray<CircleDataFp>(maxProjectiles, Allocator.Persistent);
            _targetDataFp = new NativeArray<CircleDataFp>(maxTargets, Allocator.Persistent);
            
            // 分配浮点数版本
            _projectileData = new NativeArray<CircleData>(maxProjectiles, Allocator.Persistent);
            _targetData = new NativeArray<CircleData>(maxTargets, Allocator.Persistent);
            
            _hitResults = new NativeArray<int>(maxProjectiles, Allocator.Persistent);
            
            _isInitialized = true;
        }

        /// <summary>
        /// 执行碰撞检测
        /// </summary>
        public void RunCollisionDetection(
            IReadOnlyList<ProjectileEntity> projectiles,
            IReadOnlyList<BaseEntity> targets,
            List<CollisionHit> results)
        {
            if (UseDeterministicMode)
            {
                RunCollisionDetectionDeterministic(projectiles, targets, results);
            }
            else
            {
                RunCollisionDetectionFast(projectiles, targets, results);
            }
        }
        
        /// <summary>
        /// 定点数碰撞检测（确定性，用于帧同步）
        /// </summary>
        private void RunCollisionDetectionDeterministic(
            IReadOnlyList<ProjectileEntity> projectiles,
            IReadOnlyList<BaseEntity> targets,
            List<CollisionHit> results)
        {
            if (!_isInitialized) return;
            
            results.Clear();
            
            int projectileCount = Mathf.Min(projectiles.Count, _maxProjectiles);
            int targetCount = Mathf.Min(targets.Count, _maxTargets);
            
            if (projectileCount == 0 || targetCount == 0) return;

            // 填充弹道数据（转换为Unity.Mathematics.FixedPoint的fp类型）
            for (int i = 0; i < projectileCount; i++)
            {
                var p = projectiles[i];
                _projectileDataFp[i] = new CircleDataFp
                {
                    // 从自定义Fixed64转换为fp
                    // 注意：Fixed64使用PRECISION=1000000，fp使用不同的内部表示
                    // 使用浮点数作为中间转换确保精度正确
                    CenterX = (fp)p.Position.X.ToFloat(),
                    CenterZ = (fp)p.Position.Z.ToFloat(),
                    Radius = (fp)p.Stats.CollisionRadius.ToFloat(),
                    EntityId = p.EntityId,
                    TeamId = p.TeamId,
                    Enabled = (!p.IsDestroyed && !p.IsDestroyPending) ? 1 : 0
                };
            }

            // 填充目标数据
            for (int i = 0; i < targetCount; i++)
            {
                var t = targets[i];
                _targetDataFp[i] = new CircleDataFp
                {
                    CenterX = (fp)t.Position.X.ToFloat(),
                    CenterZ = (fp)t.Position.Z.ToFloat(),
                    Radius = (fp)t.Stats.CollisionRadius.ToFloat(),
                    EntityId = t.EntityId,
                    TeamId = t.TeamId,
                    Enabled = (!t.IsDestroyed && !t.IsDestroyPending && !(t is ProjectileEntity)) ? 1 : 0
                };
            }

            // 创建并调度定点数Job
            var job = new ParallelCollisionJobFp
            {
                Projectiles = _projectileDataFp,
                Targets = _targetDataFp,
                TargetCount = targetCount,
                HitTargetIndex = _hitResults
            };

            JobHandle handle = job.Schedule(projectileCount, 4);
            handle.Complete();

            // 收集结果
            CollectResults(projectileCount, targetCount, results);
        }
        
        /// <summary>
        /// 浮点数碰撞检测（高性能，用于非确定性场景）
        /// </summary>
        private void RunCollisionDetectionFast(
            IReadOnlyList<ProjectileEntity> projectiles,
            IReadOnlyList<BaseEntity> targets,
            List<CollisionHit> results)
        {
            if (!_isInitialized) return;
            
            results.Clear();
            
            int projectileCount = Mathf.Min(projectiles.Count, _maxProjectiles);
            int targetCount = Mathf.Min(targets.Count, _maxTargets);
            
            if (projectileCount == 0 || targetCount == 0) return;

            // 填充弹道数据（浮点数版本）
            for (int i = 0; i < projectileCount; i++)
            {
                var p = projectiles[i];
                _projectileData[i] = new CircleData
                {
                    CenterX = p.Position.X.ToFloat(),
                    CenterZ = p.Position.Z.ToFloat(),
                    Radius = p.Stats.CollisionRadius.ToFloat(),
                    EntityId = p.EntityId,
                    TeamId = p.TeamId,
                    Enabled = (!p.IsDestroyed && !p.IsDestroyPending) ? 1 : 0
                };
            }

            // 填充目标数据
            for (int i = 0; i < targetCount; i++)
            {
                var t = targets[i];
                _targetData[i] = new CircleData
                {
                    CenterX = t.Position.X.ToFloat(),
                    CenterZ = t.Position.Z.ToFloat(),
                    Radius = t.Stats.CollisionRadius.ToFloat(),
                    EntityId = t.EntityId,
                    TeamId = t.TeamId,
                    Enabled = (!t.IsDestroyed && !t.IsDestroyPending && !(t is ProjectileEntity)) ? 1 : 0
                };
            }

            // 创建并调度浮点数Job
            var job = new ParallelCollisionJob
            {
                Projectiles = _projectileData,
                Targets = _targetData,
                TargetCount = targetCount,
                HitTargetIndex = _hitResults
            };

            JobHandle handle = job.Schedule(projectileCount, 4);
            handle.Complete();

            // 收集结果
            CollectResultsFloat(projectileCount, targetCount, results);
        }
        
        /// <summary>
        /// 收集定点数版本的碰撞结果
        /// </summary>
        private void CollectResults(int projectileCount, int targetCount, List<CollisionHit> results)
        {
            for (int i = 0; i < projectileCount; i++)
            {
                int hitIndex = _hitResults[i];
                if (hitIndex >= 0 && hitIndex < targetCount)
                {
                    results.Add(new CollisionHit
                    {
                        ProjectileIndex = i,
                        TargetIndex = hitIndex,
                        ProjectileEntityId = _projectileDataFp[i].EntityId,
                        TargetEntityId = _targetDataFp[hitIndex].EntityId
                    });
                }
            }
        }
        
        /// <summary>
        /// 收集浮点数版本的碰撞结果
        /// </summary>
        private void CollectResultsFloat(int projectileCount, int targetCount, List<CollisionHit> results)
        {
            for (int i = 0; i < projectileCount; i++)
            {
                int hitIndex = _hitResults[i];
                if (hitIndex >= 0 && hitIndex < targetCount)
                {
                    results.Add(new CollisionHit
                    {
                        ProjectileIndex = i,
                        TargetIndex = hitIndex,
                        ProjectileEntityId = _projectileData[i].EntityId,
                        TargetEntityId = _targetData[hitIndex].EntityId
                    });
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isInitialized)
            {
                if (_projectileDataFp.IsCreated) _projectileDataFp.Dispose();
                if (_targetDataFp.IsCreated) _targetDataFp.Dispose();
                if (_projectileData.IsCreated) _projectileData.Dispose();
                if (_targetData.IsCreated) _targetData.Dispose();
                if (_hitResults.IsCreated) _hitResults.Dispose();
                _isInitialized = false;
            }
        }
    }
}