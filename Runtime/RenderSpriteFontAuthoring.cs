using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.SpriteFont
{
    [RequireComponent(typeof(RectTransform))]
    public class RenderSpriteFontAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
#pragma warning disable 0649
        [SerializeField] private string text;
        [SerializeField] internal SpriteFontAsset spriteFontAsset;
        [Space]
        [SerializeField] private int persistentCharacterEntities;
        [SerializeField] private TextStructure textStructure;
        [SerializeField] private TextTransform textTransform;
#pragma warning restore 0649

        void Reset()
        {
            GetComponent<RectTransform>().sizeDelta = new Vector2(10, 10);
            textTransform = new TextTransform
            {
                lineHeightMultiplier = 1
            };
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<TextTransformFixed>(entity);
            dstManager.AddBuffer<CharacterEntityGroup>(entity);

            for (int i = 0; i < persistentCharacterEntities; i++)
            {
                Entity persistentCharacter = conversionSystem.CreateAdditionalEntity(this.gameObject);
                dstManager.SetName(persistentCharacter, $"{this.name}_Char{i}");
                foreach (var type in ArchetypeCollection.CharacterTypes)
                {
                    dstManager.AddComponent(persistentCharacter, type);
                }

                dstManager.SetComponentData(persistentCharacter, new Parent {Value = entity});
            }

            dstManager.AddComponentData(entity, new Text512
            {
                text = text,
            });

            dstManager.AddComponentData<TextStructure>(entity, textStructure);

            var rt = this.GetComponent<RectTransform>();
            textTransform.rectSize = rt.sizeDelta;
            dstManager.AddComponentData<TextTransform>(entity, textTransform);

            dstManager.AddSharedComponentData<SpriteFontAssetHolder>(entity, new SpriteFontAssetHolder
            {
                spriteFontAsset = spriteFontAsset
            });
        }
    }
}