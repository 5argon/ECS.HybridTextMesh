using Unity.Entities;

namespace E7.ECS.HybridTextMesh
{
    /// <summary>
    /// Similar to <see cref="LinkedEntityGroup"/> but used for iterating through
    /// all characters.
    /// </summary>
    [InternalBufferCapacity(16)]
    internal struct GlyphEntityGroup : IBufferElementData
    {
        internal Entity character;
    }
}