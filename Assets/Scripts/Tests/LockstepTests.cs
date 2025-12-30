
using System;
using System.Collections.Generic;
using UnityEngine;
using MobaCombatCore.Core.Math;
using MobaCombatCore.Core.Lockstep;
using MobaCombatCore.Core.Network;
using MobaCombatCore.Gameplay.Entity;

namespace MobaCombatCore.Tests
{
    /// <summary>
    /// Day 2 帧同步系统单元测试
    /// 测试LockstepManager、FrameInput、InputBuffer、Snapshot、DelaySimulator、LocalServer、BaseEntity
    /// </summary>
    public class LockstepTests : MonoBehaviour
    {
        // 测试统计
        private int _totalTests = 0;
        private int _passedTests = 0;
        private int _failedTests = 0;

        [ContextMenu("Run All Lockstep Tests")]
        public void RunAllTests()
        {
            _totalTests = 0;
            _passedTests = 0;
            _failedTests = 0;

            Debug.Log("========================================");
            Debug.Log("开始运行Day 2帧同步系统单元测试");
            Debug.Log("========================================\n");

            // FrameInput 测试
            TestPlayerInput();
            TestFrameInput();

            // InputBuffer 测试
            TestInputBuffer();

            // LogicFrame 测试
            TestLogicFrame();
            TestLockstepConfig();

            // Snapshot 测试
            TestEntitySnapshot();
            TestGameSnapshot();
            TestSnapshotManager();

            // DelaySimulator 测试
            TestDelayConfig();
            TestDelaySimulator();

            // LocalServer 测试
            TestLocalServer();

            // BaseEntity 测试
            TestBaseEntityCreation();
            TestBaseEntityMovement();
            TestBaseEntityCombat();
            TestBaseEntitySnapshot();

            // LockstepManager 测试
            TestLockstepManager();

            // 输出总结
            Debug.Log("\n========================================");
            Debug.Log($"测试完成: {_passedTests}/{_totalTests} 通过");
            if (_failedTests > 0)
            {
                Debug.LogError($"失败测试数: {_failedTests}");
            }
            else
            {
                Debug.Log("<color=green>所有测试通过!</color>");
            }
            Debug.Log("========================================");
        }

        #region PlayerInput & PlayerAction Tests

        [ContextMenu("Test PlayerAction & PlayerInput")]
        public void TestPlayerInput()
        {
            Debug.Log("\n--- PlayerAction & PlayerInput 测试 ---");

            // 1. 创建一个移动 Action
            var moveAction = new PlayerAction
            {
                Type = ActionType.Move,
                TargetPosition = FixedVector3.FromInt(10, 0, 5)
            };
            AssertEqual((int)moveAction.Type, (int)ActionType.Move, "Action Type is Move");
            AssertTrue(moveAction.TargetPosition.X == 10, "Move Action TargetPosition.X is 10");

            // 2. 创建一个技能 Action
            var skillAction = new PlayerAction
            {
                Type = ActionType.Skill,
                SkillSlot = 2
            };
            AssertEqual((int)skillAction.Type, (int)ActionType.Skill, "Action Type is Skill");
            AssertEqual(skillAction.SkillSlot, 2, "Skill Action SkillSlot is 2");

            // 3. 创建 PlayerInput 并添加 Actions
            var playerInput = new PlayerInput { PlayerId = 1 };
            AssertTrue(!playerInput.HasActions, "新 PlayerInput 没有 Actions");

            playerInput.Actions.Add(moveAction);
            playerInput.Actions.Add(skillAction);

            AssertTrue(playerInput.HasActions, "添加后 PlayerInput 有 Actions");
            AssertEqual(playerInput.Actions.Count, 2, "PlayerInput 有 2 个 Actions");
            AssertEqual((int)playerInput.Actions[0].Type, (int)ActionType.Move, "第一个 Action 是 Move");
            AssertEqual((int)playerInput.Actions[1].Type, (int)ActionType.Skill, "第二个 Action 是 Skill");
        }

        #endregion

        #region FrameInput Tests

        [ContextMenu("Test FrameInput")]
        public void TestFrameInput()
        {
            Debug.Log("\n--- FrameInput 测试 ---");

            // 1. 创建帧输入
            var frameInput = new FrameInput(10, 2); // 帧号10, 2个玩家
            AssertEqual(frameInput.FrameNumber, 10, "FrameNumber = 10");
            AssertEqual(frameInput.PlayerInputs.Length, 2, "PlayerInputs.Length = 2");
            AssertEqual(frameInput.PlayerInputs[0].PlayerId, 0, "Player 0 Id is 0");
            AssertEqual(frameInput.PlayerInputs[1].PlayerId, 1, "Player 1 Id is 1");

            // 2. 向其中一个玩家输入添加Action
            var skillAction = new PlayerAction { Type = ActionType.Skill, SkillSlot = 1 };
            frameInput.PlayerInputs[1].Actions.Add(skillAction);

            // 3. 验证
            AssertTrue(frameInput.PlayerInputs[1].HasActions, "Player 1 has actions");
            AssertEqual(frameInput.PlayerInputs[1].Actions.Count, 1, "Player 1 has 1 action");
            AssertEqual((int)frameInput.PlayerInputs[1].Actions[0].Type, (int)ActionType.Skill, "Player 1 action is Skill");
            AssertTrue(!frameInput.PlayerInputs[0].HasActions, "Player 0 has no actions");
        }

        #endregion

        #region InputBuffer Tests

        [ContextMenu("Test InputBuffer")]
        public void TestInputBuffer()
        {
            Debug.Log("\n--- InputBuffer 测试 ---");

            var buffer = new InputBuffer(2);
            AssertEqual(buffer.BufferedFrameCount, 0, "初始BufferedFrameCount = 0");
            AssertEqual(buffer.LastConfirmedFrame, -1, "初始LastConfirmedFrame = -1");

            // 添加帧输入
            buffer.AddInput(new FrameInput(0, 2));
            buffer.AddInput(new FrameInput(1, 2));
            buffer.AddInput(new FrameInput(2, 2));

            AssertEqual(buffer.BufferedFrameCount, 3, "添加3帧后BufferedFrameCount = 3");

            // 获取输入
            var retrieved = buffer.GetInput(1);
            AssertTrue(retrieved != null, "GetInput(1) != null");
            AssertEqual(retrieved.FrameNumber, 1, "GetInput(1).FrameNumber = 1");

            // 确认帧
            buffer.ConfirmFrame(1);
            AssertEqual(buffer.LastConfirmedFrame, 1, "ConfirmFrame(1)后LastConfirmedFrame = 1");
            AssertEqual(buffer.BufferedFrameCount, 1, "确认后BufferedFrameCount = 1");

            // 添加玩家输入
            var playerInput = new PlayerInput { PlayerId = 0 };
            playerInput.Actions.Add(new PlayerAction { Type = ActionType.Move });
            buffer.AddPlayerInput(5, 0, playerInput);

            var frame5 = buffer.GetInput(5);
            AssertTrue(frame5 != null, "AddPlayerInput创建了新帧");
            AssertTrue(frame5.PlayerInputs[0].HasActions, "玩家输入已添加");
            AssertEqual((int)frame5.PlayerInputs[0].Actions[0].Type, (int)ActionType.Move, "Player 0 Action is Move");

            // 重置到帧
            buffer.ResetToFrame(3);
            AssertEqual(buffer.LastConfirmedFrame, 2, "ResetToFrame(3)后LastConfirmedFrame = 2");

            // 清空
            buffer.Clear();
            AssertEqual(buffer.BufferedFrameCount, 0, "Clear后BufferedFrameCount = 0");
        }

        #endregion

        #region LogicFrame Tests

        [ContextMenu("Test LogicFrame")]
        public void TestLogicFrame()
        {
            Debug.Log("\n--- LogicFrame 测试 ---");

            var frame = new LogicFrame(5);
            AssertEqual(frame.FrameNumber, 5, "FrameNumber = 5");
            AssertEqual((int)frame.State, (int)FrameState.WaitingForInput, "初始State = WaitingForInput");

            // 设置输入 - 需要为所有玩家设置InputFlags才能使IsComplete()返回true
            var input = new FrameInput(5, 2);
            // 为每个玩家设置InputFlags，使输入被视为完整
            input.PlayerInputs[0].InputFlags = InputType.Movement;
            input.PlayerInputs[1].InputFlags = InputType.Movement;
            frame.SetInput(input);
            AssertEqual((int)frame.State, (int)FrameState.Ready, "SetInput后State = Ready");
            AssertTrue(frame.CanExecute(), "CanExecute() = true");

            // 开始执行
            frame.BeginExecution();
            AssertEqual((int)frame.State, (int)FrameState.Executing, "BeginExecution后State = Executing");
            AssertTrue(frame.StartTimestamp > 0, "StartTimestamp > 0");

            // 结束执行
            frame.EndExecution();
            AssertEqual((int)frame.State, (int)FrameState.Completed, "EndExecution后State = Completed");
            AssertTrue(frame.EndTimestamp >= frame.StartTimestamp, "EndTimestamp >= StartTimestamp");

            // 重置
            frame.Reset();
            AssertEqual((int)frame.State, (int)FrameState.WaitingForInput, "Reset后State = WaitingForInput");
        }

        [ContextMenu("Test LockstepConfig")]
        public void TestLockstepConfig()
        {
            Debug.Log("\n--- LockstepConfig 测试 ---");

            var config = LockstepConfig.Default;
            AssertEqual(config.LogicFrameRate, 30, "Default LogicFrameRate = 30");
            AssertNearlyEqual(config.LogicFrameIntervalSeconds, 1f / 30f, "LogicFrameIntervalSeconds");

            var highPerf = LockstepConfig.HighPerformance;
            AssertEqual(highPerf.LogicFrameRate, 60, "HighPerformance LogicFrameRate = 60");

            var lowLatency = LockstepConfig.LowLatency;
            AssertEqual(lowLatency.InputBufferFrames, 1, "LowLatency InputBufferFrames = 1");
        }

        #endregion

        #region Snapshot Tests

        [ContextMenu("Test EntitySnapshot")]
        public void TestEntitySnapshot()
        {
            Debug.Log("\n--- EntitySnapshot 测试 ---");

            var snapshot = new EntitySnapshot
            {
                EntityId = 1,
                EntityType = (int)EntityType.Hero,
                Position = FixedVector3.FromFloat(10f, 0f, 20f),
                Rotation = Fixed64.FromFloat(1.5f),
                CurrentHealth = 800,
                MaxHealth = 1000,
                IsAlive = true,
                IsActive = true,
                OwnerId = 0
            };

            AssertEqual(snapshot.EntityId, 1, "EntityId = 1");
            AssertEqual(snapshot.CurrentHealth, 800, "CurrentHealth = 800");
            AssertTrue(snapshot.IsAlive, "IsAlive = true");

            // 哈希测试
            long hash1 = snapshot.GetHash();
            long hash2 = snapshot.GetHash();
            AssertEqual(hash1, hash2, "相同快照哈希相等");

            // 修改后哈希不同
            snapshot.CurrentHealth = 700;
            long hash3 = snapshot.GetHash();
            AssertTrue(hash1 != hash3, "修改后哈希不同");
        }

        [ContextMenu("Test GameSnapshot")]
        public void TestGameSnapshot()
        {
            Debug.Log("\n--- GameSnapshot 测试 ---");

            var snapshot = new GameSnapshot(100);
            AssertEqual(snapshot.FrameNumber, 100, "FrameNumber = 100");
            AssertTrue(snapshot.Timestamp > 0, "Timestamp > 0");
            AssertEqual(snapshot.EntitySnapshots.Count, 0, "初始EntitySnapshots为空");

            // 添加实体快照
            var entitySnapshot1 = new EntitySnapshot { EntityId = 1, CurrentHealth = 1000 };
            var entitySnapshot2 = new EntitySnapshot { EntityId = 2, CurrentHealth = 500 };
            snapshot.AddEntitySnapshot(entitySnapshot1);
            snapshot.AddEntitySnapshot(entitySnapshot2);

            AssertEqual(snapshot.EntitySnapshots.Count, 2, "添加后EntitySnapshots.Count = 2");

            // 获取实体快照
            var retrieved = snapshot.GetEntitySnapshot(1);
            AssertTrue(retrieved.HasValue, "GetEntitySnapshot(1) != null");
            AssertEqual(retrieved.Value.CurrentHealth, 1000, "GetEntitySnapshot(1).CurrentHealth = 1000");

            // 计算哈希
            snapshot.CalculateHash();
            AssertTrue(snapshot.StateHash != 0, "CalculateHash后StateHash != 0");

            // 克隆
            var clone = snapshot.Clone();
            AssertEqual(clone.FrameNumber, snapshot.FrameNumber, "Clone.FrameNumber");
            AssertEqual(clone.EntitySnapshots.Count, snapshot.EntitySnapshots.Count, "Clone.EntitySnapshots.Count");
        }

        [ContextMenu("Test SnapshotManager")]
        public void TestSnapshotManager()
        {
            Debug.Log("\n--- SnapshotManager 测试 ---");

            var manager = new SnapshotManager(10, 5);
            AssertEqual(manager.SnapshotCount, 0, "初始SnapshotCount = 0");

            // 检查是否应该保存快照
            AssertTrue(manager.ShouldSaveSnapshot(0), "ShouldSaveSnapshot(0)");
            AssertTrue(manager.ShouldSaveSnapshot(5), "ShouldSaveSnapshot(5)");
            AssertTrue(!manager.ShouldSaveSnapshot(3), "!ShouldSaveSnapshot(3)");

            // 保存快照
            for (int i = 0; i <= 50; i += 5)
            {
                var snapshot = new GameSnapshot(i);
                manager.SaveSnapshot(snapshot);
            }

            AssertEqual(manager.SnapshotCount, 10, "保存11个快照后Count = 10（最大限制）");

            // 获取快照
            var retrieved = manager.GetSnapshot(25);
            AssertTrue(retrieved != null, "GetSnapshot(25) != null");
            AssertEqual(retrieved.FrameNumber, 25, "GetSnapshot(25).FrameNumber = 25");

            // 获取最近快照
            var nearest = manager.GetNearestSnapshot(27);
            AssertTrue(nearest != null, "GetNearestSnapshot(27) != null");
            AssertEqual(nearest.FrameNumber, 25, "GetNearestSnapshot(27).FrameNumber = 25");

            // 删除之后的快照
            manager.RemoveSnapshotsAfter(30);
            var after35 = manager.GetSnapshot(35);
            AssertTrue(after35 == null, "RemoveSnapshotsAfter(30)后GetSnapshot(35) = null");

            // 清空
            manager.Clear();
            AssertEqual(manager.SnapshotCount, 0, "Clear后SnapshotCount = 0");
        }

        #endregion

        #region DelaySimulator Tests

        [ContextMenu("Test DelayConfig")]
        public void TestDelayConfig()
        {
            Debug.Log("\n--- DelayConfig 测试 ---");

            var noDelay = DelayConfig.NoDelay;
            AssertEqual(noDelay.BaseDelay, 0, "NoDelay.BaseDelay = 0");
            AssertTrue(!noDelay.Enabled, "NoDelay.Enabled = false");

            var low = DelayConfig.LowLatency;
            AssertEqual(low.BaseDelay, 50, "LowLatency.BaseDelay = 50");

            var medium = DelayConfig.MediumLatency;
            AssertEqual(medium.BaseDelay, 100, "MediumLatency.BaseDelay = 100");

            var high = DelayConfig.HighLatency;
            AssertEqual(high.BaseDelay, 200, "HighLatency.BaseDelay = 200");

            var extreme = DelayConfig.ExtremeLatency;
            AssertEqual(extreme.BaseDelay, 500, "ExtremeLatency.BaseDelay = 500");
        }

        [ContextMenu("Test DelaySimulator")]
        public void TestDelaySimulator()
        {
            Debug.Log("\n--- DelaySimulator 测试 ---");

            // 无延迟测试
            var noDelayConfig = DelayConfig.NoDelay;
            var noDelaySim = new DelaySimulator<int>(noDelayConfig, 12345);

            noDelaySim.Send(100);
            AssertEqual(noDelaySim.PacketsSent, 1, "NoDelay发送后PacketsSent = 1");

            // 有延迟测试
            var delayConfig = new DelayConfig
            {
                BaseDelay = 0, // 0延迟以便立即接收
                Jitter = 0,
                PacketLossRate = 0f,
                Enabled = true
            };
            var delaySim = new DelaySimulator<string>(delayConfig, 54321);

            delaySim.Send("test1");
            delaySim.Send("test2");
            delaySim.Send("test3");

            AssertEqual(delaySim.PacketsSent, 3, "发送3个包后PacketsSent = 3");
            AssertEqual(delaySim.QueuedPackets, 3, "QueuedPackets = 3");

            // 接收（0延迟应该立即可接收）
            var received = delaySim.Receive();
            AssertEqual(received.Count, 3, "接收到3个包");
            AssertEqual(delaySim.PacketsReceived, 3, "PacketsReceived = 3");
            AssertEqual(delaySim.QueuedPackets, 0, "接收后QueuedPackets = 0");

            // 丢包测试
            var lossConfig = new DelayConfig
            {
                BaseDelay = 0,
                Jitter = 0,
                PacketLossRate = 1.0f, // 100%丢包
                Enabled = true
            };
            var lossSim = new DelaySimulator<int>(lossConfig, 99999);

            bool sent = lossSim.Send(1);
            AssertTrue(!sent, "100%丢包率下Send返回false");
            AssertEqual(lossSim.PacketsLost, 1, "PacketsLost = 1");

            // 清空和重置
            delaySim.Clear();
            AssertEqual(delaySim.QueuedPackets, 0, "Clear后QueuedPackets = 0");

            delaySim.ResetStats();
            AssertEqual(delaySim.PacketsSent, 0, "ResetStats后PacketsSent = 0");
        }

        #endregion

        #region LocalServer Tests

        [ContextMenu("Test LocalServer")]
        public void TestLocalServer()
        {
            Debug.Log("\n--- LocalServer 测试 ---");

            var server = new LocalServer(2, DelayConfig.NoDelay);
            AssertEqual((int)server.State, (int)ServerState.Stopped, "初始State = Stopped");
            AssertEqual(server.PlayerCount, 2, "PlayerCount = 2");

            // 启动服务器
            server.Start();
            AssertEqual((int)server.State, (int)ServerState.WaitingForPlayers, "Start后State = WaitingForPlayers");

            // 玩家连接
            server.ConnectPlayer(0);
            server.ConnectPlayer(1);
            AssertEqual(server.GetConnectedPlayerCount(), 2, "2个玩家连接后Count = 2");

            // 玩家准备
            server.SetPlayerReady(0, true);
            AssertEqual((int)server.State, (int)ServerState.WaitingForPlayers, "1个玩家准备后仍在等待");

            server.SetPlayerReady(1, true);
            AssertEqual((int)server.State, (int)ServerState.Running, "所有玩家准备后State = Running");

            // 接收输入
            var playerInput = new PlayerInput { PlayerId = 0 };
            playerInput.Actions.Add(new PlayerAction { Type = ActionType.Move });
            playerInput.InputFlags = InputType.Movement;
            server.ReceivePlayerInput(0, 0, playerInput);

            // 推进帧
            server.AdvanceFrame();
            AssertEqual(server.CurrentFrame, 1, "AdvanceFrame后CurrentFrame = 1");

            // 暂停/恢复
            server.Pause();
            AssertEqual((int)server.State, (int)ServerState.Paused, "Pause后State = Paused");

            server.Resume();
            AssertEqual((int)server.State, (int)ServerState.Running, "Resume后State = Running");

            // 玩家断开
            server.DisconnectPlayer(1);
            var player1 = server.GetPlayerConnection(1);
            AssertTrue(!player1.IsConnected, "DisconnectPlayer后IsConnected = false");

            // 停止服务器
            server.Stop();
            AssertEqual((int)server.State, (int)ServerState.Stopped, "Stop后State = Stopped");
        }

        #endregion

        #region BaseEntity Tests

        [ContextMenu("Test BaseEntity Creation")]
        public void TestBaseEntityCreation()
        {
            Debug.Log("\n--- BaseEntity 创建测试 ---");

            // 重置ID计数器
            BaseEntity.ResetIdCounter();

            var entity1 = new BaseEntity();
            var entity2 = new BaseEntity();

            AssertEqual(entity1.EntityId, 1, "第一个实体ID = 1");
            AssertEqual(entity2.EntityId, 2, "第二个实体ID = 2");

            // 默认状态
            AssertTrue(entity1.IsActive, "默认IsActive = true");
            AssertTrue(entity1.IsAlive, "默认IsAlive = true");
            AssertTrue(!entity1.IsDestroyed, "默认IsDestroyed = false");

            // 默认属性
            AssertEqual(entity1.Stats.MaxHealth, 1000, "默认MaxHealth = 1000");
            AssertEqual(entity1.Stats.CurrentHealth, 1000, "默认CurrentHealth = 1000");

            // 初始化
            entity1.Initialize(0);
            AssertEqual(entity1.CreatedFrame, 0, "Initialize后CreatedFrame = 0");
        }

        [ContextMenu("Test BaseEntity Movement")]
        public void TestBaseEntityMovement()
        {
            Debug.Log("\n--- BaseEntity 移动测试 ---");

            BaseEntity.ResetIdCounter();
            var entity = new BaseEntity();
            entity.Initialize(0);

            // 设置移动速度
            entity.Stats.MoveSpeed = Fixed64.FromFloat(5f);

            // 移动到目标
            var target = FixedVector3.FromFloat(10f, 0f, 0f);
            entity.MoveTo(target);

            AssertTrue(entity.IsMoving, "MoveTo后IsMoving = true");
            AssertTrue(entity.TargetPosition == target, "TargetPosition已设置");

            // 模拟逻辑更新
            var deltaTime = Fixed64.FromFloat(1f / 30f);
            entity.LogicUpdate(1, deltaTime);

            AssertTrue(entity.Position.X > Fixed64.Zero, "更新后Position.X > 0");
            AssertTrue(entity.AliveFrames == 1, "AliveFrames = 1");

            // 停止移动
            entity.StopMovement();
            AssertTrue(!entity.IsMoving, "StopMovement后IsMoving = false");

            // 瞬移
            var newPos = FixedVector3.FromFloat(100f, 0f, 100f);
            entity.SetPosition(newPos);
            AssertTrue(entity.Position == newPos, "SetPosition后Position已更新");

            // 状态影响移动
            entity.AddState(EntityState.Stunned);
            AssertTrue(!entity.CanMove, "Stunned状态下CanMove = false");

            entity.RemoveState(EntityState.Stunned);
            entity.AddState(EntityState.Rooted);
            AssertTrue(!entity.CanMove, "Rooted状态下CanMove = false");
        }

        [ContextMenu("Test BaseEntity Combat")]
        public void TestBaseEntityCombat()
        {
            Debug.Log("\n--- BaseEntity 战斗测试 ---");

            BaseEntity.ResetIdCounter();
            var entity = new BaseEntity();
            entity.Initialize(0);
            entity.Stats.MaxHealth = 1000;
            entity.Stats.CurrentHealth = 1000;

            // 受到伤害
            entity.TakeDamage(300);
            AssertEqual(entity.Stats.CurrentHealth, 700, "TakeDamage(300)后CurrentHealth = 700");
            AssertTrue(entity.IsAlive, "受伤后仍存活");

            // 治疗
            entity.Heal(200);
            AssertEqual(entity.Stats.CurrentHealth, 900, "Heal(200)后CurrentHealth = 900");

            // 过量治疗
            entity.Heal(500);
            AssertEqual(entity.Stats.CurrentHealth, 1000, "过量治疗后CurrentHealth = MaxHealth");

            // 无敌状态
            entity.AddState(EntityState.Invincible);
            entity.TakeDamage(500);
            AssertEqual(entity.Stats.CurrentHealth, 1000, "无敌状态下不受伤害");
            entity.RemoveState(EntityState.Invincible);

            // 致死伤害
            bool deathEventFired = false;
            entity.OnDeath += (e, killer) => deathEventFired = true;
            entity.TakeDamage(1500);
            AssertEqual(entity.Stats.CurrentHealth, 0, "致死伤害后CurrentHealth = 0");
            AssertTrue(!entity.IsAlive, "致死伤害后IsAlive = false");
            AssertTrue(deathEventFired, "死亡事件已触发");

            // 复活
            entity.Revive(50);
            AssertTrue(entity.IsAlive, "Revive后IsAlive = true");
            AssertEqual(entity.Stats.CurrentHealth, 500, "Revive(50%)后CurrentHealth = 500");
        }

        [ContextMenu("Test BaseEntity Snapshot")]
        public void TestBaseEntitySnapshot()
        {
            Debug.Log("\n--- BaseEntity 快照测试 ---");

            BaseEntity.ResetIdCounter();
            var entity = new BaseEntity();
            entity.Initialize(0);
            entity.Position = FixedVector3.FromFloat(5f, 0f, 10f);
            entity.Rotation = Fixed64.FromFloat(1.5f);
            entity.Stats.CurrentHealth = 750;
            entity.OwnerId = 1;

            // 创建快照
            var snapshot = new EntitySnapshot
            {
                EntityId = entity.EntityId,
                EntityType = (int)entity.Type,
                Position = entity.Position,
                Rotation = entity.Rotation,
                Velocity = entity.Velocity,
                CurrentHealth = entity.Stats.CurrentHealth,
                MaxHealth = entity.Stats.MaxHealth,
                IsAlive = entity.IsAlive,
                IsActive = entity.IsActive,
                OwnerId = entity.OwnerId
            };
            AssertEqual(snapshot.EntityId, entity.EntityId, "Snapshot.EntityId");
            AssertEqual(snapshot.CurrentHealth, 750, "Snapshot.CurrentHealth = 750");
            AssertTrue(snapshot.Position == entity.Position, "Snapshot.Position");

            // 修改实体
            entity.Position = FixedVector3.FromFloat(100f, 0f, 100f);
            entity.Stats.CurrentHealth = 500;

            // 从快照恢复
            entity.Position = snapshot.Position;
            entity.Stats.CurrentHealth = snapshot.CurrentHealth;
            AssertNearlyEqual(entity.Position.X.ToFloat(), 5f, "恢复后Position.X = 5");
            AssertEqual(entity.Stats.CurrentHealth, 750, "恢复后CurrentHealth = 750");
        }

        #endregion

        #region LockstepManager Tests

        [ContextMenu("Test LockstepManager")]
        public void TestLockstepManager()
        {
            Debug.Log("\n--- LockstepManager 测试 ---");

            var config = LockstepConfig.Default;
            var manager = new LockstepManager(config);

            // 初始状态
            AssertTrue(!manager.IsRunning, "初始IsRunning = false");
            AssertEqual(manager.CurrentFrame, 0, "初始CurrentFrame = 0");

            // 启动
            manager.Start();
            AssertTrue(manager.IsRunning, "Start后IsRunning = true");

            // 逻辑帧间隔
            AssertNearlyEqual(manager.LogicDeltaTime.ToFloat(), 1f / config.LogicFrameRate, "LogicDeltaTime ≈ 1/30", 0.01f);

            // 提交输入
            var playerInput = new PlayerInput { PlayerId = 0 };
            playerInput.Actions.Add(new PlayerAction { Type = ActionType.Skill, SkillSlot = 1 });
            manager.SubmitInput(config.InputBufferFrames, playerInput);

            // 检查输入缓冲
            var bufferedInput = manager.InputBuffer.GetInput(config.InputBufferFrames);
            AssertTrue(bufferedInput != null, "输入已缓冲");
            AssertTrue(bufferedInput.PlayerInputs[0].HasActions, "缓冲的输入有Action");

            // 模拟执行
            int logicRanCount = 0;
            manager.OnLogicUpdate += (frame, dt, input) => {
                logicRanCount++;
                if (frame == config.InputBufferFrames)
                {
                    AssertTrue(input.PlayerInputs[0].HasActions, $"Frame {frame} 有输入");
                    AssertEqual((int)input.PlayerInputs[0].Actions[0].Type, (int)ActionType.Skill, "输入Action为Skill");
                }
            };

            // 推进足够的时间来执行帧
            for(int i = 0; i < config.InputBufferFrames + 5; i++)
            {
                manager.Update(1f / config.LogicFrameRate);
            }

            AssertTrue(logicRanCount > 0, "逻辑更新已执行");

            // 停止
            manager.Stop();
            AssertTrue(!manager.IsRunning, "Stop后IsRunning = false");
        }

        #endregion

        #region Assert Helpers

        private void AssertTrue(bool condition, string testName)
        {
            _totalTests++;
            if (condition)
            {
                _passedTests++;
                Debug.Log($"<color=green>✓ PASS:</color> {testName}");
            }
            else
            {
                _failedTests++;
                Debug.LogError($"<color=red>✗ FAIL:</color> {testName}");
            }
        }

        private void AssertEqual<T>(T actual, T expected, string testName) where T : IEquatable<T>
        {
            _totalTests++;
            if (actual.Equals(expected))
            {
                _passedTests++;
                Debug.Log($"<color=green>✓ PASS:</color> {testName} (值: {actual})");
            }
            else
            {
                _failedTests++;
                Debug.LogError($"<color=red>✗ FAIL:</color> {testName} - 期望: {expected}, 实际: {actual}");
            }
        }

        private void AssertEqual(int actual, int expected, string testName)
        {
            _totalTests++;
            if (actual == expected)
            {
                _passedTests++;
                Debug.Log($"<color=green>✓ PASS:</color> {testName} (值: {actual})");
            }
            else
            {
                _failedTests++;
                Debug.LogError($"<color=red>✗ FAIL:</color> {testName} - 期望: {expected}, 实际: {actual}");
            }
        }

        private void AssertEqual(long actual, long expected, string testName)
        {
            _totalTests++;
            if (actual == expected)
            {
                _passedTests++;
                Debug.Log($"<color=green>✓ PASS:</color> {testName} (值: {actual})");
            }
            else
            {
                _failedTests++;
                Debug.LogError($"<color=red>✗ FAIL:</color> {testName} - 期望: {expected}, 实际: {actual}");
            }
        }

        private void AssertNearlyEqual(float actual, float expected, string testName, float tolerance = 0.001f)
        {
            _totalTests++;
            if (Mathf.Abs(actual - expected) <= tolerance)
            {
                _passedTests++;
                Debug.Log($"<color=green>✓ PASS:</color> {testName} (值: {actual:F6})");
            }
            else
            {
                _failedTests++;
                Debug.LogError($"<color=red>✗ FAIL:</color> {testName} - 期望: {expected:F6}, 实际: {actual:F6}");
            }
        }

        #endregion
    }
}