using System;
using System.Collections.Generic;

namespace IsometricMagic.Engine
{
    class SpriteHolder
    {
        private sealed class TagBucket
        {
            public readonly List<Sprite> Sprites = new();
            public readonly Dictionary<Sprite, int> Indices = new();
        }

        private static readonly SpriteHolder Instance = new();
        private static readonly IReadOnlyList<Sprite> Empty = Array.Empty<Sprite>();
        private readonly Dictionary<string, TagBucket> _tags = new();
        private readonly Dictionary<Sprite, List<string>> _tagIndex = new();

        public static SpriteHolder GetInstance()
        {
            return Instance;
        }

        public void Add(Sprite sprite, string tag0)
        {
            if (!_tags.TryGetValue(tag0, out var bucket))
            {
                bucket = new TagBucket();
                _tags.Add(tag0, bucket);
            }

            if (bucket.Indices.ContainsKey(sprite))
            {
                return;
            }

            var insertIndex = FindInsertIndex(bucket.Sprites, sprite.Sorting);
            bucket.Sprites.Insert(insertIndex, sprite);
            for (var i = insertIndex; i < bucket.Sprites.Count; i++)
            {
                bucket.Indices[bucket.Sprites[i]] = i;
            }

            if (!_tagIndex.TryGetValue(sprite, out var tags))
            {
                tags = new List<string>();
                _tagIndex[sprite] = tags;
            }

            tags.Add(tag0);
        }

        public IReadOnlyList<Sprite> GetSprites(string tag0)
        {
            if (!_tags.TryGetValue(tag0, out var bucket)) return Empty;
            return bucket.Sprites;
        }

        public void Remove(Sprite sprite)
        {
            if (!_tagIndex.TryGetValue(sprite, out var tags)) return;

            foreach (var tag in tags)
            {
                if (!_tags.TryGetValue(tag, out var bucket)) continue;
                if (!bucket.Indices.TryGetValue(sprite, out var index)) continue;

                bucket.Sprites.RemoveAt(index);
                bucket.Indices.Remove(sprite);

                for (var i = index; i < bucket.Sprites.Count; i++)
                {
                    bucket.Indices[bucket.Sprites[i]] = i;
                }
            }

            _tagIndex.Remove(sprite);
        }

        public void TrySetReindex(Sprite sprite, int oldSorting, int newSorting)
        {
            if (!_tagIndex.TryGetValue(sprite, out var tags)) return;
            if (oldSorting == newSorting) return;

            foreach (var tag in tags)
            {
                if (!_tags.TryGetValue(tag, out var bucket)) continue;
                if (!bucket.Indices.TryGetValue(sprite, out var index)) continue;

                if (newSorting > oldSorting)
                {
                    while (index + 1 < bucket.Sprites.Count && Compare(bucket.Sprites[index], bucket.Sprites[index + 1]) > 0)
                    {
                        Swap(bucket, index, index + 1);
                        index++;
                    }
                }
                else
                {
                    while (index - 1 >= 0 && Compare(bucket.Sprites[index - 1], bucket.Sprites[index]) > 0)
                    {
                        Swap(bucket, index - 1, index);
                        index--;
                    }
                }
            }
        }

        private static int Compare(Sprite a, Sprite b)
        {
            if (a.Sorting == b.Sorting) return 0;
            return a.Sorting < b.Sorting ? -1 : 1;
        }

        private static void Swap(TagBucket bucket, int indexA, int indexB)
        {
            var tmp = bucket.Sprites[indexA];
            bucket.Sprites[indexA] = bucket.Sprites[indexB];
            bucket.Sprites[indexB] = tmp;
            bucket.Indices[bucket.Sprites[indexA]] = indexA;
            bucket.Indices[bucket.Sprites[indexB]] = indexB;
        }

        private static int FindInsertIndex(List<Sprite> sprites, int sorting)
        {
            var low = 0;
            var high = sprites.Count;
            while (low < high)
            {
                var mid = (low + high) / 2;
                if (sprites[mid].Sorting <= sorting)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }
            return low;
        }
    }
}
