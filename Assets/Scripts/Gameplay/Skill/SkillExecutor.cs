// SkillExecutor.cs - 技能执行器
// 管理实体的所有技能，协调状态机和技能逻辑
// 纯逻辑层实现

using System;
using System.Collections.Generic;
using MobaCombatCore.Core.Math;
using MobaCombatCore.Gameplay.Entity;
using MobaCombatCore.Glue.Services;

namespace MobaCombatCore.Gameplay.Skill
{
    /// <summary>
    /// 技能槽位
    /// </summary>
    public class SkillSlot
    {
        /// <summary>
        /// 槽位索引
        /// </summary>
        public int SlotIndex { get; private set; }

        /// <summary>
        /// 技能配置
        /// </summary>
        public SkillData SkillData { get; private set; }

        /// <summary>
        /// 技能状态机
        /// </summary>
        public SkillStateMachine StateMachine { get; private set; }

        /// <summary>
        /// 技能逻辑
        /// </summary>
        public ISkillLogic SkillLogic { get; private set; }

        /// <summary>
        /// 当前技能上下文
        /// </summary>
        public SkillContext CurrentContext;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => SkillData != null && SkillLogic != null;

        public SkillSlot(int slotIndex)
        {
            SlotIndex = slotIndex;
        }

        /// <summary>
        /// 设置技能
        /// </summary>
        public void SetSkill(SkillData skillData, ISkillLogic skillLogic)
        {
            SkillData = skillData;
            SkillLogic = skillLogic;
            StateMachine = new SkillStateMachine(skillData);
        }

        /// <summary>
        /// 清除技能
        /// </summary>
        public void ClearSkill()
        {
            SkillData = null;
            SkillLogic = null;
            StateMachine = null;
        }
    }

    /// <summary>
    /// 技能执行器 - 管理实体的所有技能
    /// </summary>
    public class SkillExecutor
    {
        private const string LOG_TAG = "SkillExecutor";
        
        #region 常量

        /// <summary>
        /// 最大技能槽位数
        /// </summary>
        public const int MAX_SKILL_SLOTS = 4;

        #endregion

        #region 属性

        /// <summary>
        /// 所属实体
        /// </summary>
        public BaseEntity Owner { get; private set; }

        /// <summary>
        /// 技能槽位
        /// </summary>
        public SkillSlot[] SkillSlots { get; private set; }

        /// <summary>
        /// 当前正在施放的技能槽位索引（-1表示无）
        /// </summary>
        public int ActiveSkillSlot { get; private set; } = -1;

        /// <summary>
        /// 是否正在施法
        /// </summary>
        public bool IsCasting => ActiveSkillSlot >= 0;

        #endregion

        #region 事件

        /// <summary>
        /// 技能开始施放事件
        /// </summary>
        public event Action<int, SkillData> OnSkillCastStart;

        /// <summary>
        /// 技能执行事件
        /// </summary>
        public event Action<int, SkillData, SkillResult> OnSkillExecute;

        /// <summary>
        /// 技能完成事件
        /// </summary>
        public event Action<int, SkillData> OnSkillComplete;

        /// <summary>
        /// 技能被打断事件
        /// </summary>
        public event Action<int, SkillData> OnSkillInterrupted;

        /// <summary>
        /// 弹道生成事件
        /// </summary>
        public event Action<ProjectileEntity> OnProjectileCreated;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public SkillExecutor(BaseEntity owner)
        {
            Owner = owner;

            // 初始化技能槽位
            SkillSlots = new SkillSlot[MAX_SKILL_SLOTS];
            for (int i = 0; i < MAX_SKILL_SLOTS; i++)
            {
                SkillSlots[i] = new SkillSlot(i);
            }
            
            GameLog.Debug(LOG_TAG, "Constructor", "技能执行器初始化完成");
        }

        #region 技能管理

        /// <summary>
        /// 设置技能到指定槽位
        /// </summary>
        public void SetSkill(int slotIndex, SkillData skillData, ISkillLogic skillLogic)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SKILL_SLOTS) return;

            SkillSlots[slotIndex].SetSkill(skillData, skillLogic);

            // 绑定状态机事件
            var slot = SkillSlots[slotIndex];
            slot.StateMachine.OnExecute += () => HandleSkillExecute(slotIndex);
            slot.StateMachine.OnComplete += () => HandleSkillComplete(slotIndex);
            slot.StateMachine.OnInterrupted += () => HandleSkillInterrupted(slotIndex);
        }

        /// <summary>
        /// 获取技能槽位
        /// </summary>
        public SkillSlot GetSkillSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SKILL_SLOTS) return null;
            return SkillSlots[slotIndex];
        }

        /// <summary>
        /// 检查技能是否可以释放
        /// </summary>
        public bool CanCastSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SKILL_SLOTS)
            {
                GameLog.Debug(LOG_TAG, "CanCastSkill", $"技能{slotIndex}无法释放: 槽位索引无效");
                return false;
            }

            var slot = SkillSlots[slotIndex];
            if (!slot.IsInitialized)
            {
                GameLog.Debug(LOG_TAG, "CanCastSkill", $"技能{slotIndex}无法释放: 技能未初始化");
                return false;
            }

            // 检查是否正在施法
            if (IsCasting)
            {
                GameLog.Debug(LOG_TAG, "CanCastSkill",
                    $"技能{slotIndex}无法释放: 正在施法中(ActiveSkillSlot={ActiveSkillSlot})");
                return false;
            }

            // 检查实体状态
            if (!Owner.CanCast)
            {
                GameLog.Debug(LOG_TAG, "CanCastSkill", $"技能{slotIndex}无法释放: 实体无法施法");
                return false;
            }

            // 检查技能状态机
            if (!slot.StateMachine.CanCast)
            {
                GameLog.Debug(LOG_TAG, "CanCastSkill",
                    $"技能{slotIndex}无法释放: 状态机不允许(State={slot.StateMachine.CurrentState}, CD={slot.StateMachine.CooldownRemaining.ToFloat():F2}s)");
                return false;
            }

            // 检查法力值
            if (Owner.Stats.CurrentMana < slot.SkillData.manaCost)
            {
                GameLog.Debug(LOG_TAG, "CanCastSkill",
                    $"技能{slotIndex}无法释放: 法力不足({Owner.Stats.CurrentMana}/{slot.SkillData.manaCost})");
                return false;
            }

            return true;
        }

        #endregion

        #region 技能释放

        /// <summary>
        /// 释放技能（指定方向）
        /// </summary>
        public bool CastSkill(int slotIndex, FixedVector3 direction, int currentFrame, Fixed64 deltaTime)
        {
            return CastSkillInternal(slotIndex, null, FixedVector3.Zero, direction, currentFrame, deltaTime);
        }

        /// <summary>
        /// 释放技能（指定目标位置）
        /// </summary>
        public bool CastSkillAtPosition(int slotIndex, FixedVector3 targetPosition, int currentFrame, Fixed64 deltaTime)
        {
            var direction = (targetPosition - Owner.Position).Normalized2D;
            return CastSkillInternal(slotIndex, null, targetPosition, direction, currentFrame, deltaTime);
        }

        /// <summary>
        /// 释放技能（指定目标实体）
        /// </summary>
        public bool CastSkillAtTarget(int slotIndex, BaseEntity target, int currentFrame, Fixed64 deltaTime)
        {
            if (target == null) return false;
            var direction = (target.Position - Owner.Position).Normalized2D;
            return CastSkillInternal(slotIndex, target, target.Position, direction, currentFrame, deltaTime);
        }

        private bool CastSkillInternal(int slotIndex, BaseEntity target, FixedVector3 targetPosition,
            FixedVector3 direction, int currentFrame, Fixed64 deltaTime)
        {
            if (!CanCastSkill(slotIndex)) return false;

            var slot = SkillSlots[slotIndex];

            // 创建技能上下文
            slot.CurrentContext = new SkillContext
            {
                Caster = Owner,
                Target = target,
                TargetPosition = targetPosition,
                Direction = direction,
                SkillData = slot.SkillData,
                CurrentFrame = currentFrame,
                DeltaTime = deltaTime
            };

            // 验证技能
            if (!slot.SkillLogic.Validate(ref slot.CurrentContext))
            {
                return false;
            }

            // 开始施法
            if (!slot.StateMachine.StartCast())
            {
                return false;
            }

            // 设置当前激活的技能槽位
            ActiveSkillSlot = slotIndex;

            // 调用技能逻辑的前摇开始
            slot.SkillLogic.OnCastStart(ref slot.CurrentContext);

            // 触发事件
            OnSkillCastStart?.Invoke(slotIndex, slot.SkillData);

            return true;
        }

        /// <summary>
        /// 打断当前技能
        /// </summary>
        public void InterruptCurrentSkill()
        {
            if (!IsCasting) return;

            var slot = SkillSlots[ActiveSkillSlot];
            slot.StateMachine.Interrupt();
        }

        /// <summary>
        /// 强制释放技能（跳过前摇和冷却检查，用于压力测试）
        /// </summary>
        public void CastSkillForced(int slot, FixedVector3 direction)
        {
            if (slot < 0 || slot >= SkillSlots.Length || SkillSlots[slot] == null)
            {
                GameLog.Warning(LOG_TAG, "CastSkillForced", $"尝试释放无效的技能槽位: {slot}");
                return;
            }

            var skillSlot = SkillSlots[slot];
            if (!skillSlot.IsInitialized)
            {
                GameLog.Warning(LOG_TAG, "CastSkillForced", $"技能槽位{slot}未初始化");
                return;
            }

            // 初始化完整的技能上下文（这是关键！）
            skillSlot.CurrentContext = new SkillContext
            {
                Caster = Owner,
                Target = null,
                TargetPosition = Owner.Position + direction * skillSlot.SkillData.GetRangeFixed(),
                Direction = direction,
                SkillData = skillSlot.SkillData,
                CurrentFrame = 0,
                DeltaTime = Fixed64.FromFloat(0.033f) // 默认逻辑帧间隔
            };

            // 强制执行技能（跳过前摇）
            skillSlot.StateMachine.ForceExecute(direction);
        }

        #endregion

        #region 更新

        /// <summary>
        /// 逻辑更新
        /// </summary>
        public void Update(int currentFrame, Fixed64 deltaTime)
        {
            // 更新所有技能状态机
            for (int i = 0; i < MAX_SKILL_SLOTS; i++)
            {
                var slot = SkillSlots[i];
                if (slot.IsInitialized)
                {
                    // 更新上下文的帧信息
                    slot.CurrentContext.CurrentFrame = currentFrame;
                    slot.CurrentContext.DeltaTime = deltaTime;

                    slot.StateMachine.Update(deltaTime);
                }
            }
        }

        #endregion

        #region 事件处理

        private void HandleSkillExecute(int slotIndex)
        {
            var slot = SkillSlots[slotIndex];
            if (slot == null || !slot.IsInitialized)
            {
                GameLog.Warning(LOG_TAG, "HandleSkillExecute",
                    $"技能槽位{slotIndex}无效或未初始化");
                return;
            }

            GameLog.Info(LOG_TAG, "HandleSkillExecute",
                $"开始执行技能 - 槽位:{slotIndex}, 技能名:{slot.SkillData?.skillName ?? "null"}, " +
                $"施法者:{slot.CurrentContext.Caster?.Name ?? "null"}");

            // 执行技能逻辑
            var result = slot.SkillLogic.Execute(ref slot.CurrentContext);

            GameLog.Info(LOG_TAG, "HandleSkillExecute",
                $"技能执行结果 - 成功:{result.Success}, 弹道:{(result.Projectile != null ? "已创建" : "无")}, " +
                $"伤害:{result.TotalDamage}, 错误:{result.ErrorMessage ?? "无"}");

            // 如果生成了弹道，触发事件
            if (result.Projectile != null)
            {
                GameLog.Info(LOG_TAG, "HandleSkillExecute",
                    $"触发OnProjectileCreated事件 - 弹道ID:{result.Projectile.EntityId}, " +
                    $"订阅者数量:{OnProjectileCreated?.GetInvocationList()?.Length ?? 0}");
                OnProjectileCreated?.Invoke(result.Projectile);
            }
            else
            {
                GameLog.Warning(LOG_TAG, "HandleSkillExecute",
                    "技能执行未生成弹道!");
            }

            // 触发事件
            OnSkillExecute?.Invoke(slotIndex, slot.SkillData, result);
        }

        private void HandleSkillComplete(int slotIndex)
        {
            var slot = SkillSlots[slotIndex];
            if (slot == null || !slot.IsInitialized) return;

            GameLog.Debug(LOG_TAG, "HandleSkillComplete",
                $"技能{slotIndex}完成 - 当前ActiveSkillSlot={ActiveSkillSlot}");

            // 调用技能逻辑的完成回调
            slot.SkillLogic.OnComplete(ref slot.CurrentContext);

            // 清除激活状态
            if (ActiveSkillSlot == slotIndex)
            {
                ActiveSkillSlot = -1;
                GameLog.Debug(LOG_TAG, "HandleSkillComplete", "ActiveSkillSlot已重置为-1");
            }

            // 触发事件
            OnSkillComplete?.Invoke(slotIndex, slot.SkillData);
        }

        private void HandleSkillInterrupted(int slotIndex)
        {
            var slot = SkillSlots[slotIndex];
            if (slot == null || !slot.IsInitialized) return;

            // 调用技能逻辑的打断回调
            slot.SkillLogic.OnInterrupted(ref slot.CurrentContext);

            // 清除激活状态
            if (ActiveSkillSlot == slotIndex)
            {
                ActiveSkillSlot = -1;
            }

            // 触发事件
            OnSkillInterrupted?.Invoke(slotIndex, slot.SkillData);
        }

        #endregion

        #region 快照

        /// <summary>
        /// 创建快照
        /// </summary>
        public SkillExecutorSnapshot CreateSnapshot()
        {
            var snapshot = new SkillExecutorSnapshot
            {
                ActiveSkillSlot = ActiveSkillSlot,
                SkillSnapshots = new SkillStateSnapshot[MAX_SKILL_SLOTS]
            };

            for (int i = 0; i < MAX_SKILL_SLOTS; i++)
            {
                if (SkillSlots[i].IsInitialized)
                {
                    snapshot.SkillSnapshots[i] = SkillSlots[i].StateMachine.CreateSnapshot();
                }
            }

            return snapshot;
        }

        /// <summary>
        /// 从快照恢复
        /// </summary>
        public void RestoreFromSnapshot(SkillExecutorSnapshot snapshot)
        {
            ActiveSkillSlot = snapshot.ActiveSkillSlot;

            for (int i = 0; i < MAX_SKILL_SLOTS; i++)
            {
                if (SkillSlots[i].IsInitialized && snapshot.SkillSnapshots != null)
                {
                    SkillSlots[i].StateMachine.RestoreFromSnapshot(snapshot.SkillSnapshots[i]);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 技能执行器快照
    /// </summary>
    public struct SkillExecutorSnapshot
    {
        public int ActiveSkillSlot;
        public SkillStateSnapshot[] SkillSnapshots;
    }
}
