// ILogService.cs - 日志服务接口
// 提供统一的日志记录功能

namespace MobaCombatCore.Glue.Services
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        None = 4
    }

    /// <summary>
    /// 日志服务接口
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// 当前日志级别
        /// </summary>
        LogLevel CurrentLogLevel { get; set; }

        /// <summary>
        /// 是否启用日志
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        void LogDebug(string tag, string method, string message);

        /// <summary>
        /// 记录信息日志
        /// </summary>
        void LogInfo(string tag, string method, string message);

        /// <summary>
        /// 记录警告日志
        /// </summary>
        void LogWarning(string tag, string method, string message);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        void LogError(string tag, string method, string message);
    }
}