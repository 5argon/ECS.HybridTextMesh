using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.HybridTextMesh
{
    [RequireComponent(typeof(RectTransform))]
    public class HybridTextMeshAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
#pragma warning disable 0649
        [Multiline][SerializeField] private string text;
        [SerializeField] internal HtmFontAsset htmFontAsset;
        [Space] [SerializeField] private int persistentCharacterEntities;
        [SerializeField] private TextStructure textStructure;
        [SerializeField] private TextTransform textTransform;
#pragma warning restore 0649

        void Reset()
        {
            GetComponent<RectTransform>().sizeDelta = new Vector2(10, 10);
            textTransform = new TextTransform
            {
                modifyLeading = 1
            };
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var rt = this.GetComponent<RectTransform>();
            textTransform.rect = rt.rect;
            dstManager.AddComponentData<TextTransform>(entity, textTransform);
            
            //Account for pivot, so pivot could be anywhere and not influencing starting position of glyphs.
            var shiftBack = rt.pivot;
            shiftBack.y = 1 - shiftBack.y;
            shiftBack *= rt.sizeDelta;
            var translation = dstManager.GetComponentData<Translation>(entity);
            translation.Value.x -= shiftBack.x;
            translation.Value.y += shiftBack.y;
            dstManager.SetComponentData<Translation>(entity, translation);
            
            dstManager.AddComponent<FontMetrics>(entity);

            var ea = new NativeArray<GlyphEntityGroup>(persistentCharacterEntities, Allocator.Temp);
            for (int i = 0; i < persistentCharacterEntities; i++)
            {
                Entity persistentCharacter = conversionSystem.CreateAdditionalEntity(this.gameObject);
#if UNITY_EDITOR
                dstManager.SetName(persistentCharacter, $"{this.name}_Char{i}");
#endif
                foreach (var type in ArchetypeCollection.CharacterTypes)
                {
                    dstManager.AddComponent(persistentCharacter, type);
                }

                ea[i] = new GlyphEntityGroup {character = persistentCharacter};

                //buffer.Add(new CharacterEntityGroup {character = persistentCharacter});
                dstManager.SetComponentData(persistentCharacter, new Parent {Value = entity});
            }

            var buffer = dstManager.AddBuffer<GlyphEntityGroup>(entity);
            buffer.AddRange(ea);

            dstManager.AddComponentData(entity, new TextContent
            {
                text = text,
            });

            textStructure.persistentCharacterEntityMode = persistentCharacterEntities > 0;
            dstManager.AddComponentData<TextStructure>(entity, textStructure);


            dstManager.AddSharedComponentData<FontAssetHolder>(entity, new FontAssetHolder
            {
                htmFontAsset = htmFontAsset
            });
        }
    }
}