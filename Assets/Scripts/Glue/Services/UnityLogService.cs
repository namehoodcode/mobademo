// UnityLogService.cs - Unity日志服务实现
// 使用Unity的Debug类输出日志
// 优化：避免不必要的字符串分配

using System.Text;
using UnityEngine;

namespace MobaCombatCore.Glue.Services
{
    /// <summary>
    /// Unity日志服务实现
    /// 优化版本：使用StringBuilder缓存，避免频繁的字符串分配
    /// </summary>
    public class UnityLogService : ILogService
    {
        /// <summary>
        /// 当前日志级别
        /// </summary>
        public LogLevel CurrentLogLevel { get; set; } = LogLevel.Debug;

        /// <summary>
        /// 是否启用日志
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 日志频率控制 - 每N帧记录一次重复日志
        /// </summary>
        public int LogFrequency { get; set; } = 1;

        private int _frameCounter = 0;
        
        /// <summary>
        /// 共享的StringBuilder，避免每次日志都分配新字符串
        /// 注意：非线程安全，但Unity主线程单线程执行所以没问题
        /// </summary>
        [System.ThreadStatic]
        private static StringBuilder _sharedBuilder;
        
        private static StringBuilder GetBuilder()
        {
            if (_sharedBuilder == null)
            {
                _sharedBuilder = new StringBuilder(256);
            }
            return _sharedBuilder;
        }

        /// <summary>
        /// 记录调试日志
        /// 优化：先检查日志级别，避免不必要的字符串操作
        /// </summary>
        public void LogDebug(string tag, string method, string message)
        {
            // 先检查是否需要记录，避免不必要的字符串分配
            if (!IsEnabled) return;
            if (LogLevel.Debug < CurrentLogLevel) return;
            
            Debug.Log(FormatMessageOptimized(tag, method, message));
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public void LogInfo(string tag, string method, string message)
        {
            if (!IsEnabled) return;
            if (LogLevel.Info < CurrentLogLevel) return;
            
            Debug.Log(FormatMessageOptimized(tag, method, message));
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public void LogWarning(string tag, string method, string message)
        {
            if (!IsEnabled) return;
            if (LogLevel.Warning < CurrentLogLevel) return;
            
            Debug.LogWarning(FormatMessageOptimized(tag, method, message));
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public void LogError(string tag, string method, string message)
        {
            if (!IsEnabled) return;
            if (LogLevel.Error < CurrentLogLevel) return;
            
            Debug.LogError(FormatMessageOptimized(tag, method, message));
        }

        /// <summary>
        /// 检查是否应该记录日志
        /// </summary>
        private bool ShouldLog(LogLevel level)
        {
            if (!IsEnabled) return false;
            if (level < CurrentLogLevel) return false;
            return true;
        }

        /// <summary>
        /// 格式化日志消息（优化版本，使用StringBuilder）
        /// </summary>
        private string FormatMessageOptimized(string tag, string method, string message)
        {
            var sb = GetBuilder();
            sb.Clear();
            sb.Append('[');
            sb.Append(tag);
            sb.Append("] [");
            sb.Append(method);
            sb.Append("] ");
            sb.Append(message);
            return sb.ToString();
        }

        /// <summary>
        /// 增加帧计数器（用于频率控制）
        /// </summary>
        public void IncrementFrameCounter()
        {
            _frameCounter++;
        }

        /// <summary>
        /// 检查是否应该记录周期性日志
        /// </summary>
        public bool ShouldLogPeriodic()
        {
            return _frameCounter % LogFrequency == 0;
        }

        /// <summary>
        /// 重置帧计数器
        /// </summary>
        public void ResetFrameCounter()
        {
            _frameCounter = 0;
        }
    }
}