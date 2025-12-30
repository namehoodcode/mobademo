// EntityView.cs - 实体表现层
// 负责将逻辑层实体渲染到Unity场景中
// 实现逻辑/表现分离，支持插值平滑

using UnityEngine;
using MobaCombatCore.Core.Math;
using MobaCombatCore.DebugTools;
using MobaCombatCore.Gameplay.Entity;
using MobaCombatCore.Glue.Services;

namespace MobaCombatCore.Presentation
{
    /// <summary>
    /// 实体表现层基类 - 负责渲染和插值
    /// </summary>
    public class EntityView : MonoBehaviour
    {
        private const string LOG_TAG = "EntityView";
        
        #region 引用

        /// <summary>
        /// 关联的逻辑层实体
        /// </summary>
        protected BaseEntity _entity;

        /// <summary>
        /// 逻辑层实体（只读）
        /// </summary>
        public BaseEntity Entity => _entity;

        /// <summary>
        /// 实体ID
        /// </summary>
        public int EntityId => _entity?.EntityId ?? -1;

        #endregion

        #region 插值参数

        /// <summary>
        /// 是否启用插值
        /// </summary>
        [Header("Interpolation Settings")]
        public bool enableInterpolation = true;

        /// <summary>
        /// 插值速度
        /// </summary>
        [Range(1f, 30f)]
        public float interpolationSpeed = 15f;

        /// <summary>
        /// 瞬移阈值（超过此距离直接瞬移）
        /// </summary>
        public float teleportThreshold = 5f;

        /// <summary>
        /// 旋转插值速度
        /// </summary>
        [Range(1f, 30f)]
        public float rotationSpeed = 10f;

        #endregion

        #region 插值状态

        /// <summary>
        /// 上一帧逻辑位置
        /// </summary>
        protected Vector3 _previousLogicPosition;

        /// <summary>
        /// 当前逻辑位置
        /// </summary>
        protected Vector3 _currentLogicPosition;

        /// <summary>
        /// 目标旋转
        /// </summary>
        protected float _targetRotationY;

        /// <summary>
        /// 插值进度（0-1）
        /// </summary>
        protected float _interpolationAlpha;

        /// <summary>
        /// 逻辑帧间隔
        /// </summary>
        protected float _logicFrameInterval = 1f / 30f;

        #endregion

        #region Debug

        /// <summary>
        /// 是否显示Debug信息
        /// </summary>
        [Header("Debug")]
        public bool showDebugGizmos = true;

        /// <summary>
        /// 逻辑位置颜色
        /// </summary>
        public Color logicPositionColor = Color.red;

        /// <summary>
        /// 渲染位置颜色
        /// </summary>
        public Color renderPositionColor = Color.green;

        #endregion

        #region 生命周期

        protected virtual void Awake()
        {
            _previousLogicPosition = transform.position;
            _currentLogicPosition = transform.position;
            _interpolationAlpha = 0f;
        }

        protected virtual void Update()
        {
            if (_entity == null) return;

            // 更新插值
            UpdateInterpolation();

            // 更新旋转
            UpdateRotation();

            // 上报渲染位置给GizmosDrawer
            if (GizmosDrawer.Instance != null && _entity != null)
            {
                GizmosDrawer.Instance.UpdateViewPosition(_entity.EntityId, transform.position);
            }
        }

        protected virtual void OnDestroy()
        {
            // 解除与逻辑实体的绑定
            if (_entity != null)
            {
                _entity.OnPositionChanged -= OnEntityPositionChanged;
                _entity.OnDestroyed -= OnEntityDestroyed;
                _entity = null;
            }
        }

        #endregion

        #region 绑定

        /// <summary>
        /// 绑定逻辑层实体
        /// </summary>
        public virtual void BindEntity(BaseEntity entity)
        {
            if (_entity != null)
            {
                GameLog.Debug(LOG_TAG, "BindEntity",
                    $"解除旧绑定 - 实体ID:{_entity.EntityId}, 名称:{_entity.Name}");
                // 解除旧绑定
                _entity.OnPositionChanged -= OnEntityPositionChanged;
                _entity.OnDestroyed -= OnEntityDestroyed;
            }

            _entity = entity;

            if (_entity != null)
            {
                // 建立新绑定
                _entity.OnPositionChanged += OnEntityPositionChanged;
                _entity.OnDestroyed += OnEntityDestroyed;

                // 初始化位置
                var pos = FixedVector3ToVector3(_entity.Position);
                _previousLogicPosition = pos;
                _currentLogicPosition = pos;
                transform.position = pos;

                // 初始化旋转
                _targetRotationY = (float)_entity.Rotation * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, _targetRotationY, 0);

                gameObject.name = $"{_entity.Type}_{_entity.EntityId}";
                
                GameLog.Info(LOG_TAG, "BindEntity",
                    $"绑定新实体 - 实体ID:{_entity.EntityId}, 类型:{_entity.Type}, 名称:{_entity.Name}, OwnerId:{_entity.OwnerId}, 初始位置:{pos}");
            }
            else
            {
                GameLog.Warning(LOG_TAG, "BindEntity", "绑定的实体为空!");
            }
        }

        /// <summary>
        /// 解除绑定
        /// </summary>
        public virtual void UnbindEntity()
        {
            if (_entity != null)
            {
                _entity.OnPositionChanged -= OnEntityPositionChanged;
                _entity.OnDestroyed -= OnEntityDestroyed;
                _entity = null;
            }
        }

        #endregion

        #region 逻辑更新回调

        // 用于减少日志频率的计数器
        private int _logicUpdateLogCounter = 0;
        private const int LOG_EVERY_N_FRAMES = 30; // 每30帧记录一次

        /// <summary>
        /// 逻辑帧更新（由LockstepManager调用）
        /// </summary>
        public virtual void OnLogicUpdate(float interpolationAlpha)
        {
            if (_entity == null)
            {
                if (_logicUpdateLogCounter == 0)
                {
                    GameLog.Warning(LOG_TAG, "OnLogicUpdate", $"{gameObject.name} - 实体为空!");
                }
                return;
            }

            // 保存上一帧位置
            _previousLogicPosition = _currentLogicPosition;

            // 更新当前逻辑位置
            _currentLogicPosition = FixedVector3ToVector3(_entity.Position);

            // 更新目标旋转
            _targetRotationY = (float)_entity.Rotation * Mathf.Rad2Deg;

            // 检查位置是否变化
            bool positionChanged = Vector3.Distance(_previousLogicPosition, _currentLogicPosition) > 0.001f;
            
            // 每N帧记录一次，或者位置变化时记录
            _logicUpdateLogCounter++;
            if (positionChanged || _logicUpdateLogCounter >= LOG_EVERY_N_FRAMES)
            {
                if (positionChanged)
                {
                    GameLog.Debug(LOG_TAG, "OnLogicUpdate",
                        $"{gameObject.name} - 位置变化! 上一帧:{_previousLogicPosition} -> 当前:{_currentLogicPosition}, IsMoving:{_entity.IsMoving}, TargetPos:{FixedVector3ToVector3(_entity.TargetPosition)}");
                }
                else
                {
                    GameLog.Debug(LOG_TAG, "OnLogicUpdate",
                        $"{gameObject.name} - 位置未变化, 当前:{_currentLogicPosition}, IsMoving:{_entity.IsMoving}");
                }
                _logicUpdateLogCounter = 0;
            }

            // 重置插值进度
            _interpolationAlpha = 0f;
        }

        /// <summary>
        /// 实体位置变化回调
        /// </summary>
        protected virtual void OnEntityPositionChanged(BaseEntity entity, FixedVector3 oldPos, FixedVector3 newPos)
        {
            // 检查是否需要瞬移
            var oldPosV3 = FixedVector3ToVector3(oldPos);
            var newPosV3 = FixedVector3ToVector3(newPos);
            float distance = Vector3.Distance(oldPosV3, newPosV3);

            GameLog.Debug(LOG_TAG, "OnEntityPositionChanged",
                $"{gameObject.name} - 位置变化事件: {oldPosV3} -> {newPosV3}, 距离:{distance:F3}, 瞬移阈值:{teleportThreshold}");

            if (distance > teleportThreshold)
            {
                GameLog.Info(LOG_TAG, "OnEntityPositionChanged", $"{gameObject.name} - 执行瞬移!");
                // 瞬移
                _previousLogicPosition = newPosV3;
                _currentLogicPosition = newPosV3;
                transform.position = newPosV3;
                _interpolationAlpha = 1f;
            }
        }

        /// <summary>
        /// 实体销毁回调
        /// </summary>
        protected virtual void OnEntityDestroyed(BaseEntity entity)
        {
            // 默认销毁GameObject
            Destroy(gameObject);
        }

        #endregion

        #region 插值

        /// <summary>
        /// 更新位置插值
        /// </summary>
        protected virtual void UpdateInterpolation()
        {
            if (!enableInterpolation)
            {
                // 不插值，直接使用逻辑位置
                transform.position = _currentLogicPosition;
                return;
            }

            // 累积插值进度
            _interpolationAlpha += Time.deltaTime / _logicFrameInterval;
            _interpolationAlpha = Mathf.Clamp01(_interpolationAlpha);

            // 线性插值
            Vector3 targetPos = Vector3.Lerp(_previousLogicPosition, _currentLogicPosition, _interpolationAlpha);

            // 平滑移动到目标位置
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * interpolationSpeed);
        }

        /// <summary>
        /// 更新旋转插值
        /// </summary>
        protected virtual void UpdateRotation()
        {
            Quaternion targetRotation = Quaternion.Euler(0, _targetRotationY, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        /// <summary>
        /// 设置逻辑帧间隔
        /// </summary>
        public void SetLogicFrameInterval(float interval)
        {
            _logicFrameInterval = interval;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// FixedVector3转Unity Vector3
        /// </summary>
        protected Vector3 FixedVector3ToVector3(FixedVector3 fixedVec)
        {
            return new Vector3(
                (float)fixedVec.X,
                (float)fixedVec.Y,
                (float)fixedVec.Z
            );
        }

        /// <summary>
        /// Unity Vector3转FixedVector3
        /// </summary>
        protected FixedVector3 Vector3ToFixedVector3(Vector3 vec)
        {
            return new FixedVector3(
                Fixed64.FromFloat(vec.x),
                Fixed64.FromFloat(vec.y),
                Fixed64.FromFloat(vec.z)
            );
        }

        /// <summary>
        /// 获取逻辑位置（Unity坐标）
        /// </summary>
        public Vector3 GetLogicPosition()
        {
            return _currentLogicPosition;
        }

        /// <summary>
        /// 获取渲染位置（插值后）
        /// </summary>
        public Vector3 GetRenderPosition()
        {
            return transform.position;
        }

        /// <summary>
        /// 获取位置偏差（逻辑与渲染的差距）
        /// </summary>
        public float GetPositionOffset()
        {
            return Vector3.Distance(_currentLogicPosition, transform.position);
        }

        #endregion
    }
}
