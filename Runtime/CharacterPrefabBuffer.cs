using Unity.Collections;
using Unity.Entities;

namespace E7.ECS.SpriteFont
{
    /// <summary>
    /// Only exist temporarily to create a lookup next frame.
    /// </summary>
    [InternalBufferCapacity(128)]
    internal struct CharacterPrefabBuffer : IBufferElementData
    {
        internal NativeString32 character;
        internal Entity prefab;
        internal Entity prefabWithScale;
    }
}