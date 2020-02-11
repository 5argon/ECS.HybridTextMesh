using System;
using Unity.Collections;
using Unity.Entities;

namespace E7.ECS.SpriteFont
{
    public struct SpriteFontAssetHolder : ISharedComponentData, IEquatable<SpriteFontAssetHolder>
    {
        internal SpriteFontAsset spriteFontAsset;

        public bool Equals(SpriteFontAssetHolder other)
        {
            return Equals(spriteFontAsset, other.spriteFontAsset);
        }

        public override bool Equals(object obj)
        {
            return obj is SpriteFontAssetHolder other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (spriteFontAsset != null ? spriteFontAsset.GetHashCode() : 0);
        }
    }
}