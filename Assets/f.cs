using UnityEngine;

public class FilterSubObjectsBySize : MonoBehaviour
{
    public GameObject parentObject;

    public float sizeThreshold = 100.0f;
    public float thicknessThreshold = 1f;
    public float ignoreYPosition = 3f;

    void Start()
    {
        if (parentObject == null)
            parentObject = gameObject;

        FilterChildObjects(parentObject.transform);
        FilterChildObjectsThick(parentObject.transform);
    }

    void FilterChildObjects(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                Vector3 center = renderer.bounds.center;

                if (center.y > ignoreYPosition)
                {
                    Debug.Log($"Objekt '{child.name}' wird ignoriert, da der Mittelpunkt ({center.y}) über {ignoreYPosition} liegt.");
                }
                else
                {
                    Vector3 size = renderer.bounds.size;
                    if (size.x > sizeThreshold && size.z > sizeThreshold)
                    {
                        Debug.Log($"Objekt '{child.name}' (Größe X: {size.x}, Z: {size.z}) überschreitet den Schwellenwert und wird deaktiviert.");
                        child.gameObject.SetActive(false);
                    }
                }
            }

            if (child.childCount > 0)
                FilterChildObjects(child);
        }
    }

    void FilterChildObjectsThick(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                Vector3 center = renderer.bounds.center;

                if (center.y > ignoreYPosition)
                {
                    Debug.Log($"Objekt '{child.name}' wird ignoriert, da der Mittelpunkt ({center.y}) über {ignoreYPosition} liegt.");
                }
                else
                {
                    float thickness = renderer.bounds.size.y;
                    if (thickness < thicknessThreshold)
                    {
                        Debug.Log($"Objekt '{child.name}' (Dicke: {thickness}) unterschreitet den Schwellenwert und wird deaktiviert.");
                        child.gameObject.SetActive(false);
                    }
                }
            }

            if (child.childCount > 0)
                FilterChildObjectsThick(child);
        }
    }
}
