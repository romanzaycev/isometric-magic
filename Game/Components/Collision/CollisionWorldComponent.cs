using System;
using System.Collections.Generic;
using System.Numerics;
using IsometricMagic.Engine;

namespace IsometricMagic.Game.Components.Collision
{
    public class CollisionWorldComponent : Component
    {
        public int CellSize { get; set; } = 128;
        public float ContactSkin { get; set; } = 0.1f;

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
                    var combined = radius + collider.Radius - ContactSkin;
                    if (combined <= 0f)
                    {
                        continue;
                    }

                    if (Vector2.DistanceSquared(center, otherCenter) < combined * combined)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetOverlap(Vector2 center, float radius, WorldColliderComponent? ignore,
            out WorldColliderComponent? hit, out Vector2 hitCenter, out Vector2 normal, out float penetration)
        {
            hit = null;
            hitCenter = Vector2.Zero;
            normal = Vector2.Zero;
            penetration = 0f;

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
                    var combined = radius + collider.Radius - ContactSkin;
                    if (combined <= 0f)
                    {
                        continue;
                    }

                    var delta = center - otherCenter;
                    var distSq = delta.LengthSquared();
                    if (distSq >= combined * combined)
                    {
                        continue;
                    }

                    var dist = MathF.Sqrt(MathF.Max(0f, distSq));
                    var currentPenetration = combined - dist;
                    if (currentPenetration <= penetration) continue;

                    hit = collider;
                    hitCenter = otherCenter;
                    penetration = currentPenetration;

                    if (dist > 0.00001f)
                    {
                        normal = delta / dist;
                    }
                    else
                    {
                        normal = Vector2.UnitX;
                    }
                }
            }

            return hit != null;
        }

        public bool CastCircle(Vector2 startCenter, Vector2 delta, float radius, WorldColliderComponent? ignore,
            out WorldColliderComponent? hit, out float t, out Vector2 normal)
        {
            hit = null;
            t = 1f;
            normal = Vector2.Zero;

            var deltaLenSq = delta.LengthSquared();
            if (deltaLenSq < 0.000001f)
            {
                return false;
            }

            var deltaLen = MathF.Sqrt(deltaLenSq);
            var queryCenter = startCenter + delta * 0.5f;
            var queryRadius = deltaLen * 0.5f + radius + _maxRadius;
            var keys = GetCellKeys(queryCenter, queryRadius);
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
                    var combined = radius + collider.Radius - ContactSkin;
                    if (combined <= 0f)
                    {
                        continue;
                    }

                    var m = startCenter - otherCenter;
                    var c = Vector2.Dot(m, m) - combined * combined;
                    if (c <= 0f)
                    {
                        hit = collider;
                        t = 0f;
                        var len = m.Length();
                        if (len > 0.00001f)
                        {
                            normal = m / len;
                        }
                        else
                        {
                            normal = Vector2.UnitX;
                        }
                        return true;
                    }

                    var b = 2f * Vector2.Dot(m, delta);
                    var disc = b * b - 4f * deltaLenSq * c;
                    if (disc < 0f)
                    {
                        continue;
                    }

                    var sqrtDisc = MathF.Sqrt(disc);
                    var inv = 0.5f / deltaLenSq;
                    var t0 = (-b - sqrtDisc) * inv;
                    if (t0 < 0f || t0 > 1f)
                    {
                        continue;
                    }

                    if (t0 < t)
                    {
                        t = t0;
                        hit = collider;
                        var contact = startCenter + delta * t;
                        var n = contact - otherCenter;
                        var nLen = n.Length();
                        if (nLen > 0.00001f)
                        {
                            normal = n / nLen;
                        }
                        else
                        {
                            normal = Vector2.UnitX;
                        }
                    }
                }
            }

            return hit != null;
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
