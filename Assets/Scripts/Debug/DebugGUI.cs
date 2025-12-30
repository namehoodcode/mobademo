// DebugGUI.cs - Debug GUI 静态门面
// 提供统一的调试信息显示接口
// 使用方法：在任意脚本中调用 DebugGUI.AddLabel() 注册要显示的信息

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MobaCombatCore.DebugTools
{
    /// <summary>
    /// Debug GUI 静态门面 - 提供统一的调试信息显示接口
    /// </summary>
    public static class DebugGUI
    {
        #region 配置

        /// <summary>
        /// 是否启用 Debug GUI
        /// </summary>
        public static bool IsEnabled { get; set; } = true;

        /// <summary>
        /// GUI 区域位置（默认右上角）
        /// </summary>
        public static Rect GUIArea { get; set; } = new Rect(Screen.width - 360, 10, 350, 500);
        
        /// <summary>
        /// 是否自动定位到右上角
        /// </summary>
        public static bool AutoPositionTopRight { get; set; } = true;

        /// <summary>
        /// 标签样式
        /// </summary>
        public static GUIStyle LabelStyle { get; set; }

        /// <summary>
        /// 标题样式
        /// </summary>
        public static GUIStyle TitleStyle { get; set; }

        #endregion

        #region 内部数据

        private static readonly Dictionary<string, Func<string>> _labelProviders = new Dictionary<string, Func<string>>();
        private static readonly List<string> _orderedKeys = new List<string>();
        private static bool _stylesInitialized = false;

        #endregion

        #region 公开方法

        /// <summary>
        /// 注册一个标签（每帧动态获取内容）
        /// </summary>
        /// <param name="key">唯一标识符</param>
        /// <param name="contentProvider">内容提供函数</param>
        public static void AddLabel(string key, Func<string> contentProvider)
        {
            if (string.IsNullOrEmpty(key) || contentProvider == null) return;

            if (!_labelProviders.ContainsKey(key))
            {
                _orderedKeys.Add(key);
            }
            _labelProviders[key] = contentProvider;
        }

        /// <summary>
        /// 注册一个静态标签
        /// </summary>
        /// <param name="key">唯一标识符</param>
        /// <param name="content">静态内容</param>
        public static void AddLabel(string key, string content)
        {
            AddLabel(key, () => content);
        }

        /// <summary>
        /// 移除标签
        /// </summary>
        /// <param name="key">唯一标识符</param>
        public static void RemoveLabel(string key)
        {
            if (_labelProviders.ContainsKey(key))
            {
                _labelProviders.Remove(key);
                _orderedKeys.Remove(key);
            }
        }

        /// <summary>
        /// 清除所有标签
        /// </summary>
        public static void ClearAll()
        {
            _labelProviders.Clear();
            _orderedKeys.Clear();
        }

        /// <summary>
        /// 绘制 GUI（需要在 OnGUI 中调用）
        /// </summary>
        public static void Draw()
        {
            if (!IsEnabled || !Application.isPlaying) return;

            InitializeStyles();
            
            // 如果启用自动定位，将面板放到右上角
            Rect area = GUIArea;
            if (AutoPositionTopRight)
            {
                area = new Rect(Screen.width - GUIArea.width - 10, 10, GUIArea.width, GUIArea.height);
            }

            GUILayout.BeginArea(area);
            
            // 绘制标题
            GUILayout.Label("=== Debug Info ===", TitleStyle);
            GUILayout.Space(5);

            // 绘制所有注册的标签
            foreach (var key in _orderedKeys)
            {
                if (_labelProviders.TryGetValue(key, out var provider))
                {
                    try
                    {
                        var content = provider();
                        if (!string.IsNullOrEmpty(content))
                        {
                            GUILayout.Label(content, LabelStyle);
                        }
                    }
                    catch (Exception e)
                    {
                        GUILayout.Label($"[Error: {key}] {e.Message}", LabelStyle);
                    }
                }
            }

            GUILayout.EndArea();
        }

        #endregion

        #region 内部方法

        private static void InitializeStyles()
        {
            if (_stylesInitialized) return;

            LabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };

            TitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };

            _stylesInitialized = true;
        }

        #endregion
    }

    /// <summary>
    /// DebugGUI 渲染器组件 - 挂载到场景中的 GameObject 上
    /// 负责在 OnGUI 中调用 DebugGUI.Draw()
    /// </summary>
    public class DebugGUIRenderer : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("是否启用 Debug GUI")]
        public bool enableDebugGUI = true;

        [Tooltip("GUI 区域位置 X")]
        public float areaX = 10;
        
        [Tooltip("GUI 区域位置 Y")]
        public float areaY = 10;
        
        [Tooltip("GUI 区域宽度")]
        public float areaWidth = 350;
        
        [Tooltip("GUI 区域高度")]
        public float areaHeight = 500;

        private void Start()
        {
            DebugGUI.IsEnabled = enableDebugGUI;
            DebugGUI.GUIArea = new Rect(areaX, areaY, areaWidth, areaHeight);
        }

        private void OnValidate()
        {
            DebugGUI.IsEnabled = enableDebugGUI;
            DebugGUI.GUIArea = new Rect(areaX, areaY, areaWidth, areaHeight);
        }

        private void OnGUI()
        {
            DebugGUI.Draw();
        }
    }
}