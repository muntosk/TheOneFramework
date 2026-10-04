using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

/// <summary>
/// Imports Valve Material Type (.vmt) files as URP Lit/Unlit materials, using the
/// sibling .vtf assets (imported by VtfImporter). The result is read-only; use
/// Tools > Portal 2 > Bake VMTs To Editable Materials for editable copies.
/// </summary>
[ScriptedImporter(2, "vmt", importQueueOffset: 1)]
public class VmtImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var vmt = VmtData.Parse(File.ReadAllText(ctx.assetPath));
        var mat = VmtMaterialBuilder.Build(Path.GetFileNameWithoutExtension(ctx.assetPath), vmt,
            (texRef, kind) => LoadTexture(ctx, texRef, kind));

        if (mat == null)
        {
            ctx.LogImportError("Could not find the 'Universal Render Pipeline/Lit' shader.");
            return;
        }

        ctx.AddObjectToAsset("material", mat);
        ctx.SetMainObject(mat);
    }

    static Texture2D LoadTexture(AssetImportContext ctx, string texRef, VtfTextureKind kind)
    {
        string path = VmtMaterialBuilder.ResolveTexturePath(ctx.assetPath, texRef);
        if (path == null)
        {
            ctx.LogImportWarning($"Could not find texture '{texRef}' referenced by '{ctx.assetPath}'.");
            return null;
        }

        ctx.DependsOnArtifact(path);

        if (kind == VtfTextureKind.SSBump && AssetImporter.GetAtPath(path) is VtfImporter vtf &&
            vtf.kind == VtfTextureKind.Auto && !Path.GetFileName(path).ToLowerInvariant().Contains("ssbump"))
        {
            ctx.LogImportWarning($"'{path}' is used as an $ssbump by '{ctx.assetPath}' but may not be detected as one. " +
                "Set its Kind to SSBump in the inspector.");
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
