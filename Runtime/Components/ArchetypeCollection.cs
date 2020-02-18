using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace E7.ECS.HybridTextMesh
{
    internal static class ArchetypeCollection
    {
        /// <summary>
        /// You need all these to get all systems working.
        /// </summary>
        public static readonly ComponentType[] TextParentTypes = new[]
        {
            ComponentType.ReadOnly<TextContent>(),
            ComponentType.ReadOnly<TextTransform>(),
            ComponentType.ReadOnly<FontMetrics>(),
            ComponentType.ReadOnly<FontAssetHolder>(),
            ComponentType.ReadOnly<LinkedEntityGroup>() //For remembering each character.
        };
            
        public static readonly ComponentType[] CharacterTypes = new[]
        {
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadOnly<RenderMesh>(),
            ComponentType.ReadOnly<RenderBounds>(), //New hybrid renderer no longer add this.
            ComponentType.ReadOnly<LocalToWorld>(),
            ComponentType.ReadOnly<LocalToParent>(),
            ComponentType.ReadOnly<GlyphUv>(),
            ComponentType.ReadOnly<GlyphMetrics>(),
            ComponentType.ReadOnly<Parent>(),
            ComponentType.ReadOnly<SpecialCharacter>(),
        };
        internal static readonly ComponentType[] FontAssetTypes = new[]
        {
            ComponentType.ReadOnly<FontAssetEntity>(),
            ComponentType.ReadOnly<FontAssetHolder>(),
            ComponentType.ReadOnly<GlyphPrefabBuffer>(),
            //ComponentType.ReadOnly<CharacterPrefabLookup>() //Added when buffer removed.
        };
    }
}