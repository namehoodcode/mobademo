
using System;
using UnityEngine;
using MobaCombatCore.Core.Math;

namespace MobaCombatCore.Tests
{
    /// <summary>
    /// 定点数数学库单元测试
    /// 在Unity Editor中运行，验证Day 1的所有数学库功能
    /// </summary>
    public class FixedMathTests : MonoBehaviour
    {
        // 测试统计
        private int _totalTests = 0;
        private int _passedTests = 0;
        private int _failedTests = 0;

        // 允许的误差范围（用于浮点数比较）
        private const float FLOAT_TOLERANCE = 0.001f;
        private const double DOUBLE_TOLERANCE = 0.0001;

        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            _totalTests = 0;
            _passedTests = 0;
            _failedTests = 0;

            Debug.Log("========================================");
            Debug.Log("开始运行定点数数学库单元测试");
            Debug.Log("========================================\n");

            // Fixed64 测试
            TestFixed64Creation();
            TestFixed64BasicOperations();
            TestFixed64Comparison();
            TestFixed64MathFunctions();
            TestFixed64EdgeCases();

            // FixedVector3 测试
            TestFixedVector3Creation();
            TestFixedVector3Operations();
            TestFixedVector3VectorMath();
            TestFixedVector3Distance();

            // FixedMath 测试
            TestFixedMathTrigonometry();
            TestFixedMathAngle();
            TestFixedMathInterpolation();

            // FixedRandom 测试
            TestFixedRandomDeterminism();
            TestFixedRandomRange();
            TestFixedRandomDistribution();

            // 输出总结
            Debug.Log("\n========================================");
            Debug.Log($"测试完成: {_passedTests}/{_totalTests} 通过");
            if (_failedTests > 0)
            {
                Debug.LogError($"失败测试数: {_failedTests}");
            }
            else
            {
                Debug.Log("<color=green>所有测试通过!</color>");
            }
            Debug.Log("========================================");
        }

        #region Fixed64 Tests

        [ContextMenu("Test Fixed64 Creation")]
        public void TestFixed64Creation()
        {
            Debug.Log("\n--- Fixed64 创建测试 ---");

            // 从整数创建
            AssertEqual(Fixed64.FromInt(5).ToInt(), 5, "FromInt(5).ToInt()");
            AssertEqual(Fixed64.FromInt(-10).ToInt(), -10, "FromInt(-10).ToInt()");
            AssertEqual(Fixed64.FromInt(0).ToInt(), 0, "FromInt(0).ToInt()");

            // 从浮点数创建
            AssertNearlyEqual(Fixed64.FromFloat(1.5f).ToFloat(), 1.5f, "FromFloat(1.5f).ToFloat()");
            AssertNearlyEqual(Fixed64.FromFloat(-2.25f).ToFloat(), -2.25f, "FromFloat(-2.25f).ToFloat()");
            AssertNearlyEqual(Fixed64.FromFloat(0.001f).ToFloat(), 0.001f, "FromFloat(0.001f).ToFloat()");

            // 从双精度创建
            AssertNearlyEqual(Fixed64.FromDouble(3.141592).ToDouble(), 3.141592, "FromDouble(3.141592).ToDouble()");

            // 常量验证
            AssertEqual(Fixed64.Zero.RawValue, 0L, "Fixed64.Zero");
            AssertEqual(Fixed64.One.RawValue, Fixed64.PRECISION, "Fixed64.One");
            AssertNearlyEqual(Fixed64.Pi.ToFloat(), 3.141593f, "Fixed64.Pi");
            AssertNearlyEqual(Fixed64.HalfPi.ToFloat(), 1.570796f, "Fixed64.HalfPi");
        }

        [ContextMenu("Test Fixed64 Basic Operations")]
        public void TestFixed64BasicOperations()
        {
            Debug.Log("\n--- Fixed64 基本运算测试 ---");

            Fixed64 a = Fixed64.FromFloat(5.5f);
            Fixed64 b = Fixed64.FromFloat(2.5f);

            // 加法
            AssertNearlyEqual((a + b).ToFloat(), 8.0f, "5.5 + 2.5 = 8.0");

            // 减法
            AssertNearlyEqual((a - b).ToFloat(), 3.0f, "5.5 - 2.5 = 3.0");

            // 乘法
            AssertNearlyEqual((a * b).ToFloat(), 13.75f, "5.5 * 2.5 = 13.75");

            // 除法
            AssertNearlyEqual((a / b).ToFloat(), 2.2f, "5.5 / 2.5 = 2.2");

            // 取负
            AssertNearlyEqual((-a).ToFloat(), -5.5f, "-5.5");

            // 取模
            Fixed64 c = Fixed64.FromFloat(7.0f);
            Fixed64 d = Fixed64.FromFloat(3.0f);
            AssertNearlyEqual((c % d).ToFloat(), 1.0f, "7.0 % 3.0 = 1.0");

            // 与整数运算
            AssertNearlyEqual((a * 2).ToFloat(), 11.0f, "5.5 * 2 = 11.0");
            AssertNearlyEqual((a / 2).ToFloat(), 2.75f, "5.5 / 2 = 2.75");

            // 连续运算
            Fixed64 result = (a + b) * Fixed64.FromFloat(2.0f) - Fixed64.FromFloat(1.0f);
            AssertNearlyEqual(result.ToFloat(), 15.0f, "(5.5 + 2.5) * 2 - 1 = 15.0");
        }

        [ContextMenu("Test Fixed64 Comparison")]
        public void TestFixed64Comparison()
        {
            Debug.Log("\n--- Fixed64 比较运算测试 ---");

            Fixed64 a = Fixed64.FromFloat(5.0f);
            Fixed64 b = Fixed64.FromFloat(3.0f);
            Fixed64 c = Fixed64.FromFloat(5.0f);

            // 相等
            AssertTrue(a == c, "5.0 == 5.0");
            AssertTrue(a != b, "5.0 != 3.0");

            // 大小比较
            AssertTrue(a > b, "5.0 > 3.0");
            AssertTrue(b < a, "3.0 < 5.0");
            AssertTrue(a >= c, "5.0 >= 5.0");
            AssertTrue(a <= c, "5.0 <= 5.0");

            // CompareTo
            AssertTrue(a.CompareTo(b) > 0, "5.0.CompareTo(3.0) > 0");
            AssertTrue(b.CompareTo(a) < 0, "3.0.CompareTo(5.0) < 0");
            AssertTrue(a.CompareTo(c) == 0, "5.0.CompareTo(5.0) == 0");
        }

        [ContextMenu("Test Fixed64 Math Functions")]
        public void TestFixed64MathFunctions()
        {
            Debug.Log("\n--- Fixed64 数学函数测试 ---");

            // 绝对值
            AssertNearlyEqual(Fixed64.Abs(Fixed64.FromFloat(-5.5f)).ToFloat(), 5.5f, "Abs(-5.5) = 5.5");
            AssertNearlyEqual(Fixed64.Abs(Fixed64.FromFloat(3.0f)).ToFloat(), 3.0f, "Abs(3.0) = 3.0");

            // 符号
            AssertEqual(Fixed64.Sign(Fixed64.FromFloat(5.0f)), 1, "Sign(5.0) = 1");
            AssertEqual(Fixed64.Sign(Fixed64.FromFloat(-5.0f)), -1, "Sign(-5.0) = -1");
            AssertEqual(Fixed64.Sign(Fixed64.Zero), 0, "Sign(0) = 0");

            // 取整
            AssertNearlyEqual(Fixed64.Floor(Fixed64.FromFloat(5.7f)).ToFloat(), 5.0f, "Floor(5.7) = 5.0");
            AssertNearlyEqual(Fixed64.Floor(Fixed64.FromFloat(-5.3f)).ToFloat(), -6.0f, "Floor(-5.3) = -6.0");
            AssertNearlyEqual(Fixed64.Ceiling(Fixed64.FromFloat(5.3f)).ToFloat(), 6.0f, "Ceiling(5.3) = 6.0");
            AssertNearlyEqual(Fixed64.Ceiling(Fixed64.FromFloat(-5.7f)).ToFloat(), -5.0f, "Ceiling(-5.7) = -5.0");
            AssertNearlyEqual(Fixed64.Round(Fixed64.FromFloat(5.5f)).ToFloat(), 6.0f, "Round(5.5) = 6.0");
            AssertNearlyEqual(Fixed64.Round(Fixed64.FromFloat(5.4f)).ToFloat(), 5.0f, "Round(5.4) = 5.0");

            // Min/Max
            Fixed64 a = Fixed64.FromFloat(3.0f);
            Fixed64 b = Fixed64.FromFloat(7.0f);
            AssertNearlyEqual(Fixed64.Min(a, b).ToFloat(), 3.0f, "Min(3, 7) = 3");
            AssertNearlyEqual(Fixed64.Max(a, b).ToFloat(), 7.0f, "Max(3, 7) = 7");

            // Clamp
            AssertNearlyEqual(Fixed64.Clamp(Fixed64.FromFloat(5.0f), a, b).ToFloat(), 5.0f, "Clamp(5, 3, 7) = 5");
            AssertNearlyEqual(Fixed64.Clamp(Fixed64.FromFloat(1.0f), a, b).ToFloat(), 3.0f, "Clamp(1, 3, 7) = 3");
            AssertNearlyEqual(Fixed64.Clamp(Fixed64.FromFloat(10.0f), a, b).ToFloat(), 7.0f, "Clamp(10, 3, 7) = 7");

            // Clamp01
            AssertNearlyEqual(Fixed64.Clamp01(Fixed64.FromFloat(0.5f)).ToFloat(), 0.5f, "Clamp01(0.5) = 0.5");
            AssertNearlyEqual(Fixed64.Clamp01(Fixed64.FromFloat(-0.5f)).ToFloat(), 0.0f, "Clamp01(-0.5) = 0.0");
            AssertNearlyEqual(Fixed64.Clamp01(Fixed64.FromFloat(1.5f)).ToFloat(), 1.0f, "Clamp01(1.5) = 1.0");

            // Lerp
            AssertNearlyEqual(Fixed64.Lerp(a, b, Fixed64.FromFloat(0.5f)).ToFloat(), 5.0f, "Lerp(3, 7, 0.5) = 5");
            AssertNearlyEqual(Fixed64.Lerp(a, b, Fixed64.Zero).ToFloat(), 3.0f, "Lerp(3, 7, 0) = 3");
            AssertNearlyEqual(Fixed64.Lerp(a, b, Fixed64.One).ToFloat(), 7.0f, "Lerp(3, 7, 1) = 7");

            // Sqrt
            AssertNearlyEqual(Fixed64.Sqrt(Fixed64.FromFloat(4.0f)).ToFloat(), 2.0f, "Sqrt(4) = 2", 0.01f);
            AssertNearlyEqual(Fixed64.Sqrt(Fixed64.FromFloat(9.0f)).ToFloat(), 3.0f, "Sqrt(9) = 3", 0.01f);
            AssertNearlyEqual(Fixed64.Sqrt(Fixed64.FromFloat(2.0f)).ToFloat(), 1.414f, "Sqrt(2) ≈ 1.414", 0.01f);
        }

        [ContextMenu("Test Fixed64 Edge Cases")]
        public void TestFixed64EdgeCases()
        {
            Debug.Log("\n--- Fixed64 边界情况测试 ---");

            // 零值运算
            AssertNearlyEqual((Fixed64.Zero + Fixed64.One).ToFloat(), 1.0f, "0 + 1 = 1");
            AssertNearlyEqual((Fixed64.Zero * Fixed64.FromFloat(100.0f)).ToFloat(), 0.0f, "0 * 100 = 0");

            // 小数精度 - Fixed64精度为百万分之一，所以0.000001是最小单位
            // 但由于浮点数精度问题，0.000001f可能无法精确表示
            // 使用稍大的值来测试精度
            Fixed64 small = Fixed64.FromFloat(0.00001f);  // 十万分之一
            AssertTrue(small.RawValue > 0, "0.00001 > 0 (精度测试)");
            
            // 直接使用RawValue创建最小精度单位
            Fixed64 epsilon = Fixed64.FromRaw(1L);
            AssertTrue(epsilon.RawValue == 1, "Epsilon.RawValue == 1");
            AssertTrue(epsilon > Fixed64.Zero, "Epsilon > Zero");

            // 大数运算
            Fixed64 large1 = Fixed64.FromInt(1000000);
            Fixed64 large2 = Fixed64.FromInt(2);
            AssertEqual((large1 * large2).ToInt(), 2000000, "1000000 * 2 = 2000000");

            // 除零异常
            bool exceptionThrown = false;
            try
            {
                var result = Fixed64.One / Fixed64.Zero;
            }
            catch (DivideByZeroException)
            {
                exceptionThrown = true;
            }
            AssertTrue(exceptionThrown, "除零应抛出异常");

            // 负数平方根异常
            exceptionThrown = false;
            try
            {
                var result = Fixed64.Sqrt(Fixed64.FromFloat(-1.0f));
            }
            catch (ArgumentException)
            {
                exceptionThrown = true;
            }
            AssertTrue(exceptionThrown, "负数平方根应抛出异常");
        }

        #endregion

        #region FixedVector3 Tests

        [ContextMenu("Test FixedVector3 Creation")]
        public void TestFixedVector3Creation()
        {
            Debug.Log("\n--- FixedVector3 创建测试 ---");

            // 基本创建
            FixedVector3 v1 = new FixedVector3(Fixed64.FromFloat(1.0f), Fixed64.FromFloat(2.0f), Fixed64.FromFloat(3.0f));
            AssertNearlyEqual(v1.X.ToFloat(), 1.0f, "v1.X = 1.0");
            AssertNearlyEqual(v1.Y.ToFloat(), 2.0f, "v1.Y = 2.0");
            AssertNearlyEqual(v1.Z.ToFloat(), 3.0f, "v1.Z = 3.0");

            // 从整数创建
            FixedVector3 v2 = FixedVector3.FromInt(5, 10, 15);
            AssertEqual(v2.X.ToInt(), 5, "FromInt v2.X = 5");
            AssertEqual(v2.Y.ToInt(), 10, "FromInt v2.Y = 10");
            AssertEqual(v2.Z.ToInt(), 15, "FromInt v2.Z = 15");

            // 从浮点数创建
            FixedVector3 v3 = FixedVector3.FromFloat(1.5f, 2.5f, 3.5f);
            AssertNearlyEqual(v3.X.ToFloat(), 1.5f, "FromFloat v3.X = 1.5");
            AssertNearlyEqual(v3.Y.ToFloat(), 2.5f, "FromFloat v3.Y = 2.5");
            AssertNearlyEqual(v3.Z.ToFloat(), 3.5f, "FromFloat v3.Z = 3.5");

            // 常量验证
            AssertTrue(FixedVector3.Zero.X == Fixed64.Zero && FixedVector3.Zero.Y == Fixed64.Zero && FixedVector3.Zero.Z == Fixed64.Zero, "FixedVector3.Zero");
            AssertTrue(FixedVector3.One.X == Fixed64.One && FixedVector3.One.Y == Fixed64.One && FixedVector3.One.Z == Fixed64.One, "FixedVector3.One");
            AssertTrue(FixedVector3.Up.Y == Fixed64.One, "FixedVector3.Up");
            AssertTrue(FixedVector3.Right.X == Fixed64.One, "FixedVector3.Right");
            AssertTrue(FixedVector3.Forward.Z == Fixed64.One, "FixedVector3.Forward");
        }

        [ContextMenu("Test FixedVector3 Operations")]
        public void TestFixedVector3Operations()
        {
            Debug.Log("\n--- FixedVector3 运算测试 ---");

            FixedVector3 a = FixedVector3.FromFloat(1.0f, 2.0f, 3.0f);
            FixedVector3 b = FixedVector3.FromFloat(4.0f, 5.0f, 6.0f);

            // 加法
            FixedVector3 sum = a + b;
            AssertNearlyEqual(sum.X.ToFloat(), 5.0f, "(a+b).X = 5.0");
            AssertNearlyEqual(sum.Y.ToFloat(), 7.0f, "(a+b).Y = 7.0");
            AssertNearlyEqual(sum.Z.ToFloat(), 9.0f, "(a+b).Z = 9.0");

            // 减法
            FixedVector3 diff = b - a;
            AssertNearlyEqual(diff.X.ToFloat(), 3.0f, "(b-a).X = 3.0");
            AssertNearlyEqual(diff.Y.ToFloat(), 3.0f, "(b-a).Y = 3.0");
            AssertNearlyEqual(diff.Z.ToFloat(), 3.0f, "(b-a).Z = 3.0");

            // 标量乘法
            FixedVector3 scaled = a * Fixed64.FromFloat(2.0f);
            AssertNearlyEqual(scaled.X.ToFloat(), 2.0f, "(a*2).X = 2.0");
            AssertNearlyEqual(scaled.Y.ToFloat(), 4.0f, "(a*2).Y = 4.0");
            AssertNearlyEqual(scaled.Z.ToFloat(), 6.0f, "(a*2).Z = 6.0");

            // 标量除法
            FixedVector3 divided = b / Fixed64.FromFloat(2.0f);
            AssertNearlyEqual(divided.X.ToFloat(), 2.0f, "(b/2).X = 2.0");
            AssertNearlyEqual(divided.Y.ToFloat(), 2.5f, "(b/2).Y = 2.5");
            AssertNearlyEqual(divided.Z.ToFloat(), 3.0f, "(b/2).Z = 3.0");

            // 取负
            FixedVector3 neg = -a;
            AssertNearlyEqual(neg.X.ToFloat(), -1.0f, "(-a).X = -1.0");
            AssertNearlyEqual(neg.Y.ToFloat(), -2.0f, "(-a).Y = -2.0");
            AssertNearlyEqual(neg.Z.ToFloat(), -3.0f, "(-a).Z = -3.0");

            // 相等比较
            FixedVector3 c = FixedVector3.FromFloat(1.0f, 2.0f, 3.0f);
            AssertTrue(a == c, "a == c");
            AssertTrue(a != b, "a != b");
        }

        [ContextMenu("Test FixedVector3 Vector Math")]
        public void TestFixedVector3VectorMath()
        {
            Debug.Log("\n--- FixedVector3 向量数学测试 ---");

            // 长度
            FixedVector3 v1 = FixedVector3.FromFloat(3.0f, 4.0f, 0.0f);
            AssertNearlyEqual(v1.Magnitude.ToFloat(), 5.0f, "|(3,4,0)| = 5", 0.01f);
            AssertNearlyEqual(v1.SqrMagnitude.ToFloat(), 25.0f, "|(3,4,0)|² = 25");

            // 归一化
            FixedVector3 normalized = v1.Normalized;
            AssertNearlyEqual(normalized.X.ToFloat(), 0.6f, "normalized.X = 0.6", 0.01f);
            AssertNearlyEqual(normalized.Y.ToFloat(), 0.8f, "normalized.Y = 0.8", 0.01f);
            AssertNearlyEqual(normalized.Magnitude.ToFloat(), 1.0f, "|normalized| = 1", 0.01f);

            // 点积
            FixedVector3 a = FixedVector3.FromFloat(1.0f, 0.0f, 0.0f);
            FixedVector3 b = FixedVector3.FromFloat(0.0f, 1.0f, 0.0f);
            FixedVector3 c = FixedVector3.FromFloat(1.0f, 0.0f, 0.0f);
            AssertNearlyEqual(FixedVector3.Dot(a, b).ToFloat(), 0.0f, "Dot(right, up) = 0");
            AssertNearlyEqual(FixedVector3.Dot(a, c).ToFloat(), 1.0f, "Dot(right, right) = 1");

            // 叉积
            FixedVector3 cross = FixedVector3.Cross(a, b);
            AssertNearlyEqual(cross.Z.ToFloat(), 1.0f, "Cross(right, up).Z = 1", 0.01f);

            // 线性插值
            FixedVector3 start = FixedVector3.FromFloat(0.0f, 0.0f, 0.0f);
            FixedVector3 end = FixedVector3.FromFloat(10.0f, 10.0f, 10.0f);
            FixedVector3 mid = FixedVector3.Lerp(start, end, Fixed64.FromFloat(0.5f));
            AssertNearlyEqual(mid.X.ToFloat(), 5.0f, "Lerp.X = 5.0");
            AssertNearlyEqual(mid.Y.ToFloat(), 5.0f, "Lerp.Y = 5.0");
            AssertNearlyEqual(mid.Z.ToFloat(), 5.0f, "Lerp.Z = 5.0");

            // MoveTowards
            FixedVector3 current = FixedVector3.FromFloat(0.0f, 0.0f, 0.0f);
            FixedVector3 target = FixedVector3.FromFloat(10.0f, 0.0f, 0.0f);
            FixedVector3 moved = FixedVector3.MoveTowards(current, target, Fixed64.FromFloat(3.0f));
            AssertNearlyEqual(moved.X.ToFloat(), 3.0f, "MoveTowards.X = 3.0", 0.01f);
        }

        [ContextMenu("Test FixedVector3 Distance")]
        public void TestFixedVector3Distance()
        {
            Debug.Log("\n--- FixedVector3 距离测试 ---");

            FixedVector3 a = FixedVector3.FromFloat(0.0f, 0.0f, 0.0f);
            FixedVector3 b = FixedVector3.FromFloat(3.0f, 4.0f, 0.0f);

            // 3D距离
            AssertNearlyEqual(FixedVector3.Distance(a, b).ToFloat(), 5.0f, "Distance = 5.0", 0.01f);
            AssertNearlyEqual(FixedVector3.SqrDistance(a, b).ToFloat(), 25.0f, "SqrDistance = 25.0");

            // 2D距离（忽略Y轴）
            FixedVector3 c = FixedVector3.FromFloat(0.0f, 100.0f, 0.0f);
            FixedVector3 d = FixedVector3.FromFloat(3.0f, 200.0f, 4.0f);
            AssertNearlyEqual(FixedVector3.Distance2D(c, d).ToFloat(), 5.0f, "Distance2D = 5.0", 0.01f);
        }

        #endregion

        #region FixedMath Tests

        [ContextMenu("Test FixedMath Trigonometry")]
        public void TestFixedMathTrigonometry()
        {
            Debug.Log("\n--- FixedMath 三角函数测试 ---");

            // Sin测试
            AssertNearlyEqual(FixedMath.Sin(Fixed64.Zero).ToFloat(), 0.0f, "Sin(0) = 0", 0.01f);
            AssertNearlyEqual(FixedMath.Sin(Fixed64.HalfPi).ToFloat(), 1.0f, "Sin(π/2) = 1", 0.01f);
            AssertNearlyEqual(FixedMath.Sin(Fixed64.Pi).ToFloat(), 0.0f, "Sin(π) = 0", 0.01f);

            // Cos测试
            AssertNearlyEqual(FixedMath.Cos(Fixed64.Zero).ToFloat(), 1.0f, "Cos(0) = 1", 0.01f);
            AssertNearlyEqual(FixedMath.Cos(Fixed64.HalfPi).ToFloat(), 0.0f, "Cos(π/2) = 0", 0.01f);
            AssertNearlyEqual(FixedMath.Cos(Fixed64.Pi).ToFloat(), -1.0f, "Cos(π) = -1", 0.01f);

            // Tan测试
            AssertNearlyEqual(FixedMath.Tan(Fixed64.Zero).ToFloat(), 0.0f, "Tan(0) = 0", 0.01f);
            AssertNearlyEqual(FixedMath.Tan(Fixed64.FromFloat(0.7854f)).ToFloat(), 1.0f, "Tan(π/4) ≈ 1", 0.05f);

            // Sin²(x) + Cos²(x) = 1 验证
            Fixed64 angle = Fixed64.FromFloat(1.234f);
            Fixed64 sin = FixedMath.Sin(angle);
            Fixed64 cos = FixedMath.Cos(angle);
            Fixed64 sum = sin * sin + cos * cos;
            AssertNearlyEqual(sum.ToFloat(), 1.0f, "Sin²(x) + Cos²(x) = 1", 0.01f);

            // 度数版本
            AssertNearlyEqual(FixedMath.SinDegrees(Fixed64.FromInt(90)).ToFloat(), 1.0f, "Sin(90°) = 1", 0.01f);
            AssertNearlyEqual(FixedMath.CosDegrees(Fixed64.FromInt(0)).ToFloat(), 1.0f, "Cos(0°) = 1", 0.01f);
        }

        [ContextMenu("Test FixedMath Angle")]
        public void TestFixedMathAngle()
        {
            Debug.Log("\n--- FixedMath 角度函数测试 ---");

            // 角度弧度转换
            AssertNearlyEqual(FixedMath.DegToRad(Fixed64.FromInt(180)).ToFloat(), (float)System.Math.PI, "180° = π rad", 0.01f);
            AssertNearlyEqual(FixedMath.RadToDeg(Fixed64.Pi).ToFloat(), 180.0f, "π rad = 180°", 0.1f);

            // Atan2测试
            Fixed64 atan2Result = FixedMath.Atan2(Fixed64.One, Fixed64.One);
            AssertNearlyEqual(atan2Result.ToFloat(), (float)(System.Math.PI / 4), "Atan2(1,1) = π/4", 0.05f);

            // 方向向量
            FixedVector3 dir = FixedMath.AngleToDirection2D(Fixed64.Zero);
            AssertNearlyEqual(dir.X.ToFloat(), 1.0f, "AngleToDirection2D(0).X = 1", 0.01f);
            AssertNearlyEqual(dir.Z.ToFloat(), 0.0f, "AngleToDirection2D(0).Z = 0", 0.01f);

            // 绕Y轴旋转
            FixedVector3 original = FixedVector3.FromFloat(1.0f, 0.0f, 0.0f);
            FixedVector3 rotated = FixedMath.RotateAroundY(original, Fixed64.HalfPi);
            AssertNearlyEqual(rotated.X.ToFloat(), 0.0f, "RotateAroundY(90°).X ≈ 0", 0.01f);
            AssertNearlyEqual(rotated.Z.ToFloat(), 1.0f, "RotateAroundY(90°).Z ≈ 1", 0.01f);
        }

        [ContextMenu("Test FixedMath Interpolation")]
        public void TestFixedMathInterpolation()
        {
            Debug.Log("\n--- FixedMath 插值函数测试 ---");

            Fixed64 a = Fixed64.FromFloat(0.0f);
            Fixed64 b = Fixed64.FromFloat(10.0f);

            // SmoothStep
            Fixed64 smooth = FixedMath.SmoothStep(a, b, Fixed64.FromFloat(0.5f));
            AssertTrue(smooth > a && smooth < b, "SmoothStep(0.5) 在范围内");

            // MoveTowards
            Fixed64 current = Fixed64.FromFloat(0.0f);
            Fixed64 target = Fixed64.FromFloat(10.0f);
            Fixed64 moved = FixedMath.MoveTowards(current, target, Fixed64.FromFloat(3.0f));
            AssertNearlyEqual(moved.ToFloat(), 3.0f, "MoveTowards(0, 10, 3) = 3");

            // InverseLerp
            Fixed64 inverseLerp = FixedMath.InverseLerp(a, b, Fixed64.FromFloat(5.0f));
            AssertNearlyEqual(inverseLerp.ToFloat(), 0.5f, "InverseLerp(0, 10, 5) = 0.5");

            // Remap
            Fixed64 remapped = FixedMath.Remap(Fixed64.FromFloat(5.0f),
                Fixed64.Zero, Fixed64.FromFloat(10.0f),
                Fixed64.Zero, Fixed64.FromFloat(100.0f));
            AssertNearlyEqual(remapped.ToFloat(), 50.0f, "Remap(5, 0-10, 0-100) = 50");
        }

        #endregion

        #region FixedRandom Tests

        [ContextMenu("Test FixedRandom Determinism")]
        public void TestFixedRandomDeterminism()
        {
            Debug.Log("\n--- FixedRandom 确定性测试 ---");

            // 相同种子应产生相同序列
            FixedRandom rand1 = new FixedRandom(12345);
            FixedRandom rand2 = new FixedRandom(12345);

            bool allSame = true;
            for (int i = 0; i < 100; i++)
            {
                if (rand1.Next() != rand2.Next())
                {
                    allSame = false;
                    break;
                }
            }
            AssertTrue(allSame, "相同种子产生相同整数序列");

            // 重置后应产生相同序列
            rand1.Reset();
            rand2.Reset();
            allSame = true;
            for (int i = 0; i < 100; i++)
            {
                if (rand1.NextFixed().RawValue != rand2.NextFixed().RawValue)
                {
                    allSame = false;
                    break;
                }
            }
            AssertTrue(allSame, "重置后产生相同定点数序列");

            // 状态保存和恢复
            FixedRandom rand3 = new FixedRandom(54321);
            for (int i = 0; i < 50; i++) rand3.Next(); // 前进50步
            long savedState = rand3.State;
            int nextValue = rand3.Next();

            rand3.SetState(savedState);
            int restoredValue = rand3.Next();
            AssertEqual(nextValue, restoredValue, "状态恢复后产生相同值");
        }

        [ContextMenu("Test FixedRandom Range")]
        public void TestFixedRandomRange()
        {
            Debug.Log("\n--- FixedRandom 范围测试 ---");

            FixedRandom rand = new FixedRandom(99999);

            // 整数范围测试
            bool allInRange = true;
            for (int i = 0; i < 1000; i++)
            {
                int value = rand.Next(10, 20);
                if (value < 10 || value >= 20)
                {
                    allInRange = false;
                    break;
                }
            }
            AssertTrue(allInRange, "Next(10, 20) 在范围 [10, 20) 内");

            // 定点数范围测试
            allInRange = true;
            Fixed64 min = Fixed64.FromFloat(-5.0f);
            Fixed64 max = Fixed64.FromFloat(5.0f);
            for (int i = 0; i < 1000; i++)
            {
                Fixed64 value = rand.NextFixed(min, max);
                if (value < min || value >= max)
                {
                    allInRange = false;
                    break;
                }
            }
            AssertTrue(allInRange, "NextFixed(-5, 5) 在范围 [-5, 5) 内");

            // NextFixed() 在 [0, 1) 范围
            allInRange = true;
            for (int i = 0; i < 1000; i++)
            {
                Fixed64 value = rand.NextFixed();
                if (value < Fixed64.Zero || value >= Fixed64.One)
                {
                    allInRange = false;
                    break;
                }
            }
            AssertTrue(allInRange, "NextFixed() 在范围 [0, 1) 内");
        }

        [ContextMenu("Test FixedRandom Distribution")]
        public void TestFixedRandomDistribution()
        {
            Debug.Log("\n--- FixedRandom 分布测试 ---");

            FixedRandom rand = new FixedRandom(11111);

            // 概率测试 (50%概率)
            int hits = 0;
            int total = 10000;
            for (int i = 0; i < total; i++)
            {
                if (rand.Chance(Fixed64.FromFloat(0.5f)))
                {
                    hits++;
                }
            }
            float hitRate = (float)hits / total;
            AssertTrue(hitRate > 0.45f && hitRate < 0.55f, $"50%概率命中率: {hitRate:P2} (应在45%-55%之间)");

            // 单位圆内随机点
            bool allInCircle = true;
            for (int i = 0; i < 100; i++)
            {
                FixedVector3 point = rand.InsideUnitCircle();
                if (point.SqrMagnitude2D > Fixed64.One)
                {
                    allInCircle = false;
                    break;
                }
            }
            AssertTrue(allInCircle, "InsideUnitCircle() 所有点在单位圆内");

            // 单位圆上随机点
            bool allOnCircle = true;
            for (int i = 0; i < 100; i++)
            {
                FixedVector3 point = rand.OnUnitCircle();
                Fixed64 mag = point.Magnitude2D;
                if (Fixed64.Abs(mag - Fixed64.One).ToFloat() > 0.01f)
                {
                    allOnCircle = false;
                    break;
                }
            }
            AssertTrue(allOnCircle, "OnUnitCircle() 所有点在单位圆上");

            // 权重选择测试
            int[] weights = { 1, 2, 3, 4 }; // 总权重10
            int[] counts = new int[4];
            for (int i = 0; i < 10000; i++)
            {
                int index = rand.WeightedChoice(weights);
                counts[index]++;
            }
            // 检查分布是否大致符合权重比例
            bool distributionOk = counts[3] > counts[2] && counts[2] > counts[1] && counts[1] > counts[0];
            AssertTrue(distributionOk, $"WeightedChoice分布: [{counts[0]}, {counts[1]}, {counts[2]}, {counts[3]}]");

            // 洗牌测试
            int[] array = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            int[] original = (int[])array.Clone();
            rand.Shuffle(array);
            bool shuffled = false;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != original[i])
                {
                    shuffled = true;
                    break;
                }
            }
            AssertTrue(shuffled, "Shuffle() 改变了数组顺序");
        }

        #endregion

        #region Assert Helpers

        private void AssertTrue(bool condition, string testName)
        {
            _totalTests++;
            if (condition)
            {
                _passedTests++;
                Debug.Log($"<color=green>✓ PASS:</color> {testName}");
            }
            else
            {
                _failedTests++;
                Debug.LogError($"<color=red>✗ FAIL:</color> {testName}");
            }
        }

        private void AssertEqual<T>(T actual, T expected, string testName) where T : IEquatable<T>
        {
            _totalTests++;
            if (actual.Equals(expected))
            {
                _passedTests++;
                Debug.Log($"<color=green>✓ PASS:</color> {testName} (值: {actual})");
            }
            else
            {
                _failedTests++;
                Debug.LogError($"<color=red>✗ FAIL:</color> {testName} - 期望: {expected}, 实际: {actual}");
            }
        }

        private void AssertNearlyEqual(float actual, float expected, string testName, float tolerance = FLOAT_TOLERANCE)
        {
            _totalTests++;
            if (Mathf.Abs(actual - expected) <= tolerance)
            {
                _passedTests++;
                Debug.Log($"<color=green>✓ PASS:</color> {testName} (值: {actual:F6})");
            }
            else
            {
                _failedTests++;
                Debug.LogError($"<color=red>✗ FAIL:</color> {testName} - 期望: {expected:F6}, 实际: {actual:F6}, 误差: {Mathf.Abs(actual - expected):F6}");
            }
        }

        private void AssertNearlyEqual(double actual, double expected, string testName, double tolerance = DOUBLE_TOLERANCE)
        {
            _totalTests++;
            if (System.Math.Abs(actual - expected) <= tolerance)
            {
                _passedTests++;
                Debug.Log($"<color=green>✓ PASS:</color> {testName} (值: {actual:F6})");
            }
            else
            {
                _failedTests++;
                Debug.LogError($"<color=red>✗ FAIL:</color> {testName} - 期望: {expected:F6}, 实际: {actual:F6}, 误差: {System.Math.Abs(actual - expected):F6}");
            }
        }

        #endregion
    }
}