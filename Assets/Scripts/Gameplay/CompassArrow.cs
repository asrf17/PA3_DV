using UnityEngine;
using UnityEngine.UI;

namespace ForestJourney
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CompassArrow : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r = rectTransform.rect;
            Vector2[] points = {new Vector2(.5f,1), new Vector2(.12f,.16f), new Vector2(.5f,.35f), new Vector2(.88f,.16f)};
            foreach (var p in points) vh.AddVert(new Vector3(r.x + p.x*r.width, r.y + p.y*r.height), color, Vector2.zero);
            vh.AddTriangle(0,1,2); vh.AddTriangle(0,2,3);
        }
    }
}
