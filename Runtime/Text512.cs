using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.SpriteFont
{
    /// <summary>
    /// If you change this then all of them are regenerated.
    /// Text component is separated so you can replace without care
    /// about other settings.
    /// </summary>
    public struct Text512 : IComponentData
    {
        public NativeString512 text;
    }

    /// <summary>
    /// Similar to <see cref="LinkedEntityGroup"/> but used for iterating through
    /// all characters.
    /// </summary>
    [InternalBufferCapacity(16)]
    internal struct CharacterEntityGroup : IBufferElementData
    {
        internal Entity character;
    }

    /// <summary>
    /// Things that affects spawned text meshes, in addition to <see cref="Text512"/>.
    /// If you change any of this then all of them are regenerated.
    /// </summary>
    [Serializable]
    public struct TextStructure : IComponentData
    {
#pragma warning disable 0649
        /// <summary>
        /// When more than 0, only use entities available in the buffer.
        /// There is no destroy or regenerate.
        /// 
        /// If text is longer than that, it is truncated. This allows you to pre
        /// assign the characters as a part of other entity's <see cref="LinkedEntityGroup"/>,
        /// as a part of conversion, for example.
        ///
        /// In conversion, add empty children game objects to represent these persistent characters.
        /// </summary>
        [HideInInspector][SerializeField] internal bool persistentCharacterEntity;
        
        /// <summary>
        /// Each character entities came with <see cref="NonUniformScale"/> or not
        /// depending on this settings. It could save some work on transform systems if you
        /// could go with purely base mesh size setup on the font asset file. I think
        /// in the most case we could just scale the parent unless you want to do some
        /// fancy animation.
        /// </summary>
        [SerializeField] internal bool perCharacterScaling;
        
        /// <summary>
        /// Doesn't work yet.
        /// </summary>
        [SerializeField] internal bool singleMeshMode;
#pragma warning restore 0649
    }

    /// <summary>
    /// Changing these only affects layout, there is no need to respawn text meshes.
    /// </summary>
    [Serializable]
    public struct TextTransform : IComponentData
    {
#pragma warning disable 0649
        [SerializeField] internal float tracking;
        [SerializeField] internal TextAlignment textAlignment;
        [SerializeField] internal float lineHeightMultiplier;
        
        /// <summary>
        /// Converted fron RectTransform if using authoring component.
        /// </summary>
        [HideInInspector] [SerializeField] internal float2 rectSize;
#pragma warning restore 0649
    }

    /// <summary>
    /// Migrated from font asset.
    /// </summary>
    public struct TextTransformFixed : IComponentData
    {
        internal float lineHeight;
    }
}