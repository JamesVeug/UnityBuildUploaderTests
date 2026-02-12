using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;
using Wireframe;

public abstract class BaseTest
{
    public BaseTest()
    {
        
    }
    
    public UploadTask SetupNewTask()
    {
        LogAssert.ignoreFailingMessages = true;
        UploadTask task = new UploadTask();
        return task;
    } 
    
    public IEnumerator Succeed(UploadTask task, string failMessage)
    {
        return ExecuteAsync(task, failMessage, true);
    }
    
    public IEnumerator Fail(UploadTask task, string failMessage)
    {
        return ExecuteAsync(task, failMessage, false);
    }
    
    private IEnumerator ExecuteAsync(UploadTask task, string failMessage, bool shouldSucceed)
    {
        _ = task.StartAsync();
        while (!task.IsComplete)
        {
            yield return null;
        }

        if (shouldSucceed)
        {
            Assert.IsTrue(task.IsSuccessful, failMessage);
        }
        else
        {
            Assert.IsFalse(task.IsSuccessful, failMessage);
        }
    }
    
    public void AssertCompareDirectories(string path1, string path2)
    {
        Assert.IsTrue(Directory.Exists(path1), $"Directory does not exist: '{path1}'");
        Assert.IsTrue(Directory.Exists(path2), $"Directory does not exist: '{path2}'");
        
        // Compare files in all folders of both directories
        string[] path1Files = Directory.GetFiles(path1, "*.*", SearchOption.AllDirectories);
        string[] path2Files = Directory.GetFiles(path2, "*.*", SearchOption.AllDirectories);
        
        Assert.AreEqual(path1Files.Length, path2Files.Length, $"Directory '{path1}' and '{path2}' have different number of files.");
    }
}