using System;
using UnityEngine;
using Newtonsoft.Json;

namespace EpicChain.Unity.SDK.Protocol.Response
{
    /// <summary>
    /// Response for the getunclaimedgas RPC call.
    /// Returns the amount of unclaimed EpicPulse tokens for a specific address.
    /// </summary>
    [System.Serializable]
    public class EpicChainGetUnclaimedGasResponse : EpicChainResponse<EpicChainGetUnclaimedGasResponse.UnclaimedGasInfo>
    {
        /// <summary>
        /// Gets the unclaimed EpicPulse information from the response.
        /// </summary>
        public UnclaimedGasInfo UnclaimedGasInfo => Result;

        /// <summary>
        /// Default constructor for JSON deserialization.
        /// </summary>
        public EpicChainGetUnclaimedGasResponse() : base()
        {
        }

        /// <summary>
        /// Creates a successful unclaimed EpicPulse response.
        /// </summary>
        /// <param name="unclaimedGasInfo">The unclaimed EpicPulse information</param>
        /// <param name="id">The request ID</param>
        public EpicChainGetUnclaimedGasResponse(UnclaimedGasInfo unclaimedGasInfo, int id = 1) : base(unclaimedGasInfo, id)
        {
        }

        /// <summary>
        /// Creates an error unclaimed EpicPulse response.
        /// </summary>
        /// <param name="error">The error information</param>
        /// <param name="id">The request ID</param>
        public EpicChainGetUnclaimedGasResponse(ResponseError error, int id = 1) : base(error, id)
        {
        }

        /// <summary>
        /// Contains information about unclaimed XPP tokens for an address.
        /// </summary>
        [System.Serializable]
        public class UnclaimedGasInfo
        {
            /// <summary>The amount of unclaimed XPP in string format (to preserve precision)</summary>
            [JsonProperty("unclaimed")]
            public string Unclaimed { get; set; }

            /// <summary>The EpicChain address for which the unclaimed EpicPulse is being queried</summary>
            [JsonProperty("address")]
            public string Address { get; set; }

            /// <summary>
            /// Default constructor for JSON deserialization.
            /// </summary>
            public UnclaimedGasInfo()
            {
            }

            /// <summary>
            /// Creates new unclaimed XPP information.
            /// </summary>
            /// <param name="unclaimed">The amount of unclaimed XPP</param>
            /// <param name="address">The EpicChain address</param>
            public UnclaimedGasInfo(string unclaimed, string address)
            {
                Unclaimed = unclaimed;
                Address = address;
            }

            /// <summary>
            /// Gets the unclaimed XPP amount as a decimal value.
            /// Note: EpicChain XPP uses 8 decimal places (smallest unit is 0.00000001).
            /// </summary>
            [JsonIgnore]
            public decimal UnclaimedDecimal
            {
                get
                {
                    if (string.IsNullOrEmpty(Unclaimed))
                        return 0m;

                    if (decimal.TryParse(Unclaimed, out decimal result))
                        return result;

                    return 0m;
                }
            }

            /// <summary>
            /// Gets the unclaimed EpicPulse amount as a double value.
            /// Note: May lose precision for very small amounts.
            /// </summary>
            [JsonIgnore]
            public double UnclaimedDouble
            {
                get
                {
                    if (string.IsNullOrEmpty(Unclaimed))
                        return 0.0;

                    if (double.TryParse(Unclaimed, out double result))
                        return result;

                    return 0.0;
                }
            }

            /// <summary>
            /// Checks if there is any unclaimed EpicPulse available.
            /// </summary>
            [JsonIgnore]
            public bool HasUnclaimedGas => UnclaimedDecimal > 0m;

            /// <summary>
            /// Gets the unclaimed EpicPulse amount in the smallest unit (equivalent to satoshis).
            /// </summary>
            [JsonIgnore]
            public long UnclaimedInSmallestUnit
            {
                get
                {
                    try
                    {
                        return (long)(UnclaimedDecimal * 100_000_000m); // 8 decimal places
                    }
                    catch
                    {
                        return 0L;
                    }
                }
            }

            /// <summary>
            /// Returns a formatted string representation of the unclaimed EpicPulse amount.
            /// </summary>
            /// <param name="decimals">Number of decimal places to show (default: 8)</param>
            /// <returns>Formatted EpicPulse amount</returns>
            public string GetFormattedAmount(int decimals = 8)
            {
                if (decimals < 0 || decimals > 8)
                    decimals = 8;

                return UnclaimedDecimal.ToString($"F{decimals}");
            }

            /// <summary>
            /// Returns a string representation of the unclaimed EpicPulse information.
            /// </summary>
            /// <returns>String representation</returns>
            public override string ToString()
            {
                return $"UnclaimedGasInfo(Address: {Address}, Unclaimed: {Unclaimed} GAS)";
            }

            /// <summary>
            /// Validates the unclaimed EpicPulse information.
            /// </summary>
            /// <exception cref="ArgumentException">If the information is invalid</exception>
            public void Validate()
            {
                if (string.IsNullOrEmpty(Address))
                    throw new ArgumentException("Address cannot be null or empty");

                if (string.IsNullOrEmpty(Unclaimed))
                    throw new ArgumentException("Unclaimed amount cannot be null or empty");

                if (!decimal.TryParse(Unclaimed, out decimal amount))
                    throw new ArgumentException($"Invalid unclaimed amount format: {Unclaimed}");

                if (amount < 0)
                    throw new ArgumentException("Unclaimed amount cannot be negative");

                // Basic address validation (EpicChain addresses start with 'N' and are typically 34 characters)
                if (!Address.StartsWith("N") || Address.Length != 34)
                    throw new ArgumentException($"Invalid EpicChain address format: {Address}");
            }

            /// <summary>
            /// Compares unclaimed amounts between two UnclaimedGasInfo instances.
            /// </summary>
            /// <param name="other">The other instance to compare with</param>
            /// <returns>Comparison result</returns>
            public int CompareTo(UnclaimedGasInfo other)
            {
                if (other == null)
                    return 1;

                return UnclaimedDecimal.CompareTo(other.UnclaimedDecimal);
            }
        }
    }
}