using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BuildingCreator : MonoBehaviour
{
    [Header("Building blocks", order = 0)]
    [SerializeField] private GameObject _rectangleBlock;
    [SerializeField] private GameObject _roof;
    [SerializeField] private GameObject _grass;
    [SerializeField] private Material _roofMaterial;
    [SerializeField] private Material _grassMaterial;
    [SerializeField] private int _doorHeight;
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
    const float BlockWidth = 1;
    const float BlockHeight = 2;

    public void CreateBuildings(List<List<PlotMarker>> plots, Vector3 centre)
    {
        _centre = centre;

        foreach (var plot in plots)
        {
            var cleanPlot = RemoveOverlappingMarkers(plot);

            int height = GetHeight(plot[0].transform.position, centre);

            if (IsPlotOnRoad(plot))
            {
                var buildingColour = GetRandomColour();

                CreateBuildingWithinMarkers(cleanPlot, height, buildingColour);
                CreateRoof(cleanPlot, height, buildingColour);
            }
            else
            {
                CreateGrassedArea(cleanPlot);
            }
        }
    }

    private List<PlotMarker> RemoveOverlappingMarkers(List<PlotMarker> plot)
    {
        var positions = new List<Vector3>();
        var cleanPlot = new List<PlotMarker>();

        foreach (var p in plot)
        {
            bool tooClose = false;

            foreach (var pos in positions)
            {
                if (Vector3.Distance(p.transform.position, pos) < 1)
                    tooClose = true;
            }

            if (tooClose) continue;

            cleanPlot.Add(p);
            positions.Add(p.transform.position);
        }

        return cleanPlot;
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

    private void CreateBuildingWithinMarkers(List<PlotMarker> plot, int wallHeight, Color buildingColour)
    {
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
        Vector3 halfWay = a.transform.position + direction * (distance / 2);

        Quaternion rotation = Quaternion.LookRotation(direction);
        rotation *= Quaternion.Euler(0, -90, 90);

        Vector3 buffer = new Vector3(0, BlockLength / 2, 0);

        Vector3 startPoint = a.transform.position;

        int blocks = (int)(distance / BlockLength);
        int staticBlock = blocks;

        for (int w = 0; w < wallHeight; w++)
        {
            // Check for existing wall

            var hitsFromA = Physics.OverlapSphere(a.transform.position + (direction * BlockLength / 3) + new Vector3(0, BlockHeight * w, 0), 0.1f);
            var hitsFromB = Physics.OverlapSphere(b.transform.position - (direction * BlockLength / 3) + new Vector3(0, BlockHeight * w, 0), 0.1f);
            //var hits = Physics.OverlapSphere(halfWay, 1f);
            if (hitsFromA.Length > 0 && hitsFromB.Length > 0) continue;
            //if (hits.Length > 0) continue;

            float gapsDistance = 0;

            for (int i = 0; i < blocks; i++)
            {
                if (w < _doorHeight)
                {
                    if (i == staticBlock / 2) continue;
                }

                if (Random.Range(0, 3 + 1) == 0)
                    gapsDistance += Random.Range(0, _maxGapDistanceBetweenBlocks);

                var block = Instantiate(_rectangleBlock);
                block.transform.position = startPoint;
                block.transform.rotation = rotation;
                block.transform.Translate(-(buffer + new Vector3(0, BlockLength, 0) * i + new Vector3(0, gapsDistance, 0)));
                block.transform.Translate(new Vector3(BlockHeight * w, 0, 0));
                block.transform.position += block.transform.forward * 0.1f;

                var blockMat = block.GetComponent<MeshRenderer>().material;
                blockMat.SetColor("TintColour", tint);
                blockMat.SetVector("OffsetWood", GetRandomOffset());
                blockMat.SetVector("OffsetMarks", GetRandomOffset());

                blocks = (int)((distance - gapsDistance) / BlockLength);
            }

            float remainingDistance = (distance - gapsDistance) % 3;

            var endBlock = Instantiate(_rectangleBlock);
            endBlock.transform.position = startPoint;
            endBlock.transform.rotation = rotation;
            endBlock.transform.localScale = new Vector3(endBlock.transform.localScale.x, endBlock.transform.localScale.y * (remainingDistance / BlockLength), endBlock.transform.localScale.z);
            Vector3 endBuffer = new Vector3(0, remainingDistance / 2, 0);
            endBlock.transform.Translate(-(endBuffer + new Vector3(0, BlockLength, 0) * blocks + new Vector3(0, gapsDistance, 0)));
            endBlock.transform.Translate(new Vector3(BlockHeight * w, 0, 0));
            endBlock.transform.position += endBlock.transform.forward * 0.1f;

            var endBlockMat = endBlock.GetComponent<MeshRenderer>().material;
            endBlockMat.SetColor("TintColour", tint);
            endBlockMat.SetVector("OffsetWood", GetRandomOffset());
            endBlockMat.SetVector("OffsetMarks", GetRandomOffset());
        }

        Physics.SyncTransforms();
    }

    private bool CheckForDoor(PlotMarker p1, PlotMarker p2) // Not used
    {
        float distance = Vector3.Distance(p1.transform.position, p2.transform.position);
        Vector3 direction = (p2.transform.position - p1.transform.position).normalized;
        Vector3 halfWay = p1.transform.position + direction * (distance / 2);
        Vector3 normal = new Vector3(-direction.z, 0, direction.x);

        RaycastHit hit;

        if (Physics.SphereCast(halfWay + normal * BlockLength, 1, normal, out hit, 100))
        {
            if (hit.transform.tag == "Road")
            {
                return true;
            }
        }

        return false;
    }

    private void CreateRoof(List<PlotMarker> plot, int height, Color buildingColour)
    {
        int n = plot.Count;

        Vector2[] vertices2D = new Vector2[n];

        for (int i = 0; i < n; i++)
        {
            vertices2D[i] = new Vector2(plot[i].transform.position.x, plot[i].transform.position.z);
        }

        var triangulator = new Triangulator(vertices2D);

        var indicies = triangulator.Triangulate();

        Vector3[] vertices3D = new Vector3[n];

        for (int k = 0; k < n; k++)
        {
            vertices3D[k] = new Vector3(vertices2D[k].x, 0, vertices2D[k].y);
        }

        var roof = Instantiate(_roof);

        var mesh = new Mesh();
        mesh.vertices = vertices3D;
        mesh.uv = vertices2D;
        mesh.triangles = indicies;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var meshRenderer = roof.AddComponent<MeshRenderer>();
        var mat = new Material(_roofMaterial);
        mat.SetColor("TintColour", buildingColour);
        meshRenderer.material = mat;

        var meshFilter = roof.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        roof.transform.position = new Vector3(0, height * BlockHeight - BlockHeight / 2 + 0.01f, 0);
    }

    private void CreateGrassedArea(List<PlotMarker> plot)
    {
        int n = plot.Count;

        Vector2[] vertices2D = new Vector2[n];

        for (int i = 0; i < n; i++)
        {
            vertices2D[i] = new Vector2(plot[i].transform.position.x, plot[i].transform.position.z);
        }

        var triangulator = new Triangulator(vertices2D);

        var indicies = triangulator.Triangulate();

        Vector3[] vertices3D = new Vector3[n];

        for (int k = 0; k < n; k++)
        {
            vertices3D[k] = new Vector3(vertices2D[k].x, 0, vertices2D[k].y);
        }

        var grass = Instantiate(_grass);

        var mesh = new Mesh();
        mesh.vertices = vertices3D;
        mesh.uv = vertices2D;
        mesh.triangles = indicies;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var meshRenderer = grass.AddComponent<MeshRenderer>();
        var mat = new Material(_grassMaterial);
        meshRenderer.material = mat;

        var meshFilter = grass.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        var meshCollider = grass.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    public Vector3 GetRandomOffset()
    {
        float x = Random.Range(0, 10.0f);
        float y = Random.Range(0, 10.0f);

        return new Vector3(x, y, 0);
    }
}
