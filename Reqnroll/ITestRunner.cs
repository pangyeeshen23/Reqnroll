using System;
using System.Threading.Tasks;

namespace Reqnroll
{
    public interface ITestRunner
    {
        /// <summary>
        /// The ID of the parallel test worker processing the current scenario.
        /// </summary>
        string TestWorkerId { get; }
        FeatureContext FeatureContext { get; }
        ScenarioContext ScenarioContext { get; }
        ITestThreadContext TestThreadContext { get; }

        [Obsolete("TestWorkerId is now managed by Reqnroll internally - Method will be removed in v3")]
        void InitializeTestRunner(string testWorkerId);

        Task OnTestRunStartAsync();
        Task OnTestRunEndAsync();

        Task OnFeatureStartAsync(FeatureInfo featureInfo);
        Task OnFeatureEndAsync();

        void OnScenarioInitialize(ScenarioInfo scenarioInfo);
        Task OnScenarioStartAsync();

        Task CollectScenarioErrorsAsync();
        Task OnScenarioEndAsync();

        void SkipScenario();

        Task GivenAsync(string text, string multilineTextArg, Table tableArg, string keyword = null, string pickleStepId = null);
        Task WhenAsync(string text, string multilineTextArg, Table tableArg, string keyword = null, string pickleStepId = null);
        Task ThenAsync(string text, string multilineTextArg, Table tableArg, string keyword = null, string pickleStepId = null);
        Task AndAsync(string text, string multilineTextArg, Table tableArg, string keyword = null, string pickleStepId = null);
        Task ButAsync(string text, string multilineTextArg, Table tableArg, string keyword = null, string pickleStepId = null);

        void Pending();
    }
}
