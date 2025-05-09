using UnityEngine;
using UnityEngine.UI;

public class ShadowGraph : MaskableGraphic
{
    public ButtonHandler CalcShadowForYear;
    public bool shouldDraw = false;

    public float maxYValue = 100f;
    public float graphPadding = 10f;
    public float lineThickness = 2f;
    public float axisThickness = 4f;
    public Color lineColor = Color.green;
    public Color axisColor = Color.black;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetComponent<RectTransform>().rect;
        float width = rect.width - 2 * graphPadding;
        float height = rect.height - 2 * graphPadding;



        float xStep = width / (CalcShadowForYear.shadowDataList.Count - 1);
        float yScale = height / maxYValue;

        Vector2 prevPoint = Vector2.zero;
        for (int i = 0; i < CalcShadowForYear.shadowDataList.Count; i++)
        {
            float shadowPercentage = CalcShadowForYear.shadowDataList[i].ShadowPercentage;

            float sunlightPercentage = 100f - shadowPercentage;


            float x = graphPadding + i * xStep;
            float y = graphPadding + sunlightPercentage * yScale;

            Vector2 currentPoint = new Vector2(x, y);

            if (i > 0)
            {
                AddLine(vh, prevPoint, currentPoint, lineThickness, lineColor);
            }

            prevPoint = currentPoint;
        }
        DrawAxes(vh, rect);
    }

    private void DrawAxes(VertexHelper vh, Rect rect)
    {
        Vector2 xStart = new Vector2(graphPadding - 2, graphPadding - 3);
        Vector2 xEnd = new Vector2(rect.width - graphPadding - 2, graphPadding - 3);
        AddLine(vh, xStart, xEnd, axisThickness, axisColor);
        AddArrow(vh, xEnd, Vector2.right, axisThickness, axisColor);

        Vector2 yStart = new Vector2(graphPadding, graphPadding - 2);
        Vector2 yEnd = new Vector2(graphPadding, rect.height - graphPadding - 2);
        AddLine(vh, yStart, yEnd, axisThickness, axisColor);
        AddArrow(vh, yEnd, Vector2.up, axisThickness, axisColor);
    }

    private void AddArrow(VertexHelper vh, Vector2 position, Vector2 direction, float thickness, Color color)
    {
        Vector2 dir = direction.normalized;
        float arrowSize = thickness * 3f;
        float arrowWidth = thickness * 2f;

        Vector2 tip = position + dir * arrowSize;
        Vector2 left = position + Vector2.Perpendicular(dir) * arrowWidth / 2;
        Vector2 right = position - Vector2.Perpendicular(dir) * arrowWidth / 2;

        int index = vh.currentVertCount;
        vh.AddVert(tip, color, Vector2.zero);
        vh.AddVert(left, color, Vector2.zero);
        vh.AddVert(right, color, Vector2.zero);

        vh.AddTriangle(index, index + 1, index + 2);
    }

    private void AddLine(VertexHelper vh, Vector2 start, Vector2 end, float thickness, Color color)
    {
        Vector2 dir = (end - start).normalized;
        Vector2 perp = Vector2.Perpendicular(dir) * thickness / 2;

        Vector2 v1 = start - perp;
        Vector2 v2 = start + perp;
        Vector2 v3 = end + perp;
        Vector2 v4 = end - perp;

        int index = vh.currentVertCount;

        vh.AddVert(v1, color, Vector2.zero);
        vh.AddVert(v2, color, Vector2.zero);
        vh.AddVert(v3, color, Vector2.zero);
        vh.AddVert(v4, color, Vector2.zero);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }


    public void DrawGraph()
    {
        shouldDraw = true;
        SetVerticesDirty();
    }
}
