// StressTest.cs
// 用于压力测试的脚本，模拟大量弹道发射
//
// 使用方法：
// 1. 在场景中创建一个空的 GameObject
// 2. 添加此脚本组件
// 3. 运行场景，通过 GUI 按钮控制测试的启动/停止/重置
// 4. 可以在 GUI 中调整每秒发射弹道数
//
// 符合架构设计原则："需要数据去GameWorld"

using MobaCombatCore.Core.Math;
using MobaCombatCore.Gameplay.Entity;
using MobaCombatCore.Glue;
using UnityEngine;

namespace MobaCombatCore.DebugTools
{
    /// <summary>
    /// 压力测试脚本 - 模拟大量弹道发射
    /// 用于验证性能指标：500弹道+10敌人时逻辑帧耗时<3ms
    /// </summary>
    public class StressTest : MonoBehaviour
    {
        [Header("测试配置")]
        [Tooltip("每秒发射的弹道数量")]
        public int projectilesPerSecond = 5;
        
        [Tooltip("测试持续时间（秒）")]
        public float duration = 100f;

        [Header("运行时状态（只读）")]
        [SerializeField] private int _totalProjectilesFired;
        [SerializeField] private float _elapsedTime;
        [SerializeField] private bool _isRunning = false;

        private float _spawnTimer;
        private HeroEntity _hero;
        
        // GUI 控制相关
        private string _projectilesPerSecondInput;
        private GUIStyle _buttonStyle;
        private GUIStyle _inputStyle;
        private GUIStyle _labelStyleLocal;
        private bool _guiStylesInitialized = false;

        private void Start()
        {
            // 初始化输入框的值
            _projectilesPerSecondInput = projectilesPerSecond.ToString();
            
            // 注册 Debug GUI 标签
            RegisterDebugLabels();
        }

        private void OnDestroy()
        {
            // 移除 Debug GUI 标签
            UnregisterDebugLabels();
        }

        private void Update()
        {
            // 如果测试未运行或已超时，不执行
            if (!_isRunning || _elapsedTime > duration) return;

            _elapsedTime += Time.deltaTime;
            _spawnTimer += Time.deltaTime;

            // 通过 GameWorld.Current 获取英雄实体
            // 符合设计原则："需要数据去GameWorld"
            if (_hero == null && GameWorld.Current != null)
            {
                _hero = GameWorld.Current.GetPlayerHero();
            }

            if (_hero != null && _spawnTimer >= 1f / projectilesPerSecond)
            {
                _spawnTimer = 0;

                // 随机一个方向
                var randomAngle = Random.Range(0f, 360f);
                var direction = new FixedVector3(
                    Fixed64.FromFloat(Mathf.Cos(randomAngle * Mathf.Deg2Rad)),
                    Fixed64.Zero,
                    Fixed64.FromFloat(Mathf.Sin(randomAngle * Mathf.Deg2Rad))
                );

                _hero.SkillExecutor.CastSkillForced(0, direction);
                _totalProjectilesFired++;
            }
        }

        #region 公开控制方法

        /// <summary>
        /// 开始压力测试
        /// </summary>
        public void StartTest()
        {
            _isRunning = true;
        }

        /// <summary>
        /// 停止压力测试
        /// </summary>
        public void StopTest()
        {
            _isRunning = false;
        }

        /// <summary>
        /// 重置压力测试
        /// </summary>
        public void ResetTest()
        {
            _isRunning = false;
            _elapsedTime = 0f;
            _totalProjectilesFired = 0;
            _spawnTimer = 0f;
        }

        #endregion

        #region Debug GUI

        private void RegisterDebugLabels()
        {
            DebugGUI.AddLabel("StressTest_Title", "--- 压力测试 ---");
            DebugGUI.AddLabel("StressTest_Status", () => $"状态: {(_isRunning ? "<color=green>运行中</color>" : "<color=red>已停止</color>")}");
            DebugGUI.AddLabel("StressTest_Progress", () => $"测试进度: {_elapsedTime:F1}s / {duration}s");
            DebugGUI.AddLabel("StressTest_Fired", () => $"已发射弹道: {_totalProjectilesFired}");
            DebugGUI.AddLabel("StressTest_Current", () => $"当前弹道数: {GameWorld.Current?.Projectiles.Count ?? 0}");
            DebugGUI.AddLabel("StressTest_Entities", () => $"总实体数: {GameWorld.Current?.AllEntities.Count ?? 0}");
        }

        private void UnregisterDebugLabels()
        {
            DebugGUI.RemoveLabel("StressTest_Title");
            DebugGUI.RemoveLabel("StressTest_Status");
            DebugGUI.RemoveLabel("StressTest_Progress");
            DebugGUI.RemoveLabel("StressTest_Fired");
            DebugGUI.RemoveLabel("StressTest_Current");
            DebugGUI.RemoveLabel("StressTest_Entities");
        }

        private void InitializeGUIStyles()
        {
            if (_guiStylesInitialized) return;

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            _inputStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            _labelStyleLocal = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };

            _guiStylesInitialized = true;
        }

        /// <summary>
        /// 渲染 Debug GUI
        /// </summary>
        private void OnGUI()
        {
            // 先绘制 DebugGUI 的标签
            DebugGUI.Draw();
            
            // 绘制控制面板
            DrawControlPanel();
        }

        /// <summary>
        /// 绘制控制面板
        /// </summary>
        private void DrawControlPanel()
        {
            if (!Application.isPlaying) return;

            InitializeGUIStyles();

            // 控制面板位置（在屏幕右下角）
            float panelWidth = 280;
            float panelHeight = 120;
            float panelX = Screen.width - panelWidth - 10;
            float panelY = Screen.height - panelHeight - 10;

            GUILayout.BeginArea(new Rect(panelX, panelY, panelWidth, panelHeight));

            // 标题
            GUILayout.Label("=== 压力测试控制 ===", new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan }
            });

            GUILayout.Space(5);

            // 每秒发射数量设置
            GUILayout.BeginHorizontal();
            GUILayout.Label("每秒发射数:", _labelStyleLocal, GUILayout.Width(100));
            _projectilesPerSecondInput = GUILayout.TextField(_projectilesPerSecondInput, _inputStyle, GUILayout.Width(60));
            if (GUILayout.Button("应用", _buttonStyle, GUILayout.Width(50)))
            {
                if (int.TryParse(_projectilesPerSecondInput, out int value) && value > 0)
                {
                    projectilesPerSecond = value;
                }
                else
                {
                    _projectilesPerSecondInput = projectilesPerSecond.ToString();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 控制按钮
            GUILayout.BeginHorizontal();
            
            // 开始/停止按钮
            if (_isRunning)
            {
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("停止", _buttonStyle, GUILayout.Height(30)))
                {
                    StopTest();
                }
            }
            else
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("开始", _buttonStyle, GUILayout.Height(30)))
                {
                    StartTest();
                }
            }

            // 重置按钮
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("重置", _buttonStyle, GUILayout.Height(30)))
            {
                ResetTest();
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        #endregion
    }
}