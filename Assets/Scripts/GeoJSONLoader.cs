using UnityEngine;
using System.IO;
using System.Collections.Generic;
using SimpleJSON;
using LibTessDotNet;

public class GeoJSONLoader : MonoBehaviour
{
    // Name der GeoJSON-Datei (muss im StreamingAssets-Ordner liegen)
    public string geoJsonFileName = "berlin_planungsraeume.geojson";
    public string socialIndexFileName = "socialIndex.csv";

    private SocialIndexLoader socialIndexLoader;

    void Start()
    {
        // Erstelle ein übergeordnetes GameObject, das alle Planungsräume enthalten soll
        GameObject parent = new GameObject("Planungsräume");

        // Lade die Social-Index-Daten
        socialIndexLoader = gameObject.AddComponent<SocialIndexLoader>();
        socialIndexLoader.LoadSocialIndices(socialIndexFileName);

        // Pfad zur GeoJSON-Datei im StreamingAssets-Ordner
        string filePath = Path.Combine(Application.streamingAssetsPath, geoJsonFileName);
        string jsonText = File.ReadAllText(filePath);

        // Parse die GeoJSON-Datei
        var json = JSON.Parse(jsonText);
        var features = json["features"];

        // Iteriere über alle Features mittels .Children
        foreach (JSONNode feature in features.Children)
        {
            var properties = feature["properties"];
            string plrId = properties["PLR_ID"];
            var geometry = feature["geometry"];
            string geoType = geometry["type"];

            if (geoType == "MultiPolygon")
            {
                var multipolygons = geometry["coordinates"];
                foreach (JSONNode polygonArray in multipolygons.Children)
                {
                    var rings = polygonArray;
                    var outerRing = rings[0];

                    List<Vector2> polygonPoints = new List<Vector2>();
                    foreach (JSONNode point in outerRing.Children)
                    {
                        float x = point[0].AsFloat;
                        float y = point[1].AsFloat;
                        Vector2 convertedPoint = ConvertUTMToUnityCoordinates(x, y);
                        polygonPoints.Add(convertedPoint);
                    }

                    Mesh mesh = CreateMeshFromPolygon(polygonPoints);

                    // Erzeuge ein GameObject für den Planungsraum und setze es als Kind des Parent-Objekts
                    GameObject area = new GameObject("Planungsraum_" + plrId);
                    area.transform.SetParent(parent.transform);
                    MeshFilter mf = area.AddComponent<MeshFilter>();
                    MeshRenderer mr = area.AddComponent<MeshRenderer>();
                    mf.mesh = mesh;

                    // Hole die Farbe basierend auf dem Social-Index
                    Color areaColor = socialIndexLoader.GetColorForSocialIndex(plrId);
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = areaColor;
                    mr.material = mat;

                }
            }
            else if (geoType == "Polygon")
            {
                var rings = geometry["coordinates"];
                var outerRing = rings[0];

                List<Vector2> polygonPoints = new List<Vector2>();
                foreach (JSONNode point in outerRing.Children)
                {
                    float x = point[0].AsFloat;
                    float y = point[1].AsFloat;
                    Vector2 convertedPoint = ConvertUTMToUnityCoordinates(x, y);
                    polygonPoints.Add(convertedPoint);
                }
                Mesh mesh = CreateMeshFromPolygon(polygonPoints);

                GameObject area = new GameObject("Planungsraum_" + plrId);
                area.transform.SetParent(parent.transform);
                MeshFilter mf = area.AddComponent<MeshFilter>();
                MeshRenderer mr = area.AddComponent<MeshRenderer>();
                mf.mesh = mesh;

                Material mat = new Material(Shader.Find("Standard"));
                mat.color = Color.green;  // Farbe anpassen
                mr.material = mat;
            }
        }
        parent.transform.rotation = Quaternion.Euler(90, -1.1f, 0);
        parent.transform.position = new Vector3(32679, -29, -12677.8f);

    }

    Vector2 ConvertUTMToUnityCoordinates(float x, float y)
    {
        Vector2 offset = new Vector2(397000, 5806000);
        return new Vector2(x - offset.x, y - offset.y);
    }
    public Mesh CreateMeshFromPolygon(List<Vector2> polygon)
    {
        // Konvertiere die 2D-Punkte in eine Liste von ContourVertex
        List<ContourVertex> contourVertices = new List<ContourVertex>();
        foreach (Vector2 pt in polygon)
        {
            // LibTessDotNet arbeitet mit Vec3 - hier verwenden wir X und Y, wobei Y = 0 als Höhe dient
            ContourVertex cv = new ContourVertex();
            cv.Position = new Vec3(pt.x, 0, pt.y);
            contourVertices.Add(cv);
        }



        // Erstelle eine Tessellator-Instanz
        Tess tess = new Tess();
        tess.AddContour(contourVertices.ToArray(), ContourOrientation.Clockwise);
        tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

        // Erstelle die Vertex-Liste
        Vector3[] vertices = new Vector3[tess.Vertices.Length];
        for (int i = 0; i < tess.Vertices.Length; i++)
        {
            // Die Tessellator-Vektoren zurück in Unity-Vektoren konvertieren
            var v = tess.Vertices[i].Position;
            vertices[i] = new Vector3(v.X, v.Z, v.Y);
            // Hinweis: Je nach deiner gewünschten Achsenzuordnung musst du evtl. X, Y, Z anders mappen.
        }

        // Erhalte die Dreiecks-Indizes
        int[] indices = tess.Elements;

        // Erstelle und konfiguriere das Mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}

