using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AnthonyExtensionMethods
{
    // Extension method
    //  needs to be in static class and static function
    public static Vector3 Round(this Vector3 v)
    {
        v.x = Mathf.Round(v.x);
        v.y = Mathf.Round(v.y);
        v.z = Mathf.Round(v.z);
        return v;
    }

    // Round to other unit than 1
    public static Vector3 Round(this Vector3 v, float size)
    {
        return (v / size).Round() * size;
    }

    public static float Round(this float v, float size)
    {
        return Mathf.Round(v / size) * size;
;    }

    public static float AtLeast(this float v, float min) => Mathf.Max(v, min);
    public static int AtLeast(this int v, int min) => Mathf.Max(v, min);
}