using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using EpicChain.Unity.SDK.Core;

namespace EpicChain.Unity.SDK.Protocol
{
    /// <summary>
    /// Provides automatic polling of blockchain block height for real-time monitoring.
    /// Unity-optimized implementation using coroutines and proper lifecycle management.
    /// </summary>
    [System.Serializable]
    public class BlockIndexPolling : IDisposable
    {
        #region Events
        
        /// <summary>Fired when a new block is detected</summary>
        public event Action<int> OnNewBlock;
        
        /// <summary>Fired when block polling encounters an error</summary>
        public event Action<string> OnPollingError;
        
        /// <summary>Fired when polling starts</summary>
        public event Action OnPollingStarted;
        
        /// <summary>Fired when polling stops</summary>
        public event Action OnPollingStopped;
        
        #endregion
        
        #region Private Fields
        
        private readonly EpicChainUnity epicchainUnity;
        private readonly MonoBehaviour monoBehaviour;
        private readonly float pollingInterval;
        
        private bool isPolling = false;
        private bool disposed = false;
        private int lastKnownBlockHeight = 0;
        private Coroutine pollingCoroutine;
        private CancellationTokenSource cancellationTokenSource;
        
        #endregion
        
        #region Properties
        
        /// <summary>Whether polling is currently active</summary>
        public bool IsPolling => isPolling && !disposed;
        
        /// <summary>The last known block height</summary>
        public int LastKnownBlockHeight => lastKnownBlockHeight;
        
        /// <summary>The polling interval in seconds</summary>
        public float PollingInterval => pollingInterval;
        
        /// <summary>Whether the polling service has been disposed</summary>
        public bool IsDisposed => disposed;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a new block index polling service.
        /// </summary>
        /// <param name="epicchainUnity">The EpicChainUnity instance for blockchain queries</param>
        /// <param name="monoBehaviour">MonoBehaviour for coroutine execution</param>
        /// <param name="pollingInterval">Polling interval in seconds</param>
        public BlockIndexPolling(EpicChainUnity epicchainUnity, MonoBehaviour monoBehaviour, float pollingInterval = 15f)
        {
            this.epicchainUnity = epicchainUnity ?? throw new ArgumentNullException(nameof(epicchainUnity));
            this.monoBehaviour = monoBehaviour ?? throw new ArgumentNullException(nameof(monoBehaviour));
            
            if (pollingInterval <= 0)
                throw new ArgumentException("Polling interval must be positive", nameof(pollingInterval));
            
            this.pollingInterval = pollingInterval;
            this.cancellationTokenSource = new CancellationTokenSource();
        }
        
        #endregion
        
        #region Polling Control
        
        /// <summary>
        /// Starts block index polling.
        /// </summary>
        /// <param name="startFromCurrent">Whether to start from current block height</param>
        public void StartPolling(bool startFromCurrent = true)
        {
            EnsureNotDisposed();
            
            if (isPolling)
            {
                Debug.LogWarning("[BlockIndexPolling] Already polling");
                return;
            }
            
            if (!EpicChainUnityIsInitialized)
            {
                throw new InvalidOperationException("EpicChainUnity must be initialized before starting polling");
            }
            
            isPolling = true;
            
            if (startFromCurrent)
            {
                _ = InitializeCurrentBlockHeight();
            }
            
            pollingCoroutine = monoBehaviour.StartCoroutine(PollingCoroutine());
            
            OnPollingStarted?.Invoke();
            
            if (EpicChainUnityConfig.EnableDebugLogging)
            {
                Debug.Log($"[BlockIndexPolling] Started polling with {pollingInterval}s interval");
            }
        }
        
        /// <summary>
        /// Stops block index polling.
        /// </summary>
        public void StopPolling()
        {
            if (!isPolling)
                return;
            
            isPolling = false;
            
            if (pollingCoroutine != null && monoBehaviour != null)
            {
                monoBehaviour.StopCoroutine(pollingCoroutine);
                pollingCoroutine = null;
            }
            
            cancellationTokenSource?.Cancel();
            
            OnPollingStopped?.Invoke();
            
            if (EpicChainUnityConfig?.EnableDebugLogging == true)
            {
                Debug.Log("[BlockIndexPolling] Stopped polling");
            }
        }
        
        /// <summary>
        /// Restarts polling with the current configuration.
        /// </summary>
        public void RestartPolling()
        {
            StopPolling();
            StartPolling(false); // Don't reset block height
        }
        
        #endregion
        
        #region Polling Implementation
        
        /// <summary>
        /// Coroutine for continuous block height polling.
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator PollingCoroutine()
        {
            while (isPolling && !disposed && Application.isPlaying)
            {
                try
                {
                    // Check for new blocks asynchronously
                    _ = CheckForNewBlocks();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BlockIndexPolling] Polling error: {ex.Message}");
                    OnPollingError?.Invoke(ex.Message);
                }
                
                yield return new WaitForSeconds(pollingInterval);
            }
        }
        
        /// <summary>
        /// Initializes the current block height.
        /// </summary>
        private async Task InitializeCurrentBlockHeight()
        {
            try
            {
                var response = await EpicChainUnityGetBlockCount().SendAsync();
                lastKnownBlockHeight = response.GetResult();
                
                if (EpicChainUnityConfig.EnableDebugLogging)
                {
                    Debug.Log($"[BlockIndexPolling] Initialized at block height: {lastKnownBlockHeight}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlockIndexPolling] Failed to initialize block height: {ex.Message}");
                OnPollingError?.Invoke($"Initialization failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks for new blocks and fires events.
        /// </summary>
        private async Task CheckForNewBlocks()
        {
            if (!isPolling || disposed || cancellationTokenSource.Token.IsCancellationRequested)
                return;
            
            try
            {
                var response = await EpicChainUnityGetBlockCount().SendAsync();
                var currentHeight = response.GetResult();
                
                if (currentHeight > lastKnownBlockHeight)
                {
                    var blocksDetected = currentHeight - lastKnownBlockHeight;
                    
                    if (EpicChainUnityConfig.EnableDebugLogging && lastKnownBlockHeight > 0)
                    {
                        Debug.Log($"[BlockIndexPolling] New block(s) detected: {lastKnownBlockHeight + 1} → {currentHeight} ({blocksDetected} blocks)");
                    }
                    
                    // Fire events for each new block
                    for (int blockHeight = lastKnownBlockHeight + 1; blockHeight <= currentHeight; blockHeight++)
                    {
                        OnNewBlock?.Invoke(blockHeight);
                    }
                    
                    lastKnownBlockHeight = currentHeight;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BlockIndexPolling] Failed to check block height: {ex.Message}");
                OnPollingError?.Invoke(ex.Message);
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Forces an immediate check for new blocks.
        /// </summary>
        /// <returns>True if new blocks were found</returns>
        public async Task<bool> CheckNow()
        {
            EnsureNotDisposed();
            
            var previousHeight = lastKnownBlockHeight;
            await CheckForNewBlocks();
            
            return lastKnownBlockHeight > previousHeight;
        }
        
        /// <summary>
        /// Gets the current polling status information.
        /// </summary>
        /// <returns>Polling status information</returns>
        public PollingStatus GetStatus()
        {
            EnsureNotDisposed();
            
            return new PollingStatus
            {
                IsPolling = isPolling,
                LastKnownBlockHeight = lastKnownBlockHeight,
                PollingInterval = pollingInterval,
                IsConnected = EpicChainUnityIsInitialized
            };
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Ensures the polling service has not been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If service is disposed</exception>
        private void EnsureNotDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(BlockIndexPolling));
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        /// <summary>
        /// Disposes of the polling service and stops all operations.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                StopPolling();
                
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                
                disposed = true;
            }
        }
        
        #endregion
        
        #region String Representation
        
        /// <summary>
        /// Returns a string representation of this polling service.
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            var status = disposed ? "Disposed" : (isPolling ? "Active" : "Stopped");
            return $"BlockIndexPolling(Status: {status}, Height: {lastKnownBlockHeight}, Interval: {pollingInterval}s)";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Represents the current status of block index polling.
    /// </summary>
    [System.Serializable]
    public class PollingStatus
    {
        /// <summary>Whether polling is currently active</summary>
        public bool IsPolling { get; set; }
        
        /// <summary>The last known block height</summary>
        public int LastKnownBlockHeight { get; set; }
        
        /// <summary>The polling interval in seconds</summary>
        public float PollingInterval { get; set; }
        
        /// <summary>Whether connected to blockchain</summary>
        public bool IsConnected { get; set; }
        
        /// <summary>
        /// Returns a string representation of the polling status.
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return $"PollingStatus(Active: {IsPolling}, Height: {LastKnownBlockHeight}, Connected: {IsConnected})";
        }
    }
}