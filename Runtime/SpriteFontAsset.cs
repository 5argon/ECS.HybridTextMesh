using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace E7.ECS.SpriteFont
{
    [CreateAssetMenu]
    public class SpriteFontAsset : ScriptableObject
    {
#pragma warning disable 0649

        [Header("Source")] [SerializeField] internal Material material;
        [SerializeField] internal float lineHeight;
        

        [Header("Generator")] [SerializeField] internal float baseCharacterMeshSize;
        [SerializeField] internal Sprite[] spriteFontRects;
        [SerializeField] internal Sprite[] spriteFontBounds;
        [SerializeField] internal TMP_FontAsset tmpFontAsset;

        [Header("Preprocessed")] [SerializeField] bool singleMeshSupport;
        [SerializeField] internal CharacterInfo[] characterInfos;
        [SerializeField] internal Mesh singleMesh;
#pragma warning restore 0649

#if UNITY_EDITOR
        [ContextMenu("Calculate")]
        void DoSomething()
        {
            if (spriteFontRects.Length == 0)
            {
                throw new Exception($"Please add some rects.");
            }

            var pairs = new List<CharacterInfo>(spriteFontRects.Length);
            var sizes = new HashSet<float2>();
            var sizesValue = new List<float2>();
            float2 uvSize = default;
            float2 pixelSize = default;
            foreach (var s in spriteFontRects)
            {
                if (s.name.Length == 1)
                {
                    float uvWidth = s.uv[1].x - s.uv[0].x;
                    float uvHeight = s.uv[0].y - s.uv[2].y;
                    uvSize = new float2((float) Math.Round(uvWidth, 6), (float) Math.Round(uvHeight, 6));
                    pixelSize = new float2(s.rect.width, s.rect.height);

                    float h = 1;
                    float w = pixelSize.x / pixelSize.y;
                    h *= baseCharacterMeshSize;
                    w *= baseCharacterMeshSize;

                    Mesh perCharMesh = MakeMesh(w, h, s.uv);
                    perCharMesh.name = this.name + "_" + s.name;

                    pairs.Add(new CharacterInfo
                    {
                        character = s.name[0],
                        meshForCharacter = perCharMesh,
                        metrics = new Metrics
                        {
                            uvOffset = new float2(s.uv[0]),
                            size = new float2(w, h),
                        },
                    });
                    sizes.Add(uvSize);
                    sizesValue.Add(uvSize);
                }
            }

            if (sizes.Count > 1)
            {
                singleMeshSupport = false;
            }
            else
            {
                singleMeshSupport = true;
            }

            characterInfos = pairs.ToArray();

            float height = 1;
            float width = pixelSize.x / pixelSize.y;
            height *= baseCharacterMeshSize;
            width *= baseCharacterMeshSize;

            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, uvSize.y),
                new Vector2(uvSize.x, uvSize.y),
                new Vector2(0, 0),
                new Vector2(uvSize.x, 0),
            };
            var mesh = MakeMesh(width, height, uv);
            mesh.name = this.name + "GlyphMesh";

            // string path = Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(this)) ?? throw new Exception(),
            //     this.name + "Mesh.asset");
            // AssetDatabase.CreateAsset(mesh, path);

            // var meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var all = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this));
            foreach (var a in all)
            {
                if (AssetDatabase.IsSubAsset(a))
                {
                    AssetDatabase.RemoveObjectFromAsset(a);
                }
            }

            AssetDatabase.AddObjectToAsset(mesh, this);
            singleMesh = mesh;
            foreach (var a in pairs)
            {
                AssetDatabase.AddObjectToAsset(a.meshForCharacter, this);
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private Mesh MakeMesh(float width, float height, Vector2[] uv)
        {
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(0, 0, 0),
                new Vector3(width, 0, 0),
                new Vector3(0, height, 0),
                new Vector3(width, height, 0)
            };
            mesh.vertices = vertices;

            int[] tris = new int[6]
            {
                // lower left triangle
                0, 2, 1,
                // upper right triangle
                2, 3, 1
            };
            mesh.triangles = tris;

            Vector3[] normals = new Vector3[4]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
            };
            mesh.normals = normals;

            var rearrange = new Vector2[uv.Length];
            rearrange[0] = uv[2];
            rearrange[1] = uv[3];
            rearrange[2] = uv[0];
            rearrange[3] = uv[1];
            mesh.uv = rearrange;
            return mesh;
        }
#endif
    }
}