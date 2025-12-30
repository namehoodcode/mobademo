// GameLog.cs - 静态门面日志类
// 提供全局静态访问的日志功能，同时支持可替换的后端实现

namespace MobaCombatCore.Glue.Services
{
    /// <summary>
    /// 静态门面日志类
    /// 提供全局静态访问的日志功能，内部持有可替换的 ILogService 实现
    /// 
    /// 使用场景：
    /// - 开发中：GameLog.Info("Player", "Spawn", "Player Spawned"); （像单例一样方便）
    /// - 单元测试中：GameLog.SetLogger(mockLogger); 注入模拟日志器
    /// - 真机发布时：GameLog.SetLogger(new FileLogger()); 替换为文件日志
    /// </summary>
    public static class GameLog
    {
        /// <summary>
        /// 内部日志实现，默认使用 UnityLogService
        /// </summary>
        private static ILogService _impl = new UnityLogService();

        /// <summary>
        /// 设置日志实现（依赖注入的变体）
        /// 允许在启动时或测试时注入不同的实现
        /// </summary>
        /// <param name="logger">日志服务实现</param>
        public static void SetLogger(ILogService logger)
        {
            _impl = logger ?? new UnityLogService();
        }

        /// <summary>
        /// 获取当前日志实现（用于高级配置）
        /// </summary>
        public static ILogService GetLogger()
        {
            return _impl;
        }

        /// <summary>
        /// 当前日志级别
        /// </summary>
        public static LogLevel CurrentLogLevel
        {
            get => _impl.CurrentLogLevel;
            set => _impl.CurrentLogLevel = value;
        }

        /// <summary>
        /// 是否启用日志
        /// </summary>
        public static bool IsEnabled
        {
            get => _impl.IsEnabled;
            set => _impl.IsEnabled = value;
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="tag">日志标签（通常是类名）</param>
        /// <param name="method">方法名</param>
        /// <param name="message">日志消息</param>
        public static void Debug(string tag, string method, string message)
        {
            _impl.LogDebug(tag, method, message);
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="tag">日志标签（通常是类名）</param>
        /// <param name="method">方法名</param>
        /// <param name="message">日志消息</param>
        public static void Info(string tag, string method, string message)
        {
            _impl.LogInfo(tag, method, message);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="tag">日志标签（通常是类名）</param>
        /// <param name="method">方法名</param>
        /// <param name="message">日志消息</param>
        public static void Warning(string tag, string method, string message)
        {
            _impl.LogWarning(tag, method, message);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="tag">日志标签（通常是类名）</param>
        /// <param name="method">方法名</param>
        /// <param name="message">日志消息</param>
        public static void Error(string tag, string method, string message)
        {
            _impl.LogError(tag, method, message);
        }

        /// <summary>
        /// 重置为默认日志实现
        /// </summary>
        public static void ResetToDefault()
        {
            _impl = new UnityLogService();
        }
    }
}