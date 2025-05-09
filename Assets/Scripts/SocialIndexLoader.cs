using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SocialIndexLoader : MonoBehaviour
{
    public Dictionary<string, int> SocialIndexDict = new Dictionary<string, int>();

    // Lädt die Social-Index-Daten aus der CSV-Datei
    public void LoadSocialIndices(string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                // Überspringe die Header-Zeile
                if (line.StartsWith("PLR_ID"))
                    continue;

                string[] parts = line.Split(';');
                if (parts.Length >= 2)
                {
                    string plrId = parts[0].Trim();
                    int index;
                    if (int.TryParse(parts[1].Trim(), out index))
                    {
                        SocialIndexDict[plrId] = index;
                    }
                }
            }
        }
        else
        {
            Debug.LogError("CSV file not found: " + filePath);
        }
    }

    // Gibt eine Farbe basierend auf dem Social-Index zurück
    public Color GetColorForSocialIndex(string plrId)
    {
        if (SocialIndexDict.ContainsKey(plrId))
        {
            int index = SocialIndexDict[plrId];
            switch (index)
            {
                case 1: return Color.green;               // niedrigster Index
                case 2: return Color.yellow;
                case 3: return new Color(1f, 0.5f, 0f);     // Orange
                case 4: return Color.red;                   // höchster Index
                default: return Color.white;
            }
        }
        return Color.white; // Fallback, falls keine Zuordnung gefunden wurde
    }
}
