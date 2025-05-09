using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

public class PlanningAreaSunExposureCalculator : MonoBehaviour
{
    [Header("Planungsräume (als Mesh‑Objekte)")]
    // Alle Planungsraum‑Objekte in der Szene, z. B. "Planungsraum_01100101", "Planungsraum_01100102", etc.
    public List<GameObject> planningAreaObjects;

    [Header("Gebäude (Parent-Objekte)")]
    // Hier kannst du Parent-Objekte angeben, die (rekursiv) Gebäude enthalten, die das Material "Buildings" nutzen.
    public List<GameObject> buildingObjects;

    [Header("Verarbeitung Parameter")]

    public int baseSamples = 10;       // Basiszahl an Samples für die Referenzfläche
    public float areaReference = 1.0f;   // Referenzfläche (in den gleichen Einheiten wie dein Mesh)
    public int minSamples = 10;        // Mindestanzahl an Samples pro Dreieck
    public int maxSamples = 100;       // Maximalanzahl an Samples pro Dreieck

    public int samplesPerTriangle = 10;   // Stichproben pro Dreieck (Monte‑Carlo)
    public int chunkSize = 500;           // Anzahl Dreiecke pro Chunk (optional)
    public int yieldInterval = 50;        // Intervall für yield in der Verarbeitung

    [Header("3D-Karten-Grenzen")]
    public float mapXMin = 14952.97f;
    public float mapXMax = 34152.97f;
    public float mapZMin = -5365f;
    public float mapZMax = 4835f;



    // Aggregierte Flächenwerte für den aktuell verarbeiteten Planungsraum
    private float aggregatedTotalArea = 0f;
    private float aggregatedLitArea = 0f;

    // Das 2D‑Polygon (auf XZ‑Ebene) des aktuell verarbeiteten Planungsraums
    private List<Vector2> planningPolygon;

    // PUBLIC Methode – diese kannst du per Button OnClick aufrufen.

    private string csvFilePath;

    public void StartCalculation()
    {
        if (planningAreaObjects == null || planningAreaObjects.Count == 0)
        {
            Debug.LogError("Keine Planungsraum‑Objekte zugewiesen!");
            return;
        }
        if (buildingObjects == null || buildingObjects.Count == 0)
        {
            Debug.LogError("Keine Gebäude‑Parent-Objekte zugewiesen!");
            return;
        }
        // Initialisiere die CSV-Datei – hier wird der Header geschrieben
        csvFilePath = Path.Combine(Application.persistentDataPath, "SunExposure_All.csv");
        using (StreamWriter writer = new StreamWriter(csvFilePath, false))
        {
            writer.WriteLine("Planungsraum;Datum;Uhrzeit;Sonnenanteil;Gebäudeanzahl;Gebäudefläche");
        }

        Debug.Log("CSV-Datei initialisiert: " + csvFilePath);

        StartCoroutine(ProcessAllPlanningAreas());
    }

    // Verarbeitet alle Planungsraum‑Objekte nacheinander
    IEnumerator ProcessAllPlanningAreas()
    {
        foreach (GameObject planningArea in planningAreaObjects)
        {
            yield return StartCoroutine(ProcessPlanningArea(planningArea));
        }
        yield break;
    }

    // Verarbeitet einen einzelnen Planungsraum
    IEnumerator ProcessPlanningArea(GameObject planningArea)
    {
        // Extrahiere das 2D‑Polygon (auf XZ‑Ebene) aus dem Planungsraum‑Mesh
        MeshFilter planningMF = planningArea.GetComponent<MeshFilter>();
        if (planningMF == null || planningMF.sharedMesh == null)
        {
            Debug.LogError("Planungsraum " + planningArea.name + " hat keinen gültigen MeshFilter.");
            yield break;
        }
        planningPolygon = GetPolygonFromMesh(planningMF.sharedMesh, planningArea.transform);
        Debug.Log(planningArea.name + " Polygon hat " + planningPolygon.Count + " Punkte.");

        // Berechne den Bounding Box des Planungsraum-Polygons:
        float polyXMin = planningPolygon.Min(p => p.x);
        float polyXMax = planningPolygon.Max(p => p.x);
        float polyZMin = planningPolygon.Min(p => p.y);
        float polyZMax = planningPolygon.Max(p => p.y);

        if (polyXMax < mapXMin || polyXMin > mapXMax || polyZMax < mapZMin || polyZMin > mapZMax)
        {
            Debug.Log("Planungsraum " + planningArea.name + " liegt außerhalb der 3D-Karte.");
            // Schreibe eine Zeile in die CSV mit 0 Sonnenanteil und 0 Gebäude
            WriteCSVForPlanningArea(planningArea.name, 0f, 0, 0);
            yield break;
        }



        // Ermittele die maximale Y-Höhe des Planungsraums (als Schwellenwert)
        float planningAreaHeight = GetMaxY(planningMF.sharedMesh, planningArea.transform);
        Debug.Log(planningArea.name + " oberste Höhe: " + planningAreaHeight.ToString("F2"));


        // Reset für diese Berechnung
        aggregatedTotalArea = 0f;
        aggregatedLitArea = 0f;

        // Erstelle eine finale Liste aller Gebäude, die (rekursiv) in den Parent-Objekten vorhanden sind
        List<GameObject> finalBuildingObjects = new List<GameObject>();
        foreach (GameObject parent in buildingObjects)
        {
            // Durchsuche rekursiv alle Child-Objekte
            Renderer[] childRenderers = parent.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in childRenderers)
            {
                if (r.sharedMaterials != null)
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m != null && m.name.Contains("Buildings"))
                        {
                            if (!finalBuildingObjects.Contains(r.gameObject))
                                finalBuildingObjects.Add(r.gameObject);
                            break;
                        }
                    }
                }
            }
        }
        Debug.Log("Anzahl gefundener Gebäude (mit Material 'Buildings'): " + finalBuildingObjects.Count);

        // Filtere die Gebäude, deren repräsentativer Punkt (z. B. Bounding Box Center) im Planungsraum liegt.
        List<GameObject> buildingsInArea = new List<GameObject>();
        foreach (GameObject building in finalBuildingObjects)
        {
            Renderer rend = building.GetComponent<Renderer>();
            if (rend == null)
                continue;
            Vector3 center = rend.bounds.center;
            Vector2 center2D = new Vector2(center.x, center.z);
            if (IsPointInPolygon(center2D, planningPolygon))
                buildingsInArea.Add(building);
        }
        Debug.Log("Planungsraum " + planningArea.name + " enthält " + buildingsInArea.Count + " Gebäude.");

        // Verarbeite jedes Gebäude in diesem Planungsraum
        foreach (GameObject building in buildingsInArea)
        {
            MeshFilter mf = building.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogWarning("Gebäude " + building.name + " hat keinen gültigen MeshFilter.");
                continue;
            }
            yield return StartCoroutine(ProcessMeshInChunks(
                mf.sharedMesh, building.transform, chunkSize, yieldInterval, samplesPerTriangle, planningAreaHeight,
                (float buildingTotal, float buildingLit) =>
                {
                    aggregatedTotalArea += buildingTotal;
                    aggregatedLitArea += buildingLit;
                    Debug.Log("Gebäude " + building.name + " -> Gesamtfläche: "
                        + buildingTotal.ToString("F2") + ", beleuchtet: " + buildingLit.ToString("F2"));
                }
            ));
            yield return null;
        }

        float exposurePercentage = aggregatedTotalArea > 0 ? (aggregatedLitArea / aggregatedTotalArea * 100f) : 0f;
        Debug.Log("Planungsraum " + planningArea.name + " hat einen Sonnenexpositionsprozentsatz von: " + exposurePercentage.ToString("F2") + "%");

        // Schreibe die Ergebnisse in die CSV-Datei
        WriteCSVForPlanningArea(planningArea.name, exposurePercentage, buildingsInArea.Count, aggregatedTotalArea);

        yield break;
    }

    // Diese Coroutine verarbeitet das Mesh eines Gebäudes in Blöcken (chunkweise).
    // Es werden nur Dreiecke berücksichtigt, deren Mittelpunkt (auf XZ) im Planungsraum liegt.
    // Für jedes Dreieck wird per Monte-Carlo-Sampling der Beleuchtungsanteil berechnet.
    IEnumerator ProcessMeshInChunks(Mesh mesh, Transform buildingTransform, int chunkSize, int yieldInterval, int samplesPerTriangle, float thresholdHeight, System.Action<float, float> onComplete)
    {
        float totalSurfaceArea = 0f;
        float litSurfaceArea = 0f;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        int totalTriangles = triangles.Length / 3;

        for (int i = 0; i < totalTriangles; i++)
        {
            int triIndex = i * 3;
            Vector3 v0 = buildingTransform.TransformPoint(vertices[triangles[triIndex]]);
            Vector3 v1 = buildingTransform.TransformPoint(vertices[triangles[triIndex + 1]]);
            Vector3 v2 = buildingTransform.TransformPoint(vertices[triangles[triIndex + 2]]);
            Vector3 centroid = (v0 + v1 + v2) / 3f;
            Vector2 centroid2D = new Vector2(centroid.x, centroid.z);

            // Nur Dreiecke berücksichtigen, deren Mittelpunkt im Planungsraum-Polygon liegt
            if (!IsPointInPolygon(centroid2D, planningPolygon))
                continue;

            // Zusätzlich: Ignoriere Dreiecke, deren Höhe (Centroid.y) nicht über dem Planungsraum liegt
            if (centroid.y <= thresholdHeight)
                continue;

            float area = Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
            totalSurfaceArea += area;

            // Dynamisch berechne die Anzahl der Samples anhand der Dreiecksfläche
            int dynamicSamples = Mathf.Clamp(Mathf.RoundToInt(baseSamples * (area / areaReference)), minSamples, maxSamples);
            float illuminatedFraction = SampleTriangleIllumination(v0, v1, v2, buildingTransform, dynamicSamples);
            litSurfaceArea += area * illuminatedFraction;

            if (i % yieldInterval == 0)
                yield return null;
        }

        onComplete(totalSurfaceArea, litSurfaceArea);
        yield return null;
    }


    // Führt für ein Dreieck baryzentrisches Sampling durch und liefert den Anteil der Stichproben, die als beleuchtet gewertet werden.
    // Zusätzlich werden Debug.DrawRay-Aufrufe zur Visualisierung ausgegeben.
    // Führt für ein Dreieck baryzentrisches Sampling durch und liefert den Anteil der Stichproben, die als beleuchtet gewertet werden.
    // Die Anzahl der Samples wird hierbei abhängig von der Dreiecksfläche bestimmt.
    float SampleTriangleIllumination(Vector3 v0, Vector3 v1, Vector3 v2, Transform buildingTransform, int samples)
    {
        int illuminatedCount = 0;
        // Berechne die Dreiecksnormal (für den Offset)
        Vector3 triangleNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
        // Definiere einen Mindest-Hit-Abstand, z. B. 0.02 Meter
        float minHitDistance = 0.02f;

        for (int i = 0; i < samples; i++)
        {
            float r1 = UnityEngine.Random.value;
            float r2 = UnityEngine.Random.value;
            if (r1 + r2 > 1)
            {
                r1 = 1 - r1;
                r2 = 1 - r2;
            }
            Vector3 samplePoint = v0 + r1 * (v1 - v0) + r2 * (v2 - v0);
            // Offset, um Selbsttreffer zu vermeiden – 1 cm außerhalb der Oberfläche
            Vector3 offsetPoint = samplePoint + triangleNormal * 0.01f;
            Vector3 currentSunDir = GetCurrentSunDirection();

            if (Physics.Raycast(offsetPoint, currentSunDir, out RaycastHit hit, Mathf.Infinity))
            {
                // Wenn der Treffer vom eigenen Gebäude stammt
                if (hit.collider.gameObject == buildingTransform.gameObject)
                {
                    // Wenn der Hit sehr nah ist, betrachten wir ihn als Selbsttreffer (also beleuchtet)
                    if (hit.distance < minHitDistance)
                    {
                        illuminatedCount++;
                        Debug.DrawRay(offsetPoint, currentSunDir * 5f, Color.green, 1f);
                    }
                    else
                    {
                        // Andernfalls wird angenommen, dass ein selbst erzeugter Schatten vorliegt
                        Debug.DrawRay(offsetPoint, currentSunDir * 5f, Color.red, 1f);
                    }
                }
                else
                {
                    Debug.DrawRay(offsetPoint, currentSunDir * 5f, Color.red, 1f);
                }
            }
            else
            {
                // Wenn nichts getroffen wird, nehmen wir an, der Punkt ist beleuchtet
                illuminatedCount++;
                Debug.DrawRay(offsetPoint, currentSunDir * 5f, Color.green, 1f);
            }
        }
        return (float)illuminatedCount / samples;
    }




    // Extrahiert aus einem Mesh (mit Transform) ein 2D-Polygon (auf XZ-Ebene) durch Projektion der Vertices in Weltkoordinaten.
    List<Vector2> GetPolygonFromMesh(Mesh mesh, Transform meshTransform)
    {
        Vector3[] localVerts = mesh.vertices;
        List<Vector2> worldVerts2D = new List<Vector2>();
        foreach (Vector3 v in localVerts)
        {
            Vector3 worldV = meshTransform.TransformPoint(v);
            worldVerts2D.Add(new Vector2(worldV.x, worldV.z));
        }
        // Berechne den Schwerpunkt zur Sortierung
        Vector2 centroid = Vector2.zero;
        foreach (Vector2 p in worldVerts2D)
            centroid += p;
        centroid /= worldVerts2D.Count;
        List<Vector2> sorted = worldVerts2D.OrderBy(p => Mathf.Atan2(p.y - centroid.y, p.x - centroid.x)).ToList();
        return sorted;
    }

    // Punkt-in-Polygon-Test (Ray-Casting-Algorithmus)
    bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        bool inside = false;
        int n = polygon.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    // Dynamisch die aktuelle Sonnenrichtung abrufen (berücksichtigt die Rotation des Licht-Objekts inklusive dessen Eltern)
    Vector3 GetCurrentSunDirection()
    {
        Light sun = FindObjectOfType<Light>();
        if (sun != null)
        {
            Vector3 sunDir = -sun.transform.forward;
            sunDir.Normalize();

            if (sunDir.y < -29.9f)
            {
                return Vector3.zero;
            }
            return sunDir;
        }
        return new Vector3(1, -1, 1).normalized;
    }

    // Schreibt eine Zeile in die CSV-Datei (alle Ergebnisse werden in eine Datei geschrieben)
    void WriteCSVForPlanningArea(string planningAreaName, float exposurePercentage, int buildingCount, float buildingArea)
    {
        SunCalculator sunCalculator = GameObject.Find("SunCalc").GetComponent<SunCalculator>();

        string date = sunCalculator.m_Day + "/" + sunCalculator.m_Month + "/" + sunCalculator.m_Year;
        string time = sunCalculator.m_Hour + ":" + sunCalculator.m_Minute + ":00";
        string line = planningAreaName + ";" + date + ";" + time + ";" + exposurePercentage.ToString("F2") + ";" + buildingCount + ";" + buildingArea.ToString("F2");
        using (StreamWriter writer = new StreamWriter(csvFilePath, true))
        {
            writer.WriteLine(line);
        }
        Debug.Log("Ergebnisse in CSV geschrieben: " + csvFilePath);
    }

    // Berechnet die maximale Y-Koordinate (Höhe) eines Meshes in Weltkoordinaten.
    float GetMaxY(Mesh mesh, Transform meshTransform)
    {
        float maxY = float.MinValue;
        foreach (Vector3 v in mesh.vertices)
        {
            float y = meshTransform.TransformPoint(v).y;
            if (y > maxY)
                maxY = y;
        }
        return maxY;
    }



}
