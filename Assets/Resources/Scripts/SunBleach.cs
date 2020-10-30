using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunBleach : MonoBehaviour
{
    [SerializeField] [ColorUsage(true, true)] private Color _bleachedColour;

    [Header("Bleached Faces", order = 0)]
    [SerializeField] private bool _top = false; // 0
    [SerializeField] private bool _bottom = false; // 3
    [SerializeField] private bool _left = false; // 1
    [SerializeField] private bool _right = false; // 5
    [SerializeField] private bool _front = false; // 4
    [SerializeField] private bool _back = false; // 2

    private Material[] _materials;
    private int _shaderLightnessId;

    private void Awake()
    {
        _materials = GetComponent<Renderer>().materials;
    }

    private void Start()
    {
        BleachFaces();
    }

    private void BleachFaces()
    {
        if (_top)
            BleachFace(0);

        if (_bottom)
            BleachFace(3);

        if (_left)
            BleachFace(1);

        if (_right)
            BleachFace(5);

        if (_front)
            BleachFace(4);

        if (_back)
            BleachFace(2);
    }

    private void BleachFace(int index)
    {
        var mat = _materials[index];

        mat.SetColor("DefaultColour", _bleachedColour);
    }

    public void SetFacesAndBleach(bool top = false, bool bottom = false, bool left = false, bool right = false, bool front = false, bool back = false)
    {
        _top = top;
        _bottom = bottom;
        _left = left;
        _right = right;
        _front = front;
        _back = back;

        BleachFaces();
    }
}
