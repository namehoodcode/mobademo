// LockstepConfig.cs - 帧同步配置
// 集中管理所有帧同步相关的参数
// 无Unity依赖

namespace MobaCombatCore.Core.Lockstep
{
    public class LockstepConfig
    {
        /// <summary>
        /// 逻辑帧率（每秒）
        /// </summary>
        public int LogicFrameRate { get; set; } = 30;

        /// <summary>
        /// 逻辑帧间隔（秒）
        /// </summary>
        public float LogicFrameIntervalSeconds => 1f / LogicFrameRate;

        /// <summary>
        /// 玩家数量
        /// </summary>
        public int PlayerCount { get; set; } = 2;

        /// <summary>
        /// 最大追帧数量
        /// </summary>
        public int MaxCatchUpFrames { get; set; } = 5;

        /// <summary>
        // 输入缓冲帧数
        /// </summary>
        public int InputBufferFrames { get; set; } = 2;

        /// <summary>
        /// 是否启用客户端预测
        /// </summary>
        public bool EnablePrediction { get; set; } = true;

        /// <summary>
        /// 模拟网络延迟（毫秒）
        /// </summary>
        public int SimulatedDelayMs { get; set; } = 0;

        /// <summary>
        /// 快照回滚的最大帧数
        /// </summary>
        public int MaxRollbackFrames { get; set; } = 60;

        /// <summary>
        /// 关键帧间隔（每隔多少帧保存一次快照）
        /// </summary>
        public int KeyFrameInterval { get; set; } = 10;

        /// <summary>
        /// 是否启用状态校验
        /// </summary>
        public bool EnableStateValidation { get; set; } = true;

        /// <summary>
        /// 默认配置
        /// </summary>
        public static LockstepConfig Default => new LockstepConfig();

        /// <summary>
        /// 高性能配置（60fps逻辑帧）
        /// </summary>
        public static LockstepConfig HighPerformance => new LockstepConfig
        {
            LogicFrameRate = 60,
            KeyFrameInterval = 15,
            MaxCatchUpFrames = 3
        };

        /// <summary>
        /// 低延迟配置
        /// </summary>
        public static LockstepConfig LowLatency => new LockstepConfig
        {
            LogicFrameRate = 30,
            InputBufferFrames = 1,
            EnablePrediction = true
        };
    }
}
