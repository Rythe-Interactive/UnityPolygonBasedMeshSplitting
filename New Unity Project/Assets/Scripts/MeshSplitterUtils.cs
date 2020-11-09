using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshSplitterUtils 
{
    public static readonly float splitterEpsilon = 0.01f;
    public static readonly float splitterAbovePlaneEpsilon = 0.02f;
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

    static public void GetTangents(Vector3 normal,out Vector3 tangent1,out Vector3 tangent2)
    {
        float closenessToRight = Vector3.Dot(normal, new Vector3(1, 0, 0));
        if (Mathf.Approximately(Mathf.Abs(closenessToRight),1.0f))
        {
            tangent1 = Vector3.up;
            tangent2 = Vector3.forward;
            return;
        }

        tangent1 = Vector3.Cross(normal, Vector3.right).normalized;
        tangent2 = Vector3.Cross(normal, tangent1).normalized;
    }

    static public void LineToPlaneIntersection(Vector3 start, Vector3 end, Vector3 planePosition, Vector3 planeNormal, out Vector3 intersection)
    {
        Vector3 lineToUse = start - end;

        Vector3 P0 = start;
        Vector3 P1 = lineToUse.normalized;
        Vector3 A = planePosition;

        //float t = FindLineToPlaneInterpolant(A,planeNormal, start, P1);
        float t = (Vector3.Dot(A, planeNormal) - Vector3.Dot(P0, planeNormal)) / Vector3.Dot(P1, planeNormal);

        intersection = P0 + P1 * t;


    }

    static public float FindLineToPlaneInterpolant(Vector3 planePosition,Vector3 planeNormal,Vector3 start,Vector3 startToEnd)
    {
        return (Vector3.Dot(planePosition, planeNormal) - Vector3.Dot(start, planeNormal)) / Vector3.Dot(startToEnd, planeNormal);
    }

    static public bool GetSupportPoint(Transform trans, List<HalfEdgeEdge> edges,Vector3 startPosition, Vector3 supportDirection, out Vector3 worldSupportPoint)
    {
        float furthestProjection = float.MinValue;
        worldSupportPoint = new Vector3();

        foreach(var edge in edges)
        {
            Vector3 worldEdgePosition = trans.localToWorldMatrix.MultiplyPoint(edge.GetEdgeLocalCentroid());

            Vector3 edgeToStart = worldEdgePosition - startPosition;

            float projection = Vector3.Dot(edgeToStart, supportDirection);
            
            if(projection > furthestProjection )
            {
                furthestProjection = projection;
                worldSupportPoint = worldEdgePosition;
            }


        }

        return !Mathf.Approximately(furthestProjection, float.MinValue);

    }



}
