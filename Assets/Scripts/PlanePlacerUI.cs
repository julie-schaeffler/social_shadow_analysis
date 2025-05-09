using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;


public class PlanePlacerUI : MonoBehaviour
{
    public InputField widthInput;
    public InputField heightInput;
    public Button placePlaneButton;
    public GameObject pvPlanePrefab;

    public Transform allowedParent;

    private float planeWidth = 1f;
    private float planeHeight = 1f;
    private bool isPlacing = false;
    public bool planeExists = false;

    void Start()
    {
        placePlaneButton.onClick.AddListener(OnPlacePlaneClicked);
    }

    void Update()
    {
        if (isPlacing && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                GameObject clickedUI = GetClickedUIElement();

                if (clickedUI != null && clickedUI.transform.IsChildOf(allowedParent.transform))
                {
                    Debug.Log("Click on UI element within the allowed canvas recognised.");
                }
                else
                {
                    Debug.Log("Click on UI detected, placement is cancelled.");
                    return;
                }
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Building"))
                {
                    Debug.Log("Click on Building.");
                    CreatePlane(hit.point, hit.normal);
                }
            }
        }
    }

    private GameObject GetClickedUIElement()
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);

        if (results.Count > 0)
        {
            return results[0].gameObject;
        }

        return null;
    }

    void OnPlacePlaneClicked()
    {
        if (float.TryParse(widthInput.text, out float width) && float.TryParse(heightInput.text, out float height))
        {
            planeWidth = width / 5;
            planeHeight = height / 5;
            isPlacing = true;
        }
        else
        {
            Debug.LogError("Invalid input for width or height");
        }
    }

    void CreatePlane(Vector3 position, Vector3 normal)
    {
        if (!planeExists)
        {
            GameObject plane = Instantiate(pvPlanePrefab, position, Quaternion.identity);

            plane.transform.position = position + normal * 0.1f;

            AlignPlaneToSurface(plane, normal);

            AlignScaleToRotation(plane);

            planeExists = true;
        }


    }

    void AlignPlaneToSurface(GameObject plane, Vector3 normal)
    {
        Quaternion surfaceRotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, normal), normal);
        plane.transform.rotation = surfaceRotation;
    }

    void AlignScaleToRotation(GameObject plane)
    {
        Vector3 newScale = new Vector3(planeWidth, 1f, planeHeight);
        plane.transform.localScale = newScale;
    }
}