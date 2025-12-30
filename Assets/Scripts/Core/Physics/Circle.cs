// Circle.cs - 圆形碰撞体
// 用于英雄、弹道等圆形碰撞检测
// 基于定点数，保证确定性

using System;
using MobaCombatCore.Core.Math;

namespace MobaCombatCore.Core.Physics
{
    /// <summary>
    /// 圆形碰撞体
    /// </summary>
    [Serializable]
    public struct Circle : IEquatable<Circle>
    {
        /// <summary>
        /// 圆心位置（2D，使用X和Z）
        /// </summary>
        public FixedVector3 Center;

        /// <summary>
        /// 半径
        /// </summary>
        public Fixed64 Radius;

        /// <summary>
        /// 所属实体ID（用于碰撞过滤）
        /// </summary>
        public int EntityId;

        /// <summary>
        /// 碰撞层（用于碰撞过滤）
        /// </summary>
        public int Layer;

        /// <summary>
        /// 实体队伍ID
        /// </summary>
        public int TeamId;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// 构造函数
        /// </summary>
        public Circle(FixedVector3 center, Fixed64 radius, int entityId = 0, int layer = 0, int teamId = 0)
        {
            Center = center;
            Radius = radius;
            EntityId = entityId;
            Layer = layer;
            TeamId = teamId;
            Enabled = true;
        }

        /// <summary>
        /// 从整数创建
        /// </summary>
        public static Circle FromInt(int centerX, int centerZ, int radius, int entityId = 0)
        {
            return new Circle(
                FixedVector3.FromInt(centerX, 0, centerZ),
                Fixed64.FromInt(radius),
                entityId
            );
        }

        /// <summary>
        /// 从浮点数创建（仅用于配置）
        /// </summary>
        public static Circle FromFloat(float centerX, float centerZ, float radius, int entityId = 0)
        {
            return new Circle(
                FixedVector3.FromFloat(centerX, 0f, centerZ),
                Fixed64.FromFloat(radius),
                entityId
            );
        }

        /// <summary>
        /// 半径的平方（避免开方运算）
        /// </summary>
        public Fixed64 RadiusSqr => Radius * Radius;

        /// <summary>
        /// 检查点是否在圆内
        /// </summary>
        public bool ContainsPoint(FixedVector3 point)
        {
            Fixed64 dx = point.X - Center.X;
            Fixed64 dz = point.Z - Center.Z;
            Fixed64 distSqr = dx * dx + dz * dz;
            return distSqr <= RadiusSqr;
        }

        /// <summary>
        /// 检查点是否在圆内（2D坐标）
        /// </summary>
        public bool ContainsPoint2D(Fixed64 x, Fixed64 z)
        {
            Fixed64 dx = x - Center.X;
            Fixed64 dz = z - Center.Z;
            Fixed64 distSqr = dx * dx + dz * dz;
            return distSqr <= RadiusSqr;
        }

        /// <summary>
        /// 获取到点的距离
        /// </summary>
        public Fixed64 DistanceToPoint(FixedVector3 point)
        {
            return FixedVector3.Distance2D(Center, point);
        }

        /// <summary>
        /// 获取到点的距离平方
        /// </summary>
        public Fixed64 SqrDistanceToPoint(FixedVector3 point)
        {
            return FixedVector3.SqrDistance2D(Center, point);
        }

        /// <summary>
        /// 获取圆上最近的点
        /// </summary>
        public FixedVector3 ClosestPoint(FixedVector3 point)
        {
            FixedVector3 direction = new FixedVector3(
                point.X - Center.X,
                Fixed64.Zero,
                point.Z - Center.Z
            );

            Fixed64 distance = direction.Magnitude2D;
            if (distance.RawValue == 0)
            {
                // 点在圆心，返回任意边界点
                return new FixedVector3(Center.X + Radius, Center.Y, Center.Z);
            }

            FixedVector3 normalized = direction / distance;
            return new FixedVector3(
                Center.X + normalized.X * Radius,
                Center.Y,
                Center.Z + normalized.Z * Radius
            );
        }

        /// <summary>
        /// 获取包围盒
        /// </summary>
        public AABB GetBounds()
        {
            return new AABB(
                new FixedVector3(Center.X - Radius, Center.Y, Center.Z - Radius),
                new FixedVector3(Center.X + Radius, Center.Y, Center.Z + Radius),
                EntityId
            );
        }

        /// <summary>
        /// 移动圆心
        /// </summary>
        public void SetCenter(FixedVector3 newCenter)
        {
            Center = newCenter;
        }

        /// <summary>
        /// 偏移圆心
        /// </summary>
        public void Offset(FixedVector3 offset)
        {
            Center += offset;
        }

        /// <summary>
        /// 缩放半径
        /// </summary>
        public void Scale(Fixed64 scale)
        {
            Radius *= scale;
        }

        #region 接口实现

        public bool Equals(Circle other)
        {
            return Center == other.Center &&
                   Radius == other.Radius &&
                   EntityId == other.EntityId;
        }

        public override bool Equals(object obj)
        {
            return obj is Circle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Center.GetHashCode();
                hash = (hash * 397) ^ Radius.GetHashCode();
                hash = (hash * 397) ^ EntityId;
                return hash;
            }
        }

        public static bool operator ==(Circle a, Circle b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Circle a, Circle b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return $"Circle(Center:{Center}, Radius:{Radius.ToFloat():F2}, Entity:{EntityId})";
        }

        #endregion
    }
}
