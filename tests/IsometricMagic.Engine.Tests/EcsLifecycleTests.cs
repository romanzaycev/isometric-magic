using IsometricMagic.Engine;

namespace IsometricMagic.Engine.Tests;

public sealed class EcsLifecycleTests
{
    [Fact]
    public void AddComponent_AlreadyOwned_Throws()
    {
        var a = new Entity("a");
        var b = new Entity("b");
        var component = new ProbeComponent();
        a.AddComponent(component);

        Assert.Throws<InvalidOperationException>(() => b.AddComponent(component));
    }

    [Fact]
    public void AddComponent_OnEntityInScene_CallsAwakeAndEnable()
    {
        var scene = new Scene("test_scene");
        var entity = scene.CreateEntity("e");
        var component = new ProbeComponent();

        entity.AddComponent(component);

        Assert.Equal(1, component.AwakeCalls);
        Assert.Equal(1, component.EnableCalls);
    }

    [Fact]
    public void EnabledToggle_OnActiveEntity_CallsEnableDisableOnTransitions()
    {
        var scene = new Scene("test_scene");
        var entity = scene.CreateEntity("e");
        var component = new ProbeComponent();
        entity.AddComponent(component);

        component.Enabled = false;
        component.Enabled = false;
        component.Enabled = true;
        component.Enabled = true;

        Assert.Equal(2, component.EnableCalls);
        Assert.Equal(1, component.DisableCalls);
    }

    [Fact]
    public void EnabledToggle_OnInactiveEntity_DoesNotCallEnableDisable()
    {
        var scene = new Scene("test_scene");
        var entity = scene.CreateEntity("e");
        var component = new ProbeComponent();
        entity.AddComponent(component);

        entity.ActiveSelf = false;
        component.EnableCalls = 0;
        component.DisableCalls = 0;

        component.Enabled = false;
        component.Enabled = true;

        Assert.Equal(0, component.EnableCalls);
        Assert.Equal(0, component.DisableCalls);
    }

    [Fact]
    public void ActiveSelf_PropagatesToChildren_AndCallsEnableDisable()
    {
        var scene = new Scene("test_scene");
        var parent = scene.CreateEntity("parent");
        var child = scene.CreateEntity("child", parent);
        var component = new ProbeComponent();
        child.AddComponent(component);
        component.EnableCalls = 0;

        parent.ActiveSelf = false;
        parent.ActiveSelf = true;

        Assert.Equal(1, component.DisableCalls);
        Assert.Equal(1, component.EnableCalls);
        Assert.True(child.ActiveInHierarchy);
    }

    [Fact]
    public void Start_CalledOnce_UpdateAndLateUpdateCalledOnlyWhenActiveAndEnabled()
    {
        var scene = new Scene("test_scene");
        var entity = scene.CreateEntity("e");
        var component = new ProbeComponent();
        entity.AddComponent(component);

        scene.InternalUpdate();
        scene.InternalUpdate();

        Assert.Equal(1, component.StartCalls);
        Assert.Equal(2, component.UpdateCalls);
        Assert.Equal(2, component.LateUpdateCalls);

        component.Enabled = false;
        scene.InternalUpdate();

        Assert.Equal(2, component.UpdateCalls);
        Assert.Equal(2, component.LateUpdateCalls);
    }

    [Fact]
    public void Destroy_IsDeferredUntilDestroyQueueProcessing()
    {
        var scene = new Scene("test_scene");
        var entity = scene.CreateEntity("e");
        var component = new ProbeComponent();
        entity.AddComponent(component);

        entity.Destroy();
        Assert.Equal(0, component.DestroyCalls);

        scene.InternalUpdate();

        Assert.Equal(1, component.DestroyCalls);
        Assert.Empty(entity.Components);
    }

    [Fact]
    public void SetParent_PropagatesSceneRecursively()
    {
        var scene = new Scene("test_scene");
        var parent = scene.CreateEntity("parent");
        var child = new Entity("child");
        var grandChild = new Entity("grandChild");
        grandChild.SetParent(child, worldPositionStays: false);

        child.SetParent(parent, worldPositionStays: false);

        Assert.Same(scene, child.Scene);
        Assert.Same(scene, grandChild.Scene);
    }

    private sealed class ProbeComponent : Component
    {
        public int AwakeCalls;
        public int EnableCalls;
        public int DisableCalls;
        public int StartCalls;
        public int UpdateCalls;
        public int LateUpdateCalls;
        public int DestroyCalls;

        protected override void Awake() => AwakeCalls++;
        protected override void OnEnable() => EnableCalls++;
        protected override void OnDisable() => DisableCalls++;
        protected override void Start() => StartCalls++;
        protected override void Update(float dt) => UpdateCalls++;
        protected override void LateUpdate(float dt) => LateUpdateCalls++;
        protected override void OnDestroy() => DestroyCalls++;
    }
}
