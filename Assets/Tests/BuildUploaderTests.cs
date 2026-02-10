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
        Assert.AreEqual(3, 3);
    }
    
    [Test]
    public void FailTest()
    {
        Assert.AreEqual(3, 2);
    }
    
    [UnityTest]
    public IEnumerator SucceedUnityTest()
    {
        yield return null;
        Assert.AreEqual(3, 3);
    }
}
