using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering; // Notwendig für IndexFormat

public class CombineChildMeshes : EditorWindow
{
    GameObject targetObject;

    [MenuItem("Tools/Combine Child Meshes")]
    public static void ShowWindow()
    {
        GetWindow(typeof(CombineChildMeshes), false, "Mesh Kombinierer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Kombiniere Child Meshes", EditorStyles.boldLabel);
        targetObject = EditorGUILayout.ObjectField("Ziel GameObject", targetObject, typeof(GameObject), true) as GameObject;

        if (GUILayout.Button("Kombinieren und speichern"))
        {
            if (targetObject == null)
            {
                EditorUtility.DisplayDialog("Fehler", "Bitte wähle ein Ziel GameObject aus.", "OK");
                return;
            }
            CombineMeshes(targetObject);
        }
    }

    static void CombineMeshes(GameObject target)
    {
        // Alle aktiven MeshFilter abrufen
        MeshFilter[] meshFilters = target.GetComponentsInChildren<MeshFilter>();
        if (meshFilters == null || meshFilters.Length == 0)
        {
            EditorUtility.DisplayDialog("Fehler", "Es wurden keine MeshFilter in den Child Objekten gefunden.", "OK");
            return;
        }

        CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];
        int index = 0;
        Transform parentTransform = target.transform;
        foreach (MeshFilter mf in meshFilters)
        {
            // Nur aktive GameObjects berücksichtigen
            if (!mf.gameObject.activeInHierarchy)
                continue;
            if (mf.sharedMesh == null)
                continue;

            combineInstances[index].mesh = mf.sharedMesh;
            // Berechnung der relativen Transformationsmatrix
            combineInstances[index].transform = parentTransform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            index++;
        }

        if (index == 0)
        {
            EditorUtility.DisplayDialog("Fehler", "Keine gültigen MeshFilter mit einem Mesh gefunden.", "OK");
            return;
        }

        System.Array.Resize(ref combineInstances, index);

        Mesh combinedMesh = new Mesh();
        combinedMesh.name = target.name + "_CombinedMesh";
        // Setzt das IndexFormat auf UInt32, um mehr als 65.535 Vertices zu unterstützen.
        combinedMesh.indexFormat = IndexFormat.UInt32;

        combinedMesh.CombineMeshes(combineInstances, true, true);

        // Speichern des kombinierten Meshes als Asset
        string assetPath = "Assets/" + combinedMesh.name + ".asset";
        AssetDatabase.CreateAsset(combinedMesh, assetPath);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Erfolg", "Kombiniertes Mesh gespeichert unter: " + assetPath, "OK");
    }
}
