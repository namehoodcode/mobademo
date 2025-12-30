
// CollisionJob.cs
// 使用Unity Job System并行计算碰撞检测
// 核心思想：将碰撞检测任务分解到多个核心上并行执行，充分利用CPU性能
//
// 注意：此文件需要安装 Unity Collections 和 Burst 包才能使用
// 如果没有安装这些包，请使用 GameManager 中的简单碰撞检测实现

#if UNITY_COLLECTIONS_INSTALLED && UNITY_BURST_INSTALLED
using MobaCombatCore.Core.Math;
using MobaCombatCore.Core.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace MobaCombatCore.Optimization.Jobs
{
    [BurstCompile]
    public struct CollisionJob : IJobParallelFor
    {
        // 输入
        [ReadOnly] public NativeArray<Circle> Projectiles;
        [ReadOnly] public NativeArray<Circle> Targets;

        // 输出
        // 使用NativeQueue而不是NativeList，因为多线程写入List不安全
        public NativeQueue<CollisionResult>.ParallelWriter CollisionResults;

        public void Execute(int index)
        {
            var projectile = Projectiles[index];
            if (!projectile.Enabled) return;

            for (int i = 0; i < Targets.Length; i++)
            {
                var target = Targets[i];
                if (!target.Enabled) continue;

                // 简单的队伍检查，避免友军伤害
                if(projectile.TeamId == target.TeamId) continue;

                if (CollisionDetector.CircleVsCircle(projectile, target))
                {
                    var result = CollisionDetector.CircleVsCircleDetailed(projectile, target);
                    if (result.IsColliding)
                    {
                        CollisionResults.Enqueue(result);
                    }
                }
            }
        }
    }
}
#else
// 当 Unity Collections 或 Burst 包未安装时，提供一个空的占位符
namespace MobaCombatCore.Optimization.Jobs
{
    /// <summary>
    /// CollisionJob 占位符 - 需要安装 Unity Collections 和 Burst 包才能使用完整功能
    /// 当前使用 GameManager 中的简单碰撞检测实现
    /// </summary>
    public struct CollisionJob
    {
        // 占位符结构体，实际碰撞检测在 GameManager.RunCollisionDetection() 中实现
        //todo
    }
}
#endif
