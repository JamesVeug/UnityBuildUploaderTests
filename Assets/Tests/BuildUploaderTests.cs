using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BuildUploaderTests
{
    [Test]
    public void SucceedTest()
    {
        Assert.AreEqual(3, 3, "This test passed? 3 != 3? ABSURD!");
    }
    
    [Test]
    public void FailTest()
    {
        Assert.AreNotEqual(3, 3, "This test failed? 3 == 3? ABSURD!");
    }
    
    [UnityTest]
    public IEnumerator SucceedUnityTest()
    {
        yield return null;
        Assert.AreEqual(3, 3, "This test passed? 3 != 3? ABSURD!");
    }
}
