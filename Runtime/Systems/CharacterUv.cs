using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace E7.ECS.SpriteFont
{
    /// <summary>
    /// For use in single mesh mode, which is not supported yet.
    /// </summary>
    [MaterialProperty("_UnlitColorMap_ST", MaterialPropertyFormat.Float4)]
    internal struct CharacterUv : IComponentData
    {
        internal float4 uvValue;
    }
}