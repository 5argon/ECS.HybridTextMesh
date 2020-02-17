using System;
using Unity.Collections;
using Unity.Entities;

namespace E7.ECS.HybridTextMesh
{
    public struct FontAssetHolder : ISharedComponentData, IEquatable<FontAssetHolder>
    {
        internal HtmFontAsset htmFontAsset;

        public bool Equals(FontAssetHolder other)
        {
            return Equals(htmFontAsset, other.htmFontAsset);
        }

        public override bool Equals(object obj)
        {
            return obj is FontAssetHolder other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (htmFontAsset != null ? htmFontAsset.GetHashCode() : 0);
        }
    }
}