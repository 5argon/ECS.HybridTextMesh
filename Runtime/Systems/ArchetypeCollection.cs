using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace E7.ECS.SpriteFont
{
    internal static class ArchetypeCollection
    {
        /// <summary>
        /// You need all these to get all systems working.
        /// </summary>
        public static readonly ComponentType[] TextParentTypes = new[]
        {
            ComponentType.ReadOnly<Text512>(),
            ComponentType.ReadOnly<TextTransform>(),
            ComponentType.ReadOnly<TextTransformFixed>(),
            ComponentType.ReadOnly<SpriteFontAssetHolder>(),
            ComponentType.ReadOnly<LinkedEntityGroup>() //For remembering each character.
        };
            
        internal static readonly ComponentType[] CharacterTypes = new[]
        {
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadOnly<RenderMesh>(),
            ComponentType.ReadOnly<LocalToWorld>(),
            ComponentType.ReadOnly<LocalToParent>(),
            ComponentType.ReadOnly<CharacterUv>(),
            ComponentType.ReadOnly<Metrics>(),
            ComponentType.ReadOnly<Parent>()
        };
        internal static readonly ComponentType[] FontAssetTypes = new[]
        {
            ComponentType.ReadOnly<FontAsset>(),
            ComponentType.ReadOnly<SpriteFontAssetHolder>(),
            ComponentType.ReadOnly<CharacterPrefabBuffer>(),
            //ComponentType.ReadOnly<CharacterPrefabLookup>() //Added when buffer removed.
        };
    }
}