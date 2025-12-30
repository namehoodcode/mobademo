// DelaySimulator.cs - 网络延迟模拟器
// 用于模拟网络延迟，测试帧同步在高延迟环境下的表现
// 无Unity依赖，保证确定性

using System;
using System.Collections.Generic;

namespace MobaCombatCore.Core.Network
{
    /// <summary>
    /// 延迟数据包
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class DelayedPacket<T>
    {
        /// <summary>
        /// 数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 发送时间戳（毫秒）
        /// </summary>
        public long SendTimestamp { get; set; }

        /// <summary>
        /// 到达时间戳（毫秒）
        /// </summary>
        public long ArrivalTimestamp { get; set; }

        /// <summary>
        /// 发送者ID
        /// </summary>
        public int SenderId { get; set; }

        /// <summary>
        /// 接收者ID（-1表示广播）
        /// </summary>
        public int ReceiverId { get; set; }
    }

    /// <summary>
    /// 延迟配置
    /// </summary>
    public class DelayConfig
    {
        /// <summary>
        /// 基础延迟（毫秒）
        /// </summary>
        public int BaseDelay { get; set; } = 50;

        /// <summary>
        /// 延迟抖动（毫秒）
        /// </summary>
        public int Jitter { get; set; } = 10;

        /// <summary>
        /// 丢包率（0-1）
        /// </summary>
        public float PacketLossRate { get; set; } = 0f;

        /// <summary>
        /// 是否启用延迟模拟
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 无延迟配置
        /// </summary>
        public static DelayConfig NoDelay => new DelayConfig
        {
            BaseDelay = 0,
            Jitter = 0,
            PacketLossRate = 0f,
            Enabled = false
        };

        /// <summary>
        /// 低延迟配置（50ms）
        /// </summary>
        public static DelayConfig LowLatency => new DelayConfig
        {
            BaseDelay = 50,
            Jitter = 10,
            PacketLossRate = 0f
        };

        /// <summary>
        /// 中等延迟配置（100ms）
        /// </summary>
        public static DelayConfig MediumLatency => new DelayConfig
        {
            BaseDelay = 100,
            Jitter = 20,
            PacketLossRate = 0.01f
        };

        /// <summary>
        /// 高延迟配置（200ms）
        /// </summary>
        public static DelayConfig HighLatency => new DelayConfig
        {
            BaseDelay = 200,
            Jitter = 50,
            PacketLossRate = 0.02f
        };

        /// <summary>
        /// 极端延迟配置（500ms）
        /// </summary>
        public static DelayConfig ExtremeLatency => new DelayConfig
        {
            BaseDelay = 500,
            Jitter = 100,
            PacketLossRate = 0.05f
        };
    }

    /// <summary>
    /// 网络延迟模拟器
    /// </summary>
    /// <typeparam name="T">数据包类型</typeparam>
    public class DelaySimulator<T>
    {
        /// <summary>
        /// 延迟配置
        /// </summary>
        public DelayConfig Config { get; set; }

        /// <summary>
        /// 延迟队列
        /// </summary>
        private readonly List<DelayedPacket<T>> _delayQueue;

        /// <summary>
        /// 随机数生成器
        /// </summary>
        private readonly Random _random;

        /// <summary>
        /// 统计：发送的数据包数量
        /// </summary>
        public int PacketsSent { get; private set; }

        /// <summary>
        /// 统计：接收的数据包数量
        /// </summary>
        public int PacketsReceived { get; private set; }

        /// <summary>
        /// 统计：丢失的数据包数量
        /// </summary>
        public int PacketsLost { get; private set; }

        /// <summary>
        /// 当前队列中的数据包数量
        /// </summary>
        public int QueuedPackets => _delayQueue.Count;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DelaySimulator(DelayConfig config = null, int? seed = null)
        {
            Config = config ?? DelayConfig.LowLatency;
            _delayQueue = new List<DelayedPacket<T>>();
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            PacketsSent = 0;
            PacketsReceived = 0;
            PacketsLost = 0;
        }

        /// <summary>
        /// 发送数据包（加入延迟队列）
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="senderId">发送者ID</param>
        /// <param name="receiverId">接收者ID（-1表示广播）</param>
        /// <returns>是否成功加入队列（可能因丢包而失败）</returns>
        public bool Send(T data, int senderId = 0, int receiverId = -1)
        {
            PacketsSent++;

            // 如果未启用延迟模拟，直接返回
            if (!Config.Enabled)
            {
                return true;
            }

            // 模拟丢包
            if (Config.PacketLossRate > 0 && _random.NextDouble() < Config.PacketLossRate)
            {
                PacketsLost++;
                return false;
            }

            // 计算延迟
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int delay = CalculateDelay();

            var packet = new DelayedPacket<T>
            {
                Data = data,
                SendTimestamp = currentTime,
                ArrivalTimestamp = currentTime + delay,
                SenderId = senderId,
                ReceiverId = receiverId
            };

            _delayQueue.Add(packet);
            return true;
        }

        /// <summary>
        /// 接收已到达的数据包
        /// </summary>
        /// <param name="receiverId">接收者ID（-1表示接收所有）</param>
        /// <returns>已到达的数据包列表</returns>
        public List<T> Receive(int receiverId = -1)
        {
            var result = new List<T>();
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 找出所有已到达的数据包
            var arrivedPackets = new List<DelayedPacket<T>>();
            foreach (var packet in _delayQueue)
            {
                if (packet.ArrivalTimestamp <= currentTime)
                {
                    // 检查接收者
                    if (receiverId == -1 || packet.ReceiverId == -1 || packet.ReceiverId == receiverId)
                    {
                        arrivedPackets.Add(packet);
                    }
                }
            }

            // 移除已到达的数据包并返回数据
            foreach (var packet in arrivedPackets)
            {
                _delayQueue.Remove(packet);
                result.Add(packet.Data);
                PacketsReceived++;
            }

            return result;
        }

        /// <summary>
        /// 接收单个已到达的数据包
        /// </summary>
        /// <param name="data">输出数据</param>
        /// <param name="receiverId">接收者ID</param>
        /// <returns>是否有数据包到达</returns>
        public bool TryReceive(out T data, int receiverId = -1)
        {
            data = default;
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            for (int i = 0; i < _delayQueue.Count; i++)
            {
                var packet = _delayQueue[i];
                if (packet.ArrivalTimestamp <= currentTime)
                {
                    if (receiverId == -1 || packet.ReceiverId == -1 || packet.ReceiverId == receiverId)
                    {
                        data = packet.Data;
                        _delayQueue.RemoveAt(i);
                        PacketsReceived++;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 计算延迟（包含抖动）
        /// </summary>
        private int CalculateDelay()
        {
            int delay = Config.BaseDelay;

            if (Config.Jitter > 0)
            {
                // 添加随机抖动
                int jitter = _random.Next(-Config.Jitter, Config.Jitter + 1);
                delay += jitter;
            }

            return System.Math.Max(0, delay);
        }

        /// <summary>
        /// 清空延迟队列
        /// </summary>
        public void Clear()
        {
            _delayQueue.Clear();
        }

        /// <summary>
        /// 重置统计
        /// </summary>
        public void ResetStats()
        {
            PacketsSent = 0;
            PacketsReceived = 0;
            PacketsLost = 0;
        }

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            float lossRate = PacketsSent > 0 ? (float)PacketsLost / PacketsSent * 100 : 0;
            return $"DelaySimulator: Delay={Config.BaseDelay}±{Config.Jitter}ms, " +
                   $"Queued={QueuedPackets}, Sent={PacketsSent}, Received={PacketsReceived}, " +
                   $"Lost={PacketsLost} ({lossRate:F1}%)";
        }
    }

    /// <summary>
    /// 输入延迟模拟器（专门用于帧输入）
    /// </summary>
    public class InputDelaySimulator : DelaySimulator<Lockstep.FrameInput>
    {
        public InputDelaySimulator(DelayConfig config = null, int? seed = null)
            : base(config, seed)
        {
        }
    }

    /// <summary>
    /// 延迟统计信息
    /// </summary>
    public class DelayStatistics
    {
        /// <summary>
        /// 当前配置的延迟（毫秒）
        /// </summary>
        public int ConfiguredDelay { get; set; }

        /// <summary>
        /// 实际平均延迟（毫秒）
        /// </summary>
        public float AverageDelay { get; set; }

        /// <summary>
        /// 最小延迟（毫秒）
        /// </summary>
        public int MinDelay { get; set; }

        /// <summary>
        /// 最大延迟（毫秒）
        /// </summary>
        public int MaxDelay { get; set; }

        /// <summary>
        /// 抖动（毫秒）
        /// </summary>
        public int Jitter { get; set; }

        /// <summary>
        /// 丢包率
        /// </summary>
        public float PacketLossRate { get; set; }

        /// <summary>
        /// 队列中的数据包数量
        /// </summary>
        public int QueuedPackets { get; set; }

        /// <summary>
        /// 总发送数据包
        /// </summary>
        public int TotalSent { get; set; }

        /// <summary>
        /// 总接收数据包
        /// </summary>
        public int TotalReceived { get; set; }

        /// <summary>
        /// 总丢失数据包
        /// </summary>
        public int TotalLost { get; set; }
    }

    /// <summary>
    /// 网络状态监控器 - 用于Debug面板显示
    /// </summary>
    public class NetworkMonitor
    {
        private readonly List<long> _recentDelays = new List<long>();
        private const int MaxSamples = 100;

        /// <summary>
        /// 记录一次延迟
        /// </summary>
        public void RecordDelay(long delayMs)
        {
            _recentDelays.Add(delayMs);
            while (_recentDelays.Count > MaxSamples)
            {
                _recentDelays.RemoveAt(0);
            }
        }

        /// <summary>
        /// 获取平均延迟
        /// </summary>
        public float GetAverageDelay()
        {
            if (_recentDelays.Count == 0) return 0;
            long sum = 0;
            foreach (var delay in _recentDelays)
            {
                sum += delay;
            }
            return (float)sum / _recentDelays.Count;
        }

        /// <summary>
        /// 获取最小延迟
        /// </summary>
        public long GetMinDelay()
        {
            if (_recentDelays.Count == 0) return 0;
            long min = long.MaxValue;
            foreach (var delay in _recentDelays)
            {
                if (delay < min) min = delay;
            }
            return min;
        }

        /// <summary>
        /// 获取最大延迟
        /// </summary>
        public long GetMaxDelay()
        {
            if (_recentDelays.Count == 0) return 0;
            long max = 0;
            foreach (var delay in _recentDelays)
            {
                if (delay > max) max = delay;
            }
            return max;
        }

        /// <summary>
        /// 清空记录
        /// </summary>
        public void Clear()
        {
            _recentDelays.Clear();
        }
    }
}
