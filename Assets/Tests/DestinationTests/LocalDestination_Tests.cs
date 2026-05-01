using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.TestTools;

namespace Wireframe.Tests
{
    /// <summary>
    /// https://docs.nunit.org/articles/nunit/writing-tests/attributes/testcasesource.html
    /// </summary>
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

        public static IEnumerable Sources()
        {
            // Folder Source
            FolderSource folderSource = new FolderSource(Path.Combine(Application.dataPath, "Scenes"));
            Func<AUploadSource, string[]> getFolderFiles = static (source) =>
            {
                return Directory.GetFiles(source.SourceFilePath(), "*.*", SearchOption.AllDirectories);
            };
            yield return new TestCaseData(new Args(folderSource, getFolderFiles)).Returns(null);
                
            // File Source
            FileSource fileSource = new FileSource(Path.Combine(Application.dataPath, "Scenes", "SampleScene.unity"));
            Func<AUploadSource, string[]> getFileFiles = static (source) => { return new string[]{ source.SourceFilePath() }; };
            yield return new TestCaseData(new Args(fileSource, getFileFiles)).Returns(null);
            
            // Build Config
            BuildConfig config = new BuildConfig();
            config.ProductName = nameof(LocalDestination_Test);
            config.Target = BuildTarget.StandaloneWindows64;
            config.TargetPlatform = BuildTargetGroup.Standalone;
            config.SceneGUIDs = new List<string>() { "99c9720ab356a0642a771bea13969a05" }; // SampleScene.unity
            
            BuildConfigSource configSource = new BuildConfigSource(config);
            Func<AUploadSource, string[]> getBuildConfigFiles = static (source) =>
            {
                BuildConfigSource buildSource = source as BuildConfigSource;
                return new string[] { Path.Combine(source.SourceFilePath(), buildSource.BuildConfig.GetProductName + ".exe") };
            };
            yield return new TestCaseData(new Args(configSource, getBuildConfigFiles)).Returns(null); 
            
            // Build Profile
            BuildProfile profile = BuildUtils.GetAllCustomBuildProfiles().FirstOrDefault(a=>new BuildProfileWrapper(a).GetTarget == BuildTarget.StandaloneWindows64);
            BuildProfileSource profileSource = new BuildProfileSource(profile); 
            Func<AUploadSource, string[]> getBuildProfileFiles = static (source) =>
            {
                BuildProfileSource profileSource = source as BuildProfileSource;
                return new string[] { Path.Combine(source.SourceFilePath(), profileSource.BuildConfig.GetProductName + ".exe") };
            };
            yield return new TestCaseData(new Args(profileSource, getBuildProfileFiles)).Returns(null); 
        }

        [UnityTest, TestCaseSource(nameof(Sources))]
        public IEnumerator LocalDestination_Test(Args input)
        {
            UploadTask task = SetupNewTask();

            UploadConfig config = new UploadConfig();
            task.AddConfig(config);
            
            // Get contents from the source
            config.AddSource(input.source);
            config.Sources[^1].DoNotCache = doNotCache;
            
            // Copy them to this path
            string destinationPath = Path.Combine(Application.temporaryCachePath, "Tests", nameof(LocalDestination_Test), input.source.GetType().Name);
            config.AddDestination(new LocalPathDestination(destinationPath));

            // Execute and make sure it was successful
            yield return Succeed(task, "Task should be successful because we know the source directory exists.");

            // string[] files = Directory.GetFiles(destinationPath, "*.*", SearchOption.AllDirectories);
            string[] expectedFiles = input.expectedFiles(input.source);
            
            string filePath = Utils.GetDirectoryOrFileDirectory(input.source.SourceFilePath());
            List<string> missingFiles = new List<string>();
            for (int i = 0; i < expectedFiles.Length; i++)
            {
                string endPath = expectedFiles[i].Substring(filePath.Length + 1);
                string expectedPath = Path.Combine(destinationPath, endPath);
                if (!File.Exists(expectedPath))
                {
                    missingFiles.Add(expectedPath);
                }
            }

            if (missingFiles.Count > 0)
            {
                Assert.Fail($"Missing files: {string.Join(", ", missingFiles)}");
            }

            // Confirm the content arrived
            // string sourceFilePath = input.source.SourceFilePath();
            // if (Utils.IsPathADirectory(sourceFilePath))
            // {
            //     AssertCompareDirectories(sourceFilePath, destinationPath);
            // }
            // else
            // {
            //     string fileName = Path.GetFileName(sourceFilePath);
            //     string expectedPath = Path.Combine(destinationPath, fileName);
            //     Assert.True(File.Exists(expectedPath), $"File '{expectedPath}' does not exist.");
            // }
        }

        public class Args
        {
            public AUploadSource source;
            public Func<AUploadSource, string[]> expectedFiles;

            public Args(AUploadSource fileSource, Func<AUploadSource, string[]> expectedFiles)
            {
                this.source = fileSource;
                this.expectedFiles = expectedFiles;
            }

            public override string ToString()
            {
                return source.ToString();
            }
        }
    }
}