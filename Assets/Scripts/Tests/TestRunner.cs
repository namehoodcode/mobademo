using UnityEngine;

namespace MobaCombatCore.Tests
{
    /// <summary>
    /// Unity测试运行器
    /// 在场景中添加此组件，可以通过Inspector按钮运行测试
    /// 也可以在游戏启动时自动运行测试
    /// </summary>
    public class TestRunner : MonoBehaviour
    {
        [Header("测试配置")]
        [Tooltip("是否在启动时自动运行测试")]
        public bool runOnStart = false;

        [Tooltip("是否在测试完成后暂停编辑器")]
        public bool pauseOnComplete = false;

        [Header("Day 1 - 定点数数学库测试")]
        [SerializeField] private FixedMathTests _fixedMathTests;

        [Header("Day 2 - 帧同步系统测试")]
        [SerializeField] private LockstepTests _lockstepTests;

        private void Start()
        {
            // 确保测试组件存在
            EnsureTestComponents();

            if (runOnStart)
            {
                RunAllTests();
            }
        }

        private void EnsureTestComponents()
        {
            if (_fixedMathTests == null)
            {
                _fixedMathTests = GetComponent<FixedMathTests>();
                if (_fixedMathTests == null)
                {
                    _fixedMathTests = gameObject.AddComponent<FixedMathTests>();
                }
            }

            if (_lockstepTests == null)
            {
                _lockstepTests = GetComponent<LockstepTests>();
                if (_lockstepTests == null)
                {
                    _lockstepTests = gameObject.AddComponent<LockstepTests>();
                }
            }
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        [ContextMenu("运行所有测试")]
        public void RunAllTests()
        {
            Debug.Log("========================================");
            Debug.Log("MOBA Combat Core - 完整单元测试");
            Debug.Log("========================================");

            EnsureTestComponents();

            // Day 1 测试
            Debug.Log("\n\n========== Day 1: 定点数数学库测试 ==========\n");
            _fixedMathTests.RunAllTests();

            // Day 2 测试
            Debug.Log("\n\n========== Day 2: 帧同步系统测试 ==========\n");
            _lockstepTests.RunAllTests();

            if (pauseOnComplete)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPaused = true;
#endif
            }
        }

        /// <summary>
        /// 仅运行Day 1测试
        /// </summary>
        [ContextMenu("运行 Day 1 测试 (定点数数学库)")]
        public void RunDay1Tests()
        {
            EnsureTestComponents();
            Debug.Log("\n========== Day 1: 定点数数学库测试 ==========\n");
            _fixedMathTests.RunAllTests();
        }

        /// <summary>
        /// 仅运行Day 2测试
        /// </summary>
        [ContextMenu("运行 Day 2 测试 (帧同步系统)")]
        public void RunDay2Tests()
        {
            EnsureTestComponents();
            Debug.Log("\n========== Day 2: 帧同步系统测试 ==========\n");
            _lockstepTests.RunAllTests();
        }

        /// <summary>
        /// 运行Fixed64测试
        /// </summary>
        [ContextMenu("运行 Fixed64 测试")]
        public void RunFixed64Tests()
        {
            EnsureTestComponents();
            Debug.Log("\n========== Fixed64 测试 ==========");
            _fixedMathTests.TestFixed64Creation();
            _fixedMathTests.TestFixed64BasicOperations();
            _fixedMathTests.TestFixed64Comparison();
            _fixedMathTests.TestFixed64MathFunctions();
            _fixedMathTests.TestFixed64EdgeCases();
        }

        /// <summary>
        /// 运行FixedVector3测试
        /// </summary>
        [ContextMenu("运行 FixedVector3 测试")]
        public void RunFixedVector3Tests()
        {
            EnsureTestComponents();
            Debug.Log("\n========== FixedVector3 测试 ==========");
            _fixedMathTests.TestFixedVector3Creation();
            _fixedMathTests.TestFixedVector3Operations();
            _fixedMathTests.TestFixedVector3VectorMath();
            _fixedMathTests.TestFixedVector3Distance();
        }

        /// <summary>
        /// 运行FixedMath测试
        /// </summary>
        [ContextMenu("运行 FixedMath 测试")]
        public void RunFixedMathTests()
        {
            EnsureTestComponents();
            Debug.Log("\n========== FixedMath 测试 ==========");
            _fixedMathTests.TestFixedMathTrigonometry();
            _fixedMathTests.TestFixedMathAngle();
            _fixedMathTests.TestFixedMathInterpolation();
        }

        /// <summary>
        /// 运行FixedRandom测试
        /// </summary>
        [ContextMenu("运行 FixedRandom 测试")]
        public void RunFixedRandomTests()
        {
            EnsureTestComponents();
            Debug.Log("\n========== FixedRandom 测试 ==========");
            _fixedMathTests.TestFixedRandomDeterminism();
            _fixedMathTests.TestFixedRandomRange();
            _fixedMathTests.TestFixedRandomDistribution();
        }

        /// <summary>
        /// 运行帧同步核心测试
        /// </summary>
        [ContextMenu("运行 Lockstep 核心测试")]
        public void RunLockstepCoreTests()
        {
            EnsureTestComponents();
            Debug.Log("\n========== Lockstep 核心测试 ==========");
            _lockstepTests.TestPlayerInput();
            _lockstepTests.TestFrameInput();
            _lockstepTests.TestInputBuffer();
            _lockstepTests.TestLogicFrame();
            _lockstepTests.TestLockstepConfig();
        }

        /// <summary>
        /// 运行快照系统测试
        /// </summary>
        [ContextMenu("运行 Snapshot 测试")]
        public void RunSnapshotTests()
        {
            EnsureTestComponents();
            Debug.Log("\n========== Snapshot 测试 ==========");
            _lockstepTests.TestEntitySnapshot();
            _lockstepTests.TestGameSnapshot();
            _lockstepTests.TestSnapshotManager();
        }

        /// <summary>
        /// 运行网络模拟测试
        /// </summary>
        [ContextMenu("运行 Network 测试")]
        public void RunNetworkTests()
        {
            EnsureTestComponents();
            Debug.Log("\n========== Network 测试 ==========");
            _lockstepTests.TestDelayConfig();
            _lockstepTests.TestDelaySimulator();
            _lockstepTests.TestLocalServer();
        }

        /// <summary>
        /// 运行实体测试
        /// </summary>
        [ContextMenu("运行 Entity 测试")]
        public void RunEntityTests()
        {
            EnsureTestComponents();
            Debug.Log("\n========== Entity 测试 ==========");
            _lockstepTests.TestBaseEntityCreation();
            _lockstepTests.TestBaseEntityMovement();
            _lockstepTests.TestBaseEntityCombat();
            _lockstepTests.TestBaseEntitySnapshot();
        }

        /// <summary>
        /// 在Inspector中显示使用说明
        /// </summary>
        private void OnValidate()
        {
            if (_fixedMathTests == null)
            {
                _fixedMathTests = GetComponent<FixedMathTests>();
            }
        }
    }
}