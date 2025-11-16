using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EpicChainUnityRuntime.Core;
using EpicChainUnityRuntime.Protocol.Response;

namespace EpicChainUnityRuntime.Reactive
{
    /// <summary>
    /// Unity-compatible reactive blockchain client implementation.
    /// Provides async enumerable patterns for blockchain event streams without external dependencies.
    /// </summary>
    public class EpicChainUnityRx : IEpicChainUnityRx
    {
        private readonly JsonRpc2_0Rx jsonRpcRx;
        private readonly IEpicChain epicchainUnity;

        /// <summary>
        /// Initializes a new instance of EpicChainUnityRx.
        /// </summary>
        /// <param name="epicchainUnity">The EpicChainUnity instance</param>
        /// <param name="cancellationToken">Default cancellation token</param>
        public EpicChainUnityRx(IEpicChain epicchainUnity, CancellationToken cancellationToken = default)
        {
            this.epicchainUnity = epicchainUnity ?? throw new ArgumentNullException(nameof(epicchainUnity));
            this.jsonRpcRx = new JsonRpc2_0Rx(epicchainUnity, cancellationToken);
        }

        /// <summary>
        /// Creates an async enumerable that emits newly created blocks on the blockchain.
        /// </summary>
        /// <param name="fullTransactionObjects">If true, provides transactions embedded in blocks, otherwise transaction hashes</param>
        /// <param name="pollingIntervalMs">Polling interval in milliseconds</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>An async enumerable that emits all new blocks as they are added to the blockchain</returns>
        public IAsyncEnumerable<EpicChainBlock> GetBlockStream(bool fullTransactionObjects = false, int pollingIntervalMs = 1000, CancellationToken cancellationToken = default)
        {
            return jsonRpcRx.GetBlockStream(fullTransactionObjects, pollingIntervalMs, cancellationToken);
        }

        /// <summary>
        /// Creates an async enumerable that emits all blocks from the blockchain contained within the requested range.
        /// </summary>
        /// <param name="startBlock">The block number to commence with</param>
        /// <param name="endBlock">The block number to finish with</param>
        /// <param name="fullTransactionObjects">If true, provides transactions embedded in blocks, otherwise transaction hashes</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>An async enumerable to emit these blocks</returns>
        public IAsyncEnumerable<EpicChainBlock> ReplayBlocks(int startBlock, int endBlock, bool fullTransactionObjects = false, CancellationToken cancellationToken = default)
        {
            return jsonRpcRx.ReplayBlocks(startBlock, endBlock, fullTransactionObjects, true, cancellationToken);
        }

        /// <summary>
        /// Creates an async enumerable that emits all blocks from the blockchain contained within the requested range.
        /// </summary>
        /// <param name="startBlock">The block number to commence with</param>
        /// <param name="endBlock">The block number to finish with</param>
        /// <param name="fullTransactionObjects">If true, provides transactions embedded in blocks, otherwise transaction hashes</param>
        /// <param name="ascending">If true, emits blocks in ascending order between range, otherwise, in descending order</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>An async enumerable to emit these blocks</returns>
        public IAsyncEnumerable<EpicChainBlock> ReplayBlocks(int startBlock, int endBlock, bool fullTransactionObjects, bool ascending, CancellationToken cancellationToken = default)
        {
            return jsonRpcRx.ReplayBlocks(startBlock, endBlock, fullTransactionObjects, ascending, cancellationToken);
        }

        /// <summary>
        /// Creates an async enumerable that emits all blocks from the blockchain starting with a provided block number.
        /// Once it has replayed up to the most current block, the enumerable completes.
        /// </summary>
        /// <param name="startBlock">The block number to commence with</param>
        /// <param name="fullTransactionObjects">If true, provides transactions embedded in blocks, otherwise transaction hashes</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>An async enumerable to emit all requested blocks</returns>
        public async IAsyncEnumerable<EpicChainBlock> CatchUpToLatestBlocks(int startBlock, bool fullTransactionObjects = false, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                // Get the latest block index
                var latestBlockIndex = await jsonRpcRx.GetLatestBlockIndex(cancellationToken);

                // Replay blocks from start to latest
                await foreach (var block in jsonRpcRx.ReplayBlocks(startBlock, latestBlockIndex, fullTransactionObjects, true, cancellationToken))
                {
                    yield return block;
                }
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
        }

        /// <summary>
        /// Creates an async enumerable that emits all blocks from the requested block number to the most current.
        /// Once it has emitted the most current block, it starts emitting new blocks as they are created.
        /// </summary>
        /// <param name="startBlock">The block number to commence with</param>
        /// <param name="fullTransactionObjects">If true, provides transactions embedded in blocks, otherwise transaction hashes</param>
        /// <param name="pollingIntervalMs">Polling interval for new blocks in milliseconds</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>An async enumerable to emit all requested blocks and future blocks</returns>
        public IAsyncEnumerable<EpicChainBlock> CatchUpToLatestAndSubscribeToNewBlocks(int startBlock, bool fullTransactionObjects = false, int pollingIntervalMs = 1000, CancellationToken cancellationToken = default)
        {
            return jsonRpcRx.CatchUpToLatestAndSubscribeToNewBlocks(startBlock, fullTransactionObjects, pollingIntervalMs, cancellationToken);
        }

        /// <summary>
        /// Creates an async enumerable that emits new blocks as they are created on the blockchain (starting from the latest block).
        /// </summary>
        /// <param name="fullTransactionObjects">If true, provides transactions embedded in blocks, otherwise transaction hashes</param>
        /// <param name="pollingIntervalMs">Polling interval in milliseconds</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>An async enumerable to emit all future blocks</returns>
        public IAsyncEnumerable<EpicChainBlock> SubscribeToNewBlocks(bool fullTransactionObjects = false, int pollingIntervalMs = 1000, CancellationToken cancellationToken = default)
        {
            return jsonRpcRx.GetBlockStream(fullTransactionObjects, pollingIntervalMs, cancellationToken);
        }

        /// <summary>
        /// Creates an async enumerable that emits transactions matching the provided predicate.
        /// </summary>
        /// <param name="transactionPredicate">Predicate to filter transactions (null to include all)</param>
        /// <param name="pollingIntervalMs">Polling interval in milliseconds</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>An async enumerable of matching transactions</returns>
        public IAsyncEnumerable<EpicChainTransaction> GetTransactionStream(Func<EpicChainTransaction, bool> transactionPredicate = null, int pollingIntervalMs = 1000, CancellationToken cancellationToken = default)
        {
            return jsonRpcRx.GetTransactionStream(transactionPredicate, pollingIntervalMs, cancellationToken);
        }

        /// <summary>
        /// Creates an async enumerable that emits block indices as they are created.
        /// </summary>
        /// <param name="pollingIntervalMs">Polling interval in milliseconds</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>An async enumerable of block indices</returns>
        public IAsyncEnumerable<int> GetBlockIndexStream(int pollingIntervalMs = 1000, CancellationToken cancellationToken = default)
        {
            return jsonRpcRx.GetBlockIndexStream(pollingIntervalMs, cancellationToken);
        }

        /// <summary>
        /// Creates a reactive wrapper with Unity-friendly event handlers.
        /// </summary>
        /// <returns>A new ReactiveEpicChainUnityWrapper</returns>
        public ReactiveEpicChainUnityWrapper CreateUnityWrapper()
        {
            return new ReactiveEpicChainUnityWrapper(this);
        }
    }

    /// <summary>
    /// Unity-friendly wrapper for reactive blockchain operations.
    /// Provides event-based patterns more suitable for Unity's component system.
    /// </summary>
    public class ReactiveEpicChainUnityWrapper
    {
        private readonly EpicChainUnityRx epicchainUnityRx;
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Event triggered when a new block is received.
        /// </summary>
        public event Action<EpicChainBlock> OnNewBlock;

        /// <summary>
        /// Event triggered when a new transaction is received.
        /// </summary>
        public event Action<EpicChainTransaction> OnNewTransaction;

        /// <summary>
        /// Event triggered when an error occurs.
        /// </summary>
        public event Action<Exception> OnError;

        /// <summary>
        /// Event triggered when block streaming starts.
        /// </summary>
        public event Action OnStreamStarted;

        /// <summary>
        /// Event triggered when block streaming stops.
        /// </summary>
        public event Action OnStreamStopped;

        /// <summary>
        /// Whether the wrapper is currently streaming.
        /// </summary>
        public bool IsStreaming { get; private set; }

        /// <summary>
        /// Initializes a new ReactiveEpicChainUnityWrapper.
        /// </summary>
        /// <param name="epicchainUnityRx">The EpicChainUnityRx instance</param>
        public ReactiveEpicChainUnityWrapper(EpicChainUnityRx epicchainUnityRx)
        {
            this.epicchainUnityRx = epicchainUnityRx ?? throw new ArgumentNullException(nameof(epicchainUnityRx));
        }

        /// <summary>
        /// Start streaming new blocks.
        /// </summary>
        /// <param name="fullTransactionObjects">Whether to include full transaction objects</param>
        /// <param name="pollingIntervalMs">Polling interval in milliseconds</param>
        public void StartBlockStream(bool fullTransactionObjects = false, int pollingIntervalMs = 1000)
        {
            if (IsStreaming)
                return;

            cancellationTokenSource = new CancellationTokenSource();
            IsStreaming = true;
            OnStreamStarted?.Invoke();

            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var block in epicchainUnityRx.GetBlockStream(fullTransactionObjects, pollingIntervalMs, cancellationTokenSource.Token))
                    {
                        OnNewBlock?.Invoke(block);

                        // Emit individual transactions if they exist
                        if (fullTransactionObjects && block.Transactions != null)
                        {
                            foreach (var transaction in block.Transactions)
                            {
                                OnNewTransaction?.Invoke(transaction);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                }
                finally
                {
                    IsStreaming = false;
                    OnStreamStopped?.Invoke();
                }
            });
        }

        /// <summary>
        /// Start streaming transactions with filtering.
        /// </summary>
        /// <param name="transactionPredicate">Predicate to filter transactions</param>
        /// <param name="pollingIntervalMs">Polling interval in milliseconds</param>
        public void StartTransactionStream(Func<EpicChainTransaction, bool> transactionPredicate = null, int pollingIntervalMs = 1000)
        {
            if (IsStreaming)
                return;

            cancellationTokenSource = new CancellationTokenSource();
            IsStreaming = true;
            OnStreamStarted?.Invoke();

            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var transaction in epicchainUnityRx.GetTransactionStream(transactionPredicate, pollingIntervalMs, cancellationTokenSource.Token))
                    {
                        OnNewTransaction?.Invoke(transaction);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                }
                finally
                {
                    IsStreaming = false;
                    OnStreamStopped?.Invoke();
                }
            });
        }

        /// <summary>
        /// Stop streaming.
        /// </summary>
        public void StopStream()
        {
            if (!IsStreaming)
                return;

            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }

        /// <summary>
        /// Dispose and cleanup resources.
        /// </summary>
        public void Dispose()
        {
            StopStream();
        }
    }
}