using System;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    // Reference to the planet and the face
    private readonly Planet _planet;

    private readonly Face _face;

    // Mesh generation data
    private Vector3 _position;

    private readonly float _radius;
    
    // Mesh data
    private Vector3[] _vertices;

    private int[] _triangles;

    private Vector3[] _borderVertices;

    private int[] _borderTriangles;

    private List<int> _leftmerged;

    // Chunk data
    private readonly int _lodLevel;

    private readonly Chunk _parent;

    private Chunk[] _children;

    private readonly int _hashValue;

    public Chunk(Chunk parent, Face face, Planet planet, Vector3 position, float radius, int lodLevel, int hashValue)
    {
        _parent = parent;
        _face = face;
        _planet = planet;
        _position = position;
        _radius = radius;
        _lodLevel = lodLevel;
        
        _children = Array.Empty<Chunk>();
        _hashValue = hashValue;
    }

    public void GenerateVisibleChildren(List<Chunk> chunks)
    {
        float d = Vector3.Distance(_planet.transform.position + _position.normalized * _planet.radius, _planet.player.position);
        if (_lodLevel < _planet.detailLevel.Length && d <= _planet.detailLevel[_lodLevel])
        {
            if (_children.Length == 0)
            {
                _children = new Chunk[4];
                float halfR = _radius / 2;

                Vector3 downPart = _position + _face.axisA * halfR;
                Vector3 upPart = _position - _face.axisA * halfR;
                Vector3 leftPart = _face.axisB * halfR;
                int nHash = _hashValue << 2;

                _children[0] = new Chunk(this, _face, _planet, upPart + leftPart, halfR, _lodLevel + 1, nHash); // Top left
                _children[1] = new Chunk(this, _face, _planet, upPart - leftPart, halfR, _lodLevel + 1, nHash + 1); // Top right
                _children[2] = new Chunk(this, _face, _planet, downPart + leftPart, halfR, _lodLevel + 1, nHash + 2); // Bottom left
                _children[3] = new Chunk(this, _face, _planet, downPart - leftPart, halfR, _lodLevel + 1, nHash + 3); // Bottom Right
            }
                
            foreach (Chunk child in _children)
                child.GenerateVisibleChildren(chunks);
        }
        else
            _children = Array.Empty<Chunk>();
        
        if (_children.Length == 0)
        {
            // We apply a custom culling based on the angle formed in the center of the planet by
            // this chunk and the position of the player.
            // Then we decide to cull or not the chunk with a threshold determined by how far the player is from
            // the planet.
            // If the player is far, we want to see more of the planet so the threshold angle is big. And if it's near,
            // we want to see only a portion of the planet, so the threshold is low
            float a = _planet.radius;
            float b = Vector3.Distance(_planet.transform.position, _planet.player.position);
            
            // We calculate the threshold. To gain time, we work with cos(y) and not with y.
            // so acos(0.9) is near 10° (when the player is near the planet, we ant few detail)
            // acos(-0.2f) is near 100° (when the player is far away wa want to see more)
            float th = Mathf.Lerp(0.9f, -0.3f, Mathf.Clamp01(b/a-1));
            
            // The law of cosine.
            // cos(y) = (a² + b² - c²) / 2ab
            // Where y is the angle is the center of the planet between the position of the chunk and the player
            float cosValue = (a * a + b * b - d * d) / (2 * a * b);
            if(cosValue >= th)
                chunks.Add(this);
        }
    }

    /**
     * This function will generate the data for this chunk only if they have not been generated yet
     */
    public void Generate()
    {
        if (_vertices != null)
            return;
        
        int res = _planet.resolution;

        _vertices = new Vector3[res * res];
        _triangles = new int[(res - 1) * (res - 1) * 6];
        
        // Border data
        _borderVertices = new Vector3[4 * (res + 1)];
        _borderTriangles = new int[24 * res];
        
        int triIndex = 0;
        int borderTriIndex = 0;

        for (int y = 0; y < res + 2; y++)
        {
            for (int x = 0; x < res + 2; x++)
            {
                int vertexIndex = _planet.VertGenIndices[x, y];
                Vector2 percent = new Vector2(x - 1, y - 1) / (res - 1);
                Vector3 pointOnUnitCube = _position + ((percent.x - .5f) * 2 * _face.axisA + (percent.y - .5f) * 2 * _face.axisB) * _radius;
                Vector3 point = pointOnUnitCube.normalized * _planet.radius;
                float height = (_planet.generator.Evaluate(point) + 1) * 0.5f + 0.5f;
                // _vertices[i] = point * height;

                Vector3 vertex = point * height;
               
                // We add the vertex
                if (vertexIndex < 0)
                    _borderVertices[-vertexIndex - 1] = vertex;
                else
                    _vertices[vertexIndex] = vertex;

                if (x != res + 1 && y != res + 1) // If we are not on the last row/column
                {
                    int b = _planet.VertGenIndices[x + 1, y];
                    int c = _planet.VertGenIndices[x, y + 1];
                    int d = _planet.VertGenIndices[x + 1, y + 1];
                    
                    // Edge triangles
                    if (vertexIndex < 0 || b < 0 || c < 0 || d < 0)
                    {
                        _borderTriangles[borderTriIndex] = vertexIndex;
                        _borderTriangles[borderTriIndex + 1] = d;
                        _borderTriangles[borderTriIndex + 2] = c;
                        
                        _borderTriangles[borderTriIndex + 3] = vertexIndex;
                        _borderTriangles[borderTriIndex + 4] = b;
                        _borderTriangles[borderTriIndex + 5] = d;
                        borderTriIndex += 6;
                    }
                    else
                    {
                        _triangles[triIndex] = vertexIndex;
                        _triangles[triIndex + 1] = d;
                        _triangles[triIndex + 2] = c;
                        
                        _triangles[triIndex + 3] = vertexIndex;
                        _triangles[triIndex + 4] = b;
                        _triangles[triIndex + 5] = d;
                        triIndex += 6;
                    }
                }
            }
        }
    }
    
    /**
     * Add all the vertices and the triangles of this chunk to the mesh of the faces of the planet
     */
    public int AddToMesh(List<Vector3> meshV, List<Vector3> meshN, List<int> meshT, Dictionary<Vector3, int> vectI, int triangleOffset)
    {
        // We fix edge fans
        Vector3[] verts = (Vector3[]) _vertices.Clone();
        MergeEdges(verts);
        
        // We construct normals
        Vector3[] normals = CalculateNormals(verts);
        int added = 0;

        for (int i = 0; i < verts.Length; ++i)
        {
            Vector3 vert = verts[i];
            if (!vectI.ContainsKey(vert))
            {
                vectI.Add(vert, triangleOffset + added);
                meshV.Add(vert);
                meshN.Add(normals[i]);
                added++;
            }
        }

        foreach (int t in _triangles)
        {
            int newIndex = vectI[verts[t]];
            meshT.Add(newIndex);
        }

        return added;
    }

    public bool Equal(Chunk other) { return _hashValue == other._hashValue; }
    
    /*
     * Below functions are functions to calculate normals
     */
    private Vector3[] CalculateNormals(Vector3[] vertices)
    {
        Vector3[] normals = new Vector3[vertices.Length];
        
        int triangleCount = _triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int triangleIndex = i * 3;
            int vectorA = _triangles[triangleIndex];
            int vectorB = _triangles[triangleIndex + 1];
            int vectorC = _triangles[triangleIndex + 2];

            Vector3 triangleNormal = GetNormalFromVertices(vectorA, vectorB, vectorC, vertices);
            normals[vectorA] += triangleNormal;
            normals[vectorB] += triangleNormal;
            normals[vectorC] += triangleNormal;
        }
        
        int borderTriangleCount = _borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int triangleIndex = i * 3;
            int vectorA = _borderTriangles[triangleIndex];
            int vectorB = _borderTriangles[triangleIndex + 1];
            int vectorC = _borderTriangles[triangleIndex + 2];

            Vector3 triangleNormal = GetNormalFromVertices(vectorA, vectorB, vectorC, vertices);
            if(vectorA >= 0)
                normals[vectorA] += triangleNormal;
            
            if(vectorB >= 0)
                normals[vectorB] += triangleNormal;
            
            if(vectorC >= 0)
                normals[vectorC] += triangleNormal;
        }
        
        FixEdgesNormals(normals);

        for (int i = 0; i < normals.Length; i++)
            normals[i] = normals[i].normalized;
        
        return normals;
    }

    private void FixEdgesNormals(Vector3[] normals)
    {
        int res = _planet.resolution;
        foreach (int i in _leftmerged)
        {
            normals[i] = Vector3.Slerp(normals[i + 1], normals[i - 1], 0.5f);
        }
    }

    private Vector3 GetNormalFromVertices(int indexA, int indexB, int indexC, Vector3[] vertices)
    {
        Vector3 vectorA = indexA < 0 ? _borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 vectorB = indexB < 0 ? _borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 vectorC = indexC < 0 ? _borderVertices[-indexC - 1] : vertices[indexC];

        return Vector3.Cross(vectorB - vectorA, vectorC - vectorA);
    }

    /*
     * Below functions are functions to solve edge fans of chunks on the same faces
     */
    private void MergeEdges(Vector3[] verts)
    {
        _leftmerged = new List<int>();
        // 0:east (go to right), 1:west (go to left), 2:north (up), 3:south (down)
        bool right = (_hashValue & 1) == 1; // Odd hashValue corresponds to the right part
        bool south = (_hashValue & 2) == 2; // South parts have their two last bit = 0b_10 or 0b_11

        if (right)
        {
            if (MergeEdgesWithNeighbour(0))
                MergeRightEdges(verts);
        }
        else if(MergeEdgesWithNeighbour(1)) // Left part
            MergeLeftEdges(verts);

        if (south)
        {
            if(MergeEdgesWithNeighbour(3))
                MergeBottomEdges(verts);
        }
        else if(MergeEdgesWithNeighbour(2)) // North part
            MergeTopEdges(verts);
    }

    private void MergeBottomEdges(Vector3[] verts)
    {
        int res = _planet.resolution;
        
        for (int y = 1; y < res - 1; y+=2)
        {
            int index = y * res + res - 1;
            verts[index] = GetMid(index - res, index + res, verts);
        }
    }
    
    private void MergeTopEdges(Vector3[] verts)
    {
        int res = _planet.resolution;
        
        for (int y = 1; y < res - 1; y+=2)
        {
            int index = y * res;
            verts[index] = GetMid(index - res, index + res, verts);
        }
    }
    
    private void MergeLeftEdges(Vector3[] verts)
    {
        int res = _planet.resolution;
        int baseIndex = (res - 1) * res;
        for (int x = 1; x < _planet.resolution - 1; x+=2)
        {
            int index = baseIndex + x;
            verts[index] = GetMid(index - 1, index + 1, verts);
            _leftmerged.Add(index);
        }
    }
    
    private void MergeRightEdges(Vector3[] verts)
    {
        for (int x = 1; x < _planet.resolution - 1; x+=2)
            verts[x] = GetMid(x - 1, x + 1, verts);
    }

    private Vector3 GetMid(int indexA, int indexB, Vector3[] verts)
    {
        return (verts[indexA] + verts[indexB]) / 2;
    }
    
    // return True if we need to merge edges and false otherwise
    private bool MergeEdgesWithNeighbour(byte direction)
    {
        Chunk ch = GetNeighbor(direction);
        return ch != null && _lodLevel != ch._lodLevel;
    }
    
    // This array contains pattern for getting neighbors in each direction
    // If we want to go to direction=0, we look at the first pattern (0, 2, 1)
    /*
     * MEMO:
     * direction:
     * 0:east (go to right), 1:west (go to left), 2:north (up), 3:south (down)
     *
     * parts:
     * 0 : top left, 1 : top right, 2 : bottom left, 3 : bottom right
     */
    private readonly (int, int, int)[] _neighborPatterns = {(0, 2, 1), (1, 3, -1),(2, 3, -2), (0, 1, 2)};
    
    private Chunk GetNeighbor(int direction)
    {
        int part = _hashValue & 3;

        if (_parent == null) // We probably go to another face
            return null;

        (int part1, int part2, int modifier) = _neighborPatterns[direction];
        if (part == part1 || part == part2)
            return _parent._children[part + modifier];
        
        Chunk chunk = _parent.GetNeighbor(direction);
        if (chunk == null || chunk._children.Length == 0)
            return chunk;

        return chunk._children[part - modifier];
    }
}