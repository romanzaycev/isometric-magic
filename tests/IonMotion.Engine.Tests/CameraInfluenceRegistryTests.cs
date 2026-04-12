using System.Numerics;
using IonMotion.Engine;

namespace IonMotion.Engine.Tests;

public sealed class CameraInfluenceRegistryTests
{
    [Fact]
    public void AddComponent_OnActiveEntity_IsCollected()
    {
        var scene = new Scene("test_scene");
        var entity = scene.CreateEntity("e");
        entity.AddComponent(new ProbeCameraInfluenceComponent());
        var buffer = new List<CameraInfluence>();

        scene.CollectCameraInfluences(buffer);

        Assert.Single(buffer);
        Assert.Equal(CameraInfluenceKind.SetCenter, buffer[0].Kind);
    }

    [Fact]
    public void EnabledToggle_UpdatesRegistryMembership()
    {
        var scene = new Scene("test_scene");
        var entity = scene.CreateEntity("e");
        var component = new ProbeCameraInfluenceComponent();
        entity.AddComponent(component);
        var buffer = new List<CameraInfluence>();

        component.Enabled = false;
        scene.CollectCameraInfluences(buffer);
        Assert.Empty(buffer);

        component.Enabled = true;
        scene.CollectCameraInfluences(buffer);
        Assert.Single(buffer);
    }

    [Fact]
    public void Destroy_RemovesComponentFromRegistry()
    {
        var scene = new Scene("test_scene");
        var entity = scene.CreateEntity("e");
        entity.AddComponent(new ProbeCameraInfluenceComponent());
        var buffer = new List<CameraInfluence>();

        entity.Destroy();
        scene.InternalUpdate();
        scene.CollectCameraInfluences(buffer);

        Assert.Empty(buffer);
    }

    [Fact]
    public void DetachFromScene_DoesNotLeaveStaleRegistration()
    {
        var scene = new Scene("test_scene");
        var parent = scene.CreateEntity("parent");
        var entity = scene.CreateEntity("e", parent);
        entity.AddComponent(new ProbeCameraInfluenceComponent());
        var buffer = new List<CameraInfluence>();

        entity.SetParent(null, canvasPositionStays: false);
        scene.CollectCameraInfluences(buffer);

        Assert.Empty(buffer);
    }

    private sealed class ProbeCameraInfluenceComponent : CameraInfluenceComponent
    {
        public override void CollectInfluence(List<CameraInfluence> buffer)
        {
            AddSetCenter(buffer, new Vector2(42f, 17f));
        }
    }
}
