// LocalServer.cs - 本地服务器模拟
// 模拟帧同步服务器行为，用于单机测试
// 无Unity依赖，保证确定性

using System;
using System.Collections.Generic;
using MobaCombatCore.Core.Lockstep;

namespace MobaCombatCore.Core.Network
{
    /// <summary>
    /// 服务器状态
    /// </summary>
    public enum ServerState
    {
        Stopped,
        WaitingForPlayers,
        Running,
        Paused
    }

    /// <summary>
    /// 玩家连接状态
    /// </summary>
    public class PlayerConnection
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// 是否已准备
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// 最后收到输入的帧号
        /// </summary>
        public int LastInputFrame { get; set; }

        /// <summary>
        /// 延迟（毫秒）
        /// </summary>
        public int Latency { get; set; }

        /// <summary>
        /// 丢包数
        /// </summary>
        public int PacketsLost { get; set; }
    }

    /// <summary>
    /// 本地服务器模拟 - 模拟帧同步服务器行为
    /// </summary>
    public class LocalServer
    {
        #region 配置与状态

        /// <summary>
        /// 服务器状态
        /// </summary>
        public ServerState State { get; private set; }

        /// <summary>
        /// 当前服务器帧号
        /// </summary>
        public int CurrentFrame { get; private set; }

        /// <summary>
        /// 玩家数量
        /// </summary>
        public int PlayerCount { get; private set; }

        /// <summary>
        /// 玩家连接信息
        /// </summary>
        private readonly Dictionary<int, PlayerConnection> _players;

        /// <summary>
        /// 帧输入缓冲（按帧号存储）
        /// </summary>
        private readonly Dictionary<int, FrameInput> _frameInputs;

        /// <summary>
        /// 延迟模拟器（服务器到客户端）
        /// </summary>
        private readonly DelaySimulator<FrameInput> _outputDelaySimulator;

        /// <summary>
        /// 延迟配置
        /// </summary>
        public DelayConfig DelayConfig { get; set; }

        /// <summary>
        /// 逻辑帧率
        /// </summary>
        public int LogicFrameRate { get; set; } = 30;

        /// <summary>
        /// 输入等待超时（帧数）
        /// </summary>
        public int InputWaitTimeout { get; set; } = 10;

        #endregion

        #region 事件

        /// <summary>
        /// 帧输入广播事件
        /// </summary>
        public event Action<FrameInput> OnFrameInputBroadcast;

        /// <summary>
        /// 游戏开始事件
        /// </summary>
        public event Action OnGameStarted;

        /// <summary>
        /// 游戏结束事件
        /// </summary>
        public event Action OnGameEnded;

        /// <summary>
        /// 玩家连接事件
        /// </summary>
        public event Action<int> OnPlayerConnected;

        /// <summary>
        /// 玩家断开事件
        /// </summary>
        public event Action<int> OnPlayerDisconnected;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public LocalServer(int playerCount, DelayConfig delayConfig = null)
        {
            PlayerCount = playerCount;
            DelayConfig = delayConfig ?? DelayConfig.NoDelay;
            State = ServerState.Stopped;
            CurrentFrame = 0;

            _players = new Dictionary<int, PlayerConnection>();
            _frameInputs = new Dictionary<int, FrameInput>();
            _outputDelaySimulator = new DelaySimulator<FrameInput>(DelayConfig);

            // 初始化玩家连接
            for (int i = 0; i < playerCount; i++)
            {
                _players[i] = new PlayerConnection
                {
                    PlayerId = i,
                    IsConnected = false,
                    IsReady = false,
                    LastInputFrame = -1,
                    Latency = 0,
                    PacketsLost = 0
                };
            }
        }

        #region 服务器生命周期

        /// <summary>
        /// 启动服务器
        /// </summary>
        public void Start()
        {
            if (State != ServerState.Stopped) return;

            State = ServerState.WaitingForPlayers;
            CurrentFrame = 0;
            _frameInputs.Clear();
            _outputDelaySimulator.Clear();
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void Stop()
        {
            State = ServerState.Stopped;
            OnGameEnded?.Invoke();
        }

        /// <summary>
        /// 暂停服务器
        /// </summary>
        public void Pause()
        {
            if (State == ServerState.Running)
            {
                State = ServerState.Paused;
            }
        }

        /// <summary>
        /// 恢复服务器
        /// </summary>
        public void Resume()
        {
            if (State == ServerState.Paused)
            {
                State = ServerState.Running;
            }
        }

        #endregion

        #region 玩家管理

        /// <summary>
        /// 玩家连接
        /// </summary>
        public bool ConnectPlayer(int playerId)
        {
            if (!_players.TryGetValue(playerId, out var player))
            {
                return false;
            }

            if (player.IsConnected)
            {
                return true;
            }

            player.IsConnected = true;
            OnPlayerConnected?.Invoke(playerId);

            // 检查是否所有玩家都已连接
            CheckAllPlayersReady();

            return true;
        }

        /// <summary>
        /// 玩家断开
        /// </summary>
        public void DisconnectPlayer(int playerId)
        {
            if (_players.TryGetValue(playerId, out var player))
            {
                player.IsConnected = false;
                player.IsReady = false;
                OnPlayerDisconnected?.Invoke(playerId);
            }
        }

        /// <summary>
        /// 玩家准备
        /// </summary>
        public void SetPlayerReady(int playerId, bool ready)
        {
            if (_players.TryGetValue(playerId, out var player))
            {
                player.IsReady = ready;
                CheckAllPlayersReady();
            }
        }

        /// <summary>
        /// 检查是否所有玩家都已准备
        /// </summary>
        private void CheckAllPlayersReady()
        {
            if (State != ServerState.WaitingForPlayers) return;

            bool allReady = true;
            foreach (var player in _players.Values)
            {
                if (!player.IsConnected || !player.IsReady)
                {
                    allReady = false;
                    break;
                }
            }

            if (allReady)
            {
                StartGame();
            }
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        private void StartGame()
        {
            State = ServerState.Running;
            CurrentFrame = 0;
            OnGameStarted?.Invoke();
        }

        /// <summary>
        /// 获取玩家连接信息
        /// </summary>
        public PlayerConnection GetPlayerConnection(int playerId)
        {
            return _players.TryGetValue(playerId, out var player) ? player : null;
        }

        /// <summary>
        /// 获取所有已连接的玩家数量
        /// </summary>
        public int GetConnectedPlayerCount()
        {
            int count = 0;
            foreach (var player in _players.Values)
            {
                if (player.IsConnected) count++;
            }
            return count;
        }

        #endregion

        #region 输入处理

        /// <summary>
        /// 接收玩家输入
        /// </summary>
        public void ReceivePlayerInput(int playerId, int frameNumber, PlayerInput input)
        {
            if (State != ServerState.Running) return;

            if (!_players.TryGetValue(playerId, out var player))
            {
                return;
            }

            // 更新玩家最后输入帧
            player.LastInputFrame = frameNumber;

            // 获取或创建帧输入
            if (!_frameInputs.TryGetValue(frameNumber, out var frameInput))
            {
                frameInput = new FrameInput(frameNumber, PlayerCount);
                _frameInputs[frameNumber] = frameInput;
            }

            // 设置玩家输入
            frameInput.SetPlayerInput(playerId, input);

            // 检查帧输入是否完整
            if (IsFrameInputComplete(frameNumber))
            {
                BroadcastFrameInput(frameNumber);
            }
        }

        /// <summary>
        /// 检查帧输入是否完整
        /// </summary>
        private bool IsFrameInputComplete(int frameNumber)
        {
            if (!_frameInputs.TryGetValue(frameNumber, out var frameInput))
            {
                return false;
            }

            // 检查所有已连接玩家是否都有输入
            foreach (var player in _players.Values)
            {
                if (player.IsConnected)
                {
                    var playerInput = frameInput.GetPlayerInput(player.PlayerId);
                    // 简单检查：如果玩家输入为空，认为未完成
                    // 实际项目中可能需要更复杂的检查
                }
            }

            return true; // 本地模拟中，简化处理
        }

        /// <summary>
        /// 广播帧输入
        /// </summary>
        private void BroadcastFrameInput(int frameNumber)
        {
            if (!_frameInputs.TryGetValue(frameNumber, out var frameInput))
            {
                return;
            }

            // 通过延迟模拟器发送
            if (DelayConfig.Enabled)
            {
                _outputDelaySimulator.Send(frameInput.Clone());
            }
            else
            {
                // 直接广播
                OnFrameInputBroadcast?.Invoke(frameInput);
            }
        }

        #endregion

        #region 服务器更新

        /// <summary>
        /// 服务器更新（每帧调用）
        /// </summary>
        public void Update()
        {
            if (State != ServerState.Running) return;

            // 处理延迟队列中的数据包
            if (DelayConfig.Enabled)
            {
                var arrivedInputs = _outputDelaySimulator.Receive();
                foreach (var input in arrivedInputs)
                {
                    OnFrameInputBroadcast?.Invoke(input);
                }
            }

            // 检查输入超时
            CheckInputTimeout();
        }

        /// <summary>
        /// 检查输入超时
        /// </summary>
        private void CheckInputTimeout()
        {
            // 如果某个玩家的输入落后太多，可以采取措施
            // 例如：使用上一帧的输入，或者暂停等待
            foreach (var player in _players.Values)
            {
                if (player.IsConnected)
                {
                    int frameDiff = CurrentFrame - player.LastInputFrame;
                    if (frameDiff > InputWaitTimeout)
                    {
                        // 玩家输入超时，可以记录或处理
                        player.PacketsLost++;
                    }
                }
            }
        }

        /// <summary>
        /// 推进服务器帧
        /// </summary>
        public void AdvanceFrame()
        {
            if (State != ServerState.Running) return;

            CurrentFrame++;

            // 清理旧的帧输入
            CleanupOldFrameInputs();
        }

        /// <summary>
        /// 清理旧的帧输入
        /// </summary>
        private void CleanupOldFrameInputs()
        {
            var framesToRemove = new List<int>();
            foreach (var kvp in _frameInputs)
            {
                if (kvp.Key < CurrentFrame - 100) // 保留最近100帧
                {
                    framesToRemove.Add(kvp.Key);
                }
            }

            foreach (var frame in framesToRemove)
            {
                _frameInputs.Remove(frame);
            }
        }

        #endregion

        #region 调试

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            string playerInfo = "";
            foreach (var player in _players.Values)
            {
                playerInfo += $"\n  Player{player.PlayerId}: Connected={player.IsConnected}, " +
                              $"Ready={player.IsReady}, LastInput={player.LastInputFrame}, " +
                              $"Lost={player.PacketsLost}";
            }

            return $"LocalServer: State={State}, Frame={CurrentFrame}, " +
                   $"Players={GetConnectedPlayerCount()}/{PlayerCount}" +
                   playerInfo + "\n" +
                   _outputDelaySimulator.GetDebugInfo();
        }

        #endregion
    }
}
