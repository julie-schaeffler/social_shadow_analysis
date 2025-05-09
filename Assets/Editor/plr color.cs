#if UNITY_EDITOR          // rein editor‑seitig
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SocialColorAssignerEditor : EditorWindow
{
    // Name der CSV im StreamingAssets‑Ordner
    private string socialIndexFileName = "socialIndex.csv";

    // Root‑Objekt mit allen Planungsräumen (wird gesucht, falls leer)
    private GameObject planungsraeumeRoot;

    // Menüeintrag anlegen
    [MenuItem("Tools/Social‑Index/Assign Colors")]
    public static void OpenWindow() => GetWindow<SocialColorAssignerEditor>("Social Color Assigner");

    private void OnGUI()
    {
        GUILayout.Label("Social‑Index → Farbe", EditorStyles.boldLabel);
        socialIndexFileName = EditorGUILayout.TextField("CSV‑Datei", socialIndexFileName);
        planungsraeumeRoot = (GameObject)EditorGUILayout.ObjectField("Planungsräume‑Root", planungsraeumeRoot, typeof(GameObject), true);

        if (GUILayout.Button("Farben anwenden"))
            ApplyColors();
    }

    // ------------------------------------------------------------

    private void ApplyColors()
    {
        if (planungsraeumeRoot == null)
            planungsraeumeRoot = GameObject.Find("Planungsräume");

        if (planungsraeumeRoot == null)
        {
            Debug.LogError("GameObject »Planungsräume« nicht gefunden.");
            return;
        }

        string csvPath = Path.Combine(Application.streamingAssetsPath, socialIndexFileName);
        Dictionary<string, int> dict = LoadSocialIndices(csvPath);
        if (dict == null) return;

        AssignColors(dict, planungsraeumeRoot.transform);

        // Szene als geändert markieren, damit Unity sie speichert
#if UNITY_EDITOR
        EditorSceneManager.MarkSceneDirty(planungsraeumeRoot.scene);
#endif
        Debug.Log("Social‑Farben angewendet – Szene speichern (⌘/Ctrl + S).");
    }

    // ------------------------------------------------------------

    private Dictionary<string, int> LoadSocialIndices(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("CSV nicht gefunden: " + path);
            return null;
        }

        var dict = new Dictionary<string, int>();
        string[] lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)                  // Header überspringen
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split(';');
            if (parts.Length < 2) continue;

            if (int.TryParse(parts[1].Trim(), out int index))
                dict[parts[0].Trim()] = index;
        }
        return dict;
    }

    private void AssignColors(Dictionary<string, int> dict, Transform root)
    {
        foreach (Transform child in root)
        {
            string plrId = ExtractPlrId(child.name);
            if (string.IsNullOrEmpty(plrId) || !dict.TryGetValue(plrId, out int idx))
            {
                Debug.LogWarning($"Kein Social‑Index für: {child.name}");
                continue;
            }

            // Renderer besorgen
            MeshRenderer mr = child.GetComponent<MeshRenderer>() ?? child.gameObject.AddComponent<MeshRenderer>();

            // passenden Shader finden (URP oder Standard‑Pipeline)
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // neues Material erzeugen & zuweisen
            var mat = new Material(shader) { color = GetColor(idx) };
            mr.sharedMaterial = mat;        // sharedMaterial für dauerhafte Änderung

#if UNITY_EDITOR
            EditorUtility.SetDirty(mr);     // Objekt als geändert markieren
#endif
        }
    }

    // Helfer ------------------------------------------------------

    private static string ExtractPlrId(string goName)
        => goName.Contains("_") ? goName.Substring(goName.LastIndexOf('_') + 1) : string.Empty;

    private static Color GetColor(int idx) => idx switch
    {
        1 => Color.green,
        2 => Color.yellow,
        3 => new Color(1f, 0.5f, 0f),   // Orange
        4 => Color.red,
        _ => Color.white
    };
}
