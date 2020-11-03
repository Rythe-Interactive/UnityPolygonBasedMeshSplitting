using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugDrawer : MonoBehaviour
{
    public static List<DrawObject> drawObject = new List<DrawObject>();

    private void Start()
    {
        //DrawSphere(new Vector3(0, 1, 0), 0.5f, new Color(1, 0, 0));
        //DrawCircleLine(new Vector3(0, 0, 0), new Vector3(0, 0, 1), 0.02f, 5, new Color(0, 1, 0));

    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        //drawObject.Add(new )
    }

    public static void DrawSphere(Vector3 position,float radius,Color color)
    {
        drawObject.Add(new SphereObject(position, radius, color));
    }

    public static void DrawCircleLine(Vector3 start,Vector3 end,float radius,int circleCount, Color color)
    {
        drawObject.Add(new SphereLine(start, end, radius, circleCount, color));
    }

    private void OnDrawGizmos()
    {
        foreach(var obj in drawObject)
        {
            obj.Draw();
        }
    }

    public void OnApplicationQuit()
    {
        drawObject.Clear();
    }
}

public abstract class DrawObject
{
    public Color color;

    public DrawObject(Color color)
    {
        this.color = color;
    }

    public abstract void Draw();

}

public class SphereObject : DrawObject
{
    public float radius;
    public Vector3 position;
    public SphereObject(Vector3 position,float radius, Color color) :base(color)
    {
        this.position = position;
        this.radius = radius;
    }

    public override void Draw()
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(position, radius);
    }

}

public class SphereLine : DrawObject
{
    public float radius;
    public Vector3 start, startToEnd;
    int sphereCount;
    public SphereLine(Vector3 start, Vector3 end, float radius,int sphereCount, Color color) : base(color)
    {
        this.start = start;
        this.startToEnd = end - start;
        this.sphereCount = sphereCount;
        this.radius = radius;
    }

    public override void Draw()
    {
        if(sphereCount <= 0) { return; }

        Gizmos.color = color;
        for (int i = 0; i < sphereCount; i++)
        {
            float t = (float)i / (sphereCount-1);
            Gizmos.DrawSphere(start + startToEnd * t, radius);
        }

    }

}




