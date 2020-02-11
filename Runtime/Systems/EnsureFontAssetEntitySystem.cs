using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace E7.ECS.SpriteFont
{
    /// <summary>
    /// For any <see cref="SpriteFontAsset"/> usage ensure an asset entity representing that
    /// font asset exist. Then we could make a lookup hash map and prefab per character.
    ///
    /// Has 1 frame delay.
    /// </summary>
    [UpdateInGroup(typeof(SimulationGroup))]
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
                ComponentType.ReadOnly<FontAsset>(),
                ComponentType.ReadOnly<SpriteFontAssetHolder>()
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
                .WithAll<Text512>()
                .WithNone<FontAssetEntityExistForThisText>()
                .ForEach((Entity e, SpriteFontAssetHolder sfah) =>
                {
                    var sfa = sfah.spriteFontAsset;
                    ecb.SetComponent(e, new TextTransformFixed
                    {
                        lineHeight = sfa.lineHeight
                    });

                    int sfaInstanceId = sfa.GetInstanceID();
                    //One SPA get only one prepare, so if there are something here don't do it anymore.
                    fontAssetQuery.SetSharedComponentFilter(sfah);
                    if (fontAssetQuery.CalculateChunkCount() == 0 && worked.IndexOf(sfaInstanceId) == -1)
                    {
                        Entity fontAssetEntity = ecb.CreateEntity(fontAssetArchetype);

                        ecb.SetSharedComponent(fontAssetEntity, sfah);
                        var buffer = ecb.SetBuffer<CharacterPrefabBuffer>(fontAssetEntity);
                        //Prepare prefabs for this asset.
                        for (int i = 0; i < sfa.characterInfos.Length; i++)
                        {
                            var c = sfa.characterInfos[i];
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

                            ecb.SetComponent(characterPrefab, c.metrics);
                            ecb.SetComponent(characterPrefabWithScale, c.metrics);

                            buffer.Add(new CharacterPrefabBuffer
                            {
                                character = c.character.ToString(),
                                prefab = characterPrefab,
                                prefabWithScale = characterPrefabWithScale
                            });
                        }

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
    }
}