using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;

public class AddressLocator : MonoBehaviour
{
    public InputField addressInput;
    public Camera mapCamera;
    private string geocodeUrl = "https://nominatim.openstreetmap.org/search?q={0}&format=json";

    private const float leftLon = 13.224508f;
    private const float rightLon = 13.507701f;
    private const float topLat = 52.551110f;
    private const float bottomLat = 52.460448f;


    public void OnSearchButtonClicked()
    {
        string adresse = addressInput.text;
        StartCoroutine(GetCoordinates(adresse));
    }

    public float[] ConvertLatLonToUnity(float lat, float lon)
    {
        float normalizedX = (lon - leftLon) / (rightLon - leftLon);
        float normalizedZ = (lat - bottomLat) / (topLat - bottomLat);

        float posX = 14953f + (normalizedX * 19200f);
        float posZ = -5395f + (normalizedZ * 10200f);



        return new float[] { posX, posZ };
    }

    IEnumerator GetCoordinates(string adresse)
    {
        string url = string.Format(geocodeUrl, UnityWebRequest.EscapeURL(adresse));
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("User-Agent", "UnityApp");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var jsonResult = SimpleJSON.JSON.Parse(request.downloadHandler.text);
            if (jsonResult != null && jsonResult.Count > 0)
            {
                float lat = jsonResult[0]["lat"].AsFloat;
                float lon = jsonResult[0]["lon"].AsFloat;

                Debug.Log("lat: " + lat + ", lon: " + lon);

                float[] location = ConvertLatLonToUnity(lat, lon);

                if (lat >= bottomLat && lat <= topLat && lon >= leftLon && lon <= rightLon)
                {
                    mapCamera.transform.position = new Vector3(location[0], mapCamera.transform.position.y, location[1]);
                }
                else
                {
                    Debug.Log("Destination outside the map, camera stays unchanged.");
                }
            }
            else
            {
                Debug.LogError("No results found.");
            }
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }
}
