using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TestSuitePlay
{
    [UnityTest]
    public IEnumerator WoodenBlockFacesAreBleached()
    {
        var block = Object.Instantiate(Resources.Load<GameObject>("Prefabs/Wood Block"));

        var blockMats = block.GetComponent<Renderer>().materials;

        var bleach = block.GetComponent<SunBleach>();
        bleach.SetFacesAndBleach(true);

        Assert.AreNotEqual(blockMats[0].GetColor("DefaultColour"), blockMats[1].GetColor("DefaultColour"));

        yield return null;
    }

    [UnityTest]
    public IEnumerator WoodenBlockFacesAreNotBleached()
    {
        var block = Object.Instantiate(Resources.Load<GameObject>("Prefabs/Wood Block"));

        var blockMats = block.GetComponent<Renderer>().materials;

        var bleach = block.GetComponent<SunBleach>();
        bleach.SetFacesAndBleach(false);

        Assert.AreEqual(blockMats[0].GetColor("DefaultColour"), blockMats[1].GetColor("DefaultColour"));

        yield return null;
    }
}
