using System;
using System.Runtime.CompilerServices;

namespace MobaCombatCore.Core.Math
{
    /// <summary>
    /// 64位定点数结构，用于确定性计算
    /// 内部使用long存储，精度为百万分之一（6位小数）
    /// 范围：±9,223,372,036（约92亿）
    /// </summary>
    [Serializable]
    public struct Fixed64 : IEquatable<Fixed64>, IComparable<Fixed64>
    {
        // 精度因子：1,000,000（百万分之一）
        public const long PRECISION = 1000000L;
        public const int FRACTIONAL_BITS = 20; // 用于位移运算的近似值

        // 内部原始值
        public long RawValue;

        #region 常用常量

        public static readonly Fixed64 Zero = new Fixed64(0L);
        public static readonly Fixed64 One = new Fixed64(PRECISION);
        public static readonly Fixed64 NegativeOne = new Fixed64(-PRECISION);
        public static readonly Fixed64 Half = new Fixed64(PRECISION / 2);
        public static readonly Fixed64 Two = new Fixed64(PRECISION * 2);
        public static readonly Fixed64 Pi = new Fixed64(3141593L);           // π ≈ 3.141593
        public static readonly Fixed64 TwoPi = new Fixed64(6283185L);        // 2π ≈ 6.283185
        public static readonly Fixed64 HalfPi = new Fixed64(1570796L);       // π/2 ≈ 1.570796
        public static readonly Fixed64 Deg2Rad = new Fixed64(17453L);        // π/180 ≈ 0.017453
        public static readonly Fixed64 Rad2Deg = new Fixed64(57295780L);     // 180/π ≈ 57.29578
        public static readonly Fixed64 Epsilon = new Fixed64(1L);            // 最小精度单位
        public static readonly Fixed64 MaxValue = new Fixed64(long.MaxValue);
        public static readonly Fixed64 MinValue = new Fixed64(long.MinValue);

        #endregion

        #region 构造函数

        /// <summary>
        /// 从原始long值构造
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed64(long rawValue)
        {
            RawValue = rawValue;
        }

        /// <summary>
        /// 从原始值创建Fixed64（公开方法）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 FromRaw(long rawValue)
        {
            return new Fixed64(rawValue);
        }

        /// <summary>
        /// 从整数创建
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 FromInt(int value)
        {
            return new Fixed64((long)value * PRECISION);
        }

        /// <summary>
        /// 从浮点数创建（仅用于初始化配置，运行时避免使用）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 FromFloat(float value)
        {
            return new Fixed64((long)(value * PRECISION));
        }

        /// <summary>
        /// 从双精度浮点数创建（仅用于初始化配置，运行时避免使用）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 FromDouble(double value)
        {
            return new Fixed64((long)(value * PRECISION));
        }

        #endregion

        #region 转换方法

        /// <summary>
        /// 转换为整数（截断小数部分）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToInt()
        {
            return (int)(RawValue / PRECISION);
        }

        /// <summary>
        /// 转换为浮点数（仅用于表现层显示）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ToFloat()
        {
            return (float)RawValue / PRECISION;
        }

        /// <summary>
        /// 转换为双精度浮点数（仅用于调试）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ToDouble()
        {
            return (double)RawValue / PRECISION;
        }

        #endregion

        #region 基本运算符

        /// <summary>
        /// 加法
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 operator +(Fixed64 a, Fixed64 b)
        {
            return new Fixed64(a.RawValue + b.RawValue);
        }

        /// <summary>
        /// 减法
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 operator -(Fixed64 a, Fixed64 b)
        {
            return new Fixed64(a.RawValue - b.RawValue);
        }

        /// <summary>
        /// 取负
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 operator -(Fixed64 a)
        {
            return new Fixed64(-a.RawValue);
        }

        /// <summary>
        /// 乘法（使用128位中间结果防止溢出）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 operator *(Fixed64 a, Fixed64 b)
        {
            // 使用decimal作为中间类型防止溢出
            // 对于更高性能可以使用128位整数运算
            long result = (long)(((decimal)a.RawValue * b.RawValue) / PRECISION);
            return new Fixed64(result);
        }

        /// <summary>
        /// 除法
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 operator /(Fixed64 a, Fixed64 b)
        {
            if (b.RawValue == 0)
            {
                throw new DivideByZeroException("Fixed64 division by zero");
            }
            // 使用decimal作为中间类型防止溢出
            long result = (long)(((decimal)a.RawValue * PRECISION) / b.RawValue);
            return new Fixed64(result);
        }

        /// <summary>
        /// 取模
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 operator %(Fixed64 a, Fixed64 b)
        {
            return new Fixed64(a.RawValue % b.RawValue);
        }

        /// <summary>
        /// 与整数相乘（快速运算）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 operator *(Fixed64 a, int b)
        {
            return new Fixed64(a.RawValue * b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 operator *(int a, Fixed64 b)
        {
            return new Fixed64(b.RawValue * a);
        }

        /// <summary>
        /// 与整数相除（快速运算）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 operator /(Fixed64 a, int b)
        {
            return new Fixed64(a.RawValue / b);
        }

        #endregion

        #region 比较运算符

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Fixed64 a, Fixed64 b)
        {
            return a.RawValue == b.RawValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Fixed64 a, Fixed64 b)
        {
            return a.RawValue != b.RawValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Fixed64 a, Fixed64 b)
        {
            return a.RawValue < b.RawValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Fixed64 a, Fixed64 b)
        {
            return a.RawValue > b.RawValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Fixed64 a, Fixed64 b)
        {
            return a.RawValue <= b.RawValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Fixed64 a, Fixed64 b)
        {
            return a.RawValue >= b.RawValue;
        }

        #endregion

        #region 隐式/显式转换

        public static implicit operator Fixed64(int value)
        {
            return FromInt(value);
        }

        public static explicit operator int(Fixed64 value)
        {
            return value.ToInt();
        }

        public static explicit operator float(Fixed64 value)
        {
            return value.ToFloat();
        }

        public static explicit operator double(Fixed64 value)
        {
            return value.ToDouble();
        }

        #endregion

        #region 数学函数

        /// <summary>
        /// 绝对值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 Abs(Fixed64 value)
        {
            return value.RawValue < 0 ? new Fixed64(-value.RawValue) : value;
        }

        /// <summary>
        /// 符号函数
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(Fixed64 value)
        {
            if (value.RawValue > 0) return 1;
            if (value.RawValue < 0) return -1;
            return 0;
        }

        /// <summary>
        /// 向下取整
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 Floor(Fixed64 value)
        {
            long remainder = value.RawValue % PRECISION;
            if (remainder < 0)
            {
                return new Fixed64(value.RawValue - remainder - PRECISION);
            }
            return new Fixed64(value.RawValue - remainder);
        }

        /// <summary>
        /// 向上取整
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 Ceiling(Fixed64 value)
        {
            long remainder = value.RawValue % PRECISION;
            if (remainder > 0)
            {
                return new Fixed64(value.RawValue - remainder + PRECISION);
            }
            if (remainder < 0)
            {
                return new Fixed64(value.RawValue - remainder);
            }
            return value;
        }

        /// <summary>
        /// 四舍五入
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 Round(Fixed64 value)
        {
            long remainder = value.RawValue % PRECISION;
            if (remainder >= PRECISION / 2)
            {
                return new Fixed64(value.RawValue - remainder + PRECISION);
            }
            if (remainder <= -PRECISION / 2)
            {
                return new Fixed64(value.RawValue - remainder - PRECISION);
            }
            return new Fixed64(value.RawValue - remainder);
        }

        /// <summary>
        /// 最小值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 Min(Fixed64 a, Fixed64 b)
        {
            return a.RawValue < b.RawValue ? a : b;
        }

        /// <summary>
        /// 最大值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 Max(Fixed64 a, Fixed64 b)
        {
            return a.RawValue > b.RawValue ? a : b;
        }

        /// <summary>
        /// 限制范围
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 Clamp(Fixed64 value, Fixed64 min, Fixed64 max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// 限制在0-1范围
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 Clamp01(Fixed64 value)
        {
            if (value.RawValue < 0) return Zero;
            if (value.RawValue > PRECISION) return One;
            return value;
        }

        /// <summary>
        /// 线性插值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 Lerp(Fixed64 a, Fixed64 b, Fixed64 t)
        {
            t = Clamp01(t);
            return a + (b - a) * t;
        }

        /// <summary>
        /// 无限制线性插值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 LerpUnclamped(Fixed64 a, Fixed64 b, Fixed64 t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// 平方根（牛顿迭代法）
        /// </summary>
        public static Fixed64 Sqrt(Fixed64 value)
        {
            if (value.RawValue < 0)
            {
                throw new ArgumentException("Cannot calculate square root of negative number");
            }

            if (value.RawValue == 0)
            {
                return Zero;
            }

            // 牛顿迭代法
            // x_{n+1} = (x_n + value/x_n) / 2
            long n = value.RawValue;
            long x = n;

            // 初始估计值
            while (x * x / PRECISION > n)
            {
                x = (x + n * PRECISION / x) / 2;
            }

            // 精确迭代
            for (int i = 0; i < 10; i++)
            {
                long nextX = (x + (long)((decimal)n * PRECISION / x)) / 2;
                if (nextX >= x) break;
                x = nextX;
            }

            return new Fixed64(x);
        }

        #endregion

        #region 接口实现

        public bool Equals(Fixed64 other)
        {
            return RawValue == other.RawValue;
        }

        public override bool Equals(object obj)
        {
            return obj is Fixed64 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return RawValue.GetHashCode();
        }

        public int CompareTo(Fixed64 other)
        {
            return RawValue.CompareTo(other.RawValue);
        }

        public override string ToString()
        {
            return ToDouble().ToString("F6");
        }

        public string ToString(string format)
        {
            return ToDouble().ToString(format);
        }

        #endregion
    }
}
