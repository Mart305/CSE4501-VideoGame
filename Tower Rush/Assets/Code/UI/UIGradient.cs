using UnityEngine;
using UnityEngine.UI;

/// Simple UI Gradient effect for Images
[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    [SerializeField] private Color topColor = Color.white;
    [SerializeField] private Color bottomColor = Color.black;
    [SerializeField] private bool useHorizontal = false;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
            return;

        var vertexList = new System.Collections.Generic.List<UIVertex>();
        vh.GetUIVertexStream(vertexList);

        ModifyVertices(vertexList);

        vh.Clear();
        vh.AddUIVertexTriangleStream(vertexList);
    }

    private void ModifyVertices(System.Collections.Generic.List<UIVertex> vertexList)
    {
        if (vertexList.Count == 0)
            return;

        float bottomY = vertexList[0].position.y;
        float topY = vertexList[0].position.y;
        float leftX = vertexList[0].position.x;
        float rightX = vertexList[0].position.x;

        for (int i = 1; i < vertexList.Count; i++)
        {
            float y = vertexList[i].position.y;
            float x = vertexList[i].position.x;
            
            if (y > topY)
                topY = y;
            else if (y < bottomY)
                bottomY = y;
                
            if (x > rightX)
                rightX = x;
            else if (x < leftX)
                leftX = x;
        }

        float height = topY - bottomY;
        float width = rightX - leftX;

        for (int i = 0; i < vertexList.Count; i++)
        {
            UIVertex vertex = vertexList[i];

            if (useHorizontal)
            {
                float ratio = (vertex.position.x - leftX) / width;
                vertex.color *= Color.Lerp(topColor, bottomColor, ratio);
            }
            else
            {
                float ratio = (vertex.position.y - bottomY) / height;
                vertex.color *= Color.Lerp(bottomColor, topColor, ratio);
            }

            vertexList[i] = vertex;
        }
    }
}
