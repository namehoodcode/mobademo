// SkillStateMachine.cs - 技能状态机
// 管理技能的状态流转：Idle → Casting → Executing → Recovery → Cooldown → Idle
// 纯逻辑层实现，无Unity依赖

using System;
using MobaCombatCore.Core.Math;
using MobaCombatCore.Glue.Services;

namespace MobaCombatCore.Gameplay.Skill
{
    /// <summary>
    /// 技能状态
    /// </summary>
    public enum SkillState
    {
        /// <summary>
        /// 空闲状态，可以释放技能
        /// </summary>
        Idle = 0,

        /// <summary>
        /// 前摇阶段，播放蓄力动画，可被打断
        /// </summary>
        Casting = 1,

        /// <summary>
        /// 执行阶段，生成弹道/判定范围
        /// </summary>
        Executing = 2,

        /// <summary>
        /// 后摇阶段，锁定操作
        /// </summary>
        Recovery = 3,

        /// <summary>
        /// 冷却阶段，等待CD结束
        /// </summary>
        Cooldown = 4
    }

    /// <summary>
    /// 技能状态机
    /// </summary>
    public class SkillStateMachine
    {
        private const string LOG_TAG = "SkillStateMachine";
        
        #region 属性

        /// <summary>
        /// 当前状态
        /// </summary>
        public SkillState CurrentState { get; private set; }

        /// <summary>
        /// 技能配置数据
        /// </summary>
        public SkillData SkillData { get; private set; }

        /// <summary>
        /// 当前状态已持续时间
        /// </summary>
        public Fixed64 StateElapsedTime { get; private set; }

        /// <summary>
        /// 冷却剩余时间
        /// </summary>
        public Fixed64 CooldownRemaining { get; private set; }

        /// <summary>
        /// 是否可以释放技能
        /// </summary>
        public bool CanCast => CurrentState == SkillState.Idle && CooldownRemaining <= Fixed64.Zero;

        /// <summary>
        /// 是否正在施法（前摇或执行阶段）
        /// </summary>
        public bool IsCasting => CurrentState == SkillState.Casting || CurrentState == SkillState.Executing;

        /// <summary>
        /// 是否在后摇阶段
        /// </summary>
        public bool IsRecovering => CurrentState == SkillState.Recovery;

        /// <summary>
        /// 是否在冷却中
        /// </summary>
        public bool IsOnCooldown => CooldownRemaining > Fixed64.Zero;

        /// <summary>
        /// 冷却进度（0-1）
        /// </summary>
        public Fixed64 CooldownProgress
        {
            get
            {
                if (SkillData == null) return Fixed64.One;
                var totalCooldown = SkillData.GetCooldownFixed();
                if (totalCooldown <= Fixed64.Zero) return Fixed64.One;
                return Fixed64.One - (CooldownRemaining / totalCooldown);
            }
        }

        #endregion

        #region 事件

        /// <summary>
        /// 状态变化事件
        /// </summary>
        public event Action<SkillState, SkillState> OnStateChanged;

        /// <summary>
        /// 前摇开始事件
        /// </summary>
        public event Action OnCastStart;

        /// <summary>
        /// 技能执行事件（前摇结束，开始判定）
        /// </summary>
        public event Action OnExecute;

        /// <summary>
        /// 后摇开始事件
        /// </summary>
        public event Action OnRecoveryStart;

        /// <summary>
        /// 技能完成事件（后摇结束）
        /// </summary>
        public event Action OnComplete;

        /// <summary>
        /// 技能被打断事件
        /// </summary>
        public event Action OnInterrupted;

        /// <summary>
        /// 冷却结束事件
        /// </summary>
        public event Action OnCooldownEnd;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public SkillStateMachine(SkillData skillData)
        {
            SkillData = skillData;
            CurrentState = SkillState.Idle;
            StateElapsedTime = Fixed64.Zero;
            CooldownRemaining = Fixed64.Zero;
        }

        #region 状态控制

        /// <summary>
        /// 开始施法
        /// </summary>
        /// <returns>是否成功开始施法</returns>
        public bool StartCast()
        {
            if (!CanCast)
            {
                GameLog.Warning(LOG_TAG, "StartCast",
                    $"无法开始施法 - 当前状态:{CurrentState}, CD剩余:{CooldownRemaining.ToFloat():F2}s");
                return false;
            }

            GameLog.Info(LOG_TAG, "StartCast",
                $"开始施法 - 技能:{SkillData?.skillName ?? "null"}, 前摇时间:{SkillData?.castTime ?? 0}s");
            TransitionTo(SkillState.Casting);
            OnCastStart?.Invoke();
            return true;
        }

        /// <summary>
        /// 打断技能
        /// </summary>
        public void Interrupt()
        {
            if (CurrentState == SkillState.Casting || CurrentState == SkillState.Executing)
            {
                OnInterrupted?.Invoke();
                TransitionTo(SkillState.Idle);
                // 打断不进入冷却
            }
        }

        /// <summary>
        /// 强制重置（用于死亡等情况）
        /// </summary>
        public void ForceReset()
        {
            TransitionTo(SkillState.Idle);
            CooldownRemaining = Fixed64.Zero;
        }

        /// <summary>
        /// 重置冷却
        /// </summary>
        public void ResetCooldown()
        {
            CooldownRemaining = Fixed64.Zero;
            if (CurrentState == SkillState.Cooldown)
            {
                TransitionTo(SkillState.Idle);
                OnCooldownEnd?.Invoke();
            }
        }

        /// <summary>
        /// 减少冷却时间
        /// </summary>
        public void ReduceCooldown(Fixed64 amount)
        {
            CooldownRemaining = Fixed64.Max(Fixed64.Zero, CooldownRemaining - amount);
            if (CooldownRemaining <= Fixed64.Zero && CurrentState == SkillState.Cooldown)
            {
                TransitionTo(SkillState.Idle);
                OnCooldownEnd?.Invoke();
            }
        }

        public void ForceExecute(FixedVector3 direction)
        {
            if (SkillData.targetType == SkillTargetType.Direction)
            {
                // This is not how the state machine should work.
                // The direction should be part of the context, not set here.
                // However, for the stress test, this is a quick way to get it working.
                // TODO: Refactor this to be more robust.
            }
            TransitionTo(SkillState.Executing);
            OnExecute?.Invoke();
        }

        #endregion

        #region 更新

        /// <summary>
        /// 逻辑更新
        /// </summary>
        /// <param name="deltaTime">逻辑帧间隔</param>
        public void Update(Fixed64 deltaTime)
        {
            StateElapsedTime += deltaTime;

            switch (CurrentState)
            {
                case SkillState.Idle:
                    UpdateIdle(deltaTime);
                    break;
                case SkillState.Casting:
                    UpdateCasting(deltaTime);
                    break;
                case SkillState.Executing:
                    UpdateExecuting(deltaTime);
                    break;
                case SkillState.Recovery:
                    UpdateRecovery(deltaTime);
                    break;
                case SkillState.Cooldown:
                    UpdateCooldown(deltaTime);
                    break;
            }
        }

        private void UpdateIdle(Fixed64 deltaTime)
        {
            // 更新冷却
            if (CooldownRemaining > Fixed64.Zero)
            {
                CooldownRemaining -= deltaTime;
                if (CooldownRemaining <= Fixed64.Zero)
                {
                    CooldownRemaining = Fixed64.Zero;
                    OnCooldownEnd?.Invoke();
                }
            }
        }

        private void UpdateCasting(Fixed64 deltaTime)
        {
            // 检查前摇是否结束
            var castTime = SkillData.GetCastTimeFixed();
            if (StateElapsedTime >= castTime)
            {
                GameLog.Info(LOG_TAG, "UpdateCasting",
                    $"前摇结束 - 已用时:{StateElapsedTime.ToFloat():F3}s, 前摇时间:{castTime.ToFloat():F3}s, 触发OnExecute事件");
                TransitionTo(SkillState.Executing);
                
                GameLog.Info(LOG_TAG, "UpdateCasting",
                    $"OnExecute订阅者数量:{OnExecute?.GetInvocationList()?.Length ?? 0}");
                OnExecute?.Invoke();
            }
        }

        private void UpdateExecuting(Fixed64 deltaTime)
        {
            // 执行阶段通常是瞬时的，立即进入后摇
            // 对于持续性技能（如暴风雪），需要在外部控制何时结束执行阶段
            TransitionTo(SkillState.Recovery);
            OnRecoveryStart?.Invoke();
        }

        private void UpdateRecovery(Fixed64 deltaTime)
        {
            // 检查后摇是否结束
            if (StateElapsedTime >= SkillData.GetRecoveryTimeFixed())
            {
                // 进入冷却
                CooldownRemaining = SkillData.GetCooldownFixed();
                TransitionTo(SkillState.Cooldown);
                OnComplete?.Invoke();
            }
        }

        private void UpdateCooldown(Fixed64 deltaTime)
        {
            CooldownRemaining -= deltaTime;
            if (CooldownRemaining <= Fixed64.Zero)
            {
                CooldownRemaining = Fixed64.Zero;
                TransitionTo(SkillState.Idle);
                OnCooldownEnd?.Invoke();
            }
        }

        #endregion

        #region 内部方法

        private void TransitionTo(SkillState newState)
        {
            if (CurrentState == newState) return;

            var oldState = CurrentState;
            CurrentState = newState;
            StateElapsedTime = Fixed64.Zero;

            GameLog.Debug(LOG_TAG, "TransitionTo",
                $"状态转换: {oldState} -> {newState}");

            OnStateChanged?.Invoke(oldState, newState);
        }

        #endregion

        #region 快照

        /// <summary>
        /// 创建状态快照
        /// </summary>
        public SkillStateSnapshot CreateSnapshot()
        {
            return new SkillStateSnapshot
            {
                State = CurrentState,
                StateElapsedTime = StateElapsedTime,
                CooldownRemaining = CooldownRemaining
            };
        }

        /// <summary>
        /// 从快照恢复
        /// </summary>
        public void RestoreFromSnapshot(SkillStateSnapshot snapshot)
        {
            CurrentState = snapshot.State;
            StateElapsedTime = snapshot.StateElapsedTime;
            CooldownRemaining = snapshot.CooldownRemaining;
        }

        #endregion
    }

    /// <summary>
    /// 技能状态快照
    /// </summary>
    public struct SkillStateSnapshot
    {
        public SkillState State;
        public Fixed64 StateElapsedTime;
        public Fixed64 CooldownRemaining;
    }
}
