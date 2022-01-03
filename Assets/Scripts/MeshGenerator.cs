using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve meshHeightCurve, int detail, bool useFlatShading) {
        int meshSimplificationIncrement = detail == 0 ? 1 : detail * 2;

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        AnimationCurve heightCurve = new AnimationCurve(meshHeightCurve.keys);

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, useFlatShading);
        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
                bool isBorder = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if (isBorder) {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                } else {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
                int vertexIndex = vertexIndicesMap[x, y];
                Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertex = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

                meshData.AddVertex(vertex, percent, vertexIndex);

                if (x < borderedSize - 1 && y < borderedSize - 1) {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x +meshSimplificationIncrement, y + meshSimplificationIncrement];
                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }

                vertexIndex++;
            }
        }

        meshData.Finalize();
        return meshData;
    }
    
}


public class MeshData {
    Vector3[] vertices;
    int[] triangles;
    public Vector2[] uvs;
    Vector3[] bakedNormals;

    Vector3[] borderVertices;
    int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;

    bool useFlatShading;

    public MeshData(int size, bool useFlatShading) {
        this.useFlatShading = useFlatShading;
        vertices = new Vector3[size * size];
        uvs = new Vector2[size * size];
        triangles = new int[(size - 1) * (size - 1) * 6];

        borderVertices = new Vector3[size * 4 + 4];
        borderTriangles = new int[6 * 4 * size];
    }

    public void AddVertex(Vector3 vertex, Vector2 uv, int vertexIndex) {
        if (vertexIndex < 0) {
            borderVertices[-vertexIndex - 1] = vertex;
        } else {
            vertices[vertexIndex] = vertex;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c) {
        if (a<0 || b<0 ||c<0) {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;

            borderTriangleIndex += 3;
        }
        else {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;

            triangleIndex += 3;
        } 
    }

    Vector3[] CalculateNormals() {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int trianglesCount = triangles.Length / 3;
        for (int i = 0; i < trianglesCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex +1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 normal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += normal;
            vertexNormals[vertexIndexB] += normal;
            vertexNormals[vertexIndexC] += normal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 normal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0) {
                vertexNormals[vertexIndexA] += normal;
            }

            if (vertexIndexB >= 0) {
                vertexNormals[vertexIndexB] += normal;
            }

            if (vertexIndexC >= 0) {
                vertexNormals[vertexIndexC] += normal;
            }
        }


        for (int i = 0; i < vertexNormals.Length; i++) {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int a, int b, int c) {
        Vector3 pointA = a >= 0 ? vertices[a] : borderVertices[-a-1];
        Vector3 pointB = b >= 0 ? vertices[b] : borderVertices[-b-1];
        Vector3 pointC = c >= 0 ? vertices[c] : borderVertices[-c-1];

        Vector3 ab = pointB - pointA;
        Vector3 ac = pointC - pointA;
        return Vector3.Cross(ab, ac).normalized;
    }
    
    public void Finalize() {
        if (useFlatShading) {
            FlatShading();
        } else {
            BakeNormals();
        }
    }

    void BakeNormals() {
        bakedNormals = CalculateNormals();
    }

    void FlatShading() {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++) {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUvs;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        if (useFlatShading) {
            mesh.RecalculateNormals();
        } else {
            mesh.normals = bakedNormals;
        }

        return mesh;
    }
}