using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCar : MonoBehaviour
{
    [SerializeField] private bool _useKeyboardInput;
    [SerializeField] private bool _useTestMarkers;
    [SerializeField] private WheelCollider[] _wheelsColliders;
    [SerializeField] private Transform[] _wheelsTransforms;
    [SerializeField] [Range(0, 100)] private float _maxSteerAngle = 30;
    [SerializeField] [Range(0, 500)] private float _torque = 50;
    [SerializeField] [Range(0, 5)] private float _torqueToSteerRatio = 2;
    [SerializeField] [Range(0, 5)] private float _minNextMarkerDistance = 1;
    [SerializeField] private Transform[] _testMarkers;

    private float _horizontalInput;
    private float _verticalInput;
    private float _steeringAngle;
    private int _markerIndex;
    private Transform _target;
    private float _torqueWeight;

    private void Awake()
    {
        _markerIndex = 0;

        if (_useTestMarkers)
            GetNextMarkerTransform();
    }

    private void Update()
    {
        if (_useTestMarkers)
        {
            if (_target == null)
                GetNextMarkerTransform();

            if (Vector3.Distance(transform.position, _target.position) <= _minNextMarkerDistance)
                GetNextMarkerTransform();
        }
    }

    private void FixedUpdate()
    {
        if (_useKeyboardInput)
            GetKeyboardInput();

        Steer();
        Accelerate();
        UpdateWheelPositionAndRotation();
    }

    private void GetKeyboardInput()
    {
        _horizontalInput = Input.GetAxis("Horizontal");
        _verticalInput = Input.GetAxis("Vertical");
    }

    private void GetNextMarkerTransform()
    {
        if (_markerIndex == _testMarkers.Length)
            _markerIndex = 0;

        _target = _testMarkers[_markerIndex];

        _markerIndex++;
    }

    private void Steer()
    {
        if (_useKeyboardInput)
            _steeringAngle = _maxSteerAngle * _horizontalInput;
        else if (_useTestMarkers)
            _steeringAngle = _maxSteerAngle * GetSteeringAngle();

        for (int i = 0; i < 2; i++)
            _wheelsColliders[i].steerAngle = _steeringAngle;
    }

    private void Accelerate()
    {
        for (int i = 0; i < 2; i++)
        {
            if (_useKeyboardInput)
                _wheelsColliders[i].motorTorque = _torque * _verticalInput;
            else if (_useTestMarkers)
                _wheelsColliders[i].motorTorque = _torque * (1 - _torqueWeight / _torqueToSteerRatio);
        }
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
        if (_target == null) return 0;

        var direction = _target.position - transform.position;
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
