# MOBA Combat Core - Tech Demo

<p align="center">
  <strong>一个专注于展示帧同步与确定性战斗逻辑的技术验证原型</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3%20LTS-blue" alt="Unity Version">
  <img src="https://img.shields.io/badge/C%23-.NET%20Standard%202.1-green" alt="C# Version">
  <img src="https://img.shields.io/badge/License-MIT-yellow" alt="License">
</p>

---

## 项目概述

**MOBA Combat Core** 是一个技术演示项目，专注于展示MOBA游戏底层的核心技术实现能力。本项目不追求完整的游戏体验，而是通过最简化的视觉表现（几何体+线框可视化），深度展示：

- **帧同步（Lockstep）框架**：自研核心，支持200ms延迟模拟
- **确定性战斗逻辑**：全定点数计算，抛弃Unity物理引擎
- **高扩展技能系统**：配置化设计，3个技能覆盖MOBA所有机制
- **多线程优化**：Job System + Burst优化碰撞检测
- **可视化Debug**：Gizmos实时显示逻辑/表现分离状态

---

## 核心特性

### 技术亮点

| 特性 | 说明 |
|------|------|
| **定点数数学库** | 自研Fixed64，保证跨平台确定性 |
| **帧同步框架** | 仅同步输入，客户端本地模拟 |
| **自定义物理** | Circle/AABB碰撞，空间哈希优化 |
| **逻辑/表现分离** | 逻辑层纯C#，无Unity依赖 |
| **客户端预测** | 高延迟下保持流畅手感 |
| **对象池** | 消除高频GC，稳定帧率 |

### 性能指标

| 指标 | 目标 | 实际 |
|------|------|------|
| 逻辑帧耗时 | <3ms | **1.5ms** |
| 渲染帧率 | ≥60fps | **60fps** |
| 内存占用 | <150MB | **95MB** |
| GC分配/帧 | <1KB | **0.3KB** |
| 500弹道碰撞 | <2ms | **1.5ms** |

---

## 快速开始

### 环境要求

- Unity 2022.3 LTS 或更高版本
- .NET Standard 2.1
- Windows / macOS / Linux

### 安装步骤

1. 克隆仓库
```bash
git clone https://github.com/your-username/moba-combat-core.git
```

2. 使用Unity Hub打开项目

3. 打开主演示场景
```
Assets/_Project/00_Scenes/MainDemo.unity
```

4. 点击Play运行

### 操作说明

| 按键 | 功能 |
|------|------|
| **鼠标右键** | 移动到目标位置 |
| **Q** | 释放火球术（弹道型） |
| **W** | 释放闪现（位移型） |
| **E** | 释放暴风雪（AOE型） |
| **F3** | 切换Debug面板 |

---

## 项目架构

```
Assets/Scripts/
├── Core/                    # 核心框架（无Unity依赖）
│   ├── Math/               # 定点数数学库
│   ├── Lockstep/           # 帧同步核心
│   ├── Physics/            # 自定义物理
│   └── Network/            # 网络模拟层
│
├── Gameplay/                # 游戏逻辑层（纯数据）
│   ├── Entity/             # 实体系统
│   ├── Skill/              # 技能系统
│   └── Combat/             # 战斗系统
│
├── Presentation/            # 表现层（Unity依赖）
│   └── EntityView.cs       # 实体渲染器
│
├── Glue/                    # 胶水层（系统整合）
│   ├── GameManager.cs      # 游戏主控制器
│   ├── EntityManager.cs    # 实体管理器
│   └── ViewManager.cs      # 视图管理器
│
├── Optimization/            # 性能优化
│   ├── ObjectPool.cs       # 对象池
│   └── Jobs/               # Job System
│
└── UI/                      # 调试UI
    └── DebugPanel.cs       # Debug面板
```

### 分层架构

```
┌─────────────────────────────────────────────────────┐
│              表现层 (Presentation Layer)              │
│     Unity MonoBehaviour / 60fps渲染 / 插值平滑       │
└─────────────────────────────────────────────────────┘
                        ↑ 单向依赖（读取）
┌─────────────────────────────────────────────────────┐
│              胶水层 (Glue Layer)                      │
│     游戏初始化 / 系统协调 / 实体管理 / 视图绑定        │
└─────────────────────────────────────────────────────┘
                        ↑ 协调调度
┌─────────────────────────────────────────────────────┐
│               逻辑层 (Logic Layer)                    │
│      纯C#数据 / 15-30fps定帧 / 确定性计算            │
└─────────────────────────────────────────────────────┘
                        ↑ 输入驱动
┌─────────────────────────────────────────────────────┐
│              核心层 (Core Layer)                      │
│       帧同步 / 定点数 / 自定义物理 / 输入管理          │
└─────────────────────────────────────────────────────┘
```

---

## 技术详解

### 1. 定点数数学库

```csharp
// Fixed64 - 64位定点数
// 精度：百万分之一（6位小数）
// 范围：±9,223,372,036

Fixed64 a = Fixed64.FromFloat(3.14159f);
Fixed64 b = Fixed64.FromInt(2);
Fixed64 c = a * b;  // 确定性乘法

// 三角函数（查找表+插值）
Fixed64 sin = FixedMath.Sin(angle);
Fixed64 cos = FixedMath.Cos(angle);
```

### 2. 帧同步框架

```csharp
// 帧同步核心流程
LockstepManager.Update(deltaTime)
├── 累积时间
├── 检查是否需要执行逻辑帧
├── 获取当前帧输入
├── 执行确定性逻辑更新
├── 保存快照（每10帧）
└── 更新帧号
```

### 3. 技能系统

```csharp
// 技能状态机
Idle → Casting → Executing → Recovery → Cooldown → Idle

// 策略模式实现
public interface ISkillLogic
{
    void OnCastStart(SkillContext context);
    void OnExecute(SkillContext context);
    void OnCastEnd(SkillContext context);
}
```

---

## 运行测试

### 单元测试

1. 打开测试场景
```
Assets/_Project/00_Scenes/UnitTest.unity
```

2. 选择 `_TEST_RUNNER_` GameObject

3. 在Inspector中右键选择"运行所有测试"

### 压力测试

1. 打开压力测试场景
```
Assets/_Project/00_Scenes/StressTest.unity
```

2. 运行并观察性能面板

---

## 文档

- [架构设计文档](Assets/Documentation/Architecture.md)
- [性能测试报告](Assets/Documentation/PerformanceReport.md)
- [面试话术准备](Assets/Documentation/InterviewGuide.md)
- [系统分析报告](Assets/Documentation/SystemAnalysis.md)

---

## 开发计划

### 已完成

- [x] Day 1: 定点数数学库
- [x] Day 2: 帧同步核心与实体
- [x] Day 3: 自定义物理与战斗基础
- [x] Day 4: 技能系统核心
- [x] Day 5: 网络模拟与可视化
- [x] Day 5.5: 胶水层整合
- [x] Day 6: 性能优化
- [x] Day 7: 封装与交付

### 未来计划

- [ ] WebGL在线演示
- [ ] 战斗回放系统
- [ ] 更多技能类型
- [ ] AI对手

---

## 技术栈

| 技术领域 | 使用方案 | 选型理由 |
|----------|----------|----------|
| **数学库** | 自研Fixed64定点数 | 解决浮点数跨平台精度问题 |
| **同步框架** | Lockstep（帧同步） | 仅同步输入，适合MOBA场景 |
| **物理碰撞** | 自定义2D碰撞检测 | Unity PhysX非确定性 |
| **多线程** | Job System + Burst | 碰撞检测并行化 |
| **架构模式** | Logic & View分离 | 逻辑层可单元测试 |
| **资源管理** | 对象池 | 避免GC |
| **配置系统** | ScriptableObject | 策划可配置 |

---

## 许可证

MIT License

---

## 联系方式

如有问题或建议，欢迎提交Issue或Pull Request。

---

**项目名称**：MOBA Combat Core - Tech Demo
**开发周期**：7天
**代码规模**：约4000行
**技术深度**：⭐⭐⭐⭐⭐（专注底层）
