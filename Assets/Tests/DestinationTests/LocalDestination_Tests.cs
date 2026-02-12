using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Wireframe.Tests
{
    [TestFixture(true, true)]
    [TestFixture(false, true)]
    [TestFixture(false, false)]
    [TestFixture(true, false)]
    public class LocalDestination_Tests : BaseTest
    {
        private bool doNotCache;
        
        public LocalDestination_Tests(bool deleteCacheAfterUpload, bool doNotCache)
        {
            this.doNotCache = doNotCache;
            Preferences.DeleteCacheAfterUpload = deleteCacheAfterUpload; // Not - parallelizable
        }

        [UnityTest]
        public IEnumerator Copy_ScenesFolder_To_Destination()
        {
            UploadTask task = SetupNewTask();

            UploadConfig config = new UploadConfig();
            task.AddConfig(config);

            // Copy Scenes folder
            string sourcePath = Path.Combine(Application.dataPath, "Scenes");
            FolderSource source = new FolderSource(sourcePath);
            config.AddSource(source);
            config.Sources[^1].DoNotCache = doNotCache;

            // Past to Temp/Tests/Copy_ScenesFolder_To_Destination
            string destinationPath = Path.Combine(Application.temporaryCachePath, "Tests", "Copy_ScenesFolder_To_Destination");
            config.AddDestination(new LocalPathDestination(destinationPath));

            yield return Succeed(task, "Task should be successful because we know the source directory exists.");

            AssertCompareDirectories(sourcePath, destinationPath);
        }
    }
}