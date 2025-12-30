// ReplayRecorder.cs - 战斗回放录制器
// 记录游戏过程中的所有输入和初始状态

using UnityEngine;
using System.Collections.Generic;
using MobaCombatCore.Core.Lockstep;
using MobaCombatCore.Glue;
using MobaCombatCore.Gameplay.Entity;
using System.IO;

namespace MobaCombatCore.DebugTools
{
    /// <summary>
    /// 战斗回放录制器
    /// </summary>
    public class ReplayRecorder : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("是否在开始时自动录制")]
        public bool autoRecordOnStart = true;
        [Tooltip("录制文件保存路径（相对于项目根目录）")]
        public string savePath = "Replays";
        [Tooltip("录制文件名格式")]
        public string fileNameFormat = "replay_{0}.json";

        [Header("引用")]
        [Tooltip("GameManager引用（可选，自动查找）")]
        public GameManager gameManager;

        private ReplayData _replayData;
        private bool _isRecording;
        private LockstepManager _lockstepManager;
        private GameWorld _gameWorld;
        private EntityManager _entityManager;

        private void Start()
        {
            if (autoRecordOnStart)
            {
                StartRecording();
            }
        }

        private void OnDestroy()
        {
            StopRecording();
        }

        /// <summary>
        /// 开始录制
        /// </summary>
        public void StartRecording()
        {
            if (_isRecording) return;

            // 获取引用
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }

            if (gameManager != null)
            {
                var field = typeof(GameManager).GetField("_lockstepManager",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) _lockstepManager = field.GetValue(gameManager) as LockstepManager;

                var worldField = typeof(GameManager).GetField("_gameWorld",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (worldField != null) _gameWorld = worldField.GetValue(gameManager) as GameWorld;

                var entityMgrField = typeof(GameManager).GetField("_entityManager",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (entityMgrField != null) _entityManager = entityMgrField.GetValue(gameManager) as EntityManager;
            }

            if (_lockstepManager == null || _gameWorld == null || _entityManager == null)
            {
                Debug.LogError("ReplayRecorder: 无法获取必要的引用，录制失败！");
                return;
            }

            // 创建回放数据
            _replayData = new ReplayData
            {
                RandomSeed = gameManager.randomSeed,
                PlayerCount = _lockstepManager.Config.PlayerCount,
                LockstepConfig = _lockstepManager.Config,
                FrameInputs = new List<FrameInput>()
            };

            // 记录初始实体
            foreach (var entity in _gameWorld.AllEntities)
            {
                _replayData.InitialEntities.Add(new InitialEntityData
                {
                    EntityId = entity.EntityId,
                    TeamId = entity.TeamId,
                    OwnerId = entity.OwnerId,
                    Position = entity.Position,
                    EntityType = entity.GetType().Name
                });
            }

            // 订阅事件
            _lockstepManager.OnLogicUpdate += OnLogicUpdate;
            _entityManager.OnEntityCreated += OnEntityCreated;

            _isRecording = true;
            Debug.Log("ReplayRecorder: 开始录制...");
        }

        /// <summary>
        /// 停止录制并保存
        /// </summary>
        public void StopRecording()
        {
            if (!_isRecording) return;

            // 取消订阅
            if (_lockstepManager != null)
            {
                _lockstepManager.OnLogicUpdate -= OnLogicUpdate;
            }
            if (_entityManager != null)
            {
                _entityManager.OnEntityCreated -= OnEntityCreated;
            }

            // 保存文件
            SaveReplay();

            _isRecording = false;
            Debug.Log("ReplayRecorder: 停止录制。");
        }

        private void OnLogicUpdate(int frame, Core.Math.Fixed64 deltaTime, FrameInput input)
        {
            if (_isRecording && input != null)
            {
                // 仅记录有实际动作的输入帧，以减小文件大小
                bool hasAction = false;
                foreach (var playerInput in input.PlayerInputs)
                {
                    if (playerInput != null && playerInput.HasActions)
                    {
                        hasAction = true;
                        break;
                    }
                }

                if (hasAction)
                {
                    _replayData.FrameInputs.Add(input);
                }
            }
        }

        private void OnEntityCreated(BaseEntity entity)
        {
            if (_isRecording)
            {
                // 记录动态创建的实体（如弹道）
                // 简化处理：回放时，弹道由技能触发重新生成，因此不在此处记录
            }
        }

        private void SaveReplay()
        {
            if (_replayData == null) return;

            try
            {
                string json = JsonUtility.ToJson(_replayData, true);
                string directory = Path.Combine(Application.dataPath, "..", savePath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string fileName = string.Format(fileNameFormat, System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                string filePath = Path.Combine(directory, fileName);

                File.WriteAllText(filePath, json);

                Debug.Log($"ReplayRecorder: 回放已保存到 {filePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"ReplayRecorder: 保存回放失败！错误: {ex.Message}");
            }
        }
    }
}
