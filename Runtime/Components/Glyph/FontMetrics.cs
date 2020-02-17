using System;
using Unity.Entities;
using UnityEngine;

namespace E7.ECS.HybridTextMesh
{
    /// <summary>
    /// This is on the font asset and also migrated to the parent of glyph entities
    /// using that font. In order so it could perform parallel work
    /// without needing to ask its assigned font asset (SCD).
    /// </summary>
    [Serializable]
    public struct FontMetrics : IComponentData
    {
        [SerializeField] internal float baseSize;
        [SerializeField] internal float ascent;
        [SerializeField] internal float descent;
        [SerializeField] internal float leading;

        internal float ScaledAscent => baseSize * ascent;
        internal float ScaledDescent => baseSize * descent;
        internal float ScaledLeading => baseSize * leading;

        internal float LineHeight => ScaledAscent + ScaledDescent + ScaledLeading;
    }
}