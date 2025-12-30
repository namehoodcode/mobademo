// LogicFrame.cs - 逻辑帧数据结构
// 用于帧同步系统，存储每个逻辑帧的完整状态
// 无Unity依赖，保证确定性

using System;

namespace MobaCombatCore.Core.Lockstep
{
    /// <summary>
    /// 逻辑帧状态
    /// </summary>
    public enum FrameState : byte
    {
        /// <summary>
        /// 等待输入
        /// </summary>
        WaitingForInput,

        /// <summary>
        /// 输入完整，准备执行
        /// </summary>
        Ready,

        /// <summary>
        /// 正在执行
        /// </summary>
        Executing,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 已回滚
        /// </summary>
        RolledBack
    }

    /// <summary>
    /// 逻辑帧数据 - 包含帧的元数据和执行状态
    /// </summary>
    public class LogicFrame
    {
        /// <summary>
        /// 帧号（从0开始）
        /// </summary>
        public int FrameNumber { get; private set; }

        /// <summary>
        /// 帧状态
        /// </summary>
        public FrameState State { get; set; }

        /// <summary>
        /// 帧输入数据
        /// </summary>
        public FrameInput Input { get; set; }

        /// <summary>
        /// 帧开始时间戳（毫秒）
        /// </summary>
        public long StartTimestamp { get; set; }

        /// <summary>
        /// 帧结束时间戳（毫秒）
        /// </summary>
        public long EndTimestamp { get; set; }

        /// <summary>
        /// 帧执行耗时（毫秒）
        /// </summary>
        public long ExecutionTime => EndTimestamp - StartTimestamp;

        /// <summary>
        /// 状态哈希（用于同步校验）
        /// </summary>
        public long StateHash { get; set; }

        /// <summary>
        /// 是否为关键帧（需要保存快照）
        /// </summary>
        public bool IsKeyFrame { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public LogicFrame(int frameNumber)
        {
            FrameNumber = frameNumber;
            State = FrameState.WaitingForInput;
            StartTimestamp = 0;
            EndTimestamp = 0;
            StateHash = 0;
            IsKeyFrame = false;
        }

        /// <summary>
        /// 标记帧开始执行
        /// </summary>
        public void BeginExecution()
        {
            State = FrameState.Executing;
            StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 标记帧执行完成
        /// </summary>
        public void EndExecution()
        {
            State = FrameState.Completed;
            EndTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 标记帧已回滚
        /// </summary>
        public void MarkRolledBack()
        {
            State = FrameState.RolledBack;
        }

        /// <summary>
        /// 检查帧是否可以执行
        /// </summary>
        public bool CanExecute()
        {
            return State == FrameState.Ready && Input != null && Input.IsComplete();
        }

        /// <summary>
        /// 设置输入并更新状态
        /// </summary>
        public void SetInput(FrameInput input)
        {
            Input = input;
            if (input != null && input.IsComplete())
            {
                State = FrameState.Ready;
            }
        }

        /// <summary>
        /// 重置帧状态（用于回滚）
        /// </summary>
        public void Reset()
        {
            State = FrameState.WaitingForInput;
            StartTimestamp = 0;
            EndTimestamp = 0;
            StateHash = 0;
        }

        public override string ToString()
        {
            return $"LogicFrame[{FrameNumber}] State:{State} ExecTime:{ExecutionTime}ms Hash:{StateHash:X8}";
        }
    }
}