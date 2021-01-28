using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlotMarker : MonoBehaviour
{
    public GameObject ROADCOLLISION;
    public bool CHECKAGAIN = false;
    public GameObject INTERSECTION_PARENT;
    public RoadPiece[] VALID_INTERSECTING_PIECES; 
    public List<RoadPiece> VALID_INTERSECTING_PIECES_CHECKED = new List<RoadPiece>();
    public List<PlotMarker> SEARCHED_PIECES = new List<PlotMarker>();
    public List<PlotMarker> SEARCHED_VALID_LEFT = new List<PlotMarker>();
    public List<PlotMarker> SEARCHED_VALID_RIGHT = new List<PlotMarker>();
    public List<GameObject> SEARCHED_COLLISIONS = new List<GameObject>();
    public GameObject ERROR_PIECE;

    [Header("Plot Connections")]
    [SerializeField] private LayerMask _plotConnectionMask;
    [SerializeField] [Range(0, 500)] private float _maxConnectionDistance;

    [Header("Debug", order = 1)]
    [SerializeField] private bool _showRayCasts;

    public bool IsLeftCycle { get; set; }
    public PlotMarker LeftConnection { get; set; }
    public PlotMarker RightConnection { get; set; }
    public PlotMarker ForwardConnection { get; set; }
    public bool IsParcelMarker { get; set; }

    public RoadPiece Road { get; set; }

    private bool _isInitialised;
    private bool _isEnd;
    private bool _isLeftPlotMarker;
    private Vector3 _forwardDirection;
    private bool _triedToMakePlots;

    private bool _showConnectionRays;

    private List<List<PlotMarker>> _plotContainers;

    private RoadMapCreator _roadMapCreator;
    private bool _finishedMakingConnections;

    private void Awake()
    {
        _showRayCasts = false;
        _triedToMakePlots = false;
        _finishedMakingConnections = false;
    }

    public void Initialise(RoadPiece road, bool isLeftPlotMarker, List<List<PlotMarker>> plotContainers, RoadMapCreator roadMapCreator, bool isEnd = false)
    {
        Road = road;
        _isLeftPlotMarker = isLeftPlotMarker;
        _plotContainers = plotContainers;
        _roadMapCreator = roadMapCreator;
        _isInitialised = true;
        _isEnd = isEnd;

        if (isLeftPlotMarker && !isEnd)
            _forwardDirection = transform.TransformDirection(Vector3.right);
        else
            _forwardDirection = transform.TransformDirection(Vector3.left);

        ShowConnectionRays();
    }

    private void Update()
    {
        if (_isInitialised && !_finishedMakingConnections)
        {
            if (CanMakeInitialConnections())
                MakeInitialConnections();
            //else if (Road.HeadConnection != null && !Road.HeadConnection.GetComponent<RoadPiece>().CanMakePlots)
                //MakeIntersectionConnections();
            
            if (_isEnd)
                MakeEndConnection();

            if (StillHasConnectionsToMake())
                MakeClosestValidConnection(SearchForNewConnections(Road, true));

            //if (!IsEndPiece() && StillHasConnectionsToMake())
                //TrySearchForNewConnections();

        }

        if (_showRayCasts)
            DrawDebugRays();
    }

    private void LateUpdate()
    {
        if (_isInitialised)
        {
            if (CHECKAGAIN)
            {
                CHECKAGAIN = false;
                _triedToMakePlots = false;
            }

            if (!_triedToMakePlots && !StillHasConnectionsToMake() && !IsNextToPlot())
            {
                TryMakeFinalPlotConnection();
                _triedToMakePlots = true;
            }

            if (!_finishedMakingConnections)
            {
                _roadMapCreator.PlotsToTryToMakeConnections--;
                _finishedMakingConnections = true;
            }
        }
    }

    private void MakeInitialConnections()
    {
        var roadBefore = Road.HeadConnection.GetComponent<RoadPiece>();
        var roadAhead = Road.TailConnection.GetComponent<RoadPiece>();

        if (_isLeftPlotMarker)
        {
            LeftConnection = roadAhead.LeftPlotMarker;
            RightConnection = roadBefore.LeftPlotMarker;

            roadAhead.LeftPlotMarker.RightConnection = Road.LeftPlotMarker;
            roadBefore.LeftPlotMarker.LeftConnection = Road.LeftPlotMarker;
        }
        else
        {
            LeftConnection = roadBefore.RightPlotMarker;
            RightConnection = roadAhead.RightPlotMarker;

            roadBefore.RightPlotMarker.RightConnection = Road.RightPlotMarker;
            roadAhead.RightPlotMarker.LeftConnection = Road.RightPlotMarker;
        }

        ShowConnectionRays();
    }

    private void MakeIntersectionConnections()
    {
        var intersection = Road.HeadConnection.GetComponent<RoadPiece>();
        var head = intersection;

        while (intersection != null && intersection.IntersectingRoads.Count <= 1)
            intersection = intersection.HeadConnection.GetComponent<RoadPiece>();

        if (intersection == null) return;

        INTERSECTION_PARENT = intersection.gameObject;

        var validIntersectingPiece = new List<RoadPiece>();

        foreach (var intersectingPiece in intersection.IntersectingRoads)
        {
            var roadPiece = intersectingPiece.GetComponent<RoadPiece>();

            if (roadPiece != head) validIntersectingPiece.Add(roadPiece);
        }

        VALID_INTERSECTING_PIECES = validIntersectingPiece.ToArray();

        foreach (var intersectingPiece in validIntersectingPiece)
        {
            var dummy = intersectingPiece.gameObject;

            while (dummy != null)
            {
                var piece = dummy.GetComponent<RoadPiece>();

                if (piece.CanMakePlots) break;

                dummy = piece.TailConnection;
            }

            if (dummy == null) continue;

            var possibleConnection = dummy.GetComponent<RoadPiece>();

            VALID_INTERSECTING_PIECES_CHECKED.Add(possibleConnection);

            if (_isLeftPlotMarker)
            {
                if (IsValidConnection(possibleConnection))
                {
                    RightConnection = possibleConnection.LeftPlotMarker;
                    possibleConnection.LeftPlotMarker.LeftConnection = Road.LeftPlotMarker;

                    break;
                }
                else if (IsValidConnection(possibleConnection, true))
                {
                    RightConnection = possibleConnection.RightPlotMarker;
                    possibleConnection.RightPlotMarker.LeftConnection = Road.LeftPlotMarker;

                    break;
                }
            }
            else
            {
                if (IsValidConnection(possibleConnection))
                {
                    LeftConnection = possibleConnection.RightPlotMarker;
                    possibleConnection.RightPlotMarker.RightConnection = Road.RightPlotMarker;

                    break;
                }
                else if (IsValidConnection(possibleConnection, true))
                {
                    LeftConnection = possibleConnection.LeftPlotMarker;
                    possibleConnection.LeftPlotMarker.RightConnection = Road.RightPlotMarker;

                    break;
                }
            }
        }

        //var intersection = Road.HeadConnection.GetComponent<RoadPiece>();

        //while (intersection != null && intersection.HeadConnection != null && !intersection.CanMakePlots)
        //    intersection = intersection.HeadConnection.GetComponent<RoadPiece>();

        //if (intersection == null || !intersection.CanMakePlots) return;

        //RoadPiece intersectionTailTail = intersection.TailConnection.GetComponent<RoadPiece>();

        //while (intersectionTailTail != null && intersectionTailTail.TailConnection != null && !intersectionTailTail.CanMakePlots)
        //    intersectionTailTail = intersectionTailTail.TailConnection.GetComponent<RoadPiece>();

        //if (_isLeftPlotMarker)
        //{
        //    if (IsValidConnection(intersection))
        //    {
        //        RightConnection = intersection.LeftPlotMarker;
        //        intersection.LeftPlotMarker.LeftConnection = Road.LeftPlotMarker;
        //    }
        //    else if (intersectionTailTail != null && intersectionTailTail.CanMakePlots && intersectionTailTail != Road)
        //    {
        //        RightConnection = intersectionTailTail.RightPlotMarker;
        //        intersectionTailTail.RightPlotMarker.LeftConnection = Road.LeftPlotMarker;
        //    }
        //}
        //else
        //{
        //    if (IsValidConnection(intersection))
        //    {
        //        LeftConnection = intersection.RightPlotMarker;
        //        intersection.RightPlotMarker.RightConnection = Road.RightPlotMarker;
        //    }
        //    else if (intersectionTailTail != null && intersectionTailTail.CanMakePlots && intersectionTailTail != Road)
        //    {
        //        LeftConnection = intersectionTailTail.LeftPlotMarker;
        //        intersectionTailTail.LeftPlotMarker.RightConnection = Road.RightPlotMarker;
        //    }
        //}

        ShowConnectionRays();
    }

    private void MakeEndConnection()
    {
        if (_isLeftPlotMarker)
            LeftConnection = Road.BottomRightPlotMarker;
        else
            RightConnection = Road.BottomLeftPlotMarker;

        if (Road.CanMakePlots)
        {
            if (_isLeftPlotMarker)
            {
                RightConnection = Road.LeftPlotMarker;
                Road.LeftPlotMarker.LeftConnection = Road.BottomLeftPlotMarker;
            }
            else
            {
                LeftConnection = Road.RightPlotMarker;
                Road.RightPlotMarker.RightConnection = Road.BottomRightPlotMarker;
            }
        }

        ShowConnectionRays();
    }

    private List<PlotMarker> SearchForNewConnections(RoadPiece start, bool up)
    {
        var possibleConnections = new List<PlotMarker>();

        GameObject dummy;

        if (up)
            dummy = start.HeadConnection;
        else
            dummy = start.gameObject; // Down may be end or another intersection immediately

        RoadPiece dummyPreviousPiece = null;

        while (dummy != null)
        {
            var dummyPiece = dummy.GetComponent<RoadPiece>();

            if (dummyPiece.IsIntersection)
            {
                foreach (var intersectionPiece in dummyPiece.IntersectingRoads)
                {
                    if (dummyPreviousPiece == null && intersectionPiece == Road.gameObject) continue;
                    if (dummyPreviousPiece == intersectionPiece.GetComponent<RoadPiece>()) continue;

                    possibleConnections.AddRange(SearchForNewConnections(intersectionPiece.GetComponent<RoadPiece>(), false));
                }

                if (!up) // Handling duplicate comparison with concurrent intersections
                    break;
            }

            if (dummyPiece.CanMakePlots)
            {
                possibleConnections.Add(dummyPiece.LeftPlotMarker);
                possibleConnections.Add(dummyPiece.RightPlotMarker);

                break;
            }

            if (!dummyPiece.TailConnected)
            {
                possibleConnections.Add(dummyPiece.BottomLeftPlotMarker);
                possibleConnections.Add(dummyPiece.BottomRightPlotMarker);

                break;
            }

            dummyPreviousPiece = dummyPiece;

            if (up)
                dummy = dummyPiece.HeadConnection;
            else
                dummy = dummyPiece.TailConnection;
        }

        return possibleConnections;
    }

    private void MakeClosestValidConnection(List<PlotMarker> possibleConnections)
    {
        SEARCHED_PIECES = possibleConnections;

        var leftPossibleConnections = new List<PlotMarker>();
        var rightPossibleConnections = new List<PlotMarker>();

        foreach (var marker in possibleConnections)
        {
            if (!IsValidConnection(this, marker)) continue;

            if (IsLeft(marker.transform.position - transform.position))
                leftPossibleConnections.Add(marker);
            else
                rightPossibleConnections.Add(marker);
        }

        if (LeftConnection == null)
        {
            int closestIndex = -1;
            float shortestDistance = float.MaxValue;

            for (int i = 0; i < leftPossibleConnections.Count; i++)
            {
                var distance = Vector3.Distance(transform.position, leftPossibleConnections[i].transform.position);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestIndex = i;
                }

                if (closestIndex != -1)
                {
                    LeftConnection = leftPossibleConnections[closestIndex];
                    leftPossibleConnections[closestIndex].RightConnection = this;
                }
                else
                    Debug.LogWarning("No valid left connection found!");
            }
        }

        if (RightConnection == null)
        {
            int closestIndex = -1;
            float shortestDistance = float.MaxValue;

            for (int i = 0; i < rightPossibleConnections.Count; i++)
            {
                var distance = Vector3.Distance(transform.position, rightPossibleConnections[i].transform.position);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestIndex = i;
                }

                if (closestIndex != -1)
                {
                    RightConnection = rightPossibleConnections[closestIndex];
                    rightPossibleConnections[closestIndex].LeftConnection = this;
                }
                else
                    Debug.LogWarning("No valid right connection found!");
            }
        }

        SEARCHED_VALID_LEFT = leftPossibleConnections;
        SEARCHED_VALID_RIGHT = rightPossibleConnections;
    }

    private void TrySearchForNewConnections()
    {
        var roadBefore = Road.HeadConnection.GetComponent<RoadPiece>();

        while (roadBefore != null && roadBefore.HeadConnection != null && !roadBefore.CanMakePlots)
            roadBefore = roadBefore.HeadConnection.GetComponent<RoadPiece>();

        var roadAhead = Road.TailConnection.GetComponent<RoadPiece>();

        while (roadAhead != null && roadAhead.TailConnection != null && !roadAhead.CanMakePlots)
            roadAhead = roadAhead.TailConnection.GetComponent<RoadPiece>();

        if (_isLeftPlotMarker)
        {
            if (LeftConnection == null)
            {
                if (IsValidConnection(this, roadAhead.LeftPlotMarker))
                {
                    LeftConnection = roadAhead.LeftPlotMarker;
                    roadAhead.LeftPlotMarker.RightConnection = Road.LeftPlotMarker;
                }
            }

            if (RightConnection == null)
            {
                if (IsValidConnection(this, roadBefore.LeftPlotMarker))
                {
                    RightConnection = roadBefore.LeftPlotMarker;
                    roadBefore.LeftPlotMarker.LeftConnection = Road.LeftPlotMarker;
                }
            }
        }
        else
        {
            if (LeftConnection == null)
            {
                if (IsValidConnection(this, roadBefore.RightPlotMarker))
                {
                    LeftConnection = roadBefore.RightPlotMarker;
                    roadBefore.RightPlotMarker.RightConnection = Road.RightPlotMarker;
                }
            }

            if (RightConnection == null)
            {
                if (IsValidConnection(this, roadAhead.RightPlotMarker))
                {
                    RightConnection = roadAhead.RightPlotMarker;
                    roadAhead.RightPlotMarker.LeftConnection = Road.RightPlotMarker;
                }
            }
        }
    }

    private void TryMakeFinalPlotConnection()
    {
        RaycastHit hit;
        RoadPiece roadHit = null;

        if (Physics.Raycast(transform.position, _forwardDirection , out hit, _maxConnectionDistance, _plotConnectionMask))
        {
            roadHit = hit.transform.root.GetComponent<RoadPiece>();
        }

        if (roadHit == null || !roadHit.CanMakePlots) return;

        ROADCOLLISION = roadHit.gameObject;

        PlotMarker marker;

        if (IsCollisionOnLeftSide(hit.point, roadHit))
            marker = roadHit.LeftPlotMarker;
        else
            marker = roadHit.RightPlotMarker;

        if (marker.ForwardConnection != null) return;

        bool cycleFound = false;
        bool cycleEncountered = false;

        PlotMarker dummy = marker.LeftConnection;

        int comparions = 0;

        while (dummy != null)
        {
            if (comparions == 30)
            {
                var errorPiece = Instantiate(ERROR_PIECE);
                errorPiece.transform.position = dummy.transform.position;
                break;
            }

            if (dummy == this)
            {
                IsLeftCycle = true;
                cycleFound = true;

                break;
            }

            if (dummy.ForwardConnection == null)
                dummy = dummy.LeftConnection;
            else
            {
                if (cycleEncountered) break;
                if (dummy.IsLeftCycle != IsLeftCycle) break;

                dummy = dummy.ForwardConnection.LeftConnection;
                cycleEncountered = true;
            }

            comparions++;
        }

        comparions = 0;

        if (!cycleFound)
        {
            dummy = marker.RightConnection;
            cycleEncountered = false;

            while (dummy != null)
            {
                if (comparions == 30)
                {
                    var errorPiece = Instantiate(ERROR_PIECE);
                    errorPiece.transform.position = dummy.transform.position;
                    break;
                }

                if (dummy == this)
                {
                    cycleFound = true;

                    break;
                }

                if (dummy.ForwardConnection == null)
                    dummy = dummy.RightConnection;
                else
                {
                    if (cycleEncountered) break;
                    if (dummy.IsLeftCycle != IsLeftCycle) break;

                    dummy = dummy.ForwardConnection.RightConnection;
                    cycleEncountered = true;
                }

                comparions++;
            }
        }

        if (!cycleFound) return;

        ForwardConnection = marker;
        marker.ForwardConnection = this;
        marker.IsLeftCycle = !IsLeftCycle;

        var plotContainer = new List<PlotMarker>();
        plotContainer.Add(this);

        _plotContainers.Add(plotContainer);

        // Plot connections stored later
    }

    private bool IsValidConnection(RoadPiece road, bool swap = false)
    {
        if (road == null) return false;
        if (!road.CanMakePlots) return false;

        Vector3 position;

        if (swap)
        {
            if (!_isLeftPlotMarker)
            {
                position = road.LeftPlotMarker.transform.position;
            }
            else
            {
                position = road.RightPlotMarker.transform.position;
            }
        }
        else
        {
            if (_isLeftPlotMarker)
            {
                position = road.LeftPlotMarker.transform.position;
            }
            else
            {
                position = road.RightPlotMarker.transform.position;
            }
        }

        if (Physics.Raycast(transform.position, position - transform.position, Vector3.Distance(transform.position, position)))
            return false;

        return true;
    }

    public bool IsValidConnection(PlotMarker p1, PlotMarker p2)
    {
        RaycastHit hit;

        if (Physics.Raycast(p1.transform.position, p2.transform.position - p1.transform.position, out hit, Vector3.Distance(p1.transform.position, p2.transform.position), _plotConnectionMask))
        {
            SEARCHED_COLLISIONS.Add(hit.transform.gameObject);
            return false;
        }

        return true;
    }

    private bool IsEndPiece()
    {
        return !Road.HeadConnected || !Road.TailConnected;
    }

    private bool StillHasConnectionsToMake()
    {
        return LeftConnection == null || RightConnection == null;
    }

    private bool IsNextToPlot()
    {
        if (LeftConnection != null && LeftConnection.ForwardConnection != null) return true;
        if (RightConnection != null && RightConnection.ForwardConnection != null) return true;
        if (ForwardConnection != null) return true;

        return false;
    }

    private bool IsLeft(Vector3 targetDirection)
    {
        var perpendicular = Vector3.Cross(_forwardDirection, targetDirection);
        var direction = Vector3.Dot(perpendicular, -transform.forward);

        if (direction < 0)
            return true;

        return false;
    }

    private bool CanMakeInitialConnections()
    {
        if (!Road.HeadConnected || !Road.TailConnected) return false;
        if (!Road.HeadConnection.GetComponent<RoadPiece>().CanMakePlots || !Road.TailConnection.GetComponent<RoadPiece>().CanMakePlots) return false;

        return true;
    }

    private bool IsCollisionOnLeftSide(Vector3 hitPosition, RoadPiece road)
    {
        if (Vector3.Distance(hitPosition, road.LeftPlotMarker.transform.position) <= Vector3.Distance(hitPosition, road.RightPlotMarker.transform.position)) return true;

        return false;
    }

    private void DrawDebugRays()
    {
        if (_showConnectionRays)
        {
            if (LeftConnection != null)
                Debug.DrawRay(transform.position, Vector3.Normalize(LeftConnection.transform.position - transform.position) * Vector3.Distance(transform.position, LeftConnection.transform.position), Color.blue);

            if (RightConnection != null)
                Debug.DrawRay(transform.position, Vector3.Normalize(RightConnection.transform.position - transform.position) * Vector3.Distance(transform.position, RightConnection.transform.position), Color.blue);

            if (_isLeftPlotMarker)
                Debug.DrawRay(transform.position, _forwardDirection * 10, Color.green);
            else
                Debug.DrawRay(transform.position, _forwardDirection * 10, Color.green);

            if (ForwardConnection != null)
                Debug.DrawRay(transform.position, Vector3.Normalize(ForwardConnection.transform.position - transform.position) * Vector3.Distance(transform.position, ForwardConnection.transform.position), Color.magenta);
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * 500, Color.red);
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * 500, Color.red);
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.up) * 500, Color.red);
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * 500, Color.red);
        }
    }

    public void ShowConnectionRays()
    {
        _showConnectionRays = true;

        _showRayCasts = true; // TODO: Remove this
    }
}
