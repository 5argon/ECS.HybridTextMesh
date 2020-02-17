using Unity.Collections;
using Unity.Entities;

namespace E7.ECS.HybridTextMesh
{
    /// <summary>
    /// Only exist temporarily to create a lookup next frame.
    /// </summary>
    [InternalBufferCapacity(128)]
    internal struct GlyphPrefabBuffer : IBufferElementData
    {
        internal NativeString32 character;
        internal Entity prefab;
        internal Entity prefabWithScale;
    }
}