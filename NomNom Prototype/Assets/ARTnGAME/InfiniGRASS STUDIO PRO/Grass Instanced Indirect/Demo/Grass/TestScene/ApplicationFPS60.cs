using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Artngame.GIBLI.Utilities
{
    public class ApplicationFPS60 : MonoBehaviour
    {
        // Start is called before the first frame update
        void Awake()
        {
            Application.targetFrameRate = 60;
        }
    }
}