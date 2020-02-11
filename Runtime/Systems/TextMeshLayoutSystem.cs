using System;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.SpriteFont
{
    /// <summary>
    /// Each top parent is responsible to position all its children.
    /// After this is done once, it is all up to transform system every frame.
    /// </summary>
    [UpdateInGroup(typeof(ToTransformGroup))]
    internal class TextMeshLayoutSystem : JobComponentSystem
    {
        EntityQuery notLayoutYetQuery;
        BeginInitializationEntityCommandBufferSystem ecbs;
        EntityQuery layoutAgainQuery;

        public struct LayoutCompleted : IComponentData
        {
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            ecbs = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var ecb = ecbs.CreateCommandBuffer();
            var TranslationCdfe = GetComponentDataFromEntity<Translation>(isReadOnly: false);
            var ScaleCdfe = GetComponentDataFromEntity<NonUniformScale>(isReadOnly: false);
            var MetricsCdfe = GetComponentDataFromEntity<Metrics>(isReadOnly: true);

            var jh = Entities
                .WithReadOnly(MetricsCdfe)
                .WithNativeDisableParallelForRestriction(TranslationCdfe)
                .WithNone<LayoutCompleted>()
                .WithAll<TextMeshSpawningSystem.TextMeshSpawned>()
                .ForEach((Entity e, in DynamicBuffer<CharacterEntityGroup> leg, in TextTransform tt,
                    in TextTransformFixed ttf) =>
                {
                    //Debug.Log($"Layout");
                    Layout(e, tt, ttf, leg, TranslationCdfe, MetricsCdfe);
                })
                .WithStoreEntityQueryInField(ref notLayoutYetQuery)
                .Schedule(inputDeps);
            ecb.AddComponent<LayoutCompleted>(notLayoutYetQuery);

            var jh2 = Entities
                .WithReadOnly(MetricsCdfe)
                .WithNativeDisableParallelForRestriction(TranslationCdfe)
                .WithChangeFilter<TextTransform>()
                .WithAll<LayoutCompleted>()
                .WithAll<TextMeshSpawningSystem.TextMeshSpawned>()
                .ForEach((Entity e, in DynamicBuffer<CharacterEntityGroup> leg, in TextTransform tt,
                    in TextTransformFixed ttf) =>
                {
                    //Debug.Log($"Layout Changed");
                    Layout(e, tt, ttf, leg, TranslationCdfe, MetricsCdfe);
                })
                .WithStoreEntityQueryInField(ref layoutAgainQuery)
                .Schedule(jh);

            //Also create a job that redo on layout updates.

            return JobHandle.CombineDependencies(jh, jh2);
        }

        static void Layout(
            Entity head,
            TextTransform tt, TextTransformFixed ttf,
            DynamicBuffer<CharacterEntityGroup> leg,
            ComponentDataFromEntity<Translation> TranslationCdfe, ComponentDataFromEntity<Metrics> MetricsCdfe)
        {
            float lineSize = tt.rectSize.x;

            bool lineHasCharacter = false;
            float cumulativeX = 0;
            float yNow = 0;
            int beginOfLineCharacterIndex = 0;

            for (int i = 0; i < leg.Length; i++)
            {
                Entity c = leg[i].character;
                if (c == head) continue;
                Translation translation = TranslationCdfe[c];
                Metrics metrics = MetricsCdfe[c];
                float sizeToAdd = metrics.size.x + tt.tracking;

                if (cumulativeX + sizeToAdd > lineSize && lineHasCharacter)
                {
                    //New line, time to offset this line before going ahead.
                    LineAlign(tt, leg, TranslationCdfe, beginOfLineCharacterIndex, i, cumulativeX);

                    cumulativeX = 0;
                    lineHasCharacter = false;
                    yNow -= ttf.lineHeight * tt.lineHeightMultiplier;
                    beginOfLineCharacterIndex = i;
                    //Retry this character next line.
                    i--;
                    continue;
                }
                else
                {
                    translation.Value = new float3(cumulativeX, yNow, 0);
                    cumulativeX += sizeToAdd;
                    TranslationCdfe[c] = translation;
                    lineHasCharacter = true;
                }
            }

            //For the last line
            LineAlign(tt, leg, TranslationCdfe, beginOfLineCharacterIndex, leg.Length, cumulativeX);
        }

        static void LineAlign(TextTransform tt, DynamicBuffer<CharacterEntityGroup> leg,
            ComponentDataFromEntity<Translation> TranslationCdfe, int beginOfLineCharacterIndex, int indexNewLine,
            float cumulativeX)
        {
            if (tt.textAlignment != TextAlignment.Left)
            {
                for (int j = beginOfLineCharacterIndex; j < indexNewLine; j++)
                {
                    Entity prev = leg[j].character;
                    var prevTrans = TranslationCdfe[prev];
                    switch (tt.textAlignment)
                    {
                        case TextAlignment.Center:
                            prevTrans.Value.x -= cumulativeX / 2f;
                            break;
                        case TextAlignment.Right:
                            //Entity lastCharacterInLine = leg[i - 1].Value;
                            prevTrans.Value.x -= cumulativeX; //+ MetricsCdfe[lastCharacterInLine].size.x;
                            break;
                    }

                    TranslationCdfe[prev] = prevTrans;
                }
            }
        }
    }
}