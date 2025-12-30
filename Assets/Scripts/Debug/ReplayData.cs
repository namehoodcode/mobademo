// ReplayData.cs - 战斗回放数据结构
// 用于序列化和反序列化

using System;
using System.Collections.Generic;
using MobaCombatCore.Core.Lockstep;
using MobaCombatCore.Core.Math;

namespace MobaCombatCore.DebugTools
{
    /// <summary>
    /// 初始实体数据
    /// </summary>
    [Serializable]
    public class InitialEntityData
    {
        public int EntityId;
        public int TeamId;
        public int OwnerId;
        public FixedVector3 Position;
        public string EntityType;
    }

    /// <summary>
    /// 战斗回放数据
    /// </summary>
    [Serializable]
    public class ReplayData
    {
        public int RandomSeed;
        public int PlayerCount;
        public LockstepConfig LockstepConfig;
        public List<InitialEntityData> InitialEntities = new List<InitialEntityData>();
        public List<FrameInput> FrameInputs = new List<FrameInput>();
    }
}
