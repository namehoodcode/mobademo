// GameManager.cs - 游戏主控制器
// 详细设计见 Architecture.md - Glue层架构
// 作为Unity场景的入口点，初始化所有系统并驱动主循环

using System.Collections.Generic;
using System.Diagnostics;
using MobaCombatCore.Core.Lockstep;
using MobaCombatCore.Core.Math;
using MobaCombatCore.Core.Physics;
using MobaCombatCore.DebugTools;
using MobaCombatCore.Gameplay.Combat;
using MobaCombatCore.Gameplay.Entity;
using MobaCombatCore.Gameplay.Skill;
using MobaCombatCore.Gameplay.Skill.SkillTypes;
using MobaCombatCore.Glue.Services;
using MobaCombatCore.Optimization.Jobs;
using MobaCombatCore.UI;
using UnityEngine;

namespace MobaCombatCore.Glue
{
    /// <summary>
    /// 游戏主控制器 - 场景入口点
    /// 负责初始化所有系统、驱动主循环、协调各模块交互
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private const string LOG_TAG = "GameManager";

        #region 配置

        [Header("帧同步配置")]
        [Tooltip("逻辑帧率")]
        public int logicFrameRate = 30;

        [Tooltip("模拟网络延迟（毫秒）")]
        public int simulatedDelayMs = 0;

        [Tooltip("启用客户端预测")]
        public bool enablePrediction = true;

        [Header("游戏配置")]
        [Tooltip("玩家数量")]
        public int playerCount = 1;

        [Tooltip("随机种子")]
        public int randomSeed = 12345;

        [Header("实体预制体")]
        public GameObject heroPrefab;
        public GameObject dummyPrefab;
        public GameObject projectilePrefab;

        [Header("技能配置")]
        public SkillData[] heroSkills;

        [Header("调试")]
        [Tooltip("启用Debug面板")]
        public bool enableDebugPanel = true;

        [Tooltip("启用Gizmos绘制")]
        public bool enableGizmos = true;
        
        [Tooltip("日志级别 (0=Debug, 1=Info, 2=Warning, 3=Error, 4=None)")]
        [Range(0, 4)]
        public int logLevel = 3; // 默认Warning级别，提高性能
        
        [Tooltip("启用详细调试日志（会严重影响性能）")]
        public bool enableVerboseLogging = false;
        
        [Header("性能优化")]
        [Tooltip("使用Job System进行碰撞检测（推荐开启）")]
        public bool useJobSystemCollision = true;
        
        [Tooltip("使用定点数进行碰撞检测（确定性，用于帧同步）")]
        public bool useDeterministicCollision = true;

        #endregion

        #region 私有字段

        private LockstepManager _lockstepManager;
        private GameWorld _gameWorld;
        private EntityManager _entityManager;
        private ViewManager _viewManager;
        private InputHandler _inputHandler;
        private DebugPanel _debugPanel;

        // 性能统计
        private Stopwatch _logicStopwatch;
        private int _collisionChecksThisFrame;
        private float _collisionTimeMs;
        
        // Job System 碰撞检测
        private CollisionJobSystem _collisionJobSystem;
        private List<CollisionHit> _collisionResults = new List<CollisionHit>();
        private List<BaseEntity> _nonProjectileEntities = new List<BaseEntity>();

        #endregion

        #region 公开属性

        /// <summary>
        /// 帧同步管理器
        /// </summary>
        public LockstepManager LockstepManager => _lockstepManager;

        /// <summary>
        /// 游戏世界
        /// </summary>
        public GameWorld GameWorld => _gameWorld;

        /// <summary>
        /// 实体管理器
        /// </summary>
        public EntityManager EntityManager => _entityManager;

        #endregion

        #region 生命周期

        private void Awake()
        {
            // 首先配置日志级别（在任何日志输出之前）
            ConfigureLogging();
            
            GameLog.Info(LOG_TAG, "Awake", "游戏管理器初始化开始");

            // 初始化性能计时器
            _logicStopwatch = new Stopwatch();

            // 创建帧同步配置
            var config = new LockstepConfig
            {
                LogicFrameRate = logicFrameRate,
                PlayerCount = playerCount,
                SimulatedDelayMs = simulatedDelayMs,
                EnablePrediction = enablePrediction,
                InputBufferFrames = 2,
                MaxRollbackFrames = 30,
                KeyFrameInterval = 10
            };

            // 创建核心管理器
            _lockstepManager = new LockstepManager(config);
            _gameWorld = new GameWorld(randomSeed);
            _entityManager = new EntityManager(_gameWorld);

            // 创建视图管理器
            var prefabs = new Dictionary<EntityType, GameObject>
            {
                { EntityType.Hero, heroPrefab },
                { EntityType.Dummy, dummyPrefab },
                { EntityType.Projectile, projectilePrefab }
            };
            _viewManager = new ViewManager(prefabs);

            // 创建输入处理器
            _inputHandler = new InputHandler(_lockstepManager);
            
            // 创建Job System碰撞检测系统
            _collisionJobSystem = new CollisionJobSystem(512, 64);
            _collisionJobSystem.UseDeterministicMode = useDeterministicCollision;

            // 注册事件
            _entityManager.OnEntityCreated += OnEntityCreated;
            _entityManager.OnEntityDestroyed += OnEntityDestroyed;
            _lockstepManager.OnLogicUpdate += OnLogicUpdate;

            GameLog.Info(LOG_TAG, "Awake", "游戏管理器初始化完成");
        }

        private void Start()
        {
            GameLog.Info(LOG_TAG, "Start", "游戏启动");

            // 初始化Debug面板
            InitializeDebugPanel();

            // 初始化Gizmos
            InitializeGizmos();

            // 生成初始实体
            SpawnInitialEntities();

            // 启动帧同步
            _lockstepManager.Start();

            GameLog.Info(LOG_TAG, "Start", "帧同步已启动");
        }

        private void Update()
        {
            // 收集并提交输入
            _inputHandler.CollectAndSubmitInput();

            // 驱动帧同步
            _lockstepManager.Update(Time.deltaTime);

            // 更新视图插值
            float alpha = _lockstepManager.GetInterpolationAlpha();
            _viewManager.UpdateAllViews(alpha);

            // 更新性能监控
            UpdatePerformanceMonitor();
        }

        private void OnDestroy()
        {
            GameLog.Info(LOG_TAG, "OnDestroy", "游戏管理器销毁");

            // 注销事件
            if (_entityManager != null)
            {
                _entityManager.OnEntityCreated -= OnEntityCreated;
                _entityManager.OnEntityDestroyed -= OnEntityDestroyed;
            }

            if (_lockstepManager != null)
            {
                _lockstepManager.OnLogicUpdate -= OnLogicUpdate;
                _lockstepManager.Stop();
            }

            // 清理资源
            _viewManager?.ClearAllViews();
            _entityManager?.ClearAllEntities();
            
            // 释放Job System资源
            _collisionJobSystem?.Dispose();
        }

        #endregion

        #region 初始化
        
        /// <summary>
        /// 配置日志系统
        /// </summary>
        private void ConfigureLogging()
        {
            // 设置日志级别
            GameLog.CurrentLogLevel = (LogLevel)logLevel;
            
            // 如果不启用详细日志，将级别设置为Warning以提高性能
            if (!enableVerboseLogging && GameLog.CurrentLogLevel < LogLevel.Warning)
            {
                GameLog.CurrentLogLevel = LogLevel.Warning;
            }
            
            // 在编辑器中输出日志配置信息（使用Debug.Log直接输出，避免被日志级别过滤）
            #if UNITY_EDITOR
            UnityEngine.Debug.Log($"[GameManager] 日志级别设置为: {GameLog.CurrentLogLevel} (enableVerboseLogging={enableVerboseLogging})");
            #endif
        }

        private void InitializeDebugPanel()
        {
            if (!enableDebugPanel) return;

            // 查找或创建DebugPanel
            _debugPanel = FindFirstObjectByType<DebugPanel>();
            if (_debugPanel == null)
            {
                var go = new GameObject("DebugPanel");
                _debugPanel = go.AddComponent<DebugPanel>();
            }

            // 传递引用
            _debugPanel.gameManager = this;
        }

        private void InitializeGizmos()
        {
            if (!enableGizmos) return;

            // 初始化GizmosDrawer
            var gizmosDrawer = GizmosDrawer.Instance;
            if (gizmosDrawer != null)
            {
                gizmosDrawer.enableGizmos = true;
            }
        }

        private void SpawnInitialEntities()
        {
            GameLog.Info(LOG_TAG, "SpawnInitialEntities", "生成初始实体");

            // 生成玩家英雄
            var hero = SpawnHero(0, new FixedVector3(Fixed64.Zero, Fixed64.Zero, Fixed64.Zero));
            
            // 订阅英雄的弹道创建事件
            SubscribeToHeroSkillEvents(hero);

            // 生成木桩
            SpawnDummy(new FixedVector3(Fixed64.FromInt(5), Fixed64.Zero, Fixed64.Zero));
            SpawnDummy(new FixedVector3(Fixed64.FromInt(-5), Fixed64.Zero, Fixed64.Zero));
            SpawnDummy(new FixedVector3(Fixed64.Zero, Fixed64.Zero, Fixed64.FromInt(5)));

            GameLog.Info(LOG_TAG, "SpawnInitialEntities",
                $"初始实体生成完成 - 总数:{_gameWorld.AllEntities.Count}");
        }
        
        /// <summary>
        /// 订阅英雄的技能事件
        /// </summary>
        private void SubscribeToHeroSkillEvents(HeroEntity hero)
        {
            if (hero?.SkillExecutor == null) return;
            
            // 订阅弹道创建事件，将弹道注册到游戏世界
            hero.SkillExecutor.OnProjectileCreated += OnHeroProjectileCreated;
            
            GameLog.Info(LOG_TAG, "SubscribeToHeroSkillEvents",
                $"已订阅英雄{hero.Name}的技能事件");
        }
        
        /// <summary>
        /// 处理英雄创建的弹道
        /// </summary>
        private void OnHeroProjectileCreated(ProjectileEntity projectile)
        {
            if (projectile == null)
            {
                GameLog.Warning(LOG_TAG, "OnHeroProjectileCreated", "弹道实体为空!");
                return;
            }
            
            GameLog.Info(LOG_TAG, "OnHeroProjectileCreated",
                $"注册弹道到游戏世界 - ID:{projectile.EntityId}, 施法者:{projectile.Caster?.Name ?? "null"}, " +
                $"位置:({projectile.Position.X.ToFloat():F2}, {projectile.Position.Z.ToFloat():F2}), " +
                $"方向:({projectile.Direction.X.ToFloat():F2}, {projectile.Direction.Z.ToFloat():F2})");
            
            // 将弹道注册到游戏世界
            _entityManager.RegisterEntity(projectile);
            
            // 触发实体创建事件（用于创建视图）
            // 注意：RegisterEntity不会触发OnEntityCreated，需要手动触发
            _viewManager.CreateViewForEntity(projectile);
            
            GameLog.Info(LOG_TAG, "OnHeroProjectileCreated",
                $"弹道注册完成 - 当前弹道数量:{_gameWorld.Projectiles.Count}");
        }

        #endregion

        #region 实体生成

        /// <summary>
        /// 生成英雄
        /// </summary>
        public HeroEntity SpawnHero(int ownerId, FixedVector3 position)
        {
            var hero = _entityManager.CreateEntity<HeroEntity>(position);
            hero.OwnerId = ownerId;
            hero.TeamId = ownerId; // 简化：玩家ID即队伍ID
            hero.Name = $"Hero_{ownerId}";

            // 配置技能
            if (heroSkills != null && heroSkills.Length > 0)
            {
                // 为英雄设置技能（使用SetSkill方法逐个设置）
                for (int i = 0; i < heroSkills.Length && i < 4; i++)
                {
                    if (heroSkills[i] != null)
                    {
                        // 创建对应的技能逻辑实例
                        ISkillLogic skillLogic = CreateSkillLogic(heroSkills[i]);
                        hero.SetSkill(i, heroSkills[i], skillLogic);
                    }
                }
            }

            GameLog.Info(LOG_TAG, "SpawnHero",
                $"生成英雄 - ID:{hero.EntityId}, OwnerId:{ownerId}, 位置:{position}");

            return hero;
        }

        /// <summary>
        /// 生成木桩
        /// </summary>
        public DummyEntity SpawnDummy(FixedVector3 position)
        {
            var dummy = _entityManager.CreateEntity<DummyEntity>(position);
            dummy.TeamId = 1; // 敌方队伍
            dummy.Name = $"Dummy_{dummy.EntityId}";

            GameLog.Info(LOG_TAG, "SpawnDummy",
                $"生成木桩 - ID:{dummy.EntityId}, 位置:{position}");

            return dummy;
        }

        /// <summary>
        /// 生成弹道
        /// </summary>
        public ProjectileEntity SpawnProjectile(BaseEntity caster, SkillData skillData, FixedVector3 direction)
        {
            var projectile = _entityManager.CreateProjectile(caster, skillData, direction);

            GameLog.Info(LOG_TAG, "SpawnProjectile",
                $"生成弹道 - ID:{projectile.EntityId}, 施法者:{caster.Name}, 方向:{direction}");

            return projectile;
        }

        /// <summary>
        /// 根据技能数据创建对应的技能逻辑
        /// </summary>
        private ISkillLogic CreateSkillLogic(SkillData skillData)
        {
            // 根据技能类型创建对应的逻辑实现
            switch (skillData.skillType)
            {
                case SkillType.Projectile:
                    return new Skill_Fireball();
                case SkillType.Blink:
                    return new Skill_Blink();
                case SkillType.AreaOfEffect:
                    return new Skill_Blizzard();
                default:
                    return new Skill_Fireball(); // 默认使用弹道技能逻辑
            }
        }

        #endregion

        #region 逻辑更新

        /// <summary>
        /// 逻辑帧更新回调
        /// </summary>
        private void OnLogicUpdate(int frameNumber, Fixed64 deltaTime, FrameInput input)
        {
            _logicStopwatch.Restart();

            // 更新游戏世界帧号
            _gameWorld.CurrentFrame = frameNumber;

            // 1. 处理输入
            _inputHandler.ProcessFrameInput(input, _gameWorld);

            // 2. 更新所有实体
            _entityManager.UpdateAllEntities(frameNumber, deltaTime);

            // 3. 碰撞检测
            RunCollisionDetection();

            // 4. 处理战斗事件
            ProcessCombatEvents();

            _logicStopwatch.Stop();

            // 更新Gizmos
            UpdateGizmos();
        }

        /// <summary>
        /// 执行碰撞检测
        /// </summary>
        private void RunCollisionDetection()
        {
            if (useJobSystemCollision)
            {
                RunCollisionDetectionWithJobs();
            }
            else
            {
                RunCollisionDetectionSimple();
            }
        }
        
        /// <summary>
        /// 使用Job System进行碰撞检测（高性能）
        /// 优化：使用for循环替代foreach避免迭代器GC分配
        /// </summary>
        private void RunCollisionDetectionWithJobs()
        {
            var collisionStopwatch = Stopwatch.StartNew();
            _collisionChecksThisFrame = 0;

            var projectiles = _gameWorld.Projectiles;
            var allEntities = _gameWorld.AllEntities;
            
            if (projectiles.Count == 0)
            {
                collisionStopwatch.Stop();
                _collisionTimeMs = (float)collisionStopwatch.Elapsed.TotalMilliseconds;
                return;
            }
            
            // 构建非弹道实体列表 - 使用for循环避免foreach迭代器分配
            _nonProjectileEntities.Clear();
            int entityCount = allEntities.Count;
            for (int i = 0; i < entityCount; i++)
            {
                var entity = allEntities[i];
                if (!(entity is ProjectileEntity) && !entity.IsDestroyed && !entity.IsDestroyPending)
                {
                    _nonProjectileEntities.Add(entity);
                }
            }
            
            if (_nonProjectileEntities.Count == 0)
            {
                collisionStopwatch.Stop();
                _collisionTimeMs = (float)collisionStopwatch.Elapsed.TotalMilliseconds;
                return;
            }
            
            // 使用Job System进行碰撞检测
            _collisionJobSystem.RunCollisionDetection(
                (IReadOnlyList<ProjectileEntity>)projectiles,
                _nonProjectileEntities,
                _collisionResults);
            
            _collisionChecksThisFrame = projectiles.Count * _nonProjectileEntities.Count;
            
            // 处理碰撞结果 - 使用for循环避免foreach迭代器分配
            int hitCount = _collisionResults.Count;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = _collisionResults[i];
                // 通过EntityId查找实体
                var projectile = _gameWorld.GetEntity(hit.ProjectileEntityId) as ProjectileEntity;
                var target = _gameWorld.GetEntity(hit.TargetEntityId);
                
                if (projectile != null && target != null &&
                    !projectile.IsDestroyed && !projectile.IsDestroyPending)
                {
                    GameLog.Info(LOG_TAG, "RunCollisionDetectionWithJobs",
                        $"碰撞检测成功! 弹道{projectile.EntityId} 命中 {target.Name}");
                    OnProjectileHit(projectile, target);
                }
            }

            collisionStopwatch.Stop();
            _collisionTimeMs = (float)collisionStopwatch.Elapsed.TotalMilliseconds;
        }
        
        /// <summary>
        /// 简单碰撞检测（单线程，用于对比或备选）
        /// </summary>
        private void RunCollisionDetectionSimple()
        {
            var collisionStopwatch = Stopwatch.StartNew();
            _collisionChecksThisFrame = 0;

            var projectiles = _gameWorld.Projectiles;
            var allEntities = _gameWorld.AllEntities;
            
            // 调试：输出弹道数量
            if (projectiles.Count > 0 && enableVerboseLogging)
            {
                GameLog.Debug(LOG_TAG, "RunCollisionDetectionSimple",
                    $"碰撞检测开始 - 弹道数量:{projectiles.Count}, 实体数量:{allEntities.Count}");
            }

            // 遍历所有弹道
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = projectiles[i];
                if (projectile.IsDestroyed || projectile.IsDestroyPending) continue;
                
                // 调试：输出弹道信息
                if (enableVerboseLogging)
                {
                    GameLog.Debug(LOG_TAG, "RunCollisionDetectionSimple",
                        $"检测弹道 - ID:{projectile.EntityId}, TeamId:{projectile.TeamId}, " +
                        $"位置:({projectile.Position.X.ToFloat():F2}, {projectile.Position.Z.ToFloat():F2}), " +
                        $"碰撞半径:{projectile.Stats.CollisionRadius.ToFloat():F2}");
                }

                // 检测与所有实体的碰撞
                foreach (var entity in allEntities)
                {
                    if (entity == projectile) continue;
                    if (entity.IsDestroyed || entity.IsDestroyPending) continue;
                    if (entity.TeamId == projectile.TeamId) continue; // 跳过友军
                    
                    // 跳过其他弹道
                    if (entity is ProjectileEntity) continue;

                    _collisionChecksThisFrame++;
                    
                    // 调试：输出目标信息
                    if (enableVerboseLogging)
                    {
                        var distance = FixedVector3.Distance2D(projectile.Position, entity.Position);
                        GameLog.Debug(LOG_TAG, "RunCollisionDetectionSimple",
                            $"检测目标 - {entity.Name}(ID:{entity.EntityId}), TeamId:{entity.TeamId}, " +
                            $"位置:({entity.Position.X.ToFloat():F2}, {entity.Position.Z.ToFloat():F2}), " +
                            $"碰撞半径:{entity.Stats.CollisionRadius.ToFloat():F2}, " +
                            $"距离:{distance.ToFloat():F2}");
                    }

                    // 圆形碰撞检测
                    var projectileCircle = new Circle(projectile.Position, projectile.Stats.CollisionRadius);
                    var entityCircle = new Circle(entity.Position, entity.Stats.CollisionRadius);

                    if (CollisionDetector.CircleVsCircle(projectileCircle, entityCircle))
                    {
                        // 碰撞发生
                        GameLog.Info(LOG_TAG, "RunCollisionDetectionSimple",
                            $"碰撞检测成功! 弹道{projectile.EntityId} 命中 {entity.Name}");
                        OnProjectileHit(projectile, entity);
                        break; // 弹道只能命中一个目标
                    }
                }
            }

            collisionStopwatch.Stop();
            _collisionTimeMs = (float)collisionStopwatch.Elapsed.TotalMilliseconds;
        }

        /// <summary>
        /// 弹道命中处理
        /// </summary>
        private void OnProjectileHit(ProjectileEntity projectile, BaseEntity target)
        {
            GameLog.Info(LOG_TAG, "OnProjectileHit",
                $"弹道命中 - 弹道ID:{projectile.EntityId}, 目标:{target.Name}(ID:{target.EntityId})");

            // 计算伤害
            var damageInfo = new DamageInfo
            {
                Source = projectile.Caster,
                Target = target,
                BaseDamage = projectile.Damage,
                Type = MobaCombatCore.Gameplay.Combat.DamageType.Magical
            };
            int finalDamage = DamageCalculator.CalculateDamage(damageInfo);

            // 应用伤害
            target.TakeDamage(finalDamage, projectile.Caster);

            // 发布战斗事件
            CombatEvent.TriggerDamageDealt(projectile.Caster, target, finalDamage, MobaCombatCore.Gameplay.Combat.DamageType.Magical);

            // 销毁弹道
            _entityManager.DestroyEntity(projectile);
        }

        /// <summary>
        /// 处理战斗事件
        /// 优化：使用for循环替代foreach避免迭代器GC分配
        /// </summary>
        private void ProcessCombatEvents()
        {
            // 处理死亡事件 - 使用for循环避免foreach迭代器分配
            var allEntities = _gameWorld.AllEntities;
            int count = allEntities.Count;
            for (int i = 0; i < count; i++)
            {
                var entity = allEntities[i];
                if (entity.Stats.CurrentHealth <= 0 && !entity.IsDestroyed && !entity.IsDestroyPending)
                {
                    GameLog.Info(LOG_TAG, "ProcessCombatEvents",
                        $"实体死亡 - {entity.Name}(ID:{entity.EntityId})");

                    CombatEvent.TriggerEntityDied(null, entity);

                    // 如果是木桩，重生
                    if (entity is DummyEntity dummy)
                    {
                        dummy.Revive(100);
                    }
                }
            }
        }

        #endregion

        #region Gizmos更新

        private void UpdateGizmos()
        {
            if (!enableGizmos) return;

            var gizmosDrawer = GizmosDrawer.Instance;
            if (gizmosDrawer == null) return;

            // 注意：不要调用 ClearAll()！
            // GizmosDrawer 使用 OnDrawGizmos 自动绘制已注册的实体
            // 实体在 BaseEntity.Initialize() 时注册，在 BaseEntity.Destroy() 时注销
            // 不需要每帧清除和重新注册
        }

        #endregion

        #region 性能监控

        /// <summary>
        /// 更新性能监控
        /// 优化：使用for循环替代foreach避免迭代器GC分配
        /// </summary>
        private void UpdatePerformanceMonitor()
        {
            var monitor = PerformanceMonitor.Instance;
            if (monitor == null) return;

            // 更新碰撞统计
            monitor.UpdateCollisionStats(_collisionChecksThisFrame, _collisionTimeMs);

            // 更新实体统计 - 使用for循环避免foreach迭代器分配
            var allEntities = _gameWorld.AllEntities;
            int total = allEntities.Count;
            int projectiles = _gameWorld.Projectiles.Count;
            int heroes = 0;
            int dummies = 0;

            for (int i = 0; i < total; i++)
            {
                var entity = allEntities[i];
                if (entity is HeroEntity) heroes++;
                else if (entity is DummyEntity) dummies++;
            }

            monitor.UpdateEntityStats(total, total, projectiles, heroes, dummies);

            // 更新网络统计
            monitor.UpdateNetworkStats(
                _lockstepManager.Config.SimulatedDelayMs,
                _lockstepManager.RollbackCount,
                _lockstepManager.InputBuffer.BufferedFrameCount
            );
        }

        #endregion

        #region 实体事件

        private void OnEntityCreated(BaseEntity entity)
        {
            GameLog.Debug(LOG_TAG, "OnEntityCreated",
                $"实体创建 - {entity.Name}(ID:{entity.EntityId}, 类型:{entity.Type})");

            // 创建视图
            _viewManager.CreateViewForEntity(entity);
        }

        private void OnEntityDestroyed(BaseEntity entity)
        {
            GameLog.Debug(LOG_TAG, "OnEntityDestroyed",
                $"实体销毁 - {entity.Name}(ID:{entity.EntityId}, 类型:{entity.Type})");

            // 销毁视图
            _viewManager.DestroyViewForEntity(entity);
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            _lockstepManager.IsPaused = true;
            GameLog.Info(LOG_TAG, "PauseGame", "游戏已暂停");
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            _lockstepManager.IsPaused = false;
            GameLog.Info(LOG_TAG, "ResumeGame", "游戏已恢复");
        }

        /// <summary>
        /// 重置游戏
        /// </summary>
        public void ResetGame()
        {
            GameLog.Info(LOG_TAG, "ResetGame", "重置游戏");

            // 停止帧同步
            _lockstepManager.Stop();

            // 清理实体和视图
            _viewManager.ClearAllViews();
            _entityManager.ClearAllEntities();

            // 重置帧同步
            _lockstepManager.Reset();

            // 重新生成实体
            SpawnInitialEntities();

            // 重新启动
            _lockstepManager.Start();
        }

        #endregion
    }
}
