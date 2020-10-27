using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HalfEdgeFinder))]
public class PolygonMeshSplitter : MonoBehaviour
{
    void CutAt()
    {
        //SplitPolygon: a List<Polygon> contining the polygons collidiing with the splitting plane

        //SplitPolygonBelow: a List<Polygon> containing the polygons completely Below the polygon
        //SplitPolygonAbove: a List<Polygon> containing the polygons completely Above the polygon

        //for each polygon in mesh
            //Put polygon in appropriate list


        //for each polygon in splitPolygon
            //find all triangles that are intersecting with intersection point

            //do island detection


            //for each island, arrange based on tangent












    }


}
