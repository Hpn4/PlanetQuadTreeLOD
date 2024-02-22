using UnityEngine;

[System.Serializable]
public class SimpleNoiseSettings
{
    public Vector3 center = Vector3.zero;

    [Range(0, 8)]
    public int numLayers = 4;

    public int dropLayer;

    public int endLayer;
    
    public float persistence = 2;
    
    public float lacunarity = 2;
    
    public float scale = 1;
    
    public float strength = 1;
    
    public float verticalShift = 0;
}