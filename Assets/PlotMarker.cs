using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlotMarker : MonoBehaviour
{

    [Header("Debug", order = 1)]
    [SerializeField] private bool _showRayCasts;

    private RoadPiece _road;
    private PlotMarker _leftConnection;
    private PlotMarker _rightConnection;
    private bool _isInitialised;
    private bool _isLeftPlotMarker;

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
    }

    private void Update()
    {
        if (_isInitialised)
        {
            if (CanMakeInitialConnections())
                MakeInitialConnections();
            else if (_road.HeadConnection != null && !_road.HeadConnection.GetComponent<RoadPiece>().CanMakePlots)
                MakeIntersectionConnections();
        }

        if (_showRayCasts)
            DrawDebugRays();
    }

    private void MakeInitialConnections()
    {
        var roadBefore = _road.HeadConnection.GetComponent<RoadPiece>();
        var roadAhead = _road.TailConnection.GetComponent<RoadPiece>();

        if (_isLeftPlotMarker)
        {
            _leftConnection = roadAhead.LeftPlotMarker;
            _rightConnection = roadBefore.LeftPlotMarker;
        }
        else
        {
            _leftConnection = roadBefore.RightPlotMarker;
            _rightConnection = roadAhead.RightPlotMarker;
        }

        ShowConnectionRays();
    }

    private void MakeIntersectionConnections()
    {
        var intersection = _road.HeadConnection.GetComponent<RoadPiece>();

        while (intersection != null && !intersection.CanMakePlots)
            intersection = intersection.HeadConnection.GetComponent<RoadPiece>();

        if (intersection == null) return;

        if (_isLeftPlotMarker)
        {
            if (IsValidConnection(intersection.LeftPlotMarker.transform.position))
                _rightConnection = intersection.LeftPlotMarker;
        }
        else
        {
            if (IsValidConnection(intersection.RightPlotMarker.transform.position))
                _leftConnection = intersection.RightPlotMarker;
        }

        ShowConnectionRays();
    }

    private bool IsValidConnection(Vector3 possibleConnectionPosition)
    {
        if (Physics.Raycast(transform.position, possibleConnectionPosition - transform.position, Vector3.Distance(transform.position, possibleConnectionPosition)))
            return false;

        return true;
    }

    private bool CanMakeInitialConnections()
    {
        if (!_road.HeadConnected || !_road.TailConnected) return false;
        if (!_road.HeadConnection.GetComponent<RoadPiece>().CanMakePlots || !_road.TailConnection.GetComponent<RoadPiece>().CanMakePlots) return false;

        return true;
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
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * 10, Color.green);
            else
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * 10, Color.green);
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
