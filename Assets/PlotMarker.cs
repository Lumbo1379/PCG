﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlotMarker : MonoBehaviour
{
    public GameObject ROADCOLLISION;

    [Header("Plot Connections")]
    [SerializeField] private LayerMask _plotConnectionMask;
    [SerializeField] [Range(0, 500)] private float _maxConnectionDistance;

    [Header("Debug", order = 1)]
    [SerializeField] private bool _showRayCasts;

    public bool ContainedInPlot { get; set; } = false;

    private RoadPiece _road;
    private PlotMarker _leftConnection;
    private PlotMarker _rightConnection;
    private PlotMarker _plotConnection;
    private bool _isInitialised;
    private bool _isLeftPlotMarker;
    private Vector3 _forwardDirection;

    private bool _showConnectionRays;

    private void Awake()
    {
        _showRayCasts = false;        
    }

    public void Initialise(RoadPiece road, bool isLeftPlotMarker)
    {
        _road = road;
        _isLeftPlotMarker = isLeftPlotMarker;
        _isInitialised = true;

        if (isLeftPlotMarker)
            _forwardDirection = transform.TransformDirection(Vector3.right);
        else
            _forwardDirection = transform.TransformDirection(Vector3.left);

        ShowConnectionRays();
    }

    private void Update()
    {
        if (_isInitialised)
        {
            if (CanMakeInitialConnections())
                MakeInitialConnections();
            else if (_road.HeadConnection != null && !_road.HeadConnection.GetComponent<RoadPiece>().CanMakePlots)
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
            if (!StillHasConnectionsToMake() && !ContainedInPlot)
                TryMakeFinalPlotConnection();
        }
    }

    private void MakeInitialConnections()
    {
        var roadBefore = _road.HeadConnection.GetComponent<RoadPiece>();
        var roadAhead = _road.TailConnection.GetComponent<RoadPiece>();

        if (_isLeftPlotMarker)
        {
            _leftConnection = roadAhead.LeftPlotMarker;
            _rightConnection = roadBefore.LeftPlotMarker;

            roadAhead.LeftPlotMarker._rightConnection = _road.LeftPlotMarker;
            roadBefore.LeftPlotMarker._leftConnection = _road.LeftPlotMarker;
        }
        else
        {
            _leftConnection = roadBefore.RightPlotMarker;
            _rightConnection = roadAhead.RightPlotMarker;

            roadBefore.RightPlotMarker._rightConnection = _road.RightPlotMarker;
            roadAhead.RightPlotMarker._leftConnection = _road.RightPlotMarker;
        }

        ShowConnectionRays();
    }

    private void MakeIntersectionConnections()
    {
        var intersection = _road.HeadConnection.GetComponent<RoadPiece>();

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
                _rightConnection = intersection.LeftPlotMarker;
                intersection.LeftPlotMarker._leftConnection = _road.LeftPlotMarker;
            }
            else if (intersectionTailTail != null && intersectionTailTail.CanMakePlots && intersectionTailTail != _road)
            {
                _rightConnection = intersectionTailTail.RightPlotMarker;
                intersectionTailTail.RightPlotMarker._leftConnection = _road.LeftPlotMarker;
            }
        }
        else
        {
            if (IsValidConnection(intersection))
            {
                _leftConnection = intersection.RightPlotMarker;
                intersection.RightPlotMarker._rightConnection = _road.RightPlotMarker;
            }
            else if (intersectionTailTail != null && intersectionTailTail.CanMakePlots && intersectionTailTail != _road)
            {
                _leftConnection = intersectionTailTail.LeftPlotMarker;
                intersectionTailTail.LeftPlotMarker._rightConnection = _road.RightPlotMarker;
            }
        }

        ShowConnectionRays();
    }

    private void TrySearchForNewConnections()
    {
        var roadBefore = _road.HeadConnection.GetComponent<RoadPiece>();

        while (roadBefore != null && roadBefore.HeadConnection != null && !roadBefore.CanMakePlots)
            roadBefore = roadBefore.HeadConnection.GetComponent<RoadPiece>();

        var roadAhead = _road.TailConnection.GetComponent<RoadPiece>();

        while (roadAhead != null && roadAhead.TailConnection != null && !roadAhead.CanMakePlots)
            roadAhead = roadAhead.TailConnection.GetComponent<RoadPiece>();

        if (_isLeftPlotMarker)
        {
            if (_leftConnection == null)
            {
                if (IsValidConnection(roadAhead))
                {
                    _leftConnection = roadAhead.LeftPlotMarker;
                    roadAhead.LeftPlotMarker._rightConnection = _road.LeftPlotMarker;
                }
            }

            if (_rightConnection == null)
            {
                if (IsValidConnection(roadBefore))
                {
                    _rightConnection = roadBefore.LeftPlotMarker;
                    roadBefore.LeftPlotMarker._leftConnection = _road.LeftPlotMarker;
                }
            }
        }
        else
        {
            if (_leftConnection == null)
            {
                if (IsValidConnection(roadBefore))
                {
                    _leftConnection = roadBefore.RightPlotMarker;
                    roadBefore.RightPlotMarker._rightConnection = _road.RightPlotMarker;
                }
            }

            if (_rightConnection == null)
            {
                if (IsValidConnection(roadAhead))
                {
                    _rightConnection = roadAhead.RightPlotMarker;
                    roadAhead.RightPlotMarker._leftConnection = _road.RightPlotMarker;
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

        bool cycleFound = false;
        bool leftCycle = false;
        bool cycleEncountered = false;

        PlotMarker dummy = marker._leftConnection;

        while (dummy != null)
        {
            if (dummy == this)
            {
                leftCycle = true;
                cycleFound = true;

                break;
            }

            if (dummy._plotConnection == null)
                dummy = dummy._leftConnection;
            else
            {
                if (cycleEncountered) break;

                dummy = dummy._plotConnection._leftConnection;
                cycleEncountered = true;
            }

            if (dummy != null && dummy._plotConnection == null && dummy.ContainedInPlot) break;
        }
        
        if (!cycleFound)
        {
            dummy = marker._rightConnection;
            cycleEncountered = false;

            while (dummy != null)
            {
                if (dummy == this)
                {
                    cycleFound = true;

                    break;
                }

                if (dummy._plotConnection == null)
                    dummy = dummy._rightConnection;
                else
                {
                    if (cycleEncountered) break;

                    dummy = dummy._plotConnection._rightConnection;
                    cycleEncountered = true;
                }

                if (dummy != null && dummy._plotConnection == null && dummy.ContainedInPlot) return;
            }
        }

        if (!cycleFound) return;

        dummy = marker;

        while (dummy != this)
        {
            dummy.ContainedInPlot = true;

            if (leftCycle)
                dummy = dummy._leftConnection;
            else
                dummy = dummy._rightConnection;
        }

        ContainedInPlot = true;
        _plotConnection = marker;
        marker._plotConnection = this;
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
        return !_road.HeadConnected || !_road.TailConnected;
    }

    private bool StillHasConnectionsToMake()
    {
        return _leftConnection == null || _rightConnection == null;
    }

    private bool CanMakeInitialConnections()
    {
        if (!_road.HeadConnected || !_road.TailConnected) return false;
        if (!_road.HeadConnection.GetComponent<RoadPiece>().CanMakePlots || !_road.TailConnection.GetComponent<RoadPiece>().CanMakePlots) return false;

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
            if (_leftConnection != null)
                Debug.DrawRay(transform.position, Vector3.Normalize(_leftConnection.transform.position - transform.position) * Vector3.Distance(transform.position, _leftConnection.transform.position), Color.blue);

            if (_rightConnection != null)
                Debug.DrawRay(transform.position, Vector3.Normalize(_rightConnection.transform.position - transform.position) * Vector3.Distance(transform.position, _rightConnection.transform.position), Color.blue);

            if (_isLeftPlotMarker)
                Debug.DrawRay(transform.position, _forwardDirection * 10, Color.green);
            else
                Debug.DrawRay(transform.position, _forwardDirection * 10, Color.green);

            if (_plotConnection != null)
                Debug.DrawRay(transform.position, Vector3.Normalize(_plotConnection.transform.position - transform.position) * Vector3.Distance(transform.position, _plotConnection.transform.position), Color.magenta);
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * 500, Color.red);
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * 500, Color.red);
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.up) * 500, Color.red);
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * 500, Color.red);
        }
    }

    private void ShowConnectionRays()
    {
        _showConnectionRays = true;

        _showRayCasts = true; // TODO: Remove this
    }
}
