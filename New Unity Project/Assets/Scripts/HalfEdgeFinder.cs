using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Assertions;

public enum PolygonState
{
	Unmarked,
	Above,
	Under,
	Split
}
public class SplittablePolygon
{
	//mostly used for debugging reasons
	public Vector3 localCentroid;
	private List<HalfEdgeEdge> polygonEdges;
	public PolygonState state = PolygonState.Unmarked;
	public bool visited = false;

	public SplittablePolygon() { }

	public SplittablePolygon CreateSplitPolygonCopy()
    {
		SplittablePolygon result = new SplittablePolygon();
		
		result.localCentroid = localCentroid;
		result.state = state;
		result.visited = visited;

		List<HalfEdgeEdge> newEdgeList = new List<HalfEdgeEdge>();
		recursiveCopy(null, polygonEdges[0], newEdgeList);
		result.SetEdgeList(newEdgeList);

		return result;
    }

	private void recursiveCopy(HalfEdgeEdge copyEdge,HalfEdgeEdge shadowEdge,List<HalfEdgeEdge> newEdgeList)
    {
		HalfEdgeEdge shadowEdge2, shadowEdge3;
		shadowEdge.GetTrianglesEdges(out shadowEdge2, out shadowEdge3);
		//shadowEdge.MarkTriangleVisited();


		HalfEdgeEdge newEdge1 = new HalfEdgeEdge(shadowEdge.position, shadowEdge.isBoundary);
		HalfEdgeEdge newEdge2 = new HalfEdgeEdge(shadowEdge2.position, shadowEdge2.isBoundary);
		HalfEdgeEdge newEdge3 = new HalfEdgeEdge(shadowEdge3.position, shadowEdge3.isBoundary);

		HalfEdgeEdge.ConnectIntoTriangle(newEdge1, newEdge2, newEdge3);

		newEdgeList.Add(newEdge1);
		newEdgeList.Add(newEdge2);
		newEdgeList.Add(newEdge3);

		if (copyEdge != null)
		{
			newEdge1.SetPairing(copyEdge);
		}

		shadowEdge.copyEdge = newEdge1;
		shadowEdge2.copyEdge = newEdge2;
		shadowEdge3.copyEdge = newEdge3;

	

		if (!shadowEdge.isBoundary && shadowEdge.pairingEdge != null)
		{
			if (shadowEdge.pairingEdge.copyEdge == null)
			{
				recursiveCopy(newEdge1, shadowEdge.pairingEdge, newEdgeList);
			}
			else
			{
				shadowEdge.pairingEdge.copyEdge.SetPairing(newEdge1);
			}
		}




		if (!shadowEdge2.isBoundary && shadowEdge2.pairingEdge != null)
		{
			if (shadowEdge2.pairingEdge.copyEdge == null)
			{
				recursiveCopy(newEdge2, shadowEdge2.pairingEdge, newEdgeList);
			}
			else
			{
				shadowEdge2.pairingEdge.copyEdge.SetPairing(newEdge2);
			}
		}

		if (!shadowEdge3.isBoundary && shadowEdge3.pairingEdge != null)
		{
			if (shadowEdge3.pairingEdge.copyEdge == null)
			{
				recursiveCopy(newEdge3, shadowEdge3.pairingEdge, newEdgeList);
			}
			else
			{
				shadowEdge3.pairingEdge.copyEdge.SetPairing(newEdge3);
			}
		}



	}

	public void SetEdgeList(List<HalfEdgeEdge> newList)
    {
		polygonEdges = newList.ToList();

		foreach(var edge in polygonEdges)
        {
			edge.parentPolygon = this;
        }
	}

	public  List<HalfEdgeEdge> GetEdgeList()
	{
		return polygonEdges;
	}

	public void CalculateLocalCentroid()
    {
		foreach(HalfEdgeEdge edge in polygonEdges)
        {
			localCentroid += edge.position;
        }

		localCentroid /= polygonEdges.Count;
    }

	public void resetVisited()
	{
		foreach (HalfEdgeEdge edge in polygonEdges)
		{
			edge.isVisited = false;
		}
	}

	public void FindBoundaryEdges(Transform trans)
    {
		foreach (HalfEdgeEdge edge in polygonEdges)
		{
			bool edgeIsBoundary = true;

			if(edge.pairingEdge != null)
            {
				if(edge.IsPlanarWith(edge.pairingEdge, trans))
                {
					edgeIsBoundary = false;
                }
            }

			edge.isBoundary = edgeIsBoundary;

		}

	}

	public void IsSplitByPlane(Vector3 position, Vector3 normal,Matrix4x4 world)
	{
		int vertexAbove = 0;
		int vertexBelow = 0;


		foreach (HalfEdgeEdge edge in polygonEdges)
		{
			Vector3 worldEdgePosition = world.MultiplyPoint(edge.position);

			if(MeshSplitterUtils.IsPointAbovePlane(worldEdgePosition,position,normal))
            {
				vertexAbove++;
			}
            else
            {
				vertexBelow++;
			}

			bool isSplit = vertexAbove * vertexBelow != 0;

			if(isSplit)
            {
				state = PolygonState.Split;
				return;
			}
		}

		if(vertexAbove > vertexBelow)
        {
			state = PolygonState.Above;
		}
		else if(vertexBelow > vertexAbove)
        {
			state = PolygonState.Under;
		}


	}


}

public class HalfEdgeFinder : MonoBehaviour
{
	public int edgeCheckMax = 5000;

	public List<SplittablePolygon> polygonsFound = new List<SplittablePolygon>();
	private Color[] debugColors;

    Mesh mesh;
	List<HalfEdgeEdge> edges = new List<HalfEdgeEdge>();

	HalfEdgeEdge currentEdge;

	public int edgeCheckCount = 0;

	public int polygonCount;
	// Start is called before the first frame update
	void Start()
    {
		//Debug.unityLogger.logEnabled = false;

		mesh = GetComponent<MeshFilter>().mesh;

        //Debug.Log("This mesh has " + mesh.vertices.Length + " vertices");

		FindHalfEdge();
		polygonize();

		debugColors = new Color[polygonsFound.Count];

		polygonCount = polygonsFound.Count;



		for (int i = 0; i < debugColors.Length; i++)
        {
			debugColors[i] = new Color
				(UnityEngine.Random.Range(0.1f, 0.5f), UnityEngine.Random.Range(0.1f, 0.5f), UnityEngine.Random.Range(0.1f, 0.5f));

		}

		//Debug.Log("polygonsFound " + polygonsFound.Count );
		currentEdge = edges[0];

	}


    void FindHalfEdge()
    {
		// holds the "pointer" to the unique indices inside mesh->Indices
		List<int> uniqueIndex = new List<int>();
		// stores the unique vertices of the mesh
		List<Vector3> uniquePositions = new List<Vector3>();

		int uniqueIndexCount = -1;

		for (int i = 0; i < mesh.vertices.Length; ++i)
		{
			Vector3 position = mesh.vertices[i];

			bool isVectorSeen = false;
			//have we found this vector before?
			for (int j = 0; j < uniquePositions.Count; j++)
			{
				Vector3 transformedPos = transform.localToWorldMatrix.MultiplyPoint(position);
				Vector3 uniqueTransformedPos = transform.localToWorldMatrix.MultiplyPoint(uniquePositions[j]);

				//ApproximatelyWithThreshold(Vector3.Distance(uniqueTransformedPos, transformedPos), 0.0f, 0.0001f)
				//Mathf.Approximately(Vector3.Distance(uniqueTransformedPos, transformedPos),0.0f)
				if (Mathf.Approximately(Vector3.Distance(uniqueTransformedPos, transformedPos), 0.0f))
				{
					//we have seen this vector before
					uniqueIndex.Add(j);
					isVectorSeen = true;
					break;
				}
			}

			if (!isVectorSeen)
			{
				//we have not seen this position before,add it 
				uniqueIndexCount++;
				uniqueIndex.Add(uniqueIndexCount);

				uniquePositions.Add(position);

			}
		}

		Debug.Log("There are " + uniqueIndex.Count + "  uniqueIndex");
		Debug.Log("There are " + uniquePositions.Count + " uniquePositions");



		Dictionary<Tuple<int, int>, HalfEdgeEdge> vertexIndexToHalfEdge = new Dictionary<Tuple<int, int>, HalfEdgeEdge>();

		for (int i = 0; i < mesh.triangles.Length; i += 3)
		{
			int firstVertIndex = mesh.triangles[i];
			int secondVertIndex = mesh.triangles[i+1];
			int thirdVertIndex = mesh.triangles[i + 2];

			int uniqueFirstIndex = uniqueIndex[firstVertIndex];
			int uniqueSecondIndex = uniqueIndex[secondVertIndex];
			int uniqueThirdIndex = uniqueIndex[thirdVertIndex];

			//-----------------instantiate first half edge---------------------//
			HalfEdgeEdge firstEdge = new HalfEdgeEdge(mesh.vertices[firstVertIndex]);

			edges.Add((UniqueAdd(vertexIndexToHalfEdge, Tuple.Create(uniqueFirstIndex, uniqueSecondIndex), firstEdge)));



			//-----------------instantiate second half edge---------------------//
			HalfEdgeEdge secondEdge = new HalfEdgeEdge(mesh.vertices[secondVertIndex]);
			edges.Add(secondEdge);

			edges.Add(UniqueAdd(vertexIndexToHalfEdge, Tuple.Create(uniqueSecondIndex, uniqueThirdIndex), secondEdge));

			//-----------------instantiate third half edge---------------------//
			HalfEdgeEdge thirdEdge = new HalfEdgeEdge(mesh.vertices[thirdVertIndex]);

			edges.Add(UniqueAdd(vertexIndexToHalfEdge, Tuple.Create(uniqueThirdIndex, uniqueFirstIndex), thirdEdge));



			//-------------------- link half edges to each other
			firstEdge.nextEdge = secondEdge;
			secondEdge.nextEdge = thirdEdge;
			thirdEdge.nextEdge = firstEdge;

		}

		foreach (var indexEdgePair in vertexIndexToHalfEdge)
		{

			int u = indexEdgePair.Key.Item1;
			int v = indexEdgePair.Key.Item2;

			//for a Halfedge paired with vertices with an index of (u,v),
			//its pair would be a HalfEdge paired with vertices with an index of (v,u)
			var pairEdgeKey = Tuple.Create(v, u);

			if(!vertexIndexToHalfEdge.ContainsKey(pairEdgeKey))
            {
				continue;
            }

			HalfEdgeEdge otherEdge = vertexIndexToHalfEdge[pairEdgeKey];
			HalfEdgeEdge edge = indexEdgePair.Value;

			otherEdge.pairingEdge = edge;
			edge.pairingEdge = otherEdge;



		}







	}

	private HalfEdgeEdge UniqueAdd(Dictionary<Tuple<int, int>, HalfEdgeEdge> vertexIndexToHalfEdge, Tuple<int, int> key,HalfEdgeEdge value)
    {
		if(vertexIndexToHalfEdge.ContainsKey(key))
        {
			return vertexIndexToHalfEdge[key];
		}

		vertexIndexToHalfEdge.Add(key, value);
		return value;
    }

	private void polygonize()
    {
		bool foundUnvisitedEdge = true;
		int i = 0;

		while(foundUnvisitedEdge)
        {
			HalfEdgeEdge initialEdge;
			foundUnvisitedEdge = findUnvisitedEdge(out initialEdge);

			if(foundUnvisitedEdge)
            {
				Queue<HalfEdgeEdge> nonPlanarQueue = new Queue<HalfEdgeEdge>();
				List<HalfEdgeEdge> planarList = new List<HalfEdgeEdge>();

				nonPlanarQueue.Enqueue(initialEdge);

				while (nonPlanarQueue.Count != 0)
                {
					//Debug.Log(" nonPlanarQueue.Count " + nonPlanarQueue.Count);

					HalfEdgeEdge firstEdge = nonPlanarQueue.Dequeue();

					if(firstEdge.isVisited) { continue; }

					planarList.Add(firstEdge);
					planarList.Add(firstEdge.nextEdge);
					planarList.Add(firstEdge.nextEdge.nextEdge);

					int oldEdgeCheckCount = edgeCheckCount;

					//firstEdge.LogTrianglePositions(transform);

					recursiveFindPlanarTriangles(firstEdge, 
						nonPlanarQueue, planarList, firstEdge);

					SplittablePolygon newPolygon = new SplittablePolygon();

					newPolygon.SetEdgeList(planarList);
					
					newPolygon.FindBoundaryEdges(transform);
					newPolygon.CalculateLocalCentroid();
					polygonsFound.Add(newPolygon);

					planarList.Clear();
					//return;
					i++; 
					//if(i > 2) { return; }

				}

				
			}					


		}


		foreach(var polygon in polygonsFound)
        {
			polygon.resetVisited();
        }
			
    }

	private void recursiveFindPlanarTriangles(HalfEdgeEdge initialEdge, Queue<HalfEdgeEdge> nonPlanarQueue, List<HalfEdgeEdge> planarList,HalfEdgeEdge comparisionEdge)
	{


		if (initialEdge.isVisited) { return; }

		//Debug.Log("This node has not been visited before");

		initialEdge.MarkTriangleVisited();
		

		HalfEdgeEdge pair1, pair2, pair3;
		initialEdge.GetTrianglePairings(out pair1, out pair2, out pair3);

		//Debug.Assert(pair1 != pair2);

		//Debug.Log("pair1 Check");
		CheckEdgePlanarity( pair1, nonPlanarQueue, planarList, comparisionEdge);
		//Debug.Log("pair2 Check");
		CheckEdgePlanarity( pair2, nonPlanarQueue, planarList, comparisionEdge);
		//Debug.Log("pair2 Check");
		CheckEdgePlanarity( pair3, nonPlanarQueue, planarList, comparisionEdge);

	
	}

	private void CheckEdgePlanarity(HalfEdgeEdge newTriangleEdge, Queue<HalfEdgeEdge> nonPlanarQueue, List<HalfEdgeEdge> planarList, HalfEdgeEdge comparisionEdge)
    {
		if ( newTriangleEdge == null ) { return; }

		if (newTriangleEdge.isVisited) { return; }

		edgeCheckCount++;

		if(edgeCheckCount > edgeCheckMax) 
		{
			///Debug.LogError("edgeCheckCount > 2000");
			return; 
		}

		if (comparisionEdge.IsPlanarWith(newTriangleEdge, transform))
		{
			newTriangleEdge.MarkTriangleVisited();

			HalfEdgeEdge currentNextEdge = newTriangleEdge.nextEdge;
			HalfEdgeEdge currentPrevEdge = newTriangleEdge.nextEdge.nextEdge;

			planarList.Add(newTriangleEdge);
			planarList.Add(currentNextEdge);
			planarList.Add(currentPrevEdge);

			Debug.Assert(currentNextEdge != currentPrevEdge);

			CheckEdgePlanarity(currentNextEdge.pairingEdge, nonPlanarQueue, planarList, comparisionEdge);
			CheckEdgePlanarity(currentPrevEdge.pairingEdge, nonPlanarQueue, planarList, comparisionEdge);
		}
		else
		{
			//add to non planar list
			nonPlanarQueue.Enqueue(newTriangleEdge);

		}


	}

	private bool findUnvisitedEdge(out HalfEdgeEdge outEdge)
    {
		foreach(HalfEdgeEdge edge in edges)
        {
			if(!edge.isVisited)
            {
				outEdge = edge;
				return true;
            }
        }

		outEdge = null;
		return false;
    }

	private bool ApproximatelyWithThreshold(float val,float compVal,float threshold)
    {
		return Mathf.Abs(compVal - val) <= threshold;
    }

    private void OnDrawGizmos()
    {
		if(polygonsFound != null)
        {
			//Debug.Log("DRAW POLYGON");

			for (int i = 0; i < polygonsFound.Count; i++)
            {
				
				Gizmos.color = debugColors[i];
				Vector3 end = transform.localToWorldMatrix.MultiplyPoint(polygonsFound[i].localCentroid );

				//end += new Vector3(UnityEngine.Random.Range(0, 0.01f), UnityEngine.Random.Range(0, 0.01f), 0);
				Gizmos.DrawSphere(end,0.01f);

				//Debug.Log("This polygon has " + polygonsFound[i].polygonEdges.Count + " edges");
				foreach (HalfEdgeEdge edge in polygonsFound[i].GetEdgeList())
                {
					if(edge.isBoundary)
                    {
						Vector3 start = transform.localToWorldMatrix.MultiplyPoint(edge.position + (polygonsFound[i].localCentroid - edge.position) * 0.01f);
						Vector3 nextEdge = transform.localToWorldMatrix.MultiplyPoint(edge.nextEdge.position + (polygonsFound[i].localCentroid - edge.nextEdge.position) * 0.01f);

						//if(edge.pairingEdge != null)
      //                  {
						//	Gizmos.DrawSphere(transform.localToWorldMatrix.MultiplyPoint(edge.GetEdgeLocalCentroid()), 0.01f);
						//}

						Gizmos.DrawLine(start, nextEdge);
					}
				}					
            }
        }


		//if(currentEdge != null)
  //      {
		//	Matrix4x4 worldMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

		//	Vector3 worldEdgePosition = worldMatrix.MultiplyPoint3x4( currentEdge.position);

		//	Vector3 worldNextEdgePosition = worldMatrix.MultiplyPoint3x4(currentEdge.nextEdge.position);

		//	Gizmos.DrawLine(worldEdgePosition, worldNextEdgePosition);

		//	Gizmos.color = Color.red;
		//	Gizmos.DrawSphere(worldEdgePosition, 0.02f);
		//	Gizmos.DrawCube(worldNextEdgePosition, new Vector3(0.02f, 0.02f, 0.02f));
		//}



	}

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
			currentEdge = currentEdge.nextEdge;
        }

		if (Input.GetKeyDown(KeyCode.D))
		{
			currentEdge = currentEdge.pairingEdge;
		}
	}


}
