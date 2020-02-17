using System;
using Unity.Mathematics;
using UnityEngine;

namespace E7.ECS.HybridTextMesh
{
    [Serializable]
    public struct CharacterInfo
    {
#pragma warning disable 0649
        [SerializeField] internal char character;
        [SerializeField] internal GlyphMetrics glyphMetrics;
        [SerializeField] internal Mesh meshForCharacter;
#pragma warning restore 0649
    }
}