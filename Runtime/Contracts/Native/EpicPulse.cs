using System;
using System.Threading.Tasks;
using UnityEngine;
using EpicChain.Unity.SDK.Core;
using EpicChain.Unity.SDK.Types;

namespace EpicChain.Unity.SDK.Contracts.Native
{
    /// <summary>
    /// Represents the EpicPulse native contract and provides methods to invoke its functions.
    /// The EpicPulse token is used to pay for transaction fees and contract execution on the EpicChain blockchain.
    /// </summary>
    [System.Serializable]
    public class EpicPulse : FungibleToken
    {
        #region Constants

        public const string NAME = "EpicPulse";
        public static readonly Hash160 SCRIPT_HASH = SmartContract.CalcNativeContractHash(NAME);
        public const int DECIMALS = 8;
        public const string SYMBOL = "XPP";

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new EpicPulseToken instance that uses the given EpicChainUnity instance for invocations.
        /// </summary>
        /// <param name="epicchainUnity">The EpicChainUnity instance to use for invocations</param>
        public EpicPulseToken(EpicChainUnity epicchainUnity) : base(SCRIPT_HASH, epicchainUnity)
        {
        }

        /// <summary>
        /// Constructs a new EpicPulseToken instance using the singleton EpicChainUnity instance.
        /// </summary>
        public EpicPulseToken() : base(SCRIPT_HASH)
        {
        }

        #endregion

        #region Token Metadata Overrides

        /// <summary>
        /// Returns the name of the EpicPulseToken contract.
        /// Doesn't require a call to the EpicChain node.
        /// </summary>
        /// <returns>The name</returns>
        public async Task<string> GetName()
        {
            return NAME;
        }

        /// <summary>
        /// Returns the symbol of the EpicPulseToken contract.
        /// Doesn't require a call to the EpicChain node.
        /// </summary>
        /// <returns>The symbol</returns>
        public override async Task<string> GetSymbol()
        {
            return SYMBOL;
        }

        /// <summary>
        /// Returns the number of decimals of the EpicPulse token.
        /// Doesn't require a call to the EpicChain node.
        /// </summary>
        /// <returns>The number of decimals</returns>
        public override async Task<int> GetDecimals()
        {
            return DECIMALS;
        }

        #endregion

        #region EpicPulse Token Utilities

        /// <summary>
        /// Converts a decimal EpicPulse amount to EpicPulse fractions.
        /// </summary>
        /// <param name="gasAmount">The EpicPulse amount in decimal form</param>
        /// <returns>The EpicPulse amount in fractions</returns>
        public static long ToGasFractions(decimal gasAmount)
        {
            return ToFractions(gasAmount, DECIMALS);
        }

        /// <summary>
        /// Converts EpicPulse fractions to decimal EpicPulse amount.
        /// </summary>
        /// <param name="gasFractions">The EpicPulse amount in fractions</param>
        /// <returns>The EpicPulse amount in decimal form</returns>
        public static decimal FromGasFractions(long gasFractions)
        {
            return ToDecimals(gasFractions, DECIMALS);
        }

        /// <summary>
        /// Gets the EpicPulse balance formatted for display with proper decimal places.
        /// </summary>
        /// <param name="scriptHash">The script hash to get balance for</param>
        /// <returns>Formatted EpicPulse balance string (e.g., "1.50000000 GAS")</returns>
        public async Task<string> GetFormattedGasBalance(Hash160 scriptHash)
        {
            var balance = await GetBalanceOf(scriptHash);
            var decimalAmount = FromGasFractions(balance);
            return $"{decimalAmount:F8} {SYMBOL}";
        }

        /// <summary>
        /// Gets the EpicPulse balance formatted for display with customizable decimal places.
        /// </summary>
        /// <param name="scriptHash">The script hash to get balance for</param>
        /// <param name="decimalPlaces">Number of decimal places to display (0-8)</param>
        /// <param name="includeSymbol">Whether to include the EpicPulse symbol</param>
        /// <returns>Formatted EpicPulse balance string</returns>
        public async Task<string> GetFormattedGasBalance(Hash160 scriptHash, int decimalPlaces = 8, bool includeSymbol = true)
        {
            if (decimalPlaces < 0 || decimalPlaces > DECIMALS)
            {
                throw new ArgumentException($"Decimal places must be between 0 and {DECIMALS}.", nameof(decimalPlaces));
            }

            var balance = await GetBalanceOf(scriptHash);
            var decimalAmount = FromGasFractions(balance);
            var formatString = $"F{decimalPlaces}";
            var formattedAmount = decimalAmount.ToString(formatString);

            return includeSymbol ? $"{formattedAmount} {SYMBOL}" : formattedAmount;
        }

        /// <summary>
        /// Checks if an account has sufficient EpicPulse balance for a transaction or operation.
        /// </summary>
        /// <param name="scriptHash">The account script hash to check</param>
        /// <param name="requiredGasFractions">The required EpicPulse amount in fractions</param>
        /// <returns>True if the account has sufficient EpicPulse balance</returns>
        public async Task<bool> HasSufficientGas(Hash160 scriptHash, long requiredGasFractions)
        {
            if (scriptHash == null)
            {
                throw new ArgumentNullException(nameof(scriptHash));
            }

            if (requiredGasFractions < 0)
            {
                throw new ArgumentException("Required EpicPulse amount cannot be negative.", nameof(requiredGasFractions));
            }

            var balance = await GetBalanceOf(scriptHash);
            var hasSufficientGas = balance >= requiredGasFractions;

            if (EpicChainUnityConfig.EnableDebugLogging)
            {
                var balanceDecimal = FromGasFractions(balance);
                var requiredDecimal = FromGasFractions(requiredGasFractions);
                Debug.Log($"[EpicPulseToken] EpicPulse check for {scriptHash}: Balance={balanceDecimal:F8} GAS, Required={requiredDecimal:F8} GAS, Sufficient={hasSufficientGas}");
            }

            return hasSufficientGas;
        }

        /// <summary>
        /// Calculates the remaining EpicPulse balance after a hypothetical transaction.
        /// </summary>
        /// <param name="scriptHash">The account script hash</param>
        /// <param name="transactionCost">The transaction cost in EpicPulse fractions</param>
        /// <returns>The remaining EpicPulse balance in fractions, or -1 if insufficient</returns>
        public async Task<long> GetRemainingGasAfterTransaction(Hash160 scriptHash, long transactionCost)
        {
            if (scriptHash == null)
            {
                throw new ArgumentNullException(nameof(scriptHash));
            }

            if (transactionCost < 0)
            {
                throw new ArgumentException("Transaction cost cannot be negative.", nameof(transactionCost));
            }

            var balance = await GetBalanceOf(scriptHash);
            return balance >= transactionCost ? balance - transactionCost : -1;
        }

        /// <summary>
        /// Gets the EpicPulse balance in a more user-friendly decimal format.
        /// </summary>
        /// <param name="scriptHash">The script hash to get balance for</param>
        /// <returns>The EpicPulse balance in decimal form</returns>
        public async Task<decimal> GetGasBalanceDecimal(Hash160 scriptHash)
        {
            var balance = await GetBalanceOf(scriptHash);
            return FromGasFractions(balance);
        }

        #endregion

        #region Unity Integration Helpers

        /// <summary>
        /// Estimates the EpicPulse cost for a simple XEP-17 token transfer.
        /// This is a rough estimate based on typical network fees.
        /// </summary>
        /// <returns>Estimated EpicPulse cost in fractions for a simple transfer</returns>
        public static long EstimateTransferGasCost()
        {
            // Conservative estimate: 0.01 EpicPulse for network fee + 0.005 EpicPulse for system fee
            return ToGasFractions(0.015m);
        }

        /// <summary>
        /// Estimates the EpicPulse cost for a contract invocation.
        /// This is a rough estimate and actual costs may vary significantly.
        /// </summary>
        /// <param name="complexity">The complexity level of the contract call (1-10)</param>
        /// <returns>Estimated EpicPulse cost in fractions</returns>
        public static long EstimateContractInvocationGasCost(int complexity = 5)
        {
            if (complexity < 1 || complexity > 10)
            {
                throw new ArgumentException("Complexity must be between 1 and 10.", nameof(complexity));
            }

            // Base cost: 0.02 EpicPulse + complexity factor
            var baseCost = 0.02m;
            var complexityFactor = complexity * 0.005m; // 0.005 EpicPulse per complexity point
            
            return ToGasFractions(baseCost + complexityFactor);
        }

        /// <summary>
        /// Formats a EpicPulse amount for Unity UI display with appropriate precision.
        /// </summary>
        /// <param name="gasFractions">The EpicPulse amount in fractions</param>
        /// <param name="shortFormat">Whether to use short format (fewer decimals for small amounts)</param>
        /// <returns>User-friendly formatted EpicPulse string</returns>
        public static string FormatGasAmountForUI(long gasFractions, bool shortFormat = true)
        {
            var gasDecimal = FromGasFractions(gasFractions);

            if (shortFormat)
            {
                // For UI display, show fewer decimals for very small amounts
                if (gasDecimal >= 1m)
                {
                    return $"{gasDecimal:F4} GAS"; // 4 decimal places for amounts >= 1 GAS
                }
                else if (gasDecimal >= 0.001m)
                {
                    return $"{gasDecimal:F6} GAS"; // 6 decimal places for amounts >= 0.001 GAS
                }
                else
                {
                    return $"{gasDecimal:F8} GAS"; // Full precision for very small amounts
                }
            }
            else
            {
                return $"{gasDecimal:F8} GAS"; // Always show full precision
            }
        }

        /// <summary>
        /// Validates that a EpicPulse amount is within reasonable bounds for the EpicChain network.
        /// </summary>
        /// <param name="gasFractions">The EpicPulse amount to validate</param>
        /// <returns>True if the amount is valid</returns>
        public static bool IsValidGasAmount(long gasFractions)
        {
            // EpicPulse amounts must be non-negative and within the maximum supply bounds
            // EpicChain's maximum EpicPulse supply is theoretically unlimited, but let's use a reasonable upper bound
            const long maxReasonableGas = 100_000_000L * 100_000_000L; // 100 million EpicPulse in fractions
            
            return gasFractions >= 0 && gasFractions <= maxReasonableGas;
        }

        #endregion

        #region Debugging and Logging

        /// <summary>
        /// Logs detailed EpicPulse balance information for debugging purposes.
        /// </summary>
        /// <param name="scriptHash">The script hash to log information for</param>
        /// <param name="context">Additional context for the log entry</param>
        public async Task LogGasBalance(Hash160 scriptHash, string context = null)
        {
            if (!EpicChainUnityConfig.EnableDebugLogging) return;

            try
            {
                var balance = await GetBalanceOf(scriptHash);
                var balanceDecimal = FromGasFractions(balance);
                var contextInfo = !string.IsNullOrEmpty(context) ? $" ({context})" : "";
                
                Debug.Log($"[EpicPulseToken] EpicPulse Balance{contextInfo}: {scriptHash} = {balanceDecimal:F8} EpicPulse ({balance} fractions)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EpicPulseToken] Failed to log EpicPulse balance for {scriptHash}: {ex.Message}");
            }
        }

        #endregion
    }
}