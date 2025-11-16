using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicChainUnityProtocol.Response
{
    /// <summary>
    /// Response for XPR getclaimable RPC method
    /// </summary>
    [Serializable]
    public class EpicChainGetClaimableResponse : EpicChainResponse<EpicChainGetClaimableResponse.Claimables>
    {
        /// <summary>
        /// Gets the claimable result
        /// </summary>
        public Claimables ClaimableData => Result;
        
        /// <summary>
        /// Represents claimable EpicPulse data for an address
        /// </summary>
        [Serializable]
        public struct Claimables : IEquatable<Claimables>
        {
            [JsonProperty("claimable")]
            [SerializeField] private List<Claim> _claims;
            
            [JsonProperty("address")]
            [SerializeField] private string _address;
            
            [JsonProperty("unclaimed")]
            [SerializeField] private string _totalUnclaimed;
            
            /// <summary>
            /// Gets the list of claimable transactions
            /// </summary>
            public List<Claim> Claims => _claims ?? new List<Claim>();
            
            /// <summary>
            /// Gets the address for which claims are available
            /// </summary>
            public string Address => _address;
            
            /// <summary>
            /// Gets the total unclaimed EpicPulse amount as string
            /// </summary>
            public string TotalUnclaimed => _totalUnclaimed;
            
            /// <summary>
            /// Gets the total unclaimed EpicPulse as decimal
            /// </summary>
            public decimal TotalUnclaimedDecimal => decimal.TryParse(_totalUnclaimed, out var result) ? result / 100_000_000m : 0m;
            
            /// <summary>
            /// Initializes a new instance of Claimables
            /// </summary>
            [JsonConstructor]
            public Claimables(List<Claim> claims, string address, string totalUnclaimed)
            {
                _claims = claims ?? new List<Claim>();
                _address = address ?? throw new ArgumentNullException(nameof(address));
                _totalUnclaimed = totalUnclaimed ?? "0";
            }
            
            /// <summary>
            /// Gets whether there are any claimable transactions
            /// </summary>
            public bool HasClaimable => _claims != null && _claims.Count > 0;
            
            public bool Equals(Claimables other)
            {
                return _address == other._address && 
                       _totalUnclaimed == other._totalUnclaimed &&
                       Claims.Count == other.Claims.Count;
            }
            
            public override bool Equals(object obj)
            {
                return obj is Claimables other && Equals(other);
            }
            
            public override int GetHashCode()
            {
                return HashCode.Combine(_address, _totalUnclaimed, _claims?.Count ?? 0);
            }
            
            public override string ToString()
            {
                return $"Claimables(Address: {_address}, Claims: {Claims.Count}, Total: {TotalUnclaimedDecimal} GAS)";
            }
        }
        
        /// <summary>
        /// Represents a single claimable transaction
        /// </summary>
        [Serializable]
        public struct Claim : IEquatable<Claim>
        {
            [JsonProperty("txid")]
            [SerializeField] private string _txId;
            
            [JsonProperty("n")]
            [SerializeField] private int _index;
            
            [JsonProperty("value")]
            [SerializeField] private long _epicchainValue;
            
            [JsonProperty("start_height")]
            [SerializeField] private int _startHeight;
            
            [JsonProperty("end_height")]
            [SerializeField] private int _endHeight;
            
            [JsonProperty("generated")]
            [SerializeField] private string _generatedGas;
            
            [JsonProperty("sysfee")]
            [SerializeField] private string _systemFee;
            
            [JsonProperty("unclaimed")]
            [SerializeField] private string _unclaimedGas;
            
            /// <summary>
            /// Gets the transaction ID
            /// </summary>
            public string TxId => _txId;
            
            /// <summary>
            /// Gets the output index
            /// </summary>
            public int Index => _index;
            
            /// <summary>
            /// Gets the EpicChain value
            /// </summary>
            public long epicchainValue => _epicchainValue;
            
            /// <summary>
            /// Gets the start height
            /// </summary>
            public int StartHeight => _startHeight;
            
            /// <summary>
            /// Gets the end height
            /// </summary>
            public int EndHeight => _endHeight;
            
            /// <summary>
            /// Gets the generated EpicPulse amount as string
            /// </summary>
            public string GeneratedGas => _generatedGas;
            
            /// <summary>
            /// Gets the system fee as string
            /// </summary>
            public string SystemFee => _systemFee;
            
            /// <summary>
            /// Gets the unclaimed XPP amount as string
            /// </summary>
            public string UnclaimedGas => _unclaimedGas;
            
            /// <summary>
            /// Gets the XPR value as decimal
            /// </summary>
            public decimal EpicChainValueDecimal => _epicchainValue / 1_000_000_000b;
            
            /// <summary>
            /// Gets the unclaimed EpicPulse as decimal
            /// </summary>
            public decimal UnclaimedGasDecimal => decimal.TryParse(_unclaimedGas, out var result) ? result / 100_000_000m : 0m;
            
            /// <summary>
            /// Gets the block span for this claim
            /// </summary>
            public int BlockSpan => _endHeight - _startHeight;
            
            /// <summary>
            /// Initializes a new instance of Claim
            /// </summary>
            [JsonConstructor]
            public Claim(
                string txId, 
                int index, 
                long epicchainValue, 
                int startHeight, 
                int endHeight, 
                string generatedGas, 
                string systemFee, 
                string unclaimedGas)
            {
                _txId = txId ?? throw new ArgumentNullException(nameof(txId));
                _index = index;
                _epicchainValue = epicchainValue;
                _startHeight = startHeight;
                _endHeight = endHeight;
                _generatedGas = generatedGas ?? "0";
                _systemFee = systemFee ?? "0";
                _unclaimedGas = unclaimedGas ?? "0";
            }
            
            public bool Equals(Claim other)
            {
                return _txId == other._txId && 
                       _index == other._index && 
                       _epicchainValue == other._epicchainValue &&
                       _startHeight == other._startHeight &&
                       _endHeight == other._endHeight;
            }
            
            public override bool Equals(object obj)
            {
                return obj is Claim other && Equals(other);
            }
            
            public override int GetHashCode()
            {
                return HashCode.Combine(_txId, _index, _epicchainValue, _startHeight, _endHeight);
            }
            
            public override string ToString()
            {
                return $"Claim({_txId}[{_index}], {EpicChainValueDecimal} XPR, {UnclaimedGasDecimal} GAS, Blocks: {_startHeight}-{_endHeight})";
            }
        }
    }
}