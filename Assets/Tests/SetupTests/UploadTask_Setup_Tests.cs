using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using Wireframe;

[Parallelizable]
public class UploadTask_Setup_Tests : BaseTest
{
    public UploadTask_Setup_Tests()
    {
        
    }

    [UnityTest]
    public IEnumerator EmptyUploadTask()
    {
        UploadTask task = SetupNewTask();
        yield return Succeed(task, "Task should be successful since we have no actions, modifiers, destinations");
    }
        
    [UnityTest]
    public IEnumerator EmptyUploadConfig()
    {
        UploadTask task = SetupNewTask();
        
        UploadConfig config = new UploadConfig();
        task.AddConfig(config);
        
        yield return Succeed(task, "Task should be successful since the config has no actions, modifiers, destinations");
    }

    [UnityTest]
    public IEnumerator NullEnabledSource()
    {
        UploadTask task = SetupNewTask();
        
        UploadConfig config = new UploadConfig();
        task.AddConfig(config);
        
        config.AddSource(new UploadConfig.SourceData());
        
        yield return Fail(task, "Task should have failed because the source isn't defined!");
    }

    [UnityTest]
    public IEnumerator NullDisabledSource()
    {
        UploadTask task = SetupNewTask();
        
        UploadConfig config = new UploadConfig();
        task.AddConfig(config);
        
        UploadConfig.SourceData data = new UploadConfig.SourceData();
        data.Enabled = false;
        config.AddSource(data);
        
        yield return Succeed(task, "Task should have succeeded because although the source is null, its disabled!");
    }

    [UnityTest]
    public IEnumerator NullEnabledModifier()
    {
        UploadTask task = SetupNewTask();
        
        UploadConfig config = new UploadConfig();
        task.AddConfig(config);
        
        UploadConfig.ModifierData data = new UploadConfig.ModifierData();
        config.AddModifier(data);
        
        yield return Fail(task, "Task should have failed because the modifier is null, its disabled!");
    }

    [UnityTest]
    public IEnumerator NullDisabledModifier()
    {
        UploadTask task = SetupNewTask();
        
        UploadConfig config = new UploadConfig();
        task.AddConfig(config);
        
        UploadConfig.ModifierData data = new UploadConfig.ModifierData();
        data.Enabled = false;
        config.AddModifier(data);
        
        yield return Succeed(task, "Task should have succeeded because although the modifier is null, its disabled!");
    }

    [UnityTest]
    public IEnumerator NullEnabledDestination()
    {
        UploadTask task = SetupNewTask();
        
        UploadConfig config = new UploadConfig();
        task.AddConfig(config);
        
        UploadConfig.DestinationData data = new UploadConfig.DestinationData();
        config.AddDestination(data);
        
        yield return Fail(task, "Task should have failed because the destination is null, its disabled!");
    }

    [UnityTest]
    public IEnumerator NullDisabledDestination()
    {
        UploadTask task = SetupNewTask();
        
        UploadConfig config = new UploadConfig();
        task.AddConfig(config);
        
        UploadConfig.DestinationData data = new UploadConfig.DestinationData();
        data.Enabled = false;
        config.AddDestination(data);
        
        yield return Succeed(task, "Task should have succeeded because although the destination is null, its disabled!");
    }
}