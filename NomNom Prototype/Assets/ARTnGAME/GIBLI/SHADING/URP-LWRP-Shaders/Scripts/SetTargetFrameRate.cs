using UnityEngine;
namespace Artngame.GIBLI
{
    public class SetTargetFrameRate : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 60;
        }
    }
}