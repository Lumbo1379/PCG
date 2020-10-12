using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TestSuite
{
    [Test]
    public void SimpleTest()
    {
        int ans = 1 + 1;

        Assert.AreEqual(ans, 2);
    }

    [Test]
    public void SimpleTest2()
    {
        Assert.AreEqual(true, true);
    }
}
