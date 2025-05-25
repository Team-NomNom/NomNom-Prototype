using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;
public class Constants
{
    public enum Colors { Red, Blue };

    public static Material redMaterial => Resources.Load<Material>("Materials/Red");
    public static Material blueMaterial => Resources.Load<Material>("Materials/Blue");

}