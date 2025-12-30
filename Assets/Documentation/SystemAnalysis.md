# MOBA核心战斗架构 - 系统分析文档

本文档基于 `Architecture.md` 的设计蓝图和对项目源代码的实际审查，旨在提供一份清晰、准确的系统分析，包括命名规范、核心类的职责划分以及功能在不同架构层中的分布。

---

## 1. 命名规范 (Naming Conventions)

代码库遵循了一套清晰且一致的命名规范，极大地提高了可读性和可维护性。

| 类别 | 规范 | 示例 | 职责说明 |
| :--- | :--- | :--- | :--- |
| **确定性数学库** | `Fixed*` 前缀 | `Fixed64`, `FixedVector3`, `FixedMath` | 所有与确定性计算相关的数学类型和函数。 |
| **逻辑层实体** | `*Entity` 后缀 | `BaseEntity`, `HeroEntity`, `ProjectileEntity` | 代表游戏世界中的逻辑对象，只包含数据和确定性逻辑，无任何表现层依赖。 |
| **表现层视图** | `*View` 后缀 | `EntityView`, `HeroView`, `ProjectileView` | Unity `MonoBehaviour`，负责将对应的 `Entity` 数据渲染到屏幕上，处理视觉插值、动画和特效。 |
| **技能/Buff** | `Skill_*`, `Buff_*` | `Skill_Fireball`, `Buff_Stun` | 具体的技能逻辑或Buff效果实现，通常作为独立的策略类。 |
| **配置数据** | `*Data` 后缀 | `HeroData`, `SkillData` | `ScriptableObject`，用于在Unity编辑器中配置游戏数据，实现数据驱动。 |
| **核心管理器** | `*Manager` 后缀 | `LockstepManager`, `SnapshotManager` | 负责管理特定子系统的生命周期和核心逻辑。 |
| **接口** | `I*` 前缀 | `ISkillLogic`, `IDamageable` | 定义模块间的契约和行为规范。 |
| **静态工具类** | `*Detector`, `*Calculator` | `CollisionDetector`, `DamageCalculator` | 提供无状态的、纯函数的计算服务。 |

---

## 2. 核心类职责 (Core Class Responsibilities)

项目中的核心类严格遵循单一职责原则，每个类都有明确且独立的任务。

### Core Layer (核心层)

| 类名 | 核心职责 | 禁止做什么 |
| :--- | :--- | :--- |
| **`Fixed64`** | 提供确定性的64位定点数运算。 | 依赖任何非 `System` 的库；产生不确定性结果。 |
| **`LockstepManager`** | **驱动逻辑帧循环**。根据固定的时间间隔 (`LogicDeltaTime`)，调用 `OnLogicUpdate` 委托，推进游戏世界状态。 | **处理具体游戏逻辑** (如移动、施法)；直接依赖 `UnityEngine`。 |
| **`InputBuffer`** | **缓冲和管理输入**。按帧号存储来自所有玩家的输入，并确保在正确的逻辑帧提供给 `LockstepManager`。 | 决定输入是否有效；修改输入内容。 |
| **`SnapshotManager`** | **管理游戏状态快照**。在关键帧（KeyFrame）保存和恢复整个游戏世界的状态 (`GameSnapshot`)，用于网络回滚。 | 创建快照的具体内容（由外部委托实现）；决定何时回滚。 |
| **`CollisionDetector`** | 提供**确定性的几何碰撞检测算法**（如圆与圆、圆与AABB）。所有函数都是纯静态函数。 | 维护任何碰撞状态；处理碰撞后的响应（如造成伤害）。 |
| **`SpatialHash`** | **优化碰撞检测**。通过将实体注册到空间网格中，减少不必要的碰撞对检测，降低算法复杂度。 | 执行具体的碰撞检测算法；存储实体对象本身。 |

### Gameplay Layer (游戏逻辑层)

| 类名 | 核心职责 | 禁止做什么 |
| :--- | :--- | :--- |
| **`BaseEntity`** | **存储逻辑状态**。包含一个游戏单位的所有核心数据，如ID、位置、朝向、生命值等。 | 包含任何与 `UnityEngine` 相关的表现层代码；直接渲染自己。 |
| **`SkillExecutor`** | **管理单个实体的技能**。持有该实体的所有技能，并协调 `SkillStateMachine` 处理技能的释放、冷却。 | 实现具体的技能效果（由 `ISkillLogic` 策略实现）。 |
| **`SkillStateMachine`** | **管理技能的生命周期**。控制技能从 `Idle` -> `Casting` -> `Executing` -> `Recovery` -> `Cooldown` 的状态流转。 | 实现技能的具体效果；处理多个技能的调度。 |
| **`ISkillLogic`** | **定义技能效果的契约**。这是一个策略接口，具体的技能类（如 `Skill_Fireball`）实现它来定义技能在 `Executing` 状态下做什么。 | 管理技能状态（由 `SkillStateMachine` 负责）。 |
| **`DamageCalculator`** | **计算最终伤害**。根据攻击方、防御方和技能信息，计算出确定性的伤害值。 | 应用伤害（由 `IDamageable` 接口处理）；触发伤害事件。 |

### Presentation Layer (表现层)

| 类名 | 核心职责 | 禁止做什么 |
| :--- | :--- | :--- |
| **`EntityView`** | **渲染逻辑实体**。在 `Update` 中平滑插值 `transform.position`，使其追赶逻辑层 `BaseEntity.Position` 的位置，实现流畅的视觉效果。 | **直接修改逻辑数据** (`BaseEntity`)；执行任何确定性计算。 |
| **`InputCollector`** | **收集并提交玩家输入**。监听键盘和鼠标事件，将其转换为 `PlayerAction` 数据结构，并提交给 `LockstepManager`。 | **直接执行输入**（所有输入必须通过帧同步框架）。 |
| **`VFXPlayer`** | **播放视觉特效**。监听战斗事件（如伤害、技能释放），在指定位置播放粒子特效或声音。 | 包含任何游戏逻辑。 |

---

## 3. 功能分布 (Function Distribution)

项目的功能严格按照 `表现层 -> 逻辑层 -> 核心层` 的单向依赖关系进行分布。数据流和控制流清晰明了。

```
┌──────────────────────────────────────────────────┐
│              Presentation Layer (表现层)             │
│ - 职责: 收集输入、渲染状态、播放视听效果           │
│ - 示例: InputCollector.cs, EntityView.cs, VFXPlayer.cs │
│ - 特点: 依赖 UnityEngine，每帧 (60fps) 更新         │
└──────────────────────────────────────────────────┘
                           |
                           | Input (PlayerAction)
                           ↓
┌──────────────────────────────────────────────────┐
│                Gameplay Layer (逻辑层)               │
│ - 职责: 实现具体游戏玩法，如技能效果、伤害计算     │
│ - 示例: Skill_Fireball.cs, DamageCalculator.cs, HeroEntity.cs │
│ - 特点: 纯C#，无 UnityEngine 依赖，由 Lockstep 驱动 │
└──────────────────────────────────────────────────┘
                           |
                           | Logic Tick (OnLogicUpdate)
                           ↓
┌──────────────────────────────────────────────────┐
│                   Core Layer (核心层)                  │
│ - 职责: 提供确定性环境，驱动游戏世界               │
│ - 示例: LockstepManager.cs, Fixed64.cs, CollisionDetector.cs │
│ - 特点: 框架基础，完全独立，可移植                 │
└──────────────────────────────────────────────────┘
```

### 一个“移动”指令的完整生命周期：

1.  **表现层 (`InputCollector.cs`)**
    *   在 `Update()` 中检测到鼠标右键点击。
    *   通过 `Camera.ScreenPointToRay` 获取地面点击的 `UnityEngine.Vector3` 坐标。
    *   创建一个 `PlayerAction` 对象，**将 `Vector3` 坐标存入 `TargetPosition_Unity` 字段**。
    *   将此 `PlayerAction` 提交给 `LockstepManager`。

2.  **核心层 (`LockstepManager.cs`)**
    *   将 `PlayerAction` 存入 `InputBuffer`，等待目标逻辑帧。
    *   在 `ExecuteLogicFrame` 中，从 `InputBuffer` 取出该帧的输入。
    *   **(当前错误的实现)** 在 `PreProcessInput` 中，将 `TargetPosition_Unity` (Vector3) 转换为 `TargetPosition` (FixedVector3)。
    *   调用 `OnLogicUpdate` 委托，并将包含 `FixedVector3` 坐标的输入传递下去。

3.  **游戏逻辑层 (`HeroEntity.cs`)**
    *   在 `OnLogicUpdate` 的实现中，接收到 `Move` 指令。
    *   读取 `action.TargetPosition` (FixedVector3)，将其设为移动目标。
    *   在后续的逻辑帧中，根据当前位置和目标位置，更新自身的 `Position` (FixedVector3)。

4.  **表现层 (`HeroView.cs`)**
    *   `HeroView` 持有对应 `HeroEntity` 的引用。
    *   在 `Update()` 中，通过 `Vector3.Lerp`，平滑地将 `transform.position` 向 `HeroEntity.Position.ToVector3()` 插值移动。

这个流程清晰地展示了各层职责：表现层负责与Unity引擎交互，逻辑层负责确定性状态更新，核心层负责驱动整个流程。
