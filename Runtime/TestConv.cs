using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.SpriteFont
{
    internal class TestConv : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float4 uv;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new CharacterUv {uvValue = uv});
        }
    }
}