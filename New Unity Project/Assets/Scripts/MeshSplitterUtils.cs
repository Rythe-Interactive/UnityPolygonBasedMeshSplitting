using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshSplitterUtils 
{
    public static readonly float splitterEpsilon = 0.005f;
    public static void CreateNewellPlane(Vector3[] v,out Vector3 normal,out float d)
    {
        Vector3 centroid = new Vector3();
        normal = new Vector3();

        for (int i = v.Length -1,j = 0; j < v.Length; i = j,j++)
        {
            normal.x += (v[i].y - v[j].y) * (v[i].z + v[j].z); // projection on yz
            normal.y += (v[i].z - v[j].z) * (v[i].x + v[j].x); // projection on xz
            normal.z += (v[i].x - v[j].x) * (v[i].y + v[j].y); // projection on xy
            centroid += v[j];
        }
        normal.Normalize();
        d = Vector3.Dot(centroid, normal) / v.Length; ;

        
    }

    static public bool IsPointAbovePlane(Vector3 pointPosition, Vector3 planePosition, Vector3 normal)
    {
        return PointDistanceToPlane(pointPosition , planePosition, normal) > 0;
    }

    static public float PointDistanceToPlane(Vector3 pointPosition, Vector3 planePosition, Vector3 normal)
    {
        return Vector3.Dot(pointPosition - planePosition, normal) ;
    }




}
