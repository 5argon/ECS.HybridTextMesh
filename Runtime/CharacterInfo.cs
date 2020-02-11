using System;
using Unity.Mathematics;
using UnityEngine;

namespace E7.ECS.SpriteFont
{
    [Serializable]
    public struct CharacterInfo
    {
#pragma warning disable 0649
        [SerializeField] internal char character;
        [SerializeField] internal Metrics metrics;
        [SerializeField] internal Mesh meshForCharacter;
#pragma warning restore 0649
    }
}