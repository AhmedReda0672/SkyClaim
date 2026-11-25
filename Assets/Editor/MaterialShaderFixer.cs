#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class MaterialShaderFixer
{
    static MaterialShaderFixer()
    {
        EditorApplication.delayCall += FixShaders;
    }

    [MenuItem("SkyClaim/Fix All Material Shaders")]
    public static void FixShaders()
    {
        Shader standardShader = Shader.Find("Standard");
        if (standardShader == null)
        {
            Debug.LogError("[MaterialShaderFixer] Standard shader not found!");
            return;
        }

        string[] searchFolders = new[]
        {
            "Assets/Rolling_Balls-Sci-fi_Pack",
            "Assets/BTM_Assets",
            "Assets/AwesomeCartoonPlanes"
        };

        string[] guids = AssetDatabase.FindAssets("t:Material", searchFolders);
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            bool isBroken = mat.shader == null ||
                            mat.shader.name.Contains("InternalError") ||
                            mat.shader.name.Contains("Error") ||
                            mat.shader.name.Contains("Universal") ||
                            mat.shader.name.Contains("URP") ||
                            mat.shader.name == "Hidden/InternalErrorShader";

            // Also check if shader name is not Standard / Mobile / Legacy
            if (isBroken || mat.shader.name.StartsWith("Universal Render Pipeline"))
            {
                Texture mainTex = null;
                if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
                    mainTex = mat.GetTexture("_MainTex");
                else if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
                    mainTex = mat.GetTexture("_BaseMap");

                Texture bumpMap = null;
                if (mat.HasProperty("_BumpMap") && mat.GetTexture("_BumpMap") != null)
                    bumpMap = mat.GetTexture("_BumpMap");

                Texture emissionMap = null;
                if (mat.HasProperty("_EmissionMap") && mat.GetTexture("_EmissionMap") != null)
                    emissionMap = mat.GetTexture("_EmissionMap");

                Color baseColor = Color.white;
                if (mat.HasProperty("_BaseColor"))
                    baseColor = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))
                    baseColor = mat.GetColor("_Color");

                mat.shader = standardShader;

                if (mainTex != null)
                {
                    mat.SetTexture("_MainTex", mainTex);
                    mat.color = baseColor;
                }
                else
                {
                    mat.color = baseColor;
                }

                if (bumpMap != null)
                {
                    mat.SetTexture("_BumpMap", bumpMap);
                    mat.EnableKeyword("_NORMALMAP");
                }

                if (emissionMap != null)
                {
                    mat.SetTexture("_EmissionMap", emissionMap);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.white);
                }

                EditorUtility.SetDirty(mat);
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MaterialShaderFixer] Successfully converted {fixedCount} materials to Standard shader with textures intact!");
        }
    }
}
#endif
