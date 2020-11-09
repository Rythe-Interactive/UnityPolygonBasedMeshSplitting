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

    void CopySplitPolygons(List<SplittablePolygon> splitPolygons, PolygonState connectState)
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
            foreach (HalfEdgeEdge edge in pol.GetEdgeList())
            {
                edge.copyEdge = null;
            }
        }
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

            CopySplitPolygons(splitPgn, splitState);




            //do island detection on splitPgn
            List<List<SplittablePolygon>> slicingIslandList = new List<List<SplittablePolygon>>();
         
            DetectSplitIslands(slicingIslandList, splitPgn);

            //--------------------Slice intersecting polygons ---------------------------------------------//
            Debug.Log("slicingIslandList  " + slicingIslandList.Count);
            foreach (List<SplittablePolygon> polygonIsland in slicingIslandList)
            {
                Debug.Log("polygonIsland has count  " + polygonIsland.Count);
                foreach(SplittablePolygon pgn in polygonIsland)
                {
                    pgn.visited = false;
                    categorizeEdges(pgn, splitState, cutPlanePosition, cutPlaneNormal);
                }
            }

            



            foreach (List<SplittablePolygon> polygonIsland in slicingIslandList)
            {
                //Debug.Log("polygonIsland count " + polygonIsland.Count);
                foreach (SplittablePolygon pgn in polygonIsland)
                {
                    pgn.resetVisited();
                }
            }

            foreach (var pgn in splitPgn)
            {
                pgn.visited = false;
            }


                //Put split polygons in a single list
            foreach (var polygonList in slicingIslandList)
            {
                unSplitPgn.AddRange(polygonList);
            }

            PrimitiveMesh splitMesh = new PrimitiveMesh(unSplitPgn, new List<PrimitiveTriangle>());
            splitMesh.createNewGameObject(gameObject);


            foundUnvisitedPolygon = FindFirstUnvisitedPolygonWithExcluded(polygons, out initialSplit, PolygonState.Split);
            //break;
            cutCount++;
        }

        Debug.Log("Final Cut count " + cutCount);

        //Destroy(gameObject);

    }



    void categorizeEdges(SplittablePolygon pgn, PolygonState splitState,Vector3 planePosition,Vector3 planeNormal)
    {
        Debug.Log("categorizeEdges");
        pgn.resetVisited();
        //Split Edge boundaries

        //EffectedTriangles
        List<HalfEdgeEdge> effectedEdges = new List<HalfEdgeEdge>();

        //non effectedTriangles
        List<HalfEdgeEdge> nonEffectedEdges = new List<HalfEdgeEdge>();

        bool shouldBeAbove = splitState == PolygonState.Above;

        //recursive check each triangle if its effected by the split
        recrusiveCheckTriangleSplitState(pgn.GetEdgeList()[0], planePosition, planeNormal,
            effectedEdges, nonEffectedEdges, shouldBeAbove);

        Debug.Log(" effectedEdges " + effectedEdges.Count);


        foreach (var edge in nonEffectedEdges)
        {
            edge.isVisited = false;
        }

        List<HalfEdgeEdge> effectedBoundaryEdges = new List<HalfEdgeEdge>();
        List<HalfEdgeEdge> splitEffectedBoundaryEdges = new List<HalfEdgeEdge>();

        foreach (var edge in effectedEdges)
        {
            bool isVisitedBoundary = edge.isBoundary && edge.isVisited;
            bool isVisitedNonBoundary = !edge.isBoundary && !edge.pairingEdge.isVisited;
            bool edgeAtCorrectSpot = shouldBeAbove ?
                edge.isEdgePartlyAbovePlane(transform, planePosition, planeNormal) : edge.isEdgePartlyBelowPlane(transform, planePosition, planeNormal);

            if ((isVisitedBoundary || isVisitedNonBoundary) && edgeAtCorrectSpot)
            {
                //bool isSplit = edge.isSplitByPlaneNonRobust(transform, planePosition, planeNormal);

                //if(isSplit)
                //{
                //    effectedBoundaryEdges.Add(edge);
                //}
                //else
                //{
                //    splitEffectedBoundaryEdges.Add(edge);
                //}

                effectedBoundaryEdges.Add(edge);
            }
        }

        Vector3 polygonNormal = effectedBoundaryEdges[0].GetTriangleNormal(transform);
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



        //project av

        //float mult = shouldBeAbove ? -1 : 1;
        //Vector3 worldSupport;
        //Vector3 supportDirection = tangentToPolygonNormal * mult;
        //MeshSplitterUtils.GetSupportPoint(transform, effectedBoundaryEdges, worldAverageEffectedEdgesCentroid
        //    , supportDirection, out worldSupport);


        //float tSupportPoint = MeshSplitterUtils.FindLineToPlaneInterpolant
        //    (planePosition, planeNormal, worldSupport, -supportDirection);


        //MeshSplitterUtils.LineToPlaneIntersection
        //    (worldAverageEffectedEdgesCentroid,
        //    worldAverageEffectedEdgesCentroid + tangentToPolygonNormal,
        //    planePosition,
        //    planeNormal,
        //    out worldAverageEffectedEdgesCentroid);

        //worldAverageEffectedEdgesCentroid += supportDirection * tSupportPoint;



        Debug.Log(" effectedBoundaryEdges " + effectedBoundaryEdges.Count);

        pgn.GetEdgeList().Clear();

        pgn.GetEdgeList().AddRange(nonEffectedEdges);
        //pgn.GetEdgeList().AddRange(effectedEdges);


        //get tangent normals 


        EdgeComparer comparer = new EdgeComparer(transform, normalCrossPolygonNormal, worldAverageEffectedEdgesCentroid);

        //splitEffectedBoundaryEdges.Sort(comparer);
        effectedBoundaryEdges.Sort(comparer);




        //---------------------- Splitting

        List<HalfEdgeEdge> generatedHalfEdges = new List<HalfEdgeEdge>();

        if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
        {
            DebugDrawer.DrawCircleLine(worldAverageEffectedEdgesCentroid, worldAverageEffectedEdgesCentroid + normalCrossPolygonNormal, 0.05f, 5, Color.magenta);
            DebugDrawer.DrawCircleLine(worldAverageEffectedEdgesCentroid, worldAverageEffectedEdgesCentroid + tangentToPolygonNormal, 0.05f, 5, Color.cyan);
            //DebugDrawer.DrawSphere(worldSupport, 0.05f, Color.green);

        }




        int j = 0;
        int maxI = effectedBoundaryEdges.Count;
        foreach (var edge in effectedBoundaryEdges)
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
                    0.01f, 20, new Color(c,c,c));
                j++;
            }
        }

        Vector3 worldStartEdge = transform.localToWorldMatrix.MultiplyPoint(effectedBoundaryEdges[0].position);

        bool startFromOutsideIntersection = shouldBeAbove == MeshSplitterUtils.IsPointAbovePlane(worldStartEdge, planePosition, planeNormal);

        //Debug.Log("startFromOutsideIntersection ? " + startFromOutsideIntersection);

        HalfEdgeEdge supportEdge = null;

        //-------------------------------------- Find intersection line ----------------------------------//
        if(effectedBoundaryEdges.Count == 0) { return; }

        Vector3 startCurrentEdge = transform.localToWorldMatrix.MultiplyPoint(effectedBoundaryEdges[0].position);
        Vector3 startNextEdge = transform.localToWorldMatrix.MultiplyPoint(effectedBoundaryEdges[0].nextEdge.position);

        Vector3 startIntersection;
        MeshSplitterUtils.LineToPlaneIntersection(startCurrentEdge, startNextEdge, planePosition, planeNormal, out startIntersection);


        Vector3 endCurrentEdge = transform.localToWorldMatrix.MultiplyPoint(effectedBoundaryEdges[effectedBoundaryEdges.Count-1].position);
        Vector3 endNextEdge = transform.localToWorldMatrix.MultiplyPoint(effectedBoundaryEdges[effectedBoundaryEdges.Count - 1].nextEdge.position);

        Vector3 endIntersection;
        MeshSplitterUtils.LineToPlaneIntersection(endCurrentEdge, endNextEdge, planePosition, planeNormal, out endIntersection);

        Vector3 startToEnd = endIntersection - startIntersection;

        //if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
        //{
        //    DebugDrawer.DrawSphere(
        //       endCurrentEdge, 0.2f, Color.gray);

        //    DebugDrawer.DrawSphere(
        //       endNextEdge, 0.2f, Color.gray);

        //    DebugDrawer.DrawSphere(
        //     endIntersection, 0.2f, Color.cyan);
        //}


        for (int i = 1; i < effectedBoundaryEdges.Count - 1; i++)
        {
            if(startFromOutsideIntersection)
            {
                OutsideIntersectionMeshRegeneration(effectedBoundaryEdges, i
            ,ref supportEdge, planePosition, planeNormal, tangentToPolygonNormal, startIntersection, startToEnd);
            }
            else
            {
                InsideIntersectionMeshRegeneration(effectedBoundaryEdges, i
            ,ref supportEdge, planePosition, planeNormal, tangentToPolygonNormal,startIntersection,startToEnd);
            }
            

        }


        pgn.GetEdgeList().AddRange(effectedBoundaryEdges);
        pgn.GetEdgeList().AddRange(generatedHalfEdges);




    }



    void InsideIntersectionMeshRegeneration(List<HalfEdgeEdge> effectedBoundaryEdges, int i
        , ref HalfEdgeEdge supportEdge, Vector3 planePosition, Vector3 planeNormal, Vector3 tangentToPolygonNormal
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
        }


        //----------------------------------- Create nextcurrentSupportEdge -------------------------------------------------------------//
        HalfEdgeEdge nextSupportEdge;
        if (i + 1 == effectedBoundaryEdges.Count - 1)
        {
            nextSupportEdge = effectedBoundaryEdges[effectedBoundaryEdges.Count - 1];

        }
        else
        {
            nextSupportEdge = new HalfEdgeEdge(baseEdge.nextEdge.position);

            

        }
        supportEdge = nextSupportEdge;
        //----------------------------------- Create Intersection -------------------------------------------------------------//
        //Vector3 intersectionPosition;
        int maxData = effectedBoundaryEdges.Count - 1;
        int currentIndex = i + 1;
        float interpolant = (float) currentIndex / maxData;

        if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
        {
            Debug.Log("currentIndex  " + currentIndex);
        }

            HalfEdgeEdge intersectionEdge = new HalfEdgeEdge(
            transform.worldToLocalMatrix.MultiplyPoint(worldStartIntersection + (startToEndIntersection * interpolant) ));

        CreateAllignedQuad(currentSupportEdge,  nextSupportEdge,  baseEdge,
        intersectionEdge);

    }

    void CreateAllignedQuad(HalfEdgeEdge currentSupport, HalfEdgeEdge nextSupport, HalfEdgeEdge baseEdge, 
        HalfEdgeEdge intersectionEdge)
    {
        //create support triangle located at base end
        HalfEdgeEdge supportTriangle = new HalfEdgeEdge(baseEdge.nextEdge.position);

        //connect currentSupport - supportTriangle - baseEdge
        HalfEdgeEdge.ConnectIntoTriangle(currentSupport,baseEdge,supportTriangle );

        //create next support triangle located at base
        HalfEdgeEdge nextSupportTriangle = new HalfEdgeEdge(currentSupport.position);

        //connect next support triangle - intersection - next support
        HalfEdgeEdge.ConnectIntoTriangle(nextSupportTriangle, nextSupport, intersectionEdge);

        supportTriangle.SetPairing(nextSupportTriangle);

        if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
        {
            Debug.Log("////////////////////CREATE NON ALLIGN DRAW");

            DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(currentSupport.position), 0.1f, Color.red);

            DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(intersectionEdge.position), 0.1f, Color.black);

            DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(baseEdge.position), 0.1f, Color.green);

            DebugDrawer.DrawSphere(
                transform.localToWorldMatrix.MultiplyPoint(nextSupport.position), 0.1f, Color.blue);

            //Debug.Log(" ")


        }
        categorizeCount++;


    }

    
    void OutsideIntersectionMeshRegeneration(List<HalfEdgeEdge> effectedBoundaryEdges,int i
        ,ref HalfEdgeEdge supportEdge,Vector3 planePosition,Vector3 planeNormal,Vector3 tangentToPolygonNormal,
        Vector3 worldStartIntersection, Vector3 startToEndIntersection)
    {
        //select Base Half Edge
        HalfEdgeEdge baseEdge = effectedBoundaryEdges[i];
       

        //----------------------------------- Create currentSupportEdge -------------------------------------------------------------//
        HalfEdgeEdge currentSupportEdge;

        if (i == 1)
        {
            currentSupportEdge = effectedBoundaryEdges[0];

        }
        else
        {
            currentSupportEdge = new HalfEdgeEdge(baseEdge.nextEdge.position);
            currentSupportEdge.SetPairing(supportEdge);

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
                transform.worldToLocalMatrix.MultiplyPoint(worldStartIntersection + startToEndIntersection * (float)i/maxData));
        }

        //----------------------------------- Create Intersection -------------------------------------------------------------//
        
        int currentIndex = i - 1 < 0 ? 0 : i - 1;
        if (categorizeCount == categorizeCountDebugAt && cutCount == cutCountDebugAt)
        {
            Debug.Log("currentIndex  " + currentIndex);
        }

        //i - 1 < 0 ? 0 : i - 1 ;
        float interpolant = (float)currentIndex / maxData;

        HalfEdgeEdge intersectionEdge = new HalfEdgeEdge(
            transform.worldToLocalMatrix.MultiplyPoint(worldStartIntersection + (startToEndIntersection * interpolant)));

        CreateNonAllignedQuad(currentSupportEdge, nextSupportEdge, baseEdge, intersectionEdge
    , tangentToPolygonNormal, planePosition, planeNormal);

        supportEdge = nextSupportEdge;

    }

    void CreateNonAllignedQuad(
        HalfEdgeEdge currentSupport, HalfEdgeEdge nextSupport, HalfEdgeEdge baseEdge, HalfEdgeEdge intersectionEdge
        , Vector3 connectionEdgeDirection, Vector3 planePosition, Vector3 planeNormal)
    {
        //create new supporttriangle located at next support
        HalfEdgeEdge supportTriangle = new HalfEdgeEdge(nextSupport.position);


        //currentSupport-intersection-supporttriangle
        HalfEdgeEdge.ConnectIntoTriangle(currentSupport, intersectionEdge, supportTriangle);

        //create new nextsupporttriangle located at currentsupport
        HalfEdgeEdge nextSupportTriangle = new HalfEdgeEdge(currentSupport.position);

        //nextsupporttriangle-nextSupport-baseEdge
        HalfEdgeEdge.ConnectIntoTriangle(nextSupportTriangle, nextSupport, baseEdge);

        supportTriangle.SetPairing(nextSupportTriangle);

 


    }






    void recrusiveCheckTriangleSplitState(HalfEdgeEdge edge,Vector3 planePosition,Vector3 planeNormal
        ,List<HalfEdgeEdge> effectedEdge, List<HalfEdgeEdge> unEffectedEdge,bool shouldkeepAbove)
    {
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

        //while a non visited split polygon can be found 
        while (foundUnvisitedSplitPolygon)
        {
            Debug.Log("->Creating island");
            List<SplittablePolygon> slicingIsland = new List<SplittablePolygon>();

            recursiceDetectSplitPolygonIsland
                (initialSplitPgn, slicingIsland);

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


    void recursiceDetectSplitPolygonIsland(SplittablePolygon polygon,List<SplittablePolygon> splitIsland)
    {
        if(polygon.state == PolygonState.Split && !polygon.visited)
        {
            polygon.visited = true;

            splitIsland.Add(polygon);

            foreach(var edge in polygon.GetEdgeList())
            {
                if(edge.isBoundary)
                {
                    if(edge.pairingEdge != null)
                    {
                        recursiceDetectSplitPolygonIsland(
                            edge.pairingEdge.parentPolygon, splitIsland);
                    }
                }
            }
        }
    }



}
