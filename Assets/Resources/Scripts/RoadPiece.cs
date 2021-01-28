using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadPiece : MonoBehaviour
{
    [Header("Bounds", order = 0)]
    [SerializeField] private GameObject _topLeft;
    [SerializeField] private GameObject _topRight;
    [SerializeField] private GameObject _bottomLeft;
    [SerializeField] private GameObject _bottomRight;
    [SerializeField] private GameObject _middleLeft;
    [SerializeField] private GameObject _middleRight;
    [SerializeField] private GameObject _bottomLeftMarker;
    [SerializeField] private GameObject _bottomRightMarker;

    [Header("Pieces", order = 1)]
    [SerializeField] private GameObject _head;
    [SerializeField] private GameObject[] _bones;

    public bool CanMakePlots { get; set; }
    public bool IsIntersection { get; set; }
    public float BoneRotation { get; set; }
    public float HeadRotation { get; set; }
    public bool HeadConnected { get; set; }
    public bool TailConnected { get; set; }
    public GameObject HeadConnection { get; set; }
    public GameObject TailConnection { get; set; }
    public PlotMarker LeftPlotMarker { get; set; }
    public PlotMarker RightPlotMarker { get; set; }
    public List<GameObject> IntersectingRoads { get; set; }

    public float IntersectionDecrease
    {
        get 
        {
            IsIntersection = true;
            return _intersectionDecrease += 0.01f; 
        }
        set => _intersectionDecrease = value;
    }

    public GameObject GetTopLeft() { return _topLeft; }
    public GameObject GetTopRight() { return _topRight; }
    public GameObject GetBottomLeft() { return _bottomLeft; }
    public GameObject GetBottomRight() { return _bottomRight; }
    public GameObject GetMiddleLeft() { return _middleLeft; }
    public GameObject GetMiddleRight() { return _middleRight; }
    public GameObject GetBottomLeftMarker() { return _bottomLeftMarker; }
    public GameObject GetBottomRightMarker() { return _bottomRightMarker; }
    public GameObject[] GetBones() { return _bones; }

    private float _intersectionDecrease = 0;

    private void Awake()
    {
        IntersectingRoads = new List<GameObject>();
    }

    public void SetBoneRotations(float rotation)
    {
        if (rotation > 18.75f)
            rotation -= 30.0f;
        else if (rotation < -18.75f)
            rotation += 30.0f;

        BoneRotation = rotation;

        foreach (var bone in _bones)
        {
            var newRotation = new Vector3(bone.transform.rotation.eulerAngles.x, bone.transform.rotation.eulerAngles.y, rotation);
            bone.transform.rotation = Quaternion.Euler(newRotation.x, newRotation.y, newRotation.z);
        }
    }

    public void SetHeadRotation(float rotation)
    {
        HeadRotation = rotation;

        var newRotation = new Vector3(90, 0, rotation * _bones.Length);
        _head.transform.rotation = Quaternion.Euler(newRotation.x, newRotation.y, newRotation.z);
    }
}
