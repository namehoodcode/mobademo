using UnityEngine;
using UnityEditor;

namespace MobaCombatCore.Tests.Editor
{
    /// <summary>
    /// 测试运行器编辑器窗口
    /// 提供一个方便的界面来运行Day 1和Day 2的单元测试
    /// </summary>
    public class TestRunnerEditor : EditorWindow
    {
        private Vector2 _scrollPosition;
        
        // Day 1 折叠状态
        private bool _showDay1 = true;
        private bool _showFixed64Tests = true;
        private bool _showVector3Tests = true;
        private bool _showMathTests = true;
        private bool _showRandomTests = true;
        
        // Day 2 折叠状态
        private bool _showDay2 = true;
        private bool _showInputTests = true;
        private bool _showSnapshotTests = true;
        private bool _showNetworkTests = true;
        private bool _showEntityTests = true;
        private bool _showLockstepManagerTests = true;

        [MenuItem("MOBA Combat Core/测试运行器", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<TestRunnerEditor>("测试运行器");
            window.minSize = new Vector2(300, 400);
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // 标题
            EditorGUILayout.Space(10);
            GUILayout.Label("MOBA Combat Core", EditorStyles.boldLabel);
            GUILayout.Label("单元测试运行器 (Day 1 & Day 2)", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);

            // 分隔线
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 运行所有测试按钮
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("▶ 运行所有测试 (Day 1 + Day 2)", GUILayout.Height(40)))
            {
                RunAllTests();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            // Day 1 和 Day 2 快捷按钮
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.5f, 0.7f, 1f);
            if (GUILayout.Button("▶ Day 1 测试", GUILayout.Height(30)))
            {
                RunDay1Tests();
            }
            GUI.backgroundColor = new Color(1f, 0.7f, 0.5f);
            if (GUILayout.Button("▶ Day 2 测试", GUILayout.Height(30)))
            {
                RunDay2Tests();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // ==================== Day 1 测试 ====================
            _showDay1 = EditorGUILayout.Foldout(_showDay1, "📅 Day 1: 定点数数学库", true, EditorStyles.foldoutHeader);
            if (_showDay1)
            {
                EditorGUI.indentLevel++;

                // Fixed64 测试组
                _showFixed64Tests = EditorGUILayout.Foldout(_showFixed64Tests, "Fixed64 定点数测试", true);
                if (_showFixed64Tests)
                {
                    EditorGUI.indentLevel++;
                    if (GUILayout.Button("创建测试")) RunFixed64CreationTest();
                    if (GUILayout.Button("基本运算测试")) RunFixed64BasicOperationsTest();
                    if (GUILayout.Button("比较运算测试")) RunFixed64ComparisonTest();
                    if (GUILayout.Button("数学函数测试")) RunFixed64MathFunctionsTest();
                    if (GUILayout.Button("边界情况测试")) RunFixed64EdgeCasesTest();
                    EditorGUI.indentLevel--;
                }

                // FixedVector3 测试组
                _showVector3Tests = EditorGUILayout.Foldout(_showVector3Tests, "FixedVector3 向量测试", true);
                if (_showVector3Tests)
                {
                    EditorGUI.indentLevel++;
                    if (GUILayout.Button("创建测试")) RunVector3CreationTest();
                    if (GUILayout.Button("运算测试")) RunVector3OperationsTest();
                    if (GUILayout.Button("向量数学测试")) RunVector3VectorMathTest();
                    if (GUILayout.Button("距离测试")) RunVector3DistanceTest();
                    EditorGUI.indentLevel--;
                }

                // FixedMath 测试组
                _showMathTests = EditorGUILayout.Foldout(_showMathTests, "FixedMath 数学函数测试", true);
                if (_showMathTests)
                {
                    EditorGUI.indentLevel++;
                    if (GUILayout.Button("三角函数测试")) RunMathTrigonometryTest();
                    if (GUILayout.Button("角度函数测试")) RunMathAngleTest();
                    if (GUILayout.Button("插值函数测试")) RunMathInterpolationTest();
                    EditorGUI.indentLevel--;
                }

                // FixedRandom 测试组
                _showRandomTests = EditorGUILayout.Foldout(_showRandomTests, "FixedRandom 随机数测试", true);
                if (_showRandomTests)
                {
                    EditorGUI.indentLevel++;
                    if (GUILayout.Button("确定性测试")) RunRandomDeterminismTest();
                    if (GUILayout.Button("范围测试")) RunRandomRangeTest();
                    if (GUILayout.Button("分布测试")) RunRandomDistributionTest();
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // ==================== Day 2 测试 ====================
            _showDay2 = EditorGUILayout.Foldout(_showDay2, "📅 Day 2: 帧同步系统", true, EditorStyles.foldoutHeader);
            if (_showDay2)
            {
                EditorGUI.indentLevel++;

                // Input 测试组
                _showInputTests = EditorGUILayout.Foldout(_showInputTests, "输入系统测试", true);
                if (_showInputTests)
                {
                    EditorGUI.indentLevel++;
                    if (GUILayout.Button("PlayerInput测试")) RunPlayerInputTest();
                    if (GUILayout.Button("FrameInput测试")) RunFrameInputTest();
                    if (GUILayout.Button("InputBuffer测试")) RunInputBufferTest();
                    if (GUILayout.Button("LogicFrame测试")) RunLogicFrameTest();
                    if (GUILayout.Button("LockstepConfig测试")) RunLockstepConfigTest();
                    EditorGUI.indentLevel--;
                }

                // Snapshot 测试组
                _showSnapshotTests = EditorGUILayout.Foldout(_showSnapshotTests, "快照系统测试", true);
                if (_showSnapshotTests)
                {
                    EditorGUI.indentLevel++;
                    if (GUILayout.Button("EntitySnapshot测试")) RunEntitySnapshotTest();
                    if (GUILayout.Button("GameSnapshot测试")) RunGameSnapshotTest();
                    if (GUILayout.Button("SnapshotManager测试")) RunSnapshotManagerTest();
                    EditorGUI.indentLevel--;
                }

                // Network 测试组
                _showNetworkTests = EditorGUILayout.Foldout(_showNetworkTests, "网络模拟测试", true);
                if (_showNetworkTests)
                {
                    EditorGUI.indentLevel++;
                    if (GUILayout.Button("DelayConfig测试")) RunDelayConfigTest();
                    if (GUILayout.Button("DelaySimulator测试")) RunDelaySimulatorTest();
                    if (GUILayout.Button("LocalServer测试")) RunLocalServerTest();
                    EditorGUI.indentLevel--;
                }

                // Entity 测试组
                _showEntityTests = EditorGUILayout.Foldout(_showEntityTests, "实体系统测试", true);
                if (_showEntityTests)
                {
                    EditorGUI.indentLevel++;
                    if (GUILayout.Button("创建测试")) RunEntityCreationTest();
                    if (GUILayout.Button("移动测试")) RunEntityMovementTest();
                    if (GUILayout.Button("战斗测试")) RunEntityCombatTest();
                    if (GUILayout.Button("快照测试")) RunEntitySnapshotRestoreTest();
                    EditorGUI.indentLevel--;
                }

                // LockstepManager 测试组
                _showLockstepManagerTests = EditorGUILayout.Foldout(_showLockstepManagerTests, "LockstepManager测试", true);
                if (_showLockstepManagerTests)
                {
                    EditorGUI.indentLevel++;
                    if (GUILayout.Button("LockstepManager测试")) RunLockstepManagerTest();
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 帮助信息
            EditorGUILayout.HelpBox(
                "测试结果将显示在Console窗口中。\n" +
                "绿色 ✓ 表示测试通过\n" +
                "红色 ✗ 表示测试失败",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // 打开Console按钮
            if (GUILayout.Button("打开 Console 窗口"))
            {
                EditorApplication.ExecuteMenuItem("Window/General/Console");
            }

            EditorGUILayout.EndScrollView();
        }

        private FixedMathTests GetOrCreateFixedMathTests()
        {
            var testRunner = FindObjectOfType<TestRunner>();
            if (testRunner != null)
            {
                var tests = testRunner.GetComponent<FixedMathTests>();
                if (tests != null) return tests;
            }

            var tempGO = new GameObject("_TempTestRunner");
            var testComponent = tempGO.AddComponent<FixedMathTests>();
            
            EditorApplication.delayCall += () =>
            {
                if (tempGO != null)
                {
                    DestroyImmediate(tempGO);
                }
            };

            return testComponent;
        }

        private LockstepTests GetOrCreateLockstepTests()
        {
            var testRunner = FindObjectOfType<TestRunner>();
            if (testRunner != null)
            {
                var tests = testRunner.GetComponent<LockstepTests>();
                if (tests != null) return tests;
            }

            var tempGO = new GameObject("_TempLockstepTestRunner");
            var testComponent = tempGO.AddComponent<LockstepTests>();
            
            EditorApplication.delayCall += () =>
            {
                if (tempGO != null)
                {
                    DestroyImmediate(tempGO);
                }
            };

            return testComponent;
        }

        private void RunAllTests()
        {
            Debug.Log("========================================");
            Debug.Log("开始运行所有单元测试 (Day 1 + Day 2)");
            Debug.Log("========================================");
            
            var fixedMathTests = GetOrCreateFixedMathTests();
            fixedMathTests.RunAllTests();

            var lockstepTests = GetOrCreateLockstepTests();
            lockstepTests.RunAllTests();
        }

        private void RunDay1Tests()
        {
            Debug.Log("========================================");
            Debug.Log("开始运行 Day 1 测试 (定点数数学库)");
            Debug.Log("========================================");
            
            var tests = GetOrCreateFixedMathTests();
            tests.RunAllTests();
        }

        private void RunDay2Tests()
        {
            Debug.Log("========================================");
            Debug.Log("开始运行 Day 2 测试 (帧同步系统)");
            Debug.Log("========================================");
            
            var tests = GetOrCreateLockstepTests();
            tests.RunAllTests();
        }

        // ==================== Day 1 测试方法 ====================
        
        private void RunFixed64CreationTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixed64Creation();
        }

        private void RunFixed64BasicOperationsTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixed64BasicOperations();
        }

        private void RunFixed64ComparisonTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixed64Comparison();
        }

        private void RunFixed64MathFunctionsTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixed64MathFunctions();
        }

        private void RunFixed64EdgeCasesTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixed64EdgeCases();
        }

        private void RunVector3CreationTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixedVector3Creation();
        }

        private void RunVector3OperationsTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixedVector3Operations();
        }

        private void RunVector3VectorMathTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixedVector3VectorMath();
        }

        private void RunVector3DistanceTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixedVector3Distance();
        }

        private void RunMathTrigonometryTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixedMathTrigonometry();
        }

        private void RunMathAngleTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixedMathAngle();
        }

        private void RunMathInterpolationTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixedMathInterpolation();
        }

        private void RunRandomDeterminismTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixedRandomDeterminism();
        }

        private void RunRandomRangeTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixedRandomRange();
        }

        private void RunRandomDistributionTest()
        {
            var tests = GetOrCreateFixedMathTests();
            tests.TestFixedRandomDistribution();
        }

        // ==================== Day 2 测试方法 ====================

        private void RunPlayerInputTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestPlayerInput();
        }

        private void RunFrameInputTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestFrameInput();
        }

        private void RunInputBufferTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestInputBuffer();
        }

        private void RunLogicFrameTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestLogicFrame();
        }

        private void RunLockstepConfigTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestLockstepConfig();
        }

        private void RunEntitySnapshotTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestEntitySnapshot();
        }

        private void RunGameSnapshotTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestGameSnapshot();
        }

        private void RunSnapshotManagerTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestSnapshotManager();
        }

        private void RunDelayConfigTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestDelayConfig();
        }

        private void RunDelaySimulatorTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestDelaySimulator();
        }

        private void RunLocalServerTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestLocalServer();
        }

        private void RunEntityCreationTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestBaseEntityCreation();
        }

        private void RunEntityMovementTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestBaseEntityMovement();
        }

        private void RunEntityCombatTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestBaseEntityCombat();
        }

        private void RunEntitySnapshotRestoreTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestBaseEntitySnapshot();
        }

        private void RunLockstepManagerTest()
        {
            var tests = GetOrCreateLockstepTests();
            tests.TestLockstepManager();
        }
    }
}