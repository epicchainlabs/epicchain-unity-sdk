using System;
using UnityEngine;
using EpicChain.Unity.SDK.Types;

namespace EpicChain.Unity.SDK.Core
{
    /// <summary>
    /// EpicChain Unity SDK configuration settings as a ScriptableObject for Unity Inspector integration.
    /// Configure blockchain connection parameters, network settings, and SDK behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "EpicChainUnityConfig", menuName = "EpicChain Unity SDK/Configuration", order = 1)]
    public class EpicChainUnityConfig : ScriptableObject
    {
        #region Constants
        
        /// <summary>Default block time interval in milliseconds (15 seconds)</summary>
        public const int DEFAULT_BLOCK_TIME = 15_000;
        
        /// <summary>Default EpicChain address version byte</summary>
        public const byte DEFAULT_ADDRESS_VERSION = 0x35;
        
        /// <summary>Maximum valid until block increment base time in milliseconds (24 hours)</summary>
        public const int MAX_VALID_UNTIL_BLOCK_INCREMENT_BASE = 86_400_000;
        
        /// <summary>Mainnet EpicChainNameService contract hash</summary>
        public static readonly Hash160 MAINNET_XNS_CONTRACT_HASH = new Hash160("50ac1c37690cc2cfc594472833cf57505d5f46de");
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Network Configuration")]
        [SerializeField] 
        [Tooltip("EpicChain RPC node endpoint URL")]
        private string nodeUrl = "https://mainnet1-seed.epic-chain.org:10111";
        
        [SerializeField] 
        [Tooltip("Network magic number (will be auto-fetched if not set)")]
        private int networkMagic = 0;
        
        [SerializeField] 
        [Tooltip("Block interval in milliseconds")]
        private int blockInterval = DEFAULT_BLOCK_TIME;
        
        [SerializeField] 
        [Tooltip("Polling interval for blockchain monitoring in milliseconds")]
        private int pollingInterval = DEFAULT_BLOCK_TIME;
        
        [SerializeField] 
        [Tooltip("Maximum valid until block increment in milliseconds")]
        private int maxValidUntilBlockIncrement = MAX_VALID_UNTIL_BLOCK_INCREMENT_BASE / DEFAULT_BLOCK_TIME;
        
        [Header("Blockchain Settings")]
        [SerializeField] 
        [Tooltip("EpicChain address version byte")]
        private byte addressVersion = DEFAULT_ADDRESS_VERSION;
        
        [SerializeField] 
        [Tooltip("Allow transmission of scripts that result in VM fault")]
        private bool allowTransmissionOnFault = false;
        
        [SerializeField] 
        [Tooltip("EpicChain Name Service resolver contract hash")]
        private string xnsResolver = "50ac1c37690cc2cfc594472833cf57505d5f46de";
        
        [Header("Unity Integration")]
        [SerializeField] 
        [Tooltip("Enable debug logging")]
        private bool enableDebugLogging = true;
        
        [SerializeField] 
        [Tooltip("Request timeout in seconds")]
        private float requestTimeout = 30f;
        
        [SerializeField] 
        [Tooltip("Maximum concurrent requests")]
        private int maxConcurrentRequests = 10;
        
        #endregion
        
        #region Properties
        
        /// <summary>EpicChain RPC node endpoint URL</summary>
        public string NodeUrl 
        { 
            get => nodeUrl; 
            set => nodeUrl = value; 
        }
        
        /// <summary>Network magic number (auto-fetched if not explicitly set)</summary>
        public int? NetworkMagic 
        { 
            get => networkMagic == 0 ? null : networkMagic; 
            set => networkMagic = value ?? 0; 
        }
        
        /// <summary>Block interval in milliseconds</summary>
        public int BlockInterval 
        { 
            get => blockInterval; 
            set => blockInterval = value; 
        }
        
        /// <summary>Polling interval for blockchain monitoring</summary>
        public int PollingInterval 
        { 
            get => pollingInterval; 
            set => pollingInterval = value; 
        }
        
        /// <summary>Maximum valid until block increment</summary>
        public int MaxValidUntilBlockIncrement 
        { 
            get => maxValidUntilBlockIncrement; 
            set => maxValidUntilBlockIncrement = value; 
        }
        
        /// <summary>EpicChain address version byte</summary>
        public byte AddressVersion 
        { 
            get => addressVersion; 
            set => addressVersion = value; 
        }
        
        /// <summary>Allow transmission of scripts that result in VM fault</summary>
        public bool AllowTransmissionOnFault 
        { 
            get => allowTransmissionOnFault; 
            set => allowTransmissionOnFault = value; 
        }
        
        /// <summary>EpicChain Name Service resolver contract hash</summary>
        public Hash160 XNSResolver 
        { 
            get => new Hash160(xnsResolver); 
            set => xnsResolver = value.ToString(); 
        }
        
        /// <summary>Enable debug logging</summary>
        public bool EnableDebugLogging 
        { 
            get => enableDebugLogging; 
            set => enableDebugLogging = value; 
        }
        
        /// <summary>Request timeout in seconds</summary>
        public float RequestTimeout 
        { 
            get => requestTimeout; 
            set => requestTimeout = value; 
        }
        
        /// <summary>Maximum concurrent requests</summary>
        public int MaxConcurrentRequests 
        { 
            get => maxConcurrentRequests; 
            set => maxConcurrentRequests = value; 
        }
        
        #endregion
        
        #region Fluent Configuration Methods
        
        /// <summary>
        /// Sets the polling interval for blockchain monitoring.
        /// </summary>
        /// <param name="pollingInterval">Polling interval in milliseconds</param>
        /// <returns>This configuration instance for method chaining</returns>
        public EpicChainUnityConfig SetPollingInterval(int pollingInterval)
        {
            this.pollingInterval = pollingInterval;
            return this;
        }
        
        /// <summary>
        /// Sets the network magic number.
        /// </summary>
        /// <param name="magic">Network magic number (must fit in 32-bit unsigned integer)</param>
        /// <returns>This configuration instance for method chaining</returns>
        /// <exception cref="ArgumentException">If magic number is invalid</exception>
        public EpicChainUnityConfig SetNetworkMagic(int magic)
        {
            if (magic < 0 || magic > 0xFFFFFFFF)
            {
                throw new ArgumentException("Network magic number must fit into a 32-bit unsigned integer, i.e., it must be positive and not greater than 0xFFFFFFFF.");
            }
            this.networkMagic = magic;
            return this;
        }
        
        /// <summary>
        /// Sets the block interval.
        /// </summary>
        /// <param name="blockInterval">Block interval in milliseconds</param>
        /// <returns>This configuration instance for method chaining</returns>
        public EpicChainUnityConfig SetBlockInterval(int blockInterval)
        {
            this.blockInterval = blockInterval;
            return this;
        }
        
        /// <summary>
        /// Sets the maximum valid until block increment.
        /// </summary>
        /// <param name="maxValidUntilBlockIncrement">Maximum valid until block increment in milliseconds</param>
        /// <returns>This configuration instance for method chaining</returns>
        public EpicChainUnityConfig SetMaxValidUntilBlockIncrement(int maxValidUntilBlockIncrement)
        {
            this.maxValidUntilBlockIncrement = maxValidUntilBlockIncrement;
            return this;
        }
        
        /// <summary>
        /// Sets the EpicChain Name Service resolver contract hash.
        /// </summary>
        /// <param name="xnsResolver">XNS resolver contract hash</param>
        /// <returns>This configuration instance for method chaining</returns>
        public EpicChainUnityConfig SetXNSResolver(Hash160 xnsResolver)
        {
            this.xnsResolver = xnsResolver.ToString();
            return this;
        }
        
        /// <summary>
        /// Allow transmission of scripts that result in VM fault.
        /// </summary>
        /// <returns>This configuration instance for method chaining</returns>
        public EpicChainUnityConfig AllowTransmissionOnFault()
        {
            this.allowTransmissionOnFault = true;
            return this;
        }
        
        /// <summary>
        /// Prevent transmission of scripts that result in VM fault (default behavior).
        /// </summary>
        /// <returns>This configuration instance for method chaining</returns>
        public EpicChainUnityConfig PreventTransmissionOnFault()
        {
            this.allowTransmissionOnFault = false;
            return this;
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnValidate()
        {
            // Validate configuration values in Unity Inspector
            blockInterval = Mathf.Max(1000, blockInterval); // Minimum 1 second
            pollingInterval = Mathf.Max(1000, pollingInterval); // Minimum 1 second
            requestTimeout = Mathf.Clamp(requestTimeout, 1f, 300f); // 1-300 seconds
            maxConcurrentRequests = Mathf.Clamp(maxConcurrentRequests, 1, 50); // 1-50 requests
            
            // Validate URL format
            if (!string.IsNullOrEmpty(nodeUrl) && !nodeUrl.StartsWith("http"))
            {
                Debug.LogWarning($"[EpicChainUnityConfig] Invalid node URL format: {nodeUrl}. Should start with 'http' or 'https'.");
            }
            
            // Validate XNS resolver hash format
            if (!string.IsNullOrEmpty(xnsResolver) && xnsResolver.Length != 40)
            {
                Debug.LogWarning($"[EpicChainUnityConfig] Invalid XNS resolver hash format: {xnsResolver}. Should be 40 characters long.");
            }
        }
        
        #endregion
        
        #region Static Utilities
        
        /// <summary>
        /// Creates a default configuration for EpicChain mainnet.
        /// </summary>
        /// <returns>Default mainnet configuration</returns>
        public static EpicChainUnityConfig CreateMainnetConfig()
        {
            var config = CreateInstance<EpicChainUnityConfig>();
            config.nodeUrl = "https://mainnet1-seed.epic-chain.org:10111";
            config.networkMagic = 860833102; // EpicChain mainnet magic
            config.xnsResolver = MAINNET_XNS_CONTRACT_HASH.ToString();
            config.name = "EpicChain Mainnet Config";
            return config;
        }
        
        /// <summary>
        /// Creates a default configuration for EpicChain testnet.
        /// </summary>
        /// <returns>Default testnet configuration</returns>
        public static EpicChainUnityConfig CreateTestnetConfig()
        {
            var config = CreateInstance<EpicChainUnityConfig>();
            config.nodeUrl = "https://testnet1-seed.epic-chain.org:20111";
            config.networkMagic = 894710606; // EpicChain testnet magic
            config.xnsResolver = "0xa46c1e9f936d2967adf5d8ee5c3e2b4b5a7fff3a"; // Testnet XNS
            config.name = "EpicChain Testnet Config";
            return config;
        }
        
        #endregion
    }
}