﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField] private WheelCollider[] _wheelsColliders;
    [SerializeField] private Transform[] _wheelsTransforms;
    [SerializeField] [Range(0, 100)] private float _maxSteerAngle = 30;
    [SerializeField] [Range(0, 500)] private float _torque = 50;
    [SerializeField] [Range(0, 5)] private float _torqueToSteerRatio = 2;
    [SerializeField] [Range(0, 5)] private float _minNextMarkerDistance = 1;

    [SerializeField] private RoadPiece[] _PATH;
    [SerializeField] private RoadPiece _START;
    [SerializeField] private RoadPiece _TARGET;
    [SerializeField] private RoadPiece _PATHPOINT;

    private float _horizontalInput;
    private float _verticalInput;
    private float _steeringAngle;
    private int _markerIndex;
    private RoadPiece _target;
    private float _torqueWeight;
    private bool _isInitialised;

    private RoadPiece _currentRoad;
    private List<RoadPiece> _targets;
    private List<RoadPiece> _path;
    private int _pathPointIndex;
    private Transform _pathPoint;

    private void Awake()
    {
        _markerIndex = 0;
        _isInitialised = false;
        _targets = new List<RoadPiece>();
    }

    public void Initialise(GameObject[,] _roadMapObjects, int width, int length)
    {
        GetTargets(_roadMapObjects, width, length);

        _isInitialised = true;
    }

    private void GetTargets(GameObject[,] _roadMapObjects, int width, int length)
    {
        for (int row = 0; row < length; row++)
        {
            for (int column = 0; column < width; column++)
            {
                if (_roadMapObjects[row, column] == null) continue;

                RoadPiece roadPiece = _roadMapObjects[row, column].GetComponent<RoadPiece>();

                if (_currentRoad == null) _currentRoad = roadPiece;

                if (!roadPiece.TailConnected) _targets.Add(roadPiece);
            }
        }

        FindPath();
    }

    private void FindPath()
    {
        GetNextTarget();

        var _currentPieceToStart = new List<RoadPiece>();
        var _targetToStart = new List<RoadPiece>();
        _pathPointIndex = 0;

        var dummy = _currentRoad.gameObject;

        while (dummy != null)
        {
            var dummyPiece = dummy.GetComponent<RoadPiece>();

            _currentPieceToStart.Add(dummyPiece);

            dummy = dummyPiece.HeadConnection;
        }

        dummy = _target.gameObject;

        while (dummy != null)
        {
            var dummyPiece = dummy.GetComponent<RoadPiece>();

            _targetToStart.Add(dummyPiece);

            dummy = dummyPiece.HeadConnection;
        }

        int intersectionCurrent = -1;
        int intersectionTarget = -1;

        for (int i = 0; i < _currentPieceToStart.Count; i++)
        {
            for (int k = 0; k <_targetToStart.Count; k++)
            {
                if (_currentPieceToStart[i] == _targetToStart[k])
                {
                    intersectionCurrent = i;
                    intersectionTarget = k;

                    break;
                }
            }

            if (intersectionCurrent != -1) break;
        }

        _path = new List<RoadPiece>();

        for (int i = 0; i < intersectionCurrent - 1; i++)
            _path.Add(_currentPieceToStart[i]);

        for (int i = intersectionTarget - 1; i >= 0; i--)
            _path.Add(_targetToStart[i]);

        _PATH = _path.ToArray();
        _START = _currentRoad;
        _TARGET = _target;

        GetNextPathPoint();
    }

    private void Update()
    {
        if (_isInitialised)
        {
            if (Vector3.Distance(transform.position, _pathPoint.transform.position) <= _minNextMarkerDistance)
                GetNextPathPoint();
        }
    }

    private void FixedUpdate()
    {
        if (_isInitialised)
        {
            Steer();
            Accelerate();
            UpdateWheelPositionAndRotation();
        }
    }

    private void GetNextTarget()
    {
        if (_markerIndex == _targets.Count)
            _markerIndex = 0;

        _target = _targets[_markerIndex];

        _markerIndex++;
    }

    private void GetNextPathPoint()
    {
        if (_path.Count == 0)
        {
            Debug.LogError("Car pathining failed...");
            return;
        }

        if (_pathPointIndex == _path.Count)
        {
            FindPath();
            return;
        }

        _PATHPOINT = _path[_pathPointIndex];
        _pathPoint = _path[_pathPointIndex].transform;
        _currentRoad = _pathPoint.GetComponent<RoadPiece>();
        _pathPointIndex++;
    }

    private void Steer()
    {

        _steeringAngle = _maxSteerAngle * GetSteeringAngle();

        for (int i = 0; i < 2; i++)
            _wheelsColliders[i].steerAngle = _steeringAngle;
    }

    private void Accelerate()
    {
        for (int i = 0; i < 2; i++)
            _wheelsColliders[i].motorTorque = _torque * (1 - _torqueWeight / _torqueToSteerRatio);
    }

    private void UpdateWheelPositionAndRotation()
    {
        for (int i = 0; i < _wheelsTransforms.Length; i++)
        {
            var position = _wheelsTransforms[i].position;
            var rotation = _wheelsTransforms[i].rotation;

            _wheelsColliders[i].GetWorldPose(out position, out rotation);

            _wheelsTransforms[i].position = position;
            _wheelsTransforms[i].rotation = rotation;
        }
    }

    private float GetSteeringAngle()
    {
        if (_pathPoint == null) return 0;

        var direction = _pathPoint.transform.position - transform.position;
        float angle = Vector3.Angle(direction, transform.forward);
        angle = Mathf.Clamp(angle / _maxSteerAngle, 0, 1);

        _torqueWeight = angle;

        bool isLeft = IsLeft(direction);

        if (isLeft) angle *= -1;

        return angle;
    }

    private bool IsLeft(Vector3 targetDirection)
    {
        var perpendicular = Vector3.Cross(transform.forward, targetDirection);
        var direction = Vector3.Dot(perpendicular, transform.up);

        if (direction < 0)
            return true;

        return false;
    }
}