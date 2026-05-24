using System.IO;
using UnityEditor;
using UnityEngine;

public class LightCookieGenerator : EditorWindow
{
    static readonly int[] SizeOptions = { 256, 512, 1024 };
    static readonly string[] SizeLabels = { "256", "512", "1024" };
    const string ExportDir = "Assets/BonkPark/Art/Lighting";

    int size = 512;
    Vector2 center = new Vector2(0.5f, 0.6f);
    float outerRadius = 0.5f;
    float falloffPower = 1.5f;
    float headAngleDeg = 90f;
    float occlusionConeDeg = 60f;
    float occlusionSoftnessDeg = 35f;
    float occlusionStrength = 0.75f;
    string fileName = "LumiLightCookie";

    Texture2D preview;
    byte[] alphaBytes;
    bool dirty = true;

    [MenuItem("BonkPark/Light Cookie Generator")]
    static void Open()
    {
        GetWindow<LightCookieGenerator>("Light Cookie");
    }

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        size = EditorGUILayout.IntPopup("Size", size, SizeLabels, SizeOptions);
        center = EditorGUILayout.Vector2Field("Center (UV)", center);
        outerRadius = EditorGUILayout.Slider("Outer Radius", outerRadius, 0.1f, 1f);
        falloffPower = EditorGUILayout.Slider("Falloff Power", falloffPower, 0.5f, 4f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Head occlusion", EditorStyles.boldLabel);
        headAngleDeg = EditorGUILayout.Slider("Head Angle", headAngleDeg, 0f, 360f);
        occlusionConeDeg = EditorGUILayout.Slider("Cone Half-Angle", occlusionConeDeg, 0f, 180f);
        occlusionSoftnessDeg = EditorGUILayout.Slider("Edge Softness", occlusionSoftnessDeg, 1f, 90f);
        occlusionStrength = EditorGUILayout.Slider("Strength", occlusionStrength, 0f, 1f);

        if (EditorGUI.EndChangeCheck()) dirty = true;

        if (dirty)
        {
            Generate();
            dirty = false;
        }

        EditorGUILayout.Space();
        Rect r = GUILayoutUtility.GetRect(256f, 256f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, new Color(0.12f, 0.12f, 0.12f));
        if (preview != null)
            GUI.DrawTexture(r, preview, ScaleMode.ScaleToFit, true);

        EditorGUILayout.Space();
        fileName = EditorGUILayout.TextField("File Name", fileName);
        EditorGUILayout.LabelField("Output", $"{ExportDir}/{fileName}.png");
        if (GUILayout.Button("Export PNG"))
            Export();
    }

    static float Smoothstep01(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / Mathf.Max(1e-6f, edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    void Generate()
    {
        if (preview == null || preview.width != size)
        {
            if (preview != null) DestroyImmediate(preview);
            preview = new Texture2D(size, size, TextureFormat.RGBA32, false);
            preview.wrapMode = TextureWrapMode.Clamp;
        }
        if (alphaBytes == null || alphaBytes.Length != size * size)
            alphaBytes = new byte[size * size];

        float headRad = headAngleDeg * Mathf.Deg2Rad;
        Vector2 headDir = new Vector2(Mathf.Cos(headRad), Mathf.Sin(headRad));
        float coneInner = Mathf.Max(0f, occlusionConeDeg - occlusionSoftnessDeg);
        float coneOuter = Mathf.Min(180f, occlusionConeDeg + occlusionSoftnessDeg);

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                Vector2 p = uv - center;
                float dist = p.magnitude;

                float radial = 1f - Smoothstep01(0f, outerRadius, dist);
                radial = Mathf.Pow(radial, falloffPower);

                float occlusion = 1f;
                if (dist > 1e-5f)
                {
                    Vector2 dir = p / dist;
                    float angleFromHead = Mathf.Acos(Mathf.Clamp(Vector2.Dot(dir, headDir), -1f, 1f)) * Mathf.Rad2Deg;
                    float blockT = 1f - Smoothstep01(coneInner, coneOuter, angleFromHead);
                    occlusion = 1f - occlusionStrength * blockT;
                }

                byte a = (byte)(Mathf.Clamp01(radial * occlusion) * 255f);
                int idx = y * size + x;
                alphaBytes[idx] = a;
                pixels[idx] = new Color32(a, a, a, 255);
            }
        }
        preview.SetPixels32(pixels);
        preview.Apply();
    }

    void Export()
    {
        if (alphaBytes == null) Generate();

        if (!Directory.Exists(ExportDir))
            Directory.CreateDirectory(ExportDir);

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] exportPixels = new Color32[size * size];
        for (int i = 0; i < exportPixels.Length; i++)
            exportPixels[i] = new Color32(255, 255, 255, alphaBytes[i]);
        tex.SetPixels32(exportPixels);
        tex.Apply();

        string path = $"{ExportDir}/{fileName}.png";
        byte[] pngBytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, pngBytes);
        DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (asset != null) EditorGUIUtility.PingObject(asset);

        int aMin = 255, aMax = 0;
        long aSum = 0;
        for (int i = 0; i < alphaBytes.Length; i++)
        {
            if (alphaBytes[i] < aMin) aMin = alphaBytes[i];
            if (alphaBytes[i] > aMax) aMax = alphaBytes[i];
            aSum += alphaBytes[i];
        }
        Debug.Log($"Exported {path}  alpha min/avg/max = {aMin}/{aSum / alphaBytes.Length}/{aMax}  png={pngBytes.Length}B");
    }
}
