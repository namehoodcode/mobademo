// InputBuffer.cs - 输入缓冲队列
// 用于帧同步系统，管理输入的缓冲和延迟模拟
// 无Unity依赖，保证确定性
// 优化：预分配List，避免每帧GC分配

using System;
using System.Collections.Generic;
using MobaCombatCore.Glue.Services;

namespace MobaCombatCore.Core.Lockstep
{
    /// <summary>
    /// 输入缓冲队列 - 管理帧输入的缓冲和调度
    /// 优化版本：预分配List避免GC
    /// </summary>
    public class InputBuffer
    {
        private const string LOG_TAG = "InputBuffer";

        /// <summary>
        /// 输入队列（按帧号排序）
        /// </summary>
        private readonly SortedDictionary<int, FrameInput> _inputQueue;

        /// <summary>
        /// 最大缓冲帧数
        /// </summary>
        private readonly int _maxBufferSize;

        /// <summary>
        /// 已确认的最后帧号
        /// </summary>
        private int _lastConfirmedFrame;

        /// <summary>
        /// 玩家数量
        /// </summary>
        private readonly int _playerCount;
        
        /// <summary>
        /// 预分配的帧号列表，用于删除操作，避免每帧分配
        /// </summary>
        private readonly List<int> _framesToRemoveCache;

        /// <summary>
        /// 当前缓冲的帧数
        /// </summary>
        public int BufferedFrameCount => _inputQueue.Count;

        /// <summary>
        /// 最后确认的帧号
        /// </summary>
        public int LastConfirmedFrame => _lastConfirmedFrame;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="playerCount">玩家数量</param>
        /// <param name="maxBufferSize">最大缓冲帧数</param>
        public InputBuffer(int playerCount, int maxBufferSize = 60)
        {
            _playerCount = playerCount;
            _maxBufferSize = maxBufferSize;
            _inputQueue = new SortedDictionary<int, FrameInput>();
            _lastConfirmedFrame = -1;
            // 预分配容量，避免运行时扩容
            _framesToRemoveCache = new List<int>(maxBufferSize);
        }

        /// <summary>
        /// 添加帧输入到缓冲区
        /// </summary>
        public void AddInput(FrameInput input)
        {
            if (input == null)
            {
                GameLog.Warning(LOG_TAG, "AddInput", "输入为空，忽略");
                return;
            }

            // 忽略过旧的帧
            if (input.FrameNumber <= _lastConfirmedFrame)
            {
                GameLog.Warning(LOG_TAG, "AddInput",
                    $"帧{input.FrameNumber} <= 已确认帧{_lastConfirmedFrame}，输入被丢弃!");
                return;
            }

            // 如果已存在，合并输入
            if (_inputQueue.TryGetValue(input.FrameNumber, out var existing))
            {
                GameLog.Debug(LOG_TAG, "AddInput", $"帧{input.FrameNumber} 已存在，合并输入");
                MergeInputs(existing, input);
            }
            else
            {
                GameLog.Debug(LOG_TAG, "AddInput",
                    $"添加新帧输入 - 帧号:{input.FrameNumber}, 玩家数:{input.PlayerCount}, 缓冲区大小:{_inputQueue.Count + 1}");
                _inputQueue[input.FrameNumber] = input.Clone();
            }

            // 清理过多的缓冲
            CleanupOldFrames();
        }

        /// <summary>
        /// 添加单个玩家的输入
        /// </summary>
        public void AddPlayerInput(int frameNumber, int playerId, PlayerInput playerInput)
        {
            if (frameNumber <= _lastConfirmedFrame)
            {
                GameLog.Warning(LOG_TAG, "AddPlayerInput",
                    $"帧{frameNumber} <= 已确认帧{_lastConfirmedFrame}，玩家{playerId}的输入被丢弃! 动作数:{playerInput?.Actions?.Count ?? 0}");
                return;
            }

            bool isNewFrame = false;
            if (!_inputQueue.TryGetValue(frameNumber, out var frameInput))
            {
                frameInput = new FrameInput(frameNumber, _playerCount);
                _inputQueue[frameNumber] = frameInput;
                isNewFrame = true;
            }

            // 统计输入信息
            int actionCount = playerInput?.Actions?.Count ?? 0;
            bool hasActions = playerInput?.HasActions ?? false;
            
            GameLog.Debug(LOG_TAG, "AddPlayerInput",
                $"{(isNewFrame ? "创建新帧并" : "")}添加玩家输入 - 帧号:{frameNumber}, 玩家ID:{playerId}, 有动作:{hasActions}, 动作数:{actionCount}, 缓冲区大小:{_inputQueue.Count}");
            
            if (hasActions && playerInput != null)
            {
                foreach (var action in playerInput.Actions)
                {
                    GameLog.Debug(LOG_TAG, "AddPlayerInput",
                        $"  - 动作: 类型={action.Type}, 目标位置=({action.TargetPosition.X.ToFloat():F2}, {action.TargetPosition.Y.ToFloat():F2}, {action.TargetPosition.Z.ToFloat():F2})");
                }
            }

            frameInput.SetPlayerInput(playerId, playerInput);
        }

        /// <summary>
        /// 获取指定帧的输入（如果存在）
        /// </summary>
        public FrameInput GetInput(int frameNumber)
        {
            if (_inputQueue.TryGetValue(frameNumber, out var input))
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
                GameLog.Debug(LOG_TAG, "GetInput",
                    $"获取帧{frameNumber}的输入 - 有动作的玩家:{playersWithActions}/{input.PlayerCount}, 总动作数:{totalActions}");
                return input;
            }
            
            GameLog.Debug(LOG_TAG, "GetInput",
                $"帧{frameNumber}无输入 - 缓冲区范围:[{GetEarliestBufferedFrame()}-{GetLatestBufferedFrame()}], 已确认帧:{_lastConfirmedFrame}");
            return null;
        }

        /// <summary>
        /// 尝试获取下一帧的输入
        /// </summary>
        public bool TryGetNextInput(out FrameInput input)
        {
            int nextFrame = _lastConfirmedFrame + 1;
            if (_inputQueue.TryGetValue(nextFrame, out input))
            {
                return true;
            }
            input = null;
            return false;
        }

        /// <summary>
        /// 确认帧已处理
        /// 优化：使用预分配的List避免每帧GC分配
        /// </summary>
        public void ConfirmFrame(int frameNumber)
        {
            int previousConfirmed = _lastConfirmedFrame;
            if (frameNumber > _lastConfirmedFrame)
            {
                _lastConfirmedFrame = frameNumber;
            }

            // 使用预分配的List，避免每帧创建新List
            _framesToRemoveCache.Clear();
            
            // 使用迭代器遍历，收集需要删除的帧
            foreach (var kvp in _inputQueue)
            {
                if (kvp.Key <= _lastConfirmedFrame)
                {
                    _framesToRemoveCache.Add(kvp.Key);
                }
            }

            if (_framesToRemoveCache.Count > 0)
            {
                GameLog.Debug(LOG_TAG, "ConfirmFrame",
                    $"确认帧{frameNumber} (之前:{previousConfirmed}) - 移除{_framesToRemoveCache.Count}个旧帧, 剩余缓冲:{_inputQueue.Count - _framesToRemoveCache.Count}");
            }

            // 使用for循环避免foreach迭代器分配
            for (int i = 0; i < _framesToRemoveCache.Count; i++)
            {
                _inputQueue.Remove(_framesToRemoveCache[i]);
            }
        }

        /// <summary>
        /// 检查指定帧的输入是否完整
        /// </summary>
        public bool IsFrameComplete(int frameNumber)
        {
            if (_inputQueue.TryGetValue(frameNumber, out var input))
            {
                return input.IsComplete();
            }
            return false;
        }

        /// <summary>
        /// 获取缓冲区中最早的帧号
        /// </summary>
        public int GetEarliestBufferedFrame()
        {
            foreach (var kvp in _inputQueue)
            {
                return kvp.Key;
            }
            return _lastConfirmedFrame + 1;
        }

        /// <summary>
        /// 获取缓冲区中最新的帧号
        /// </summary>
        public int GetLatestBufferedFrame()
        {
            int latest = _lastConfirmedFrame;
            foreach (var kvp in _inputQueue)
            {
                if (kvp.Key > latest)
                {
                    latest = kvp.Key;
                }
            }
            return latest;
        }

        /// <summary>
        /// 清空缓冲区
        /// </summary>
        public void Clear()
        {
            GameLog.Debug(LOG_TAG, "Clear",
                $"清空缓冲区 - 之前有{_inputQueue.Count}帧, 已确认帧:{_lastConfirmedFrame}");
            _inputQueue.Clear();
            _lastConfirmedFrame = -1;
        }

        /// <summary>
        /// 重置到指定帧
        /// 优化：使用预分配的List避免GC分配
        /// </summary>
        public void ResetToFrame(int frameNumber)
        {
            int previousConfirmed = _lastConfirmedFrame;
            _lastConfirmedFrame = frameNumber - 1;

            // 使用预分配的List
            _framesToRemoveCache.Clear();
            foreach (var kvp in _inputQueue)
            {
                if (kvp.Key < frameNumber)
                {
                    _framesToRemoveCache.Add(kvp.Key);
                }
            }

            GameLog.Debug(LOG_TAG, "ResetToFrame",
                $"重置到帧{frameNumber} - 之前已确认:{previousConfirmed}, 移除{_framesToRemoveCache.Count}帧");

            // 使用for循环避免foreach迭代器分配
            for (int i = 0; i < _framesToRemoveCache.Count; i++)
            {
                _inputQueue.Remove(_framesToRemoveCache[i]);
            }
        }

        /// <summary>
        /// 合并两个帧输入
        /// </summary>
        private void MergeInputs(FrameInput existing, FrameInput newInput)
        {
            int mergedCount = 0;
            for (int i = 0; i < existing.PlayerCount && i < newInput.PlayerCount; i++)
            {
                var newPlayer = newInput.PlayerInputs[i];

                // 如果新输入有内容，使用SetPlayerInput进行合并
                if (newPlayer != null && (newPlayer.InputFlags != InputType.None || newPlayer.HasActions))
                {
                    GameLog.Debug(LOG_TAG, "MergeInputs",
                        $"帧{existing.FrameNumber} - 合并玩家{i}的输入, 新动作数:{newPlayer.Actions?.Count ?? 0}");
                    existing.SetPlayerInput(i, newPlayer);
                    mergedCount++;
                }
            }
            
            if (mergedCount > 0)
            {
                GameLog.Debug(LOG_TAG, "MergeInputs",
                    $"帧{existing.FrameNumber} - 合并完成, 更新了{mergedCount}个玩家的输入");
            }
        }

        /// <summary>
        /// 清理过旧的帧
        /// </summary>
        private void CleanupOldFrames()
        {
            while (_inputQueue.Count > _maxBufferSize)
            {
                int oldestFrame = GetEarliestBufferedFrame();
                if (oldestFrame <= _lastConfirmedFrame)
                {
                    _inputQueue.Remove(oldestFrame);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            return $"InputBuffer: Buffered={BufferedFrameCount}, LastConfirmed={_lastConfirmedFrame}, " +
                   $"Range=[{GetEarliestBufferedFrame()}-{GetLatestBufferedFrame()}]";
        }
    }
}