using System.Collections.Generic;
using UnityEngine;

public class Face
{
    private readonly Chunk _chunk;

    private readonly Mesh _mesh;

    public int id;

    // Cached value
    private List<Chunk> _cachedChunks;

    private readonly MeshCollider _collider;

    // Variables for children chunk
    public Vector3 axisA;

    public Vector3 axisB;

    public Face(int id, Planet planet, Vector3 localUp)
    {
        this.id = id;
        _mesh = new Mesh(); // Mesh for the face

        // Axis of the parent chunk
        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);

        // We create the parent chunk
        _chunk = new Chunk(null, this, planet, localUp * planet.radius, planet.radius, 0, 1);
        _cachedChunks = new List<Chunk>();

        // We create the face gameObject
        GameObject face = new GameObject("Face")
        {
            transform =
            {
                parent = planet.transform,
                localPosition = Vector3.zero
            }
        };

        face.AddComponent<MeshFilter>().mesh = _mesh;
        face.AddComponent<MeshRenderer>().sharedMaterial = planet.mat;
        _collider = face.AddComponent<MeshCollider>();
    }

    public void Update()
    {
        // We get all the visible chunk (the leaf of the tree)
        List<Chunk> chunks = new List<Chunk>();
        _chunk.GenerateVisibleChildren(chunks);

        // We verify that the new visible chunks are not the same as the precedent frame
        // This scenario happens if the player make little movement
        if (chunks.Count == _cachedChunks.Count)
        {
            int i = 0;
            while (i < chunks.Count && chunks[i].Equal(_cachedChunks[i]))
                ++i;

            if (i >= chunks.Count)
                return;
        }

        _cachedChunks = chunks;

        // We then regenerate the mesh of the face
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();
        Dictionary<Vector3, int> vectI = new Dictionary<Vector3, int>();
        int triangleOffset = 0;

        foreach (Chunk chunk in chunks)
        {
            chunk.Generate();
            triangleOffset += chunk.AddToMesh(vertices, normals, triangles, vectI, triangleOffset);
        }

        _mesh.Clear();
        _mesh.vertices = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
        _mesh.normals = normals.ToArray();
        _mesh.RecalculateTangents();

        _collider.sharedMesh = _mesh;
    }
}