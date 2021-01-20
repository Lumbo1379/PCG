using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GardenController : MonoBehaviour
{
    [Header("Construction", order = 0)]
    [SerializeField] private int _spacing;

    [Header("Garden Objects", order = 1)]
    [SerializeField] private GameObject _tree;

    public void PlantGarden(float minX, float maxX, float minZ, float maxZ)
    {
        float tempZ = minZ;
        float tempX = minX;

        while (tempZ < maxZ)
        {
            while (tempX < maxX)
            {
                var tree = Instantiate(_tree);
                tree.transform.position = new Vector3(tempX, 0, tempZ);

                tempX += _spacing;
            }

            tempX = minX;
            tempZ += _spacing;
        }
    }
}
