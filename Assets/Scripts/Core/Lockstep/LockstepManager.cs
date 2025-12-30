// LockstepManager.cs - 帧同步核心管理器
// 负责逻辑帧的调度、输入收集、状态同步
// 无Unity依赖，保证确定性
// 优化：复用FrameInput对象，避免每帧GC分配

using System;
using System.Collections.Generic;
using MobaCombatCore.Core.Math;
using MobaCombatCore.DebugTools;
using MobaCombatCore.Glue.Services;

namespace MobaCombatCore.Core.Lockstep
{
    /// <summary>
    /// 逻辑更新委托
    /// </summary>
    /// <param name="frameNumber">当前帧号</param>
    /// <param name="deltaTime">逻辑帧间隔（定点数秒）</param>
    /// <param name="input">当前帧输入</param>
    public delegate void LogicUpdateDelegate(int frameNumber, Fixed64 deltaTime, FrameInput input);

    /// <summary>
    /// 帧同步事件类型
    /// </summary>
    public enum LockstepEventType
    {
        FrameStarted,
        FrameCompleted,
        InputReceived,
        SnapshotSaved,
        RollbackStarted,
        RollbackCompleted,
        SyncError
    }

    /// <summary>
    /// 帧同步事件数据
    /// </summary>
    public struct LockstepEvent
    {
        public LockstepEventType Type;
        public int FrameNumber;
        public long Timestamp;
        public string Message;
    }

    /// <summary>
    /// 帧同步核心管理器
    /// </summary>
    public class LockstepManager
    {
        private const string LOG_TAG = "LockstepManager";

        #region 配置与状态

        /// <summary>
        /// 帧同步配置
        /// </summary>
        public LockstepConfig Config { get; private set; }

        /// <summary>
        /// 当前逻辑帧号
        /// </summary>
        public int CurrentFrame { get; private set; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// 累积时间（秒）
        /// </summary>
        private float _accumulatedTime;

        /// <summary>
        /// 逻辑帧间隔（定点数秒）
        /// </summary>
        public Fixed64 LogicDeltaTime { get; private set; }

        /// <summary>
        /// 本地玩家ID
        /// </summary>
        public int LocalPlayerId { get; set; }

        #endregion

        #region 子系统

        /// <summary>
        /// 输入缓冲
        /// </summary>
        public InputBuffer InputBuffer { get; private set; }

        /// <summary>
        /// 快照管理器
        /// </summary>
        public SnapshotManager SnapshotManager { get; private set; }

        /// <summary>
        /// 帧历史记录
        /// </summary>
        private readonly List<LogicFrame> _frameHistory;

        /// <summary>
        /// 最大帧历史数量
        /// </summary>
        private const int MaxFrameHistory = 300;

        /// <summary>
        /// 客户端预测实体（可选）
        /// </summary>
        public Gameplay.Entity.BaseEntity PredictedEntity { get; set; }

        #endregion

        #region 事件与回调

        /// <summary>
        /// 逻辑更新回调
        /// </summary>
        public event LogicUpdateDelegate OnLogicUpdate;

        /// <summary>
        /// 预测更新回调（用于本地玩家）
        /// </summary>
        public event Action<Gameplay.Entity.BaseEntity, FrameInput> OnPredictUpdate;

        /// <summary>
        /// 帧同步事件回调
        /// </summary>
        public event Action<LockstepEvent> OnLockstepEvent;

        /// <summary>
        /// 创建快照回调（由外部实现）
        /// </summary>
        public Func<int, GameSnapshot> OnCreateSnapshot;

        /// <summary>
        /// 恢复快照回调（由外部实现）
        /// </summary>
        public Action<GameSnapshot> OnRestoreSnapshot;

        #endregion

        #region 性能统计

        /// <summary>
        /// 上一帧逻辑耗时（毫秒）
        /// </summary>
        public float LastLogicFrameTime { get; private set; }

        /// <summary>
        /// 平均逻辑帧耗时（毫秒）
        /// </summary>
        public float AverageLogicFrameTime { get; private set; }

        /// <summary>
        /// 总执行帧数
        /// </summary>
        public int TotalFramesExecuted { get; private set; }

        /// <summary>
        /// 回滚次数
        /// </summary>
        public int RollbackCount { get; private set; }

        private readonly Queue<float> _frameTimeHistory;
        private const int FrameTimeHistorySize = 30;
        
        /// <summary>
        /// 复用的空输入对象，避免每帧创建
        /// </summary>
        private FrameInput _reusableEmptyInput;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public LockstepManager(LockstepConfig config = null)
        {
            Config = config ?? LockstepConfig.Default;
            CurrentFrame = 0;
            IsRunning = false;
            IsPaused = false;
            _accumulatedTime = 0f;
            LocalPlayerId = 0;

            // 计算逻辑帧间隔（定点数）
            LogicDeltaTime = Fixed64.One / Fixed64.FromInt(Config.LogicFrameRate);

            // 初始化子系统
            InputBuffer = new InputBuffer(Config.PlayerCount);
            SnapshotManager = new SnapshotManager(Config.MaxRollbackFrames, Config.KeyFrameInterval);
            _frameHistory = new List<LogicFrame>();
            _frameTimeHistory = new Queue<float>();
            
            // 预创建复用的空输入对象
            _reusableEmptyInput = new FrameInput(0, Config.PlayerCount);
        }

        #region 生命周期

        /// <summary>
        /// 启动帧同步
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;

            IsRunning = true;
            CurrentFrame = 0;
            _accumulatedTime = 0f;
            TotalFramesExecuted = 0;
            RollbackCount = 0;

            InputBuffer.Clear();
            SnapshotManager.Clear();
            _frameHistory.Clear();
            _frameTimeHistory.Clear();

            RaiseEvent(LockstepEventType.FrameStarted, 0, "Lockstep started");
        }

        /// <summary>
        /// 停止帧同步
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            RaiseEvent(LockstepEventType.FrameCompleted, CurrentFrame, "Lockstep stopped");
        }

        /// <summary>
        /// 重置帧同步
        /// </summary>
        public void Reset()
        {
            Stop();
            CurrentFrame = 0;
            _accumulatedTime = 0f;
            InputBuffer.Clear();
            SnapshotManager.Clear();
            _frameHistory.Clear();
            GizmosDrawer.Instance?.ClearAll();
        }

        #endregion

        #region 主循环

        /// <summary>
        /// 每帧更新（由Unity Update调用）
        /// </summary>
        /// <param name="deltaTime">渲染帧间隔（秒）</param>
        public void Update(float deltaTime)
        {
            if (!IsRunning || IsPaused) return;

            // 累积时间
            _accumulatedTime += deltaTime;

            // 计算需要执行的逻辑帧数
            float logicInterval = Config.LogicFrameIntervalSeconds;
            int framesToExecute = 0;

            while (_accumulatedTime >= logicInterval && framesToExecute < Config.MaxCatchUpFrames)
            {
                _accumulatedTime -= logicInterval;
                framesToExecute++;
            }

            // 防止累积时间过大
            if (_accumulatedTime > logicInterval * 2)
            {
                _accumulatedTime = logicInterval;
            }

            // 执行逻辑帧
            for (int i = 0; i < framesToExecute; i++)
            {
                ExecuteLogicFrame();
            }
        }

        /// <summary>
        /// 执行单个逻辑帧
        /// </summary>
        private void ExecuteLogicFrame()
        {
            var startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 创建逻辑帧
            var logicFrame = new LogicFrame(CurrentFrame);

            // 获取输入
            var input = InputBuffer.GetInput(CurrentFrame);
            bool inputWasNull = input == null;
            
            if (input == null)
            {
                // 如果没有输入，复用空输入对象避免GC分配
                // 更新帧号并清空玩家输入
                _reusableEmptyInput.FrameNumber = CurrentFrame;
                for (int i = 0; i < _reusableEmptyInput.PlayerCount; i++)
                {
                    var playerInput = _reusableEmptyInput.PlayerInputs[i];
                    if (playerInput != null)
                    {
                        playerInput.Actions.Clear();
                        playerInput.InputFlags = InputType.None;
                    }
                }
                input = _reusableEmptyInput;
                GameLog.Debug(LOG_TAG, "ExecuteLogicFrame",
                    $"帧{CurrentFrame} - 输入缓冲区无输入，使用复用空输入 (玩家数:{Config.PlayerCount})");
            }
            else
            {
                // 统计输入信息
                int totalActions = 0;
                int playersWithActions = 0;
                for (int i = 0; i < input.PlayerCount; i++)
                {
                    var playerInput = input.PlayerInputs[i];
                    if (playerInput != null && playerInput.HasActions)
                    {
                        playersWithActions++;
                        totalActions += playerInput.Actions.Count;
                    }
                }
                GameLog.Debug(LOG_TAG, "ExecuteLogicFrame",
                    $"帧{CurrentFrame} - 从缓冲区获取输入: 有动作的玩家数:{playersWithActions}/{input.PlayerCount}, 总动作数:{totalActions}");
            }
            
            logicFrame.SetInput(input);

            // 标记帧开始
            logicFrame.BeginExecution();
            RaiseEvent(LockstepEventType.FrameStarted, CurrentFrame, null);

            // 保存快照（关键帧）
            if (SnapshotManager.ShouldSaveSnapshot(CurrentFrame))
            {
                SaveSnapshot();
                GameLog.Debug(LOG_TAG, "ExecuteLogicFrame", $"帧{CurrentFrame} - 保存快照");
            }

            // 执行逻辑更新
            try
            {
                GameLog.Debug(LOG_TAG, "ExecuteLogicFrame",
                    $"帧{CurrentFrame} - 开始执行逻辑更新, deltaTime:{LogicDeltaTime.ToFloat():F4}s");
                OnLogicUpdate?.Invoke(CurrentFrame, LogicDeltaTime, input);
                GameLog.Debug(LOG_TAG, "ExecuteLogicFrame", $"帧{CurrentFrame} - 逻辑更新完成");
            }
            catch (Exception ex)
            {
                GameLog.Error(LOG_TAG, "ExecuteLogicFrame",
                    $"帧{CurrentFrame} - 逻辑更新异常: {ex.Message}\n{ex.StackTrace}");
                RaiseEvent(LockstepEventType.SyncError, CurrentFrame, $"Logic update error: {ex.Message}");
            }

            // 标记帧完成
            logicFrame.EndExecution();

            // 记录帧历史
            AddFrameToHistory(logicFrame);

            // 确认输入
            InputBuffer.ConfirmFrame(CurrentFrame);

            // 更新统计
            var endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            LastLogicFrameTime = endTime - startTime;
            UpdateFrameTimeStats(LastLogicFrameTime);

            TotalFramesExecuted++;
            CurrentFrame++;

            RaiseEvent(LockstepEventType.FrameCompleted, CurrentFrame - 1, null);
        }

        #endregion

        #region 输入处理

        /// <summary>
        /// 提交本地玩家输入
        /// </summary>
        public void SubmitLocalInput(PlayerInput input)
        {
            input.PlayerId = LocalPlayerId;
            int targetFrame = CurrentFrame + Config.InputBufferFrames;
            
            // 检查输入是否有效
            if (input.HasActions)
            {
                GameLog.Info(LOG_TAG, "SubmitLocalInput",
                    $"提交本地输入 - 玩家ID:{LocalPlayerId}, 当前帧:{CurrentFrame}, 目标帧:{targetFrame}, 动作数:{input.Actions.Count}, InputBufferFrames:{Config.InputBufferFrames}");
                foreach (var action in input.Actions)
                {
                    GameLog.Debug(LOG_TAG, "SubmitLocalInput",
                        $"  - 动作类型:{action.Type}, 目标位置:({action.TargetPosition.X.ToFloat():F2}, {action.TargetPosition.Y.ToFloat():F2}, {action.TargetPosition.Z.ToFloat():F2}), 技能槽:{action.SkillSlot}");
                }
            }
            
            // 检查目标帧是否有效
            int lastConfirmedFrame = InputBuffer.LastConfirmedFrame;
            if (targetFrame <= lastConfirmedFrame)
            {
                GameLog.Warning(LOG_TAG, "SubmitLocalInput",
                    $"警告: 目标帧{targetFrame} <= 已确认帧{lastConfirmedFrame}, 输入可能被丢弃!");
            }
            
            SubmitInput(targetFrame, input);

            // 客户端预测
            if (Config.EnablePrediction && PredictedEntity != null && OnPredictUpdate != null)
            {
                GameLog.Debug(LOG_TAG, "SubmitLocalInput", $"执行客户端预测 - 实体:{PredictedEntity.Name}");
                var predictInput = new FrameInput(CurrentFrame, Config.PlayerCount);
                predictInput.SetPlayerInput(LocalPlayerId, input);
                OnPredictUpdate(PredictedEntity, predictInput);
            }
        }

        /// <summary>
        /// 提交输入到指定帧
        /// </summary>
        public void SubmitInput(int frameNumber, PlayerInput input)
        {
            GameLog.Debug(LOG_TAG, "SubmitInput",
                $"提交输入到帧{frameNumber} - 玩家ID:{input.PlayerId}, 有动作:{input.HasActions}, 动作数:{input.Actions?.Count ?? 0}");
            
            // 检查帧号有效性
            if (frameNumber < CurrentFrame)
            {
                GameLog.Warning(LOG_TAG, "SubmitInput",
                    $"警告: 目标帧{frameNumber} < 当前帧{CurrentFrame}, 输入将被忽略!");
            }
            
            InputBuffer.AddPlayerInput(frameNumber, input.PlayerId, input);
            RaiseEvent(LockstepEventType.InputReceived, frameNumber, $"Player {input.PlayerId} input");
        }

        /// <summary>
        /// 提交完整帧输入
        /// </summary>
        public void SubmitFrameInput(FrameInput frameInput)
        {
            GameLog.Debug(LOG_TAG, "SubmitFrameInput",
                $"提交完整帧输入 - 帧号:{frameInput.FrameNumber}, 玩家数:{frameInput.PlayerCount}");
            InputBuffer.AddInput(frameInput);
            RaiseEvent(LockstepEventType.InputReceived, frameInput.FrameNumber, "Frame input received");
        }

        #endregion

        #region 快照与回滚

        /// <summary>
        /// 保存当前状态快照
        /// </summary>
        private void SaveSnapshot()
        {
            if (OnCreateSnapshot == null) return;

            var snapshot = OnCreateSnapshot(CurrentFrame);
            if (snapshot != null)
            {
                SnapshotManager.SaveSnapshot(snapshot);
                RaiseEvent(LockstepEventType.SnapshotSaved, CurrentFrame, null);
            }
        }

        /// <summary>
        /// 回滚到指定帧
        /// </summary>
        public bool Rollback(int targetFrame)
        {
            if (targetFrame >= CurrentFrame)
            {
                return false;
            }

            // 找到最近的快照
            var snapshot = SnapshotManager.GetNearestSnapshot(targetFrame);
            if (snapshot == null)
            {
                RaiseEvent(LockstepEventType.SyncError, targetFrame, "No snapshot available for rollback");
                return false;
            }

            RaiseEvent(LockstepEventType.RollbackStarted, targetFrame, $"Rolling back from {CurrentFrame} to {targetFrame}");

            // 恢复快照
            if (OnRestoreSnapshot != null)
            {
                OnRestoreSnapshot(snapshot);
            }

            // 删除回滚帧之后的快照
            SnapshotManager.RemoveSnapshotsAfter(targetFrame);

            // 重置输入缓冲
            InputBuffer.ResetToFrame(targetFrame);

            // 从快照帧重新执行到目标帧
            int snapshotFrame = snapshot.FrameNumber;
            CurrentFrame = snapshotFrame;

            // 重新执行帧
            while (CurrentFrame < targetFrame)
            {
                ExecuteLogicFrame();
            }

            RollbackCount++;
            RaiseEvent(LockstepEventType.RollbackCompleted, CurrentFrame, null);

            return true;
        }

        #endregion

        #region 帧历史

        /// <summary>
        /// 添加帧到历史记录
        /// </summary>
        private void AddFrameToHistory(LogicFrame frame)
        {
            _frameHistory.Add(frame);

            // 清理过旧的历史
            while (_frameHistory.Count > MaxFrameHistory)
            {
                _frameHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// 获取帧历史
        /// </summary>
        public LogicFrame GetFrameHistory(int frameNumber)
        {
            foreach (var frame in _frameHistory)
            {
                if (frame.FrameNumber == frameNumber)
                {
                    return frame;
                }
            }
            return null;
        }

        #endregion

        #region 统计与调试

        /// <summary>
        /// 更新帧时间统计
        /// </summary>
        private void UpdateFrameTimeStats(float frameTime)
        {
            _frameTimeHistory.Enqueue(frameTime);
            while (_frameTimeHistory.Count > FrameTimeHistorySize)
            {
                _frameTimeHistory.Dequeue();
            }

            // 计算平均值
            float sum = 0f;
            foreach (var time in _frameTimeHistory)
            {
                sum += time;
            }
            AverageLogicFrameTime = sum / _frameTimeHistory.Count;
        }

        /// <summary>
        /// 获取插值进度（用于表现层平滑）
        /// </summary>
        public float GetInterpolationAlpha()
        {
            float logicInterval = Config.LogicFrameIntervalSeconds;
            return _accumulatedTime / logicInterval;
        }

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Lockstep: Frame={CurrentFrame}, Running={IsRunning}, " +
                   $"LogicFPS={Config.LogicFrameRate}, AvgTime={AverageLogicFrameTime:F2}ms, " +
                   $"Rollbacks={RollbackCount}\n" +
                   InputBuffer.GetDebugInfo() + "\n" +
                   SnapshotManager.GetDebugInfo();
        }

        #endregion

        #region 事件

        /// <summary>
        /// 触发事件
        /// </summary>
        private void RaiseEvent(LockstepEventType type, int frameNumber, string message)
        {
            OnLockstepEvent?.Invoke(new LockstepEvent
            {
                Type = type,
                FrameNumber = frameNumber,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Message = message
            });
        }

        #endregion
    }
}
