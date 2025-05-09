using UnityEngine;
using UnityEditor;

public class AssignMaterialToObjects : EditorWindow
{
    GameObject targetObject;
    Material newMaterial;

    [MenuItem("Tools/Assign Material to Objects")]
    public static void ShowWindow()
    {
        GetWindow(typeof(AssignMaterialToObjects), false, "Material Zuweiser");
    }

    private void OnGUI()
    {
        GUILayout.Label("Material allen Objekten zuweisen", EditorStyles.boldLabel);
        targetObject = EditorGUILayout.ObjectField("Ziel GameObject", targetObject, typeof(GameObject), true) as GameObject;
        newMaterial = EditorGUILayout.ObjectField("Neues Material", newMaterial, typeof(Material), false) as Material;

        if (GUILayout.Button("Material zuweisen"))
        {
            if (targetObject == null)
            {
                EditorUtility.DisplayDialog("Fehler", "Bitte ein Ziel GameObject auswählen.", "OK");
                return;
            }
            if (newMaterial == null)
            {
                EditorUtility.DisplayDialog("Fehler", "Bitte ein Material auswählen.", "OK");
                return;
            }
            AssignMaterial(targetObject, newMaterial);
        }
    }

    static void AssignMaterial(GameObject target, Material material)
    {
        // Hole alle MeshRenderer, auch in inaktiven Objekten
        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in renderers)
        {
            // Wenn mehrere Materialien vorhanden sind, ersetzen wir alle mit dem neuen Material
            Material[] mats = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = material;
            }
            renderer.sharedMaterials = mats;
        }
        EditorUtility.DisplayDialog("Erfolg", "Material wurde allen Objekten zugewiesen.", "OK");
    }
}
