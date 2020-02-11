using Unity.Entities;
using Unity.Transforms;

namespace E7.ECS.SpriteFont
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    internal class SimulationGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    internal class ToTransformGroup : ComponentSystemGroup
    {
    }
}