# ECS Hybrid Text Mesh

Use Transform systems and Hybrid Renderer to render text meshes. 

There are tons of fields that do nothing in the package.

## How it works

I don't have time to make a good documentation or a proper sample yet but in brief :

Font asset `SpriteFontAsset : ScriptableAsset` consists of a material and mesh per each character that you preprocess in editor.
Each character has a quad mesh pre-generated with UV that lands on the right graphic.

At runtime start by create a single entity containing everything stated in `ArchetypeCollection.TextParentTypes` or alternatively use the authoring component plus `RectTransform` on it. One of the required thing is a shared component data of that font asset.

It then cause 1 `Entity` creation per character in your string and set them so Hybrid Renderer renders them. It also set all character's `Parent` to the single entity you created, so moving the parent's transform will also move all character meshes along with it.

All characters are also registered in parent's `LinkedEntityGroup`. Destroying parent will destroy all characters with it, etc.

After, the system will layout all spawned characters once. If you want to animate each character, you can remember transforms of each character after the layout as a base then you are free to change any TRS as a delta to values you remembered.

Refreshes can occurs in 2 levels. If you change text or other important parameters all character meshes are cleaned up and spawned again, plus layout. (Including appending text to old text, it discards an entire line.) But there are some parameters like text alignment that won't cause clean up but just re-layout.

## Limitations

- Uses `NativeString512` so size is not unlimited.
- Nothing else other than explained. So no multiline, no change color, no outline, no rich text, no right to left, etc. Just render a part of texture with preprocessed mesh's UV.

## About "single mesh mode"

Because each character has different mesh, only the same character of the same font are in the same batch in `BatchRendererGroup` that Hybrid Renderer uses.

"Single mesh" you see in the code are all stubs, I am planning to try using only 1 mesh to render all texts, using GPU instancing and material property block to vary offset to the correct character in texture. (Which only work if the character texture was prepared in equal grid.)

That maybe done once URP support for `[MaterialPropertyBlock]` lands as an optimization for specialized use case. (e.g. A lot of animated damage numbers or running scores, which consisting of only digits and rather need performance more than flexibility.)
