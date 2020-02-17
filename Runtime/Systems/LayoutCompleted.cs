using Unity.Entities;

namespace E7.ECS.HybridTextMesh
{
    /// <summary>
    /// Remove this tag from glyph parent to force the layout again.
    /// </summary>
    public struct LayoutCompleted : IComponentData
    {
    }
}