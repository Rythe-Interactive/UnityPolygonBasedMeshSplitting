using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using UnityEngine;

class EdgeComparer : IComparer<HalfEdgeEdge>
{
    Vector3 worldSupportPoint;
    Vector3 polygonWorldCentroid;
    Transform trans;
    public EdgeComparer(Transform trans,Vector3 worldSupportPoint,Vector3 polygonWorldCentroid)
    {
        this.trans = trans;
        this.worldSupportPoint = worldSupportPoint;
        this.polygonWorldCentroid = polygonWorldCentroid;
    }

    public int Compare(HalfEdgeEdge x, HalfEdgeEdge y)
    {
        
        Vector3 worldEdgeCentroidX = trans.localToWorldMatrix.MultiplyPoint(x.GetEdgeLocalCentroid());
        Vector3 worldEdgeCentroidY = trans.localToWorldMatrix.MultiplyPoint(y.GetEdgeLocalCentroid());

        float projectionX = Vector3.Dot(worldSupportPoint, (worldEdgeCentroidX - polygonWorldCentroid).normalized);
        float projectionY = Vector3.Dot(worldSupportPoint, (worldEdgeCentroidY - polygonWorldCentroid).normalized);


        var splitter = trans.GetComponent<PolygonMeshSplitter>();
        if (PolygonMeshSplitter.categorizeCount == splitter.categorizeCountDebugAt && PolygonMeshSplitter.cutCount == splitter.cutCountDebugAt)
        {
            //Debug.Log("COMPARE");

            //Debug.Log("worldEdgeCentroidX  " + worldEdgeCentroidX.ToString("F2"));
            //Debug.Log("worldEdgeCentroidY  " + worldEdgeCentroidY.ToString("F2"));

            //Debug.Log("projectionX  " + projectionX);
            //Debug.Log("projectionY  " + projectionY);
        }

        return projectionX.CompareTo(projectionY);

    }
}


[RequireComponent(typeof(HalfEdgeFinder))]
public class PolygonMeshSplitter : MonoBehaviour
{
    HalfEdgeFinder edgeFinder;

    public GameObject splitObject;
    //public GameObject debugPlane;

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

    List<SplittablePolygon> CopySplitPolygons(List<SplittablePolygon> splitPolygons, PolygonState connectState)
    {
        List<SplittablePolygon> tempList = new List<SplittablePolygon>();
        tempList.AddRange(splitPolygons);

        splitPolygons.Clear();

        //add copy to list
        foreach (SplittablePolygon pol in tempList)
        {
            SplittablePolygon copyPolygon = pol.CreateSplitPolygonCopy();

            splitPolygons.Add(copyPolygon);
        }

        //connect copy's
        foreach (SplittablePolygon pol in tempList)
        {
            foreach (HalfEdgeEdge edge in pol.GetEdgeList())
            {
                if (edge.isBoundary && edge.pairingEdge != null)
                {
                    var copy = edge.copyEdge;

                    if (edge.pairingEdge.parentPolygon.state == PolygonState.Split)
                    {
                        var pairingCopy = edge.pairingEdge.copyEdge;
                        if (pairingCopy != null)
                        {
                            copy.SetPairing(pairingCopy);
                        }


                    }
                    else if (edge.pairingEdge.parentPolygon.state == connectState)
                    {
                        copy.SetPairing(edge.pairingEdge);
                    }




                }
            }

        }

        foreach (SplittablePolygon pol in tempList)
        {
            pol.resetVisited();
            foreach (HalfEdgeEdge edge in pol.GetEdgeList())
            {
                edge.copyEdge = null;
            }
        }

        return tempList;


    }

    void recursiveCopy()
    {
        //copy polygon and get split edges

        //for each split edge
            //copy polygon

    }

    public static int cutCount = 0;
    public  static int categorizeCount = 0;

    public int cutCountDebugAt = 1;
    public int categorizeCountDebugAt = 0;

     int addCount = 0;
    public int reqestAdd = 0;

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
            categorizeCount = 0;
            Debug.Log("--------------- Split ------------------");

            PolygonState splitState = initialSplit.state;
            Debug.Log("splitState " + splitState);

            List<SplittablePolygon> unSplitPgn = new List<SplittablePolygon>();
            List<SplittablePolygon> splitPgn = new List<SplittablePolygon>();

            recursiveDetectPolygonIsland(initialSplit, splitState,
            splitPgn, unSplitPgn);

            Color randColor = new Color(Random.Range(0.0f, 0.5f), Random.Range(0.0f, 0.5f), Random.Range(0.0f, 0.5f));

            //-------------- DEBUG STUFF ------------------------//
            //foreach(SplittablePolygon polygon in unSplitPgn)
            //{
            //    DebugDrawer.DrawSphere(
            //        transform.localToWorldMatrix.MultiplyPoint(polygon.localCentroid), 0.1f, randColor);
            //}

            foreach (SplittablePolygon polygon in splitPgn)
            {
                polygon.visited = false;
                float mult = 1;

                if(splitState == PolygonState.Under)
                {
                    mult = -1;
                }

                //DebugDrawer.DrawSphere(
                //    transform.localToWorldMatrix.MultiplyPoint(polygon.localCentroid) + cutPlaneNormal * 0.05f * mult, 0.1f, Color.red);
            }

            //SplittablePolygon originalSplit = CopySplitPolygons(splitPgn, splitState);

            List<List<SplittablePolygon>> slicingIslandList = new List<List<SplittablePolygon>>();
            //do island detection on splitPgn
            List<List<SplittablePolygon>> originalSlicingIsland = new List<List<SplittablePolygon>>();
         
            DetectSplitIslands(slicingIslandList, splitPgn);

            //--------------------Slice intersecting polygons ---------------------------------------------//
            foreach (List<SplittablePolygon> polygonIsland in slicingIslandList)
            {
                List<SplittablePolygon> originalIsland = CopySplitPolygons(polygonIsland, splitState);

                foreach(SplittablePolygon pgn in polygonIsland)
                {
                    pgn.visited = false;
                    categorizeEdges(pgn, splitState, cutPlanePosition, cutPlaneNormal);
                }

                originalSlicingIsland.Add(originalIsland);
            }

            foreach (List<SplittablePolygon> polygonIsland in originalSlicingIsland)
            {
                //Debug.Log("polygonIsland count " + polygonIsland.Count);
                foreach (SplittablePolygon pgn in polygonIsland)
                {
                    pgn.visited = false;
                    pgn.resetVisited();
                }
            }


            bool requestReached = false;

                //Put split polygons in a single list
            foreach (var polygonList in slicingIslandList)
            {
                foreach(var pol in polygonList)
                {
                    pol.resetVisited();
                }

                unSplitPgn.AddRange(polygonList);
            }

            {
                PrimitiveMesh splitMesh = new PrimitiveMesh(unSplitPgn, new List<PrimitiveTriangle>());
                splitMesh.createNewGameObject(gameObject);
            }

            if(requestReached) { return; }

            //break;
            cutCount++;

            if (splitState == PolygonState.Above)
            {
                splitState = PolygonState.Under;
            }
            else if (splitState == PolygonState.Under)
            {
                splitState = PolygonState.Above;
            }

            if(cutCount == 1)
            {
                Debug.Log("NEXT 2");
            }

            foreach (List<SplittablePolygon> polygonIsland in originalSlicingIsland)
            {
                //break;
                Debug.Log("Slicing islands");
                cutCount++;
                categorizeCount = 0;
                List<SplittablePolygon> otherUnSplitPgn = new List<SplittablePolygon>();
                List<SplittablePolygon> otherSplitPgn = new List<SplittablePolygon>();

    

                recursiveDetectPolygonIsland(polygonIsland[0], splitState, otherSplitPgn, otherUnSplitPgn);

                Debug.Log("Cutcount " + cutCount);
                foreach (var pgn in otherUnSplitPgn)
                {
                    Vector3 centroid = transform.localToWorldMatrix.MultiplyPoint(pgn.localCentroid);
                    DebugDrawer.DrawSphere(centroid, 0.1f, Color.black);
                }

                CopySplitPolygons(otherSplitPgn, splitState);

                foreach (var pgn in otherSplitPgn)
                {
                    pgn.visited = false;
                    categorizeEdges(pgn, splitState, cutPlanePosition, cutPlaneNormal);
                }

                foreach (var pgn in otherSplitPgn)
                {
                    pgn.visited = false;
                    pgn.resetVisited();
                }


                otherUnSplitPgn.AddRange(otherSplitPgn);
                PrimitiveMesh newIslandMesh = new PrimitiveMesh(otherUnSplitPgn, new List<PrimitiveTriangle>());

                newIslandMesh.createNewGameObject(gameObject);

                foreach (var pgn in otherUnSplitPgn)
                {
                    pgn.visited = true;
                }
                
            }

           


            // recursiveDetectPolygonIsland(originalSplit, splitState,
            //otherSplitPgn, otherUnSplitPgn);

            // foreach(var pol in otherUnSplitPgn)
            // {
            //     Vector3 worldUnSplit = transform.localToWorldMatrix.MultiplyPoint(pol.localCentroid);
            //     pol.visited = false;
            //     pol.resetVisited();
            //     DebugDrawer.DrawSphere(worldUnSplit, 0.1f, Color.cyan);
            // }

            // foreach (var pol in otherSplitPgn)
            // {
            //     Vector3 worldSplit = transform.localToWorldMatrix.MultiplyPoint(pol.localCentroid);
            //     pol.visited = false;
            //     pol.resetVisited();
            //     DebugDrawer.DrawSphere(worldSplit, 0.1f, Color.green);
            // }


            foundUnvisitedPolygon = FindFirstUnvisitedPolygonWithExcluded(polygons, out initialSplit, PolygonState.Split);
        }

        Debug.Log("Final Cut count " + cutCount);

        //Destroy(gameObject);

    }



    void categorizeEdges(SplittablePolygon pgn, PolygonState splitState, Vector3 planePosition, Vector3 planeNormal)
    {
        if(pgn.GetEdgeList().Count == 0) { return; }

        //if(cutCount == 3)
        //{
        //    Debug.Log("Test 3");
        //    Vector3 centroid = transform.localToWorldMatrix.MultiplyPoint(pgn.localCentroid);
        //    DebugDrawer.DrawSphere(centroid, 0.1f, Color.blue);
        //}
        //Debug.Log("categorizeEdges");
        pgn.resetVisited();

        List<HalfEdgeEdge> effectedEdges = new List<HalfEdgeEdge>();
        List<HalfEdgeEdge> nonEffectedEdges = new List<HalfEdgeEdge>();

        bool shouldBeAbove = splitState == PolygonState.Above;

        //recursive check each triangle if its effected by the split
        recrusiveCheckTriangleSplitState(pgn.GetEdgeList()[0], planePosition, planeNormal,
            effectedEdges, nonEffectedEdges, shouldBeAbove);




        foreach (var edge in nonEffectedEdges)
        {
            edge.isVisited = false;
        }

        List<HalfEdgeEdge> effectedBoundaryEdges = new List<HalfEdgeEdge>();
        List<HalfEdgeEdge> splitEffectedBoundaryEdges = new List<HalfEdgeEdge>();


        if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
        {
            Debug.Log(" effectedEdges " + effectedEdges.Count);
            Debug.Log("------------------foreach (var edge in effectedEdges");
            Debug.Log("splitState " + splitState);

        }

        foreach (var edge in effectedEdges)
        {
            bool isVisitedBoundary = edge.isBoundary && edge.isVisited;
            bool isVisitedNonBoundary = !edge.isBoundary && !edge.pairingEdge.isVisited;
            bool edgeAtCorrectSpot = shouldBeAbove ?
                edge.isEdgePartlyAbovePlane(transform, planePosition, planeNormal)
                : edge.isEdgePartlyBelowPlane(transform, planePosition, planeNormal);

            if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
            {
                //Debug.Log("polygon same ? " + (edge.pairingEdge.parentPolygon == edge.parentPolygon));

                //Debug.Log("isVisitedBoundary? " + isVisitedBoundary
                //    + ",isVisitedNonBoundary? " + isVisitedNonBoundary
                //    + ",edgeAtCorrectSpot" + edgeAtCorrectSpot);

                //Debug.Log("Result " + ((isVisitedBoundary || isVisitedNonBoundary) && edgeAtCorrectSpot));
            }


            if ((isVisitedBoundary || isVisitedNonBoundary) && edgeAtCorrectSpot)
            {
                bool isSplit = edge.isSplitByPlaneNonRobust(transform, planePosition, planeNormal);
                bool isOnPlane = edge.isVertexOnPlane(transform, planePosition, planeNormal);

                if (isSplit || isOnPlane)
                {
                    splitEffectedBoundaryEdges.Add(edge);

                    //Debug.Log("splitEffectedBoundaryEdges " + transform.localToWorldMatrix.MultiplyPoint(edge.position));
                }
                else
                {
                    effectedBoundaryEdges.Add(edge);

                    //Debug.Log("effectedBoundaryEdges " + transform.localToWorldMatrix.MultiplyPoint(edge.position));
                }

                //effectedBoundaryEdges.Add(edge);
            }
        }

        if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
        {
            Debug.Log("------END------------foreach (var edge in effectedEdges");
            Debug.Log("splitEffectedBoundaryEdges " + splitEffectedBoundaryEdges.Count);
            Debug.Log("effectedBoundaryEdges " + effectedBoundaryEdges.Count);
            Debug.Log("effectedEdges " + effectedEdges.Count);

        }



        Vector3 polygonNormal = pgn.GetEdgeList()[0].GetTriangleNormal(transform);
        Vector3 normalCrossPolygonNormal = Vector3.Cross(planeNormal
            , polygonNormal);
        Vector3 tangentToPolygonNormal = Vector3.Cross(polygonNormal, normalCrossPolygonNormal).normalized;


        //get average position of effectedEdges
        Vector3 worldAverageEffectedEdgesCentroid = new Vector3();

        foreach (var edge in effectedEdges)
        {
            worldAverageEffectedEdgesCentroid += edge.position;
        }

        worldAverageEffectedEdgesCentroid /= (float)effectedEdges.Count;

        worldAverageEffectedEdgesCentroid =
            transform.localToWorldMatrix.MultiplyPoint(worldAverageEffectedEdgesCentroid);




        int trianglesToUse = splitEffectedBoundaryEdges.Count + effectedBoundaryEdges.Count;

        EdgeComparer splitComparer = new EdgeComparer(transform, normalCrossPolygonNormal, worldAverageEffectedEdgesCentroid);

        if (trianglesToUse == 2)
        {
            List<HalfEdgeEdge> splitEdges = new List<HalfEdgeEdge>();
            splitEdges.AddRange(splitEffectedBoundaryEdges);
            splitEdges.AddRange(effectedBoundaryEdges);

            //EdgeComparer triangleComparer = new EdgeComparer(transform, normalCrossPolygonNormal, worldAverageEffectedEdgesCentroid);

            splitEdges.Sort(splitComparer);

            int t = 0;
            int tmaxI = splitEdges.Count;
            foreach (var edge in splitEdges)
            {


                if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
                {
                    Vector3 worldEdgePosition =
                    transform.localToWorldMatrix.MultiplyPoint(edge.position);

                    Vector3 worldNextEdgePosition =
                        transform.localToWorldMatrix.MultiplyPoint(edge.nextEdge.position);

                    Vector3 triangleCentroid =
                        transform.localToWorldMatrix.MultiplyPoint(edge.GetLocalTriangleCentroid());

                    Vector3 edgeToCentroid = (triangleCentroid - worldEdgePosition) * 0.1f;
                    Vector3 nextEdgeToCentroid = (triangleCentroid - worldNextEdgePosition) * 0.1f;

                    float c = 1 * (float)t / tmaxI;
                    DebugDrawer.DrawCircleLine(
                        worldEdgePosition + edgeToCentroid,
                        worldNextEdgePosition + nextEdgeToCentroid,
                        0.001f, 20, new Color(0, 0, c));
                    t++;
                }
            }


            HalfEdgeEdge firstSplit = splitEdges[0];
            HalfEdgeEdge secondSplit = splitEdges[1];

            Vector3 worldStartEdgePosition =
                transform.localToWorldMatrix.MultiplyPoint(firstSplit.position);

            bool startFromOutside =
                shouldBeAbove == MeshSplitterUtils.IsPointAbovePlane(worldStartEdgePosition, planePosition, planeNormal);

            Vector3 start = firstSplit.edgeToPlaneIntersection(transform, planePosition, planeNormal);
            Vector3 end = secondSplit.edgeToPlaneIntersection(transform, planePosition, planeNormal);

            bool FirstOnPlane = firstSplit.isVertexOnPlane(transform, planePosition, planeNormal);

            if (FirstOnPlane)
            {
                Vector3 worldEnd = transform.localToWorldMatrix.MultiplyPoint(secondSplit.position);

                startFromOutside = shouldBeAbove == !MeshSplitterUtils.IsPointAbovePlane(worldEnd, planePosition, planeNormal);

            }


            HalfEdgeEdge intersectionEdge;

            if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
            {
                DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(firstSplit.position), 0.02f, Color.cyan);

                DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(secondSplit.position), 0.02f, Color.grey);
            }

                HandleTriangleSplit
                (startFromOutside,
                firstSplit,
                secondSplit,
                start,
                end,
                out intersectionEdge);



            pgn.GetEdgeList().Clear();
            pgn.GetEdgeList().AddRange(nonEffectedEdges);
            pgn.GetEdgeList().AddRange(splitEffectedBoundaryEdges);
            pgn.GetEdgeList().Add(intersectionEdge);
            return;
        }
        else if (trianglesToUse == 0)
        {
            //foreach (var edge in effectedEdges)
            //{
            //    Vector3 worldPosition = transform.localToWorldMatrix.MultiplyPoint(edge.position);

            //    DebugDrawer.DrawSphere(worldPosition, 0.05f, Color.red);
            //}


            pgn.GetEdgeList().Clear();
            pgn.GetEdgeList().AddRange(nonEffectedEdges);


            return;

        }



        splitEffectedBoundaryEdges.Sort(splitComparer);

        //add all  splitEffectedBoundaryEdges except first and last
        for (int i = 0; i < splitEffectedBoundaryEdges.Count; i++)
        {
            if (i != 0 && i != splitEffectedBoundaryEdges.Count - 1)
            {
                effectedBoundaryEdges.Add(splitEffectedBoundaryEdges[i]);
            }
        }

        HalfEdgeEdge firstSplitEdge = splitEffectedBoundaryEdges[0];
        HalfEdgeEdge secondSplitEdge = splitEffectedBoundaryEdges[splitEffectedBoundaryEdges.Count - 1];

        Vector3 worldStartEdge = transform.localToWorldMatrix.MultiplyPoint(firstSplitEdge.position);

        bool startFromOutsideIntersection = shouldBeAbove == MeshSplitterUtils.IsPointAbovePlane(worldStartEdge, planePosition, planeNormal);

        Vector3 startCurrentEdge = transform.localToWorldMatrix.MultiplyPoint(firstSplitEdge.position);
        Vector3 startNextEdge = transform.localToWorldMatrix.MultiplyPoint(firstSplitEdge.nextEdge.position);

        Vector3 startIntersection;
        MeshSplitterUtils.LineToPlaneIntersection(startCurrentEdge, startNextEdge, planePosition, planeNormal, out startIntersection);


        Vector3 endCurrentEdge = transform.localToWorldMatrix.MultiplyPoint(secondSplitEdge.position);
        Vector3 endNextEdge = transform.localToWorldMatrix.MultiplyPoint(secondSplitEdge.nextEdge.position);

        Vector3 endIntersection;
        MeshSplitterUtils.LineToPlaneIntersection(endCurrentEdge, endNextEdge, planePosition, planeNormal, out endIntersection);

        Vector3 startToEnd = endIntersection - startIntersection;



        Vector3 worldFirstEdgePosition = transform.localToWorldMatrix.MultiplyPoint(firstSplitEdge.GetEdgeLocalCentroid());
        Vector3 worldSecondEdgePosition = transform.localToWorldMatrix.MultiplyPoint(secondSplitEdge.GetEdgeLocalCentroid());



        Vector3 sortingDirection = (worldSecondEdgePosition - worldFirstEdgePosition).normalized;
        Vector3 sortingPosition = (worldFirstEdgePosition + worldSecondEdgePosition) / 2.0f;


        List<HalfEdgeEdge> sortedEdges = new List<HalfEdgeEdge>();

        EdgeComparer nonSplitComparer = new EdgeComparer(transform, sortingDirection, sortingPosition);
        effectedBoundaryEdges.Sort(nonSplitComparer);



        sortedEdges.Add(firstSplitEdge);
        sortedEdges.AddRange(effectedBoundaryEdges);
        sortedEdges.Add(secondSplitEdge);

        //---------------------- Splitting

        List<HalfEdgeEdge> generatedHalfEdges = new List<HalfEdgeEdge>();

        if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
        {
            DebugDrawer.DrawCircleLine(sortingPosition , sortingPosition+ sortingDirection, 0.01f, 20, Color.magenta);
            DebugDrawer.DrawCircleLine(worldAverageEffectedEdgesCentroid, worldAverageEffectedEdgesCentroid + tangentToPolygonNormal, 0.005f, 5, Color.cyan);

            //DebugDrawer.DrawSphere(startIntersection, 0.1f, Color.gray);
            //DebugDrawer.DrawSphere(endIntersection, 0.05f, Color.black);
        }




        int j = 0;
        int maxI = sortedEdges.Count;
        foreach (var edge in sortedEdges)
        {


            if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
            {
                Vector3 worldEdgePosition =
                transform.localToWorldMatrix.MultiplyPoint(edge.position);

                Vector3 worldNextEdgePosition =
                    transform.localToWorldMatrix.MultiplyPoint(edge.nextEdge.position);

                Vector3 triangleCentroid =
                    transform.localToWorldMatrix.MultiplyPoint(edge.GetLocalTriangleCentroid());

                Vector3 edgeToCentroid = (triangleCentroid - worldEdgePosition) * 0.1f;
                Vector3 nextEdgeToCentroid = (triangleCentroid - worldNextEdgePosition) * 0.1f;

                float c = 1 * (float)j / maxI;
                DebugDrawer.DrawCircleLine(
                    worldEdgePosition + edgeToCentroid,
                    worldNextEdgePosition + nextEdgeToCentroid,
                    0.01f, 20, new Color(c, c, c));
                j++;
            }
        }




        HalfEdgeEdge supportEdge = null;

        //-------------------------------------- Find intersection line for first and last split edges----------------------------------//
        //if(sortedEdges.Count == 0) { return; }

        //Debug.Log("sortedEdges.Count " + sortedEdges.Count);

        bool isFirstOnPlane = sortedEdges[0].isVertexOnPlane(transform, planePosition, planeNormal);
        //startFromOutsideIntersection = isFirstOnPlane ? false : true;

        if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
        {
           // Debug.Log(" isFirstOnPlane " + isFirstOnPlane);
        }

        if (isFirstOnPlane)
        {
            Vector3 worldEnd = transform.localToWorldMatrix.MultiplyPoint(sortedEdges[sortedEdges.Count - 1].position);

            startFromOutsideIntersection = shouldBeAbove == !MeshSplitterUtils.IsPointAbovePlane(worldEnd, planePosition, planeNormal);
        }

        for (int i = 1; i < sortedEdges.Count - 1; i++)
        {
            if (startFromOutsideIntersection)
            {
                InsideIntersectionMeshRegeneration(sortedEdges, generatedHalfEdges, i, ref supportEdge, startIntersection, startToEnd);
            }
            else
            {
                OutsideIntersectionMeshRegeneration(sortedEdges, generatedHalfEdges, i, ref supportEdge, startIntersection, startToEnd);
            }
        }

        int beforeCount = pgn.GetEdgeList().Count;
        pgn.GetEdgeList().Clear();

        if (categorizeCount == categorizeCountDebugAt + (sortedEdges.Count - 2) && cutCount == cutCountDebugAt)
        {
            Debug.Log("sortedEdges count " + sortedEdges.Count);
            foreach(var edge in sortedEdges)
            {
                Vector3 edgePos = transform.localToWorldMatrix.MultiplyPoint(edge.position);
                Vector3 edgeNext = transform.localToWorldMatrix.MultiplyPoint(edge.nextEdge.position);

                //Debug.Log("edgePos " + edgePos.ToString("F2") + edgeNext.ToString("F2"));
                //DebugDrawer.DrawCircleLine(edgePos, edgeNext, 0.015f, 8, Color.cyan);
            }

            foreach (var edge in generatedHalfEdges)
            {
                Vector3 edgePos = transform.localToWorldMatrix.MultiplyPoint(edge.position);
                Vector3 edgeNext = transform.localToWorldMatrix.MultiplyPoint(edge.nextEdge.position);


                //DebugDrawer.DrawCircleLine(edgePos, edgeNext, 0.015f, 8, Color.magenta);
            }

            //foreach (var edge in nonEffectedEdges)
            //{
            //    Vector3 edgePos = transform.localToWorldMatrix.MultiplyPoint(edge.position);
            //    Vector3 edgeNext = transform.localToWorldMatrix.MultiplyPoint(edge.nextEdge.position);


            //    DebugDrawer.DrawCircleLine(edgePos, edgeNext, 0.015f, 8, Color.grey);
            //}


            Debug.Log("nonEffectedEdges " + nonEffectedEdges.Count
                + " sortedEdges " + sortedEdges.Count
                + ",generatedHalfEdges "
                + generatedHalfEdges.Count);
        }

        pgn.GetEdgeList().AddRange(nonEffectedEdges);
        pgn.GetEdgeList().AddRange(sortedEdges);
        pgn.GetEdgeList().AddRange(generatedHalfEdges);
        int afterCount = pgn.GetEdgeList().Count;



        if (categorizeCount == categorizeCountDebugAt + (sortedEdges.Count - 2) && cutCount == cutCountDebugAt)
        {
            Debug.Log("END -> beforeCount " + beforeCount + " ,afterCount " + afterCount);
        }


    }

    void HandleTriangleSplit(bool startFromOutsideIntersection
        ,HalfEdgeEdge firstSplitEdge,HalfEdgeEdge secondSplitEdge,Vector3 startIntersection,Vector3 endIntersection,out HalfEdgeEdge intersectionEdge)
    {


        if (!startFromOutsideIntersection)
        {
            firstSplitEdge.position = transform.worldToLocalMatrix.MultiplyPoint(startIntersection);

            intersectionEdge = new HalfEdgeEdge
                (transform.worldToLocalMatrix.MultiplyPoint(endIntersection));

            HalfEdgeEdge.ConnectIntoTriangle(firstSplitEdge, secondSplitEdge, intersectionEdge);

            if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
            {

                DebugDrawer.DrawSphere(
                    transform.localToWorldMatrix.MultiplyPoint(firstSplitEdge.position), 0.005f, Color.red);

                DebugDrawer.DrawSphere(
                    transform.localToWorldMatrix.MultiplyPoint(intersectionEdge.position), 0.01f, Color.black);

                DebugDrawer.DrawSphere(
                    transform.localToWorldMatrix.MultiplyPoint(secondSplitEdge.position), 0.005f, Color.green);


            }
            categorizeCount++;

        }
        else
        {
            
            intersectionEdge = new HalfEdgeEdge
                (transform.worldToLocalMatrix.MultiplyPoint(startIntersection));

            Vector3 originalCentroid = secondSplitEdge.GetEdgeLocalCentroid();

            secondSplitEdge.position =
                transform.worldToLocalMatrix.MultiplyPoint(endIntersection);

            HalfEdgeEdge.ConnectIntoTriangle(firstSplitEdge, intersectionEdge, secondSplitEdge);

            if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
            {

                DebugDrawer.DrawSphere(
                    transform.localToWorldMatrix.MultiplyPoint(firstSplitEdge.position), 0.005f, Color.red);

                DebugDrawer.DrawSphere(
                    transform.localToWorldMatrix.MultiplyPoint(intersectionEdge.position), 0.1f, Color.black);

                DebugDrawer.DrawSphere(
                    transform.localToWorldMatrix.MultiplyPoint(secondSplitEdge.position), 0.005f, Color.green);



            }
            categorizeCount++;
        }
    }


    void OutsideIntersectionMeshRegeneration(List<HalfEdgeEdge> effectedBoundaryEdges, List<HalfEdgeEdge> generatedEdges, int i
        , ref HalfEdgeEdge supportEdge
        ,Vector3 worldStartIntersection,Vector3 startToEndIntersection)
    {
        HalfEdgeEdge baseEdge = effectedBoundaryEdges[i];

        //----------------------------------- Create currentSupportEdge -------------------------------------------------------------//
        HalfEdgeEdge currentSupportEdge;
        if (i == 1)
        {
            currentSupportEdge = effectedBoundaryEdges[0];

            currentSupportEdge.position = transform.worldToLocalMatrix.MultiplyPoint(worldStartIntersection);

        }
        else
        {
            currentSupportEdge = new HalfEdgeEdge(supportEdge.nextEdge.position);
            currentSupportEdge.SetPairing(supportEdge);
            generatedEdges.Add(currentSupportEdge);
        }


        //----------------------------------- Create nextcurrentSupportEdge -------------------------------------------------------------//
        HalfEdgeEdge nextSupportEdge;
        if (i + 1 == effectedBoundaryEdges.Count - 1)
        {
            nextSupportEdge = effectedBoundaryEdges[effectedBoundaryEdges.Count - 1];
            //currentSupportEdge.position = transform.worldToLocalMatrix.MultiplyPoint(worldStartIntersection + startToEndIntersection);
        }
        else
        {
            nextSupportEdge = new HalfEdgeEdge(baseEdge.nextEdge.position);
            generatedEdges.Add(nextSupportEdge);
        }

        supportEdge = nextSupportEdge;
        //----------------------------------- Create Intersection -------------------------------------------------------------//
        //Vector3 intersectionPosition;
        int maxData = effectedBoundaryEdges.Count - 1;
        int currentIndex = i+1;
        float interpolant = (float) currentIndex / maxData;

        HalfEdgeEdge intersectionEdge = new HalfEdgeEdge(
            transform.worldToLocalMatrix.MultiplyPoint(worldStartIntersection + (startToEndIntersection * interpolant)));
        generatedEdges.Add(intersectionEdge);


        CreateAllignedQuad(currentSupportEdge,  nextSupportEdge,  baseEdge,
        intersectionEdge,generatedEdges);

    }
    void InsideIntersectionMeshRegeneration(List<HalfEdgeEdge> effectedBoundaryEdges,List<HalfEdgeEdge> generatedEdges, int i
       , ref HalfEdgeEdge supportEdge, 
       Vector3 worldStartIntersection, Vector3 startToEndIntersection)
    {
        //select Base Half Edge
        HalfEdgeEdge baseEdge = effectedBoundaryEdges[i];


        //----------------------------------- Create currentSupportEdge -------------------------------------------------------------//
        HalfEdgeEdge currentSupportEdge;

        if (i == 1)
        {
            currentSupportEdge = effectedBoundaryEdges[0];
            //currentSupportEdge.position = transform.worldToLocalMatrix.MultiplyPoint(worldStartIntersection);
        }
        else
        {
            currentSupportEdge = new HalfEdgeEdge(baseEdge.nextEdge.position);
            currentSupportEdge.SetPairing(supportEdge);
            generatedEdges.Add(currentSupportEdge);

        }
        //----------------------------------- Create nextcurrentSupportEdge -------------------------------------------------------------//
        int maxData = effectedBoundaryEdges.Count - 1;
        HalfEdgeEdge nextSupportEdge;

        if (i + 1 == effectedBoundaryEdges.Count - 1)
        {
            nextSupportEdge = effectedBoundaryEdges[effectedBoundaryEdges.Count - 1];

            nextSupportEdge.position = transform.worldToLocalMatrix.MultiplyPoint(worldStartIntersection + startToEndIntersection);
        }
        else
        {
            nextSupportEdge = new HalfEdgeEdge(
                transform.worldToLocalMatrix.MultiplyPoint(worldStartIntersection + startToEndIntersection * (float)i / maxData));
            generatedEdges.Add(nextSupportEdge);
        }

        //----------------------------------- Create Intersection -------------------------------------------------------------//

        int currentIndex = i - 1;
        if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
        {
            Debug.Log("currentIndex  " + currentIndex);
        }

        //i - 1 < 0 ? 0 : i - 1 ;
        float interpolant = (float)currentIndex / maxData;

        HalfEdgeEdge intersectionEdge = new HalfEdgeEdge(
            transform.worldToLocalMatrix.MultiplyPoint(worldStartIntersection + (startToEndIntersection * interpolant)));
        generatedEdges.Add(intersectionEdge);


        CreateNonAllignedQuad(currentSupportEdge, nextSupportEdge, baseEdge, intersectionEdge,generatedEdges);

        supportEdge = nextSupportEdge;

    }

    void CreateAllignedQuad(HalfEdgeEdge currentSupport, HalfEdgeEdge nextSupport, HalfEdgeEdge baseEdge, 
        HalfEdgeEdge intersectionEdge,List<HalfEdgeEdge> generatedEdges)
    {
        //create new supporttriangle located at next support
        HalfEdgeEdge supportTriangle = new HalfEdgeEdge(nextSupport.position);


        //currentSupport-intersection-supporttriangle
        HalfEdgeEdge.ConnectIntoTriangle(currentSupport, supportTriangle,intersectionEdge);

        //create new nextsupporttriangle located at currentsupport
        HalfEdgeEdge nextSupportTriangle = new HalfEdgeEdge(currentSupport.position);

        //nextsupporttriangle-nextSupport-baseEdge
        HalfEdgeEdge.ConnectIntoTriangle(nextSupportTriangle, baseEdge,nextSupport );

        supportTriangle.SetPairing(nextSupportTriangle);

        generatedEdges.Add(supportTriangle);
        generatedEdges.Add(nextSupportTriangle);

        if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
        {
            Debug.Log("////////////////////CREATE NON ALLIGN DRAW");

            DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(currentSupport.position), 0.02f, Color.red);

            DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(intersectionEdge.position), 0.02f, Color.black);

            DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(baseEdge.position), 0.02f, Color.green);

            DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(nextSupport.position), 0.02f, Color.blue);

            //Debug.Log(" ")


        }
        categorizeCount++;

    }

    
   
    //this
    void CreateNonAllignedQuad(
        HalfEdgeEdge currentSupport, HalfEdgeEdge nextSupport, HalfEdgeEdge baseEdge, HalfEdgeEdge intersectionEdge,
        List<HalfEdgeEdge> generatedEdges
        )
    {
        //create new supporttriangle located at next support
        HalfEdgeEdge supportTriangle = new HalfEdgeEdge(nextSupport.position);


        //currentSupport-intersection-supporttriangle
        HalfEdgeEdge.ConnectIntoTriangle(currentSupport, intersectionEdge,supportTriangle );

        //create new nextsupporttriangle located at currentsupport
        HalfEdgeEdge nextSupportTriangle = new HalfEdgeEdge(currentSupport.position);

        //nextsupporttriangle-nextSupport-baseEdge
        HalfEdgeEdge.ConnectIntoTriangle(nextSupportTriangle, nextSupport ,baseEdge);

        supportTriangle.SetPairing(nextSupportTriangle);
        generatedEdges.Add(supportTriangle);
        generatedEdges.Add(nextSupportTriangle);

        if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
        {
            Debug.Log("////////////////////CREATE NON ALLIGN DRAW");

            DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(currentSupport.position), 0.02f, Color.red);

            DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(intersectionEdge.position), 0.02f, Color.black);

            DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(baseEdge.position), 0.02f, Color.green);

            DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(nextSupport.position), 0.02f, Color.blue);

            //Debug.Log(" ")


        }
        categorizeCount++;
    }






    void recrusiveCheckTriangleSplitState(HalfEdgeEdge edge,Vector3 planePosition,Vector3 planeNormal
        ,List<HalfEdgeEdge> effectedEdge, List<HalfEdgeEdge> unEffectedEdge,bool shouldkeepAbove)
    {
        if(edge == null) { return; }
        if (edge.isVisited) { return; }

        edge.MarkTriangleVisited();

        HalfEdgeEdge nextEdge, prevEdge;
        edge.GetTrianglesEdges(out nextEdge, out prevEdge);



        if ( edge.isSplitByPlane(transform,planePosition,planeNormal) ||
            nextEdge.isSplitByPlane(transform, planePosition, planeNormal) ||
            prevEdge.isSplitByPlane(transform, planePosition, planeNormal))
        {
            effectedEdge.Add(edge);
            effectedEdge.Add(nextEdge);
            effectedEdge.Add(prevEdge);
        }
        else
        {
            //Debug.Log("non split");
            Vector3 worldCentroid = transform.localToWorldMatrix.MultiplyPoint(edge.GetLocalTriangleCentroid());

            if (shouldkeepAbove == MeshSplitterUtils.IsPointAbovePlane(worldCentroid, planePosition, planeNormal))
            {
                unEffectedEdge.Add(edge);
                unEffectedEdge.Add(nextEdge);
                unEffectedEdge.Add(prevEdge);
            }
           

        }


        if (!edge.isBoundary)
        {
            recrusiveCheckTriangleSplitState(edge.pairingEdge, planePosition, planeNormal,effectedEdge,unEffectedEdge, shouldkeepAbove);
        }

        if (!nextEdge.isBoundary)
        {
            recrusiveCheckTriangleSplitState(nextEdge.pairingEdge, planePosition, planeNormal, effectedEdge, unEffectedEdge, shouldkeepAbove);
        }

        if (!prevEdge.isBoundary)
        {
            recrusiveCheckTriangleSplitState(prevEdge.pairingEdge, planePosition, planeNormal, effectedEdge, unEffectedEdge, shouldkeepAbove);
        }

    }


    void DetectSplitIslands(List<List<SplittablePolygon>> slicingIslandList, List<SplittablePolygon> splitPgn )
    {
        SplittablePolygon initialSplitPgn;

        bool foundUnvisitedSplitPolygon = FindFirstUnvisitedPolygonWithState(splitPgn
                   , out initialSplitPgn, PolygonState.Split);

        Debug.Log("splitPgn detect " + splitPgn.Count);

        foreach(var pol in splitPgn)
        {
            pol.visited = false;
        }

        //while a non visited split polygon can be found 
        while (foundUnvisitedSplitPolygon)
        {
            Debug.Log("->Creating island");
            List<SplittablePolygon> slicingIsland = new List<SplittablePolygon>();
          
            recursiceDetectSplitPolygonIsland
                (initialSplitPgn, slicingIsland);

            Debug.Log("island " + slicingIsland.Count);

            slicingIslandList.Add(slicingIsland);

            foundUnvisitedSplitPolygon = FindFirstUnvisitedPolygonWithState(splitPgn
            , out initialSplitPgn, PolygonState.Split);
        }

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
            }

        }

        return false;
    }

    bool FindFirstUnvisitedPolygonWithState(List<SplittablePolygon> polygons
        , out SplittablePolygon unvisitedPolygon, PolygonState includedState)
    {
        unvisitedPolygon = null;

        foreach (SplittablePolygon polygon in polygons)
        {
            if (!polygon.visited && polygon.state == includedState)
            {
                unvisitedPolygon = polygon;
                return true;
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

    public int debugR = 0;
    public int current = 0;
    void recursiceDetectSplitPolygonIsland(SplittablePolygon polygon,List<SplittablePolygon> splitIsland)
    {

        polygon.visited = true;

        splitIsland.Add(polygon);
        current++;

        Vector3 polygonCentroid = transform.localToWorldMatrix.MultiplyPoint(polygon.localCentroid);

        foreach (var edge in polygon.GetEdgeList())
        {
            if (edge.isBoundary)
            {
                if (edge.pairingEdge != null)
                {
                    var parentPolygonNext = edge.pairingEdge.parentPolygon;
                    
                    if (parentPolygonNext.state == PolygonState.Split && !parentPolygonNext.visited)
                    {
                        recursiceDetectSplitPolygonIsland(
                            edge.pairingEdge.parentPolygon, splitIsland);
                    }
                }
            }
        }


    }
    



}
