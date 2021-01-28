using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class RoadMapCreator : MonoBehaviour
{
    [Header("Controllers", order = -1)]
    [SerializeField] private BuildingCreator _buildingController;
    [SerializeField] private GardenController _gardenController;
    [SerializeField] private bool _generateBuildings = true;
    [SerializeField] private bool _generateGardens = true;

    [Header("Perlin Generation", order = 0)]
    [SerializeField] private int _width = 256;
    [SerializeField] private int _length = 256;
    [SerializeField] private float _resolution = 20;
    [SerializeField] [Range(0, 100000)] private int _maxOffsetX = 1000;
    [SerializeField] [Range(0, 100000)] private int _maxOffsetY = 1000;

    [Header("Road Generation", order = 1)]
    [SerializeField] private GameObject _roadPiece;
    [SerializeField] [Range(0, 1)] private float _minRoadValue = 0.5f;

    [Header("Plot Generation", order = 2)]
    [SerializeField] private GameObject _plotPiece;
    [SerializeField] [Range(0, 10000)] private float _minPlotArea;
    [SerializeField] [Range(0, 1000)] private float _minPlotExtent;

    [Header("Debug Road Generation", order = 3)]
    [SerializeField] private TMP_Text _mapText;
    [SerializeField] private TMP_Text _seedText;
    [SerializeField] private bool _generateFile = false;
    [SerializeField] private bool _stepThroughCheckForConnection = false;
    [SerializeField] private bool _step;
    [SerializeField] private Color32 _seenColour = new Color32(255, 0, 0, 255);

    [Header("Debug Plot Generation", order = 4)]
    [SerializeField] private bool _debugPlots = false;
    [SerializeField] private Color32 _highlightColour = new Color32(0, 255, 0, 255);
    [SerializeField] private int _numberOfPlots;
    [SerializeField] private PlotMarker _firstMarker;
    [SerializeField] private PlotMarker _lastMarker;
    [SerializeField] private bool _isLeftCycle;
    [SerializeField] private int _plotIndexToHighlight = 0;
    [SerializeField] private bool _highlightPlot = false;
    [SerializeField] private List<PlotMarker> NEXTPARCEL;

    [Header("Debug Bounding Box Generation", order = 5)]
    [SerializeField] private bool _debugBoundingBox = false;
    [SerializeField] private Color32 _highlightBoundingBoxColour = new Color32(0, 255, 0, 255);
    [SerializeField] private int _plotIndexToHighlightBB = 0;
    [SerializeField] private BoxType _type;
    [SerializeField] private bool _showBoundingBox = false;

    [Header("Debug Dividied Plots", order = 6)]
    [SerializeField] private bool _debugPlotsFinal = false;
    [SerializeField] private Color32 _highlightColourFinal = new Color32(0, 255, 0, 255);
    [SerializeField] private int _numberOfDividedPlots;
    [SerializeField] private int _plotIndexToHighlightFinal = 0;
    [SerializeField] private bool _highlightPlotFinal = false;
    [SerializeField] private PlotMarker[] PLOTS;

    [Header("Seed", order = 7)]
    [SerializeField] private bool _useSpecificSeed;
    [SerializeField] private int _seedX;
    [SerializeField] private int _seedY;

    [Header("Car", order = 8)]
    [SerializeField] private CarController _car;

    public int PlotsToTryToMakeConnections { get; set; }

    private int _offsetX;
    private int _offsetY;
    private bool[,] _roadMap; // True is road, false is empty
    private GameObject[,] _roadMapObjects;
    private List<List<Point>> _islands;
    private int _debugPointIndex;

    private float[,] _cost;
    private int[,] _linkX;
    private int[,] _linkY;
    private bool[,] _closed;
    private bool[,] _inPath;

    private List<List<PlotMarker>> _plotContainers;
    private bool _plotContainersMapped;

    private int _lastHighlightedPlotIndex;
    private int _lastHighlightedPlotIndexFinal;
    private Color32 _originalPlotColour;

    private GameObject[] _previousBoundingBox;

    private List<List<PlotMarker>> _finalDividedPlots;

    private float _maxX, _minX, _maxZ, _minZ;

    private void Awake()
    {
        if (!_useSpecificSeed)
        {
            _offsetX = Random.Range(0, _maxOffsetX);
            _offsetY = Random.Range(0, _maxOffsetY);
        }
        else
        {
            _offsetX = _seedX;
            _offsetY = _seedY;
        }

        _seedText.text = "Seed (" + _offsetX + ", " + _offsetY + ")";

        _roadMap = new bool[_length, _width];
        _roadMapObjects = new GameObject[_length, _width];

        _cost = new float[_length, _width];
        _linkX = new int[_length, _width];
        _linkY = new int[_length, _width];
        _closed = new bool[_length, _width];
        _inPath = new bool[_length, _width];

        _debugPointIndex = 0;
        _step = false;
        _debugPlots = false;
        _debugBoundingBox = false;
        _highlightPlot = false;
        _highlightPlotFinal = false;
        _showBoundingBox = false;
        _plotIndexToHighlight = 0;
        _plotIndexToHighlightFinal = 0;
        _lastHighlightedPlotIndex = -1;
        _lastHighlightedPlotIndexFinal = -1;
        _originalPlotColour = _plotPiece.GetComponent<MeshRenderer>().sharedMaterial.color;

        _plotContainers = new List<List<PlotMarker>>();
        _plotContainersMapped = false;

        NEXTPARCEL = new List<PlotMarker>();

        _finalDividedPlots = new List<List<PlotMarker>>();

        _maxX = float.MinValue;
        _maxZ = float.MinValue;

        _minX = float.MaxValue;
        _minZ = float.MaxValue;
    }

    private void Start()
    {
        GenerateMap();
    }

    private void Update()
    {
        if (_stepThroughCheckForConnection && _step && _islands != null && _debugPointIndex < _islands[0].Count)
        {
            _roadMapObjects[_islands[0][_debugPointIndex].X, _islands[0][_debugPointIndex].Y].SetActive(true);

            int characterIndex = _islands[0][_debugPointIndex].Y + _islands[0][_debugPointIndex].X * (_length + 1); // +1 for \n
            int meshIndex = _mapText.textInfo.characterInfo[characterIndex].materialReferenceIndex;
            int vertexIndex = _mapText.textInfo.characterInfo[characterIndex].vertexIndex;

            Color32[] vertexColors = _mapText.textInfo.meshInfo[meshIndex].colors32;
            vertexColors[vertexIndex + 0] = _seenColour;
            vertexColors[vertexIndex + 1] = _seenColour;
            vertexColors[vertexIndex + 2] = _seenColour;
            vertexColors[vertexIndex + 3] = _seenColour;

            _debugPointIndex++;
            _mapText.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
            _step = false;
        }

        if (_debugPlots)
        {
            _numberOfPlots = _plotContainers.Count;

            if (_highlightPlot)
            {
                _highlightPlot = false;
                _firstMarker = null;
                _lastMarker = null;

                if (_lastHighlightedPlotIndex != -1)
                {
                    foreach (var plot in _plotContainers[_lastHighlightedPlotIndex])
                    {
                        var plotMat = plot.GetComponent<MeshRenderer>().material;
                        plotMat.color = _originalPlotColour;
                    }
                }

                if (_plotIndexToHighlight > _numberOfPlots - 1) return;

                foreach (var plot in _plotContainers[_plotIndexToHighlight])
                {
                    var plotMat = plot.GetComponent<MeshRenderer>().material;
                    plotMat.color = _highlightColour;
                }

                _firstMarker = _plotContainers[_plotIndexToHighlight][0];
                _lastMarker = _plotContainers[_plotIndexToHighlight][_plotContainers[_plotIndexToHighlight].Count - 1];
                _isLeftCycle = _plotContainers[_plotIndexToHighlight][0].IsLeftCycle;

                _lastHighlightedPlotIndex = _plotIndexToHighlight;
            }
        }

        if (_debugPlotsFinal)
        {
            _numberOfDividedPlots = _finalDividedPlots.Count;

            if (_highlightPlotFinal)
            {
                _highlightPlotFinal = false;

                if (_lastHighlightedPlotIndexFinal != -1)
                {
                    foreach (var plot in _finalDividedPlots[_lastHighlightedPlotIndexFinal])
                    {
                        var plotMat = plot.GetComponent<MeshRenderer>().material;
                        plotMat.color = _originalPlotColour;
                    }
                }

                if (_plotIndexToHighlightFinal > _numberOfDividedPlots - 1) return;

                PLOTS = _finalDividedPlots[_plotIndexToHighlightFinal].ToArray();

                foreach (var plot in _finalDividedPlots[_plotIndexToHighlightFinal])
                {
                    var plotMat = plot.GetComponent<MeshRenderer>().material;
                    plotMat.color = _highlightColour;
                }

                _lastHighlightedPlotIndexFinal = _plotIndexToHighlightFinal;
            }
        }

        if (_debugBoundingBox)
        {
            _debugPlots = true;

            if (_showBoundingBox)
            {
                _showBoundingBox = false;

                if (_previousBoundingBox != null)
                {
                    for (int i = 0; i < _previousBoundingBox.Length; i++)
                    {
                        Destroy(_previousBoundingBox[i]);
                    }
                }

                if (_plotIndexToHighlightBB > _numberOfPlots - 1) return;

                var boundingBox = CreateOOB.GetMinRectangle(_plotContainers[_plotIndexToHighlightBB], _plotContainers[_plotIndexToHighlightBB][0].IsLeftCycle);

                _type = boundingBox.Type;

                _previousBoundingBox = new GameObject[5];

                var centre = Instantiate(_plotPiece);
                var centreMat = centre.GetComponent<MeshRenderer>().material;
                centreMat.color = _highlightBoundingBoxColour;
                centre.transform.position = new Vector3(boundingBox.Centre.x, 0, boundingBox.Centre.y);
                _previousBoundingBox[0] = centre;

                var corner1 = Instantiate(_plotPiece);
                var corner1Mat = corner1.GetComponent<MeshRenderer>().material;
                corner1Mat.color = _highlightBoundingBoxColour;
                corner1.transform.position = new Vector3(boundingBox.Corners[0].x, 0, boundingBox.Corners[0].y);
                _previousBoundingBox[1] = corner1;

                var corner2 = Instantiate(_plotPiece);
                var corner2Mat = corner2.GetComponent<MeshRenderer>().material;
                corner2Mat.color = _highlightBoundingBoxColour;
                corner2.transform.position = new Vector3(boundingBox.Corners[1].x, 0, boundingBox.Corners[1].y);
                _previousBoundingBox[2] = corner2;

                var corner3 = Instantiate(_plotPiece);
                var corner3Mat = corner3.GetComponent<MeshRenderer>().material;
                corner3Mat.color = _highlightBoundingBoxColour;
                corner3.transform.position = new Vector3(boundingBox.Corners[2].x, 0, boundingBox.Corners[2].y);
                _previousBoundingBox[3] = corner3;

                var corner4 = Instantiate(_plotPiece);
                var corner4Mat = corner4.GetComponent<MeshRenderer>().material;
                corner4Mat.color = _highlightBoundingBoxColour;
                corner4.transform.position = new Vector3(boundingBox.Corners[3].x, 0, boundingBox.Corners[3].y);
                _previousBoundingBox[4] = corner4;

                if (NEXTPARCEL.Count == 0)
                {
                    ParcelPlot(_plotContainers[_plotIndexToHighlightBB], _plotContainers[_plotIndexToHighlightBB][0].IsLeftCycle, _plotContainers[_plotIndexToHighlightBB], 0);
                }
                else
                {
                    ParcelPlot(NEXTPARCEL, _plotContainers[_plotIndexToHighlightBB][0].IsLeftCycle, _plotContainers[_plotIndexToHighlightBB], 0);
                }
            }

            if (_previousBoundingBox != null && _previousBoundingBox.Length != 0)
            {
                Debug.DrawRay(_previousBoundingBox[1].transform.position, Vector3.Normalize(_previousBoundingBox[2].transform.position - _previousBoundingBox[1].transform.position) * Vector3.Distance(_previousBoundingBox[1].transform.position, _previousBoundingBox[2].transform.position), Color.yellow);
                Debug.DrawRay(_previousBoundingBox[2].transform.position, Vector3.Normalize(_previousBoundingBox[3].transform.position - _previousBoundingBox[2].transform.position) * Vector3.Distance(_previousBoundingBox[2].transform.position, _previousBoundingBox[3].transform.position), Color.yellow);
                Debug.DrawRay(_previousBoundingBox[3].transform.position, Vector3.Normalize(_previousBoundingBox[4].transform.position - _previousBoundingBox[3].transform.position) * Vector3.Distance(_previousBoundingBox[3].transform.position, _previousBoundingBox[4].transform.position), Color.yellow);
                Debug.DrawRay(_previousBoundingBox[4].transform.position, Vector3.Normalize(_previousBoundingBox[1].transform.position - _previousBoundingBox[4].transform.position) * Vector3.Distance(_previousBoundingBox[4].transform.position, _previousBoundingBox[1].transform.position), Color.yellow);
            }
        }

        if (!_plotContainersMapped && PlotsToTryToMakeConnections == 0)
        {
            foreach (var plotStart in _plotContainers)
            {
                PlotMarker dummy;
                var firstPlot = plotStart[0];

                if (firstPlot.IsLeftCycle)
                    dummy = plotStart[0].ForwardConnection.LeftConnection;
                else
                    dummy = plotStart[0].ForwardConnection.RightConnection;

                plotStart.Add(firstPlot.ForwardConnection);

                while (dummy != firstPlot)
                {
                    plotStart.Add(dummy);

                    if (firstPlot.IsLeftCycle)
                    {
                        if (dummy.ForwardConnection == null)
                        {
                            dummy = dummy.LeftConnection;
                        }
                        else
                        {
                            plotStart.Add(dummy.ForwardConnection);
                            dummy = dummy.ForwardConnection.LeftConnection;
                        }
                    }
                    else
                    {
                        if (dummy.ForwardConnection == null)
                        {
                            dummy = dummy.RightConnection;
                        }
                        else
                        {
                            plotStart.Add(dummy.ForwardConnection);
                            dummy = dummy.ForwardConnection.RightConnection;
                        }
                    }
                }
            }

            _plotContainersMapped = true;

            DividePlots();

            if (_generateBuildings)
            {
                _buildingController.CreateBuildings(_finalDividedPlots, new Vector3(-_width / 2 * 20, 0, _length / 2 * 45));

                if (_generateGardens)
                    _gardenController.PlantGarden(_minX, _maxX, _minZ, _maxZ, _offsetX, _offsetY);

                _car.Initialise(_roadMapObjects, _width, _length);
            }
        }
    }

    private void GenerateMap()
    {
        for (int row = 0; row < _length; row++)
        {
            for (int column = 0; column < _width; column++)
            {
                MarkPosition(row, column, CalculateCoordinateValue(row, column));
            }
        }

        CheckKillNeighbours();

        while (_islands == null || _islands.Count > 1)
        {
            FindIslands();
            ConnectIslands();
        }

        if (_generateFile)
            WriteMapToFile();

        var firstRoad = GetFirstRoad();
        bool[,] searched = new bool[_length, _width];
        ConnectRoad(firstRoad.X, firstRoad.Y, searched, -1, -1);

        MapPlots();

        if (_stepThroughCheckForConnection)
        {
            foreach (var road in _roadMapObjects)
            {
                if (road != null) road.SetActive(false);
            }
        }
    }

    private Point GetFirstRoad()
    {
        int row = 0;
        int column = 0;

        while (row < _length)
        {
            if (_roadMap[row, column])
            {
                return new Point(row, column);
            }

            column++;

            if (column >= _width)
            {
                column = 0;
                row++;
            }
        }

        return new Point(-1, -1);
    }

    private void FindIslands()
    {
        _islands = new List<List<Point>>();
        bool[,] searched = new bool[_length, _width];
        int islandCounter = 0;

        for (int row = 0; row < _length; row++)
        {
            for (int column = 0; column < _width; column++)
            {
                if (_roadMap[row, column] && !searched[row, column])
                {
                    _islands.Add(new List<Point>());
                    FloodFill(row, column, searched, islandCounter);
                    islandCounter++;
                }

                searched[row, column] = true;
            }
        }
    }

    private void FloodFill(int row, int column, bool[,] searched, int islandCounter)
    {
        if (row < 0 || row >= _length) return;
        if (column < 0 || column >= _width) return;

        if (_roadMap[row, column] && !searched[row, column])
        {
            _islands[islandCounter].Add(new Point(row, column));
            searched[row, column] = true;

            FloodFill(row + 1, column, searched, islandCounter);
            FloodFill(row, column + 1, searched, islandCounter);
            FloodFill(row - 1, column, searched, islandCounter);
            FloodFill(row, column - 1, searched, islandCounter);
            FloodFill(row + 1, column + 1, searched, islandCounter);
            FloodFill(row + 1, column - 1, searched, islandCounter);
            FloodFill(row - 1, column + 1, searched, islandCounter);
            FloodFill(row - 1, column - 1, searched, islandCounter);
        }
    }

    private void ConnectIslands()
    {
        bool[] connected = new bool[_islands.Count];
        int islandToConnectTo = -1;
        Point closestPoint = null;

        for (int i = 0; i < _islands.Count - 1; i++)
        {
            if (!connected[i])
            {
                for (int k = 0; k < _islands.Count; k++)
                {
                    if (i == k) continue;

                    var point = FindClosestPoint(i, k);

                    if (closestPoint == null || IsPointCloser(_islands[i][0], closestPoint, point))
                    {
                        closestPoint = point;
                        islandToConnectTo = k;
                    }
                }

                PathToIsland(_islands[i][0], closestPoint);

                connected[i] = true;
                connected[islandToConnectTo] = true;
                closestPoint = null;
            }
        }
    }

    private Point FindClosestPoint(int island1, int island2)
    {
        Point closestPoint = null;

        Point startPoint = _islands[island1][0];

        foreach (var point in _islands[island2])
        {
            if (closestPoint == null || IsPointCloser(startPoint, closestPoint, point))
            {
                closestPoint = point;
            }
        }

        return closestPoint;
    }

    private bool IsPointCloser(Point startPoint, Point currentClosest, Point anotherPoint)
    {
        return Point.GetDistance(startPoint, currentClosest) > Point.GetDistance(startPoint, anotherPoint);
    }

    private void PathToIsland(Point start, Point end)
    {
        for (int x = 0; x < _length; x++)
        {
            for (int y = 0; y < _width; y++)
            {
                _closed[x, y] = false;
                _inPath[x, y] = false;
                _cost[x, y] = 10000.0f;
                _linkX[x, y] = -1;
                _linkY[x, y] = -1;
            }
        }

        _cost[start.X, start.Y] = 0;

        int nextX = -1;
        int nextY = -1;

        while (true)
        {
            float lowestCost = 10000.0f;

            for (int x = 0; x < _length; x++)
            {
                for (int y = 0; y < _width; y++)
                {
                    float heuristic = Mathf.Abs(end.X - x) + Mathf.Abs(end.Y - y);

                    if (_cost[x, y] + heuristic < lowestCost && !_closed[x, y])
                    {
                        nextX = x;
                        nextY = y;

                        lowestCost = _cost[x, y] + heuristic;
                    }
                }
            }

            _closed[nextX, nextY] = true;

            if (_closed[end.X, end.Y]) break;

            float parentCost = _cost[nextX, nextY];
            CalculateCost(nextX - 1, nextY, parentCost + 1.0f, nextX, nextY, end); // Down
            CalculateCost(nextX + 1, nextY, parentCost + 1.0f, nextX, nextY, end); // Up
            CalculateCost(nextX, nextY - 1, parentCost + 1.0f, nextX, nextY, end); // Left
            CalculateCost(nextX, nextY + 1, parentCost + 1.0f, nextX, nextY, end); // Right
            CalculateCost(nextX + 1, nextY - 1, parentCost + 1.0f, nextX, nextY, end); // Up-left
            CalculateCost(nextX - 1, nextY + 1, parentCost + 1.0f, nextX, nextY, end); // Down-right
            CalculateCost(nextX + 1, nextY + 1, parentCost + 1.0f, nextX, nextY, end); // Up-right
            CalculateCost(nextX - 1, nextY - 1, parentCost + 1.0f, nextX, nextY, end); // Down-left
        }

        bool done = false;
        int nextClosedX = end.X;
        int nextClosedY = end.Y;

        while (!done)
        {
            _inPath[nextClosedX, nextClosedY] = true;
            _roadMap[nextClosedX, nextClosedY] = true;
            int tmpX = nextClosedX;
            int tmpY = nextClosedY;
            nextClosedX = _linkX[tmpX, tmpY];
            nextClosedY = _linkY[tmpX, tmpY];

            if (nextClosedX == start.X && nextClosedY == start.Y)
                done = true;
        }
    }

    private void CalculateCost(int x, int y, float newCost, int parentX, int parentY, Point target)
    {
        if (IsValid(x, y, target))
        {
            if (!_closed[x, y])
            {
                if (newCost < _cost[x, y])
                {
                    _cost[x, y] = newCost;
                    _linkX[x, y] = parentX;
                    _linkY[x, y] = parentY;
                }
            }
        }
    }

    private bool IsValid(int x, int y, Point target)
    {
        if (x < 0 || x >= _length) return false;
        if (y < 0 || y >= _width) return false;

        return true;
    }

    private float CalculateCoordinateValue(float row, float column)
    {
        return Mathf.PerlinNoise(row / _length * _resolution + _offsetX, column / _width * _resolution + _offsetY);
    }

    private void MarkPosition(int row, int column, float value) // Initial mark, may be killed from too many neighbours
    {
        _roadMap[row, column] = value >= _minRoadValue;
    }

    private void CheckKillNeighbours()
    {
        for (int row = 0; row < _length; row++)
        {
            for (int column = 0; column < _width; column++)
            {
                int neighbours = CountNeighbours(row, column);
                neighbours -= 4;

                if (neighbours > 0)
                    KillDiagonals(row, column, neighbours);
            }
        }
    }

    private int CountNeighbours(int row, int column)
    {
        int count = 0;

        if (row > 0)
        {
            if (_roadMap[row - 1, column]) count++;

            if (column > 0)
            {
                if (_roadMap[row - 1, column - 1]) count++;
            }
        }

        if (row < _length - 1)
        {
            if (_roadMap[row + 1, column]) count++;

            if (column < _width - 1)
            {
                if (_roadMap[row + 1, column + 1]) count++;
            }
        }

        if (column > 0)
        {
            if (_roadMap[row, column - 1]) count++;

            if (row > 0)
            {
                if (_roadMap[row - 1, column - 1]) count++;
            }
        }

        if (column < _width - 1)
        {
            if (_roadMap[row, column + 1]) count++;

            if (row < _length - 1)
            {
                if (_roadMap[row + 1, column + 1]) count++;
            }
        }

        return count;
    }

    private void KillDiagonals(int row, int column, int amountToKill)
    {
        if (row > 0 && column > 0)
        {
            if (_roadMap[row - 1, column - 1])
            {
                _roadMap[row - 1, column - 1] = false;
                amountToKill--;
            }
        }

        if (amountToKill > 0 && row > 0 && column < _width - 1)
        {
            if (_roadMap[row - 1, column + 1])
            {
                _roadMap[row - 1, column + 1] = false;
                amountToKill--;
            }
        }

        if (amountToKill > 0 && row < _length - 1 && column > 0)
        {
            if (_roadMap[row + 1, column - 1])
            {
                _roadMap[row + 1, column - 1] = false;
                amountToKill--;
            }
        }

        if (amountToKill > 0 && row < _length - 1 && column < _width - 1)
        {
            if (_roadMap[row + 1, column + 1])
            {
                _roadMap[row + 1, column + 1] = false;
            }
        }
    }

    private void WriteMapToFile()
    {
        using (var sw = new StreamWriter("RoadMap.txt"))
        {
            for (int row = 0; row < _length; row++)
            {
                for (int column = 0; column < _width; column++)
                {
                    sw.Write(_roadMap[row, column] ? "X" : "O");
                    _mapText.text += _roadMap[row, column] ? "X" : "O";
                }

                sw.WriteLine();
                _mapText.text += "\n";
            }
        }

        Debug.Log("Road map wrote to file!");
    }

    private void ConnectRoad(int row, int column, bool[,] searched, int prevRow, int prevColumn)
    {
        if (row < 0 || row >= _length) return;
        if (column < 0 || column >= _width) return;

        if (_roadMap[row, column] && !searched[row, column])
        {
            _roadMapObjects[row, column] = Instantiate(_roadPiece);

            searched[row, column] = true;

            if (prevRow != -1)
            {
                int xChange = row - prevRow;
                int yChange = column - prevColumn;

                var prevRoad = _roadMapObjects[prevRow, prevColumn];
                var prevRoadPiece = prevRoad.GetComponent<RoadPiece>();
                var currentRoadPiece = _roadMapObjects[row, column].GetComponent<RoadPiece>();

                bool needsIntersection = prevRoadPiece.TailConnected;

                if (xChange == -1 && yChange == -1)
                {
                    ConnectRoadsFromHead(prevRoad, _roadMapObjects[row, column]);

                    if (needsIntersection)
                        CreateIntersection(_roadMapObjects[row, column], prevRoadPiece.IntersectionDecrease);

                    currentRoadPiece.SetBoneRotations(-11.25f - prevRoadPiece.BoneRotation - prevRoadPiece.HeadRotation);
                }
                else if (xChange == -1 && yChange == 1)
                {
                    ConnectRoadsFromHead(prevRoad, _roadMapObjects[row, column]);

                    if (needsIntersection)
                        CreateIntersection(_roadMapObjects[row, column], prevRoadPiece.IntersectionDecrease);

                    currentRoadPiece.SetBoneRotations(11.25f - prevRoadPiece.BoneRotation - prevRoadPiece.HeadRotation);
                }
                else if (xChange == 1 && yChange == -1)
                {
                    ConnectRoadsFromHead(prevRoad, _roadMapObjects[row, column]);

                    if (needsIntersection)
                        CreateIntersection(_roadMapObjects[row, column], prevRoadPiece.IntersectionDecrease);

                    currentRoadPiece.SetBoneRotations(-3.75f - prevRoadPiece.BoneRotation - prevRoadPiece.HeadRotation);
                }
                else if (xChange == 1 && yChange == 1)
                {
                    ConnectRoadsFromHead(prevRoad, _roadMapObjects[row, column]);

                    if (needsIntersection)
                        CreateIntersection(_roadMapObjects[row, column], prevRoadPiece.IntersectionDecrease);

                    currentRoadPiece.SetBoneRotations(3.75f - prevRoadPiece.BoneRotation - prevRoadPiece.HeadRotation);
                }
                else if (xChange == 1)
                {
                    ConnectRoadsFromHead(prevRoad, _roadMapObjects[row, column]);

                    if (needsIntersection)
                        CreateIntersection(_roadMapObjects[row, column], prevRoadPiece.IntersectionDecrease);

                    currentRoadPiece.SetBoneRotations(0 - prevRoadPiece.BoneRotation - prevRoadPiece.HeadRotation);
                }
                else if (yChange == 1)
                {
                    ConnectRoadsFromHead(prevRoad, _roadMapObjects[row, column]);

                    if (needsIntersection)
                        CreateIntersection(_roadMapObjects[row, column], prevRoadPiece.IntersectionDecrease);

                    currentRoadPiece.SetBoneRotations(7.5f - prevRoadPiece.BoneRotation - prevRoadPiece.HeadRotation);
                }
                else if (xChange == -1)
                {

                    ConnectRoadsFromHead(prevRoad, _roadMapObjects[row, column]);

                    if (needsIntersection)
                        CreateIntersection(_roadMapObjects[row, column], prevRoadPiece.IntersectionDecrease);

                    currentRoadPiece.SetBoneRotations(15.0f - prevRoadPiece.BoneRotation - prevRoadPiece.HeadRotation);
                }
                else if (yChange == -1)
                {
                    ConnectRoadsFromHead(prevRoad, _roadMapObjects[row, column]);

                    if (needsIntersection)
                        CreateIntersection(_roadMapObjects[row, column], prevRoadPiece.IntersectionDecrease);

                    currentRoadPiece.SetBoneRotations(-7.5f - prevRoadPiece.BoneRotation - prevRoadPiece.HeadRotation);
                }
            }

            ConnectRoad(row + 1, column, searched, row, column);
            ConnectRoad(row, column + 1, searched, row, column);
            ConnectRoad(row - 1, column, searched, row, column);
            ConnectRoad(row, column - 1, searched, row, column);
            ConnectRoad(row + 1, column + 1, searched, row, column);
            ConnectRoad(row + 1, column - 1, searched, row, column);
            ConnectRoad(row - 1, column + 1, searched, row, column);
            ConnectRoad(row - 1, column - 1, searched, row, column);
        }
    }

    private void ConnectRoadsFromHead(GameObject existingRoad, GameObject connectingRoad) // Uses bone rotation on existing to set head rotation on connecting
    {
        var existingRoadPiece = existingRoad.GetComponent<RoadPiece>();
        var connectingRoadPiece = connectingRoad.GetComponent<RoadPiece>();

        existingRoadPiece.IntersectingRoads.Add(connectingRoad);

        connectingRoadPiece.SetHeadRotation(existingRoadPiece.BoneRotation + existingRoadPiece.HeadRotation);

        var transformation = existingRoadPiece.GetBottomLeft().transform.position - connectingRoadPiece.GetTopLeft().transform.position;
        connectingRoad.transform.position += transformation;

        existingRoadPiece.TailConnection = connectingRoad;
        connectingRoadPiece.HeadConnection = existingRoad;

        existingRoadPiece.TailConnected = true;
        connectingRoadPiece.HeadConnected = true;
    }

    private void CreateIntersection(GameObject road, float yDecrease)
    {
        road.transform.position += new Vector3(0, yDecrease, 0);
    }

    private void MapPlots()
    {
        foreach (var roadPoint in _islands[0])
        {
            PlacePlotMarkerPair(_roadMapObjects[roadPoint.X, roadPoint.Y]);
        }
    }

    private void PlacePlotMarkerPair(GameObject road)
    {
        var roadPiece = road.GetComponent<RoadPiece>();

        if (!roadPiece.TailConnected)
        {
            var plotBottomLeft = Instantiate(_plotPiece);
            var plotBottomRight = Instantiate(_plotPiece);

            var bottomLeftRoadPiecePlotMarker = roadPiece.GetBottomLeftMarker();
            var bottomRightRoadPiecePlotMarker = roadPiece.GetBottomRightMarker();

            plotBottomLeft.transform.position = bottomLeftRoadPiecePlotMarker.transform.position;
            plotBottomLeft.transform.rotation = bottomLeftRoadPiecePlotMarker.transform.rotation;

            plotBottomRight.transform.position = bottomRightRoadPiecePlotMarker.transform.position;
            plotBottomRight.transform.rotation = bottomRightRoadPiecePlotMarker.transform.rotation;

            plotBottomLeft.GetComponent<PlotMarker>().Initialise(roadPiece, false, _plotContainers, this, true);
            plotBottomRight.GetComponent<PlotMarker>().Initialise(roadPiece, false, _plotContainers, this, true);

            PlotsToTryToMakeConnections += 2;
        }

        if (roadPiece.HeadConnection != null && roadPiece.HeadConnection.GetComponent<RoadPiece>().IsIntersection) return; // Don't place plots on intersecting roads

        var plotLeft = Instantiate(_plotPiece);
        var plotRight = Instantiate(_plotPiece);

        var leftRoadPiecePlotMarker = roadPiece.GetMiddleLeft();
        var rightRoadPiecePlotMarker = roadPiece.GetMiddleRight();

        plotLeft.transform.position = leftRoadPiecePlotMarker.transform.position;
        plotLeft.transform.rotation = leftRoadPiecePlotMarker.transform.rotation;

        plotRight.transform.position = rightRoadPiecePlotMarker.transform.position;
        plotRight.transform.rotation = rightRoadPiecePlotMarker.transform.rotation;

        roadPiece.CanMakePlots = true;
        roadPiece.LeftPlotMarker = plotLeft.GetComponent<PlotMarker>();
        roadPiece.RightPlotMarker = plotRight.GetComponent<PlotMarker>();

        plotLeft.GetComponent<PlotMarker>().Initialise(roadPiece, true, _plotContainers, this);
        plotRight.GetComponent<PlotMarker>().Initialise(roadPiece, false, _plotContainers, this);

        PlotsToTryToMakeConnections += 2;
    }

    private void DividePlots()
    {
        foreach (var plot in _plotContainers)
        {
            ParcelPlot(plot, plot[0].IsLeftCycle, plot, 0);
        }
    }

    private void ParcelPlot(List<PlotMarker> plot, bool isLeftCycle, List<PlotMarker> originalPlotList, int depth)
    {
        var boundingBox = CreateOOB.GetMinRectangle(plot, isLeftCycle);

        if (boundingBox.Extents[0] < _minPlotExtent && boundingBox.Extents[0] != 0 || boundingBox.Extents[1] < _minPlotExtent && boundingBox.Extents[1] != 0 || depth > 25)
        {
            Debug.Log($"Base: E0: {boundingBox.Extents[0]} | E1: {boundingBox.Extents[1]} | D: {depth}");
            return;
        }

        if (boundingBox.Area < _minPlotArea)
        {
            _finalDividedPlots.Add(plot);

            Debug.Log("Minimum area");
            return;
        }

        Vector2 b1;
        Vector2 b2;

        const int extentBuffer = 5;

        if (boundingBox.Extents[0] > boundingBox.Extents[1])
        {
            b1 = boundingBox.Centre + (boundingBox.Extents[1] + extentBuffer) * boundingBox.Axis[1];
            b2 = boundingBox.Centre - (boundingBox.Extents[1] + extentBuffer) * boundingBox.Axis[1];
        }
        else
        {
            b1 = boundingBox.Centre + (boundingBox.Extents[0] + extentBuffer) * boundingBox.Axis[0];
            b2 = boundingBox.Centre - (boundingBox.Extents[0] + extentBuffer) * boundingBox.Axis[0];
        }

        Vector2 i1 = new Vector2();
        Vector2 i2 = new Vector2();
        int i1AIndex = 0;
        int i1BIndex = 0;
        int i2AIndex = 0;
        int i2BIndex = 0;

        int numberOfIntersections = 0;

        for (int p = 0; p < plot.Count; p++)
        {
            if (plot[p].transform.position.x < _minX) _minX = plot[p].transform.position.x;
            if (plot[p].transform.position.x > _maxX) _maxX = plot[p].transform.position.x;
            if (plot[p].transform.position.z < _minZ) _minZ = plot[p].transform.position.z;
            if (plot[p].transform.position.z > _maxZ) _maxZ = plot[p].transform.position.z;

            int pAIndex;
            int pBIndex;

            var p1 = new Vector2(plot[p].transform.position.x, plot[p].transform.position.z);
            pAIndex = p;

            Vector2 p2;

            if (p == plot.Count - 1)
            {
                p2 = new Vector2(plot[0].transform.position.x, plot[0].transform.position.z);
                pBIndex = 0;
            }
            else
            {
                p2 = new Vector2(plot[p + 1].transform.position.x, plot[p + 1].transform.position.z);
                pBIndex = p + 1;
            }

            var intersection = LineSegementsIntersect(b1, b2, p1, p2);

            if (intersection != null)
            {
                numberOfIntersections++;

                if (numberOfIntersections == 1)
                {
                    i1 = intersection.Value;

                    i1AIndex = pAIndex;
                    i1BIndex = pBIndex;
                }
                else
                {
                    i2 = intersection.Value;

                    i2AIndex = pAIndex;
                    i2BIndex = pBIndex;
                }
            }
        }

        var pp1 = Instantiate(_plotPiece);
        pp1.transform.position = new Vector3(i1.x, 0, i1.y);

        var pp2 = Instantiate(_plotPiece);
        pp2.transform.position = new Vector3(i2.x, 0, i2.y);

        var pp1Marker = pp1.GetComponent<PlotMarker>();
        var pp2Marker = pp2.GetComponent<PlotMarker>();

        pp1Marker.IsParcelMarker = true;
        pp2Marker.IsParcelMarker = true;

        InsertPlot(plot[i1AIndex], plot[i1BIndex], pp1Marker, isLeftCycle);
        InsertPlot(plot[i2AIndex], plot[i2BIndex], pp2Marker, isLeftCycle);

        pp1Marker.ForwardConnection = pp2Marker;
        pp2Marker.ForwardConnection = pp1Marker;

        pp1Marker.ShowConnectionRays();
        pp2Marker.ShowConnectionRays();

        var parcel1 = new List<PlotMarker>();
        parcel1.AddRange(plot.GetRange(i1AIndex, i1BIndex - i1AIndex));
        parcel1.Add(pp1Marker);
        parcel1.Add(pp2Marker);

        int k = i2BIndex;
        while (k != i1AIndex)
        {
            parcel1.Add(plot[k]);

            k++;

            if (k == plot.Count)
                k = 0;
        }

        var parcel2 = new List<PlotMarker>();
        parcel2.Add(pp1Marker);
        parcel2.AddRange(plot.GetRange(i1BIndex, i2AIndex - i1BIndex + 1));
        parcel2.Add(pp2Marker);

        //NEXTPARCEL = parcel1;

        ParcelPlot(parcel1, isLeftCycle, originalPlotList, depth + 1);
        ParcelPlot(parcel2, isLeftCycle, originalPlotList, depth + 1);

        originalPlotList.Add(pp1Marker);
        originalPlotList.Add(pp2Marker);
    }

    //private void FindPlotToInsert(int a, int b, List<PlotMarker> plot)
    //{
    //    PlotMarker aMarker = plot[a];
    //    PlotMarker bMarker = plot[b];
    //    PlotMarker sharedConnection;

    //    if (aMarker.LeftConnection == bMarker.LeftConnection && aMarker.LeftConnection.IsParcelMarker)
    //        sharedConnection = aMarker.LeftConnection;
    //    else if (aMarker.LeftConnection == bMarker.RightConnection && aMarker.LeftConnection.IsParcelMarker)
    //        sharedConnection = aMarker.LeftConnection;
    //    else if (aMarker.LeftConnection == bMarker.ForwardConnection && aMarker.LeftConnection.IsParcelMarker)
    //        sharedConnection = aMarker.LeftConnection;
    //    else if (aMarker.RightConnection == bMarker.LeftConnection && aMarker.RightConnection.IsParcelMarker)
    //        sharedConnection = aMarker.RightConnection;
    //    else if (aMarker.RightConnection == bMarker.RightConnection && aMarker.RightConnection.IsParcelMarker)
    //        sharedConnection = aMarker.RightConnection;
    //    else if (aMarker.RightConnection == bMarker.ForwardConnection && aMarker.RightConnection.IsParcelMarker)
    //        sharedConnection = aMarker.RightConnection;
    //    else if (aMarker.ForwardConnection == bMarker.LeftConnection && aMarker.ForwardConnection.IsParcelMarker)
    //        sharedConnection = aMarker.ForwardConnection;
    //    else if (aMarker.ForwardConnection == bMarker.RightConnection && aMarker.ForwardConnection.IsParcelMarker)
    //        sharedConnection = aMarker.ForwardConnection;
    //    else if (aMarker.ForwardConnection == bMarker.ForwardConnection && aMarker.ForwardConnection.IsParcelMarker)
    //        sharedConnection = aMarker.ForwardConnection;
    //    else
    //    {
    //        Debug.LogError("Something has gone wrong");
    //        return;
    //    }

    //    plot.Insert(a + 1, sharedConnection);
    //}

    private void InsertPlot(PlotMarker a, PlotMarker b, PlotMarker newPlot, bool isLeftCycle)
    {
        PlotConnection aConnection;
        PlotConnection bConnection;

        if (a.LeftConnection == b)
            aConnection = PlotConnection.Left;
        else if (a.RightConnection == b)
            aConnection = PlotConnection.Right;
        else
            aConnection = PlotConnection.Forward;

        if (b.LeftConnection == a)
            bConnection = PlotConnection.Left;
        else if (b.RightConnection == a)
            bConnection = PlotConnection.Right;
        else
            bConnection = PlotConnection.Forward;

        if (aConnection == PlotConnection.Left)
        {
            a.LeftConnection = newPlot;
            newPlot.RightConnection = a;
        }
        else if (aConnection == PlotConnection.Right)
        {
            a.RightConnection = newPlot;
            newPlot.LeftConnection = a;
        }
        else
        {
            a.ForwardConnection = newPlot;

            if (isLeftCycle)
                newPlot.RightConnection = a;
            else
                newPlot.LeftConnection = a;
        }

        if (bConnection == PlotConnection.Left)
        {
            b.LeftConnection = newPlot;
            newPlot.RightConnection = b;
        }
        else if (bConnection == PlotConnection.Right)
        {
            b.RightConnection = newPlot;
            newPlot.LeftConnection = b;
        }
        else
        {
            b.ForwardConnection = newPlot;

            if (isLeftCycle)
                newPlot.LeftConnection = b;
            else
                newPlot.RightConnection = b;
        }
    }

    //private bool ValidateConnection(PlotMarker a, PlotMarker b)
    //{
    //    return a.LeftConnection == b || a.RightConnection == b || a.ForwardConnection == b;
    //}

    //private void ValidateConnections(List<PlotMarker> plot)
    //{
    //    foreach (var plotMarker in plot)
    //    {
    //        bool leftValid = true;
    //        bool rightValid = true;
    //        bool forwardValid = true;

    //        var left = plotMarker.LeftConnection;
    //        var right = plotMarker.RightConnection;
    //        var forward = plotMarker.ForwardConnection;

    //        if (left != null)
    //            leftValid = DoesHoldAConnection(plotMarker, left);

    //        if (right != null)
    //            rightValid = DoesHoldAConnection(plotMarker, right);

    //        if (forward != null)
    //            forwardValid = DoesHoldAConnection(plotMarker, forward);

    //        Debug.Log($"L: {leftValid} | R: {rightValid} | F: {forwardValid}  | error: {plotMarker.PossibleConnectionError}");

    //        if (!leftValid)
    //            plotMarker.LeftConnection = null;

    //        if (!rightValid)
    //            plotMarker.RightConnection = null;

    //        if (!forwardValid)
    //            plotMarker.ForwardConnection = null;
    //    }
    //}

    private bool DoesHoldAConnection(PlotMarker a, PlotMarker b)
    {
        return b.LeftConnection == a || b.RightConnection == a || b.ForwardConnection == a;
    }

    enum PlotConnection
    {
        Left,
        Right,
        Forward
    }

    private Vector2? LineSegementsIntersect(Vector2 p, Vector2 p2, Vector2 q, Vector2 q2)
    {
        var intersection = new Vector2();

        var r = p2 - p;
        var s = q2 - q;
        var rxs = Cross(r, s);
        var qpxr = Cross(q - p, r);

        if (IsZero(rxs) && !IsZero(qpxr))
            return null;

        var t = Cross(q - p, s) / rxs;

        var u = Cross(q - p, r) / rxs;

        if (!IsZero(rxs) && (0 <= t && t <= 1) && (0 <= u && u <= 1))
        {
            intersection = p + t * r;

            return intersection;
        }

        return null;
    }

    private float Cross(Vector2 p1, Vector2 p2)
    {
        return p1.x * p2.y - p1.y * p2.x;
    }

    private bool IsZero(float d)
    {
        const float epsilon = 0;

        return Mathf.Abs(d) < epsilon;
    }
}

class Point
{
    public int X { get; set; }
    public int Y { get; set; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static float GetDistance(Point point1, Point point2)
    {
        float x = Mathf.Pow(point2.X - point1.X, 2);
        float y = Mathf.Pow(point2.Y - point1.Y, 2);

        return Mathf.Sqrt(x + y);
    }
}
