using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BuildingCreator : MonoBehaviour
{
    [Header("Building blocks", order = 0)]
    [SerializeField] private GameObject _rectangleBlock;
    [SerializeField] [Range(0, 1)] private float _maxGapDistanceBetweenBlocks = 0.25f;
    [SerializeField] private LayerMask _mask;

    [Header("Zones", order = 1)]
    [SerializeField] private Vector3 _centre;
    [SerializeField] private float _zone1MinDistanceToCentre;
    [SerializeField] private int _zone1MaxHeight;
    [SerializeField] private int _zone1MinHeight;
    [SerializeField] private int _zone2MaxHeight;
    [SerializeField] private int _zone2MinHeight;

    const float BlockLength = 3;
    const float BlockWidth = 1; // Height is also 1

    public void CreateBuildings(List<List<PlotMarker>> plots, Vector3 centre)
    {
        _centre = centre;

        foreach (var plot in plots)
        {
            if (IsPlotOnRoad(plot))
                CreateBuildingWithinMarkers(plot, GetHeight(plot[0].transform.position, centre));
        }
    }

    private bool IsPlotOnRoad(List<PlotMarker> plot)
    {
        int roadConnections = 0;

        foreach (var p in plot)
        {
            if (p.Road != null) return true;
            if (p.LeftConnection != null && p.LeftConnection.Road != null) roadConnections++;
            if (p.RightConnection != null && p.RightConnection.Road != null) roadConnections++;
        }

        if (roadConnections >= 2) return true;

        return false;
    }

    private int GetHeight(Vector3 point, Vector3 centre)
    {
        var distance = Vector3.Distance(point, centre);

        if (distance <= _zone1MinDistanceToCentre)
            return Random.Range(_zone1MinHeight, _zone1MaxHeight + 1);
        else
            return Random.Range(_zone2MinHeight, _zone2MaxHeight + 1);
    }

    private void CreateBuildingWithinMarkers(List<PlotMarker> plot, int wallHeight)
    {
        var buildingColour = GetRandomColour();

        for (int p = 0; p < plot.Count; p++)
        {
            PlotMarker p1 = plot[p];
            PlotMarker p2;

            if (p == plot.Count - 1)
                p2 = plot[0];
            else
                p2 = plot[p + 1];

            CreateWallBetweenTwoMarkers(p1, p2, wallHeight, buildingColour);
        }
    }
    
    private Color GetRandomColour()
    {
        float r = Random.Range(150, 255 + 1) / 255.0f;
        float g = Random.Range(150, 255 + 1) / 255.0f;
        float b = Random.Range(150, 255 + 1) / 255.0f;

        return new Color(r, g, b, 1);
    }

    private void CreateWallBetweenTwoMarkers(PlotMarker a, PlotMarker b, int wallHeight, Color tint)
    {
        float distance = Vector3.Distance(a.transform.position, b.transform.position);

        Vector3 direction = (b.transform.position - a.transform.position).normalized;

        // Check for existing wall

        Quaternion rotation = Quaternion.LookRotation(direction);
        rotation *= Quaternion.Euler(0, -90, 90);

        Vector3 buffer = new Vector3(0, BlockLength / 2, 0);

        Vector3 startPoint = a.transform.position;

        int blocks = (int)(distance / BlockLength);

        for (int w = 0; w < wallHeight; w++)
        {
            var hitsFromA = Physics.OverlapSphere(a.transform.position + (direction * BlockLength / 3) + new Vector3(0, BlockWidth * w, 0), 0.1f);
            var hitsFromB = Physics.OverlapSphere(b.transform.position - (direction * BlockLength / 3) + new Vector3(0, BlockWidth * w, 0), 0.1f);
            if (hitsFromA.Length > 0 && hitsFromB.Length > 0) continue;

            float gapsDistance = 0;

            for (int i = 0; i < blocks; i++)
            {
                if (Random.Range(0, 3 + 1) == 0)
                    gapsDistance += Random.Range(0, _maxGapDistanceBetweenBlocks);

                var block = Instantiate(_rectangleBlock);
                block.transform.position = startPoint;
                block.transform.rotation = rotation;
                block.transform.Translate(-(buffer + new Vector3(0, BlockLength, 0) * i + new Vector3(0, gapsDistance, 0)));
                block.transform.Translate(new Vector3(BlockWidth * w, 0, 0));

                var blockMat = block.GetComponent<MeshRenderer>().material;
                blockMat.SetColor("TintColour", tint);

                blocks = (int)((distance - gapsDistance) / BlockLength);
            }

            float remainingDistance = (distance - gapsDistance) % 3;

            var endBlock = Instantiate(_rectangleBlock);
            endBlock.transform.position = startPoint;
            endBlock.transform.rotation = rotation;
            endBlock.transform.localScale = new Vector3(endBlock.transform.localScale.x, endBlock.transform.localScale.y * (remainingDistance / BlockLength), endBlock.transform.localScale.z);
            Vector3 endBuffer = new Vector3(0, remainingDistance / 2, 0);
            endBlock.transform.Translate(-(endBuffer + new Vector3(0, BlockLength, 0) * blocks + new Vector3(0, gapsDistance, 0)));
            endBlock.transform.Translate(new Vector3(BlockWidth * w, 0, 0));

            var endBlockMat = endBlock.GetComponent<MeshRenderer>().material;
            endBlockMat.SetColor("TintColour", tint);
        }

        Physics.SyncTransforms();
    }
}
