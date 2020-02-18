using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.HybridTextMesh
{
    /// <summary>
    /// - One frame delay to see the meshes because of structural changes.
    /// - If glyph prefab not ready yet, try again next frame.
    /// - If successfully setup for hybrid renderer, tag so it won't setup again.
    /// - Tag will be removed on <see cref="TextContent"/> changes so this would work again.
    /// </summary>
    [UpdateInGroup(typeof(HybridTextMeshSimulationGroup))]
    [UpdateAfter(typeof(EnsureFontAssetEntitySystem))]
    internal class GlyphSpawningSystem : SystemBase
    {
        BeginInitializationEntityCommandBufferSystem ecbs;
        EntityQuery fontAssetQuery;
        EntityQuery changedRegenerationQuery;
        FastEquality.TypeInfo textTypeInfo;
        EntityQuery initialGenerationQuery;
        FastEquality.TypeInfo structureTypeInfo;

        internal struct GlyphSpawned : IComponentData
        {
        }

        internal struct TextDiffer : IComponentData
        {
            internal TextContent previousText;
            internal TextStructure previousStructure;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            ecbs = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            fontAssetQuery = GetEntityQuery(
                ComponentType.ReadOnly<FontAssetEntity>(),
                ComponentType.ReadOnly<FontAssetHolder>(),
                ComponentType.ReadOnly<GlyphPrefabLookup>()
            );
            textTypeInfo = TypeManager.GetFastEqualityTypeInfo(TypeManager.GetTypeInfo<TextContent>());
            structureTypeInfo = TypeManager.GetFastEqualityTypeInfo(TypeManager.GetTypeInfo<TextStructure>());
        }

        protected override void OnUpdate()
        {
            if (initialGenerationQuery.CalculateChunkCount() > 0 || changedRegenerationQuery.CalculateChunkCount() > 0)
            {
                var ecb = ecbs.CreateCommandBuffer();

                Entities
                    .WithName("InitialMeshGeneration")
                    .WithNone<GlyphSpawned>().ForEach(
                        (Entity e, in TextContent t, in TextStructure ts, in FontAssetHolder holder,
                            in DynamicBuffer<GlyphEntityGroup> leg) =>
                        {
                            if (GenerateMeshes(EntityManager, fontAssetQuery, holder, t, ts, ecb, e, leg,
                                updateMode: ts.persistentCharacterEntityMode))
                            {
                                //Only add if managed to find the prefabs, so it could retry next frame.
                                ecb.AddComponent<GlyphSpawned>(e);
                                ecb.AddComponent(e, new TextDiffer
                                {
                                    previousText = t,
                                    previousStructure = ts,
                                });
                            }
                        })
                    .WithStoreEntityQueryInField(ref initialGenerationQuery)
                    .WithoutBurst().Run();

                // Do the same but on every changes to text content, regardless of spawned status.
                // If fail, remove spawned tag so it retry like the first time.
                Entities
                    .WithName("ChangedTextRegeneration")
                    .WithAll<GlyphSpawned>()
                    .WithChangeFilter<TextContent, TextStructure>()
                    .ForEach(
                        (Entity e, ref TextDiffer td, in TextContent t, in TextStructure ts,
                            in FontAssetHolder holder,
                            in DynamicBuffer<GlyphEntityGroup> leg) =>
                        {
                            //Cannot use JUST changed filter, on removing children transform system will
                            //cause archetype change and that would loop trigger change filter
                            //even though value didn't technically changed.

                            bool changedForReal1 =
                                FastEquality.Equals<TextContent>(t, td.previousText, textTypeInfo) == false;
                            bool changedForReal2 =
                                FastEquality.Equals<TextStructure>(ts, td.previousStructure, structureTypeInfo) ==
                                false;

                            if (changedForReal1 || changedForReal2)
                            {
                                //Destroy all existing text meshes next frame.

                                //If persistent mode, the generate meshes would just set data.
                                if (ts.persistentCharacterEntityMode == false)
                                {
                                    for (int i = 0; i < leg.Length; i++)
                                    {
                                        ecb.DestroyEntity(leg[i].character);
                                    }
                                }

                                if (!GenerateMeshes(EntityManager, fontAssetQuery, holder, t, ts, ecb, e, leg,
                                    updateMode: ts.persistentCharacterEntityMode))
                                {
                                    //This should make it retry with the above routine in response for this change.
                                    ecb.RemoveComponent<GlyphSpawned>(e);
                                }
                                else
                                {
                                    //Re-layout and record change.
                                    ecb.RemoveComponent<LayoutCompleted>(e);
                                    td.previousText = t;
                                    td.previousStructure = ts;
                                }
                            }
                        })
                    .WithStoreEntityQueryInField(ref changedRegenerationQuery)
                    .WithoutBurst().Run();
            }
        }

        static bool GenerateMeshes(EntityManager em,
            EntityQuery fontAssetQuery,
            FontAssetHolder holder,
            TextContent t,
            TextStructure ts,
            EntityCommandBuffer ecb,
            Entity e,
            DynamicBuffer<GlyphEntityGroup> currentBuffer,
            bool updateMode
        )
        {
            fontAssetQuery.SetSharedComponentFilter(holder);
            if (fontAssetQuery.CalculateChunkCount() > 0)
            {
                var lookup =
                    em.GetSharedComponentData<GlyphPrefabLookup>(fontAssetQuery
                        .GetSingletonEntity());
                var nhm = ts.perCharacterScalingMode
                    ? lookup.characterToPrefabEntityWithScale
                    : lookup.characterToPrefabEntity;

                string s = t.text.ToString();

                //If destroy mode, the old ones in buffer would already be on their way to destroy.
                //New one would replace at the same time next frame at playback.
                if (!updateMode)
                {
                    var buffer = ecb.SetBuffer<GlyphEntityGroup>(e);
                    for (int i = 0; i < s.Length; i++)
                    {
                        char c = s[i];

                        if (nhm.TryGetValue(c, out Entity prefab))
                        {
                            Entity instantiated = ecb.Instantiate(prefab);
                            ecb.SetComponent(instantiated, new Parent {Value = e});
                            buffer.Add(new GlyphEntityGroup {character = instantiated});
                        }

                        //If no prefab, they simply disappear?
                    }
                }
                else
                {
                    //Character capped at available entities in the buffer.
                    for (
                        int i = 0, counter = 0;
                        i < currentBuffer.Length;
                        i++)
                    {
                        var existingCharacter = currentBuffer[i].character;
                        if (existingCharacter == e)
                        {
                            //A LEG may contains itself, skip that.
                            continue;
                        }

                        if (counter < s.Length)
                        {
                            char c = s[counter];

                            if (nhm.TryGetValue(c, out Entity prefab))
                            {
                                //Manually copy from prefab instead of instantiation
                                ecb.SetSharedComponent(existingCharacter,
                                    em.GetSharedComponentData<RenderMesh>(prefab));
                                ecb.SetComponent(existingCharacter, em.GetComponentData<GlyphUv>(prefab));
                                ecb.SetComponent(existingCharacter, em.GetComponentData<GlyphMetrics>(prefab));
                            }
                        }
                        else
                        {
                            //When buffer has more than string length, set all remaining to make them disappear.
                            ecb.SetSharedComponent(existingCharacter, default(RenderMesh));
                        }

                        counter++;
                    }
                }

                fontAssetQuery.ResetFilter();
                return true;
            }

            fontAssetQuery.ResetFilter();
            return false;
        }
    }
}