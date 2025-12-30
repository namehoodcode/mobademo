// SpatialHash.cs - 空间哈希
// 用于优化碰撞检测，将O(n²)降低到O(n)
// 基于定点数，保证确定性

using System;
using System.Collections.Generic;
using MobaCombatCore.Core.Math;

namespace MobaCombatCore.Core.Physics
{
    /// <summary>
    /// 空间哈希格子
    /// </summary>
    public class SpatialHashCell
    {
        public List<int> EntityIds = new List<int>(16);

        public void Clear()
        {
            EntityIds.Clear();
        }

        public void Add(int entityId)
        {
            EntityIds.Add(entityId);
        }

        public void Remove(int entityId)
        {
            EntityIds.Remove(entityId);
        }
    }

    /// <summary>
    /// 空间哈希网格
    /// 将世界划分为固定大小的格子，实体只与相邻格子内的实体进行碰撞检测
    /// </summary>
    public class SpatialHash
    {
        /// <summary>
        /// 格子大小
        /// </summary>
        public Fixed64 CellSize { get; private set; }

        /// <summary>
        /// 格子大小的倒数（用于快速计算）
        /// </summary>
        private Fixed64 _inverseCellSize;

        /// <summary>
        /// 世界边界
        /// </summary>
        public AABB WorldBounds { get; private set; }

        /// <summary>
        /// 格子数量（X方向）
        /// </summary>
        public int CellCountX { get; private set; }

        /// <summary>
        /// 格子数量（Z方向）
        /// </summary>
        public int CellCountZ { get; private set; }

        /// <summary>
        /// 格子数组
        /// </summary>
        private SpatialHashCell[] _cells;

        /// <summary>
        /// 实体到格子的映射
        /// </summary>
        private Dictionary<int, List<int>> _entityToCells = new Dictionary<int, List<int>>();

        /// <summary>
        /// 临时列表（避免GC）
        /// </summary>
        private List<int> _tempCellIndices = new List<int>(9);
        private HashSet<int> _tempEntityIds = new HashSet<int>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="worldBounds">世界边界</param>
        /// <param name="cellSize">格子大小</param>
        public SpatialHash(AABB worldBounds, Fixed64 cellSize)
        {
            WorldBounds = worldBounds;
            CellSize = cellSize;
            _inverseCellSize = Fixed64.One / cellSize;

            // 计算格子数量
            CellCountX = (int)Fixed64.Ceiling((worldBounds.Max.X - worldBounds.Min.X) * _inverseCellSize).ToInt();
            CellCountZ = (int)Fixed64.Ceiling((worldBounds.Max.Z - worldBounds.Min.Z) * _inverseCellSize).ToInt();

            // 确保至少有1个格子
            CellCountX = System.Math.Max(1, CellCountX);
            CellCountZ = System.Math.Max(1, CellCountZ);

            // 创建格子
            _cells = new SpatialHashCell[CellCountX * CellCountZ];
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = new SpatialHashCell();
            }
        }

        /// <summary>
        /// 从整数参数创建
        /// </summary>
        public static SpatialHash Create(int minX, int minZ, int maxX, int maxZ, int cellSize)
        {
            AABB bounds = AABB.FromInt(minX, minZ, maxX, maxZ);
            return new SpatialHash(bounds, Fixed64.FromInt(cellSize));
        }

        /// <summary>
        /// 清空所有格子
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].Clear();
            }
            _entityToCells.Clear();
        }

        /// <summary>
        /// 获取位置所在的格子索引
        /// </summary>
        public int GetCellIndex(FixedVector3 position)
        {
            int x = GetCellX(position.X);
            int z = GetCellZ(position.Z);
            return GetCellIndex(x, z);
        }

        /// <summary>
        /// 获取X方向格子索引
        /// </summary>
        private int GetCellX(Fixed64 x)
        {
            int cellX = ((x - WorldBounds.Min.X) * _inverseCellSize).ToInt();
            return System.Math.Clamp(cellX, 0, CellCountX - 1);
        }

        /// <summary>
        /// 获取Z方向格子索引
        /// </summary>
        private int GetCellZ(Fixed64 z)
        {
            int cellZ = ((z - WorldBounds.Min.Z) * _inverseCellSize).ToInt();
            return System.Math.Clamp(cellZ, 0, CellCountZ - 1);
        }

        /// <summary>
        /// 从格子坐标获取索引
        /// </summary>
        private int GetCellIndex(int x, int z)
        {
            return z * CellCountX + x;
        }

        /// <summary>
        /// 插入圆形碰撞体
        /// </summary>
        public void Insert(Circle circle)
        {
            if (!circle.Enabled) return;

            // 获取圆形覆盖的所有格子
            int minCellX = GetCellX(circle.Center.X - circle.Radius);
            int maxCellX = GetCellX(circle.Center.X + circle.Radius);
            int minCellZ = GetCellZ(circle.Center.Z - circle.Radius);
            int maxCellZ = GetCellZ(circle.Center.Z + circle.Radius);

            // 记录实体所在的格子
            if (!_entityToCells.TryGetValue(circle.EntityId, out var cellList))
            {
                cellList = new List<int>(4);
                _entityToCells[circle.EntityId] = cellList;
            }
            cellList.Clear();

            // 插入到所有覆盖的格子
            for (int z = minCellZ; z <= maxCellZ; z++)
            {
                for (int x = minCellX; x <= maxCellX; x++)
                {
                    int index = GetCellIndex(x, z);
                    _cells[index].Add(circle.EntityId);
                    cellList.Add(index);
                }
            }
        }

        /// <summary>
        /// 插入AABB碰撞体
        /// </summary>
        public void Insert(AABB aabb)
        {
            if (!aabb.Enabled) return;

            int minCellX = GetCellX(aabb.Min.X);
            int maxCellX = GetCellX(aabb.Max.X);
            int minCellZ = GetCellZ(aabb.Min.Z);
            int maxCellZ = GetCellZ(aabb.Max.Z);

            if (!_entityToCells.TryGetValue(aabb.EntityId, out var cellList))
            {
                cellList = new List<int>(4);
                _entityToCells[aabb.EntityId] = cellList;
            }
            cellList.Clear();

            for (int z = minCellZ; z <= maxCellZ; z++)
            {
                for (int x = minCellX; x <= maxCellX; x++)
                {
                    int index = GetCellIndex(x, z);
                    _cells[index].Add(aabb.EntityId);
                    cellList.Add(index);
                }
            }
        }

        /// <summary>
        /// 移除实体
        /// </summary>
        public void Remove(int entityId)
        {
            if (_entityToCells.TryGetValue(entityId, out var cellList))
            {
                foreach (int cellIndex in cellList)
                {
                    _cells[cellIndex].Remove(entityId);
                }
                cellList.Clear();
            }
        }

        /// <summary>
        /// 更新实体位置
        /// </summary>
        public void Update(Circle circle)
        {
            Remove(circle.EntityId);
            Insert(circle);
        }

        /// <summary>
        /// 查询圆形范围内的所有实体
        /// </summary>
        public void QueryCircle(FixedVector3 center, Fixed64 radius, List<int> results)
        {
            results.Clear();
            _tempEntityIds.Clear();

            int minCellX = GetCellX(center.X - radius);
            int maxCellX = GetCellX(center.X + radius);
            int minCellZ = GetCellZ(center.Z - radius);
            int maxCellZ = GetCellZ(center.Z + radius);

            for (int z = minCellZ; z <= maxCellZ; z++)
            {
                for (int x = minCellX; x <= maxCellX; x++)
                {
                    int index = GetCellIndex(x, z);
                    foreach (int entityId in _cells[index].EntityIds)
                    {
                        if (_tempEntityIds.Add(entityId))
                        {
                            results.Add(entityId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 查询AABB范围内的所有实体
        /// </summary>
        public void QueryAABB(AABB bounds, List<int> results)
        {
            results.Clear();
            _tempEntityIds.Clear();

            int minCellX = GetCellX(bounds.Min.X);
            int maxCellX = GetCellX(bounds.Max.X);
            int minCellZ = GetCellZ(bounds.Min.Z);
            int maxCellZ = GetCellZ(bounds.Max.Z);

            for (int z = minCellZ; z <= maxCellZ; z++)
            {
                for (int x = minCellX; x <= maxCellX; x++)
                {
                    int index = GetCellIndex(x, z);
                    foreach (int entityId in _cells[index].EntityIds)
                    {
                        if (_tempEntityIds.Add(entityId))
                        {
                            results.Add(entityId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 查询点所在格子的所有实体
        /// </summary>
        public void QueryPoint(FixedVector3 point, List<int> results)
        {
            results.Clear();
            int index = GetCellIndex(point);
            results.AddRange(_cells[index].EntityIds);
        }

        /// <summary>
        /// 获取可能与指定实体碰撞的所有实体
        /// </summary>
        public void GetPotentialCollisions(int entityId, List<int> results)
        {
            results.Clear();
            _tempEntityIds.Clear();

            if (!_entityToCells.TryGetValue(entityId, out var cellList))
            {
                return;
            }

            foreach (int cellIndex in cellList)
            {
                foreach (int otherId in _cells[cellIndex].EntityIds)
                {
                    if (otherId != entityId && _tempEntityIds.Add(otherId))
                    {
                        results.Add(otherId);
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有可能的碰撞对
        /// </summary>
        public void GetAllPotentialPairs(List<(int, int)> pairs)
        {
            pairs.Clear();
            HashSet<long> checkedPairs = new HashSet<long>();

            for (int i = 0; i < _cells.Length; i++)
            {
                var cell = _cells[i];
                var entities = cell.EntityIds;

                for (int a = 0; a < entities.Count; a++)
                {
                    for (int b = a + 1; b < entities.Count; b++)
                    {
                        int idA = entities[a];
                        int idB = entities[b];

                        // 确保较小的ID在前，避免重复
                        if (idA > idB)
                        {
                            (idA, idB) = (idB, idA);
                        }

                        long pairKey = ((long)idA << 32) | (uint)idB;
                        if (checkedPairs.Add(pairKey))
                        {
                            pairs.Add((idA, idB));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取格子内实体数量统计
        /// </summary>
        public (int totalEntities, int maxPerCell, float avgPerCell) GetStatistics()
        {
            int total = 0;
            int max = 0;
            int nonEmptyCells = 0;

            for (int i = 0; i < _cells.Length; i++)
            {
                int count = _cells[i].EntityIds.Count;
                total += count;
                if (count > max) max = count;
                if (count > 0) nonEmptyCells++;
            }

            float avg = nonEmptyCells > 0 ? (float)total / nonEmptyCells : 0;
            return (total, max, avg);
        }

        /// <summary>
        /// 调试：获取格子边界
        /// </summary>
        public AABB GetCellBounds(int cellX, int cellZ)
        {
            Fixed64 minX = WorldBounds.Min.X + CellSize * cellX;
            Fixed64 minZ = WorldBounds.Min.Z + CellSize * cellZ;
            Fixed64 maxX = minX + CellSize;
            Fixed64 maxZ = minZ + CellSize;

            return new AABB(
                new FixedVector3(minX, Fixed64.Zero, minZ),
                new FixedVector3(maxX, Fixed64.Zero, maxZ)
            );
        }
    }
}
