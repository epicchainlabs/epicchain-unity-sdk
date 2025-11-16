using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EpicChain.Unity.SDK.Core;
using EpicChain.Unity.SDK.Types;

namespace EpicChain.Unity.SDK.Tests.Core
{
    /// <summary>
    /// Unit tests for the main EpicChainUnity SDK class.
    /// Tests initialization, configuration, and basic blockchain operations.
    /// </summary>
    [TestFixture]
    public class EpicChainUnityTests
    {
        private EpicChainUnityConfig testConfig;
        private EpicChainUnity epicchainUnity;
        
        [SetUp]
        public void SetUp()
        {
            // Create test configuration
            testConfig = ScriptableObject.CreateInstance<EpicChainUnityConfig>();
            testConfig.NodeUrl = "https://testnet1-seed.epic-chain.org:20111";
            testConfig.SetNetworkMagic(894710606); // Testnet magic
            testConfig.EnableDebugLogging = false; // Reduce test noise
            
            epicchainUnity = EpicChainUnityInstance;
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testConfig != null)
            {
                Object.DestroyImmediate(testConfig);
                testConfig = null;
            }
        }
        
        #region Initialization Tests
        
        [Test]
        public async Task TestInitialization_WithValidConfig_ShouldSucceed()
        {
            // Act
            var result = await EpicChainUnityInitialize(testConfig);
            
            // Assert
            Assert.IsTrue(result, "Initialization should succeed with valid config");
            Assert.IsTrue(EpicChainUnityIsInitialized, "EpicChainUnity should be initialized");
            Assert.IsNotNull(EpicChainUnityConfig, "Config should be set");
            Assert.AreEqual(testConfig.NodeUrl, EpicChainUnityConfig.NodeUrl, "Node URL should match");
        }
        
        [Test]
        public async Task TestInitialization_WithDefaultConfig_ShouldSucceed()
        {
            // Act
            var result = await EpicChainUnityInitialize();
            
            // Assert
            Assert.IsTrue(result, "Initialization should succeed with default config");
            Assert.IsTrue(EpicChainUnityIsInitialized, "EpicChainUnity should be initialized");
            Assert.IsNotNull(EpicChainUnityConfig, "Config should be created automatically");
        }
        
        [Test]
        public async Task TestNetworkMagicNumber_ShouldFetchFromNode()
        {
            // Arrange
            await EpicChainUnityInitialize(testConfig);
            
            // Act
            var networkMagic = await EpicChainUnityGetNetworkMagicNumber();
            
            // Assert
            Assert.AreEqual(894710606, networkMagic, "Should fetch correct testnet magic number");
        }
        
        [Test]
        public async Task TestNetworkMagicNumberBytes_ShouldReturnCorrectBytes()
        {
            // Arrange
            await EpicChainUnityInitialize(testConfig);
            
            // Act
            var magicBytes = await EpicChainUnityGetNetworkMagicNumberBytes();
            
            // Assert
            Assert.IsNotNull(magicBytes, "Magic bytes should not be null");
            Assert.AreEqual(4, magicBytes.Length, "Magic bytes should be 4 bytes long");
        }
        
        #endregion
        
        #region Configuration Tests
        
        [Test]
        public void TestConfigurationProperties_ShouldReturnCorrectValues()
        {
            // Arrange
            EpicChainUnityInitialize(testConfig).Wait();
            
            // Assert
            Assert.AreEqual(testConfig.BlockInterval, EpicChainUnityBlockInterval, "Block interval should match config");
            Assert.AreEqual(testConfig.PollingInterval, EpicChainUnityPollingInterval, "Polling interval should match config");
            Assert.AreEqual(testConfig.MaxValidUntilBlockIncrement, EpicChainUnityMaxValidUntilBlockIncrement, "Max valid until block should match config");
            Assert.AreEqual(testConfig.XNSResolver, EpicChainUnityXNSResolver, "XNS resolver should match config");
        }
        
        [Test]
        public async Task TestAllowTransmissionOnFault_ShouldUpdateConfig()
        {
            // Arrange
            await EpicChainUnityInitialize(testConfig);
            
            // Act
            EpicChainUnityAllowTransmissionOnFault();
            
            // Assert
            Assert.IsTrue(EpicChainUnityConfig.AllowTransmissionOnFault, "Should allow transmission on fault");
            
            // Act
            EpicChainUnityPreventTransmissionOnFault();
            
            // Assert
            Assert.IsFalse(EpicChainUnityConfig.AllowTransmissionOnFault, "Should prevent transmission on fault");
        }
        
        [Test]
        public async Task TestSetXNSResolver_ShouldUpdateConfig()
        {
            // Arrange
            await EpicChainUnityInitialize(testConfig);
            var newResolver = new Hash160("a46c1e9f936d2967adf5d8ee5c3e2b4b5a7fff3a");
            
            // Act
            EpicChainUnitySetXNSResolver(newResolver);
            
            // Assert
            Assert.AreEqual(newResolver, EpicChainUnityXNSResolver, "XNS resolver should be updated");
        }
        
        #endregion
        
        #region Blockchain Query Tests
        
        [Test]
        public async Task TestGetBestBlockHash_ShouldReturnValidHash()
        {
            // Arrange
            await EpicChainUnityInitialize(testConfig);
            
            // Act
            var response = await EpicChainUnityGetBestBlockHash().SendAsync();
            var blockHash = response.GetResult();
            
            // Assert
            Assert.IsNotNull(blockHash, "Block hash should not be null");
            Assert.AreEqual(64, blockHash.ToString().Length, "Block hash should be 64 characters long");
        }
        
        [Test]
        public async Task TestGetBlockCount_ShouldReturnPositiveNumber()
        {
            // Arrange
            await EpicChainUnityInitialize(testConfig);
            
            // Act
            var response = await EpicChainUnityGetBlockCount().SendAsync();
            var blockCount = response.GetResult();
            
            // Assert
            Assert.Greater(blockCount, 0, "Block count should be positive");
            Assert.Less(blockCount, int.MaxValue, "Block count should be reasonable");
        }
        
        [Test]
        public async Task TestGetVersion_ShouldReturnValidVersion()
        {
            // Arrange
            await EpicChainUnityInitialize(testConfig);
            
            // Act
            var response = await EpicChainUnityGetVersion().SendAsync();
            var version = response.GetResult();
            
            // Assert
            Assert.IsNotNull(version, "Version should not be null");
            Assert.IsNotNull(version.UserAgent, "User agent should not be null");
            Assert.IsNotNull(version.Protocol, "Protocol info should not be null");
            Assert.AreEqual(894710606, version.Protocol.Network, "Should be testnet magic number");
        }
        
        [UnityTest]
        public async Task TestGetBlock_WithValidHash_ShouldReturnBlock()
        {
            // Arrange
            await EpicChainUnityInitialize(testConfig);
            var bestHashResponse = await EpicChainUnityGetBestBlockHash().SendAsync();
            var bestHash = bestHashResponse.GetResult();
            
            // Act
            var response = await EpicChainUnityGetBlock(bestHash, true).SendAsync();
            var block = response.GetResult();
            
            // Assert
            Assert.IsNotNull(block, "Block should not be null");
            Assert.AreEqual(bestHash, block.Hash, "Block hash should match requested hash");
            Assert.IsNotNull(block.Transactions, "Block should have transactions list");
            
            yield return null;
        }
        
        [Test]
        public async Task TestGetConnectionCount_ShouldReturnNonNegative()
        {
            // Arrange
            await EpicChainUnityInitialize(testConfig);
            
            // Act
            var response = await EpicChainUnityGetConnectionCount().SendAsync();
            var connectionCount = response.GetResult();
            
            // Assert
            Assert.GreaterOrEqual(connectionCount, 0, "Connection count should be non-negative");
        }
        
        #endregion
        
        #region Smart Contract Tests
        
        [Test]
        public async Task TestInvokeFunction_GetVersion_ShouldSucceed()
        {
            // Arrange
            await EpicChainUnityInitialize(testConfig);
            var epicchainTokenHash = new Hash160("ef4073a0f2b305a38ec4050e4d3d28bc40ea63f5"); // EpicChain token on testnet
            
            // Act
            var response = await EpicChainUnityInvokeFunction(epicchainTokenHash, "symbol", new List<ContractParameter>()).SendAsync();
            var result = response.GetResult();
            
            // Assert
            Assert.IsNotNull(result, "Invocation result should not be null");
            Assert.IsFalse(result.HasStateFault(), "Invocation should not have fault state");
            Assert.IsNotNull(result.Stack, "Result stack should not be null");
            Assert.Greater(result.Stack.Count, 0, "Result stack should have items");
        }
        
        [Test]
        public async Task TestInvokeScript_EmptyScript_ShouldSucceed()
        {
            // Arrange
            await EpicChainUnityInitialize(testConfig);
            var emptyScript = "0c00"; // PUSH0, RET
            
            // Act
            var response = await EpicChainUnityInvokeScript(emptyScript).SendAsync();
            var result = response.GetResult();
            
            // Assert
            Assert.IsNotNull(result, "Invocation result should not be null");
            Assert.IsFalse(result.HasStateFault(), "Empty script should not fault");
        }
        
        #endregion
        
        #region Error Handling Tests
        
        [Test]
        public void TestInitialization_WithNullConfig_ShouldUseDefault()
        {
            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await EpicChainUnityInitialize(null), "Should not throw with null config");
        }
        
        [Test]
        public async Task TestInvalidRpcCall_ShouldThrowException()
        {
            // Arrange
            await EpicChainUnityInitialize(testConfig);
            var invalidHash = new Hash160("0000000000000000000000000000000000000000");
            
            // Act & Assert
            var ex = Assert.ThrowsAsync<EpicChainUnityException>(async () =>
            {
                var response = await EpicChainUnityGetContractState(invalidHash).SendAsync();
                response.GetResult();
            });
            
            Assert.IsNotNull(ex, "Should throw exception for invalid contract hash");
        }
        
        #endregion
        
        #region Performance Tests
        
        [Test]
        public async Task TestConcurrentRequests_ShouldHandleMultipleRequests()
        {
            // Arrange
            await EpicChainUnityInitialize(testConfig);
            
            // Act
            var tasks = new Task[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = EpicChainUnityGetBlockCount().SendAsync();
            }
            
            var responses = await Task.WhenAll(tasks);
            
            // Assert
            Assert.AreEqual(5, responses.Length, "Should complete all concurrent requests");
            
            foreach (var response in responses)
            {
                Assert.IsNotNull(response, "Each response should not be null");
                Assert.Greater(response.GetResult(), 0, "Each block count should be positive");
            }
        }
        
        #endregion
    }
}