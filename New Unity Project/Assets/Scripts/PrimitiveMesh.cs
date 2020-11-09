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

    public PrimitiveMesh(List<SplittablePolygon> splitPolygon, 
        List<PrimitiveTriangle> primitiveTriangles,bool addRigidbody = false)
    {
        this.splitPolygon = splitPolygon;
        this.primitiveTriangles = primitiveTriangles;
    }

    public void createNewGameObject(GameObject original)
    {
        GameObject newGameObject = new GameObject();


        newGameObject.name = "splitObject";

        //copy transformation
        newGameObject.transform.localScale = original.transform.localScale;
        newGameObject.transform.rotation = original.transform.rotation;
        //newGameObject.transform.position= original.transform.position;

        var meshFilter = newGameObject.AddComponent<MeshFilter>();
        var renderer = newGameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = original.GetComponent<MeshRenderer>().sharedMaterial;

        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        foreach(var polygon in splitPolygon)
        {
            recursiveAddVertex(polygon.GetEdgeList()[0],vertices);

            polygon.resetVisited();
        }

        int indicesCount = vertices.Count;

        for (int i = 0; i < indicesCount; i++)
        {
            indices.Add(i);
        }

        for (int i = 0; i < indicesCount; i++)
        {
            uvs.Add(new Vector2(0,0));
        }

        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = indices.ToArray();

        mesh.RecalculateTangents();
        mesh.RecalculateNormals();
      

        Debug.Log("  mesh.vertices " + mesh.vertices.Length);
        Debug.Log("  mesh.triangles " + mesh.triangles.Length);

    }

    void recursiveAddVertex(HalfEdgeEdge edge, List<Vector3> vertices)
    {
        HalfEdgeEdge outNextEdge, outPrevEdge;
        edge.GetTrianglesEdges(out outNextEdge, out outPrevEdge);

        vertices.Add(edge.position);
        vertices.Add(outNextEdge.position);
        vertices.Add(outPrevEdge.position);

        edge.MarkTriangleVisited();

        if(edge.pairingEdge != null)
        {
            if (!edge.isBoundary && !edge.pairingEdge.isVisited)
            {
                recursiveAddVertex(edge.pairingEdge, vertices);
            }
        }

        if (outNextEdge.pairingEdge != null)
        {
            if (!outNextEdge.isBoundary && !outNextEdge.pairingEdge.isVisited)
            {
                recursiveAddVertex(outNextEdge.pairingEdge, vertices);
            }
        }

        if (outPrevEdge.pairingEdge != null)
        {
            if (!outPrevEdge.isBoundary && !outPrevEdge.pairingEdge.isVisited)
            {
                recursiveAddVertex(outPrevEdge.pairingEdge, vertices);
            }
        }
           



    }

    void recursiveRetrieveTriangles(HalfEdgeEdge edge, List<Vector3> vertices)
    {
        Vector3 v1, v2, v3;
        
    }

    void redefineGameObject(GameObject obj)
    {

    }

}
