using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetUVs : MonoBehaviour
{
    public Vector2[] UVs { get; set; }

    private void Start()
    {
        var mesh = GetComponent<MeshFilter>().mesh;
        UVs = mesh.uv;
    }
}
