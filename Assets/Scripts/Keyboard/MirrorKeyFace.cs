using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Keybound.Keyboard
{
    [RequireComponent(typeof(Image))]
    public sealed class MirrorKeyFace : BaseMeshEffect
    {
        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || !(graphic is Image image) || image.sprite == null)
                return;

            Vector4 spriteUv = DataUtility.GetOuterUV(image.sprite);
            UIVertex vertex = default;
            for (int index = 0; index < mesh.currentVertCount; index++)
            {
                mesh.PopulateUIVertex(ref vertex, index);
                vertex.uv0.x = spriteUv.x + spriteUv.z - vertex.uv0.x;
                mesh.SetUIVertex(vertex, index);
            }
        }
    }
}
