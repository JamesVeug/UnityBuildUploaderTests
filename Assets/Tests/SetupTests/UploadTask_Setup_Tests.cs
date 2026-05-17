using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Wireframe.Tests
{
    /// <summary>
    /// Validates UploadTask and UploadConfig setup-time behaviour:
    /// empty configs, null-but-enabled data, and null-but-disabled data
    /// for each of the three data roles: Source, Modifier, Destination.
    ///
    /// Note: [Parallelizable] is intentionally absent. Unity's test runner
    /// does not support parallel execution of [UnityTest] coroutines;
    /// the attribute would be silently ignored and mislead readers.
    /// </summary>
    public class UploadTask_Setup_Tests : BaseTest
    {
        // Reused across every test via [SetUp] in BaseTest.
        // Each test gets a fresh task and config; no shared mutable state.
        private UploadTask _task;
        private UploadConfig _config;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _task   = SetupNewTask();
            _config = new UploadConfig();
            _task.AddConfig(_config);
        }

        // -------------------------------------------------------------------
        // Empty / no-op configurations
        // -------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EmptyUploadTask_Succeeds()
        {
            // A task with no configs attached should be a no-op success,
            // not an error — there is nothing to fail.
            UploadTask emptyTask = SetupNewTask();
            yield return Succeed(emptyTask,
                "An UploadTask with no configs should succeed as a no-op.");
        }

        [UnityTest]
        public IEnumerator EmptyUploadConfig_Succeeds()
        {
            // A config with no sources, modifiers, or destinations should also
            // be a no-op success. The task already has _config attached via SetUp.
            yield return Succeed(_task,
                "An UploadTask with an empty UploadConfig should succeed as a no-op.");
        }

        // -------------------------------------------------------------------
        // Source: null data, enabled vs disabled
        // -------------------------------------------------------------------

        [UnityTest]
        public IEnumerator NullEnabledSource_Fails()
        {
            _config.AddSource(new UploadConfig.SourceData());

            yield return Fail(_task,
                "An enabled SourceData with a null source should cause the task to fail.");
        }

        [UnityTest]
        public IEnumerator NullDisabledSource_Succeeds()
        {
            var data = new UploadConfig.SourceData { Enabled = false };
            _config.AddSource(data);

            yield return Succeed(_task,
                "A disabled SourceData with a null source should be skipped, not fail.");
        }

        // -------------------------------------------------------------------
        // Modifier: null data, enabled vs disabled
        // -------------------------------------------------------------------

        [UnityTest]
        public IEnumerator NullEnabledModifier_Fails()
        {
            _config.AddModifier(new UploadConfig.ModifierData());

            yield return Fail(_task,
                "An enabled ModifierData with a null modifier should cause the task to fail.");
        }

        [UnityTest]
        public IEnumerator NullDisabledModifier_Succeeds()
        {
            var data = new UploadConfig.ModifierData { Enabled = false };
            _config.AddModifier(data);

            yield return Succeed(_task,
                "A disabled ModifierData with a null modifier should be skipped, not fail.");
        }

        // -------------------------------------------------------------------
        // Destination: null data, enabled vs disabled
        // -------------------------------------------------------------------

        [UnityTest]
        public IEnumerator NullEnabledDestination_Fails()
        {
            _config.AddDestination(new UploadConfig.DestinationData());

            yield return Fail(_task,
                "An enabled DestinationData with a null destination should cause the task to fail.");
        }

        [UnityTest]
        public IEnumerator NullDisabledDestination_Succeeds()
        {
            var data = new UploadConfig.DestinationData { Enabled = false };
            _config.AddDestination(data);

            yield return Succeed(_task,
                "A disabled DestinationData with a null destination should be skipped, not fail.");
        }

        // -------------------------------------------------------------------
        // Edge cases: combinations and multi-config interactions
        // -------------------------------------------------------------------

        /// <summary>
        /// All three null-but-enabled data types in the same config.
        /// The task should fail; this catches any short-circuit that might
        /// accidentally succeed when only one type is checked.
        /// </summary>
        [UnityTest]
        public IEnumerator AllNullEnabledData_Fails()
        {
            _config.AddSource(new UploadConfig.SourceData());
            _config.AddModifier(new UploadConfig.ModifierData());
            _config.AddDestination(new UploadConfig.DestinationData());

            yield return Fail(_task,
                "A config with all three null-but-enabled data types should fail.");
        }

        /// <summary>
        /// All three null-but-disabled data types in the same config.
        /// All should be skipped; the task should succeed as a no-op.
        /// </summary>
        [UnityTest]
        public IEnumerator AllNullDisabledData_Succeeds()
        {
            _config.AddSource(new UploadConfig.SourceData { Enabled = false });
            _config.AddModifier(new UploadConfig.ModifierData { Enabled = false });
            _config.AddDestination(new UploadConfig.DestinationData { Enabled = false });

            yield return Succeed(_task,
                "A config where all null data is disabled should succeed as a no-op.");
        }

        /// <summary>
        /// Two configs on the same task: one invalid (null enabled source),
        /// one empty (valid no-op). The task should still fail because one
        /// config is invalid, regardless of the other being clean.
        /// </summary>
        [UnityTest]
        public IEnumerator MultipleConfigs_OneInvalid_Fails()
        {
            // _config from SetUp is the first config (empty / valid no-op).
            UploadConfig invalidConfig = new UploadConfig();
            invalidConfig.AddSource(new UploadConfig.SourceData());
            _task.AddConfig(invalidConfig);

            yield return Fail(_task,
                "A task with one valid config and one invalid config should fail.");
        }

        /// <summary>
        /// Two configs on the same task, both empty / valid no-ops.
        /// The task should succeed.
        /// </summary>
        [UnityTest]
        public IEnumerator MultipleConfigs_BothEmpty_Succeeds()
        {
            UploadConfig secondConfig = new UploadConfig();
            _task.AddConfig(secondConfig);

            yield return Succeed(_task,
                "A task with two empty configs should succeed as a no-op.");
        }
    }
}
