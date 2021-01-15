using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlotMarker : MonoBehaviour
{
    public GameObject ROADCOLLISION;
    public bool CHECKAGAIN = false;

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

    public void Initialise(RoadPiece road, bool isLeftPlotMarker, List<List<PlotMarker>> plotContainers, RoadMapCreator roadMapCreator)
    {
        Road = road;
        _isLeftPlotMarker = isLeftPlotMarker;
        _plotContainers = plotContainers;
        _roadMapCreator = roadMapCreator;
        _isInitialised = true;

        if (isLeftPlotMarker)
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
            else if (Road.HeadConnection != null && !Road.HeadConnection.GetComponent<RoadPiece>().CanMakePlots)
                MakeIntersectionConnections();

            if (!IsEndPiece() && StillHasConnectionsToMake())
                TrySearchForNewConnections();

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

        while (intersection != null && intersection.HeadConnection != null && !intersection.CanMakePlots)
            intersection = intersection.HeadConnection.GetComponent<RoadPiece>();

        if (intersection == null || !intersection.CanMakePlots) return;

        RoadPiece intersectionTailTail = intersection.TailConnection.GetComponent<RoadPiece>();

        while (intersectionTailTail != null && intersectionTailTail.TailConnection != null && !intersectionTailTail.CanMakePlots)
            intersectionTailTail = intersectionTailTail.TailConnection.GetComponent<RoadPiece>();

        if (_isLeftPlotMarker)
        {
            if (IsValidConnection(intersection))
            {
                RightConnection = intersection.LeftPlotMarker;
                intersection.LeftPlotMarker.LeftConnection = Road.LeftPlotMarker;
            }
            else if (intersectionTailTail != null && intersectionTailTail.CanMakePlots && intersectionTailTail != Road)
            {
                RightConnection = intersectionTailTail.RightPlotMarker;
                intersectionTailTail.RightPlotMarker.LeftConnection = Road.LeftPlotMarker;
            }
        }
        else
        {
            if (IsValidConnection(intersection))
            {
                LeftConnection = intersection.RightPlotMarker;
                intersection.RightPlotMarker.RightConnection = Road.RightPlotMarker;
            }
            else if (intersectionTailTail != null && intersectionTailTail.CanMakePlots && intersectionTailTail != Road)
            {
                LeftConnection = intersectionTailTail.LeftPlotMarker;
                intersectionTailTail.LeftPlotMarker.RightConnection = Road.RightPlotMarker;
            }
        }

        ShowConnectionRays();
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
                if (IsValidConnection(roadAhead))
                {
                    LeftConnection = roadAhead.LeftPlotMarker;
                    roadAhead.LeftPlotMarker.RightConnection = Road.LeftPlotMarker;
                }
            }

            if (RightConnection == null)
            {
                if (IsValidConnection(roadBefore))
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
                if (IsValidConnection(roadBefore))
                {
                    LeftConnection = roadBefore.RightPlotMarker;
                    roadBefore.RightPlotMarker.RightConnection = Road.RightPlotMarker;
                }
            }

            if (RightConnection == null)
            {
                if (IsValidConnection(roadAhead))
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

        while (dummy != null)
        {
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
        }
        
        if (!cycleFound)
        {
            dummy = marker.RightConnection;
            cycleEncountered = false;

            while (dummy != null)
            {
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

    private bool IsValidConnection(RoadPiece road)
    {
        if (road == null) return false;
        if (!road.CanMakePlots) return false;

        Vector3 position;

        if (_isLeftPlotMarker)
        {
            position = road.LeftPlotMarker.transform.position;
        }
        else
        {
            position = road.RightPlotMarker.transform.position;
        }

        if (Physics.Raycast(transform.position, position - transform.position, Vector3.Distance(transform.position, position)))
            return false;

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
