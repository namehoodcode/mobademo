using System;
using System.Runtime.CompilerServices;

namespace MobaCombatCore.Core.Math
{
    /// <summary>
    /// 定点数三维向量，用于确定性位置和方向计算
    /// 所有运算基于Fixed64，保证跨平台一致性
    /// </summary>
    [Serializable]
    public struct FixedVector3 : IEquatable<FixedVector3>
    {
        public Fixed64 X;
        public Fixed64 Y;
        public Fixed64 Z;

        #region 常用常量

        public static readonly FixedVector3 Zero = new FixedVector3(Fixed64.Zero, Fixed64.Zero, Fixed64.Zero);
        public static readonly FixedVector3 One = new FixedVector3(Fixed64.One, Fixed64.One, Fixed64.One);
        public static readonly FixedVector3 Up = new FixedVector3(Fixed64.Zero, Fixed64.One, Fixed64.Zero);
        public static readonly FixedVector3 Down = new FixedVector3(Fixed64.Zero, Fixed64.NegativeOne, Fixed64.Zero);
        public static readonly FixedVector3 Left = new FixedVector3(Fixed64.NegativeOne, Fixed64.Zero, Fixed64.Zero);
        public static readonly FixedVector3 Right = new FixedVector3(Fixed64.One, Fixed64.Zero, Fixed64.Zero);
        public static readonly FixedVector3 Forward = new FixedVector3(Fixed64.Zero, Fixed64.Zero, Fixed64.One);
        public static readonly FixedVector3 Back = new FixedVector3(Fixed64.Zero, Fixed64.Zero, Fixed64.NegativeOne);

        #endregion

        #region 构造函数

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedVector3(Fixed64 x, Fixed64 y, Fixed64 z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedVector3(Fixed64 x, Fixed64 y)
        {
            X = x;
            Y = y;
            Z = Fixed64.Zero;
        }

        /// <summary>
        /// 从整数创建
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 FromInt(int x, int y, int z)
        {
            return new FixedVector3(
                Fixed64.FromInt(x),
                Fixed64.FromInt(y),
                Fixed64.FromInt(z)
            );
        }

        /// <summary>
        /// 从浮点数创建（仅用于初始化配置）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 FromFloat(float x, float y, float z)
        {
            return new FixedVector3(
                Fixed64.FromFloat(x),
                Fixed64.FromFloat(y),
                Fixed64.FromFloat(z)
            );
        }

        #endregion

        #region 属性

        /// <summary>
        /// 向量长度的平方（避免开方运算）
        /// </summary>
        public Fixed64 SqrMagnitude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => X * X + Y * Y + Z * Z;
        }

        /// <summary>
        /// 向量长度
        /// </summary>
        public Fixed64 Magnitude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Fixed64.Sqrt(SqrMagnitude);
        }

        /// <summary>
        /// 归一化向量
        /// </summary>
        public FixedVector3 Normalized
        {
            get
            {
                Fixed64 mag = Magnitude;
                if (mag.RawValue == 0)
                {
                    return Zero;
                }
                return this / mag;
            }
        }

        /// <summary>
        /// 2D长度平方（忽略Y轴，用于地面距离计算）
        /// </summary>
        public Fixed64 SqrMagnitude2D
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => X * X + Z * Z;
        }

        /// <summary>
        /// 2D长度（忽略Y轴）
        /// </summary>
        public Fixed64 Magnitude2D
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Fixed64.Sqrt(SqrMagnitude2D);
        }

        /// <summary>
        /// 2D归一化向量（忽略Y轴，Y保持为0）
        /// </summary>
        public FixedVector3 Normalized2D
        {
            get
            {
                Fixed64 mag = Magnitude2D;
                if (mag.RawValue == 0)
                {
                    return Zero;
                }
                return new FixedVector3(X / mag, Fixed64.Zero, Z / mag);
            }
        }

        #endregion

        #region 运算符

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 operator +(FixedVector3 a, FixedVector3 b)
        {
            return new FixedVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 operator -(FixedVector3 a, FixedVector3 b)
        {
            return new FixedVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 operator -(FixedVector3 a)
        {
            return new FixedVector3(-a.X, -a.Y, -a.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 operator *(FixedVector3 a, Fixed64 scalar)
        {
            return new FixedVector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 operator *(Fixed64 scalar, FixedVector3 a)
        {
            return new FixedVector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 operator *(FixedVector3 a, int scalar)
        {
            return new FixedVector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 operator /(FixedVector3 a, Fixed64 scalar)
        {
            return new FixedVector3(a.X / scalar, a.Y / scalar, a.Z / scalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 operator /(FixedVector3 a, int scalar)
        {
            return new FixedVector3(a.X / scalar, a.Y / scalar, a.Z / scalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FixedVector3 a, FixedVector3 b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FixedVector3 a, FixedVector3 b)
        {
            return a.X != b.X || a.Y != b.Y || a.Z != b.Z;
        }

        #endregion

        #region 向量运算

        /// <summary>
        /// 点积
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 Dot(FixedVector3 a, FixedVector3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        /// <summary>
        /// 叉积
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 Cross(FixedVector3 a, FixedVector3 b)
        {
            return new FixedVector3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );
        }

        /// <summary>
        /// 两点间距离
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 Distance(FixedVector3 a, FixedVector3 b)
        {
            return (a - b).Magnitude;
        }

        /// <summary>
        /// 两点间距离的平方（避免开方）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 SqrDistance(FixedVector3 a, FixedVector3 b)
        {
            return (a - b).SqrMagnitude;
        }

        /// <summary>
        /// 2D距离（忽略Y轴）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 Distance2D(FixedVector3 a, FixedVector3 b)
        {
            Fixed64 dx = a.X - b.X;
            Fixed64 dz = a.Z - b.Z;
            return Fixed64.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// 2D距离的平方（忽略Y轴）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 SqrDistance2D(FixedVector3 a, FixedVector3 b)
        {
            Fixed64 dx = a.X - b.X;
            Fixed64 dz = a.Z - b.Z;
            return dx * dx + dz * dz;
        }

        /// <summary>
        /// 线性插值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 Lerp(FixedVector3 a, FixedVector3 b, Fixed64 t)
        {
            t = Fixed64.Clamp01(t);
            return new FixedVector3(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t
            );
        }

        /// <summary>
        /// 无限制线性插值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 LerpUnclamped(FixedVector3 a, FixedVector3 b, Fixed64 t)
        {
            return new FixedVector3(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t
            );
        }

        /// <summary>
        /// 向目标移动（限制最大距离）
        /// </summary>
        public static FixedVector3 MoveTowards(FixedVector3 current, FixedVector3 target, Fixed64 maxDistanceDelta)
        {
            FixedVector3 direction = target - current;
            Fixed64 sqrDist = direction.SqrMagnitude;

            if (sqrDist == Fixed64.Zero || (maxDistanceDelta >= Fixed64.Zero && sqrDist <= maxDistanceDelta * maxDistanceDelta))
            {
                return target;
            }

            Fixed64 dist = Fixed64.Sqrt(sqrDist);
            return current + direction / dist * maxDistanceDelta;
        }

        /// <summary>
        /// 分量最小值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 Min(FixedVector3 a, FixedVector3 b)
        {
            return new FixedVector3(
                Fixed64.Min(a.X, b.X),
                Fixed64.Min(a.Y, b.Y),
                Fixed64.Min(a.Z, b.Z)
            );
        }

        /// <summary>
        /// 分量最大值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 Max(FixedVector3 a, FixedVector3 b)
        {
            return new FixedVector3(
                Fixed64.Max(a.X, b.X),
                Fixed64.Max(a.Y, b.Y),
                Fixed64.Max(a.Z, b.Z)
            );
        }

        /// <summary>
        /// 分量限制
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 Clamp(FixedVector3 value, FixedVector3 min, FixedVector3 max)
        {
            return new FixedVector3(
                Fixed64.Clamp(value.X, min.X, max.X),
                Fixed64.Clamp(value.Y, min.Y, max.Y),
                Fixed64.Clamp(value.Z, min.Z, max.Z)
            );
        }

        /// <summary>
        /// 限制向量长度
        /// </summary>
        public static FixedVector3 ClampMagnitude(FixedVector3 vector, Fixed64 maxLength)
        {
            Fixed64 sqrMag = vector.SqrMagnitude;
            if (sqrMag > maxLength * maxLength)
            {
                Fixed64 mag = Fixed64.Sqrt(sqrMag);
                return vector / mag * maxLength;
            }
            return vector;
        }

        /// <summary>
        /// 反射向量
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 Reflect(FixedVector3 direction, FixedVector3 normal)
        {
            Fixed64 factor = Fixed64.Two * Dot(direction, normal);
            return direction - normal * factor;
        }

        /// <summary>
        /// 投影到另一个向量上
        /// </summary>
        public static FixedVector3 Project(FixedVector3 vector, FixedVector3 onNormal)
        {
            Fixed64 sqrMag = onNormal.SqrMagnitude;
            if (sqrMag.RawValue == 0)
            {
                return Zero;
            }
            Fixed64 dot = Dot(vector, onNormal);
            return onNormal * dot / sqrMag;
        }

        /// <summary>
        /// 投影到平面上
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 ProjectOnPlane(FixedVector3 vector, FixedVector3 planeNormal)
        {
            return vector - Project(vector, planeNormal);
        }

        /// <summary>
        /// 分量乘法
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector3 Scale(FixedVector3 a, FixedVector3 b)
        {
            return new FixedVector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }

        /// <summary>
        /// 归一化（修改自身）
        /// </summary>
        public void Normalize()
        {
            Fixed64 mag = Magnitude;
            if (mag.RawValue > 0)
            {
                X = X / mag;
                Y = Y / mag;
                Z = Z / mag;
            }
        }

        /// <summary>
        /// 设置分量
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(Fixed64 x, Fixed64 y, Fixed64 z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        #endregion

        #region 接口实现

        public bool Equals(FixedVector3 other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is FixedVector3 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = X.GetHashCode();
                hash = (hash * 397) ^ Y.GetHashCode();
                hash = (hash * 397) ^ Z.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({X.ToFloat():F3}, {Y.ToFloat():F3}, {Z.ToFloat():F3})";
        }

        #endregion
    }
}
