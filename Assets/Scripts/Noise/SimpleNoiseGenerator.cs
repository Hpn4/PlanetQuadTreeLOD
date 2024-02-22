using UnityEngine;

public class SimpleNoiseGenerator
{
    private readonly SimplexNoise _noise;

    private readonly SimpleNoiseSettings _settings;

    public SimpleNoiseGenerator(SimplexNoise noise, SimpleNoiseSettings settings)
    {
        _noise = noise;
        _settings = settings;
    }
    
    public float Evaluate(Vector3 pos) {
        // Sum up noise layers
        float noiseSum = 0;
        float amplitude = 1;
        float frequency = _settings.scale;
        
        for (int i = 0; i < _settings.numLayers; i ++)
        {
            float n = _noise.Evaluate(pos * frequency + _settings.center);
            float ridge = 1 - Mathf.Abs(n);
            n = Mathf.Lerp(n, ridge, _settings.verticalShift);
            
            noiseSum += n * amplitude;
            amplitude *= _settings.persistence;
            frequency *= _settings.lacunarity;
        }

        for (int i = 0; i < _settings.dropLayer; ++i)
        {
            amplitude *= _settings.persistence;
            frequency *= _settings.lacunarity;
        }
        
        for (int i = 0; i < _settings.endLayer; i ++)
        {
            float n = _noise.Evaluate(pos * frequency + _settings.center);
            float ridge = 1 - Mathf.Abs(n);
            n = Mathf.Lerp(n, ridge, _settings.verticalShift);
            
            noiseSum += n * amplitude;
            amplitude *= _settings.persistence;
            frequency *= _settings.lacunarity;
        }

        return noiseSum * _settings.strength;// + settings.verticalShift;
        //return (int) (h * settings.verticalShift) / settings.verticalShift;
    }
}