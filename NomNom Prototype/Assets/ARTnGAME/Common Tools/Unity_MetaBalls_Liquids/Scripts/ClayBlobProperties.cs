using UnityEngine;
namespace Artngame.CommonTools.Clay
{
    public class ClayBlobProperties : MonoBehaviour
    {
        public Color blobColor;
        public float blobBlend = 1;
        public enum blobTypes { sphere, box, torus }
        public blobTypes blobtype;
        public float radiusA = 1;
        public float radiusB = 0.5f;
        public float edgeFalloff = 1;
        public float blendFalloff = 2;
        public Vector3 colorOffset = new Vector3(1, 1, 1);
    }
}