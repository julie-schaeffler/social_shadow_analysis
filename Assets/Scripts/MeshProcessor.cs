using System;
using System.Collections;
using UnityEngine;

public class MeshProcessor : MonoBehaviour
{
    // Diese Coroutine verarbeitet das Mesh in Blöcken (chunkSize Dreiecke pro Block)
    public IEnumerator ProcessMeshInChunks(Mesh mesh, Transform buildingTransform, Vector3 sunDir, int chunkSize, Action<float, float> onComplete)
    {
        float totalSurfaceArea = 0f;
        float litSurfaceArea = 0f;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        int totalTriangles = triangles.Length / 3;

        for (int i = 0; i < totalTriangles; i++)
        {
            int triIndex = i * 3;
            // Transformiere die Dreiecksecken in Weltkoordinaten
            Vector3 v0 = buildingTransform.TransformPoint(vertices[triangles[triIndex]]);
            Vector3 v1 = buildingTransform.TransformPoint(vertices[triangles[triIndex + 1]]);
            Vector3 v2 = buildingTransform.TransformPoint(vertices[triangles[triIndex + 2]]);

            // Berechne den Mittelpunkt und die Fläche des Dreiecks
            Vector3 center = (v0 + v1 + v2) / 3f;
            float area = Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
            totalSurfaceArea += area;

            // Berechne die Normale des Dreiecks
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            // Wenn das Dreieck in Richtung der Sonne zeigt...
            if (Vector3.Dot(normal, sunDir) > 0)
            {
                // Führe einen Raycast vom Mittelpunkt in die Sonnenrichtung aus
                if (Physics.Raycast(center, sunDir, out RaycastHit hit, Mathf.Infinity))
                {
                    // Gilt als beleuchtet, wenn der getroffene Collider zum gleichen Objekt gehört
                    if (hit.collider.gameObject == buildingTransform.gameObject)
                    {
                        litSurfaceArea += area;
                    }
                }
                else
                {
                    // Wenn nichts getroffen wird, gilt das Dreieck als voll beleuchtet
                    litSurfaceArea += area;
                }
            }

            // Nach jedem "chunkSize" Dreiecken eine Pause einlegen, um den Stack zu entlasten
            if (i % chunkSize == 0)
            {
                yield return null;
            }
        }
        // Rufe den Callback auf, sobald die Verarbeitung abgeschlossen ist
        onComplete(totalSurfaceArea, litSurfaceArea);
    }
}
