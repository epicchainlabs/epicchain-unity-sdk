using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using EpicChain.Unity.SDK.Types;
using EpicChain.Unity.SDK.Protocol.Response;
using EpicChain.Unity.SDK.Transaction;
using EpicChain.Unity.SDK.Utils;

namespace EpicChain.Unity.SDK.Core
{
    /// <summary>
    /// Main entry point for EpicChain Unity SDK - implements the complete EpicChain blockchain protocol.
    /// Provides Unity-optimized access to all EpicChain blockchain functionality including
    /// smart contracts, transactions, tokens, and state management.
    /// </summary>
    [System.Serializable]
    public class EpicChainUnity : IEpicChain
    {
        #region Static Instance (Singleton Pattern)
        
        private static EpicChainUnity instance;
        
        /// <summary>
        /// Singleton instance of EpicChainUnity SDK.
        /// Use this to access EpicChain blockchain functionality from anywhere in your Unity project.
        /// </summary>
        public static EpicChainUnity Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EpicChainUnity();
                }
                return instance;
            }
        }
        
        #endregion
        
        #region Private Fields
        
        private EpicChainUnityConfig config;
        private IEpicChainUnityService epicchainService;
        private bool isInitialized = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>Current EpicChain Unity SDK configuration</summary>
        public EpicChainUnityConfig Config => config;
        
        /// <summary>The EpicChain Name Service resolver script hash configured in the config</summary>
        public Hash160 NNSResolver => config?.NNSResolver ?? EpicChainUnityConfig.MAINNET_NNS_CONTRACT_HASH;
        
        /// <summary>The interval in milliseconds in which blocks are produced</summary>
        public int BlockInterval => config?.BlockInterval ?? EpicChainUnityConfig.DEFAULT_BLOCK_TIME;
        
        /// <summary>The interval in milliseconds in which EpicChainUnity should poll the EpicChain node for new block information when observing the blockchain</summary>
        public int PollingInterval => config?.PollingInterval ?? EpicChainUnityConfig.DEFAULT_BLOCK_TIME;
        
        /// <summary>The maximum time in milliseconds that can pass from the construction of a transaction until it gets included in a block</summary>
        public int MaxValidUntilBlockIncrement => config?.MaxValidUntilBlockIncrement ?? (EpicChainUnityConfig.MAX_VALID_UNTIL_BLOCK_INCREMENT_BASE / EpicChainUnityConfig.DEFAULT_BLOCK_TIME);
        
        /// <summary>Whether the SDK has been initialized</summary>
        public bool IsInitialized => isInitialized;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initializes the EpicChain Unity SDK with the specified configuration.
        /// This must be called before using any blockchain functionality.
        /// </summary>
        /// <param name="config">EpicChain Unity configuration (optional - will create default if not provided)</param>
        /// <returns>True if initialization was successful</returns>
        public async Task<bool> Initialize(EpicChainUnityConfig config = null)
        {
            try
            {
                // Use provided config or create default
                this.config = config ?? EpicChainUnityConfig.CreateMainnetConfig();
                
                // Create HTTP service
                epicchainService = new EpicChainUnityHttpService(
                    this.config.NodeUrl,
                    includeRawResponses: false,
                    timeoutSeconds: this.config.RequestTimeout,
                    enableDebugLogging: this.config.EnableDebugLogging
                );
                
                // Test connectivity
                await TestConnectivity();
                
                // Auto-fetch network magic if not configured
                if (this.config.NetworkMagic == null)
                {
                    var networkMagic = await GetNetworkMagicNumber();
                    this.config.SetNetworkMagic(networkMagic);
                }
                
                isInitialized = true;
                
                if (this.config.EnableDebugLogging)
                {
                    Debug.Log($"[EpicChainUnity] Successfully initialized with node: {this.config.NodeUrl}");
                    Debug.Log($"[EpicChainUnity] Network Magic: {this.config.NetworkMagic}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EpicChainUnity] Failed to initialize: {ex.Message}");
                isInitialized = false;
                return false;
            }
        }
        
        /// <summary>
        /// Tests connectivity to the configured EpicChain RPC node.
        /// </summary>
        /// <returns>True if connection is successful</returns>
        private async Task<bool> TestConnectivity()
        {
            try
            {
                var response = await GetVersion().SendAsync();
                var result = response.GetResult();
                
                if (config.EnableDebugLogging)
                {
                    Debug.Log($"[EpicChainUnity] Connected to EpicChain node: {result.UserAgent} (Protocol: {result.Protocol.Network})");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                throw new EpicChainUnityException($"Failed to connect to EpicChain node at {config.NodeUrl}: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Creates a new EpicChainUnity instance with the specified service and configuration.
        /// </summary>
        /// <param name="epicchainService">EpicChain RPC service implementation</param>
        /// <param name="config">EpicChain Unity configuration</param>
        /// <returns>New EpicChainUnity instance</returns>
        public static EpicChainUnity Build(IEpicChainUnityService epicchainService, EpicChainUnityConfig config = null)
        {
            var epicchain = new EpicChainUnity();
            epicchain.config = config ?? EpicChainUnityConfig.CreateMainnetConfig();
            epicchain.epicchainService = epicchainService ?? throw new ArgumentNullException(nameof(epicchainService));
            epicchain.isInitialized = true;
            return epicchain;
        }
        
        #endregion
        
        #region Configuration Methods
        
        /// <summary>Allow the transmission of scripts that lead to a VM fault</summary>
        public void AllowTransmissionOnFault()
        {
            config?.AllowTransmissionOnFault();
        }
        
        /// <summary>Prevent the transmission of scripts that lead to a VM fault (default behavior)</summary>
        public void PreventTransmissionOnFault()
        {
            config?.PreventTransmissionOnFault();
        }
        
        /// <summary>Sets the EpicChain Name Service script hash that should be used to resolve NNS domain names</summary>
        /// <param name="nnsResolver">The NNS resolver script hash</param>
        public void SetNNSResolver(Hash160 nnsResolver)
        {
            config?.SetNNSResolver(nnsResolver);
        }
        
        #endregion
        
        #region Network Magic Methods
        
        /// <summary>
        /// Gets the configured network magic number as bytes.
        /// The magic number is an ingredient when generating the hash of a transaction.
        /// Only once this method is called for the first time the value is fetched from the connected EpicChain node.
        /// </summary>
        /// <returns>The network's magic number as bytes</returns>
        public async Task<byte[]> GetNetworkMagicNumberBytes()
        {
            var magicInt = await GetNetworkMagicNumber();
            return BitConverter.GetBytes((uint)(magicInt & 0xFFFFFFFF));
        }
        
        /// <summary>
        /// Gets the configured network magic number as an integer.
        /// The magic number is an ingredient when generating the hash of a transaction.
        /// Only once this method is called for the first time the value is fetched from the connected EpicChain node.
        /// </summary>
        /// <returns>The network's magic number</returns>
        public async Task<int> GetNetworkMagicNumber()
        {
            if (config.NetworkMagic == null)
            {
                try
                {
                    var versionResponse = await GetVersion().SendAsync();
                    var version = versionResponse.GetResult();
                    
                    if (version.Protocol?.Network == null)
                    {
                        throw new EpicChainUnityException("Unable to read Network Magic Number from Version response");
                    }
                    
                    config.SetNetworkMagic(version.Protocol.Network.Value);
                }
                catch (Exception ex)
                {
                    throw new EpicChainUnityException($"Failed to fetch network magic number: {ex.Message}", ex);
                }
            }
            
            return config.NetworkMagic.Value;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Throws an exception if the SDK is not initialized.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!isInitialized)
            {
                throw new EpicChainUnityException("EpicChainUnity SDK must be initialized before use. Call Initialize() first.");
            }
        }
        
        /// <summary>
        /// Creates a new request with the specified method and parameters.
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <typeparam name="TResult">Result type</typeparam>
        /// <param name="method">RPC method name</param>
        /// <param name="parameters">Method parameters</param>
        /// <returns>New request object</returns>
        private Request<TResponse, TResult> CreateRequest<TResponse, TResult>(string method, params object[] parameters)
            where TResponse : IResponse<TResult>, new()
        {
            EnsureInitialized();
            return new Request<TResponse, TResult>(method, parameters, epicchainService);
        }
        
        #endregion
        
        #region IEpicChain Implementation - Blockchain Methods
        
        public Request<EpicChainBlockHashResponse, Hash256> GetBestBlockHash()
        {
            return CreateRequest<EpicChainBlockHashResponse, Hash256>("getbestblockhash");
        }
        
        public Request<EpicChainBlockHashResponse, Hash256> GetBlockHash(int blockIndex)
        {
            return CreateRequest<EpicChainBlockHashResponse, Hash256>("getblockhash", blockIndex);
        }
        
        public Request<EpicChainGetBlockResponse, EpicChainBlock> GetBlock(Hash256 blockHash, bool returnFullTransactionObjects)
        {
            if (returnFullTransactionObjects)
            {
                return CreateRequest<EpicChainGetBlockResponse, EpicChainBlock>("getblock", blockHash.ToString(), 1);
            }
            else
            {
                return GetBlockHeader(blockHash);
            }
        }
        
        public Request<EpicChainGetBlockResponse, EpicChainBlock> GetBlock(int blockIndex, bool returnFullTransactionObjects)
        {
            if (returnFullTransactionObjects)
            {
                return CreateRequest<EpicChainGetBlockResponse, EpicChainBlock>("getblock", blockIndex, 1);
            }
            else
            {
                return GetBlockHeader(blockIndex);
            }
        }
        
        public Request<EpicChainGetRawBlockResponse, string> GetRawBlock(Hash256 blockHash)
        {
            return CreateRequest<EpicChainGetRawBlockResponse, string>("getblock", blockHash.ToString(), 0);
        }
        
        public Request<EpicChainGetRawBlockResponse, string> GetRawBlock(int blockIndex)
        {
            return CreateRequest<EpicChainGetRawBlockResponse, string>("getblock", blockIndex, 0);
        }
        
        public Request<EpicChainBlockHeaderCountResponse, int> GetBlockHeaderCount()
        {
            return CreateRequest<EpicChainBlockHeaderCountResponse, int>("getblockheadercount");
        }
        
        public Request<EpicChainBlockCountResponse, int> GetBlockCount()
        {
            return CreateRequest<EpicChainBlockCountResponse, int>("getblockcount");
        }
        
        public Request<EpicChainGetBlockResponse, EpicChainBlock> GetBlockHeader(Hash256 blockHash)
        {
            return CreateRequest<EpicChainGetBlockResponse, EpicChainBlock>("getblockheader", blockHash.ToString(), 1);
        }
        
        public Request<EpicChainGetBlockResponse, EpicChainBlock> GetBlockHeader(int blockIndex)
        {
            return CreateRequest<EpicChainGetBlockResponse, EpicChainBlock>("getblockheader", blockIndex, 1);
        }
        
        public Request<EpicChainGetRawBlockResponse, string> GetRawBlockHeader(Hash256 blockHash)
        {
            return CreateRequest<EpicChainGetRawBlockResponse, string>("getblockheader", blockHash.ToString(), 0);
        }
        
        public Request<EpicChainGetRawBlockResponse, string> GetRawBlockHeader(int blockIndex)
        {
            return CreateRequest<EpicChainGetRawBlockResponse, string>("getblockheader", blockIndex, 0);
        }
        
        public Request<EpicChainGetNativeContractsResponse, List<NativeContractState>> GetNativeContracts()
        {
            return CreateRequest<EpicChainGetNativeContractsResponse, List<NativeContractState>>("getnativecontracts");
        }
        
        public Request<EpicChainGetContractStateResponse, ContractState> GetContractState(Hash160 contractHash)
        {
            return CreateRequest<EpicChainGetContractStateResponse, ContractState>("getcontractstate", contractHash.ToString());
        }
        
        public Request<EpicChainGetContractStateResponse, ContractState> GetNativeContractState(string contractName)
        {
            return CreateRequest<EpicChainGetContractStateResponse, ContractState>("getcontractstate", contractName);
        }
        
        public Request<EpicChainGetMemPoolResponse, EpicChainGetMemPoolResponse.MemPoolDetails> GetMemPool()
        {
            return CreateRequest<EpicChainGetMemPoolResponse, EpicChainGetMemPoolResponse.MemPoolDetails>("getrawmempool", 1);
        }
        
        public Request<EpicChainGetRawMemPoolResponse, List<Hash256>> GetRawMemPool()
        {
            return CreateRequest<EpicChainGetRawMemPoolResponse, List<Hash256>>("getrawmempool");
        }
        
        public Request<EpicChainGetTransactionResponse, EpicChainTransaction> GetTransaction(Hash256 txHash)
        {
            return CreateRequest<EpicChainGetTransactionResponse, EpicChainTransaction>("getrawtransaction", txHash.ToString(), 1);
        }
        
        public Request<EpicChainGetRawTransactionResponse, string> GetRawTransaction(Hash256 txHash)
        {
            return CreateRequest<EpicChainGetRawTransactionResponse, string>("getrawtransaction", txHash.ToString(), 0);
        }
        
        public Request<EpicChainGetStorageResponse, string> GetStorage(Hash160 contractHash, string keyHexString)
        {
            return CreateRequest<EpicChainGetStorageResponse, string>("getstorage", contractHash.ToString(), Convert.ToBase64String(Convert.FromHexString(keyHexString)));
        }
        
        public Request<EpicChainGetTransactionHeightResponse, int> GetTransactionHeight(Hash256 txHash)
        {
            return CreateRequest<EpicChainGetTransactionHeightResponse, int>("gettransactionheight", txHash.ToString());
        }
        
        public Request<EpicChainGetNextBlockValidatorsResponse, List<EpicChainGetNextBlockValidatorsResponse.Validator>> GetNextBlockValidators()
        {
            return CreateRequest<EpicChainGetNextBlockValidatorsResponse, List<EpicChainGetNextBlockValidatorsResponse.Validator>>("getnextblockvalidators");
        }
        
        public Request<EpicChainGetCommitteeResponse, List<string>> GetCommittee()
        {
            return CreateRequest<EpicChainGetCommitteeResponse, List<string>>("getcommittee");
        }
        
        #endregion
        
        #region IEpicChain Implementation - Node Methods
        
        public Request<EpicChainConnectionCountResponse, int> GetConnectionCount()
        {
            return CreateRequest<EpicChainConnectionCountResponse, int>("getconnectioncount");
        }
        
        public Request<EpicChainGetPeersResponse, EpicChainGetPeersResponse.Peers> GetPeers()
        {
            return CreateRequest<EpicChainGetPeersResponse, EpicChainGetPeersResponse.Peers>("getpeers");
        }
        
        public Request<EpicChainGetVersionResponse, EpicChainGetVersionResponse.EpicChainVersion> GetVersion()
        {
            return CreateRequest<EpicChainGetVersionResponse, EpicChainGetVersionResponse.EpicChainVersion>("getversion");
        }
        
        public Request<EpicChainSendRawTransactionResponse, EpicChainSendRawTransactionResponse.RawTransaction> SendRawTransaction(string rawTransactionHex)
        {
            return CreateRequest<EpicChainSendRawTransactionResponse, EpicChainSendRawTransactionResponse.RawTransaction>("sendrawtransaction", Convert.ToBase64String(Convert.FromHexString(rawTransactionHex)));
        }
        
        public Request<EpicChainSubmitBlockResponse, bool> SubmitBlock(string serializedBlockAsHex)
        {
            return CreateRequest<EpicChainSubmitBlockResponse, bool>("submitblock", serializedBlockAsHex);
        }
        
        #endregion
        
        // Note: Additional method implementations for Smart Contract, Token Tracker, Application Logs, and State Service methods
        // will be implemented in the next phase to maintain file size manageable.
        
        #region IEpicChain Implementation - Smart Contract Methods (Partial Implementation)
        
        public Request<EpicChainInvokeFunctionResponse, InvocationResult> InvokeFunction(Hash160 contractHash, string functionName, List<Signer> signers = null)
        {
            return InvokeFunction(contractHash, functionName, new List<ContractParameter>(), signers);
        }
        
        public Request<EpicChainInvokeFunctionResponse, InvocationResult> InvokeFunction(Hash160 contractHash, string functionName, List<ContractParameter> contractParams, List<Signer> signers = null)
        {
            var signerParams = signers?.ConvertAll(s => new TransactionSigner(s)) ?? new List<TransactionSigner>();
            return CreateRequest<EpicChainInvokeFunctionResponse, InvocationResult>("invokefunction", contractHash.ToString(), functionName, contractParams ?? new List<ContractParameter>(), signerParams);
        }
        
        public Request<EpicChainInvokeFunctionResponse, InvocationResult> InvokeFunctionDiagnostics(Hash160 contractHash, string functionName, List<Signer> signers = null)
        {
            return InvokeFunctionDiagnostics(contractHash, functionName, new List<ContractParameter>(), signers);
        }
        
        public Request<EpicChainInvokeFunctionResponse, InvocationResult> InvokeFunctionDiagnostics(Hash160 contractHash, string functionName, List<ContractParameter> contractParams, List<Signer> signers = null)
        {
            var signerParams = signers?.ConvertAll(s => new TransactionSigner(s)) ?? new List<TransactionSigner>();
            return CreateRequest<EpicChainInvokeFunctionResponse, InvocationResult>("invokefunction", contractHash.ToString(), functionName, contractParams ?? new List<ContractParameter>(), signerParams, true);
        }
        
        public Request<EpicChainInvokeScriptResponse, InvocationResult> InvokeScript(string scriptHex, List<Signer> signers = null)
        {
            var signerParams = signers?.ConvertAll(s => new TransactionSigner(s)) ?? new List<TransactionSigner>();
            return CreateRequest<EpicChainInvokeScriptResponse, InvocationResult>("invokescript", Convert.ToBase64String(Convert.FromHexString(scriptHex)), signerParams);
        }
        
        public Request<EpicChainInvokeScriptResponse, InvocationResult> InvokeScriptDiagnostics(string scriptHex, List<Signer> signers = null)
        {
            var signerParams = signers?.ConvertAll(s => new TransactionSigner(s)) ?? new List<TransactionSigner>();
            return CreateRequest<EpicChainInvokeScriptResponse, InvocationResult>("invokescript", Convert.ToBase64String(Convert.FromHexString(scriptHex)), signerParams, true);
        }
        
        public Request<EpicChainTraverseIteratorResponse, List<StackItem>> TraverseIterator(string sessionId, string iteratorId, int count)
        {
            return CreateRequest<EpicChainTraverseIteratorResponse, List<StackItem>>("traverseiterator", sessionId, iteratorId, count);
        }
        public Request<EpicChainTerminateSessionResponse, bool> TerminateSession(string sessionId)
        {
            return CreateRequest<EpicChainTerminateSessionResponse, bool>("terminatesession", sessionId);
        }
        public Request<EpicChainInvokeContractVerifyResponse, InvocationResult> InvokeContractVerify(Hash160 contractHash, List<ContractParameter> methodParameters = null, List<Signer> signers = null)
        {
            var signerParams = signers?.ConvertAll(s => new TransactionSigner(s)) ?? new List<TransactionSigner>();
            return CreateRequest<EpicChainInvokeContractVerifyResponse, InvocationResult>("invokecontractverify", contractHash.ToString(), methodParameters ?? new List<ContractParameter>(), signerParams);
        }
        public Request<EpicChainGetUnclaimedGasResponse, EpicChainGetUnclaimedGasResponse.GetUnclaimedGas> GetUnclaimedGas(Hash160 scriptHash)
        {
            return CreateRequest<EpicChainGetUnclaimedGasResponse, EpicChainGetUnclaimedGasResponse.GetUnclaimedGas>("getunclaimedgas", scriptHash.ToAddress());
        }
        public Request<EpicChainListPluginsResponse, List<EpicChainListPluginsResponse.Plugin>> ListPlugins()
        {
            return CreateRequest<EpicChainListPluginsResponse, List<EpicChainListPluginsResponse.Plugin>>("listplugins");
        }
        public Request<EpicChainValidateAddressResponse, EpicChainValidateAddressResponse.Result> ValidateAddress(string address)
        {
            return CreateRequest<EpicChainValidateAddressResponse, EpicChainValidateAddressResponse.Result>("validateaddress", address);
        }
        public Request<EpicChainGetXep17BalancesResponse, EpicChainGetXep17BalancesResponse.Xep17Balances> GetXep17Balances(Hash160 scriptHash)
        {
            return CreateRequest<EpicChainGetXep17BalancesResponse, EpicChainGetXep17BalancesResponse.Xep17Balances>("GetXep17balances", scriptHash.ToAddress());
        }
        public Request<EpicChainGetXep17TransfersResponse, EpicChainGetXep17TransfersResponse.Xep17Transfers> GetXep17Transfers(Hash160 scriptHash)
        {
            return CreateRequest<EpicChainGetXep17TransfersResponse, EpicChainGetXep17TransfersResponse.Xep17Transfers>("GetXep17transfers", scriptHash.ToAddress());
        }
        public Request<EpicChainGetXep17TransfersResponse, EpicChainGetXep17TransfersResponse.Xep17Transfers> GetXep17Transfers(Hash160 scriptHash, DateTime from)
        {
            var fromTimestamp = ((DateTimeOffset)from).ToUnixTimeMilliseconds();
            return CreateRequest<EpicChainGetXep17TransfersResponse, EpicChainGetXep17TransfersResponse.Xep17Transfers>("GetXep17transfers", scriptHash.ToAddress(), fromTimestamp);
        }
        public Request<EpicChainGetXep17TransfersResponse, EpicChainGetXep17TransfersResponse.Xep17Transfers> GetXep17Transfers(Hash160 scriptHash, DateTime from, DateTime to)
        {
            var fromTimestamp = ((DateTimeOffset)from).ToUnixTimeMilliseconds();
            var toTimestamp = ((DateTimeOffset)to).ToUnixTimeMilliseconds();
            return CreateRequest<EpicChainGetXep17TransfersResponse, EpicChainGetXep17TransfersResponse.Xep17Transfers>("GetXep17transfers", scriptHash.ToAddress(), fromTimestamp, toTimestamp);
        }
        public Request<EpicChainGetXep11BalancesResponse, EpicChainGetXep11BalancesResponse.Xep11Balances> GetXep11Balances(Hash160 scriptHash)
        {
            return CreateRequest<EpicChainGetXep11BalancesResponse, EpicChainGetXep11BalancesResponse.Xep11Balances>("GetXep11balances", scriptHash.ToAddress());
        }
        public Request<EpicChainGetXep11TransfersResponse, EpicChainGetXep11TransfersResponse.Xep11Transfers> GetXep11Transfers(Hash160 scriptHash)
        {
            return CreateRequest<EpicChainGetXep11TransfersResponse, EpicChainGetXep11TransfersResponse.Xep11Transfers>("GetXep11transfers", scriptHash.ToAddress());
        }
        public Request<EpicChainGetXep11TransfersResponse, EpicChainGetXep11TransfersResponse.Xep11Transfers> GetXep11Transfers(Hash160 scriptHash, DateTime from)
        {
            var fromTimestamp = ((DateTimeOffset)from).ToUnixTimeMilliseconds();
            return CreateRequest<EpicChainGetXep11TransfersResponse, EpicChainGetXep11TransfersResponse.Xep11Transfers>("GetXep11transfers", scriptHash.ToAddress(), fromTimestamp);
        }
        public Request<EpicChainGetXep11TransfersResponse, EpicChainGetXep11TransfersResponse.Xep11Transfers> GetXep11Transfers(Hash160 scriptHash, DateTime from, DateTime to)
        {
            var fromTimestamp = ((DateTimeOffset)from).ToUnixTimeMilliseconds();
            var toTimestamp = ((DateTimeOffset)to).ToUnixTimeMilliseconds();
            return CreateRequest<EpicChainGetXep11TransfersResponse, EpicChainGetXep11TransfersResponse.Xep11Transfers>("GetXep11transfers", scriptHash.ToAddress(), fromTimestamp, toTimestamp);
        }
        public Request<EpicChainGetXep11PropertiesResponse, Dictionary<string, string>> GetXep11Properties(Hash160 scriptHash, string tokenId)
        {
            return CreateRequest<EpicChainGetXep11PropertiesResponse, Dictionary<string, string>>("GetXep11properties", scriptHash.ToAddress(), tokenId);
        }
        public Request<EpicChainGetApplicationLogResponse, EpicChainApplicationLog> GetApplicationLog(Hash256 txHash)
        {
            return CreateRequest<EpicChainGetApplicationLogResponse, EpicChainApplicationLog>("getapplicationlog", txHash.ToString());
        }
        public Request<EpicChainGetStateRootResponse, EpicChainGetStateRootResponse.StateRoot> GetStateRoot(int blockIndex)
        {
            return CreateRequest<EpicChainGetStateRootResponse, EpicChainGetStateRootResponse.StateRoot>("getstateroot", blockIndex);
        }
        public Request<EpicChainGetProofResponse, string> GetProof(Hash256 rootHash, Hash160 contractHash, string storageKeyHex)
        {
            return CreateRequest<EpicChainGetProofResponse, string>("getproof", rootHash.ToString(), contractHash.ToString(), Convert.ToBase64String(Convert.FromHexString(storageKeyHex)));
        }
        public Request<EpicChainVerifyProofResponse, string> VerifyProof(Hash256 rootHash, string proofDataHex)
        {
            return CreateRequest<EpicChainVerifyProofResponse, string>("verifyproof", rootHash.ToString(), Convert.ToBase64String(Convert.FromHexString(proofDataHex)));
        }
        public Request<EpicChainGetStateHeightResponse, EpicChainGetStateHeightResponse.StateHeight> GetStateHeight()
        {
            return CreateRequest<EpicChainGetStateHeightResponse, EpicChainGetStateHeightResponse.StateHeight>("getstateheight");
        }
        public Request<EpicChainGetStateResponse, string> GetState(Hash256 rootHash, Hash160 contractHash, string keyHex)
        {
            return CreateRequest<EpicChainGetStateResponse, string>("getstate", rootHash.ToString(), contractHash.ToString(), Convert.ToBase64String(Convert.FromHexString(keyHex)));
        }
        public Request<EpicChainFindStatesResponse, EpicChainFindStatesResponse.States> FindStates(Hash256 rootHash, Hash160 contractHash, string keyPrefixHex, string startKeyHex = null, int? countFindResultItems = null)
        {
            var parameters = new List<object> { rootHash.ToString(), contractHash.ToString(), Convert.ToBase64String(Convert.FromHexString(keyPrefixHex)) };
            
            if (!string.IsNullOrEmpty(startKeyHex))
            {
                parameters.Add(Convert.ToBase64String(Convert.FromHexString(startKeyHex)));
            }
            
            if (countFindResultItems.HasValue)
            {
                if (string.IsNullOrEmpty(startKeyHex))
                {
                    parameters.Add("");
                }
                parameters.Add(countFindResultItems.Value);
            }
            
            return CreateRequest<EpicChainFindStatesResponse, EpicChainFindStatesResponse.States>("findstates", parameters.ToArray());
        }
        
        #endregion
    }
}