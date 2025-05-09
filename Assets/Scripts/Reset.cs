using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class Reset : MonoBehaviour
{
    public ButtonHandler CalcShadowForYear;
    public PlanePlacerUI planePlacerUI;

    public void OnButtonClick()
    {
        RemovePVPlanes();
        RemoveShadowData();
    }

    private void RemovePVPlanes()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        planePlacerUI.planeExists = false;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "Plane(Clone)" && obj.GetComponent<MeshFilter>()?.sharedMesh?.name == "Plane")
            {
                Destroy(obj);
            }
        }
    }

    private void RemoveShadowData()
    {
        CalcShadowForYear.shadowDataList.Clear();
    }
}