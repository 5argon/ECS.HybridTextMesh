using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace E7.ECS.HybridTextMesh
{
    /// <summary>
    /// Changing these only affects layout, there is no need to respawn text meshes.
    /// </summary>
    [Serializable]
    public struct TextTransform : IComponentData
    {
#pragma warning disable 0649
        [SerializeField] internal TextAlignment textAlignmentHorizontal;
        [SerializeField] internal TextVerticalAlignment textAlignmentVertical;
        [Space]
        [SerializeField] internal float tracking;
        [SerializeField] internal float modifyLeading;
        
        [Space]
        [SerializeField] internal bool monospace; //This doesn't work correctly I think lol
        [SerializeField] internal float monospaceWidth;
        
        internal Rect rect;
#pragma warning restore 0649
    }
}