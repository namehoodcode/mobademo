// FrameInput.cs - 帧输入数据结构
// 定义了单帧内所有玩家的操作
// 无Unity依赖
// 优化：预分配List容量，避免运行时扩容GC

using System.Collections.Generic;
using MobaCombatCore.Core.Math;

namespace MobaCombatCore.Core.Lockstep
{
    /// <summary>
    /// 输入类型标志（用于快速判断输入是否有效）
    /// </summary>
    [System.Flags]
    public enum InputType
    {
        None = 0,
        Movement = 1 << 0,
        Action = 1 << 1,
        Skill = 1 << 2,
        Attack = 1 << 3,
    }

    /// <summary>
    /// 玩家行为类型
    /// </summary>
    public enum ActionType
    {
        None = 0,
        Move = 1,
        Stop = 2,
        Skill = 3,
        Attack = 4,
    }

    /// <summary>
    /// 单个玩家行为
    /// </summary>
    public struct PlayerAction
    {
        public ActionType Type;
        public int TargetEntityId;
        public FixedVector3 TargetPosition;
        public int SkillSlot;

        // 这个临时属性仅用于在表现层和逻辑层之间传递数据
        // 它不会被序列化或用于确定性计算
        [System.NonSerialized]
        public UnityEngine.Vector3 TargetPosition_Unity;
    }

    /// <summary>
    /// 单个玩家在一帧内的所有输入
    /// 优化：预分配Actions列表容量
    /// </summary>
    public class PlayerInput
    {
        /// <summary>
        /// 默认Actions列表容量，避免频繁扩容
        /// </summary>
        private const int DEFAULT_ACTIONS_CAPACITY = 4;
        
        public int PlayerId;
        public List<PlayerAction> Actions;
        public InputType InputFlags;
        public bool HasActions => Actions != null && Actions.Count > 0;

        public PlayerInput()
        {
            // 预分配容量，一般一帧内不会有超过4个动作
            Actions = new List<PlayerAction>(DEFAULT_ACTIONS_CAPACITY);
            InputFlags = InputType.None;
        }

        /// <summary>
        /// 克隆玩家输入
        /// 优化：预分配容量
        /// </summary>
        public PlayerInput Clone()
        {
            var clone = new PlayerInput
            {
                PlayerId = PlayerId,
                InputFlags = InputFlags,
                // 使用原列表容量或默认容量中较大的
                Actions = new List<PlayerAction>(Actions.Count > 0 ? Actions.Count : DEFAULT_ACTIONS_CAPACITY)
            };
            // 使用for循环复制，避免LINQ分配
            for (int i = 0; i < Actions.Count; i++)
            {
                clone.Actions.Add(Actions[i]);
            }
            return clone;
        }
    }

    /// <summary>
    /// 单个逻辑帧的所有输入
    /// 优化：使用for循环替代foreach
    /// </summary>
    public class FrameInput
    {
        public int FrameNumber;
        public PlayerInput[] PlayerInputs;

        /// <summary>
        /// 玩家数量
        /// </summary>
        public int PlayerCount => PlayerInputs?.Length ?? 0;

        public FrameInput(int frameNumber, int playerCount)
        {
            FrameNumber = frameNumber;
            PlayerInputs = new PlayerInput[playerCount];
            for (int i = 0; i < playerCount; i++)
            {
                PlayerInputs[i] = new PlayerInput { PlayerId = i };
            }
        }

        /// <summary>
        /// 设置指定玩家的输入（合并动作列表）
        /// 优化：使用for循环替代foreach避免迭代器问题
        /// </summary>
        public void SetPlayerInput(int playerId, PlayerInput input)
        {
            if (playerId >= 0 && playerId < PlayerInputs.Length)
            {
                var existing = PlayerInputs[playerId];
                
                // 如果已有输入且新输入也有动作，则合并动作列表
                if (existing != null && existing.HasActions && input != null && input.HasActions)
                {
                    // 合并动作列表 - 使用for循环避免迭代器问题
                    int actionCount = input.Actions.Count;
                    for (int i = 0; i < actionCount; i++)
                    {
                        existing.Actions.Add(input.Actions[i]);
                    }
                    // 合并输入标志
                    existing.InputFlags |= input.InputFlags;
                }
                else if (input != null && input.HasActions)
                {
                    // 如果现有输入为空或无动作，克隆新输入（避免引用同一对象）
                    PlayerInputs[playerId] = input.Clone();
                }
                // 如果新输入无动作，保留现有输入
            }
        }

        /// <summary>
        /// 获取指定玩家的输入
        /// </summary>
        public PlayerInput GetPlayerInput(int playerId)
        {
            if (playerId >= 0 && playerId < PlayerInputs.Length)
            {
                return PlayerInputs[playerId];
            }
            return null;
        }

        /// <summary>
        /// 检查所有玩家的输入是否完整
        /// 优化：使用for循环替代foreach
        /// </summary>
        public bool IsComplete()
        {
            if (PlayerInputs == null) return false;
            
            int count = PlayerInputs.Length;
            for (int i = 0; i < count; i++)
            {
                var input = PlayerInputs[i];
                if (input == null || input.InputFlags == InputType.None)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 克隆帧输入
        /// </summary>
        public FrameInput Clone()
        {
            var clone = new FrameInput(FrameNumber, PlayerCount);
            for (int i = 0; i < PlayerCount; i++)
            {
                if (PlayerInputs[i] != null)
                {
                    clone.PlayerInputs[i] = PlayerInputs[i].Clone();
                }
            }
            return clone;
        }
    }
}
