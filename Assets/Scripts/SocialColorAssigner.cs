using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SocialColorAssigner : MonoBehaviour
{
    // Name der CSV-Datei (muss im StreamingAssets-Ordner liegen)
    public string socialIndexFileName = "socialIndex.csv";

    // Dictionary zum Speichern der Social-Index-Daten, z.B. "08401245" => 2
    private Dictionary<string, int> socialIndexDict;

    void Start()
    {
        LoadSocialIndices();
        AssignColorsToMeshes();
    }

    // Liest die CSV-Datei und füllt das Dictionary
    void LoadSocialIndices()
    {
        socialIndexDict = new Dictionary<string, int>();
        string filePath = Path.Combine(Application.streamingAssetsPath, socialIndexFileName);

        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            // Überspringe die Header-Zeile (angenommen, die erste Zeile enthält "PLR_ID;SocialIndex")
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split(';');
                if (parts.Length >= 2)
                {
                    string plrId = parts[0].Trim();
                    int index;
                    if (int.TryParse(parts[1].Trim(), out index))
                    {
                        socialIndexDict[plrId] = index;
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Social index CSV nicht gefunden: " + filePath);
        }
    }

    // Durchläuft alle Kinder von "Planungsräume" und weist die Farbe anhand des Social-Index zu
    void AssignColorsToMeshes()
    {
        GameObject parent = GameObject.Find("Planungsräume");
        if (parent == null)
        {
            Debug.LogError("GameObject 'Planungsräume' nicht gefunden.");
            return;
        }

        foreach (Transform child in parent.transform)
        {
            // Annahme: der Name ist im Format "Planungsraum_PLR_ID"
            string childName = child.gameObject.name;
            string plrId = ExtractPlrIdFromName(childName);

            if (!string.IsNullOrEmpty(plrId) && socialIndexDict.ContainsKey(plrId))
            {
                int socialIndex = socialIndexDict[plrId];
                Color color = GetColorForSocialIndex(socialIndex);

                // Hole (oder füge hinzu) den MeshRenderer und weise ein neues Material zu
                MeshRenderer mr = child.gameObject.GetComponent<MeshRenderer>();
                if (mr == null)
                {
                    mr = child.gameObject.AddComponent<MeshRenderer>();
                }

                // Erstelle ein Material mit dem Standard-Shader (oder URP-Lit, falls du URP verwendest)
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
                mr.material = mat;
            }
            else
            {
                Debug.LogWarning("Kein Social-Index für PLR_ID gefunden: " + plrId);
            }
        }
    }

    // Extrahiert den PLR_ID-Teil aus dem Namen, z. B. aus "Planungsraum_08401245"
    string ExtractPlrIdFromName(string name)
    {
        string[] parts = name.Split('_');
        if (parts.Length >= 2)
        {
            return parts[1];
        }
        return "";
    }

    // Gibt eine Farbe basierend auf dem Social-Index zurück
    Color GetColorForSocialIndex(int index)
    {
        switch (index)
        {
            case 1: return Color.green;               // niedrigster Index
            case 2: return Color.yellow;
            case 3: return new Color(1f, 0.5f, 0f);     // Orange
            case 4: return Color.red;                   // höchster Index
            default: return Color.white;
        }
    }
}
