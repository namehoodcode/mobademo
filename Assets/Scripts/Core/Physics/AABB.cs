// AABB.cs - 轴对齐包围盒
// 用于技能范围、地形碰撞等矩形区域检测
// 基于定点数，保证确定性

using System;
using MobaCombatCore.Core.Math;

namespace MobaCombatCore.Core.Physics
{
    /// <summary>
    /// 轴对齐包围盒（Axis-Aligned Bounding Box）
    /// </summary>
    [Serializable]
    public struct AABB : IEquatable<AABB>
    {
        /// <summary>
        /// 最小点（左下角）
        /// </summary>
        public FixedVector3 Min;

        /// <summary>
        /// 最大点（右上角）
        /// </summary>
        public FixedVector3 Max;

        /// <summary>
        /// 所属实体ID
        /// </summary>
        public int EntityId;

        /// <summary>
        /// 碰撞层
        /// </summary>
        public int Layer;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// 从最小最大点构造
        /// </summary>
        public AABB(FixedVector3 min, FixedVector3 max, int entityId = 0, int layer = 0)
        {
            // 确保min和max正确
            Min = FixedVector3.Min(min, max);
            Max = FixedVector3.Max(min, max);
            EntityId = entityId;
            Layer = layer;
            Enabled = true;
        }

        /// <summary>
        /// 从中心和尺寸构造
        /// </summary>
        public static AABB FromCenterSize(FixedVector3 center, FixedVector3 size, int entityId = 0)
        {
            FixedVector3 halfSize = size / 2;
            return new AABB(center - halfSize, center + halfSize, entityId);
        }

        /// <summary>
        /// 从中心和半尺寸构造
        /// </summary>
        public static AABB FromCenterExtents(FixedVector3 center, FixedVector3 extents, int entityId = 0)
        {
            return new AABB(center - extents, center + extents, entityId);
        }

        /// <summary>
        /// 从整数创建
        /// </summary>
        public static AABB FromInt(int minX, int minZ, int maxX, int maxZ, int entityId = 0)
        {
            return new AABB(
                FixedVector3.FromInt(minX, 0, minZ),
                FixedVector3.FromInt(maxX, 0, maxZ),
                entityId
            );
        }

        /// <summary>
        /// 从浮点数创建（仅用于配置）
        /// </summary>
        public static AABB FromFloat(float minX, float minZ, float maxX, float maxZ, int entityId = 0)
        {
            return new AABB(
                FixedVector3.FromFloat(minX, 0f, minZ),
                FixedVector3.FromFloat(maxX, 0f, maxZ),
                entityId
            );
        }

        #region 属性

        /// <summary>
        /// 中心点
        /// </summary>
        public FixedVector3 Center => (Min + Max) / 2;

        /// <summary>
        /// 尺寸
        /// </summary>
        public FixedVector3 Size => Max - Min;

        /// <summary>
        /// 半尺寸（范围）
        /// </summary>
        public FixedVector3 Extents => Size / 2;

        /// <summary>
        /// 宽度（X轴）
        /// </summary>
        public Fixed64 Width => Max.X - Min.X;

        /// <summary>
        /// 深度（Z轴）
        /// </summary>
        public Fixed64 Depth => Max.Z - Min.Z;

        /// <summary>
        /// 高度（Y轴）
        /// </summary>
        public Fixed64 Height => Max.Y - Min.Y;

        /// <summary>
        /// 面积（2D，X*Z）
        /// </summary>
        public Fixed64 Area2D => Width * Depth;

        /// <summary>
        /// 周长（2D）
        /// </summary>
        public Fixed64 Perimeter2D => (Width + Depth) * 2;

        #endregion

        #region 碰撞检测

        /// <summary>
        /// 检查点是否在AABB内
        /// </summary>
        public bool ContainsPoint(FixedVector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }

        /// <summary>
        /// 检查点是否在AABB内（2D坐标）
        /// </summary>
        public bool ContainsPoint2D(Fixed64 x, Fixed64 z)
        {
            return x >= Min.X && x <= Max.X &&
                   z >= Min.Z && z <= Max.Z;
        }

        /// <summary>
        /// 检查是否完全包含另一个AABB
        /// </summary>
        public bool Contains(AABB other)
        {
            return other.Min.X >= Min.X && other.Max.X <= Max.X &&
                   other.Min.Z >= Min.Z && other.Max.Z <= Max.Z;
        }

        /// <summary>
        /// 检查是否与另一个AABB相交
        /// </summary>
        public bool Intersects(AABB other)
        {
            return Min.X <= other.Max.X && Max.X >= other.Min.X &&
                   Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
        }

        /// <summary>
        /// 获取与另一个AABB的交集
        /// </summary>
        public AABB? GetIntersection(AABB other)
        {
            if (!Intersects(other))
            {
                return null;
            }

            return new AABB(
                FixedVector3.Max(Min, other.Min),
                FixedVector3.Min(Max, other.Max)
            );
        }

        /// <summary>
        /// 获取包含两个AABB的最小AABB
        /// </summary>
        public static AABB Union(AABB a, AABB b)
        {
            return new AABB(
                FixedVector3.Min(a.Min, b.Min),
                FixedVector3.Max(a.Max, b.Max)
            );
        }

        /// <summary>
        /// 获取到点的最近点
        /// </summary>
        public FixedVector3 ClosestPoint(FixedVector3 point)
        {
            return new FixedVector3(
                Fixed64.Clamp(point.X, Min.X, Max.X),
                Fixed64.Clamp(point.Y, Min.Y, Max.Y),
                Fixed64.Clamp(point.Z, Min.Z, Max.Z)
            );
        }

        /// <summary>
        /// 获取到点的距离平方
        /// </summary>
        public Fixed64 SqrDistanceToPoint(FixedVector3 point)
        {
            Fixed64 sqrDist = Fixed64.Zero;

            // X轴
            if (point.X < Min.X)
            {
                Fixed64 d = Min.X - point.X;
                sqrDist += d * d;
            }
            else if (point.X > Max.X)
            {
                Fixed64 d = point.X - Max.X;
                sqrDist += d * d;
            }

            // Z轴
            if (point.Z < Min.Z)
            {
                Fixed64 d = Min.Z - point.Z;
                sqrDist += d * d;
            }
            else if (point.Z > Max.Z)
            {
                Fixed64 d = point.Z - Max.Z;
                sqrDist += d * d;
            }

            return sqrDist;
        }

        /// <summary>
        /// 获取到点的距离
        /// </summary>
        public Fixed64 DistanceToPoint(FixedVector3 point)
        {
            return Fixed64.Sqrt(SqrDistanceToPoint(point));
        }

        #endregion

        #region 变换

        /// <summary>
        /// 扩展AABB
        /// </summary>
        public AABB Expand(Fixed64 amount)
        {
            FixedVector3 expansion = new FixedVector3(amount, amount, amount);
            return new AABB(Min - expansion, Max + expansion, EntityId, Layer);
        }

        /// <summary>
        /// 扩展AABB以包含点
        /// </summary>
        public AABB ExpandToInclude(FixedVector3 point)
        {
            return new AABB(
                FixedVector3.Min(Min, point),
                FixedVector3.Max(Max, point),
                EntityId,
                Layer
            );
        }

        /// <summary>
        /// 移动AABB
        /// </summary>
        public AABB Translate(FixedVector3 offset)
        {
            return new AABB(Min + offset, Max + offset, EntityId, Layer);
        }

        /// <summary>
        /// 设置中心位置
        /// </summary>
        public AABB SetCenter(FixedVector3 newCenter)
        {
            FixedVector3 halfSize = Extents;
            return new AABB(newCenter - halfSize, newCenter + halfSize, EntityId, Layer);
        }

        #endregion

        #region 接口实现

        public bool Equals(AABB other)
        {
            return Min == other.Min &&
                   Max == other.Max &&
                   EntityId == other.EntityId;
        }

        public override bool Equals(object obj)
        {
            return obj is AABB other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Min.GetHashCode();
                hash = (hash * 397) ^ Max.GetHashCode();
                hash = (hash * 397) ^ EntityId;
                return hash;
            }
        }

        public static bool operator ==(AABB a, AABB b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(AABB a, AABB b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return $"AABB(Min:{Min}, Max:{Max}, Entity:{EntityId})";
        }

        #endregion
    }
}
