using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HalfEdgeEdge
{
	static Vector3[] v = new Vector3[4];

	public Vector3 position;
	public HalfEdgeEdge nextEdge;
	public HalfEdgeEdge pairingEdge;

	public bool isVisited = false;

	public HalfEdgeEdge(Vector3 pPosition)
	{
		position = pPosition;
	}

	public void MarkTriangleVisited()
	{
		HalfEdgeEdge next, prev;
		GetTrianglesEdges(out next, out prev);

		isVisited = true;
		next.isVisited = true;
		prev.isVisited = true;
	}

	public void GetTrianglesEdges(out HalfEdgeEdge outNextEdge, out HalfEdgeEdge outPrevEdge)
	{
		outNextEdge = nextEdge;
		outPrevEdge = nextEdge.nextEdge;
	}

	public void GetTrianglePairings(out HalfEdgeEdge outSelfPair, out HalfEdgeEdge outNextPair, out HalfEdgeEdge outPrevPair)
	{
		outSelfPair = pairingEdge;
		outNextPair = nextEdge.pairingEdge;
		outPrevPair = nextEdge.nextEdge.pairingEdge;
	}

	public void LogTrianglePositions(Transform t)
	{
		Debug.Log("--*this triangle consist of the following edges");
		LogEdgePosition(t);
		nextEdge.LogEdgePosition(t);
		nextEdge.nextEdge.LogEdgePosition(t);
	}

	public void LogEdgePosition(Transform t)
	{
		Debug.Log("Edge At " + t.localToWorldMatrix.MultiplyPoint(position).ToString("F4"));
	}

	public Vector3 GetTriangleNormal(Transform t)
	{
		Vector3 worldNextEdge = t.localToWorldMatrix * nextEdge.position;
		Vector3 worldPosition= t.localToWorldMatrix * position;
		Vector3 worldPrevEdge = t.localToWorldMatrix * nextEdge.nextEdge.position;

		return Vector3.Cross(worldNextEdge - worldPosition, worldPrevEdge - worldPosition).normalized;

	}

	public bool IsPlanarWith(HalfEdgeEdge otherEdge, Transform t, bool debug = false)
	{
		Debug.Assert(otherEdge != null);
		Vector3 a = GetTriangleNormal(t);
		Vector3 b = otherEdge.GetTriangleNormal(t);

		float angle = Vector3.Angle(a, b);

		if (debug)
		{
			Debug.Log("angle found " + angle);

			Debug.Log("a " + a.ToString("F4"));
			Debug.Log("b " + b.ToString("F4"));

			Debug.Log("Comparing Edge at " + t.localToWorldMatrix.MultiplyPoint(position).ToString("F4")
				+ " --- " + t.localToWorldMatrix.MultiplyPoint(nextEdge.position).ToString("F4"));

			Debug.Log("With Edge at " + t.localToWorldMatrix.MultiplyPoint(otherEdge.position).ToString("F4")
				+ " --- " + t.localToWorldMatrix.MultiplyPoint(otherEdge.nextEdge.position).ToString("F4"));

		}







		return angle < 1.0f;

		//HalfEdgeEdge pair = otherEdge.pairingEdge;
		//Debug.Log("----- IsPlanarCheck --------");

		//v[0] = nextEdge.position;
		//v[1] = nextEdge.nextEdge.position;
		//v[2] = otherEdge.nextEdge.position;
		//v[3] = otherEdge.nextEdge.nextEdge.position;

		//Debug.Log("Vertices:");
		//Debug.Log("V0 " + v[0].ToString("F4"));
		//Debug.Log("V1 " + v[1].ToString("F4"));
		//Debug.Log("V2 " + v[2].ToString("F4"));
		//Debug.Log("V3 " + v[3].ToString("F4"));

		//Vector3 planeNormal;
		//float d;
		//MeshSplitterUtils.CreateNewellPlane(v, out planeNormal, out d);

		//float biggestViolation = float.MinValue;

		//      for (int i = 0; i < v.Length; i++)
		//      {


		//	float pointDistanceToPlane = Vector3.Dot(v[i] , planeNormal) - d;

		//	if(Mathf.Abs( pointDistanceToPlane) > 0.003f)
		//          {
		//		Debug.Log("VIOLATED pointDistanceToPlane was " + pointDistanceToPlane);

		//		if(pointDistanceToPlane > biggestViolation)
		//              {
		//			biggestViolation = pointDistanceToPlane;
		//              }

		//		return false;
		//          }
		//      }

		//Debug.Log("LARGEST pointDistanceToPlane  violation pointDistanceToPlane was " + biggestViolation);
		//return true;
	}




}
