using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EpicChain.Unity.SDK;
using EpicChain.Unity.SDK.Contracts;
using EpicChain.Unity.SDK.Tests.Helpers;
using EpicChain.Unity.SDK.Models;

namespace EpicChain.Unity.SDK.Tests.Contract
{
    /// <summary>
    /// Unity Test Framework implementation of EpicChain contract tests
    /// Converted from Swift EpicChainTests.swift with Unity-specific enhancements
    /// </summary>
    [TestFixture]
    public class EpicChainTests
    {
        private const string EPICCHAINTOKEN_SCRIPTHASH = "ef4073a0f2b305a38ec4050e4d3d28bc40ea63f5";
        private const string VOTE = "vote";
        private const string REGISTER_CANDIDATE = "registerCandidate";
        private const string UNREGISTER_CANDIDATE = "unregisterCandidate";
        private const string GET_GAS_PER_BLOCK = "getGasPerBlock";
        private const string SET_GAS_PER_BLOCK = "setGasPerBlock";
        private const string GET_REGISTER_PRICE = "getRegisterPrice";
        private const string SET_REGISTER_PRICE = "setRegisterPrice";
        private const string GET_ACCOUNT_STATE = "getAccountState";

        private Account account1;
        private MockEpicChainSwift mockEpicChainSwift;
        private EpicChainToken epicchainToken;

        [SetUp]
        public void SetUp()
        {
            account1 = TestHelpers.CreateTestAccount("e6e919577dd7b8e97805151c05ae07ff4f752654d6d8797597aca989c02c4cb3");
            mockEpicChainSwift = new MockEpicChainSwift();
            epicchainToken = new EpicChainToken(mockEpicChainSwift);
        }

        [TearDown]
        public void TearDown()
        {
            mockEpicChainSwift?.Dispose();
        }

        [Test]
        public async Task TestConstants()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    ["name"] = TestHelpers.LoadJsonResource("invokefunction_name_epicchain"),
                    ["symbol"] = TestHelpers.LoadJsonResource("invokefunction_symbol_epicchain"),
                    ["totalSupply"] = TestHelpers.LoadJsonResource("invokefunction_totalSupply"),
                    ["decimals"] = TestHelpers.LoadJsonResource("invokefunction_decimals")
                });

            // Act
            var name = await EpicChainTokenGetNameAsync();
            var symbol = await EpicChainTokenGetSymbolAsync();
            var totalSupply = await EpicChainTokenGetTotalSupplyAsync();
            var decimals = await EpicChainTokenGetDecimalsAsync();

            // Assert
            Assert.AreEqual("EpicChain", name);
            Assert.AreEqual("XPR", symbol);
            Assert.AreEqual(1_000_000_000, totalSupply);
            Assert.AreEqual(0, decimals);
            Assert.AreEqual(EPICCHAINTOKEN_SCRIPTHASH, EpicChainTokenScriptHash.ToString());
        }

        [Test]
        public async Task TestRegisterCandidate()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockResponses(new Dictionary<string, string>
                {
                    ["invokescript"] = TestHelpers.LoadJsonResource("invokescript_registercandidate"),
                    ["getblockcount"] = TestHelpers.LoadJsonResource("getblockcount_1000")
                });

            var pubKeyBytes = account1.KeyPair.PublicKey.GetEncoded(compressed: true);
            var expectedScript = new ScriptBuilder()
                .ContractCall(EpicChainTokenSCRIPT_HASH, REGISTER_CANDIDATE, new ContractParameter[]
                {
                    ContractParameter.PublicKey(pubKeyBytes)
                })
                .ToArray();

            // Act
            var builder = await epicchainToken
                .RegisterCandidateAsync(account1.KeyPair.PublicKey)
                .ConfigureAwait(false);

            builder.Signers(new AccountSigner[] { AccountSigner.Global(account1) });

            // Assert
            Assert.AreEqual(account1.GetScriptHash(), builder.Signers[0].SignerHash);
            TestHelpers.AssertBytesEqual(expectedScript, builder.Script);
            Assert.IsTrue(builder.Signers[0].Scopes.HasFlag(WitnessScope.Global));
        }

        [Test]
        public async Task TestUnregisterCandidate()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockResponses(new Dictionary<string, string>
                {
                    ["invokescript"] = TestHelpers.LoadJsonResource("invokescript_unregistercandidate"),
                    ["getblockcount"] = TestHelpers.LoadJsonResource("getblockcount_1000")
                });

            var pubKeyBytes = account1.KeyPair.PublicKey.GetEncoded(compressed: true);
            var expectedScript = new ScriptBuilder()
                .ContractCall(EpicChainTokenSCRIPT_HASH, UNREGISTER_CANDIDATE, new ContractParameter[]
                {
                    ContractParameter.PublicKey(pubKeyBytes)
                })
                .ToArray();

            // Act
            var builder = await epicchainToken
                .UnregisterCandidateAsync(account1.KeyPair.PublicKey)
                .ConfigureAwait(false);

            builder.Signers(new AccountSigner[] { AccountSigner.Global(account1) });

            // Assert
            Assert.AreEqual(account1.GetScriptHash(), builder.Signers[0].SignerHash);
            TestHelpers.AssertBytesEqual(expectedScript, builder.Script);
            Assert.IsTrue(builder.Signers[0].Scopes.HasFlag(WitnessScope.Global));
        }

        [Test]
        public async Task TestGetCandidates()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    ["getCandidates"] = TestHelpers.LoadJsonResource("invokefunction_getcandidates")
                });

            // Act
            var result = await EpicChainTokenGetCandidatesAsync();

            // Assert
            Assert.AreEqual(2, result.Count);
            foreach (var candidate in result)
            {
                Assert.IsNotNull(candidate.PublicKey);
                Assert.AreEqual(0, candidate.Votes);
            }
        }

        [Test]
        public async Task TestIsCandidate()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    ["getCandidates"] = TestHelpers.LoadJsonResource("invokefunction_getcandidates")
                });

            var pubKey = new ECPublicKey("02c0b60c995bc092e866f15a37c176bb59b7ebacf069ba94c0ebf561cb8f956238");

            // Act
            var isCandidate = await EpicChainTokenIsCandidateAsync(pubKey);

            // Assert
            Assert.IsTrue(isCandidate);
        }

        [Test]
        public async Task TestGetAllCandidatesIterator()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockResponses(new Dictionary<string, string>
                {
                    ["terminatesession"] = TestHelpers.LoadJsonResource("terminatesession"),
                    ["traverseiterator"] = TestHelpers.LoadJsonResource("epicchain_getAllCandidates_traverseiterator")
                })
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    ["getAllCandidates"] = TestHelpers.LoadJsonResource("invokefunction_iterator_session")
                });

            // Act
            var iterator = await EpicChainTokenGetAllCandidatesIteratorAsync();
            var candidates = await iterator.TraverseAsync(2);

            // Assert
            Assert.AreEqual(2, candidates.Count);

            var expectedCandidate1 = new Candidate
            {
                PublicKey = new ECPublicKey("02607a38b8010a8f401c25dd01df1b74af1827dd16b821fc07451f2ef7f02da60f"),
                Votes = 340_356
            };
            var expectedCandidate2 = new Candidate
            {
                PublicKey = new ECPublicKey("037279f3a507817251534181116cb38ef30468b25074827db34cbbc6adc8873932"),
                Votes = 10_000_000
            };

            Assert.AreEqual(expectedCandidate1.PublicKey, candidates[0].PublicKey);
            Assert.AreEqual(expectedCandidate1.Votes, candidates[0].Votes);
            Assert.AreEqual(expectedCandidate2.PublicKey, candidates[1].PublicKey);
            Assert.AreEqual(expectedCandidate2.Votes, candidates[1].Votes);

            await iterator.TerminateSessionAsync();
        }

        [Test]
        public async Task TestGetCandidateVotes()
        {
            // Arrange
            var keyPair = ECKeyPair.CreateEcKeyPair();
            mockEpicChainSwift
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    ["getCandidateVote"] = TestHelpers.LoadJsonResource("invokefunction_getCandidateVote")
                });

            // Act
            var votes = await EpicChainTokenGetCandidateVotesAsync(keyPair.PublicKey);

            // Assert
            Assert.AreEqual(721_978, votes);
        }

        [Test]
        public async Task TestVote()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockResponses(new Dictionary<string, string>
                {
                    ["invokescript"] = TestHelpers.LoadJsonResource("invokescript_vote"),
                    ["getblockcount"] = TestHelpers.LoadJsonResource("getblockcount_1000")
                })
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    ["getCandidates"] = TestHelpers.LoadJsonResource("invokefunction_getcandidates")
                });

            var pubKey = account1.KeyPair.PublicKey.GetEncoded(compressed: true);
            var expectedScript = new ScriptBuilder()
                .ContractCall(EpicChainTokenSCRIPT_HASH, VOTE, new ContractParameter[]
                {
                    ContractParameter.Hash160(account1.GetScriptHash()),
                    ContractParameter.PublicKey(pubKey)
                })
                .ToArray();

            // Act
            var builder = await epicchainToken
                .VoteAsync(account1, new ECPublicKey(pubKey))
                .ConfigureAwait(false);

            builder.Signers(new AccountSigner[] { AccountSigner.Global(account1) });

            // Assert
            TestHelpers.AssertBytesEqual(expectedScript, builder.Script);
        }

        [Test]
        public async Task TestCancelVote()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockResponses(new Dictionary<string, string>
                {
                    ["invokescript"] = TestHelpers.LoadJsonResource("invokescript_vote"),
                    ["getblockcount"] = TestHelpers.LoadJsonResource("getblockcount_1000")
                })
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    ["getCandidates"] = TestHelpers.LoadJsonResource("invokefunction_getcandidates")
                });

            var expectedScript = new ScriptBuilder()
                .ContractCall(EpicChainTokenSCRIPT_HASH, VOTE, new ContractParameter[]
                {
                    ContractParameter.Hash160(account1.GetScriptHash()),
                    ContractParameter.Any(null)
                })
                .ToArray();

            // Act
            var builder = await epicchainToken
                .CancelVoteAsync(account1)
                .ConfigureAwait(false);

            builder.Signers(new AccountSigner[] { AccountSigner.Global(account1) });

            // Assert
            TestHelpers.AssertBytesEqual(expectedScript, builder.Script);
        }

        [Test]
        public void TestBuildVoteScript()
        {
            // Arrange
            var pubKey = account1.KeyPair.PublicKey;
            var expectedScript = new ScriptBuilder()
                .ContractCall(EpicChainTokenSCRIPT_HASH, VOTE, new ContractParameter[]
                {
                    ContractParameter.Hash160(account1.GetScriptHash()),
                    ContractParameter.PublicKey(pubKey.GetEncoded(compressed: true))
                })
                .ToArray();

            // Act
            var script = EpicChainTokenBuildVoteScript(account1.GetScriptHash(), pubKey);

            // Assert
            TestHelpers.AssertBytesEqual(expectedScript, script);
        }

        [Test]
        public void TestBuildCancelVoteScript()
        {
            // Arrange
            var expectedScript = new ScriptBuilder()
                .ContractCall(EpicChainTokenSCRIPT_HASH, VOTE, new ContractParameter[]
                {
                    ContractParameter.Hash160(account1.GetScriptHash()),
                    ContractParameter.Any(null)
                })
                .ToArray();

            // Act
            var script = EpicChainTokenBuildVoteScript(account1.GetScriptHash(), null);

            // Assert
            TestHelpers.AssertBytesEqual(expectedScript, script);
        }

        [Test]
        public async Task TestGetGasPerBlock()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    [GET_GAS_PER_BLOCK] = TestHelpers.LoadJsonResource("invokefunction_getGasPerBlock")
                });

            // Act
            var epicpulse = await EpicChainTokenGetGasPerBlockAsync();

            // Assert
            Assert.AreEqual(500_000, gas);
        }

        [Test]
        public void TestSetGasPerBlock()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockResponses(new Dictionary<string, string>
                {
                    ["invokescript"] = TestHelpers.LoadJsonResource("invokescript_vote"),
                    ["getblockcount"] = TestHelpers.LoadJsonResource("getblockcount_1000")
                });

            var epicpulse = 10_000;
            var expectedScript = new ScriptBuilder()
                .ContractCall(EpicChainTokenSCRIPT_HASH, SET_GAS_PER_BLOCK, new ContractParameter[]
                {
                    ContractParameter.Integer(epicpulse)
                })
                .ToArray();

            // Act
            var txBuilder = epicchainToken
                .SetGasPerBlock(epicpulse)
                .Signers(new AccountSigner[] { AccountSigner.CalledByEntry(account1) });

            // Assert
            TestHelpers.AssertBytesEqual(expectedScript, txBuilder.Script);
        }

        [Test]
        public async Task TestGetRegisterPrice()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    [GET_REGISTER_PRICE] = TestHelpers.LoadJsonResource("invokefunction_getRegisterPrice")
                });

            // Act
            var price = await EpicChainTokenGetRegisterPriceAsync();

            // Assert
            Assert.AreEqual(100_000_000_000, price);
        }

        [Test]
        public void TestSetRegisterPrice()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockResponses(new Dictionary<string, string>
                {
                    ["invokescript"] = TestHelpers.LoadJsonResource("invokescript_vote"),
                    ["getblockcount"] = TestHelpers.LoadJsonResource("getblockcount_1000")
                });

            var price = 50_000_000_000;
            var expectedScript = new ScriptBuilder()
                .ContractCall(EpicChainTokenSCRIPT_HASH, SET_REGISTER_PRICE, new ContractParameter[]
                {
                    ContractParameter.Integer(price)
                })
                .ToArray();

            // Act
            var txBuilder = epicchainToken
                .SetRegisterPrice(price)
                .Signers(new AccountSigner[] { AccountSigner.CalledByEntry(account1) });

            // Assert
            TestHelpers.AssertBytesEqual(expectedScript, txBuilder.Script);
        }

        [Test]
        public async Task TestGetAccountState()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    [GET_ACCOUNT_STATE] = TestHelpers.LoadJsonResource("epicchainToken_getAccountState")
                });

            // Act
            var epicchainAccountState = await epicchainToken.GetAccountStateAsync(account1.GetScriptHash());

            // Assert
            Assert.AreEqual(20_000, epicchainAccountState.Balance);
            Assert.AreEqual(259, epicchainAccountState.BalanceHeight);

            var expectedPublicKey = new ECPublicKey("037279f3a507817251534181116cb38ef30468b25074827db34cbbc6adc8873932");
            Assert.AreEqual(expectedPublicKey, epicchainAccountState.PublicKey);
        }

        [Test]
        public async Task TestGetAccountState_NoVote()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    [GET_ACCOUNT_STATE] = TestHelpers.LoadJsonResource("epicchainToken_getAccountState_noVote")
                });

            // Act
            var epicchainAccountState = await epicchainToken.GetAccountStateAsync(account1.GetScriptHash());

            // Assert
            Assert.AreEqual(12_000, epicchainAccountState.Balance);
            Assert.AreEqual(820, epicchainAccountState.BalanceHeight);
            Assert.IsNull(epicchainAccountState.PublicKey);
        }

        [Test]
        public async Task TestGetAccountState_NoBalance()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    [GET_ACCOUNT_STATE] = TestHelpers.LoadJsonResource("epicchainToken_getAccountState_noBalance")
                });

            // Act
            var epicchainAccountState = await epicchainToken.GetAccountStateAsync(account1.GetScriptHash());

            // Assert
            Assert.AreEqual(0, epicchainAccountState.Balance);
            Assert.IsNull(epicchainAccountState.BalanceHeight);
            Assert.IsNull(epicchainAccountState.PublicKey);
        }

        #region Unity-Specific Tests

        [Test]
        [Performance]
        public void TestPerformance_GetCandidates()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    ["getCandidates"] = TestHelpers.LoadJsonResource("invokefunction_getcandidates")
                });

            // Act & Assert
            var executionTime = TestHelpers.MeasureExecutionTime(() =>
            {
                _ = epicchainToken.GetCandidatesAsync().Result;
            });

            Assert.Less(executionTime, 1000, "GetCandidates should complete within 1 second");
            Debug.Log($"GetCandidates execution time: {executionTime}ms");
        }

        [Test]
        public void TestMemoryUsage_CreateEpicChainToken()
        {
            // Act & Assert
            var memoryUsage = TestHelpers.MeasureMemoryUsage(() =>
            {
                var token = new EpicChainToken(mockEpicChainSwift);
                // Force garbage collection to measure actual usage
                GC.KeepAlive(token);
            });

            Assert.Less(Math.Abs(memoryUsage), 1024 * 1024, "EpicChain creation should use less than 1MB");
            Debug.Log($"EpicChain memory usage: {memoryUsage} bytes");
        }

        [UnityTest]
        public System.Collections.IEnumerator TestUnityCoroutineCompatibility()
        {
            // Arrange
            mockEpicChainSwift
                .WithMockInvokeFunctions(new Dictionary<string, string>
                {
                    ["name"] = TestHelpers.LoadJsonResource("invokefunction_name_epicchain")
                });

            bool completed = false;
            string result = null;

            // Act
            epicchainToken.GetNameAsync().ContinueWith(task =>
            {
                result = task.Result;
                completed = true;
            });

            // Wait for completion
            yield return new WaitUntil(() => completed);

            // Assert
            Assert.AreEqual("EpicChain", result);
        }

        [Test]
        public void TestSerializationCompatibility()
        {
            // Test that EpicChain can be serialized for Unity Inspector
            var token = new EpicChainToken(mockEpicChainSwift);
            
            // This would be used for Inspector serialization
            var jsonString = JsonUtility.ToJson(token, true);
            Assert.IsNotNull(jsonString);
            
            Debug.Log($"EpicChain serialized: {jsonString}");
        }

        #endregion
    }
}