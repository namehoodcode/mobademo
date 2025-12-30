using System;
using System.Runtime.CompilerServices;

namespace MobaCombatCore.Core.Math
{
    /// <summary>
    /// 定点数数学函数库
    /// 提供三角函数、指数函数等高级数学运算
    /// 使用查找表+插值实现，保证确定性
    /// </summary>
    public static class FixedMath
    {
        #region 三角函数查找表

        // 查找表大小（0-90度，每度4个采样点）
        private const int SIN_TABLE_SIZE = 361;
        private static readonly long[] SinTable;
        private static readonly long[] CosTable;

        // 查找表精度：每个单位代表0.25度
        private const int TABLE_PRECISION = 4;
        private const int DEGREES_90 = 90 * TABLE_PRECISION;
        private const int DEGREES_180 = 180 * TABLE_PRECISION;
        private const int DEGREES_270 = 270 * TABLE_PRECISION;
        private const int DEGREES_360 = 360 * TABLE_PRECISION;

        static FixedMath()
        {
            // 初始化三角函数查找表
            SinTable = new long[SIN_TABLE_SIZE];
            CosTable = new long[SIN_TABLE_SIZE];

            for (int i = 0; i < SIN_TABLE_SIZE; i++)
            {
                double angle = i * System.Math.PI / (180.0 * TABLE_PRECISION);
                SinTable[i] = (long)(System.Math.Sin(angle) * Fixed64.PRECISION);
                CosTable[i] = (long)(System.Math.Cos(angle) * Fixed64.PRECISION);
            }
        }

        #endregion

        #region 三角函数

        /// <summary>
        /// 正弦函数（输入为弧度）
        /// </summary>
        public static Fixed64 Sin(Fixed64 radians)
        {
            // 转换为度数（0-360范围）
            Fixed64 degrees = radians * Fixed64.Rad2Deg;
            return SinDegrees(degrees);
        }

        /// <summary>
        /// 余弦函数（输入为弧度）
        /// </summary>
        public static Fixed64 Cos(Fixed64 radians)
        {
            // 转换为度数
            Fixed64 degrees = radians * Fixed64.Rad2Deg;
            return CosDegrees(degrees);
        }

        /// <summary>
        /// 正切函数（输入为弧度）
        /// </summary>
        public static Fixed64 Tan(Fixed64 radians)
        {
            Fixed64 cos = Cos(radians);
            if (cos.RawValue == 0)
            {
                return cos.RawValue >= 0 ? Fixed64.MaxValue : Fixed64.MinValue;
            }
            return Sin(radians) / cos;
        }

        /// <summary>
        /// 正弦函数（输入为度数）
        /// </summary>
        public static Fixed64 SinDegrees(Fixed64 degrees)
        {
            // 归一化到0-360度
            long rawDegrees = degrees.RawValue / (Fixed64.PRECISION / TABLE_PRECISION);
            rawDegrees = ((rawDegrees % DEGREES_360) + DEGREES_360) % DEGREES_360;

            int index;
            bool negative = false;

            if (rawDegrees <= DEGREES_90)
            {
                index = (int)rawDegrees;
            }
            else if (rawDegrees <= DEGREES_180)
            {
                index = (int)(DEGREES_180 - rawDegrees);
            }
            else if (rawDegrees <= DEGREES_270)
            {
                index = (int)(rawDegrees - DEGREES_180);
                negative = true;
            }
            else
            {
                index = (int)(DEGREES_360 - rawDegrees);
                negative = true;
            }

            if (index >= SIN_TABLE_SIZE) index = SIN_TABLE_SIZE - 1;
            if (index < 0) index = 0;

            long result = SinTable[index];
            return Fixed64.FromRaw(negative ? -result : result);
        }

        /// <summary>
        /// 余弦函数（输入为度数）
        /// </summary>
        public static Fixed64 CosDegrees(Fixed64 degrees)
        {
            // cos(x) = sin(x + 90)
            return SinDegrees(degrees + Fixed64.FromInt(90));
        }

        /// <summary>
        /// 反正弦函数（返回弧度）
        /// 使用泰勒级数近似
        /// </summary>
        public static Fixed64 Asin(Fixed64 value)
        {
            // 限制输入范围
            value = Fixed64.Clamp(value, Fixed64.NegativeOne, Fixed64.One);

            // 使用泰勒级数近似
            // asin(x) ≈ x + x³/6 + 3x⁵/40 + 15x⁷/336 + ...
            Fixed64 x = value;
            Fixed64 x2 = x * x;
            Fixed64 x3 = x2 * x;
            Fixed64 x5 = x3 * x2;
            Fixed64 x7 = x5 * x2;

            Fixed64 result = x;
            result = result + x3 / 6;
            result = result + x5 * 3 / 40;
            result = result + x7 * 15 / 336;

            return result;
        }

        /// <summary>
        /// 反余弦函数（返回弧度）
        /// </summary>
        public static Fixed64 Acos(Fixed64 value)
        {
            return Fixed64.HalfPi - Asin(value);
        }

        /// <summary>
        /// 反正切函数（返回弧度）
        /// </summary>
        public static Fixed64 Atan(Fixed64 value)
        {
            // 使用泰勒级数近似（适用于|x| <= 1）
            // atan(x) ≈ x - x³/3 + x⁵/5 - x⁷/7 + ...
            bool negative = value < Fixed64.Zero;
            if (negative) value = -value;

            bool invert = value > Fixed64.One;
            if (invert) value = Fixed64.One / value;

            Fixed64 x = value;
            Fixed64 x2 = x * x;
            Fixed64 result = x;

            Fixed64 term = x * x2;
            result = result - term / 3;

            term = term * x2;
            result = result + term / 5;

            term = term * x2;
            result = result - term / 7;

            term = term * x2;
            result = result + term / 9;

            if (invert) result = Fixed64.HalfPi - result;
            if (negative) result = -result;

            return result;
        }

        /// <summary>
        /// 二参数反正切函数（返回弧度）
        /// 返回从X轴正方向到点(x,y)的角度
        /// </summary>
        public static Fixed64 Atan2(Fixed64 y, Fixed64 x)
        {
            if (x.RawValue > 0)
            {
                return Atan(y / x);
            }
            if (x.RawValue < 0)
            {
                if (y.RawValue >= 0)
                {
                    return Atan(y / x) + Fixed64.Pi;
                }
                return Atan(y / x) - Fixed64.Pi;
            }
            // x == 0
            if (y.RawValue > 0)
            {
                return Fixed64.HalfPi;
            }
            if (y.RawValue < 0)
            {
                return -Fixed64.HalfPi;
            }
            return Fixed64.Zero; // 未定义，返回0
        }

        #endregion

        #region 指数和对数函数

        /// <summary>
        /// 幂函数（整数指数）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 Pow(Fixed64 baseValue, int exponent)
        {
            if (exponent == 0) return Fixed64.One;
            if (exponent < 0)
            {
                baseValue = Fixed64.One / baseValue;
                exponent = -exponent;
            }

            Fixed64 result = Fixed64.One;
            while (exponent > 0)
            {
                if ((exponent & 1) == 1)
                {
                    result = result * baseValue;
                }
                baseValue = baseValue * baseValue;
                exponent >>= 1;
            }
            return result;
        }

        /// <summary>
        /// 幂函数（定点数指数，使用近似算法）
        /// </summary>
        public static Fixed64 Pow(Fixed64 baseValue, Fixed64 exponent)
        {
            if (exponent == Fixed64.Zero) return Fixed64.One;
            if (baseValue == Fixed64.Zero) return Fixed64.Zero;
            if (baseValue == Fixed64.One) return Fixed64.One;

            // 使用 a^b = e^(b*ln(a)) 近似
            // 简化实现：只支持整数部分
            int intPart = exponent.ToInt();
            Fixed64 fracPart = exponent - Fixed64.FromInt(intPart);

            Fixed64 result = Pow(baseValue, intPart);

            // 小数部分使用线性插值近似
            if (fracPart.RawValue != 0)
            {
                Fixed64 nextPow = result * baseValue;
                result = Fixed64.Lerp(result, nextPow, fracPart);
            }

            return result;
        }

        /// <summary>
        /// 自然对数（近似实现）
        /// </summary>
        public static Fixed64 Log(Fixed64 value)
        {
            if (value.RawValue <= 0)
            {
                throw new ArgumentException("Logarithm of non-positive number");
            }

            // 使用泰勒级数近似 ln(1+x) = x - x²/2 + x³/3 - ...
            // 先将值归一化到 [1, 2) 范围
            int exp = 0;
            long raw = value.RawValue;

            while (raw >= 2 * Fixed64.PRECISION)
            {
                raw /= 2;
                exp++;
            }
            while (raw < Fixed64.PRECISION)
            {
                raw *= 2;
                exp--;
            }

            // 现在 raw 在 [PRECISION, 2*PRECISION) 范围
            Fixed64 x = Fixed64.FromRaw(raw) - Fixed64.One; // x 在 [0, 1) 范围

            // 泰勒级数
            Fixed64 result = x;
            Fixed64 term = x * x;
            result = result - term / 2;
            term = term * x;
            result = result + term / 3;
            term = term * x;
            result = result - term / 4;
            term = term * x;
            result = result + term / 5;

            // 加上指数部分 (exp * ln(2))
            Fixed64 ln2 = Fixed64.FromRaw(693147L); // ln(2) ≈ 0.693147
            result = result + ln2 * exp;

            return result;
        }

        /// <summary>
        /// 以10为底的对数
        /// </summary>
        public static Fixed64 Log10(Fixed64 value)
        {
            Fixed64 ln10 = Fixed64.FromRaw(2302585L); // ln(10) ≈ 2.302585
            return Log(value) / ln10;
        }

        /// <summary>
        /// 指数函数 e^x（近似实现）
        /// </summary>
        public static Fixed64 Exp(Fixed64 value)
        {
            // 使用泰勒级数 e^x = 1 + x + x²/2! + x³/3! + ...
            // 限制输入范围以保证精度
            if (value.RawValue > 10 * Fixed64.PRECISION)
            {
                return Fixed64.MaxValue;
            }
            if (value.RawValue < -10 * Fixed64.PRECISION)
            {
                return Fixed64.Zero;
            }

            Fixed64 result = Fixed64.One;
            Fixed64 term = Fixed64.One;

            for (int i = 1; i <= 12; i++)
            {
                term = term * value / i;
                result = result + term;
            }

            return result;
        }

        #endregion

        #region 角度和方向

        /// <summary>
        /// 角度转弧度
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 DegToRad(Fixed64 degrees)
        {
            return degrees * Fixed64.Deg2Rad;
        }

        /// <summary>
        /// 弧度转角度
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed64 RadToDeg(Fixed64 radians)
        {
            return radians * Fixed64.Rad2Deg;
        }

        /// <summary>
        /// 计算两个2D向量之间的角度（返回弧度）
        /// </summary>
        public static Fixed64 Angle2D(FixedVector3 from, FixedVector3 to)
        {
            Fixed64 dx = to.X - from.X;
            Fixed64 dz = to.Z - from.Z;
            return Atan2(dz, dx);
        }

        /// <summary>
        /// 计算两个向量之间的夹角（返回弧度）
        /// </summary>
        public static Fixed64 AngleBetween(FixedVector3 from, FixedVector3 to)
        {
            Fixed64 dot = FixedVector3.Dot(from.Normalized, to.Normalized);
            dot = Fixed64.Clamp(dot, Fixed64.NegativeOne, Fixed64.One);
            return Acos(dot);
        }

        /// <summary>
        /// 计算两个向量之间的有符号角度（2D，返回度数）
        /// </summary>
        public static Fixed64 SignedAngle2D(FixedVector3 from, FixedVector3 to)
        {
            Fixed64 angle = Angle2D(FixedVector3.Zero, to) - Angle2D(FixedVector3.Zero, from);
            // 归一化到 -180 到 180 度
            while (angle > Fixed64.Pi) angle = angle - Fixed64.TwoPi;
            while (angle < -Fixed64.Pi) angle = angle + Fixed64.TwoPi;
            return angle * Fixed64.Rad2Deg;
        }

        /// <summary>
        /// 根据角度获取方向向量（2D，Y轴为0）
        /// </summary>
        public static FixedVector3 AngleToDirection2D(Fixed64 radians)
        {
            return new FixedVector3(Cos(radians), Fixed64.Zero, Sin(radians));
        }

        /// <summary>
        /// 根据度数获取方向向量（2D，Y轴为0）
        /// </summary>
        public static FixedVector3 DegreesToDirection2D(Fixed64 degrees)
        {
            Fixed64 radians = degrees * Fixed64.Deg2Rad;
            return new FixedVector3(CosDegrees(degrees), Fixed64.Zero, SinDegrees(degrees));
        }

        /// <summary>
        /// 绕Y轴旋转向量
        /// </summary>
        public static FixedVector3 RotateAroundY(FixedVector3 vector, Fixed64 radians)
        {
            Fixed64 cos = Cos(radians);
            Fixed64 sin = Sin(radians);
            return new FixedVector3(
                vector.X * cos - vector.Z * sin,
                vector.Y,
                vector.X * sin + vector.Z * cos
            );
        }

        #endregion

        #region 插值和平滑

        /// <summary>
        /// 平滑步进（Hermite插值）
        /// </summary>
        public static Fixed64 SmoothStep(Fixed64 from, Fixed64 to, Fixed64 t)
        {
            t = Fixed64.Clamp01(t);
            // t = t * t * (3 - 2 * t)
            t = t * t * (Fixed64.FromInt(3) - Fixed64.Two * t);
            return Fixed64.Lerp(from, to, t);
        }

        /// <summary>
        /// 更平滑的步进（五次Hermite插值）
        /// </summary>
        public static Fixed64 SmootherStep(Fixed64 from, Fixed64 to, Fixed64 t)
        {
            t = Fixed64.Clamp01(t);
            // t = t * t * t * (t * (t * 6 - 15) + 10)
            t = t * t * t * (t * (t * 6 - Fixed64.FromInt(15)) + Fixed64.FromInt(10));
            return Fixed64.Lerp(from, to, t);
        }

        /// <summary>
        /// 向目标值平滑移动
        /// </summary>
        public static Fixed64 MoveTowards(Fixed64 current, Fixed64 target, Fixed64 maxDelta)
        {
            if (Fixed64.Abs(target - current) <= maxDelta)
            {
                return target;
            }
            return current + Fixed64.FromInt(Fixed64.Sign(target - current)) * maxDelta;
        }

        /// <summary>
        /// 角度向目标平滑移动（处理环绕）
        /// </summary>
        public static Fixed64 MoveTowardsAngle(Fixed64 current, Fixed64 target, Fixed64 maxDelta)
        {
            Fixed64 delta = DeltaAngle(current, target);
            if (-maxDelta < delta && delta < maxDelta)
            {
                return target;
            }
            target = current + delta;
            return MoveTowards(current, target, maxDelta);
        }

        /// <summary>
        /// 计算两个角度之间的最短差值
        /// </summary>
        public static Fixed64 DeltaAngle(Fixed64 current, Fixed64 target)
        {
            Fixed64 delta = (target - current) % Fixed64.FromInt(360);
            if (delta > Fixed64.FromInt(180))
            {
                delta = delta - Fixed64.FromInt(360);
            }
            else if (delta < Fixed64.FromInt(-180))
            {
                delta = delta + Fixed64.FromInt(360);
            }
            return delta;
        }

        /// <summary>
        /// 弹簧阻尼平滑（简化版）
        /// </summary>
        public static Fixed64 SmoothDamp(Fixed64 current, Fixed64 target, ref Fixed64 velocity,
            Fixed64 smoothTime, Fixed64 deltaTime)
        {
            Fixed64 maxSpeed = Fixed64.FromInt(1000);
            return SmoothDamp(current, target, ref velocity, smoothTime, maxSpeed, deltaTime);
        }

        /// <summary>
        /// 弹簧阻尼平滑
        /// </summary>
        public static Fixed64 SmoothDamp(Fixed64 current, Fixed64 target, ref Fixed64 velocity,
            Fixed64 smoothTime, Fixed64 maxSpeed, Fixed64 deltaTime)
        {
            // 基于Game Programming Gems 4的实现
            smoothTime = Fixed64.Max(Fixed64.FromRaw(1000), smoothTime); // 最小0.001
            Fixed64 omega = Fixed64.Two / smoothTime;

            Fixed64 x = omega * deltaTime;
            Fixed64 exp = Fixed64.One / (Fixed64.One + x + Fixed64.FromRaw(480000) * x * x +
                Fixed64.FromRaw(235000) * x * x * x);

            Fixed64 change = current - target;
            Fixed64 originalTo = target;

            // 限制最大变化
            Fixed64 maxChange = maxSpeed * smoothTime;
            change = Fixed64.Clamp(change, -maxChange, maxChange);
            target = current - change;

            Fixed64 temp = (velocity + omega * change) * deltaTime;
            velocity = (velocity - omega * temp) * exp;
            Fixed64 output = target + (change + temp) * exp;

            // 防止过冲
            if ((originalTo - current > Fixed64.Zero) == (output > originalTo))
            {
                output = originalTo;
                velocity = (output - originalTo) / deltaTime;
            }

            return output;
        }

        #endregion

        #region 范围映射

        /// <summary>
        /// 将值从一个范围映射到另一个范围
        /// </summary>
        public static Fixed64 Remap(Fixed64 value, Fixed64 fromMin, Fixed64 fromMax,
            Fixed64 toMin, Fixed64 toMax)
        {
            Fixed64 fromRange = fromMax - fromMin;
            if (fromRange.RawValue == 0) return toMin;

            Fixed64 t = (value - fromMin) / fromRange;
            return toMin + (toMax - toMin) * t;
        }

        /// <summary>
        /// 将值从一个范围映射到另一个范围（限制输出范围）
        /// </summary>
        public static Fixed64 RemapClamped(Fixed64 value, Fixed64 fromMin, Fixed64 fromMax,
            Fixed64 toMin, Fixed64 toMax)
        {
            Fixed64 result = Remap(value, fromMin, fromMax, toMin, toMax);
            if (toMin < toMax)
            {
                return Fixed64.Clamp(result, toMin, toMax);
            }
            return Fixed64.Clamp(result, toMax, toMin);
        }

        /// <summary>
        /// 计算值在范围内的百分比位置
        /// </summary>
        public static Fixed64 InverseLerp(Fixed64 a, Fixed64 b, Fixed64 value)
        {
            if (a != b)
            {
                return Fixed64.Clamp01((value - a) / (b - a));
            }
            return Fixed64.Zero;
        }

        #endregion

        #region 几何计算

        /// <summary>
        /// 计算点到线段的最近点
        /// </summary>
        public static FixedVector3 ClosestPointOnLineSegment(FixedVector3 point,
            FixedVector3 lineStart, FixedVector3 lineEnd)
        {
            FixedVector3 line = lineEnd - lineStart;
            Fixed64 lineLengthSqr = line.SqrMagnitude;

            if (lineLengthSqr.RawValue == 0)
            {
                return lineStart;
            }

            Fixed64 t = FixedVector3.Dot(point - lineStart, line) / lineLengthSqr;
            t = Fixed64.Clamp01(t);

            return lineStart + line * t;
        }

        /// <summary>
        /// 计算点到线段的距离
        /// </summary>
        public static Fixed64 DistanceToLineSegment(FixedVector3 point,
            FixedVector3 lineStart, FixedVector3 lineEnd)
        {
            FixedVector3 closest = ClosestPointOnLineSegment(point, lineStart, lineEnd);
            return FixedVector3.Distance(point, closest);
        }

        /// <summary>
        /// 判断点是否在三角形内（2D，忽略Y轴）
        /// </summary>
        public static bool PointInTriangle2D(FixedVector3 point,
            FixedVector3 a, FixedVector3 b, FixedVector3 c)
        {
            Fixed64 d1 = Sign2D(point, a, b);
            Fixed64 d2 = Sign2D(point, b, c);
            Fixed64 d3 = Sign2D(point, c, a);

            bool hasNeg = (d1 < Fixed64.Zero) || (d2 < Fixed64.Zero) || (d3 < Fixed64.Zero);
            bool hasPos = (d1 > Fixed64.Zero) || (d2 > Fixed64.Zero) || (d3 > Fixed64.Zero);

            return !(hasNeg && hasPos);
        }

        private static Fixed64 Sign2D(FixedVector3 p1, FixedVector3 p2, FixedVector3 p3)
        {
            return (p1.X - p3.X) * (p2.Z - p3.Z) - (p2.X - p3.X) * (p1.Z - p3.Z);
        }

        #endregion
    }
}
