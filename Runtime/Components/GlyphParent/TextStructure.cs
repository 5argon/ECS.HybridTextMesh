using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.HybridTextMesh
{
    /// <summary>
    /// Things that affects spawned text meshes, in addition to <see cref="TextContent"/>.
    /// If you change any of this then all of them are regenerated.
    /// </summary>
    [Serializable]
    public struct TextStructure : IComponentData
    {
#pragma warning disable 0649
        /// <summary>
        /// Only use entities available in the buffer.
        /// There is no destroy or regenerate.
        /// 
        /// If text is longer than that, it is truncated. This allows you to pre
        /// assign the characters as a part of other entity's <see cref="LinkedEntityGroup"/>,
        /// as a part of conversion, for example.
        /// </summary>
        internal bool persistentCharacterEntityMode;
        
        /// <summary>
        /// Each character entities came with <see cref="NonUniformScale"/> or not
        /// depending on this settings. It could save some work on transform systems if you
        /// could go with purely base mesh size setup on the font asset file. I think
        /// in the most case we could just scale the parent unless you want to do some
        /// fancy animation.
        /// </summary>
        [SerializeField] internal bool perCharacterScalingMode;
        
        /// <summary>
        /// Doesn't work yet.
        /// </summary>
        [SerializeField] internal MeshMode meshMode;
#pragma warning restore 0649
    }
}