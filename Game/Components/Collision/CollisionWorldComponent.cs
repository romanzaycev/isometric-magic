using System;
using System.Collections.Generic;
using System.Numerics;
using IsometricMagic.Engine;

namespace IsometricMagic.Game.Components.Collision
{
    public class CollisionWorldComponent : Component
    {
        public int CellSize { get; set; } = 128;

        private readonly Dictionary<long, List<WorldColliderComponent>> _cells = new();
        private readonly Dictionary<WorldColliderComponent, List<long>> _colliderCells = new();
        private float _maxRadius;

        public void Register(WorldColliderComponent collider)
        {
            if (_colliderCells.ContainsKey(collider)) return;
            var center = collider.GetWorldCenter();
            var keys = GetCellKeys(center, collider.Radius);
            _colliderCells[collider] = keys;
            if (collider.Radius > _maxRadius)
            {
                _maxRadius = collider.Radius;
            }
            foreach (var key in keys)
            {
                if (!_cells.TryGetValue(key, out var list))
                {
                    list = new List<WorldColliderComponent>();
                    _cells[key] = list;
                }

                if (!list.Contains(collider))
                {
                    list.Add(collider);
                }
            }
        }

        public void Unregister(WorldColliderComponent collider)
        {
            if (!_colliderCells.TryGetValue(collider, out var keys)) return;

            foreach (var key in keys)
            {
                if (_cells.TryGetValue(key, out var list))
                {
                    list.Remove(collider);
                    if (list.Count == 0)
                    {
                        _cells.Remove(key);
                    }
                }
            }

            _colliderCells.Remove(collider);
            if (collider.Radius >= _maxRadius - 0.001f)
            {
                RecalculateMaxRadius();
            }
        }

        public void UpdateCollider(WorldColliderComponent collider, Vector2 newCenter)
        {
            if (!_colliderCells.TryGetValue(collider, out var oldKeys))
            {
                Register(collider);
                return;
            }

            var newKeys = GetCellKeys(newCenter, collider.Radius);
            if (AreSameKeys(oldKeys, newKeys)) return;

            foreach (var key in oldKeys)
            {
                if (_cells.TryGetValue(key, out var list))
                {
                    list.Remove(collider);
                    if (list.Count == 0)
                    {
                        _cells.Remove(key);
                    }
                }
            }

            _colliderCells[collider] = newKeys;
            foreach (var key in newKeys)
            {
                if (!_cells.TryGetValue(key, out var list))
                {
                    list = new List<WorldColliderComponent>();
                    _cells[key] = list;
                }

                if (!list.Contains(collider))
                {
                    list.Add(collider);
                }
            }
        }

        public bool OverlapsCircle(Vector2 center, float radius, WorldColliderComponent? ignore = null)
        {
            var keys = GetCellKeys(center, radius + _maxRadius);
            var checkedSet = new HashSet<WorldColliderComponent>();

            foreach (var key in keys)
            {
                if (!_cells.TryGetValue(key, out var list)) continue;
                foreach (var collider in list)
                {
                    if (collider == ignore) continue;
                    if (!collider.IsActiveAndEnabled) continue;
                    if (!checkedSet.Add(collider)) continue;

                    var otherCenter = collider.GetWorldCenter();
                    var combined = radius + collider.Radius;
                    if (Vector2.DistanceSquared(center, otherCenter) <= combined * combined)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void RecalculateMaxRadius()
        {
            var max = 0f;
            foreach (var collider in _colliderCells.Keys)
            {
                if (collider.Radius > max)
                {
                    max = collider.Radius;
                }
            }
            _maxRadius = max;
        }

        private List<long> GetCellKeys(Vector2 center, float radius)
        {
            var half = MathF.Max(0f, radius);
            var minX = (int) MathF.Floor((center.X - half) / CellSize);
            var maxX = (int) MathF.Floor((center.X + half) / CellSize);
            var minY = (int) MathF.Floor((center.Y - half) / CellSize);
            var maxY = (int) MathF.Floor((center.Y + half) / CellSize);

            var keys = new List<long>();
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    keys.Add(ToKey(x, y));
                }
            }
            return keys;
        }

        private static bool AreSameKeys(List<long> left, List<long> right)
        {
            if (left.Count != right.Count) return false;
            for (var i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i]) return false;
            }
            return true;
        }

        private static long ToKey(int x, int y)
        {
            return ((long) x << 32) ^ (uint) y;
        }
    }
}
