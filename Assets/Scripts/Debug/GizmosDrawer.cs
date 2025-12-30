// GizmosDrawer.cs - 调试可视化工具
// 使用OnDrawGizmos在Scene视图中绘制逻辑对象的辅助信息
// 这是展示逻辑与表现分离、网络延迟、碰撞范围的关键

using System;
using System.Collections.Generic;
using MobaCombatCore.Core.Lockstep;
using MobaCombatCore.Gameplay.Entity;
using UnityEngine;

namespace MobaCombatCore.DebugTools
{
    public class GizmosDrawer : MonoBehaviour
    {
        [Header("启用/禁用绘制")]
        [Tooltip("总开关")]
        public bool enableGizmos = true;

        [Tooltip("在Game视图中也显示碰撞范围（使用GL绘制）")]
        public bool drawInGameView = true;

        [Tooltip("绘制实体逻辑位置")]
        public bool drawEntityLogicPosition = true;

        [Tooltip("绘制实体表现位置")]
        public bool drawEntityViewPosition = true;


        [Tooltip("绘制实体预测位置")]
        public bool drawEntityPredictionPosition = true;

        [Tooltip("绘制实体碰撞范围")]
        public bool drawEntityCollisionRadius = true;

        [Tooltip("绘制弹道逻辑位置")]
        public bool drawProjectilePosition = true;

        [Tooltip("绘制弹道碰撞范围")]
        public bool drawProjectileCollisionRadius = true;

        [Tooltip("绘制实体朝向")]
        public bool drawEntityRotation = true;

        [Tooltip("绘制实体移动目标")]
        public bool drawEntityTargetPosition = true;
        
        [Header("碰撞范围颜色")]
        [Tooltip("Dummy碰撞范围颜色")]
        public Color dummyCollisionColor = new Color(1f, 0.5f, 0f, 0.8f); // 橙色
        
        [Tooltip("弹道碰撞范围颜色")]
        public Color projectileCollisionColor = new Color(1f, 1f, 0.5f, 0.8f); // 亮黄色
        
        [Tooltip("英雄碰撞范围颜色")]
        public Color heroCollisionColor = new Color(0f, 1f, 0f, 0.8f); // 绿色
        
        [Header("性能设置")]
        [Tooltip("圆形绘制的分段数（越少性能越好）")]
        [Range(8, 64)]
        public int circleSegments = 16;
        
        // GL绘制材质
        private Material _glMaterial;
        
        // 预计算的圆形单位顶点（性能优化）
        private Vector2[] _circleUnitVertices;
        private int _cachedSegments = -1;
        
        // 缓存的实体列表，避免每帧创建新List
        private List<BaseEntity> _entityDrawCache = new List<BaseEntity>();


        private static GizmosDrawer _instance;
        public static GizmosDrawer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GizmosDrawer>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GizmosDrawer");
                        _instance = go.AddComponent<GizmosDrawer>();
                    }
                }
                return _instance;
            }
        }

        // 使用List来管理实体，而不是字典，因为我们需要在每帧迭代
        private readonly List<BaseEntity> _logicEntities = new List<BaseEntity>();
        private readonly Dictionary<int, Vector3> _viewPositions = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, Vector3> _predictionPositions = new Dictionary<int, Vector3>();
        private readonly object _lock = new object();


        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            
            // 创建GL绘制材质
            CreateGLMaterial();
            
            // 预计算圆形顶点
            UpdateCircleVertices();
        }
        
        /// <summary>
        /// 创建GL绘制材质
        /// </summary>
        private void CreateGLMaterial()
        {
            if (_glMaterial == null)
            {
                // 使用内置的着色器创建材质
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }
                _glMaterial = new Material(shader);
                _glMaterial.hideFlags = HideFlags.HideAndDontSave;
                // 设置为透明混合模式
                _glMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _glMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _glMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _glMaterial.SetInt("_ZWrite", 0);
            }
        }
        
        /// <summary>
        /// 预计算圆形单位顶点（性能优化）
        /// </summary>
        private void UpdateCircleVertices()
        {
            if (_cachedSegments == circleSegments && _circleUnitVertices != null) return;
            
            _cachedSegments = circleSegments;
            _circleUnitVertices = new Vector2[circleSegments + 1];
            
            float angleStep = 2f * Mathf.PI / circleSegments;
            for (int i = 0; i <= circleSegments; i++)
            {
                float angle = i * angleStep;
                _circleUnitVertices[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
        }

        #region 实体注册

        public void RegisterEntity(BaseEntity entity)
        {
            lock (_lock)
            {
                if (!_logicEntities.Contains(entity))
                {
                    _logicEntities.Add(entity);
                }
            }
        }

        public void UnregisterEntity(BaseEntity entity)
        {
            lock (_lock)
            {
                _logicEntities.Remove(entity);
            }
        }

        public void ClearAll()
        {
            lock (_lock)
            {
                _logicEntities.Clear();
            }
        }

        #endregion

        #region 表现层更新

        public void UpdateViewPosition(int entityId, Vector3 position)
        {
            lock (_lock)
            {
                _viewPositions[entityId] = position;
            }
        }

        public void UpdatePredictionPosition(int entityId, Vector3 position)
        {
            lock (_lock)
            {
                _predictionPositions[entityId] = position;
            }
        }

        #endregion

        #region Gizmos 绘制

        private void OnDrawGizmos()
        {
            if (!enableGizmos || !Application.isPlaying) return;

            lock (_lock)
            {
                // 使用缓存列表，避免每帧创建新List
                _entityDrawCache.Clear();
                _entityDrawCache.AddRange(_logicEntities);
            }
            
            // 在锁外进行绘制，减少锁持有时间
            foreach (var entity in _entityDrawCache)
            {
                if (entity == null || entity.IsDestroyed) continue;

                if (entity is ProjectileEntity projectile)
                {
                    DrawProjectileGizmos(projectile);
                }
                else
                {
                    DrawEntityGizmos(entity);
                }
            }
        }

        /// <summary>
        /// 绘制普通实体的Gizmos
        /// </summary>
        private void DrawEntityGizmos(BaseEntity entity)
        {
            var logicPos = entity.Position.ToUnityVector3();

            // 绘制逻辑位置（红框）
            if (drawEntityLogicPosition)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(logicPos, Vector3.one * 0.9f);
            }

            // 绘制预测位置（蓝框）
            if (drawEntityPredictionPosition && _predictionPositions.TryGetValue(entity.EntityId, out var predictPos))
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(predictPos, Vector3.one * 0.95f);
                Gizmos.DrawLine(logicPos, predictPos); // 连接逻辑与预测
            }

            // 绘制表现位置（绿框）
            if (drawEntityViewPosition && _viewPositions.TryGetValue(entity.EntityId, out var viewPos))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(viewPos, Vector3.one * 1.0f);
                if (_predictionPositions.TryGetValue(entity.EntityId, out var predictPos2))
                {
                    Gizmos.DrawLine(predictPos2, viewPos); // 连接预测与表现
                }
                else
                {
                    Gizmos.DrawLine(logicPos, viewPos); // 连接逻辑与表现
                }
            }

            // 绘制碰撞范围
            if (drawEntityCollisionRadius)
            {
                Gizmos.color = new Color(1, 0.5f, 0); // 橙色
                float radius = (float)entity.Stats.CollisionRadius;
                Gizmos.DrawWireSphere(logicPos, radius);
            }

            // 绘制朝向
            if (drawEntityRotation)
            {
                Gizmos.color = Color.blue;
                var rotation = (float)entity.Rotation;
                var direction = new Vector3(Mathf.Sin(rotation), 0, Mathf.Cos(rotation));
                Gizmos.DrawLine(logicPos, logicPos + direction * 1.5f);
            }

            // 绘制移动目标
            if (drawEntityTargetPosition && entity.IsMoving)
            {
                Gizmos.color = Color.cyan;
                var targetPos = entity.TargetPosition.ToUnityVector3();
                Gizmos.DrawLine(logicPos, targetPos);
                Gizmos.DrawWireSphere(targetPos, 0.2f);
            }
        }

        /// <summary>
        /// 绘制弹道的Gizmos
        /// </summary>
        private void DrawProjectileGizmos(ProjectileEntity projectile)
        {
            var logicPos = projectile.Position.ToUnityVector3();

            // 绘制弹道逻辑位置
            if (drawProjectilePosition)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(logicPos, 0.15f);
            }

            // 绘制弹道碰撞范围
            if (drawProjectileCollisionRadius)
            {
                Gizmos.color = new Color(1, 1, 0.5f); // 亮黄色
                float radius = (float)projectile.Stats.CollisionRadius;
                Gizmos.DrawWireSphere(logicPos, radius);
            }
        }

        #endregion
        
        #region GL绘制（Game视图）
        
        /// <summary>
        /// 在Game视图中绘制碰撞范围
        /// </summary>
        private void OnRenderObject()
        {
            if (!enableGizmos || !drawInGameView || !Application.isPlaying) return;
            if (_glMaterial == null) CreateGLMaterial();
            
            // 检查是否需要更新圆形顶点
            UpdateCircleVertices();
            
            _glMaterial.SetPass(0);
            
            // 复制实体列表到缓存
            lock (_lock)
            {
                _entityDrawCache.Clear();
                _entityDrawCache.AddRange(_logicEntities);
            }
            
            // 批量绘制所有圆形（减少GL.Begin/End调用）
            GL.Begin(GL.LINES);
            
            foreach (var entity in _entityDrawCache)
            {
                if (entity == null || entity.IsDestroyed) continue;
                
                if (entity is ProjectileEntity projectile)
                {
                    if (drawProjectileCollisionRadius)
                    {
                        DrawGLCircleBatched(projectile.Position.ToUnityVector3(),
                            (float)projectile.Stats.CollisionRadius,
                            projectileCollisionColor);
                    }
                }
                else
                {
                    if (drawEntityCollisionRadius)
                    {
                        Color color = entity is DummyEntity ? dummyCollisionColor :
                                     entity is HeroEntity ? heroCollisionColor :
                                     dummyCollisionColor;
                        DrawGLCircleBatched(entity.Position.ToUnityVector3(),
                            (float)entity.Stats.CollisionRadius,
                            color);
                    }
                }
            }
            
            GL.End();
        }
        
        /// <summary>
        /// 批量绘制圆形（在GL.Begin/End之间调用，使用预计算顶点）
        /// </summary>
        private void DrawGLCircleBatched(Vector3 center, float radius, Color color)
        {
            GL.Color(color);
            
            float y = center.y + 0.1f; // 稍微抬高避免与地面重叠
            
            for (int i = 0; i < circleSegments; i++)
            {
                Vector2 v1 = _circleUnitVertices[i];
                Vector2 v2 = _circleUnitVertices[i + 1];
                
                GL.Vertex3(center.x + v1.x * radius, y, center.z + v1.y * radius);
                GL.Vertex3(center.x + v2.x * radius, y, center.z + v2.y * radius);
            }
        }
        
        /// <summary>
        /// 使用GL绘制单个圆形（独立调用，用于特殊情况）
        /// </summary>
        private void DrawGLCircle(Vector3 center, float radius, Color color)
        {
            GL.Begin(GL.LINES);
            DrawGLCircleBatched(center, radius, color);
            GL.End();
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        private void OnDestroy()
        {
            if (_glMaterial != null)
            {
                DestroyImmediate(_glMaterial);
                _glMaterial = null;
            }
        }
        
        #endregion
    }

    /// <summary>
    /// FixedVector3到Vector3的转换扩展
    /// </summary>
    public static class GizmosVectorExtensions
    {
        public static Vector3 ToUnityVector3(this MobaCombatCore.Core.Math.FixedVector3 fixedVec)
        {
            return new Vector3(
                (float)fixedVec.X,
                (float)fixedVec.Y,
                (float)fixedVec.Z
            );
        }
    }
}
