// SkillData.cs - 技能配置数据
// 使用ScriptableObject实现配置驱动设计
// 策划可以通过编辑器修改参数，无需修改代码

using UnityEngine;
using MobaCombatCore.Core.Math;

namespace MobaCombatCore.Gameplay.Skill
{
    /// <summary>
    /// 技能类型
    /// </summary>
    public enum SkillType
    {
        /// <summary>
        /// 弹道型（如火球术）
        /// </summary>
        Projectile = 0,

        /// <summary>
        /// 位移型（如闪现）
        /// </summary>
        Blink = 1,

        /// <summary>
        /// AOE型（如暴风雪）
        /// </summary>
        AreaOfEffect = 2,

        /// <summary>
        /// 指向型（需要选择目标）
        /// </summary>
        Targeted = 3,

        /// <summary>
        /// 自身型（无需选择目标）
        /// </summary>
        Self = 4
    }

    /// <summary>
    /// 技能目标类型
    /// </summary>
    public enum SkillTargetType
    {
        /// <summary>
        /// 无目标
        /// </summary>
        None = 0,

        /// <summary>
        /// 敌方单位
        /// </summary>
        Enemy = 1,

        /// <summary>
        /// 友方单位
        /// </summary>
        Ally = 2,

        /// <summary>
        /// 自己
        /// </summary>
        Self = 3,

        /// <summary>
        /// 地面位置
        /// </summary>
        Ground = 4,

        /// <summary>
        /// 方向
        /// </summary>
        Direction = 5
    }

    /// <summary>
    /// 伤害类型
    /// </summary>
    public enum DamageType
    {
        Physical = 0,
        Magical = 1,
        True = 2
    }

    /// <summary>
    /// 技能配置数据 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "MobaCombatCore/Skill Data", order = 1)]
    public class SkillData : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("技能唯一ID")]
        public int skillId;

        [Tooltip("技能名称")]
        public string skillName;

        [Tooltip("技能描述")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("技能图标（可选）")]
        public Sprite icon;

        [Header("技能类型")]
        [Tooltip("技能类型")]
        public SkillType skillType = SkillType.Projectile;

        [Tooltip("目标类型")]
        public SkillTargetType targetType = SkillTargetType.Direction;

        [Tooltip("伤害类型")]
        public DamageType damageType = DamageType.Magical;

        [Header("时间参数（秒）")]
        [Tooltip("前摇时间")]
        [Range(0f, 2f)]
        public float castTime = 0.3f;

        [Tooltip("后摇时间")]
        [Range(0f, 2f)]
        public float recoveryTime = 0.2f;

        [Tooltip("冷却时间")]
        [Range(0f, 120f)]
        public float cooldown = 5f;

        [Header("消耗")]
        [Tooltip("法力消耗")]
        [Range(0, 500)]
        public int manaCost = 50;

        [Header("范围参数")]
        [Tooltip("施法距离")]
        [Range(0f, 50f)]
        public float range = 10f;

        [Tooltip("效果半径（AOE技能）")]
        [Range(0f, 20f)]
        public float radius = 0f;

        [Header("伤害参数")]
        [Tooltip("基础伤害")]
        [Range(0, 1000)]
        public int baseDamage = 100;

        [Tooltip("法术强度加成比例")]
        [Range(0f, 2f)]
        public float apRatio = 0.5f;

        [Header("弹道参数（仅弹道型技能）")]
        [Tooltip("弹道速度")]
        [Range(1f, 50f)]
        public float projectileSpeed = 15f;

        [Tooltip("弹道碰撞半径")]
        [Range(0.1f, 2f)]
        public float projectileRadius = 0.3f;

        [Tooltip("弹道最大飞行距离")]
        [Range(1f, 50f)]
        public float projectileMaxDistance = 15f;

        [Tooltip("命中后是否销毁")]
        public bool destroyOnHit = true;

        [Tooltip("是否是跟踪弹")]
        public bool isHoming = false;

        [Header("位移参数（仅位移型技能）")]
        [Tooltip("位移距离")]
        [Range(1f, 20f)]
        public float blinkDistance = 8f;

        [Header("AOE参数（仅AOE型技能）")]
        [Tooltip("持续时间（秒）")]
        [Range(0f, 10f)]
        public float duration = 3f;

        [Tooltip("伤害间隔（秒）")]
        [Range(0.1f, 2f)]
        public float tickInterval = 0.5f;

        [Tooltip("每次伤害")]
        [Range(0, 500)]
        public int tickDamage = 30;

        [Header("Buff效果")]
        [Tooltip("是否附带减速")]
        public bool applySlow = false;

        [Tooltip("减速比例")]
        [Range(0f, 1f)]
        public float slowPercent = 0.3f;

        [Tooltip("减速持续时间")]
        [Range(0f, 5f)]
        public float slowDuration = 2f;

        [Tooltip("是否附带眩晕")]
        public bool applyStun = false;

        [Tooltip("眩晕持续时间")]
        [Range(0f, 3f)]
        public float stunDuration = 1f;

        [Header("特效（可选）")]
        [Tooltip("施法特效")]
        public GameObject castVfxPrefab;

        [Tooltip("命中特效")]
        public GameObject hitVfxPrefab;

        [Tooltip("弹道预制体")]
        public GameObject projectilePrefab;

        #region 定点数转换方法

        /// <summary>
        /// 获取定点数前摇时间
        /// </summary>
        public Fixed64 GetCastTimeFixed()
        {
            return Fixed64.FromFloat(castTime);
        }

        /// <summary>
        /// 获取定点数后摇时间
        /// </summary>
        public Fixed64 GetRecoveryTimeFixed()
        {
            return Fixed64.FromFloat(recoveryTime);
        }

        /// <summary>
        /// 获取定点数冷却时间
        /// </summary>
        public Fixed64 GetCooldownFixed()
        {
            return Fixed64.FromFloat(cooldown);
        }

        /// <summary>
        /// 获取定点数施法距离
        /// </summary>
        public Fixed64 GetRangeFixed()
        {
            return Fixed64.FromFloat(range);
        }

        /// <summary>
        /// 获取定点数效果半径
        /// </summary>
        public Fixed64 GetRadiusFixed()
        {
            return Fixed64.FromFloat(radius);
        }

        /// <summary>
        /// 获取定点数弹道速度
        /// </summary>
        public Fixed64 GetProjectileSpeedFixed()
        {
            return Fixed64.FromFloat(projectileSpeed);
        }

        /// <summary>
        /// 获取定点数弹道半径
        /// </summary>
        public Fixed64 GetProjectileRadiusFixed()
        {
            return Fixed64.FromFloat(projectileRadius);
        }

        /// <summary>
        /// 获取定点数弹道最大距离
        /// </summary>
        public Fixed64 GetProjectileMaxDistanceFixed()
        {
            return Fixed64.FromFloat(projectileMaxDistance);
        }

        /// <summary>
        /// 获取定点数位移距离
        /// </summary>
        public Fixed64 GetBlinkDistanceFixed()
        {
            return Fixed64.FromFloat(blinkDistance);
        }

        /// <summary>
        /// 获取定点数持续时间
        /// </summary>
        public Fixed64 GetDurationFixed()
        {
            return Fixed64.FromFloat(duration);
        }

        /// <summary>
        /// 获取定点数伤害间隔
        /// </summary>
        public Fixed64 GetTickIntervalFixed()
        {
            return Fixed64.FromFloat(tickInterval);
        }

        /// <summary>
        /// 获取定点数减速比例
        /// </summary>
        public Fixed64 GetSlowPercentFixed()
        {
            return Fixed64.FromFloat(slowPercent);
        }

        /// <summary>
        /// 获取定点数减速持续时间
        /// </summary>
        public Fixed64 GetSlowDurationFixed()
        {
            return Fixed64.FromFloat(slowDuration);
        }

        /// <summary>
        /// 获取定点数眩晕持续时间
        /// </summary>
        public Fixed64 GetStunDurationFixed()
        {
            return Fixed64.FromFloat(stunDuration);
        }

        #endregion

        #region 伤害计算

        /// <summary>
        /// 计算最终伤害
        /// </summary>
        /// <param name="abilityPower">施法者法术强度</param>
        /// <returns>最终伤害值</returns>
        public int CalculateDamage(int abilityPower)
        {
            return baseDamage + (int)(abilityPower * apRatio);
        }

        #endregion
    }
}
