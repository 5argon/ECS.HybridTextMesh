using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace E7.ECS.HybridTextMesh
{
    /// <summary>
    /// For any <see cref="HtmFontAsset"/> usage ensure an asset entity representing that
    /// font asset exist. Then we could make a lookup hash map and prefab per character.
    ///
    /// Has 1 frame delay.
    /// </summary>
    [UpdateInGroup(typeof(HybridTextMeshSimulationGroup))]
    internal class EnsureFontAssetEntitySystem : JobComponentSystem
    {
        EntityQuery fontAssetQuery;
        EntityQuery potentiallyNewSpriteFontAssetQuery;
        BeginInitializationEntityCommandBufferSystem ecbs;
        EntityArchetype characterWithPrefabArchetype;
        EntityArchetype fontAssetArchetype;

        struct FontAssetEntityExistForThisText : IComponentData
        {
        }

        protected override void OnCreate()
        {
            characterWithPrefabArchetype = EntityManager.CreateArchetype(
                ArchetypeCollection.CharacterTypes.Concat(new[] {ComponentType.ReadOnly<Prefab>()}).ToArray()
            );
            ecbs = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            fontAssetQuery = GetEntityQuery(
                ComponentType.ReadOnly<FontAssetEntity>(),
                ComponentType.ReadOnly<FontAssetHolder>()
            );
            fontAssetArchetype = EntityManager.CreateArchetype(
                ArchetypeCollection.FontAssetTypes
            );
            RequireForUpdate(potentiallyNewSpriteFontAssetQuery);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var ecb = ecbs.CreateCommandBuffer();
            var worked = new NativeList<int>(4, Allocator.Temp);
            Entities
                .WithAll<TextContent>()
                .WithNone<FontAssetEntityExistForThisText>()
                .ForEach((Entity e, FontAssetHolder sfah) =>
                {
                    var sfa = sfah.htmFontAsset;
                    ecb.SetComponent(e, sfa.fontMetrics);

                    int sfaInstanceId = sfa.GetInstanceID();
                    //One SPA get only one prepare, so if there are something here don't do it anymore.
                    fontAssetQuery.SetSharedComponentFilter(sfah);
                    if (fontAssetQuery.CalculateChunkCount() == 0 && worked.IndexOf(sfaInstanceId) == -1)
                    {
                        Entity fontAssetEntity = ecb.CreateEntity(fontAssetArchetype);

                        ecb.SetSharedComponent(fontAssetEntity, sfah);
                        var buffer = ecb.SetBuffer<GlyphPrefabBuffer>(fontAssetEntity);
                        //Prepare prefabs for this asset.
                        for (int i = 0; i < sfa.characterInfos.Length; i++)
                        {
                            CharacterInfo c = sfa.characterInfos[i];
                            RegisterCharacter(sfa, c, ecb, buffer);
                        }

                        RegisterCharacter(sfa, new CharacterInfo
                        {
                            character = '\n',
                        }, ecb, buffer, new SpecialCharacter {newLine = true});

                        //Prevents loading the same font in the same frame since ECB target
                        //the next frame.
                        worked.Add(sfaInstanceId);
                    }
                })
                .WithStoreEntityQueryInField(ref potentiallyNewSpriteFontAssetQuery)
                .WithoutBurst().Run();
            ecb.AddComponent<FontAssetEntityExistForThisText>(potentiallyNewSpriteFontAssetQuery);
            worked.Dispose();
            return default;
        }

        void RegisterCharacter(HtmFontAsset sfa,
            CharacterInfo c,
            EntityCommandBuffer ecb,
            DynamicBuffer<GlyphPrefabBuffer> buffer,
            SpecialCharacter specialCharacter = default)
        {
            Entity characterPrefab = ecb.CreateEntity(characterWithPrefabArchetype);
            Entity characterPrefabWithScale = ecb.CreateEntity(characterWithPrefabArchetype);
            ecb.AddComponent<NonUniformScale>(characterPrefabWithScale,
                new NonUniformScale {Value = new float3(1)});

            ecb.SetSharedComponent(characterPrefab, new RenderMesh
            {
                material = sfa.material,
                mesh = c.meshForCharacter,
            });
            ecb.SetSharedComponent(characterPrefabWithScale, new RenderMesh
            {
                material = sfa.material,
                mesh = c.meshForCharacter,
            });

            ecb.SetComponent(characterPrefab, c.glyphMetrics);
            ecb.SetComponent(characterPrefabWithScale, c.glyphMetrics);

            ecb.SetComponent(characterPrefab, specialCharacter);
            ecb.SetComponent(characterPrefabWithScale, specialCharacter);

            buffer.Add(new GlyphPrefabBuffer
            {
                character = c.character.ToString(),
                prefab = characterPrefab,
                prefabWithScale = characterPrefabWithScale
            });
        }
    }
}