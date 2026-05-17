using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

#if UNITY_6000_3_OR_NEWER
using UnityEditor.Build.Profile;
#endif

namespace Wireframe.Tests
{
	/// <summary>
	/// Verifies that LocalPathDestination correctly copies source content to a target directory.
	///
	/// The four [TestFixture] permutations cover the full truth table of:
	///   deleteCacheAfterUpload × doNotCache
	/// so that any interaction between those flags and the copy pipeline is caught.
	///
	/// Each [UnityTest] is data-driven via [TestCaseSource] to cover:
	///   FolderSource | FileSource | BuildConfigSource | BuildProfileSource (Unity 6.3+)
	/// </summary>
	[TestFixture(true,  true)]
	[TestFixture(false, true)]
	[TestFixture(true,  false)]
	[TestFixture(false, false)]
	public class LocalDestination_Tests : BaseTest
	{
		// -------------------------------------------------------------------
		// Fixture-level state
		// -------------------------------------------------------------------

		private readonly bool _deleteCacheAfterUpload;
		private readonly bool _doNotCache;

		// The value of the preference before this fixture touched it,
		// restored in TearDown to ensure test isolation.
		private bool _originalDeleteCacheAfterUpload;

		public LocalDestination_Tests(bool deleteCacheAfterUpload, bool doNotCache)
		{
			_deleteCacheAfterUpload = deleteCacheAfterUpload;
			_doNotCache             = doNotCache;
		}

		// -------------------------------------------------------------------
		// NUnit lifecycle — override base so we can sandwich preference state
		// -------------------------------------------------------------------

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			_originalDeleteCacheAfterUpload      = Preferences.DeleteCacheAfterUpload;
			Preferences.DeleteCacheAfterUpload   = _deleteCacheAfterUpload;
		}

		[TearDown]
		public override void TearDown()
		{
			// Restore preference before base TearDown deletes temp files,
			// so any preference-driven cleanup in the asset still runs correctly.
			Preferences.DeleteCacheAfterUpload = _originalDeleteCacheAfterUpload;
			base.TearDown();
		}

		// -------------------------------------------------------------------
		// Test data
		//
		// IMPORTANT: TestCaseSource is evaluated at test-discovery time, before
		// the full editor domain is guaranteed to be ready. Therefore Sources()
		// must not call Application.dataPath or construct Unity objects here.
		// Instead we yield lightweight descriptor objects; all Unity API calls
		// are deferred into the test body via the factory lambdas.
		// -------------------------------------------------------------------

		public static IEnumerable Sources()
		{
			// FolderSource — copies an entire directory
			yield return new TestCaseData(new SourceDescriptor(
				name: "FolderSource",
				buildSource: () => new FolderSource(Path.Combine(Application.dataPath, "Scenes")),
				getExpectedFiles: source =>
					Directory.GetFiles(source.SourceFilePath(), "*.*", SearchOption.AllDirectories)
			));

			// FileSource — copies a single file
			yield return new TestCaseData(new SourceDescriptor(
				name: "FileSource",
				buildSource: () => new FileSource(Path.Combine(Application.dataPath, "Scenes", "SampleScene.unity")),
				getExpectedFiles: source => new[] { source.SourceFilePath() }
			));

			// BuildConfigSource — triggers a real build, copies the resulting executable
			yield return new TestCaseData(new SourceDescriptor(
				name: "BuildConfigSource",
				buildSource: () =>
				{
					BuildConfig config = new BuildConfig
					{
						ProductName    = nameof(LocalDestination_Test),
						Target         = BuildTarget.StandaloneWindows64,
						TargetPlatform = BuildTargetGroup.Standalone,
						SceneGUIDs     = new List<string> { "99c9720ab356a0642a771bea13969a05" }
					};
					return new BuildConfigSource(config);
				},
				getExpectedFiles: source =>
				{
					var buildSource = (BuildConfigSource)source;
					return new[] { Path.Combine(source.SourceFilePath(), buildSource.BuildConfig.GetProductName + ".exe") };
				}
			));

#if UNITY_6000_3_OR_NEWER
			// BuildProfileSource — Unity 6.3+ only
			yield return new TestCaseData(new SourceDescriptor(
				name: "BuildProfileSource",
				buildSource: () =>
				{
					BuildProfile profile = BuildUtils
						.GetAllCustomBuildProfiles()
						.FirstOrDefault(p => new BuildProfileWrapper(p).GetTarget == BuildTarget.StandaloneWindows64);

					Assert.IsNotNull(profile,
						"No StandaloneWindows64 Build Profile found. Add one to the project before running this test.");

					return new BuildProfileSource(profile);
				},
				getExpectedFiles: source =>
				{
					var profileSource = (BuildProfileSource)source;
					return new[] { Path.Combine(source.SourceFilePath(), profileSource.BuildConfig.GetProductName + ".exe") };
				}
			));
#endif
		}

		// -------------------------------------------------------------------
		// Test
		// -------------------------------------------------------------------

		[UnityTest, TestCaseSource(nameof(Sources))]
		public IEnumerator LocalDestination_Test(SourceDescriptor descriptor)
		{
			// Resolve the source here in the test body, not at discovery time.
			AUploadSource source = descriptor.BuildSource();

			string destinationPath = Path.Combine(
				Application.temporaryCachePath,
				"Tests",
				nameof(LocalDestination_Test),
				descriptor.Name);

			// Register so TearDown cleans up regardless of pass or fail.
			RegisterTempPath(destinationPath);

			UploadTask task = SetupNewTask();

			UploadConfig config = new UploadConfig();
			task.AddConfig(config);

			config.AddSource(source);
			config.Sources[config.Sources.Count - 1].DoNotCache = _doNotCache;

			config.AddDestination(new LocalPathDestination(destinationPath));

			yield return Succeed(task, "Task failed — verify the source path exists and the destination is writable.");

			// Assert that every expected file arrived at the correct relative path
			// under destinationPath. AssertCompareDirectories (for folder sources) or
			// individual file checks (for single-file/build sources) are used so that
			// both missing files *and* unexpected extra files are caught.
			string[] expectedFiles = descriptor.GetExpectedFiles(source);

			if (expectedFiles.Length == 0)
			{
				Assert.Fail($"SourceDescriptor '{descriptor.Name}' returned zero expected files. " +
							 "The test data is misconfigured.");
			}

			string sourceRoot = Utils.GetDirectoryOrFileDirectory(source.SourceFilePath());

			var missingFiles = new List<string>();
			foreach (string expectedAbsolute in expectedFiles)
			{
				string relativePath  = expectedAbsolute.Substring(sourceRoot.Length)
													   .TrimStart(Path.DirectorySeparatorChar,
																  Path.AltDirectorySeparatorChar);
				string expectedAtDest = Path.Combine(destinationPath, relativePath);

				if (!File.Exists(expectedAtDest))
				{
					missingFiles.Add(expectedAtDest);
				}
			}

			if (missingFiles.Count > 0)
			{
				Assert.Fail($"[{descriptor.Name}] The following expected files were not found at the destination:\n" +
							string.Join("\n", missingFiles));
			}
		}

		// -------------------------------------------------------------------
		// Test data carrier
		//
		// Using a factory lambda (Func<AUploadSource>) instead of a constructed
		// source object defers all Unity API calls to test-body time, keeping
		// Sources() safe to call at discovery time.
		// -------------------------------------------------------------------

		public sealed class SourceDescriptor
		{
			public readonly string Name;

			private readonly Func<AUploadSource>               _buildSource;
			private readonly Func<AUploadSource, string[]>     _getExpectedFiles;

			public SourceDescriptor(
				string name,
				Func<AUploadSource> buildSource,
				Func<AUploadSource, string[]> getExpectedFiles)
			{
				Name              = name;
				_buildSource      = buildSource;
				_getExpectedFiles = getExpectedFiles;
			}

			public AUploadSource BuildSource()           => _buildSource();
			public string[]      GetExpectedFiles(AUploadSource source) => _getExpectedFiles(source);

			// NUnit uses ToString() as the test name displayed in the runner and CI output.
			public override string ToString() => Name;
		}
	}
}
