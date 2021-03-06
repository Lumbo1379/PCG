﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GardenController : MonoBehaviour
{
    [Header("Construction", order = 0)]
    [SerializeField] private int _spacing;
    [SerializeField] private float _resolution;
    [SerializeField] private LayerMask _mask;

    [Header("Garden Objects", order = 1)]
    [SerializeField] GameObject[] _gardenObjects;
    [SerializeField] float[] _gardenObjectValues;

    [Header("Debug Garden Generation", order = 2)]
    [SerializeField] private bool _generateFile;
    [SerializeField] private GameObject _hitObjectGrass;
    [SerializeField] private GameObject _hitObjectNotGrass;
    [SerializeField] private GameObject _hitObjectNothing;
    [SerializeField] private bool _showHits;

    private int _offsetX;
    private int _offsetY;
    private float[,] _gardenMap;

    public void PlantGarden(float minX, float maxX, float minZ, float maxZ, int offsetX, int offsetY)
    {
        _offsetX = offsetX;
        _offsetY = offsetY;

        float tempZ = minZ;
        float tempX = minX;

        int x = (int)(maxX - minX);
        int z = (int)(maxZ - minZ);

        _gardenMap = new float[x, z];

        GenerateGardenMap(x, z);

        if (_generateFile)
            WriteMapToFile(x, z);

        RaycastHit hit;

        int row = 0;
        int column = 0;

        var valid = new bool[z, x];

        while (row < z)
        {
            while (column < x)
            {
                var position = new Vector3(tempX, 1, tempZ);

                if (Physics.Raycast(position, Vector3.down, out hit, 5, _mask))
                {
                    if (hit.transform.tag == "Grass")
                        valid[row, column] = true;
                }

                tempX += _spacing;
                column++;
            }

            tempX = minX;
            tempZ += _spacing;

            row++;
            column = 0;
        }

        tempZ = minZ - _spacing;
        tempX = minX;

        for (int r = 0; r < z; r++)
        {
            for (int c = 0; c < x; c++)
            {
                tempX += _spacing;
                column++;

                if (r != 0 && c != 0 && !valid[r - 1, c - 1]) continue;
                if (r != 0 && !valid[r - 1, c]) continue;
                if (r != 0 && c < x - 1 && !valid[r - 1, c + 1]) continue;
                if (c != 0 && !valid[r, c - 1]) continue;
                if (c < x - 1 && !valid[r, c + 1]) continue;
                if (r < z - 1 && c != 0 && !valid[r + 1, c - 1]) continue;
                if (r < z - 1 && !valid[r + 1, c]) continue;
                if (r < z - 1 && c < x - 1 && !valid[r + 1, c + 1]) continue;

                if (valid[r, c])
                {
                    if (_showHits)
                    {
                        var hitObject = Instantiate(_hitObjectGrass);
                        hitObject.transform.position = new Vector3(tempX, 0, tempZ);
                    }

                    var objectType = GetGardenObjectType(_gardenMap[r, c]);
                    var objectToPlace = GetObject(objectType);

                    if (objectToPlace != null)
                    {
                        var placedObject = Instantiate(objectToPlace);
                        placedObject.transform.position = new Vector3(tempX, 0, tempZ) + GetPositionBuffer();
                    }
                }
            }

            tempX = minX - _spacing;
            tempZ += _spacing;
        }
    }

    private void GenerateGardenMap(int length, int width)
    {
        for (int row = 0; row < length; row++)
        {
            for (int column = 0; column < width; column++)
            {
                MarkPosition(row, column, CalculateCoordinateValue(row, column, length, width));
            }
        }
    }

    private float CalculateCoordinateValue(float row, float column, int length, int width)
    {
        return Mathf.PerlinNoise(row / length * _resolution + _offsetX, column / width * _resolution + _offsetY);
    }

    private void MarkPosition(int row, int column, float value)
    {
        _gardenMap[row, column] = value;
    }

    private void WriteMapToFile(int length, int width)
    {
        using (var sw = new StreamWriter("GardenMap.txt"))
        {
            for (int row = 0; row < length; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    sw.Write(GetGardenObjectType(_gardenMap[row, column]).ToString());
                }

                sw.WriteLine();
            }
        }

        Debug.Log("Garden map wrote to file!");
    }

    private int GetGardenObjectType(float value)
    {
        for (int i = 0; i < _gardenObjectValues.Length - 1; i++)
        {
            if (value >= _gardenObjectValues[i] && value < _gardenObjectValues[i + 1]) return i;
        }

        return _gardenObjectValues.Length - 1;
    }

    private GameObject GetObject(int type)
    {
        if (_gardenObjects[type] == null) return null;

        var parent = _gardenObjects[type];
        var numberOfChildren = parent.transform.childCount;
        int randomChild = Random.Range(0, numberOfChildren);

        return parent.transform.GetChild(randomChild).gameObject;
    }

    private Vector3 GetPositionBuffer()
    {
        var x = Random.Range(0, (float)_spacing / 2);
        var z = Random.Range(0, (float)_spacing / 2);

        return new Vector3(x, 0, z);
    }
}
