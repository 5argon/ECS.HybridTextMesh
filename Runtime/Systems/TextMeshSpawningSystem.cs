using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.SpriteFont
{
    internal struct TextManualDirty : IComponentData
    {
    }

    /// <summary>
    /// - One frame delay to see the meshes because of structural changes.
    /// - If glyph prefab not ready yet, try again next frame.
    /// - If successfully setup for hybrid renderer, tag so it won't setup again.
    /// - Tag will be removed on <see cref="Text512"/> changes so this would work again.
    /// </summary>
    [UpdateInGroup(typeof(SimulationGroup))]
    [UpdateAfter(typeof(EnsureFontAssetEntitySystem))]
    internal class TextMeshSpawningSystem : JobComponentSystem
    {
        BeginInitializationEntityCommandBufferSystem ecbs;
        EntityQuery fontAssetQuery;
        EntityQuery changedRegenerationQuery;
        FastEquality.TypeInfo textTypeInfo;
        EntityQuery initialGenerationQuery;
        FastEquality.TypeInfo structureTypeInfo;

        internal struct TextMeshSpawned : IComponentData
        {
        }

        internal struct TextDiffer : IComponentData
        {
            internal Text512 previousText;
            internal TextStructure previousStructure;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            ecbs = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            fontAssetQuery = GetEntityQuery(
                ComponentType.ReadOnly<FontAsset>(),
                ComponentType.ReadOnly<SpriteFontAssetHolder>(),
                ComponentType.ReadOnly<CharacterPrefabLookup>()
            );
            textTypeInfo = TypeManager.GetFastEqualityTypeInfo(TypeManager.GetTypeInfo<Text512>());
            structureTypeInfo = TypeManager.GetFastEqualityTypeInfo(TypeManager.GetTypeInfo<TextStructure>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (initialGenerationQuery.CalculateChunkCount() > 0 || changedRegenerationQuery.CalculateChunkCount() > 0)
            {
                inputDeps.Complete();

                var ecb = ecbs.CreateCommandBuffer();

                Entities
                    .WithName("Initial mesh generation")
                    .WithNone<TextMeshSpawned>().ForEach(
                        (Entity e, in Text512 t, in TextStructure ts, in SpriteFontAssetHolder holder,
                            in DynamicBuffer<CharacterEntityGroup> leg) =>
                        {
                            if (GenerateMeshes(EntityManager, fontAssetQuery, holder, t, ts, ecb, e, leg,
                                updateMode: ts.persistentCharacterEntity))
                            {
                                //Only add if managed to find the prefabs, so it could retry next frame.
                                ecb.AddComponent<TextMeshSpawned>(e);
                                ecb.AddComponent(e, new TextDiffer {previousText = t});
                            }
                        })
                    .WithStoreEntityQueryInField(ref initialGenerationQuery)
                    .WithoutBurst().Run();

                // Do the same but on every changes to text content, regardless of spawned status.
                // If fail, remove spawned tag so it retry like the first time.
                Entities
                    .WithName("Changed text regeneration")
                    .WithAll<TextMeshSpawned>()
                    .WithChangeFilter<Text512, TextStructure>()
                    .ForEach(
                        (Entity e, ref TextDiffer td, in Text512 t, in TextStructure ts,
                            in SpriteFontAssetHolder holder,
                            in DynamicBuffer<CharacterEntityGroup> leg) =>
                        {
                            //Cannot use JUST changed filter, on removing children transform system will
                            //cause archetype change and that would loop trigger change filter
                            //even though value didn't technically changed.

                            bool changedForReal1 =
                                FastEquality.Equals<Text512>(t, td.previousText, textTypeInfo) == false;
                            bool changedForReal2 =
                                FastEquality.Equals<TextStructure>(ts, td.previousStructure, textTypeInfo) == false;

                            if (changedForReal1 || changedForReal2)
                            {
                                //Destroy all existing text meshes next frame.

                                //If persistent mode, the generate meshes would just set data.
                                if (ts.persistentCharacterEntity == false)
                                {
                                    for (int i = 0; i < leg.Length; i++)
                                    {
                                        ecb.DestroyEntity(leg[i].character);
                                    }
                                }

                                if (!GenerateMeshes(EntityManager, fontAssetQuery, holder, t, ts, ecb, e, leg,
                                    updateMode: ts.persistentCharacterEntity))
                                {
                                    //This should make it retry with the above routine in response for this change.
                                    ecb.RemoveComponent<TextMeshSpawned>(e);
                                }
                                else
                                {
                                    //Re-layout and record change.
                                    ecb.RemoveComponent<TextMeshLayoutSystem.LayoutCompleted>(e);
                                    td.previousText = t;
                                    td.previousStructure = ts;
                                }
                            }
                        })
                    .WithStoreEntityQueryInField(ref changedRegenerationQuery)
                    .WithoutBurst().Run();
            }

            return default;
        }

        static bool GenerateMeshes(EntityManager em,
            EntityQuery fontAssetQuery,
            SpriteFontAssetHolder holder,
            Text512 t,
            TextStructure ts,
            EntityCommandBuffer ecb,
            Entity e,
            DynamicBuffer<CharacterEntityGroup> currentBuffer,
            bool updateMode
        )
        {
            fontAssetQuery.SetSharedComponentFilter(holder);
            if (fontAssetQuery.CalculateChunkCount() > 0)
            {
                var lookup =
                    em.GetSharedComponentData<CharacterPrefabLookup>(fontAssetQuery
                        .GetSingletonEntity());
                var nhm = ts.perCharacterScaling
                    ? lookup.characterToPrefabEntityWithScale
                    : lookup.characterToPrefabEntity;

                string s = t.text.ToString();

                //If destroy mode, the old ones in buffer would already be on their way to destroy.
                //New one would replace at the same time next frame at playback.
                if (!updateMode)
                {
                    var buffer = ecb.SetBuffer<CharacterEntityGroup>(e);
                    for (int i = 0; i < s.Length; i++)
                    {
                        char c = s[i];

                        if (nhm.TryGetValue(c, out Entity prefab))
                        {
                            Entity instantiated = ecb.Instantiate(prefab);
                            ecb.SetComponent(instantiated, new Parent {Value = e});
                            buffer.Add(new CharacterEntityGroup {character = instantiated});
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
                                ecb.SetComponent(existingCharacter, em.GetComponentData<CharacterUv>(prefab));
                                ecb.SetComponent(existingCharacter, em.GetComponentData<Metrics>(prefab));
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