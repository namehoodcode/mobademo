// InputCollector.cs - 玩家输入收集器
// 负责从Unity的输入系统采集玩家操作，并转换为帧同步所需的PlayerInput数据
// 在这里实现客户端预测的关键逻辑

using System.Collections.Generic;
using MobaCombatCore.Core.Lockstep;
using UnityEngine;

namespace MobaCombatCore.Presentation
{
    public class InputCollector : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("本地玩家ID")]
        public int localPlayerId = 0;

        [Tooltip("关联的LockstepManager")]
        public LockstepManager lockstepManager; // 需要在Inspector中或通过代码设置

        [Header("客户端预测")]
        [Tooltip("是否启用客户端预测")]
        public bool enableClientPrediction = true;

        [Tooltip("本地玩家的表现层实体")]
        public EntityView localPlayerView; // 需要在Inspector中或通过代码设置

        private List<PlayerInput> _pendingInputs = new List<PlayerInput>();

        void Update()
        {
            if (lockstepManager == null || !lockstepManager.IsRunning) return;

            // 1. 采集输入
            var input = CollectInput();

            // 2. 如果有输入，提交到帧同步系统
            if (input.HasActions)
            {
                input.PlayerId = localPlayerId;
                // 注意：这里我们使用了InputBufferFrames来提前发送输入
                // 这是为了模拟网络延迟，让输入有时间到达“服务器”
                int targetFrame = lockstepManager.CurrentFrame + lockstepManager.Config.InputBufferFrames;
                lockstepManager.SubmitInput(targetFrame, input);

                // 3. 客户端预测
                if (enableClientPrediction && localPlayerView != null && localPlayerView.Entity != null)
                {
                    ApplyPrediction(input);
                }
            }
        }

        /// <summary>
        /// 采集当前帧的输入
        /// </summary>
        private PlayerInput CollectInput()
        {
            var input = new PlayerInput();
            input.Actions = new List<PlayerAction>();

            // 移动输入
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");
            if (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveZ) > 0.1f)
            {
                var moveVec = new Vector3(moveX, 0, moveZ);
                var moveAction = new PlayerAction
                {
                    Type = ActionType.Move,
                    // 表现层数据
                    TargetPosition_Unity = moveVec,
                    // 逻辑层数据，在此处完成转换
                    TargetPosition = new Core.Math.FixedVector3(
                        Core.Math.Fixed64.FromFloat(moveVec.x),
                        Core.Math.Fixed64.FromFloat(moveVec.y),
                        Core.Math.Fixed64.FromFloat(moveVec.z)
                    )
                };
                input.Actions.Add(moveAction);
            }

            // 技能输入（示例）
            if (Input.GetKeyDown(KeyCode.Q))
            {
                input.Actions.Add(new PlayerAction { Type = ActionType.Skill, SkillSlot = 0 });
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                input.Actions.Add(new PlayerAction { Type = ActionType.Skill, SkillSlot = 1 });
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                input.Actions.Add(new PlayerAction { Type = ActionType.Skill, SkillSlot = 2 });
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                input.Actions.Add(new PlayerAction { Type = ActionType.Skill, SkillSlot = 3 });
            }

            return input;
        }

        /// <summary>
        /// 应用客户端预测
        /// </summary>
        private void ApplyPrediction(PlayerInput input)
        {
            var logicEntity = localPlayerView.Entity;
            if (logicEntity == null || !logicEntity.CanMove) return;

            foreach (var action in input.Actions)
            {
                if (action.Type == ActionType.Move)
                {
                    // 在表现层立即模拟移动
                    var moveDirection = action.TargetPosition_Unity.normalized;
                    var moveSpeed = (float)logicEntity.Stats.MoveSpeed;

                    // 计算预测位置
                    // 注意：这是简化预测，仅移动表现层对象
                    // 并没有真正运行逻辑，所以不会有碰撞等
                    var predictedMovement = moveDirection * moveSpeed * Time.deltaTime;
                    localPlayerView.transform.position += predictedMovement;

                    // 预测旋转
                    if (moveDirection.sqrMagnitude > 0.1f)
                    {
                        var targetRotation = Quaternion.LookRotation(moveDirection);
                        localPlayerView.transform.rotation = Quaternion.Slerp(localPlayerView.transform.rotation, targetRotation, Time.deltaTime * localPlayerView.rotationSpeed);
                    }
                }
            }
        }

        /// <summary>
        /// 设置本地玩家实体
        /// </summary>
        public void SetLocalPlayerView(EntityView view)
        {
            localPlayerView = view;
        }

        /// <summary>
        /// 设置LockstepManager
        /// </summary>
        public void SetLockstepManager(LockstepManager manager)
        {
            lockstepManager = manager;
        }
    }
}
