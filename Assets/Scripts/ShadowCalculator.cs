using UnityEngine;

public class ShadowCalculator : MonoBehaviour
{
    public Renderer planeRenderer;
    private Light directionalLight;

    public int gridResolution = 10;


    private void Start()
    {
        directionalLight = FindObjectOfType<Light>();

    }

    private void Update()
    {
        CalculateShadowPercentage();
    }

    private void CalculateShadowPercentage()
    {
        ButtonHandler shadowForYear = GameObject.Find("CalcShadowForYear").GetComponent<ButtonHandler>();
        if (planeRenderer == null || directionalLight == null)
            return;

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

    }
}