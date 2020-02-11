using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace E7.ECS.SpriteFont
{
    [Serializable]
    public struct Metrics : IComponentData
    {
#pragma warning disable 0649
        [SerializeField] internal float2 uvOffset;
        [SerializeField] internal float2 size;
        [SerializeField] internal float2 bearing;
        [SerializeField] internal float2 advance;
#pragma warning restore 0649
    }
}