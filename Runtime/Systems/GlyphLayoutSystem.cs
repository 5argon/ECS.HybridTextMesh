using System;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.HybridTextMesh
{
    /// <summary>
    /// Each top parent is responsible to position all its children.
    /// After this is done once, it is all up to transform system every frame.
    /// </summary>
    [UpdateInGroup(typeof(HybridTextMeshToTransformGroup))]
    internal class GlyphLayoutSystem : SystemBase
    {
        EntityQuery notLayoutYetQuery;
        BeginInitializationEntityCommandBufferSystem ecbs;
        EntityQuery layoutAgainQuery;


        protected override void OnCreate()
        {
            base.OnCreate();
            ecbs = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = ecbs.CreateCommandBuffer();
            var TranslationCdfe = GetComponentDataFromEntity<Translation>(isReadOnly: false);
            var ScaleCdfe = GetComponentDataFromEntity<NonUniformScale>(isReadOnly: false);
            var MetricsCdfe = GetComponentDataFromEntity<GlyphMetrics>(isReadOnly: true);
            var SpecialCharacterCdfe = GetComponentDataFromEntity<SpecialCharacter>(isReadOnly: true);

            Dependency = Entities
                .WithReadOnly(MetricsCdfe)
                .WithReadOnly(SpecialCharacterCdfe)
                .WithNativeDisableParallelForRestriction(TranslationCdfe)
                .WithNone<LayoutCompleted>()
                .WithAll<GlyphSpawningSystem.GlyphSpawned>()
                .ForEach((Entity e, in DynamicBuffer<GlyphEntityGroup> leg, in TextTransform tt,
                    in FontMetrics ttf) =>
                {
                    //Debug.Log($"Layout");
                    Layout(e, tt, ttf, leg, TranslationCdfe, MetricsCdfe, SpecialCharacterCdfe);
                })
                .WithStoreEntityQueryInField(ref notLayoutYetQuery)
                .ScheduleParallel(Dependency);
            ecb.AddComponent<LayoutCompleted>(notLayoutYetQuery);

            Dependency = Entities
                .WithReadOnly(MetricsCdfe)
                .WithReadOnly(SpecialCharacterCdfe)
                .WithNativeDisableParallelForRestriction(TranslationCdfe)
                .WithChangeFilter<TextTransform>()
                .WithAll<LayoutCompleted>()
                .WithAll<GlyphSpawningSystem.GlyphSpawned>()
                .ForEach((Entity e, in DynamicBuffer<GlyphEntityGroup> leg, in TextTransform tt,
                    in FontMetrics ttf) =>
                {
                    //Debug.Log($"Layout Changed");
                    Layout(e, tt, ttf, leg, TranslationCdfe, MetricsCdfe, SpecialCharacterCdfe);
                })
                .WithStoreEntityQueryInField(ref layoutAgainQuery)
                .ScheduleParallel(Dependency);
        }

        static void Layout(
            Entity head,
            TextTransform tt, FontMetrics ttf,
            DynamicBuffer<GlyphEntityGroup> leg,
            ComponentDataFromEntity<Translation> TranslationCdfe,
            ComponentDataFromEntity<GlyphMetrics> MetricsCdfe,
            ComponentDataFromEntity<SpecialCharacter> SpecialCharacterCdfe)
        {
            float lineSize = tt.rect.width;

            bool lineHasCharacter = false;
            float xNow = 0;
            float cumulativeX = 0;
            float rectOffset = -ttf.ScaledDescent - ttf.ScaledAscent;
            float yNow = rectOffset;
            float cumulativeY = 0;
            float verticalMove = ttf.LineHeight + tt.modifyLeading;
            int beginOfLineCharacterIndex = 0;
            float afterGlyphAdvance = 0;

            for (int i = 0; i < leg.Length; i++)
            {
                Entity c = leg[i].character;
                if (c == head) continue;
                Translation translation = TranslationCdfe[c];
                GlyphMetrics glyphMetrics = MetricsCdfe[c];

                xNow -= glyphMetrics.texturePaddings.w;

                float compensatePadding = (glyphMetrics.texturePaddings.w + glyphMetrics.texturePaddings.y);
                float glyphAdvance = glyphMetrics.size.x - compensatePadding;

                bool forceNewLine = SpecialCharacterCdfe[c].newLine;

                if (forceNewLine ||
                    xNow + glyphMetrics.texturePaddings.w + glyphAdvance > lineSize &&
                    lineHasCharacter)
                {
                    //New line, time to offset this line before going ahead.
                    LineHorizontalAlign(tt, leg, TranslationCdfe, beginOfLineCharacterIndex, i,
                        cumulativeX - afterGlyphAdvance);

                    xNow = 0;
                    cumulativeX = 0;
                    lineHasCharacter = false;
                    yNow -= verticalMove;
                    cumulativeY += verticalMove;
                    beginOfLineCharacterIndex = i;

                    //Retry this character next line, except if it is a new line character.
                    if (!forceNewLine)
                    {
                        i--;
                    }

                    continue;
                }
                else
                {
                    translation.Value = new float3(
                        xNow,
                        yNow - glyphMetrics.texturePaddings.z, 0);

                    float allAdvance = glyphAdvance + tt.tracking;
                    allAdvance = tt.monospace ? math.max(tt.monospaceWidth, allAdvance) : allAdvance;
                    afterGlyphAdvance = allAdvance - glyphAdvance;

                    xNow += glyphMetrics.texturePaddings.w + allAdvance;
                    cumulativeX += allAdvance;
                    TranslationCdfe[c] = translation;
                    lineHasCharacter = true;
                }
            }

            //For the last line
            LineHorizontalAlign(tt, leg, TranslationCdfe, beginOfLineCharacterIndex, leg.Length,
                cumulativeX - afterGlyphAdvance);

            //Vertical alignment, loop through all characters again with total Y height knowledge.
            if (tt.textAlignmentVertical != TextVerticalAlignment.Top)
            {
                for (int i = 0; i < leg.Length; i++)
                {
                    Entity c = leg[i].character;
                    if (c == head) continue;
                    Translation translation = TranslationCdfe[c];
                    switch (tt.textAlignmentVertical)
                    {
                        case TextVerticalAlignment.Middle:
                            translation.Value.y -= ((tt.rect.height / 2f) + (rectOffset / 2f) - (cumulativeY / 2f));
                            break;
                        case TextVerticalAlignment.Bottom:
                            translation.Value.y -= ((tt.rect.height + rectOffset) - (cumulativeY));
                            break;
                    }

                    TranslationCdfe[c] = translation;
                }
            }
        }

        static void LineHorizontalAlign(
            TextTransform tt,
            DynamicBuffer<GlyphEntityGroup> leg,
            ComponentDataFromEntity<Translation> TranslationCdfe,
            int beginOfLineCharacterIndex,
            int indexNewLine,
            float totalEffectiveWidth
        )
        {
            if (tt.textAlignmentHorizontal != TextAlignment.Left)
            {
                for (int j = beginOfLineCharacterIndex; j < indexNewLine; j++)
                {
                    Entity prev = leg[j].character;
                    var prevTrans = TranslationCdfe[prev];
                    switch (tt.textAlignmentHorizontal)
                    {
                        case TextAlignment.Center:
                            prevTrans.Value.x -= totalEffectiveWidth / 2f;
                            prevTrans.Value.x += tt.rect.width / 2f;
                            break;
                        case TextAlignment.Right:
                            //Entity lastCharacterInLine = leg[i - 1].Value;
                            prevTrans.Value.x -= totalEffectiveWidth; //+ MetricsCdfe[lastCharacterInLine].size.x;
                            prevTrans.Value.x += tt.rect.width;
                            break;
                    }

                    TranslationCdfe[prev] = prevTrans;
                }
            }
        }
    }
}