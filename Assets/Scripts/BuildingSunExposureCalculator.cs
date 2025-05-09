using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BuildingSunExposureCalculator : MonoBehaviour
{
    // Konfigurierbarer Chunk-Wert: Anzahl der Dreiecke, die pro Frame verarbeitet werden
    public int chunkSize = 100;

    // Die aus dem einzigen Light abgeleitete Sonnenrichtung
    private Vector3 sunDirection;

    // Liste der Gebäude-Objekte, die das Material "Buildings" nutzen
    private List<GameObject> buildingObjects = new List<GameObject>();

    // Aggregierte Werte für alle Gebäude
    private float aggregatedTotalArea = 0f;
    private float aggregatedLitArea = 0f;

    void Start()
    {
        // Ermittel die Sonnenrichtung aus dem einzigen Light in der Szene
        Light sun = FindObjectOfType<Light>();
        if (sun != null)
        {
            // Bei einem Directional Light zeigt der forward-Vektor typischerweise in die entgegengesetzte Richtung der Strahlen
            sunDirection = -sun.transform.forward;
            Debug.Log("Sonnenrichtung: " + sunDirection);
        }
        else
        {
            Debug.LogWarning("Kein Light in der Szene gefunden – Standard-Sonnenrichtung wird genutzt.");
            sunDirection = new Vector3(1, -1, 1).normalized;
        }

        // Finde alle GameObjects, deren Renderer ein Material verwenden, dessen Name "Buildings" enthält
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        foreach (Renderer r in allRenderers)
        {
            if (r.sharedMaterials != null)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null && m.name.Contains("Buildings"))
                    {
                        if (!buildingObjects.Contains(r.gameObject))
                        {
                            buildingObjects.Add(r.gameObject);
                        }
                        break; // Nur einmal pro GameObject hinzufügen
                    }
                }
            }
        }

        Debug.Log("Anzahl gefundener Gebäude: " + buildingObjects.Count);

        // Starte die Verarbeitung aller Gebäude
        StartCoroutine(ProcessBuildings());
    }

    // Verarbeitet alle Gebäude nacheinander
    IEnumerator ProcessBuildings()
    {
        foreach (GameObject building in buildingObjects)
        {
            MeshFilter mf = building.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                float totalArea = 0f;
                float litArea = 0f;

                // Verarbeite das Mesh in Chunks, um den Stack zu entlasten
                yield return StartCoroutine(ProcessMeshInChunks(
                    mf.sharedMesh, building.transform, sunDirection, chunkSize,
                    (tArea, lArea) =>
                    {
                        totalArea = tArea;
                        litArea = lArea;
                        aggregatedTotalArea += tArea;
                        aggregatedLitArea += lArea;
                        Debug.Log($"Gebäude {building.name} -> Gesamtfläche: {tArea:F2}, beleuchtet: {lArea:F2}");
                    }
                ));
            }
            else
            {
                Debug.LogWarning("Gebäude " + building.name + " hat keinen MeshFilter oder kein gültiges Mesh.");
            }
            // Warte einen Frame zwischen den Gebäuden (optional, um Last zu verteilen)
            yield return null;
        }

        // Nachdem alle Gebäude verarbeitet wurden, berechne den Gesamt-Sonnenexpositionsprozentsatz
        float overallExposure = aggregatedTotalArea > 0 ? (aggregatedLitArea / aggregatedTotalArea * 100f) : 0f;
        Debug.Log("Gesamter Sonnenanteil auf allen Gebäuden: " + overallExposure.ToString("F2") + "%");
    }

    // Diese Coroutine verarbeitet das Mesh in Blöcken (chunkSize Dreiecke pro Frame)
    IEnumerator ProcessMeshInChunks(Mesh mesh, Transform buildingTransform, Vector3 sunDir, int chunkSize, System.Action<float, float> onComplete)
    {
        float totalSurfaceArea = 0f;
        float litSurfaceArea = 0f;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        int totalTriangles = triangles.Length / 3;

        // Optional: Definiere eine noch kleinere Iterationspause, z. B. alle 50 Dreiecke.
        int yieldInterval = 50;

        for (int i = 0; i < totalTriangles; i++)
        {
            int triIndex = i * 3;

            Vector3 v0 = buildingTransform.TransformPoint(vertices[triangles[triIndex]]);
            Vector3 v1 = buildingTransform.TransformPoint(vertices[triangles[triIndex + 1]]);
            Vector3 v2 = buildingTransform.TransformPoint(vertices[triangles[triIndex + 2]]);

            Vector3 center = (v0 + v1 + v2) / 3f;
            float area = Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
            totalSurfaceArea += area;

            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            if (Vector3.Dot(normal, sunDir) > 0)
            {
                if (Physics.Raycast(center, sunDir, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hit.collider.gameObject == buildingTransform.gameObject)
                    {
                        litSurfaceArea += area;
                    }
                }
                else
                {
                    litSurfaceArea += area;
                }
            }

            // Alle yieldInterval Iterationen kurz pausieren
            if (i % yieldInterval == 0)
            {
                yield return null;
            }
        }

        onComplete(totalSurfaceArea, litSurfaceArea);
    }

}
