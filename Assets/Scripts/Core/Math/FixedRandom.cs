using System;

namespace MobaCombatCore.Core.Math
{
    /// <summary>
    /// 确定性随机数生成器
    /// 使用线性同余生成器（LCG）算法，保证相同种子产生相同序列
    /// 适用于帧同步游戏中的随机事件
    /// </summary>
    public class FixedRandom
    {
        // LCG参数（来自Numerical Recipes）
        private const long MULTIPLIER = 1103515245L;
        private const long INCREMENT = 12345L;
        private const long MODULUS = 2147483648L; // 2^31

        // 当前状态
        private long _state;

        // 初始种子（用于重置）
        private readonly long _initialSeed;

        #region 构造函数

        /// <summary>
        /// 使用指定种子创建随机数生成器
        /// </summary>
        public FixedRandom(int seed)
        {
            _initialSeed = seed;
            _state = seed;
        }

        /// <summary>
        /// 使用指定种子创建随机数生成器
        /// </summary>
        public FixedRandom(long seed)
        {
            _initialSeed = seed;
            _state = seed;
        }

        /// <summary>
        /// 使用当前时间戳创建随机数生成器（仅用于非确定性场景）
        /// </summary>
        public FixedRandom()
        {
            _initialSeed = DateTime.Now.Ticks;
            _state = _initialSeed;
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 获取当前状态（用于保存/恢复）
        /// </summary>
        public long State => _state;

        /// <summary>
        /// 设置状态（用于保存/恢复）
        /// </summary>
        public void SetState(long state)
        {
            _state = state;
        }

        /// <summary>
        /// 重置到初始种子
        /// </summary>
        public void Reset()
        {
            _state = _initialSeed;
        }

        /// <summary>
        /// 重置到新种子
        /// </summary>
        public void Reset(long seed)
        {
            _state = seed;
        }

        /// <summary>
        /// 生成下一个原始随机数（0到MODULUS-1）
        /// </summary>
        private long NextRaw()
        {
            _state = (_state * MULTIPLIER + INCREMENT) % MODULUS;
            return _state;
        }

        #endregion

        #region 整数随机

        /// <summary>
        /// 返回非负随机整数
        /// </summary>
        public int Next()
        {
            return (int)NextRaw();
        }

        /// <summary>
        /// 返回[0, maxValue)范围内的随机整数
        /// </summary>
        public int Next(int maxValue)
        {
            if (maxValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be positive");
            }
            return (int)(NextRaw() % maxValue);
        }

        /// <summary>
        /// 返回[minValue, maxValue)范围内的随机整数
        /// </summary>
        public int Next(int minValue, int maxValue)
        {
            if (minValue >= maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minValue), "minValue must be less than maxValue");
            }
            long range = (long)maxValue - minValue;
            return minValue + (int)(NextRaw() % range);
        }

        #endregion

        #region 定点数随机

        /// <summary>
        /// 返回[0, 1)范围内的随机定点数
        /// </summary>
        public Fixed64 NextFixed()
        {
            long raw = NextRaw();
            // 将[0, MODULUS)映射到[0, PRECISION)
            long fixedRaw = (raw * Fixed64.PRECISION) / MODULUS;
            return Fixed64.FromRaw(fixedRaw);
        }

        /// <summary>
        /// 返回[0, maxValue)范围内的随机定点数
        /// </summary>
        public Fixed64 NextFixed(Fixed64 maxValue)
        {
            return NextFixed() * maxValue;
        }

        /// <summary>
        /// 返回[minValue, maxValue)范围内的随机定点数
        /// </summary>
        public Fixed64 NextFixed(Fixed64 minValue, Fixed64 maxValue)
        {
            return minValue + NextFixed() * (maxValue - minValue);
        }

        /// <summary>
        /// 返回[-1, 1)范围内的随机定点数
        /// </summary>
        public Fixed64 NextFixedSigned()
        {
            return NextFixed() * Fixed64.Two - Fixed64.One;
        }

        #endregion

        #region 向量随机

        /// <summary>
        /// 返回单位圆内的随机点（2D，Y=0）
        /// </summary>
        public FixedVector3 InsideUnitCircle()
        {
            // 使用拒绝采样法
            FixedVector3 result;
            do
            {
                Fixed64 x = NextFixedSigned();
                Fixed64 z = NextFixedSigned();
                result = new FixedVector3(x, Fixed64.Zero, z);
            } while (result.SqrMagnitude2D > Fixed64.One);

            return result;
        }

        /// <summary>
        /// 返回单位圆上的随机点（2D，Y=0）
        /// </summary>
        public FixedVector3 OnUnitCircle()
        {
            Fixed64 angle = NextFixed() * Fixed64.TwoPi;
            return new FixedVector3(
                FixedMath.Cos(angle),
                Fixed64.Zero,
                FixedMath.Sin(angle)
            );
        }

        /// <summary>
        /// 返回单位球内的随机点
        /// </summary>
        public FixedVector3 InsideUnitSphere()
        {
            // 使用拒绝采样法
            FixedVector3 result;
            do
            {
                Fixed64 x = NextFixedSigned();
                Fixed64 y = NextFixedSigned();
                Fixed64 z = NextFixedSigned();
                result = new FixedVector3(x, y, z);
            } while (result.SqrMagnitude > Fixed64.One);

            return result;
        }

        /// <summary>
        /// 返回单位球面上的随机点
        /// </summary>
        public FixedVector3 OnUnitSphere()
        {
            // 使用拒绝采样法获取非零向量，然后归一化
            FixedVector3 result;
            do
            {
                Fixed64 x = NextFixedSigned();
                Fixed64 y = NextFixedSigned();
                Fixed64 z = NextFixedSigned();
                result = new FixedVector3(x, y, z);
            } while (result.SqrMagnitude.RawValue == 0);

            return result.Normalized;
        }

        /// <summary>
        /// 返回指定范围内的随机向量
        /// </summary>
        public FixedVector3 NextVector3(FixedVector3 min, FixedVector3 max)
        {
            return new FixedVector3(
                NextFixed(min.X, max.X),
                NextFixed(min.Y, max.Y),
                NextFixed(min.Z, max.Z)
            );
        }

        /// <summary>
        /// 返回圆环内的随机点（2D，Y=0）
        /// </summary>
        public FixedVector3 InsideAnnulus(Fixed64 innerRadius, Fixed64 outerRadius)
        {
            // 使用面积加权的随机半径
            Fixed64 innerSqr = innerRadius * innerRadius;
            Fixed64 outerSqr = outerRadius * outerRadius;
            Fixed64 radiusSqr = innerSqr + NextFixed() * (outerSqr - innerSqr);
            Fixed64 radius = Fixed64.Sqrt(radiusSqr);

            Fixed64 angle = NextFixed() * Fixed64.TwoPi;
            return new FixedVector3(
                radius * FixedMath.Cos(angle),
                Fixed64.Zero,
                radius * FixedMath.Sin(angle)
            );
        }

        /// <summary>
        /// 返回扇形内的随机点（2D，Y=0）
        /// </summary>
        public FixedVector3 InsideSector(Fixed64 radius, Fixed64 angleStart, Fixed64 angleEnd)
        {
            // 随机半径（面积加权）
            Fixed64 r = Fixed64.Sqrt(NextFixed()) * radius;
            // 随机角度
            Fixed64 angle = NextFixed(angleStart, angleEnd);

            return new FixedVector3(
                r * FixedMath.Cos(angle),
                Fixed64.Zero,
                r * FixedMath.Sin(angle)
            );
        }

        #endregion

        #region 概率和选择

        /// <summary>
        /// 返回是否命中指定概率（0-1）
        /// </summary>
        public bool Chance(Fixed64 probability)
        {
            return NextFixed() < probability;
        }

        /// <summary>
        /// 返回是否命中指定百分比概率（0-100）
        /// </summary>
        public bool ChancePercent(int percent)
        {
            return Next(100) < percent;
        }

        /// <summary>
        /// 从数组中随机选择一个元素
        /// </summary>
        public T Choose<T>(T[] array)
        {
            if (array == null || array.Length == 0)
            {
                throw new ArgumentException("Array cannot be null or empty");
            }
            return array[Next(array.Length)];
        }

        /// <summary>
        /// 根据权重随机选择索引
        /// </summary>
        public int WeightedChoice(int[] weights)
        {
            if (weights == null || weights.Length == 0)
            {
                throw new ArgumentException("Weights cannot be null or empty");
            }

            int totalWeight = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                totalWeight += weights[i];
            }

            if (totalWeight <= 0)
            {
                throw new ArgumentException("Total weight must be positive");
            }

            int random = Next(totalWeight);
            int cumulative = 0;

            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (random < cumulative)
                {
                    return i;
                }
            }

            return weights.Length - 1;
        }

        /// <summary>
        /// 根据权重随机选择索引（定点数权重）
        /// </summary>
        public int WeightedChoice(Fixed64[] weights)
        {
            if (weights == null || weights.Length == 0)
            {
                throw new ArgumentException("Weights cannot be null or empty");
            }

            Fixed64 totalWeight = Fixed64.Zero;
            for (int i = 0; i < weights.Length; i++)
            {
                totalWeight = totalWeight + weights[i];
            }

            if (totalWeight.RawValue <= 0)
            {
                throw new ArgumentException("Total weight must be positive");
            }

            Fixed64 random = NextFixed(totalWeight);
            Fixed64 cumulative = Fixed64.Zero;

            for (int i = 0; i < weights.Length; i++)
            {
                cumulative = cumulative + weights[i];
                if (random < cumulative)
                {
                    return i;
                }
            }

            return weights.Length - 1;
        }

        /// <summary>
        /// Fisher-Yates洗牌算法
        /// </summary>
        public void Shuffle<T>(T[] array)
        {
            if (array == null) return;

            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

        #endregion

        #region 噪声和分布

        /// <summary>
        /// 高斯分布随机数（Box-Muller变换）
        /// </summary>
        public Fixed64 NextGaussian(Fixed64 mean, Fixed64 standardDeviation)
        {
            // Box-Muller变换
            Fixed64 u1 = NextFixed();
            Fixed64 u2 = NextFixed();

            // 避免log(0)
            if (u1.RawValue == 0) u1 = Fixed64.Epsilon;

            // z = sqrt(-2*ln(u1)) * cos(2*pi*u2)
            Fixed64 logU1 = FixedMath.Log(u1);
            Fixed64 sqrtPart = Fixed64.Sqrt(-Fixed64.Two * logU1);
            Fixed64 z = sqrtPart * FixedMath.Cos(Fixed64.TwoPi * u2);

            return mean + z * standardDeviation;
        }

        /// <summary>
        /// 泊松分布随机数
        /// </summary>
        public int NextPoisson(Fixed64 lambda)
        {
            // 使用逆变换采样
            Fixed64 L = FixedMath.Exp(-lambda);
            int k = 0;
            Fixed64 p = Fixed64.One;

            do
            {
                k++;
                p = p * NextFixed();
            } while (p > L);

            return k - 1;
        }

        #endregion
    }
}
