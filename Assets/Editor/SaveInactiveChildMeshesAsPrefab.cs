using UnityEngine;
using UnityEditor;

public class SaveInactiveChildMeshesAsPrefab : EditorWindow
{
    GameObject targetObject;

    [MenuItem("Tools/Save Inactive Child Meshes as Prefab")]
    public static void ShowWindow()
    {
        GetWindow(typeof(SaveInactiveChildMeshesAsPrefab), false, "Prefab Saver");
    }

    private void OnGUI()
    {
        GUILayout.Label("Speichere inaktive Child Meshes als Prefab", EditorStyles.boldLabel);
        targetObject = EditorGUILayout.ObjectField("Ziel GameObject", targetObject, typeof(GameObject), true) as GameObject;

        if (GUILayout.Button("Prefab erstellen und speichern"))
        {
            if (targetObject == null)
            {
                EditorUtility.DisplayDialog("Fehler", "Bitte wähle ein Ziel GameObject aus.", "OK");
                return;
            }
            SaveAsPrefab(targetObject);
        }
    }

    static void SaveAsPrefab(GameObject target)
    {
        // Container-Objekt erstellen, in dem die Klone gesammelt werden.
        GameObject container = new GameObject(target.name + "_InactiveMeshes_Prefab");

        // Alle MeshFilter (auch inaktive) abrufen.
        MeshFilter[] meshFilters = target.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter mf in meshFilters)
        {
            // Nur inaktive Objekte berücksichtigen
            if (mf.gameObject.activeInHierarchy)
                continue;
            if (mf.sharedMesh == null)
                continue;

            // Neues GameObject erstellen, das den Originalnamen erhält.
            GameObject clone = new GameObject(mf.gameObject.name);
            // Als Kind des Containers einfügen
            clone.transform.parent = container.transform;
            // Positionsdaten in Weltkoordinaten übernehmen
            clone.transform.position = mf.transform.position;
            clone.transform.rotation = mf.transform.rotation;
            clone.transform.localScale = mf.transform.lossyScale;

            // MeshFilter hinzufügen und das Mesh zuweisen
            MeshFilter newMf = clone.AddComponent<MeshFilter>();
            newMf.sharedMesh = mf.sharedMesh;

            // Falls ein MeshRenderer vorhanden ist, diesen ebenfalls kopieren
            MeshRenderer originalRenderer = mf.GetComponent<MeshRenderer>();
            if (originalRenderer != null)
            {
                MeshRenderer newMr = clone.AddComponent<MeshRenderer>();
                newMr.sharedMaterials = originalRenderer.sharedMaterials;
            }
        }

        // Speichern des Containers als Prefab im Assets-Ordner
        string path = "Assets/" + container.name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(container, path);
        EditorUtility.DisplayDialog("Erfolg", "Prefab gespeichert unter: " + path, "OK");

        // Das temporäre Container-Objekt aus der Szene entfernen
        DestroyImmediate(container);
    }
}
