using System;
using Unity.Collections;
using Unity.Entities;

namespace E7.ECS.SpriteFont
{
    internal struct CharacterPrefabLookup : ISharedComponentData, IEquatable<CharacterPrefabLookup>
    {
        internal NativeHashMap<char, Entity> characterToPrefabEntity;
        internal NativeHashMap<char, Entity> characterToPrefabEntityWithScale;

        public bool Equals(CharacterPrefabLookup other)
        {
            return characterToPrefabEntity.Equals(other.characterToPrefabEntity);
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterPrefabLookup other && Equals(other);
        }

        public override int GetHashCode()
        {
            return characterToPrefabEntity.GetHashCode();
        }
    }
}