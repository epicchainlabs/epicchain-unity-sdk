using System.Collections.Generic;
using System.Threading.Tasks;
using EpicChainUnityRuntime.Protocol.Response;
using EpicChainUnityRuntime.Types;

namespace EpicChainUnityRuntime.Protocol
{
    /// <summary>
    /// Interface for EpicChain Express development features.
    /// EpicChain Express is a developer-oriented blockchain for testing and development.
    /// </summary>
    public interface IEpicChainExpress
    {
        /// <summary>
        /// Gets the populated blocks information from EpicChain Express.
        /// Returns statistics about blocks that contain transactions.
        /// </summary>
        /// <returns>Populated blocks information</returns>
        Task<EpicChainResponse<PopulatedBlocks>> ExpressGetPopulatedBlocks();

        /// <summary>
        /// Gets all XEP-17 contracts deployed on EpicChain Express.
        /// Useful for discovering available tokens in development environment.
        /// </summary>
        /// <returns>List of XEP-17 contracts</returns>
        Task<EpicChainResponse<List<Xep17Contract>>> ExpressGetXep17Contracts();

        /// <summary>
        /// Gets the storage entries for a specific contract on EpicChain Express.
        /// Allows inspection of contract state for debugging purposes.
        /// </summary>
        /// <param name="contractHash">The contract hash to get storage for</param>
        /// <returns>List of contract storage entries</returns>
        Task<EpicChainResponse<List<ContractStorageEntry>>> ExpressGetContractStorage(Hash160 contractHash);

        /// <summary>
        /// Lists all contracts deployed on EpicChain Express.
        /// Includes both native and deployed contracts with their states.
        /// </summary>
        /// <returns>List of contract states</returns>
        Task<EpicChainResponse<List<ExpressContractState>>> ExpressListContracts();

        /// <summary>
        /// Creates a checkpoint file for EpicChain Express blockchain state.
        /// Allows saving the current blockchain state to a file for later restoration.
        /// </summary>
        /// <param name="filename">The filename for the checkpoint</param>
        /// <returns>Result message</returns>
        Task<EpicChainResponse<string>> ExpressCreateCheckpoint(string filename);

        /// <summary>
        /// Lists all pending oracle requests on EpicChain Express.
        /// Useful for development and testing of oracle functionality.
        /// </summary>
        /// <returns>List of oracle requests</returns>
        Task<EpicChainResponse<List<OracleRequest>>> ExpressListOracleRequests();

        /// <summary>
        /// Creates an oracle response transaction on EpicChain Express.
        /// Allows manual creation of oracle responses for testing.
        /// </summary>
        /// <param name="oracleResponse">The oracle response transaction attribute</param>
        /// <returns>Transaction hash</returns>
        Task<EpicChainResponse<string>> ExpressCreateOracleResponseTx(TransactionAttribute oracleResponse);

        /// <summary>
        /// Shuts down the EpicChain Express blockchain instance.
        /// Gracefully stops the EpicChain Express node.
        /// </summary>
        /// <returns>Shutdown information</returns>
        Task<EpicChainResponse<ExpressShutdown>> ExpressShutdown();
    }
}