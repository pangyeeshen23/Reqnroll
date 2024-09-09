
using Reqnroll.CucumberMessages;
using Io.Cucumber.Messages.Types;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Newtonsoft.Json.Bson;
using Reqnroll;
using System.Reflection;
using FluentAssertions;
using System.Text.Json;

namespace CucumberMessages.CompatibilityTests
{
    [TestClass]
    public class CucumberCompatibilityTests : CucumberCompatibilityTestBase
    {
        [TestMethod]
        public void NullTest()
        {
            // The purpose of this test is to confirm that when Cucumber Messages are turned off, the Cucumber Messages ecosystem does not cause any interference anywhere else

            AddFeatureFile("""
                Feature: Cucumber Messages Null Test
                  Scenario: Eating Cukes
                     When I eat 5 cukes
                """);

            AddPassingStepBinding("When");

            ExecuteTests();

            ShouldAllScenariosPass();
        }
        [TestMethod]
        public void SmokeTest()
        {
            AddCucumberMessagePlugIn();
            CucumberMessagesAddConfigurationFile("CucumberMessages.configuration.json");

            AddFeatureFile("""
                Feature: Cucumber Messages Smoke Test
                  @some-tag
                  Scenario: Log JSON
                     When the following string is attached as "application/json":
                       ```
                       {"message": "The <b>big</b> question", "foo": "bar"}
                       ```
                """);

            AddPassingStepBinding("When");
            ExecuteTests();

            ShouldAllScenariosPass();
        }

        [TestMethod]
        public void CucumberMessagesInteropWithExternalData()
        {
            // The purpose of this test is to prove that the ScenarioOutline tables generated by the ExternalData plugin can be used in Cucumber messages
            AddCucumberMessagePlugIn();
            _projectsDriver.AddNuGetPackage("Reqnroll.ExternalData", "2.2.0-local");
            CucumberMessagesAddConfigurationFile("CucumberMessages.configuration.json");

            // this test borrows a subset of a feature, the binding class, and the data file from ExternalData.ReqnrollPlugin.IntegrationTest
            var content = _testFileManager.GetTestFileContent("products.csv", "CucumberMessages.CompatibilityTests", Assembly.GetExecutingAssembly());
            _projectsDriver.AddFile("products.csv", content);
            AddFeatureFile("""
                Feature: External Data from CSV file

                @DataSource:products.csv
                Scenario: Valid product prices are calculated
                	The scenario will be treated as a scenario outline with the examples from the CSV file.
                	Given the customer has put 1 pcs of <product> to the basket
                	When the basket price is calculated
                	Then the basket price should be greater than zero
                
                """);

            AddBindingClass("""
                using System;
                using System.Collections.Generic;
                using System.Linq;

                namespace Reqnroll.ExternalData.ReqnrollPlugin.IntegrationTest.StepDefinitions
                {
                    [Binding]
                    public class PricingStepDefinitions
                    {
                        class PriceCalculator
                        {
                            private readonly Dictionary<string, int> _basket = new();
                            private readonly Dictionary<string, decimal> _itemPrices = new();

                            public void AddToBasket(string productName, int quantity)
                            {
                                if (!_basket.TryGetValue(productName, out var currentQuantity)) 
                                    currentQuantity = 0;
                                _basket[productName] = currentQuantity + quantity;
                            }

                            public decimal CalculatePrice()
                            {
                                return _basket.Sum(bi => GetPrice(bi.Key) * bi.Value);
                            }

                            private decimal GetPrice(string productName)
                            {
                                if (_itemPrices.TryGetValue(productName, out var itemPrice)) 
                                    return itemPrice;
                                return 1.5m;
                            }

                            public void SetPrice(string productName, in decimal itemPrice)
                            {
                                _itemPrices[productName] = itemPrice;
                            }
                        }

                        private readonly ScenarioContext _scenarioContext;
                        private readonly PriceCalculator _priceCalculator = new();
                        private decimal _calculatedPrice;

                        public PricingStepDefinitions(ScenarioContext scenarioContext)
                        {
                            _scenarioContext = scenarioContext;
                        }

                        [Given(@"the price of (.*) is �(.*)")]
                        public void GivenThePriceOfProductIs(string productName, decimal itemPrice)
                        {
                            _priceCalculator.SetPrice(productName, itemPrice);
                        }

                        [Given(@"the customer has put (.*) pcs of (.*) to the basket")]
                        public void GivenTheCustomerHasPutPcsOfProductToTheBasket(int quantity, string productName)
                        {
                            _priceCalculator.AddToBasket(productName, quantity);
                        }

                        [Given(@"the customer has put a product to the basket")]
                        public void GivenTheCustomerHasPutAProductToTheBasket()
                        {
                            var productName = _scenarioContext.ScenarioInfo.Arguments["product"]?.ToString();
                            _priceCalculator.AddToBasket(productName, 1);
                        }

                        [When(@"the basket price is calculated")]
                        public void WhenTheBasketPriceIsCalculated()
                        {
                            _calculatedPrice = _priceCalculator.CalculatePrice();
                        }

                        [Then(@"the basket price should be greater than zero")]
                        public void ThenTheBasketPriceShouldBeGreaterThanZero()
                        {
                            if (_calculatedPrice <= 0) throw new Exception("Basket price is less than zero: " + _calculatedPrice );
                        }

                        [Then(@"the basket price should be �(.*)")]
                        public void ThenTheBasketPriceShouldBe(decimal expectedPrice)
                        {
                            if(expectedPrice != _calculatedPrice) throw new Exception("Basket price is not as expected: " + _calculatedPrice + " vs " + expectedPrice);
                        }

                    }
                }
                
                """);
            ExecuteTests();

            ShouldAllScenariosPass();

        }

        [TestMethod]
        [DataRow("attachments")]
        [DataRow("minimal")]
        [DataRow("cdata")]
        [DataRow("pending")]
        [DataRow("examples-tables")]
        [DataRow("hooks")]
        [DataRow("data-tables")]
        [DataRow("parameter-types")]
        [DataRow("skipped")]
        [DataRow("undefined")]
        [DataRow("unknown-parameter-type")]
        [DataRow("rules")]
        public void CCKScenarios(string scenarioName)
        {
            AddCucumberMessagePlugIn();
            CucumberMessagesAddConfigurationFile("CucumberMessages.configuration.json");
            AddUtilClassWithFileSystemPath();

            scenarioName = scenarioName.Replace("-", "_");

            AddFeatureFileFromResource($"{scenarioName}/{scenarioName}.feature", "CucumberMessages.CompatibilityTests.CCK", Assembly.GetExecutingAssembly());
            AddBindingClassFromResource($"{scenarioName}/{scenarioName}.cs", "CucumberMessages.CompatibilityTests.CCK", Assembly.GetExecutingAssembly());
            //AddBinaryFilesFromResource($"{scenarioName}", "CucumberMessages.CompatibilityTests.CCK", Assembly.GetExecutingAssembly());

            ExecuteTests();

            var validator = new CucumberMessagesValidator(GetActualResults(scenarioName).ToList(), GetExpectedResults(scenarioName).ToList());
            validator.ShouldPassBasicStructuralChecks();
            validator.ResultShouldPassBasicSanityChecks();
            validator.ResultShouldPassAllComparisonTests();

            ConfirmAllTestsRan(null);
        }

        private void AddUtilClassWithFileSystemPath()
        {
            string location = AppContext.BaseDirectory;
            AddBindingClass(
                $"public class FileSystemPath {{  public static string GetFilePathForAttachments()  {{  return @\"{location}\\CCK\"; }}  }} ");
        }

        private IEnumerable<Envelope> GetExpectedResults(string scenarioName)
        {
            var workingDirectory = Path.Combine(AppContext.BaseDirectory, "..\\..\\..");
            var expectedJsonText = File.ReadAllLines(Path.Combine(workingDirectory!, "CCK", $"{scenarioName}\\{scenarioName}.feature.ndjson"));

            foreach (var json in expectedJsonText)
            {
                var e = NdjsonSerializer.Deserialize(json);
                yield return e;
            };
        }

        private IEnumerable<Envelope> GetActualResults(string scenarioName)
        {
            var configFileLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "CucumberMessages.configuration.json");
            var config = System.Text.Json.JsonSerializer.Deserialize<FileSinkConfiguration>(File.ReadAllText(configFileLocation), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var resultLocation = config!.Destinations.Where(d => d.Enabled).First().OutputDirectory;
            var basePath = config!.Destinations.Where(d => d.Enabled).First().BasePath;
            var actualJsonText = File.ReadAllLines(Path.Combine(basePath, resultLocation, $"{scenarioName}.ndjson"));

            foreach (var json in actualJsonText) yield return NdjsonSerializer.Deserialize(json);
        }
    }
    internal class FileSinkConfiguration
    {
        public bool FileSinkEnabled { get; set; }
        public List<Destination> Destinations { get; set; }

        public FileSinkConfiguration() : this(true) { }
        public FileSinkConfiguration(bool fileSinkEnabled) : this(fileSinkEnabled, new List<Destination>()) { }
        public FileSinkConfiguration(bool fileSinkEnabled, List<Destination> destinations)
        {
            FileSinkEnabled = fileSinkEnabled;
            Destinations = destinations;
        }
    }

    public class Destination
    {
        public bool Enabled { get; set; }
        public string BasePath { get; set; }
        public string OutputDirectory { get; set; }

        public Destination(bool enabled, string basePath, string outputDirectory)
        {
            Enabled = true;
            BasePath = basePath;
            OutputDirectory = outputDirectory;
        }
    }

}