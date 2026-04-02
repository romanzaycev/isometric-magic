namespace IsometricMagic.Engine.Tests;

public sealed class ComponentUpdateOrderTests
{
    [Fact]
    public void InternalUpdate_ExecutesByGroupThenOrder_ForUpdateAndLateUpdate()
    {
        var scene = new Scene("test_scene");
        var entity = scene.CreateEntity("e");
        var calls = new List<string>();

        entity.AddComponent(new OrderedProbeComponent("default_20_a", calls, ComponentUpdateGroup.Default, 20));
        entity.AddComponent(new OrderedProbeComponent("critical_5", calls, ComponentUpdateGroup.Critical, 5));
        entity.AddComponent(new OrderedProbeComponent("early_0", calls, ComponentUpdateGroup.Early, 0));
        entity.AddComponent(new OrderedProbeComponent("default_10", calls, ComponentUpdateGroup.Default, 10));
        entity.AddComponent(new OrderedProbeComponent("late_0", calls, ComponentUpdateGroup.Late, 0));
        entity.AddComponent(new OrderedProbeComponent("critical_0", calls, ComponentUpdateGroup.Critical, 0));
        entity.AddComponent(new OrderedProbeComponent("default_20_b", calls, ComponentUpdateGroup.Default, 20));

        scene.InternalUpdate();

        Assert.Equal(
            [
                "U:critical_0",
                "U:critical_5",
                "U:early_0",
                "U:default_10",
                "U:default_20_a",
                "U:default_20_b",
                "U:late_0",
                "L:critical_0",
                "L:critical_5",
                "L:early_0",
                "L:default_10",
                "L:default_20_a",
                "L:default_20_b",
                "L:late_0"
            ],
            calls
        );
    }

    private sealed class OrderedProbeComponent : Component
    {
        private readonly string _name;
        private readonly List<string> _calls;
        private readonly ComponentUpdateGroup _group;
        private readonly int _order;

        public OrderedProbeComponent(string name, List<string> calls, ComponentUpdateGroup group, int order)
        {
            _name = name;
            _calls = calls;
            _group = group;
            _order = order;
        }

        public override ComponentUpdateGroup UpdateGroup => _group;

        public override int UpdateOrder => _order;

        protected override void Update(float dt)
        {
            _calls.Add($"U:{_name}");
        }

        protected override void LateUpdate(float dt)
        {
            _calls.Add($"L:{_name}");
        }
    }
}
