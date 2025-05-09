using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

public class PlanningAreaOnMapChecker : MonoBehaviour
{
    [Header("3D-Karten-Grenzen (in Weltkoordinaten)")]
    public float mapXMin = 14952.97f;
    public float mapXMax = 34152.97f;
    public float mapZMin = -5365f;
    public float mapZMax = 4835f;

    [Header("Planungsräume (als GameObjects mit MeshFilter)")]
    public List<GameObject> planningAreaObjects;

    // CSV-Dateipfad (wird im persistentDataPath gespeichert)
    private string csvFilePath;

    // Diese Methode startet den Check – z. B. per Button OnClick
    public void CheckPlanningAreas()
    {
        if (planningAreaObjects == null || planningAreaObjects.Count == 0)
        {
            Debug.LogError("Keine Planungsraum-Objekte zugewiesen!");
            return;
        }

        // Setze den CSV-Pfad und schreibe den Header
        csvFilePath = Path.Combine(Application.persistentDataPath, "PlanningAreas_OnMap.csv");
        using (StreamWriter writer = new StreamWriter(csvFilePath, false))
        {
            writer.WriteLine("Planungsraum;Lage"); // Lage: 1 = vollständig auf der Karte, 0 = nicht vollständig
        }
        Debug.Log("CSV-Datei initialisiert: " + csvFilePath);

        // Gehe jeden Planungsraum durch und prüfe, ob er vollständig innerhalb der Grenzen liegt
        foreach (GameObject planningArea in planningAreaObjects)
        {
            bool fullyOnMap = IsPlanningAreaOnMap(planningArea);
            WriteCSVForPlanningArea(planningArea.name, fullyOnMap);
        }

        Debug.Log("Prüfung abgeschlossen. Ergebnisse in CSV geschrieben: " + csvFilePath);
    }

    // Überprüft, ob alle Vertizes des Planungsraum-Mesh innerhalb der Karten-Grenzen liegen
    bool IsPlanningAreaOnMap(GameObject planningArea)
    {
        MeshFilter mf = planningArea.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError("Planungsraum " + planningArea.name + " hat keinen gültigen MeshFilter.");
            return false;
        }

        Mesh mesh = mf.sharedMesh;
        Vector3[] localVerts = mesh.vertices;
        foreach (Vector3 v in localVerts)
        {
            // Transformiere den lokalen Vertex in Weltkoordinaten
            Vector3 worldV = planningArea.transform.TransformPoint(v);
            // Prüfe nur die XZ-Ebene
            if (worldV.x < mapXMin || worldV.x > mapXMax || worldV.z < mapZMin || worldV.z > mapZMax)
            {
                return false; // Mindestens ein Vertex liegt außerhalb
            }
        }
        return true; // Alle Vertex liegen innerhalb der Grenzen
    }

    // Schreibt eine Zeile in die CSV-Datei mit Planungsraum-Namen und Lage (1 oder 0)
    void WriteCSVForPlanningArea(string planningAreaName, bool onMap)
    {
        string line = planningAreaName + ";" + (onMap ? "1" : "0");
        using (StreamWriter writer = new StreamWriter(csvFilePath, true))
        {
            writer.WriteLine(line);
        }
    }
}
