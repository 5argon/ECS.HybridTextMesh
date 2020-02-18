namespace E7.ECS.HybridTextMesh
{
    public enum MeshMode : byte
    {
        /// <summary>
        /// Use mesh of each character generated on the font asset.
        /// </summary>
        PerCharacterMesh,
        
        /// <summary>
        /// Combine mesh of each character into a single mesh for each string.
        /// </summary>
        CombinedMesh,
        
        /// <summary>
        /// Use only one kind of mesh for all characters but instead use material property block
        /// to vary the texture into its UV.
        /// </summary>
        UniversalMesh
    }
}