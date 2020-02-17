using Unity.Collections;
using Unity.Entities;

namespace E7.ECS.HybridTextMesh
{
    /// <summary>
    /// If you change this then all of them are regenerated.
    /// Text component is separated so you can replace without care
    /// about other settings.
    /// </summary>
    public struct TextContent : IComponentData
    {
        public NativeString512 text;
    }
}