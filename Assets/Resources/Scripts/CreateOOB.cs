using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateOOB
{
    public static Rectangle GetMinRectangle(List<PlotMarker> plotMarkers, bool isLeftCycle)
    {
        var tempMarkers = new List<PlotMarker>();
        tempMarkers.AddRange(plotMarkers);

        if (!IsConvexPolygon(plotMarkers))
        {
            var AABBRectangle = new Rectangle();

            AABBRectangle.Axis[0] = new Vector2(1, 0);
            AABBRectangle.Axis[1] = new Vector2(0, 1);

            float minX = int.MaxValue;
            float maxX = int.MinValue;
            float minZ = int.MaxValue;
            float maxZ = int.MinValue;

            for (int p = 0; p < plotMarkers.Count; p++)
            {
                if (plotMarkers[p].transform.position.x < minX)
                    minX = plotMarkers[p].transform.position.x;
                
                if (plotMarkers[p].transform.position.x > maxX)
                    maxX = plotMarkers[p].transform.position.x;

                if (plotMarkers[p].transform.position.z < minZ)
                    minZ = plotMarkers[p].transform.position.z;
                
                if (plotMarkers[p].transform.position.z > maxZ)
                    maxZ = plotMarkers[p].transform.position.z;
            }

            AABBRectangle.Extents[0] = (maxX - minX) / 2;
            AABBRectangle.Extents[1] = (maxZ - minZ) / 2;

            AABBRectangle.Centre = new Vector2(maxX - AABBRectangle.Extents[0], maxZ - AABBRectangle.Extents[1]);

            AABBRectangle.Type = BoxType.AABB;

            AABBRectangle.Area = (AABBRectangle.Extents[0] * 2) * (AABBRectangle.Extents[1] * 2);

            AABBRectangle.SetCorners();

            return AABBRectangle;
        }

        // --If is convex polygon find better bounding box--

        if (!isLeftCycle)
            tempMarkers.Reverse();

        var minRectangle = new Rectangle();

        int i0 = tempMarkers.Count - 1;
        for (int i1 = 0; i1 < tempMarkers.Count; i0 = i1++)
        {
            Vector2 origin = new Vector2(tempMarkers[i0].transform.position.x, tempMarkers[i0].transform.position.z);
            Vector2 U0 = new Vector2(tempMarkers[i1].transform.position.x, tempMarkers[i1].transform.position.z) - origin;
            U0.Normalize();
            Vector2 U1 = -Vector2.Perpendicular(U0);
            float min0 = 0, max0 = 0;
            float max1 = 0;

            for (int k = 0; k < tempMarkers.Count; k++)
            {
                Vector2 D = new Vector2(tempMarkers[k].transform.position.x, tempMarkers[k].transform.position.z) - origin;
                float dot = Vector2.Dot(U0, D);

                if (dot < min0)
                    min0 = dot;
                else if (dot > max0)
                    max0 = dot;

                dot = Vector2.Dot(U1, D);

                if (dot > max1)
                    max1 = dot;
            }

            float area = (max0 - min0) * max1;

            if (area < minRectangle.Area)
            {
                minRectangle.Centre = origin + ((min0 + max0) / 2) * U0 + (max1 / 2) * U1;
                minRectangle.Axis[0] = U0;
                minRectangle.Axis[1] = U1;
                minRectangle.Extents[0] = (max0 - min0) / 2;
                minRectangle.Extents[1] = max1 / 2;
                minRectangle.Area = area;
            }
        }

        minRectangle.SetCorners();

        minRectangle.Type = BoxType.OBB;

        return minRectangle;
    }

    private static bool IsConvexPolygon(List<PlotMarker> plotMarkers)
    {
        bool negative = false;
        bool positive = false;

        for (int p = 0; p < plotMarkers.Count; p++)
        {
            Vector2 p0;
            Vector2 p1;
            Vector2 p2;

            if (p == 0)
                p0 = new Vector2(plotMarkers[plotMarkers.Count - 1].transform.position.x, plotMarkers[plotMarkers.Count - 1].transform.position.z);
            else
                p0 = new Vector2(plotMarkers[p - 1].transform.position.x, plotMarkers[p - 1].transform.position.z);

            p1 = new Vector2(plotMarkers[p].transform.position.x, plotMarkers[p].transform.position.z);

            if (p == plotMarkers.Count - 1)
                p2 = new Vector2(plotMarkers[0].transform.position.x, plotMarkers[0].transform.position.z);
            else
                p2 = new Vector2(plotMarkers[p + 1].transform.position.x, plotMarkers[p + 1].transform.position.z);

            float crossProduct = CrossProduct(p0, p1, p2);

            if (crossProduct < 0)
            {
                negative = true;
            }
            else
            {
                positive = true;
            }

            if (negative == positive) return false;
        }

        return true;
    }

    private static float CrossProduct(Vector2 A, Vector2 B, Vector2 C)
    {
        float BAx = A.x - B.x;
        float BAy = A.y - B.y;
        float BCx = C.x - B.x;
        float BCy = C.y - B.y;

        return BAx * BCy - BAy * BCx;
    }
}

public class Rectangle
{
    public Vector2 Centre { get; set; }
    public Vector2[] Axis { get; set; }
    public float[] Extents { get; set; }
    public float Area { get; set; }
    public Vector2[] Corners { get; set; } // TR, BR, BL, TR
    public BoxType Type { get; set; }

    public Rectangle()
    {
        Centre = new Vector2();
        Axis = new Vector2[2];
        Extents = new float[2];
        Area = float.MaxValue;
        Corners = new Vector2[4];
    }

    public void SetCorners()
    {
        Corners[0] = Centre + Extents[0] * Axis[0] + Extents[1] * Axis[1]; // TR
        Corners[1] = Centre + Extents[0] * Axis[0] - Extents[1] * Axis[1]; // BR
        Corners[2] = Centre - Extents[0] * Axis[0] - Extents[1] * Axis[1]; // BL
        Corners[3] = Centre - Extents[0] * Axis[0] + Extents[1] * Axis[1]; // TR
    }
}

public enum BoxType
{
    OBB,
    AABB
}
