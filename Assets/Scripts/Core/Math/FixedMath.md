# 定点数数学库 (Fixed Point Math Library)

## 概述

本数学库为MOBA核心战斗系统提供**确定性数学运算**支持。通过使用定点数替代浮点数，保证在不同平台、不同设备上的计算结果完全一致，这是帧同步游戏的核心需求。

### 为什么需要定点数？

| 问题 | 浮点数 | 定点数 |
|------|--------|--------|
| **跨平台一致性** | 不同CPU架构计算结果可能不同 | 完全一致 |
| **累积误差** | 30分钟游戏后误差可达数米 | 无累积误差 |
| **确定性** | 无法保证 | 完全确定 |
| **帧同步支持** | 不适合 | 完美支持 |

### 技术规格

| 参数 | 值 | 说明 |
|------|-----|------|
| **内部存储** | `long` (64位) | 足够大的范围 |
| **精度因子** | 1,000,000 | 百万分之一精度 |
| **小数位数** | 6位 | 满足游戏需求 |
| **数值范围** | ±9,223,372,036 | 约92亿 |

---

## 文件结构

```
Assets/Scripts/Core/Math/
├── Fixed64.cs          # 64位定点数核心结构
├── FixedVector3.cs     # 三维向量
├── FixedMath.cs        # 数学函数库
└── FixedRandom.cs      # 确定性随机数生成器
```

---

## Fixed64 - 定点数核心

### 基本用法

```csharp
using MobaCombat.Core.Math;

// 创建定点数
Fixed64 a = Fixed64.FromInt(5);           // 从整数创建
Fixed64 b = Fixed64.FromFloat(2.5f);      // 从浮点数创建（仅用于初始化）
Fixed64 c = Fixed64.FromRaw(1500000L);    // 从原始值创建（1.5）

// 基本运算
Fixed64 sum = a + b;        // 加法
Fixed64 diff = a - b;       // 减法
Fixed64 product = a * b;    // 乘法
Fixed64 quotient = a / b;   // 除法

// 转换输出（仅用于表现层）
float displayValue = sum.ToFloat();
int intValue = sum.ToInt();
```

### 常用常量

```csharp
Fixed64.Zero          // 0
Fixed64.One           // 1
Fixed64.Half          // 0.5
Fixed64.Two           // 2
Fixed64.Pi            // π ≈ 3.141593
Fixed64.TwoPi         // 2π
Fixed64.HalfPi        // π/2
Fixed64.Deg2Rad       // 度转弧度系数
Fixed64.Rad2Deg       // 弧度转度系数
Fixed64.Epsilon       // 最小精度单位
```

### 数学函数

```csharp
// 基本函数
Fixed64.Abs(value)              // 绝对值
Fixed64.Sign(value)             // 符号 (-1, 0, 1)
Fixed64.Min(a, b)               // 最小值
Fixed64.Max(a, b)               // 最大值
Fixed64.Clamp(value, min, max)  // 限制范围
Fixed64.Clamp01(value)          // 限制到0-1

// 取整函数
Fixed64.Floor(value)            // 向下取整
Fixed64.Ceiling(value)          // 向上取整
Fixed64.Round(value)            // 四舍五入

// 插值
Fixed64.Lerp(a, b, t)           // 线性插值（t限制在0-1）
Fixed64.LerpUnclamped(a, b, t)  // 无限制线性插值

// 平方根
Fixed64.Sqrt(value)             // 牛顿迭代法实现
```

---

## FixedVector3 - 三维向量

### 基本用法

```csharp
// 创建向量
FixedVector3 pos = new FixedVector3(
    Fixed64.FromFloat(10f),
    Fixed64.FromFloat(0f),
    Fixed64.FromFloat(5f)
);

// 快捷创建
FixedVector3 pos2 = FixedVector3.FromFloat(10f, 0f, 5f);
FixedVector3 pos3 = FixedVector3.FromInt(10, 0, 5);

// 向量运算
FixedVector3 sum = pos + pos2;
FixedVector3 diff = pos - pos2;
FixedVector3 scaled = pos * Fixed64.FromFloat(2f);
```

### 常用常量

```csharp
FixedVector3.Zero      // (0, 0, 0)
FixedVector3.One       // (1, 1, 1)
FixedVector3.Up        // (0, 1, 0)
FixedVector3.Down      // (0, -1, 0)
FixedVector3.Left      // (-1, 0, 0)
FixedVector3.Right     // (1, 0, 0)
FixedVector3.Forward   // (0, 0, 1)
FixedVector3.Back      // (0, 0, -1)
```

### 向量运算

```csharp
// 长度计算
Fixed64 length = vector.Magnitude;           // 向量长度
Fixed64 sqrLength = vector.SqrMagnitude;     // 长度平方（避免开方）
Fixed64 length2D = vector.Magnitude2D;       // 2D长度（忽略Y）

// 归一化
FixedVector3 normalized = vector.Normalized;
vector.Normalize();  // 修改自身

// 点积和叉积
Fixed64 dot = FixedVector3.Dot(a, b);
FixedVector3 cross = FixedVector3.Cross(a, b);

// 距离计算
Fixed64 dist = FixedVector3.Distance(a, b);
Fixed64 sqrDist = FixedVector3.SqrDistance(a, b);  // 避免开方
Fixed64 dist2D = FixedVector3.Distance2D(a, b);   // 2D距离

// 插值
FixedVector3 lerped = FixedVector3.Lerp(a, b, t);
FixedVector3 moved = FixedVector3.MoveTowards(current, target, maxDelta);

// 其他操作
FixedVector3 reflected = FixedVector3.Reflect(direction, normal);
FixedVector3 projected = FixedVector3.Project(vector, onNormal);
FixedVector3 clamped = FixedVector3.ClampMagnitude(vector, maxLength);
```

---

## FixedMath - 数学函数库

### 三角函数

```csharp
// 基本三角函数（输入为弧度）
Fixed64 sin = FixedMath.Sin(radians);
Fixed64 cos = FixedMath.Cos(radians);
Fixed64 tan = FixedMath.Tan(radians);

// 度数版本
Fixed64 sin = FixedMath.SinDegrees(degrees);
Fixed64 cos = FixedMath.CosDegrees(degrees);

// 反三角函数（返回弧度）
Fixed64 asin = FixedMath.Asin(value);
Fixed64 acos = FixedMath.Acos(value);
Fixed64 atan = FixedMath.Atan(value);
Fixed64 atan2 = FixedMath.Atan2(y, x);
```

### 角度和方向

```csharp
// 角度转换
Fixed64 rad = FixedMath.DegToRad(degrees);
Fixed64 deg = FixedMath.RadToDeg(radians);

// 方向计算
Fixed64 angle = FixedMath.Angle2D(from, to);           // 两点间角度
Fixed64 angle = FixedMath.AngleBetween(from, to);      // 向量夹角
Fixed64 signedAngle = FixedMath.SignedAngle2D(from, to);

// 方向向量
FixedVector3 dir = FixedMath.AngleToDirection2D(radians);
FixedVector3 dir = FixedMath.DegreesToDirection2D(degrees);

// 旋转
FixedVector3 rotated = FixedMath.RotateAroundY(vector, radians);
```

### 指数和对数

```csharp
Fixed64 pow = FixedMath.Pow(base, exponent);    // 幂函数
Fixed64 exp = FixedMath.Exp(value);             // e^x
Fixed64 log = FixedMath.Log(value);             // 自然对数
Fixed64 log10 = FixedMath.Log10(value);         // 以10为底
```

### 插值和平滑

```csharp
// 平滑插值
Fixed64 smooth = FixedMath.SmoothStep(from, to, t);
Fixed64 smoother = FixedMath.SmootherStep(from, to, t);

// 移动
Fixed64 moved = FixedMath.MoveTowards(current, target, maxDelta);
Fixed64 movedAngle = FixedMath.MoveTowardsAngle(current, target, maxDelta);

// 角度差值
Fixed64 delta = FixedMath.DeltaAngle(current, target);

// 弹簧阻尼
Fixed64 damped = FixedMath.SmoothDamp(current, target, ref velocity, smoothTime, deltaTime);
```

### 范围映射

```csharp
// 值映射
Fixed64 mapped = FixedMath.Remap(value, fromMin, fromMax, toMin, toMax);
Fixed64 mappedClamped = FixedMath.RemapClamped(value, fromMin, fromMax, toMin, toMax);

// 反向插值
Fixed64 t = FixedMath.InverseLerp(a, b, value);
```

### 几何计算

```csharp
// 点到线段
FixedVector3 closest = FixedMath.ClosestPointOnLineSegment(point, lineStart, lineEnd);
Fixed64 dist = FixedMath.DistanceToLineSegment(point, lineStart, lineEnd);

// 点在三角形内判断
bool inside = FixedMath.PointInTriangle2D(point, a, b, c);
```

---

## FixedRandom - 确定性随机数

### 基本用法

```csharp
// 创建随机数生成器（相同种子产生相同序列）
FixedRandom random = new FixedRandom(12345);

// 整数随机
int value = random.Next();              // 非负整数
int value = random.Next(100);           // [0, 100)
int value = random.Next(10, 20);        // [10, 20)

// 定点数随机
Fixed64 value = random.NextFixed();              // [0, 1)
Fixed64 value = random.NextFixed(maxValue);      // [0, maxValue)
Fixed64 value = random.NextFixed(min, max);      // [min, max)
Fixed64 value = random.NextFixedSigned();        // [-1, 1)
```

### 状态管理

```csharp
// 保存状态（用于回滚）
long state = random.State;

// 恢复状态
random.SetState(state);

// 重置到初始种子
random.Reset();

// 重置到新种子
random.Reset(newSeed);
```

### 向量随机

```csharp
// 圆形区域
FixedVector3 point = random.InsideUnitCircle();     // 单位圆内
FixedVector3 point = random.OnUnitCircle();         // 单位圆上

// 球形区域
FixedVector3 point = random.InsideUnitSphere();     // 单位球内
FixedVector3 point = random.OnUnitSphere();         // 单位球面上

// 自定义区域
FixedVector3 point = random.NextVector3(min, max);
FixedVector3 point = random.InsideAnnulus(innerRadius, outerRadius);  // 圆环
FixedVector3 point = random.InsideSector(radius, angleStart, angleEnd);  // 扇形
```

### 概率和选择

```csharp
// 概率判断
bool hit = random.Chance(Fixed64.FromFloat(0.3f));  // 30%概率
bool hit = random.ChancePercent(30);                 // 30%概率

// 随机选择
string item = random.Choose(itemArray);

// 权重选择
int index = random.WeightedChoice(new int[] { 10, 20, 30 });  // 权重10:20:30
int index = random.WeightedChoice(weights);  // Fixed64数组

// 洗牌
random.Shuffle(array);
```

### 分布函数

```csharp
// 高斯分布
Fixed64 value = random.NextGaussian(mean, standardDeviation);

// 泊松分布
int count = random.NextPoisson(lambda);
```

---

## 使用示例

### 示例1：角色移动

```csharp
public class HeroEntity
{
    public FixedVector3 Position;
    public Fixed64 MoveSpeed = Fixed64.FromFloat(5f);

    public void Move(FixedVector3 direction, Fixed64 deltaTime)
    {
        // 归一化方向
        direction = direction.Normalized;

        // 计算位移
        FixedVector3 displacement = direction * MoveSpeed * deltaTime;

        // 更新位置
        Position = Position + displacement;
    }
}
```

### 示例2：弹道飞行

```csharp
public class ProjectileEntity
{
    public FixedVector3 Position;
    public FixedVector3 Direction;
    public Fixed64 Speed = Fixed64.FromFloat(10f);
    public Fixed64 MaxDistance = Fixed64.FromFloat(20f);
    public Fixed64 TraveledDistance = Fixed64.Zero;

    public bool Update(Fixed64 deltaTime)
    {
        Fixed64 moveDistance = Speed * deltaTime;
        Position = Position + Direction * moveDistance;
        TraveledDistance = TraveledDistance + moveDistance;

        // 返回是否超出最大距离
        return TraveledDistance >= MaxDistance;
    }
}
```

### 示例3：范围检测

```csharp
public bool IsInRange(FixedVector3 caster, FixedVector3 target, Fixed64 range)
{
    // 使用平方距离避免开方运算
    Fixed64 sqrRange = range * range;
    Fixed64 sqrDist = FixedVector3.SqrDistance2D(caster, target);
    return sqrDist <= sqrRange;
}
```

### 示例4：技能伤害计算

```csharp
public int CalculateDamage(int baseDamage, Fixed64 critChance, Fixed64 critMultiplier, FixedRandom random)
{
    Fixed64 damage = Fixed64.FromInt(baseDamage);

    // 暴击判定
    if (random.Chance(critChance))
    {
        damage = damage * critMultiplier;
    }

    return damage.ToInt();
}
```

### 示例5：AOE范围内目标获取

```csharp
public List<Entity> GetEntitiesInRange(FixedVector3 center, Fixed64 radius, List<Entity> allEntities)
{
    List<Entity> result = new List<Entity>();
    Fixed64 sqrRadius = radius * radius;

    foreach (var entity in allEntities)
    {
        Fixed64 sqrDist = FixedVector3.SqrDistance2D(center, entity.Position);
        if (sqrDist <= sqrRadius)
        {
            result.Add(entity);
        }
    }

    return result;
}
```

---

## 性能优化建议

### 1. 避免频繁开方

```csharp
// 差：每次都开方
if (FixedVector3.Distance(a, b) < range) { }

// 好：使用平方比较
Fixed64 sqrRange = range * range;
if (FixedVector3.SqrDistance(a, b) < sqrRange) { }
```

### 2. 缓存常用值

```csharp
// 差：每帧重新计算
Fixed64 speed = Fixed64.FromFloat(5f);

// 好：缓存为字段
private static readonly Fixed64 Speed = Fixed64.FromFloat(5f);
```

### 3. 使用整数运算

```csharp
// 差：定点数乘法
Fixed64 result = value * Fixed64.FromInt(2);

// 好：整数乘法（更快）
Fixed64 result = value * 2;
```

### 4. 避免在逻辑层使用ToFloat

```csharp
// 差：逻辑层使用浮点数
float displayPos = position.X.ToFloat();
if (displayPos > 10f) { }

// 好：保持定点数
if (position.X > Fixed64.FromInt(10)) { }
```

---

## 单元测试

```csharp
[Test]
public void Fixed64_Multiply_Precision()
{
    var a = Fixed64.FromFloat(1.5f);
    var b = Fixed64.FromFloat(2.0f);
    var result = a * b;
    Assert.AreEqual(3.0f, result.ToFloat(), 0.0001f);
}

[Test]
public void Fixed64_Sqrt_Accuracy()
{
    var value = Fixed64.FromInt(16);
    var result = Fixed64.Sqrt(value);
    Assert.AreEqual(4.0f, result.ToFloat(), 0.001f);
}

[Test]
public void FixedRandom_Deterministic()
{
    var random1 = new FixedRandom(12345);
    var random2 = new FixedRandom(12345);

    for (int i = 0; i < 100; i++)
    {
        Assert.AreEqual(random1.Next(), random2.Next());
    }
}

[Test]
public void FixedVector3_Distance()
{
    var a = FixedVector3.FromFloat(0, 0, 0);
    var b = FixedVector3.FromFloat(3, 0, 4);
    var dist = FixedVector3.Distance(a, b);
    Assert.AreEqual(5.0f, dist.ToFloat(), 0.001f);
}
```

---

## 注意事项

1. **逻辑层禁止使用浮点数**：所有游戏逻辑必须使用定点数
2. **ToFloat仅用于表现层**：转换为浮点数只能在渲染/UI显示时使用
3. **FromFloat仅用于初始化**：从浮点数创建定点数只能在配置加载时使用
4. **随机数必须使用FixedRandom**：禁止使用System.Random或UnityEngine.Random
5. **保存随机数状态**：回滚时需要恢复随机数生成器状态

---

## 版本历史

| 版本 | 日期 | 更新内容 |
|------|------|----------|
| 1.0 | 2024-12 | 初始版本，包含Fixed64、FixedVector3、FixedMath、FixedRandom |

---

## 参考资料

- [FixedMath.Net](https://github.com/asik/FixedMath.Net) - C#定点数库参考
- [Fixed-Point Arithmetic](https://en.wikipedia.org/wiki/Fixed-point_arithmetic) - 定点数原理
- [Deterministic Lockstep](https://gafferongames.com/post/deterministic_lockstep/) - 帧同步确定性要求
