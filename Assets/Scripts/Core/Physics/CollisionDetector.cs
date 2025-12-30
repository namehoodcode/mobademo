// CollisionDetector.cs - 碰撞检测器
// 提供各种碰撞体之间的碰撞检测
// 纯静态方法，无状态，基于定点数保证确定性

using System;
using System.Runtime.CompilerServices;
using MobaCombatCore.Core.Math;

namespace MobaCombatCore.Core.Physics
{
    /// <summary>
    /// 碰撞结果
    /// </summary>
    public struct CollisionResult
    {
        /// <summary>
        /// 是否发生碰撞
        /// </summary>
        public bool IsColliding;

        /// <summary>
        /// 碰撞点
        /// </summary>
        public FixedVector3 ContactPoint;

        /// <summary>
        /// 碰撞法线（从A指向B）
        /// </summary>
        public FixedVector3 Normal;

        /// <summary>
        /// 穿透深度
        /// </summary>
        public Fixed64 PenetrationDepth;

        /// <summary>
        /// 实体A的ID
        /// </summary>
        public int EntityIdA;

        /// <summary>
        /// 实体B的ID
        /// </summary>
        public int EntityIdB;

        /// <summary>
        /// 无碰撞结果
        /// </summary>
        public static CollisionResult None => new CollisionResult { IsColliding = false };

        /// <summary>
        /// 创建碰撞结果
        /// </summary>
        public static CollisionResult Create(FixedVector3 contactPoint, FixedVector3 normal,
            Fixed64 penetration, int entityIdA = 0, int entityIdB = 0)
        {
            return new CollisionResult
            {
                IsColliding = true,
                ContactPoint = contactPoint,
                Normal = normal,
                PenetrationDepth = penetration,
                EntityIdA = entityIdA,
                EntityIdB = entityIdB
            };
        }
    }

    /// <summary>
    /// 碰撞检测器 - 静态工具类
    /// </summary>
    public static class CollisionDetector
    {
        #region Circle vs Circle

        /// <summary>
        /// 圆形与圆形碰撞检测
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CircleVsCircle(Circle a, Circle b)
        {
            if (!a.Enabled || !b.Enabled) return false;

            Fixed64 dx = b.Center.X - a.Center.X;
            Fixed64 dz = b.Center.Z - a.Center.Z;
            Fixed64 distSqr = dx * dx + dz * dz;
            Fixed64 radiusSum = a.Radius + b.Radius;

            return distSqr <= radiusSum * radiusSum;
        }

        /// <summary>
        /// 圆形与圆形碰撞检测（带详细结果）
        /// </summary>
        public static CollisionResult CircleVsCircleDetailed(Circle a, Circle b)
        {
            if (!a.Enabled || !b.Enabled) return CollisionResult.None;

            Fixed64 dx = b.Center.X - a.Center.X;
            Fixed64 dz = b.Center.Z - a.Center.Z;
            Fixed64 distSqr = dx * dx + dz * dz;
            Fixed64 radiusSum = a.Radius + b.Radius;
            Fixed64 radiusSumSqr = radiusSum * radiusSum;

            if (distSqr > radiusSumSqr)
            {
                return CollisionResult.None;
            }

            Fixed64 dist = Fixed64.Sqrt(distSqr);
            FixedVector3 normal;

            if (dist.RawValue == 0)
            {
                // 圆心重合，使用默认方向
                normal = FixedVector3.Right;
            }
            else
            {
                normal = new FixedVector3(dx / dist, Fixed64.Zero, dz / dist);
            }

            Fixed64 penetration = radiusSum - dist;
            FixedVector3 contactPoint = new FixedVector3(
                a.Center.X + normal.X * a.Radius,
                Fixed64.Zero,
                a.Center.Z + normal.Z * a.Radius
            );

            return CollisionResult.Create(contactPoint, normal, penetration, a.EntityId, b.EntityId);
        }

        /// <summary>
        /// 圆形与圆形碰撞检测（使用位置和半径）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CircleVsCircle(FixedVector3 centerA, Fixed64 radiusA,
            FixedVector3 centerB, Fixed64 radiusB)
        {
            Fixed64 dx = centerB.X - centerA.X;
            Fixed64 dz = centerB.Z - centerA.Z;
            Fixed64 distSqr = dx * dx + dz * dz;
            Fixed64 radiusSum = radiusA + radiusB;

            return distSqr <= radiusSum * radiusSum;
        }

        #endregion

        #region Circle vs AABB

        /// <summary>
        /// 圆形与AABB碰撞检测
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CircleVsAABB(Circle circle, AABB aabb)
        {
            if (!circle.Enabled || !aabb.Enabled) return false;

            // 找到AABB上离圆心最近的点
            Fixed64 closestX = Fixed64.Clamp(circle.Center.X, aabb.Min.X, aabb.Max.X);
            Fixed64 closestZ = Fixed64.Clamp(circle.Center.Z, aabb.Min.Z, aabb.Max.Z);

            // 计算距离
            Fixed64 dx = circle.Center.X - closestX;
            Fixed64 dz = circle.Center.Z - closestZ;
            Fixed64 distSqr = dx * dx + dz * dz;

            return distSqr <= circle.RadiusSqr;
        }

        /// <summary>
        /// 圆形与AABB碰撞检测（带详细结果）
        /// </summary>
        public static CollisionResult CircleVsAABBDetailed(Circle circle, AABB aabb)
        {
            if (!circle.Enabled || !aabb.Enabled) return CollisionResult.None;

            // 找到AABB上离圆心最近的点
            Fixed64 closestX = Fixed64.Clamp(circle.Center.X, aabb.Min.X, aabb.Max.X);
            Fixed64 closestZ = Fixed64.Clamp(circle.Center.Z, aabb.Min.Z, aabb.Max.Z);

            Fixed64 dx = circle.Center.X - closestX;
            Fixed64 dz = circle.Center.Z - closestZ;
            Fixed64 distSqr = dx * dx + dz * dz;

            if (distSqr > circle.RadiusSqr)
            {
                return CollisionResult.None;
            }

            FixedVector3 contactPoint = new FixedVector3(closestX, Fixed64.Zero, closestZ);
            FixedVector3 normal;
            Fixed64 penetration;

            if (distSqr.RawValue == 0)
            {
                // 圆心在AABB内部
                // 找到最近的边
                Fixed64 distToLeft = circle.Center.X - aabb.Min.X;
                Fixed64 distToRight = aabb.Max.X - circle.Center.X;
                Fixed64 distToBottom = circle.Center.Z - aabb.Min.Z;
                Fixed64 distToTop = aabb.Max.Z - circle.Center.Z;

                Fixed64 minDist = Fixed64.Min(Fixed64.Min(distToLeft, distToRight),
                    Fixed64.Min(distToBottom, distToTop));

                if (minDist == distToLeft)
                {
                    normal = FixedVector3.Left;
                    penetration = circle.Radius + distToLeft;
                }
                else if (minDist == distToRight)
                {
                    normal = FixedVector3.Right;
                    penetration = circle.Radius + distToRight;
                }
                else if (minDist == distToBottom)
                {
                    normal = FixedVector3.Back;
                    penetration = circle.Radius + distToBottom;
                }
                else
                {
                    normal = FixedVector3.Forward;
                    penetration = circle.Radius + distToTop;
                }
            }
            else
            {
                Fixed64 dist = Fixed64.Sqrt(distSqr);
                normal = new FixedVector3(dx / dist, Fixed64.Zero, dz / dist);
                penetration = circle.Radius - dist;
            }

            return CollisionResult.Create(contactPoint, normal, penetration, circle.EntityId, aabb.EntityId);
        }

        #endregion

        #region AABB vs AABB

        /// <summary>
        /// AABB与AABB碰撞检测
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AABBVsAABB(AABB a, AABB b)
        {
            if (!a.Enabled || !b.Enabled) return false;

            return a.Min.X <= b.Max.X && a.Max.X >= b.Min.X &&
                   a.Min.Z <= b.Max.Z && a.Max.Z >= b.Min.Z;
        }

        /// <summary>
        /// AABB与AABB碰撞检测（带详细结果）
        /// </summary>
        public static CollisionResult AABBVsAABBDetailed(AABB a, AABB b)
        {
            if (!a.Enabled || !b.Enabled) return CollisionResult.None;

            if (!AABBVsAABB(a, b))
            {
                return CollisionResult.None;
            }

            // 计算重叠量
            Fixed64 overlapX = Fixed64.Min(a.Max.X, b.Max.X) - Fixed64.Max(a.Min.X, b.Min.X);
            Fixed64 overlapZ = Fixed64.Min(a.Max.Z, b.Max.Z) - Fixed64.Max(a.Min.Z, b.Min.Z);

            FixedVector3 normal;
            Fixed64 penetration;

            // 选择最小重叠轴
            if (overlapX < overlapZ)
            {
                penetration = overlapX;
                normal = a.Center.X < b.Center.X ? FixedVector3.Left : FixedVector3.Right;
            }
            else
            {
                penetration = overlapZ;
                normal = a.Center.Z < b.Center.Z ? FixedVector3.Back : FixedVector3.Forward;
            }

            // 计算接触点（两个AABB中心的中点）
            FixedVector3 contactPoint = (a.Center + b.Center) / 2;

            return CollisionResult.Create(contactPoint, normal, penetration, a.EntityId, b.EntityId);
        }

        #endregion

        #region Point Tests

        /// <summary>
        /// 点是否在圆内
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInCircle(FixedVector3 point, Circle circle)
        {
            return circle.ContainsPoint(point);
        }

        /// <summary>
        /// 点是否在AABB内
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInAABB(FixedVector3 point, AABB aabb)
        {
            return aabb.ContainsPoint(point);
        }

        /// <summary>
        /// 点是否在扇形内（用于技能范围检测）
        /// </summary>
        public static bool PointInSector(FixedVector3 point, FixedVector3 origin,
            Fixed64 direction, Fixed64 radius, Fixed64 halfAngle)
        {
            // 检查距离
            Fixed64 dx = point.X - origin.X;
            Fixed64 dz = point.Z - origin.Z;
            Fixed64 distSqr = dx * dx + dz * dz;

            if (distSqr > radius * radius)
            {
                return false;
            }

            if (distSqr.RawValue == 0)
            {
                return true; // 点在原点
            }

            // 检查角度
            Fixed64 dist = Fixed64.Sqrt(distSqr);
            Fixed64 pointAngle = FixedMath.Atan2(dx, dz);
            Fixed64 angleDiff = FixedMath.DeltaAngle(direction, pointAngle);

            return Fixed64.Abs(angleDiff) <= halfAngle;
        }

        #endregion

        #region Ray Tests

        /// <summary>
        /// 射线与圆的相交检测
        /// </summary>
        public static bool RayVsCircle(FixedVector3 rayOrigin, FixedVector3 rayDirection,
            Circle circle, out Fixed64 distance)
        {
            distance = Fixed64.Zero;

            FixedVector3 toCircle = new FixedVector3(
                circle.Center.X - rayOrigin.X,
                Fixed64.Zero,
                circle.Center.Z - rayOrigin.Z
            );

            // 投影到射线方向
            Fixed64 proj = toCircle.X * rayDirection.X + toCircle.Z * rayDirection.Z;

            if (proj < Fixed64.Zero)
            {
                // 圆在射线后方
                return false;
            }

            // 计算最近点到圆心的距离平方
            Fixed64 distSqr = toCircle.SqrMagnitude2D - proj * proj;

            if (distSqr > circle.RadiusSqr)
            {
                return false;
            }

            // 计算交点距离
            Fixed64 offset = Fixed64.Sqrt(circle.RadiusSqr - distSqr);
            distance = proj - offset;

            return distance >= Fixed64.Zero;
        }

        /// <summary>
        /// 射线与AABB的相交检测
        /// </summary>
        public static bool RayVsAABB(FixedVector3 rayOrigin, FixedVector3 rayDirection,
            AABB aabb, out Fixed64 distance)
        {
            distance = Fixed64.Zero;

            Fixed64 tMin = Fixed64.MinValue;
            Fixed64 tMax = Fixed64.MaxValue;

            // X轴
            if (rayDirection.X.RawValue != 0)
            {
                Fixed64 invD = Fixed64.One / rayDirection.X;
                Fixed64 t1 = (aabb.Min.X - rayOrigin.X) * invD;
                Fixed64 t2 = (aabb.Max.X - rayOrigin.X) * invD;

                if (t1 > t2)
                {
                    (t1, t2) = (t2, t1);
                }

                tMin = Fixed64.Max(tMin, t1);
                tMax = Fixed64.Min(tMax, t2);

                if (tMin > tMax)
                {
                    return false;
                }
            }
            else if (rayOrigin.X < aabb.Min.X || rayOrigin.X > aabb.Max.X)
            {
                return false;
            }

            // Z轴
            if (rayDirection.Z.RawValue != 0)
            {
                Fixed64 invD = Fixed64.One / rayDirection.Z;
                Fixed64 t1 = (aabb.Min.Z - rayOrigin.Z) * invD;
                Fixed64 t2 = (aabb.Max.Z - rayOrigin.Z) * invD;

                if (t1 > t2)
                {
                    (t1, t2) = (t2, t1);
                }

                tMin = Fixed64.Max(tMin, t1);
                tMax = Fixed64.Min(tMax, t2);

                if (tMin > tMax)
                {
                    return false;
                }
            }
            else if (rayOrigin.Z < aabb.Min.Z || rayOrigin.Z > aabb.Max.Z)
            {
                return false;
            }

            distance = tMin >= Fixed64.Zero ? tMin : tMax;
            return distance >= Fixed64.Zero;
        }

        #endregion

        #region Distance Calculations

        /// <summary>
        /// 两个圆之间的距离（边缘到边缘）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 DistanceBetweenCircles(Circle a, Circle b)
        {
            Fixed64 centerDist = FixedVector3.Distance2D(a.Center, b.Center);
            return centerDist - a.Radius - b.Radius;
        }

        /// <summary>
        /// 圆到AABB的距离
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 DistanceCircleToAABB(Circle circle, AABB aabb)
        {
            Fixed64 sqrDist = aabb.SqrDistanceToPoint(circle.Center);
            return Fixed64.Sqrt(sqrDist) - circle.Radius;
        }

        #endregion

        #region Overlap Tests

        /// <summary>
        /// 获取圆形范围内的所有圆形（用于AOE技能）
        /// </summary>
        public static int OverlapCircle(FixedVector3 center, Fixed64 radius,
            Circle[] circles, int[] results, int maxResults)
        {
            int count = 0;
            Fixed64 radiusSqr = radius * radius;

            for (int i = 0; i < circles.Length && count < maxResults; i++)
            {
                if (!circles[i].Enabled) continue;

                Fixed64 dx = circles[i].Center.X - center.X;
                Fixed64 dz = circles[i].Center.Z - center.Z;
                Fixed64 distSqr = dx * dx + dz * dz;

                Fixed64 combinedRadius = radius + circles[i].Radius;
                if (distSqr <= combinedRadius * combinedRadius)
                {
                    results[count++] = i;
                }
            }

            return count;
        }

        /// <summary>
        /// 获取AABB范围内的所有圆形
        /// </summary>
        public static int OverlapAABB(AABB bounds, Circle[] circles, int[] results, int maxResults)
        {
            int count = 0;

            for (int i = 0; i < circles.Length && count < maxResults; i++)
            {
                if (!circles[i].Enabled) continue;

                if (CircleVsAABB(circles[i], bounds))
                {
                    results[count++] = i;
                }
            }

            return count;
        }

        #endregion
    }
}
