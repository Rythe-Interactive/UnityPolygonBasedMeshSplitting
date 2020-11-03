using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HalfEdgeFinder))]
public class PolygonMeshSplitter : MonoBehaviour
{
    HalfEdgeFinder edgeFinder;

    public GameObject splitObject;

    private void Start()
    {
        edgeFinder = GetComponent<HalfEdgeFinder>();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Cut applied");
            CutAt(splitObject.gameObject.transform.position, splitObject.gameObject.transform.up);
        }
    }


    void CutAt(Vector3 cutPlanePosition,Vector3 cutPlaneNormal)
    {
        DebugDrawer.drawObject.Clear();
        //Find the split state of the each polygon 

        List<SplittablePolygon> polygons = edgeFinder.polygonsFound;

        foreach(SplittablePolygon polygon in polygons)
        {
            polygon.IsSplitByPlane(cutPlanePosition, cutPlaneNormal, transform.localToWorldMatrix);
        }


        //find first non intersecting polygon
        SplittablePolygon initialSplit;
        bool foundUnvisitedPolygon = FindFirstUnvisitedPolygonWithExcluded(polygons,out initialSplit,PolygonState.Split);

        while(foundUnvisitedPolygon)
        {
            PolygonState splitState = initialSplit.state;

            List<SplittablePolygon> unSplitPgn = new List<SplittablePolygon>();
            List<SplittablePolygon> splitPgn = new List<SplittablePolygon>();

            recursiveDetectPolygonIsland(initialSplit, splitState,
            splitPgn, unSplitPgn);

            Color randColor = new Color(Random.Range(0.0f, 0.5f), Random.Range(0.0f, 0.5f), Random.Range(0.0f, 0.5f));

            //-------------- DEBUG STUFF ------------------------//
            foreach(SplittablePolygon polygon in unSplitPgn)
            {
                DebugDrawer.DrawSphere(
                    transform.localToWorldMatrix.MultiplyPoint(polygon.localCentroid), 0.1f, randColor);
            }

            foreach (SplittablePolygon polygon in splitPgn)
            {
                polygon.visited = false;
                float mult = 1;

                if(splitState == PolygonState.Under)
                {
                    mult = -1;
                }

                DebugDrawer.DrawSphere(
                    transform.localToWorldMatrix.MultiplyPoint(polygon.localCentroid) + cutPlaneNormal * 0.05f * mult, 0.1f, randColor); 
            }

            //do island detection on splitPgn





            foundUnvisitedPolygon = FindFirstUnvisitedPolygonWithExcluded(polygons, out initialSplit, PolygonState.Split);

        }

        //get unvisited polygon

            //recursively go through all polygons until intersection

            //create new SplitMesh

            //find new unvisited Polygon


            //for each SplitMesh
            //For each intersecting Polygon in SplitMesh
            //get split boundaries 
            //replace split boundaries with new split boundaries

            //Find Face Islands in mesh

            //For each island, find boundaries

            //regenerate edges with boundaries


            //for each SplitMesh
            //turn each splitMesh into a real mesh









    }

    bool FindFirstUnvisitedPolygonWithExcluded(List<SplittablePolygon> polygons,out SplittablePolygon unvisitedPolygon,PolygonState excludedState)
    {
        unvisitedPolygon = null;

        foreach(SplittablePolygon polygon in polygons)
        {
            if(!polygon.visited && polygon.state != excludedState)
            {
                unvisitedPolygon = polygon;
                return true;
                break;
               

            }

        }

        return false;
    }

    void recursiveDetectPolygonIsland(SplittablePolygon polygon,PolygonState requestedState,
        List<SplittablePolygon> splitPgn, List<SplittablePolygon> notSplitPgn)
    {
        if(polygon.visited) { return; }

        bool isPolygonReqestedState = polygon.state == requestedState;
        bool isPolygonAtIntersection = polygon.state == PolygonState.Split;
        


        if (isPolygonReqestedState || isPolygonAtIntersection)
        {
            polygon.visited = true;

            if (isPolygonReqestedState)
            {
                notSplitPgn.Add(polygon);
            }
            else if(isPolygonAtIntersection)
            {
                splitPgn.Add(polygon);
            }

            foreach(HalfEdgeEdge edge in polygon.GetEdgeList())
            {
                if(edge.pairingEdge == null) { continue; }
                
                if(edge.isBoundary)
                {
                    recursiveDetectPolygonIsland(edge.pairingEdge.parentPolygon, requestedState,
                    splitPgn, notSplitPgn);
                }


            }


        }
        
    }



}
