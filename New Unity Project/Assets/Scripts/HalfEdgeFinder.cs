using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine;


public class HalfEdgeEdge
{
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

	public void GetTrianglesEdges(out HalfEdgeEdge outNextEdge,out HalfEdgeEdge outPrevEdge)
    {
		outNextEdge = nextEdge;
		outPrevEdge = nextEdge.nextEdge;
    }

	public void GetTrianglePairings(out HalfEdgeEdge outSelfPair, out HalfEdgeEdge outNextPair,out HalfEdgeEdge outPrevPair)
    {
		outSelfPair = pairingEdge;
		outNextPair = nextEdge.pairingEdge;
		outPrevPair = nextEdge.nextEdge.pairingEdge;
    }

	public Vector3 GetTriangleNormal()
    {
		return new Vector3();

    }

	public bool IsPlanarWith(HalfEdgeEdge edge)
    {
		return false;
    }


}

public class SplittablePolygon
{
	//mostly used for debugging reasons
	public Vector3 localCentroid;
	public List<HalfEdgeEdge> polygonEdges;

	public void CalculateLocalCentroid()
    {
		foreach(HalfEdgeEdge edge in polygonEdges)
        {
			localCentroid += edge.position;
        }

		localCentroid /= polygonEdges.Count;
    }


}

public class HalfEdgeFinder : MonoBehaviour
{
	List<SplittablePolygon> polygonsFound = new List<SplittablePolygon>();
	public Color[] debugColors;

    Mesh mesh;
	List<HalfEdgeEdge> edges = new List<HalfEdgeEdge>();

	HalfEdgeEdge currentEdge;


    // Start is called before the first frame update
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        Debug.Log("This mesh has " + mesh.vertices.Length + " vertices");

		FindHalfEdge();
		polygonize();

		debugColors = new Color[polygonsFound.Count];

        for (int i = 0; i < debugColors.Length; i++)
        {
			debugColors[i] = new Color
				(UnityEngine.Random.Range(0, 1.0f), UnityEngine.Random.Range(0, 1.0f), UnityEngine.Random.Range(0, 1.0f));

		}

		Debug.Log("polygonsFound " + polygonsFound.Count );
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
				if (Mathf.Approximately(Vector3.Distance(uniqueTransformedPos, transformedPos), 0.0f)

					)
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
					Debug.Log(" nonPlanarQueue.Count " + nonPlanarQueue.Count);

					HalfEdgeEdge firstEdge = nonPlanarQueue.Dequeue();

					if(firstEdge.isVisited) { continue; }

					planarList.Add(firstEdge);
					planarList.Add(firstEdge.nextEdge);
					planarList.Add(firstEdge.nextEdge.nextEdge);

					recursiveFindPlanarTriangles(firstEdge, 
						nonPlanarQueue, planarList);

					SplittablePolygon newPolygon = new SplittablePolygon();
					newPolygon.polygonEdges = planarList.ToList();


					newPolygon.CalculateLocalCentroid();
					polygonsFound.Add(newPolygon);

					Debug.Log(" Edges in Polygon: " + newPolygon.polygonEdges.Count);

					foreach(HalfEdgeEdge edge in planarList)
                    {
						Debug.Log(" -> " + edge.position);
					}

					planarList.Clear();

				}

				
			}					


		}

			
    }

	private void recursiveFindPlanarTriangles(HalfEdgeEdge initialEdge, Queue<HalfEdgeEdge> nonPlanarQueue, List<HalfEdgeEdge> planarList)
	{
		Debug.Log("---------------start recursiveFindPlanarTriangles");

		if (initialEdge.isVisited) { return; }

		Debug.Log("This node has not been visited before");

		initialEdge.MarkTriangleVisited();


		HalfEdgeEdge pair1, pair2, pair3;
		initialEdge.GetTrianglePairings(out pair1, out pair2, out pair3);

		CheckEdgePlanarity(initialEdge, pair1, nonPlanarQueue, planarList);

		CheckEdgePlanarity(initialEdge, pair2, nonPlanarQueue, planarList);

		CheckEdgePlanarity(initialEdge, pair3, nonPlanarQueue, planarList);
	}

	private void CheckEdgePlanarity(HalfEdgeEdge initialEdge,HalfEdgeEdge pair, Queue<HalfEdgeEdge> nonPlanarQueue, List<HalfEdgeEdge> planarList)
    {
		Debug.Log("***CheckEdgePlanarity");
		if (pair != null)
		{
			if (initialEdge.IsPlanarWith(pair))
			{
				Debug.Log("---- This edge IS planar");
				//add to list 
				planarList.Add(pair);
				planarList.Add(pair.nextEdge);
				planarList.Add(pair.nextEdge.nextEdge);
				recursiveFindPlanarTriangles(pair, nonPlanarQueue, planarList);
			}
			else
			{
				Debug.Log("---- This edge is NOT planar");
				//add to non planar list
				nonPlanarQueue.Enqueue(pair);

			}
		}
        else
        {
			Debug.Log("---- This edge was found to be null");
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
				Vector3 end = transform.localToWorldMatrix.MultiplyPoint(polygonsFound[i].localCentroid);

				//Debug.Log("This polygon has " + polygonsFound[i].polygonEdges.Count + " edges");
				foreach (HalfEdgeEdge edge in polygonsFound[i].polygonEdges)
                {
					Vector3 start = transform.localToWorldMatrix.MultiplyPoint( edge.position);
					//Debug.Log("start draw " + start.ToString("F2"));
					//Debug.Log("end draw " + end.ToString("F2"));
					Gizmos.DrawLine(start, end);
				}					
            }
        }


		if(currentEdge != null)
        {
			Matrix4x4 worldMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

			Vector3 worldEdgePosition = worldMatrix.MultiplyPoint3x4( currentEdge.position);

			Vector3 worldNextEdgePosition = worldMatrix.MultiplyPoint3x4(currentEdge.nextEdge.position);

			Gizmos.DrawLine(worldEdgePosition, worldNextEdgePosition);

			Gizmos.color = Color.red;
			Gizmos.DrawSphere(worldEdgePosition, 0.02f);
			Gizmos.DrawCube(worldNextEdgePosition, new Vector3(0.02f, 0.02f, 0.02f));
		}



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
