using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Globalization;

public class ButtonHandler : MonoBehaviour
{

    public InputField startDateInput;
    public InputField endDateInput;
    public InputField startTimeInput;
    public InputField endTimeInput;
    public InputField stepInput;
    public Dropdown dropdown;
    private string dropdownText = "Minutes";
    public Text sun;
    public Text energy;
    public InputField height;
    public InputField width;
    private DateTime startDate;
    private DateTime endDate;
    public float percentage;

    public class ShadowData
    {
        public float ShadowPercentage { get; set; }
        public string Timestamp { get; set; }
    }
    public List<ShadowData> shadowDataList = new List<ShadowData>();

    public ShadowGraph makeGraph;

    public DateSlider slider;

    void Start()
    {
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        startDateInput.text = "01/01/2025";
        endDateInput.text = "01/01/2025";
        startTimeInput.text = "00:00:00";
        endTimeInput.text = "00:00:00";
    }

    void OnDropdownValueChanged(int index)
    {
        dropdownText = dropdown.options[index].text;
    }

    public void OnButtonClick()
    {
        StartCoroutine(CalcShadowCoroutine());
    }

    private IEnumerator CalcShadowCoroutine()
    {
        SunCalculator sunCalculator = GameObject.Find("SunCalc").GetComponent<SunCalculator>();

        int stepValue = int.Parse(stepInput.text);
        string stepUnit = dropdownText;

        string dateFormat = "dd/MM/yyyy HH:mm:ss";

        CultureInfo provider = CultureInfo.InvariantCulture;

        string startDateTime = startDateInput.text + " " + startTimeInput.text;
        string endDateTime = endDateInput.text + " " + endTimeInput.text;
        startDate = DateTime.ParseExact(startDateTime, dateFormat, provider);
        endDate = DateTime.ParseExact(endDateTime, dateFormat, provider);

        List<float> percentages = new List<float>();


        for (DateTime current = startDate; current <= endDate; current = IncrementDateTime(current, stepValue, stepUnit))
        {

            sunCalculator.m_Year = current.Year;
            sunCalculator.m_Month = current.Month;
            sunCalculator.m_Day = current.Day;
            sunCalculator.m_Hour = current.Hour;
            sunCalculator.m_Minute = current.Minute;

            yield return new WaitForEndOfFrame();

            percentage = CalculateShadowPercentage();

            shadowDataList.Add(new ShadowData
            {
                ShadowPercentage = percentage,
                Timestamp = $"{current.Month}/{current.Day} hour:{current.Hour}"
            });

            Debug.Log("Calculated percentage for " + current.Month + "/" + current.Day + " hour:" + current.Hour + ": " + percentage);
            percentages.Add(percentage);


            slider.UpdateSliderRange();

        }



        if (percentages.Count > 0)
        {
            float shadowAverage = 0f;
            foreach (float p in percentages)
            {
                shadowAverage += p;
            }
            shadowAverage /= percentages.Count;
            shadowAverage = 100 - shadowAverage;
            Debug.Log("Average Sun Percentage: " + shadowAverage);
            sun.text = "Sun component: " + shadowAverage.ToString("F2") + "%";
            OnCalculationComplete();
            percentages.Clear();
            TimeSpan timeSpan = endDate - startDate;
            float en = ((float.Parse(width.text) * float.Parse(height.text)) * shadowAverage / 100) * (float)timeSpan.TotalHours;
            energy.text = "Energy output: " + en.ToString("F2") + "kWh";

        }
        else
        {
            sun.text = "No data";
            energy.text = "No data";
        }
    }

    static DateTime IncrementDateTime(DateTime current, int stepValue, string stepUnit)
    {
        return stepUnit switch
        {
            "Minutes" => current.AddMinutes(stepValue),
            "Hours" => current.AddHours(stepValue),
            "Days" => current.AddDays(stepValue),
            _ => throw new ArgumentException($"Invalid unit: {stepUnit}")
        };
    }

    private float CalculateShadowPercentage()
    {
        Renderer planeRenderer = GameObject.Find("Plane(Clone)").GetComponent<Renderer>();
        Light directionalLight = FindObjectOfType<Light>();
        int gridResolution = 10;
        ButtonHandler shadowForYear = GameObject.Find("CalcShadowForYear").GetComponent<ButtonHandler>();
        if (planeRenderer == null || directionalLight == null)
            return 0;

        Transform planeTransform = planeRenderer.transform;
        Vector3 planeScale = planeTransform.localScale;

        float scaleX = 50 / (planeRenderer.transform.localScale.x * 5);
        float scaleZ = 50 / (planeRenderer.transform.localScale.z * 5);

        int shadowedPoints = 0;
        int totalPoints = gridResolution * gridResolution;

        for (int x = 0; x < gridResolution; x++)
        {
            for (int z = 0; z < gridResolution; z++)
            {
                float normalizedX = (float)x / (gridResolution - 1);
                float normalizedZ = (float)z / (gridResolution - 1);

                Vector3 localPoint = new Vector3(
                    (normalizedX - 0.5f) * planeScale.x * scaleX, 0, (normalizedZ - 0.5f) * planeScale.z * scaleZ);

                Vector3 worldPoint = planeTransform.TransformPoint(localPoint);


                Vector3 lightDirection = -directionalLight.transform.forward;
                bool isShadowed = false;

                if (Physics.Raycast(worldPoint, lightDirection, out RaycastHit hit))
                {
                    if (hit.collider != planeRenderer)
                    {
                        isShadowed = true;
                        shadowedPoints++;
                    }
                }

                Color rayColor = isShadowed ? Color.red : Color.green;
                Debug.DrawRay(worldPoint, lightDirection * 5f, rayColor, 0.1f);
            }
        }

        float shadowPercentage = (float)shadowedPoints / totalPoints * 100f;
        return shadowPercentage;
    }

    private float CalculateShadowPercentageOnBuildings()
    {
        // Finde alle Renderer, die das Material "Buildings" nutzen
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        List<Renderer> buildingRenderers = new List<Renderer>();
        foreach (Renderer r in allRenderers)
        {
            if (r.sharedMaterials != null)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null && m.name.Contains("Buildings"))
                    {
                        buildingRenderers.Add(r);
                        break; // Füge diesen Renderer nur einmal hinzu
                    }
                }
            }
        }

        if (buildingRenderers.Count == 0)
        {
            Debug.LogWarning("Keine Gebäude mit Material 'Buildings' gefunden.");
            return 0;
        }

        int totalPoints = 0;
        int shadowedPoints = 0;
        int gridResolution = 3; // Erhöhe die Auflösung, um mehr Punkte zu erhalten
        Light directionalLight = FindObjectOfType<Light>();
        if (directionalLight == null)
        {
            Debug.LogError("Kein Light in der Szene gefunden!");
            return 0;
        }
        // Bestimme die Licht-Richtung (angenommen, diese bleibt konstant)
        Vector3 lightDirection = -directionalLight.transform.forward;

        foreach (Renderer b in buildingRenderers)
        {
            // Debug: Name und Position des Gebäudes
            Debug.Log("Verarbeite Gebäude: " + b.gameObject.name);
            // Prüfe, ob ein Collider vorhanden ist
            Collider col = b.GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning("Gebäude " + b.gameObject.name + " hat keinen Collider!");
                continue;
            }

            // Nutze die Bounding Box des Gebäudes als Dachfläche (Achtung: Bei rotierten Objekten ist dies ggf. nicht exakt)
            Bounds bounds = b.bounds;
            float topY = bounds.max.y;

            for (int i = 0; i < gridResolution; i++)
            {
                for (int j = 0; j < gridResolution; j++)
                {
                    float tX = (float)i / (gridResolution - 1);
                    float tZ = (float)j / (gridResolution - 1);
                    float sampleX = Mathf.Lerp(bounds.min.x, bounds.max.x, tX);
                    float sampleZ = Mathf.Lerp(bounds.min.z, bounds.max.z, tZ);
                    Vector3 samplePoint = new Vector3(sampleX, topY, sampleZ);
                    totalPoints++;

                    bool isShadowed = false;
                    if (Physics.Raycast(samplePoint, lightDirection, out RaycastHit hit))
                    {
                        // Falls der getroffene Raycast nicht auf das gleiche Gebäude zeigt, werten wir den Punkt als schattig
                        if (hit.collider.gameObject != b.gameObject)
                        {
                            isShadowed = true;
                            shadowedPoints++;
                        }
                    }
                    // Zeichne den Ray mit längerer Dauer, damit du ihn im Scene View sehen kannst
                    Color rayColor = isShadowed ? Color.red : Color.green;
                    Debug.DrawRay(samplePoint, lightDirection * 50f, rayColor, 10f);
                }
            }
        }

        float shadowPercentage = totalPoints > 0 ? (float)shadowedPoints / totalPoints * 100f : 0;
        Debug.Log("Berechneter Schattenanteil auf Gebäuden: " + shadowPercentage + "% (an " + totalPoints + " Punkten)");
        return shadowPercentage;
    }



    public void OnCalculationComplete()
    {
        makeGraph.DrawGraph();
        Debug.Log("Graph drawn!");
    }
}