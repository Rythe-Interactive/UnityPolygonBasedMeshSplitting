using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrimitiveTriangle
{
    Vector3 v1, v2, v3;
}
public class PrimitiveMesh
{
    List<SplittablePolygon> splitPolygon;
    List<PrimitiveTriangle> primitiveTriangles;

    void createNewGameObject()
    {
        GameObject newGameObject = new GameObject();
        newGameObject.name = "splitObject";

        var meshFilter = newGameObject.AddComponent<MeshFilter>();
        newGameObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        



        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = indices.ToArray();
        


    }

    void recursiveRetrieveTriangles(HalfEdgeEdge edge, List<Vector3> vertices)
    {
        Vector3 v1, v2, v3;
        
    }

    void redefineGameObject(GameObject obj)
    {

    }

}
