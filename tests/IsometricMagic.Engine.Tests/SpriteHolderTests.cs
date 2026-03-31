using IsometricMagic.Engine;

namespace IsometricMagic.Engine.Tests;

public sealed class SpriteHolderTests
{
    [Fact]
    public void Add_MaintainsAscendingSortingOrder_AndStableInsertForEqualSorting()
    {
        var holder = SpriteHolder.GetInstance();
        var tag = $"test_tag_{Guid.NewGuid():N}";

        var a = new Sprite { Sorting = 5 };
        var b = new Sprite { Sorting = 1 };
        var c = new Sprite { Sorting = 5 };

        holder.Add(a, tag);
        holder.Add(b, tag);
        holder.Add(c, tag);

        var sprites = holder.GetSprites(tag);
        Assert.Same(b, sprites[0]);
        Assert.Same(a, sprites[1]);
        Assert.Same(c, sprites[2]);

        holder.Remove(a);
        holder.Remove(b);
        holder.Remove(c);
    }

    [Fact]
    public void Add_SameSpriteSameTag_IsIdempotent()
    {
        var holder = SpriteHolder.GetInstance();
        var tag = $"test_tag_{Guid.NewGuid():N}";
        var sprite = new Sprite();

        holder.Add(sprite, tag);
        holder.Add(sprite, tag);

        Assert.Single(holder.GetSprites(tag));

        holder.Remove(sprite);
    }

    [Fact]
    public void Remove_RemovesSpriteFromAllTags()
    {
        var holder = SpriteHolder.GetInstance();
        var tagA = $"test_tag_a_{Guid.NewGuid():N}";
        var tagB = $"test_tag_b_{Guid.NewGuid():N}";
        var sprite = new Sprite();

        holder.Add(sprite, tagA);
        holder.Add(sprite, tagB);

        holder.Remove(sprite);

        Assert.Empty(holder.GetSprites(tagA));
        Assert.Empty(holder.GetSprites(tagB));
    }

    [Fact]
    public void TrySetReindex_ReordersWhenSortingChanges()
    {
        var holder = SpriteHolder.GetInstance();
        var tag = $"test_tag_{Guid.NewGuid():N}";

        var a = new Sprite { Sorting = 1 };
        var b = new Sprite { Sorting = 2 };
        var c = new Sprite { Sorting = 3 };

        holder.Add(a, tag);
        holder.Add(b, tag);
        holder.Add(c, tag);

        a.Sorting = 10;

        var sprites = holder.GetSprites(tag);
        Assert.Same(b, sprites[0]);
        Assert.Same(c, sprites[1]);
        Assert.Same(a, sprites[2]);

        holder.Remove(a);
        holder.Remove(b);
        holder.Remove(c);
    }

    [Fact]
    public void GetSprites_UnknownTag_ReturnsEmpty()
    {
        var holder = SpriteHolder.GetInstance();

        var sprites = holder.GetSprites($"missing_{Guid.NewGuid():N}");

        Assert.Empty(sprites);
    }
}
