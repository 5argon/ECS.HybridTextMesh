using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace E7.ECS.HybridTextMesh
{
    /// <summary>
    /// For use in universal mesh mode, which is not supported yet.
    /// </summary>
    [MaterialProperty("_UnlitColorMap_ST", MaterialPropertyFormat.Float4)]
    internal struct GlyphUv : IComponentData
    {
#pragma warning disable 0649
        internal float4 uvValue;
#pragma warning restore 0649
    }
}