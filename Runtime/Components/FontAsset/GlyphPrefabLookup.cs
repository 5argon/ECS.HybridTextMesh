using System;
using Unity.Collections;
using Unity.Entities;

namespace E7.ECS.HybridTextMesh
{
    /// <summary>
    /// Turn character into a prefab which when spawn display a glyph.
    /// </summary>
    internal struct GlyphPrefabLookup : ISharedComponentData, IEquatable<GlyphPrefabLookup>
    {
        internal NativeHashMap<char, Entity> characterToPrefabEntity;
        internal NativeHashMap<char, Entity> characterToPrefabEntityWithScale;

        public bool Equals(GlyphPrefabLookup other)
        {
            return characterToPrefabEntity.Equals(other.characterToPrefabEntity);
        }

        public override bool Equals(object obj)
        {
            return obj is GlyphPrefabLookup other && Equals(other);
        }

        public override int GetHashCode()
        {
            return characterToPrefabEntity.GetHashCode();
        }
    }
}