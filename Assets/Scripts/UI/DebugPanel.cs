// DebugPanel.cs - Debug面板UI组件
// 实时显示性能监控数据
// 使用Unity IMGUI实现，便于调试

using UnityEngine;
using MobaCombatCore.Core.Lockstep;
using MobaCombatCore.Glue;

namespace MobaCombatCore.UI
{
    /// <summary>
    /// Debug面板UI - 实时显示性能监控数据
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        #region 配置

        [Header("显示设置")]
        [Tooltip("是否显示Debug面板")]
        public bool showPanel = true;

        [Tooltip("面板位置")]
        public Rect panelRect = new Rect(10, 10, 320, 450);

        [Tooltip("字体大小")]
        public int fontSize = 14;

        [Header("颜色设置")]
        public Color titleColor = Color.yellow;
        public Color normalColor = Color.white;
        public Color goodColor = Color.green;
        public Color warningColor = Color.yellow;
        public Color badColor = Color.red;

        [Header("性能阈值")]
        [Tooltip("逻辑帧耗时警告阈值（毫秒）")]
        public float logicFrameWarningMs = 2f;
        [Tooltip("逻辑帧耗时危险阈值（毫秒）")]
        public float logicFrameDangerMs = 3f;
        [Tooltip("渲染FPS警告阈值")]
        public float renderFPSWarning = 45f;
        [Tooltip("渲染FPS危险阈值")]
        public float renderFPSDanger = 30f;

        [Header("引用")]
        [Tooltip("GameManager引用（可选，自动查找）")]
        public GameManager gameManager;

        #endregion

        #region 私有字段

        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _boxStyle;
        private bool _stylesInitialized;

        private LockstepManager _lockstepManager;
        private GameWorld _gameWorld;

        // 性能数据缓存
        private float _renderFPS;
        private float _renderFrameTime;
        private int _frameCount;
        private float _fpsTimer;

        // 内存统计
        private long _lastGCAlloc;
        private float _gcAllocPerFrame;
        private float _memoryUpdateTimer;

        #endregion

        #region 生命周期

        private void Start()
        {
            // 自动查找GameManager
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }

            // 获取引用
            if (gameManager != null)
            {
                // 通过反射或公开属性获取内部引用
                var field = typeof(GameManager).GetField("_lockstepManager",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    _lockstepManager = field.GetValue(gameManager) as LockstepManager;
                }

                var worldField = typeof(GameManager).GetField("_gameWorld",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (worldField != null)
                {
                    _gameWorld = worldField.GetValue(gameManager) as GameWorld;
                }
            }
        }

        private void Update()
        {
            if (!showPanel) return;

            // 更新FPS统计
            _frameCount++;
            _fpsTimer += Time.unscaledDeltaTime;

            if (_fpsTimer >= 0.5f)
            {
                _renderFPS = _frameCount / _fpsTimer;
                _renderFrameTime = _fpsTimer / _frameCount * 1000f;
                _frameCount = 0;
                _fpsTimer = 0f;
            }

            // 更新内存统计（每秒一次）
            _memoryUpdateTimer += Time.unscaledDeltaTime;
            if (_memoryUpdateTimer >= 1f)
            {
                long currentAlloc = System.GC.GetTotalMemory(false);
                _gcAllocPerFrame = (currentAlloc - _lastGCAlloc) / 1024f; // KB
                _lastGCAlloc = currentAlloc;
                _memoryUpdateTimer = 0f;
            }

            // 更新PerformanceMonitor
            PerformanceMonitor.Instance.RecordRenderFrame(Time.unscaledDeltaTime);

            if (_lockstepManager != null)
            {
                PerformanceMonitor.Instance.RecordLogicFrame(
                    _lockstepManager.CurrentFrame,
                    _lockstepManager.LastLogicFrameTime
                );
                PerformanceMonitor.Instance.SetLogicFrameRate(_lockstepManager.Config.LogicFrameRate);
            }

            if (_gameWorld != null)
            {
                int projectiles = _gameWorld.Projectiles?.Count ?? 0;
                int total = _gameWorld.AllEntities?.Count ?? 0;
                PerformanceMonitor.Instance.UpdateEntityStats(total, total, projectiles, 1, total - projectiles - 1);
            }
        }

        private void OnGUI()
        {
            if (!showPanel) return;

            InitializeStyles();

            // 绘制面板背景
            GUI.Box(panelRect, "", _boxStyle);

            GUILayout.BeginArea(panelRect);
            GUILayout.Space(5);

            // 标题
            GUILayout.Label("MOBA Combat Core - Debug", _titleStyle);
            GUILayout.Space(5);

            // 分隔线
            DrawSeparator();

            // 渲染性能
            DrawSection("渲染性能", () =>
            {
                DrawLabelValue("Render FPS", $"{_renderFPS:F1}", GetFPSColor(_renderFPS));
                DrawLabelValue("Frame Time", $"{_renderFrameTime:F2} ms", normalColor);
            });

            // 逻辑性能
            if (_lockstepManager != null)
            {
                DrawSection("逻辑性能", () =>
                {
                    DrawLabelValue("Logic FPS", $"{_lockstepManager.Config.LogicFrameRate}", normalColor);
                    DrawLabelValue("Logic Frame", $"{_lockstepManager.CurrentFrame}", normalColor);
                    DrawLabelValue("Logic Cost", $"{_lockstepManager.LastLogicFrameTime:F2} ms",
                        GetLogicTimeColor(_lockstepManager.LastLogicFrameTime));
                    DrawLabelValue("Avg Cost", $"{_lockstepManager.AverageLogicFrameTime:F2} ms",
                        GetLogicTimeColor(_lockstepManager.AverageLogicFrameTime));
                });
            }

            // 实体统计
            if (_gameWorld != null)
            {
                DrawSection("实体统计", () =>
                {
                    int total = _gameWorld.AllEntities?.Count ?? 0;
                    int projectiles = _gameWorld.Projectiles?.Count ?? 0;
                    DrawLabelValue("Total Entities", $"{total}", normalColor);
                    DrawLabelValue("Projectiles", $"{projectiles}", normalColor);
                });
            }

            // 内存统计
            DrawSection("内存统计", () =>
            {
                long totalMemory = System.GC.GetTotalMemory(false) / (1024 * 1024);
                DrawLabelValue("Total Memory", $"{totalMemory} MB", normalColor);
                DrawLabelValue("GC Alloc/s", $"{_gcAllocPerFrame:F1} KB",
                    _gcAllocPerFrame > 10 ? warningColor : goodColor);
            });

            // 网络模拟
            if (_lockstepManager != null)
            {
                DrawSection("网络模拟", () =>
                {
                    DrawLabelValue("Delay", $"{_lockstepManager.Config.SimulatedDelayMs} ms", normalColor);
                    DrawLabelValue("Rollbacks", $"{_lockstepManager.RollbackCount}",
                        _lockstepManager.RollbackCount > 0 ? warningColor : goodColor);
                    DrawLabelValue("Prediction",
                        _lockstepManager.Config.EnablePrediction ? "ON" : "OFF",
                        _lockstepManager.Config.EnablePrediction ? goodColor : normalColor);
                });
            }

            // 控制按钮
            GUILayout.Space(10);
            DrawSeparator();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Stats", GUILayout.Height(25)))
            {
                PerformanceMonitor.Instance.Reset();
            }
            if (GUILayout.Button(showPanel ? "Hide" : "Show", GUILayout.Height(25)))
            {
                showPanel = !showPanel;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        #endregion

        #region 绘制辅助方法

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize + 2,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _titleStyle.normal.textColor = titleColor;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleLeft
            };
            _labelStyle.normal.textColor = normalColor;

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleRight
            };
            _valueStyle.normal.textColor = normalColor;

            _boxStyle = new GUIStyle(GUI.skin.box);
            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0, 0, 0, 0.8f));
            bgTex.Apply();
            _boxStyle.normal.background = bgTex;

            _stylesInitialized = true;
        }

        private void DrawSection(string title, System.Action drawContent)
        {
            GUILayout.Space(3);

            var sectionStyle = new GUIStyle(_labelStyle)
            {
                fontStyle = FontStyle.Bold
            };
            sectionStyle.normal.textColor = titleColor;

            GUILayout.Label($"[ {title} ]", sectionStyle);

            drawContent?.Invoke();
        }

        private void DrawLabelValue(string label, string value, Color valueColor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"  {label}:", _labelStyle, GUILayout.Width(120));

            var coloredValueStyle = new GUIStyle(_valueStyle);
            coloredValueStyle.normal.textColor = valueColor;
            GUILayout.Label(value, coloredValueStyle);

            GUILayout.EndHorizontal();
        }

        private void DrawSeparator()
        {
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        }

        private Color GetFPSColor(float fps)
        {
            if (fps >= 60f) return goodColor;
            if (fps >= renderFPSWarning) return normalColor;
            if (fps >= renderFPSDanger) return warningColor;
            return badColor;
        }

        private Color GetLogicTimeColor(float timeMs)
        {
            if (timeMs <= logicFrameWarningMs) return goodColor;
            if (timeMs <= logicFrameDangerMs) return warningColor;
            return badColor;
        }

        #endregion

        #region 快捷键

        private void LateUpdate()
        {
            // F3 切换面板显示
            if (Input.GetKeyDown(KeyCode.F3))
            {
                showPanel = !showPanel;
            }
        }

        #endregion
    }
}
