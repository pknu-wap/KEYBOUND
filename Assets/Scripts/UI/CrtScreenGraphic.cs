using UnityEngine;
using UnityEngine.UI;

namespace Keybound.UI
{
    [AddComponentMenu("UI/CRT Screen Graphic")]
    public sealed class CrtScreenGraphic : MaskableGraphic
    {
        [SerializeField] private bool showMissionDivider;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect screen = GetPixelAdjustedRect();
            if (screen.width <= 0f || screen.height <= 0f)
                return;

            float shortestSide = Mathf.Min(screen.width, screen.height);
            float scale = Mathf.Min(1f, shortestSide / 46f);
            float corner = Mathf.Min(13f, shortestSide * 0.16f);
            float glowWidth = 5f * scale;
            DrawRing(mesh, Inset(screen, -glowWidth), corner + glowWidth * 0.59f, screen, corner,
                new Color32(119, 151, 104, 0), new Color32(119, 151, 104, 62));

            int first = mesh.currentVertCount;
            AddChamferedPerimeter(mesh, screen, corner,
                new Color32(27, 39, 29, 252), new Color32(11, 18, 13, 250));
            for (int i = 1; i < 7; i++)
                mesh.AddTriangle(first, first + i, first + i + 1);

            for (float y = screen.yMin + 4f; y < screen.yMax - 3f; y += 5f)
            {
                float inset = Mathf.Max(3f, corner - Mathf.Min(y - screen.yMin, screen.yMax - y) + 2f);
                DrawStrip(mesh, new Rect(screen.xMin + inset, y, screen.width - inset * 2f, 1f),
                    new Color32(142, 166, 123, 13));
            }

            // Fade the edges of the phosphor display into the dark glass.
            float vignetteWidth = 14f * scale;
            DrawRing(mesh, screen, corner, Inset(screen, vignetteWidth), corner - vignetteWidth * 0.59f,
                new Color32(0, 0, 0, 117), new Color32(0, 0, 0, 0));
            DrawRing(mesh, screen, corner, Inset(screen, 1.25f * scale), corner - 0.75f * scale,
                new Color32(88, 104, 82, 190), new Color32(139, 160, 123, 218));
            DrawRing(mesh, Inset(screen, 4f * scale), corner - 2.4f * scale,
                Inset(screen, 4.75f * scale), corner - 2.85f * scale,
                new Color32(144, 169, 125, 60), new Color32(144, 169, 125, 60));

            if (showMissionDivider)
            {
                const float statusHeight = 58f;
                const float inset = 24f;
                var dividerColor = new Color32(139, 160, 123, 92);
                float statusBottom = screen.yMax - statusHeight;
                DrawStrip(mesh, new Rect(screen.xMin + inset, statusBottom, screen.width - inset * 2f, 1f),
                    dividerColor);
                float missionBottom = screen.yMin + 54f;
                DrawStrip(mesh, new Rect(screen.xMin + 171f, missionBottom, 1f, statusBottom - inset - missionBottom),
                    dividerColor);
            }
        }

        private void DrawRing(VertexHelper mesh, Rect outer, float outerCorner, Rect inner, float innerCorner,
            Color outerColor, Color innerColor)
        {
            int first = mesh.currentVertCount;
            AddChamferedPerimeter(mesh, outer, outerCorner, outerColor, outerColor);
            AddChamferedPerimeter(mesh, inner, innerCorner, innerColor, innerColor);
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                mesh.AddTriangle(first + i, first + next, first + 8 + next);
                mesh.AddTriangle(first + i, first + 8 + next, first + 8 + i);
            }
        }

        private void AddChamferedPerimeter(VertexHelper mesh, Rect rect, float corner, Color top, Color bottom)
        {
            corner = Mathf.Clamp(corner, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            top *= color;
            bottom *= color;
            Color lowSide = Color.Lerp(bottom, top, corner / rect.height);
            Color highSide = Color.Lerp(top, bottom, corner / rect.height);
            AddVertex(mesh, rect.xMin + corner, rect.yMin, bottom);
            AddVertex(mesh, rect.xMin, rect.yMin + corner, lowSide);
            AddVertex(mesh, rect.xMin, rect.yMax - corner, highSide);
            AddVertex(mesh, rect.xMin + corner, rect.yMax, top);
            AddVertex(mesh, rect.xMax - corner, rect.yMax, top);
            AddVertex(mesh, rect.xMax, rect.yMax - corner, highSide);
            AddVertex(mesh, rect.xMax, rect.yMin + corner, lowSide);
            AddVertex(mesh, rect.xMax - corner, rect.yMin, bottom);
        }

        private void DrawStrip(VertexHelper mesh, Rect rect, Color tint)
        {
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            int first = mesh.currentVertCount;
            tint *= color;
            AddVertex(mesh, rect.xMin, rect.yMin, tint);
            AddVertex(mesh, rect.xMin, rect.yMax, tint);
            AddVertex(mesh, rect.xMax, rect.yMax, tint);
            AddVertex(mesh, rect.xMax, rect.yMin, tint);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        private static Rect Inset(Rect rect, float inset) =>
            Rect.MinMaxRect(rect.xMin + inset, rect.yMin + inset, rect.xMax - inset, rect.yMax - inset);

        private static void AddVertex(VertexHelper mesh, float x, float y, Color32 tint) =>
            mesh.AddVert(new Vector3(x, y, 0f), tint, Vector2.zero);
    }
}
