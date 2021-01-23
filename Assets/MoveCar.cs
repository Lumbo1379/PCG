using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCar : MonoBehaviour
{
    [SerializeField] private bool _useKeyboardInput;
    [SerializeField] private WheelCollider[] _wheelsColliders;
    [SerializeField] private Transform[] _wheelsTransforms;
    [SerializeField] [Range(0, 100)] private float _maxSteerAngle = 30;
    [SerializeField] [Range(0, 500)] private float _torque = 50;

    private float _horizontalInput;
    private float _verticalInput;
    private float _steeringAngle;

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

    private void Steer()
    {
        _steeringAngle = _maxSteerAngle * _horizontalInput;

        for (int i = 0; i < 2; i++)
            _wheelsColliders[i].steerAngle = _steeringAngle;
    }

    private void Accelerate()
    {
        for (int i = 0; i < 2; i++)
            _wheelsColliders[i].motorTorque = _torque * _verticalInput;
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
}
