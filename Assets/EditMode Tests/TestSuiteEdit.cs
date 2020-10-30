using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TestSuiteEdit
{
    [Test]
    public void SimpleTest()
    {
        int ans = 1 + 1;

        Assert.AreEqual(ans, 2);
    }
}
