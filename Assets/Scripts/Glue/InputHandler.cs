// InputHandler.cs - 输入处理器
// 详细设计见 Architecture.md - Glue层架构
// 优化：复用PlayerInput对象，避免每帧GC分配

using MobaCombatCore.Core.Lockstep;
using MobaCombatCore.Core.Math;
using MobaCombatCore.Gameplay.Entity;
using MobaCombatCore.Glue.Services;
using UnityEngine;

namespace MobaCombatCore.Glue
{
    /// <summary>
    /// 输入处理器 - 将玩家输入转换为游戏逻辑操作
    /// 优化版本：复用PlayerInput对象避免GC
    /// </summary>
    public class InputHandler
    {
        private const string LOG_TAG = "InputHandler";
        
        private readonly LockstepManager _lockstepManager;
        private readonly Camera _mainCamera;
        
        /// <summary>
        /// 复用的PlayerInput对象，避免每帧创建新对象
        /// </summary>
        private readonly PlayerInput _reusablePlayerInput;
        
        /// <summary>
        /// 缓存的地面平面，避免每帧创建
        /// </summary>
        private readonly Plane _groundPlane;

        public InputHandler(LockstepManager lockstepManager)
        {
            _lockstepManager = lockstepManager;
            _mainCamera = Camera.main;
            _reusablePlayerInput = new PlayerInput();
            _groundPlane = new Plane(Vector3.up, Vector3.zero);
            
            GameLog.Info(LOG_TAG, "Constructor",
                $"初始化完成 - Camera: {(_mainCamera != null ? "有效" : "无效")}, LockstepManager: {(_lockstepManager != null ? "有效" : "无效")}");
        }

        public void CollectAndSubmitInput()
        {
            if (_mainCamera == null)
            {
                GameLog.Warning(LOG_TAG, "CollectAndSubmitInput", "主摄像机为空，无法收集输入");
                return;
            }
            
            // 复用PlayerInput对象，只清空Actions列表
            _reusablePlayerInput.Actions.Clear();
            _reusablePlayerInput.InputFlags = InputType.None;
            
            int currentFrame = _lockstepManager.CurrentFrame;
            int targetFrame = currentFrame + _lockstepManager.Config.InputBufferFrames;

            // 移动输入（鼠标右键）
            if (Input.GetMouseButton(1))
            {
                var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (_groundPlane.Raycast(ray, out var enter))
                {
                    var hitPoint = ray.GetPoint(enter);
                    var fixedPos = new FixedVector3(
                        Fixed64.FromFloat(hitPoint.x),
                        Fixed64.FromFloat(hitPoint.y),
                        Fixed64.FromFloat(hitPoint.z)
                    );
                    _reusablePlayerInput.Actions.Add(new PlayerAction
                    {
                        Type = ActionType.Move,
                        TargetPosition = fixedPos
                    });
                    
                    GameLog.Info(LOG_TAG, "CollectAndSubmitInput",
                        $"收集移动输入 - 当前帧:{currentFrame}, 目标帧:{targetFrame}, 目标位置:({hitPoint.x:F2}, {hitPoint.y:F2}, {hitPoint.z:F2})");
                }
                else
                {
                    GameLog.Warning(LOG_TAG, "CollectAndSubmitInput",
                        $"射线未命中地面平面 - 鼠标位置:{Input.mousePosition}");
                }
            }

            // 技能输入（Q、W、E、R 键）
            if (Input.GetKeyDown(KeyCode.Q)) TryAddSkillAction(0, currentFrame, targetFrame);
            if (Input.GetKeyDown(KeyCode.W)) TryAddSkillAction(1, currentFrame, targetFrame);
            if (Input.GetKeyDown(KeyCode.E)) TryAddSkillAction(2, currentFrame, targetFrame);
            if (Input.GetKeyDown(KeyCode.R)) TryAddSkillAction(3, currentFrame, targetFrame);

            // 提交输入
            if (_reusablePlayerInput.HasActions)
            {
                GameLog.Info(LOG_TAG, "CollectAndSubmitInput",
                    $"提交输入 - 动作数量:{_reusablePlayerInput.Actions.Count}, 当前帧:{currentFrame}, 目标帧:{targetFrame}, 玩家ID:{_lockstepManager.LocalPlayerId}");
            }
            
            _lockstepManager.SubmitLocalInput(_reusablePlayerInput);
        }

        /// <summary>
        /// 尝试添加技能动作
        /// 优化：使用缓存的地面平面
        /// </summary>
        private void TryAddSkillAction(int skillSlot, int currentFrame, int targetFrame)
        {
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (_groundPlane.Raycast(ray, out var enter))
            {
                var hitPoint = ray.GetPoint(enter);
                var fixedPos = new FixedVector3(
                    Fixed64.FromFloat(hitPoint.x),
                    Fixed64.FromFloat(hitPoint.y),
                    Fixed64.FromFloat(hitPoint.z)
                );
                _reusablePlayerInput.Actions.Add(new PlayerAction
                {
                    Type = ActionType.Skill,
                    SkillSlot = skillSlot,
                    TargetPosition = fixedPos
                });
                
                GameLog.Info(LOG_TAG, "TryAddSkillAction",
                    $"收集技能输入 - 技能槽:{skillSlot}, 当前帧:{currentFrame}, 目标帧:{targetFrame}, 目标位置:({hitPoint.x:F2}, {hitPoint.y:F2}, {hitPoint.z:F2})");
            }
            else
            {
                GameLog.Warning(LOG_TAG, "TryAddSkillAction",
                    $"技能{skillSlot}射线未命中地面平面");
            }
        }

        public void ProcessFrameInput(FrameInput frameInput, GameWorld gameWorld)
        {
            if (frameInput == null)
            {
                GameLog.Warning(LOG_TAG, "ProcessFrameInput", "frameInput 为空");
                return;
            }
            
            if (gameWorld == null)
            {
                GameLog.Warning(LOG_TAG, "ProcessFrameInput", "gameWorld 为空");
                return;
            }
            
            int frameNumber = frameInput.FrameNumber;
            int playerCount = frameInput.PlayerCount;
            int processedPlayers = 0;
            int processedActions = 0;
            
            GameLog.Debug(LOG_TAG, "ProcessFrameInput",
                $"开始处理帧输入 - 帧号:{frameNumber}, 玩家数量:{playerCount}, 实体总数:{gameWorld.AllEntities.Count}");
            
            foreach (var playerInput in frameInput.PlayerInputs)
            {
                if (playerInput == null)
                {
                    GameLog.Warning(LOG_TAG, "ProcessFrameInput",
                        $"帧{frameNumber} - playerInput 为空");
                    continue;
                }
                
                if (!playerInput.HasActions)
                {
                    GameLog.Debug(LOG_TAG, "ProcessFrameInput",
                        $"帧{frameNumber} - 玩家{playerInput.PlayerId} 无动作");
                    continue;
                }

                // 通过玩家ID（OwnerId）查找其控制的实体
                var entity = gameWorld.GetEntityByOwnerId(playerInput.PlayerId);
                if (entity == null)
                {
                    GameLog.Warning(LOG_TAG, "ProcessFrameInput",
                        $"帧{frameNumber} - 找不到玩家{playerInput.PlayerId}控制的实体! 当前实体列表:");
                    foreach (var e in gameWorld.AllEntities)
                    {
                        GameLog.Warning(LOG_TAG, "ProcessFrameInput",
                            $"  - 实体ID:{e.EntityId}, 类型:{e.Type}, OwnerId:{e.OwnerId}, 名称:{e.Name}");
                    }
                    continue;
                }
                
                processedPlayers++;
                
                GameLog.Info(LOG_TAG, "ProcessFrameInput",
                    $"帧{frameNumber} - 处理玩家{playerInput.PlayerId}的输入, 实体:{entity.Name}(ID:{entity.EntityId}), 动作数:{playerInput.Actions.Count}");

                foreach (var action in playerInput.Actions)
                {
                    ApplyActionToEntity(entity, action, frameNumber, _lockstepManager.LogicDeltaTime);
                    processedActions++;
                }
            }
            
            GameLog.Debug(LOG_TAG, "ProcessFrameInput",
                $"帧{frameNumber}处理完成 - 处理玩家数:{processedPlayers}, 处理动作数:{processedActions}");
        }

        private void ApplyActionToEntity(BaseEntity entity, PlayerAction action, int frame, Fixed64 deltaTime)
        {
            if (entity == null)
            {
                GameLog.Error(LOG_TAG, "ApplyActionToEntity", "实体为空!");
                return;
            }
            
            switch (action.Type)
            {
                case ActionType.Move:
                    var targetPos = action.TargetPosition;
                    var currentPos = entity.Position;
                    
                    GameLog.Info(LOG_TAG, "ApplyActionToEntity",
                        $"帧{frame} - 实体{entity.Name}(ID:{entity.EntityId}) 移动: 当前位置({currentPos.X.ToFloat():F2}, {currentPos.Y.ToFloat():F2}, {currentPos.Z.ToFloat():F2}) -> 目标位置({targetPos.X.ToFloat():F2}, {targetPos.Y.ToFloat():F2}, {targetPos.Z.ToFloat():F2}), CanMove:{entity.CanMove}");
                    
                    entity.MoveTo(action.TargetPosition);
                    
                    GameLog.Debug(LOG_TAG, "ApplyActionToEntity",
                        $"帧{frame} - 移动命令已发送, IsMoving:{entity.IsMoving}");
                    break;
                    
                case ActionType.Skill:
                    GameLog.Info(LOG_TAG, "ApplyActionToEntity",
                        $"帧{frame} - 实体{entity.Name}(ID:{entity.EntityId}) 使用技能{action.SkillSlot}, 目标位置:({action.TargetPosition.X.ToFloat():F2}, {action.TargetPosition.Y.ToFloat():F2}, {action.TargetPosition.Z.ToFloat():F2}), CanCast:{entity.CanCast}");
                    
                    // 如果是英雄实体，调用技能执行器
                    if (entity is HeroEntity hero)
                    {
                        bool success = hero.CastSkill(action.SkillSlot, action.TargetPosition, frame, deltaTime);
                        GameLog.Info(LOG_TAG, "ApplyActionToEntity",
                            $"帧{frame} - 技能{action.SkillSlot}释放结果: {(success ? "成功" : "失败")}");
                    }
                    else
                    {
                        GameLog.Warning(LOG_TAG, "ApplyActionToEntity",
                            $"帧{frame} - 实体{entity.Name}不是英雄，无法释放技能");
                    }
                    break;
                    
                case ActionType.Stop:
                    GameLog.Info(LOG_TAG, "ApplyActionToEntity",
                        $"帧{frame} - 实体{entity.Name}(ID:{entity.EntityId}) 停止移动");
                    entity.StopMovement();
                    break;
                    
                case ActionType.Attack:
                    GameLog.Info(LOG_TAG, "ApplyActionToEntity",
                        $"帧{frame} - 实体{entity.Name}(ID:{entity.EntityId}) 攻击目标实体ID:{action.TargetEntityId}");
                    // TODO: 实现攻击逻辑
                    break;
                    
                default:
                    GameLog.Warning(LOG_TAG, "ApplyActionToEntity",
                        $"帧{frame} - 未知动作类型:{action.Type}");
                    break;
            }
        }
    }
}
