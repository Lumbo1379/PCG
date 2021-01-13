using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BuildingCreator : MonoBehaviour
{
    [Header("Building blocks", order = 0)]
    [SerializeField] private GameObject _rectangleBlock;

    const float BlockLength = 3;
    const float BlockWidth = 1; // Height is also 1

    public void CreateBuildings(List<List<PlotMarker>> plots)
    {
        foreach (var plot in plots)
        {
            CreateBuildingWithinMarkers(plot);
        }
    }

    private void CreateBuildingWithinMarkers(List<PlotMarker> plot)
    {
        for (int p = 0; p < plot.Count; p++)
        {
            PlotMarker p1 = plot[p];
            PlotMarker p2;

            if (p == plot.Count - 1)
                p2 = plot[0];
            else
                p2 = plot[p + 1];

            CreateWallBetweenTwoMarkers(p1, p2);
        }
    }    

    private void CreateWallBetweenTwoMarkers(PlotMarker a, PlotMarker b)
    {

    }
}
