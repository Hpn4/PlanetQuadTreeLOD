using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [Range(4, 10)]
    public int resolution = 5;

    public float radius;

    public int seed;

    public SimpleNoiseGenerator generator;

    public SimpleNoiseSettings settings;
    
    public Material mat;

    public float[] detailLevel = {1500, 1000, 800, 500, 300, 100};

    public Transform player;

    private Face[] _faces;

    public int[,] VertGenIndices { get; private set; }
    
    // Optimisation
    private Vector3 _lastPlayerPos;

    public void Start()
    {
        _lastPlayerPos = Vector3.zero;
        Application.targetFrameRate = 60;
        
        // Setup cube faces
        Vector3[] facesNormal = {Vector3.up, Vector3.down, Vector3.back, Vector3.forward, Vector3.right, Vector3.left};
        _faces = new Face[6];

        for (int i = 0; i < 6; i++)
            _faces[i] = new Face(i, this, facesNormal[i]);

        SimplexNoise noise = new SimplexNoise(seed);
        generator = new SimpleNoiseGenerator(noise, settings);
        
        GenerateVertexIndices();
    }

    private void GenerateVertexIndices()
    {
        int size = resolution + 2;
        int meshVertIndex = 0;
        int edgeVertIndex = -1;
        VertGenIndices = new int[size, size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool onEdges = y == 0 || x == 0 || y == size - 1 || x == size - 1;
            if (onEdges)
                VertGenIndices[x, y] = edgeVertIndex--;
            else
                VertGenIndices[x, y] = meshVertIndex++;
        }
    }

    public void Update()
    {
        // Avoid to recalculate everything if the player don't move
        if (player.position != _lastPlayerPos)
        {
            _lastPlayerPos = player.position;
            foreach (Face face in _faces)
                face.Update();
        }
    }

    public void Attract(Rigidbody body)
    {
        Vector3 gravityUp = (body.position - transform.position).normalized;
        Vector3 localUp = body.transform.up;

        // Apply downwards gravity to body
        body.AddForce(gravityUp * -10);
        // Allign bodies up axis with the centre of planet
        body.rotation = Quaternion.FromToRotation(localUp, gravityUp) * body.rotation;
    }
}