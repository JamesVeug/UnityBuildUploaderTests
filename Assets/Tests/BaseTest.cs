using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
namespace Wireframe.Tests
{
	/// <summary>
	/// Base class for all BuildUploader edit-mode tests.
	///
	/// Responsibilities:
	///   - Provide a clean UploadTask factory with deterministic log state.
	///   - Provide Succeed/Fail coroutine helpers that drive the task to completion.
	///   - Provide a correct directory comparison assertion.
	///   - Track and clean up any temp paths registered by subclasses.
	/// </summary>
	public abstract class BaseTest
	{
		// Subclasses register temp directories here during SetUp; all are wiped in TearDown.
		private readonly List<string> _tempPathsToCleanup = new List<string>();

		// -------------------------------------------------------------------
		// NUnit lifecycle
		// -------------------------------------------------------------------

		[SetUp]
		public virtual void SetUp()
		{
			_tempPathsToCleanup.Clear();
		}

		[TearDown]
		public virtual void TearDown()
		{
			foreach (string path in _tempPathsToCleanup)
			{
				if (Directory.Exists(path))
				{
					Directory.Delete(path, recursive: true);
				}
				else if (File.Exists(path))
				{
					File.Delete(path);
				}
			}

			_tempPathsToCleanup.Clear();
		}

		// -------------------------------------------------------------------
		// Factory helpers
		// -------------------------------------------------------------------

		/// <summary>
		/// Creates a fresh UploadTask for testing.
		/// </summary>
		protected UploadTask SetupNewTask()
		{
			return new UploadTask();
		}

		/// <summary>
		/// Registers a path to be deleted automatically in TearDown.
		/// Call this for any directory or file your test writes to.
		/// </summary>
		protected void RegisterTempPath(string path)
		{
			if (!_tempPathsToCleanup.Contains(path))
			{
				_tempPathsToCleanup.Add(path);
			}
		}

		// -------------------------------------------------------------------
		// Execution helpers
		// -------------------------------------------------------------------

		/// <summary>
		/// Drives the task to completion and asserts it succeeded.
		/// </summary>
		protected IEnumerator Succeed(UploadTask task, string failMessage)
		{
			return ExecuteAsync(task, failMessage, shouldSucceed: true);
		}

		/// <summary>
		/// Drives the task to completion and asserts it failed.
		/// </summary>
		protected IEnumerator Fail(UploadTask task, string failMessage)
		{
			return ExecuteAsync(task, failMessage, shouldSucceed: false);
		}

		private IEnumerator ExecuteAsync(UploadTask task, string failMessage, bool shouldSucceed)
		{
			// Disable Unity console logging for the task — all diagnostic information
			// is captured in the report and surfaced via BuildFailReasonString below.
			// This prevents LogAssert from treating expected failure-path errors as
			// test failures, and keeps CI output clean.
			// StartAsync returns a Task whose exceptions must be observed.
			// We store it so the GC finaliser doesn't raise an unobserved exception,
			// but we intentionally do not await it here — the coroutine polls IsComplete
			// which is driven by the task internally.
			System.Threading.Tasks.Task asyncTask = task.StartAsync(invokeDebugLogs: false);

			while (!task.IsComplete)
			{
				yield return null;
			}

			// Surface any unobserved exception from the async path as a test failure,
			// rather than letting it silently disappear or crash a finaliser thread.
			if (asyncTask.IsFaulted)
			{
				Assert.Fail($"UploadTask threw an unhandled exception: {asyncTask.Exception?.Flatten().InnerException?.Message}");
				yield break;
			}

			if (shouldSucceed && !task.IsSuccessful)
			{
				// Collect all fail reasons from the report so the test output
				// immediately tells you what went wrong without needing to dig
				// through Unity logs or re-run with extra instrumentation.
				string reasons = BuildFailReasonString(task);
				Assert.Fail($"{failMessage}\n\nTask report:\n{reasons}");
			}
			else if (!shouldSucceed)
			{
				Assert.IsFalse(task.IsSuccessful, failMessage);
			}
		}

		private static string BuildFailReasonString(UploadTask task)
		{
			if (task.Report == null)
			{
				return "(no report available)";
			}

			var sb = new System.Text.StringBuilder();
			foreach (var (stepType, reason) in task.Report.GetFailReasons())
			{
				sb.AppendLine($"  [{stepType}] {reason}");
			}

			return sb.Length > 0 ? sb.ToString() : task.Report.GetReport(ignoreEmptySteps: true);
		}

		// -------------------------------------------------------------------
		// Assertion helpers
		// -------------------------------------------------------------------

		/// <summary>
		/// Asserts that two directories contain the same set of relative file paths.
		/// Count-only comparison is insufficient — this compares actual relative paths
		/// so a rename or structural difference is caught.
		/// </summary>
		protected void AssertCompareDirectories(string expectedDir, string actualDir)
		{
			Assert.IsTrue(Directory.Exists(expectedDir),
				$"Expected source directory does not exist: '{expectedDir}'");
			Assert.IsTrue(Directory.Exists(actualDir),
				$"Actual destination directory does not exist: '{actualDir}'");

			string[] expectedFiles = Directory.GetFiles(expectedDir, "*.*", SearchOption.AllDirectories);
			string[] actualFiles   = Directory.GetFiles(actualDir,   "*.*", SearchOption.AllDirectories);

			// Normalise to relative paths for comparison so root differences don't matter.
			var expectedRelative = NormaliseToRelative(expectedFiles, expectedDir);
			var actualRelative   = NormaliseToRelative(actualFiles,   actualDir);

			// Report all missing and unexpected files in one failure message rather than
			// stopping at the first mismatch, which gives the full picture in CI logs.
			var missing    = new List<string>();
			var unexpected = new List<string>();

			foreach (string rel in expectedRelative)
			{
				if (!actualRelative.Contains(rel))
					missing.Add(rel);
			}

			foreach (string rel in actualRelative)
			{
				if (!expectedRelative.Contains(rel))
					unexpected.Add(rel);
			}

			if (missing.Count > 0 || unexpected.Count > 0)
			{
				string missingStr    = missing.Count    > 0 ? $"\nMissing:    {string.Join(", ", missing)}"    : string.Empty;
				string unexpectedStr = unexpected.Count > 0 ? $"\nUnexpected: {string.Join(", ", unexpected)}" : string.Empty;
				Assert.Fail($"Directory contents differ between '{expectedDir}' and '{actualDir}'.{missingStr}{unexpectedStr}");
			}
		}

		private static HashSet<string> NormaliseToRelative(string[] absolutePaths, string root)
		{
			var result = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
			foreach (string abs in absolutePaths)
			{
				// +1 to trim the leading directory separator.
				string relative = abs.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				result.Add(relative);
			}
			return result;
		}
	}
}
