// ReplayPlayer.cs - 战斗回放播放器
// 加载回放文件并以确定性方式重现战斗过程

using UnityEngine;
using System.IO;
using MobaCombatCore.Core.Lockstep;
using MobaCombatCore.Glue;
using MobaCombatCore.Gameplay.Entity;
using MobaCombatCore.Presentation;
using System.Collections.Generic;

namespace MobaCombatCore.DebugTools
{
    /// <summary>
    /// 战斗回放播放器
    /// </summary>
    public class ReplayPlayer : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("回放文件名（位于Replays目录下）")]
        public string replayFileName;

        [Tooltip("是否在开始时自动播放")]
        public bool autoPlayOnStart = true;

        [Header("播放控制")]
        [Tooltip("播放速度")]
        [Range(0.1f, 8f)]
        public float playbackSpeed = 1f;

        private ReplayData _replayData;
        private GameManager _gameManager;
        private LockstepManager _lockstepManager;
        private bool _isPlaying;
        private int _currentReplayFrame;
        private int _inputIndex;

        private void Start()
        {
            if (autoPlayOnStart)
            {
                StartPlayback();
            }
        }

        private void Update()
        {
            if (_isPlaying)
            {
                Time.timeScale = playbackSpeed;
            }
        }

        /// <summary>
        /// 开始播放
        /// </summary>
        public void StartPlayback()
        {
            if (_isPlaying) return;

            // 加载回放文件
            if (!LoadReplay())
            {
                Debug.LogError("ReplayPlayer: 加载回放文件失败！");
                return;
            }

            // 禁用场景中的GameManager，由ReplayPlayer接管
            _gameManager = FindFirstObjectByType<GameManager>();
            if (_gameManager != null)
            {
                _gameManager.enabled = false;
            }

            // 重置场景
            ResetScene();

            // 根据回放数据初始化
            InitializeFromReplay();

            _isPlaying = true;
            Debug.Log($"ReplayPlayer: 开始播放回放文件 '{replayFileName}'...");
        }

        private bool LoadReplay()
        {
            string filePath = Path.Combine(Application.dataPath, "..", "Replays", replayFileName);

            if (!File.Exists(filePath))
            {
                Debug.LogError($"ReplayPlayer: 文件未找到: {filePath}");
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                _replayData = JsonUtility.FromJson<ReplayData>(json);
                return _replayData != null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"ReplayPlayer: 解析回放文件失败！错误: {ex.Message}");
                return false;
            }
        }

        private void ResetScene()
        {
            // 销毁所有现有实体和视图
            var views = FindObjectsByType<EntityView>(FindObjectsSortMode.None);
            foreach (var view in views)
            {
                Destroy(view.gameObject);
            }
        }

        private void InitializeFromReplay()
        {
            // 确保GameManager被禁用，但其组件可被访问
            if (_gameManager == null)
            {
                Debug.LogError("ReplayPlayer: 找不到GameManager！");
                return;
            }

            // 使用反射获取私有字段的引用
            var field = typeof(GameManager).GetField("_lockstepManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) _lockstepManager = field.GetValue(_gameManager) as LockstepManager;

            // 创建新的帧同步管理器
            _lockstepManager = new LockstepManager(_replayData.LockstepConfig);
            _lockstepManager.OnLogicUpdate += OnLogicUpdate;

            // 注入输入
            foreach(var frameInput in _replayData.FrameInputs)
            {
                _lockstepManager.SubmitFrameInput(frameInput);
            }

            // 创建初始实体
            var entityManagerField = typeof(GameManager).GetField("_entityManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var entityManager = entityManagerField?.GetValue(_gameManager) as EntityManager;

            if (entityManager != null)
            {
                foreach (var entityData in _replayData.InitialEntities)
                {
                    BaseEntity entity = null;
                    switch (entityData.EntityType)
                    {
                        case "HeroEntity":
                            entity = entityManager.CreateEntity<HeroEntity>(entityData.Position);
                            break;
                        case "DummyEntity":
                            entity = entityManager.CreateEntity<DummyEntity>(entityData.Position);
                            break;
                    }
                    if (entity != null)
                    {
                        // EntityId 是只读的，由构造函数自动分配
                        // 回放时需要确保实体创建顺序一致以保证ID一致
                        entity.TeamId = entityData.TeamId;
                        entity.OwnerId = entityData.OwnerId;
                    }
                }
            }

            // 启动帧同步
            _lockstepManager.Start();
        }

        private void OnLogicUpdate(int frame, Core.Math.Fixed64 deltaTime, FrameInput input)
        {
            // 播放器中的逻辑更新由LockstepManager内部处理，我们只需驱动它
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
