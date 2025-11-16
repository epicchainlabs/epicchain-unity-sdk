using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using EpicChain.Unity.SDK.Core;
using EpicChain.Unity.SDK.Types;
using EpicChain.Unity.SDK.Transaction;
using EpicChain.Unity.SDK.Wallet;

namespace EpicChain.Unity.SDK.Contracts
{
    /// <summary>
    /// Represents a fungible token contract that is compliant with the XEP-17 standard
    /// and provides methods to invoke it. XEP-17 is the EpicChain standard for fungible tokens
    /// similar to ERC-20 on Ethereum.
    /// </summary>
    [System.Serializable]
    public class FungibleToken : Token
    {
        #region Constants
        
        private const string BALANCE_OF = "balanceOf";
        private const string TRANSFER = "transfer";
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Constructs a FungibleToken instance representing the XEP-17 token contract with the given script hash.
        /// </summary>
        /// <param name="scriptHash">The token contract's script hash</param>
        /// <param name="epicchainUnity">The EpicChainUnity instance to use for invocations</param>
        public FungibleToken(Hash160 scriptHash, EpicChainUnity epicchainUnity) : base(scriptHash, epicchainUnity)
        {
        }
        
        /// <summary>
        /// Constructs a FungibleToken instance using the singleton EpicChainUnity instance.
        /// </summary>
        /// <param name="scriptHash">The token contract's script hash</param>
        public FungibleToken(Hash160 scriptHash) : base(scriptHash)
        {
        }
        
        #endregion
        
        #region Balance Methods
        
        /// <summary>
        /// Gets the token balance for the given account.
        /// The token amount is returned in token fractions. E.g., an amount of 1 EpicPulse is returned as 1*10^8 EpicPulse fractions.
        /// The balance is not cached locally. Every time this method is called requests are sent to the EpicChain node.
        /// </summary>
        /// <param name="account">The account to fetch the balance for</param>
        /// <returns>The token balance in fractions</returns>
        public async Task<long> GetBalanceOf(Account account)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }
            
            return await GetBalanceOf(account.GetScriptHash());
        }
        
        /// <summary>
        /// Gets the token balance for the given account script hash.
        /// The token amount is returned in token fractions. E.g., an amount of 1 EpicPulse is returned as 1*10^8 EpicPulse fractions.
        /// The balance is not cached locally. Every time this method is called requests are sent to the EpicChain node.
        /// </summary>
        /// <param name="scriptHash">The script hash to fetch the balance for</param>
        /// <returns>The token balance in fractions</returns>
        public async Task<long> GetBalanceOf(Hash160 scriptHash)
        {
            if (scriptHash == null)
            {
                throw new ArgumentNullException(nameof(scriptHash));
            }
            
            var result = await CallFunctionReturningInt(BALANCE_OF, ContractParameter.Hash160(scriptHash));
            
            if (EpicChainUnityConfig.EnableDebugLogging)
            {
                Debug.Log($"[FungibleToken] Balance of {scriptHash}: {result}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets the token balance for the given wallet, i.e., all accounts in the wallet combined.
        /// The token amount is returned in token fractions. E.g., an amount of 1 EpicPulse is returned as 1*10^8 EpicPulse fractions.
        /// The balance is not cached locally. Every time this method is called requests are sent to the EpicChain node.
        /// </summary>
        /// <param name="wallet">The wallet to fetch the balance for</param>
        /// <returns>The total token balance across all accounts in the wallet</returns>
        public async Task<long> GetBalanceOf(EpicChainWallet wallet)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }
            
            long totalBalance = 0;
            var tasks = new List<Task<long>>();
            
            foreach (var account in wallet.GetAccounts())
            {
                tasks.Add(GetBalanceOf(account));
            }
            
            var balances = await Task.WhenAll(tasks);
            
            foreach (var balance in balances)
            {
                totalBalance += balance;
            }
            
            if (EpicChainUnityConfig.EnableDebugLogging)
            {
                Debug.Log($"[FungibleToken] Total wallet balance: {totalBalance}");
            }
            
            return totalBalance;
        }
        
        #endregion
        
        #region Transfer Methods
        
        /// <summary>
        /// Creates a transfer transaction. The from account is set as a signer of the transaction.
        /// Only use this method when the recipient is a deployed smart contract to avoid unnecessary additional fees.
        /// Otherwise, use the method without a contract parameter for data.
        /// </summary>
        /// <param name="from">The sender account</param>
        /// <param name="to">The script hash of the recipient</param>
        /// <param name="amount">The amount to transfer in token fractions</param>
        /// <param name="data">The data that is passed to the onPayment method if the recipient is a contract</param>
        /// <returns>A transaction builder ready for signing</returns>
        public async Task<TransactionBuilder> Transfer(Account from, Hash160 to, long amount, ContractParameter data = null)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }
            
            var builder = await Transfer(from.GetScriptHash(), to, amount, data);
            return builder.SetSigners(new List<Signer> { new AccountSigner(from) { Scope = WitnessScope.CalledByEntry } });
        }
        
        /// <summary>
        /// Creates a transfer transaction. No signers are set on the returned transaction builder.
        /// It is up to you to set the correct ones, e.g., a ContractSigner in case the from address is a contract.
        /// </summary>
        /// <param name="from">The script hash of the sender</param>
        /// <param name="to">The script hash of the recipient</param>
        /// <param name="amount">The amount to transfer in token fractions</param>
        /// <param name="data">The data that is passed to the onPayment method if the recipient is a contract</param>
        /// <returns>A transaction builder ready for signing</returns>
        public async Task<TransactionBuilder> Transfer(Hash160 from, Hash160 to, long amount, ContractParameter data = null)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }
            
            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }
            
            if (amount < 0)
            {
                throw new ArgumentException("The amount must be greater than or equal to 0.", nameof(amount));
            }
            
            var transferScript = await BuildTransferScript(from, to, amount, data);
            return new TransactionBuilder(EpicChainUnity).SetScript(transferScript);
        }
        
        /// <summary>
        /// Builds a script that invokes the transfer method on the fungible token.
        /// </summary>
        /// <param name="from">The sender script hash</param>
        /// <param name="to">The recipient script hash</param>
        /// <param name="amount">The transfer amount in fractions</param>
        /// <param name="data">The data that is passed to the onPayment method if the recipient is a contract</param>
        /// <returns>A transfer script as byte array</returns>
        public async Task<byte[]> BuildTransferScript(Hash160 from, Hash160 to, long amount, ContractParameter data = null)
        {
            var parameters = new List<ContractParameter>
            {
                ContractParameter.Hash160(from),
                ContractParameter.Hash160(to),
                ContractParameter.Integer(amount)
            };
            
            if (data != null)
            {
                parameters.Add(data);
            }
            
            return await BuildInvokeFunctionScript(TRANSFER, parameters.ToArray());
        }
        
        #endregion
        
        #region XNS Transfer Methods
        
        /// <summary>
        /// Creates a transfer transaction using EpicChain Name Service (XNS) domain name resolution.
        /// Resolves the text record of the recipient's XNS domain name. The resolved value is expected to be a valid EpicChain address.
        /// The from account is set as a signer of the transaction.
        /// </summary>
        /// <param name="from">The sender account</param>
        /// <param name="to">The XNS domain name to resolve</param>
        /// <param name="amount">The amount to transfer in token fractions</param>
        /// <param name="data">The data that is passed to the onPayment method if the recipient is a contract</param>
        /// <returns>A transaction builder ready for signing</returns>
        public async Task<TransactionBuilder> Transfer(Account from, XNSName to, long amount, ContractParameter data = null)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }
            
            var builder = await Transfer(from.GetScriptHash(), to, amount, data);
            return builder.SetSigners(new List<Signer> { new AccountSigner(from) { Scope = WitnessScope.CalledByEntry } });
        }
        
        /// <summary>
        /// Creates a transfer transaction using EpicChain Name Service (XNS) domain name resolution.
        /// No signers are set on the returned transaction builder. It is up to you to set the correct ones,
        /// e.g., a ContractSigner in case the from address is a contract.
        /// </summary>
        /// <param name="from">The sender script hash</param>
        /// <param name="to">The XNS domain name to resolve</param>
        /// <param name="amount">The amount to transfer in token fractions</param>
        /// <param name="data">The data that is passed to the onPayment method if the recipient is a contract</param>
        /// <returns>A transaction builder ready for signing</returns>
        public async Task<TransactionBuilder> Transfer(Hash160 from, XNSName to, long amount, ContractParameter data = null)
        {
            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }
            
            var resolvedScriptHash = await ResolveXNSTextRecord(to);
            return await Transfer(from, resolvedScriptHash, amount, data);
        }
        
        #endregion
        
        #region Multi-Transfer Methods
        
        /// <summary>
        /// Creates a multi-transfer transaction to send tokens to multiple recipients in a single transaction.
        /// This is more gas-efficient than multiple individual transfers.
        /// </summary>
        /// <param name="from">The sender account</param>
        /// <param name="transfers">List of transfer destinations and amounts</param>
        /// <returns>A transaction builder ready for signing</returns>
        public async Task<TransactionBuilder> MultiTransfer(Account from, List<TokenTransfer> transfers)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }
            
            var builder = await MultiTransfer(from.GetScriptHash(), transfers);
            return builder.SetSigners(new List<Signer> { new AccountSigner(from) { Scope = WitnessScope.CalledByEntry } });
        }
        
        /// <summary>
        /// Creates a multi-transfer transaction to send tokens to multiple recipients in a single transaction.
        /// No signers are set on the returned transaction builder.
        /// </summary>
        /// <param name="from">The sender script hash</param>
        /// <param name="transfers">List of transfer destinations and amounts</param>
        /// <returns>A transaction builder ready for signing</returns>
        public async Task<TransactionBuilder> MultiTransfer(Hash160 from, List<TokenTransfer> transfers)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }
            
            if (transfers == null || transfers.Count == 0)
            {
                throw new ArgumentException("Transfer list cannot be null or empty.", nameof(transfers));
            }
            
            var scriptBuilder = new Script.ScriptBuilder();
            
            foreach (var transfer in transfers)
            {
                if (transfer.Amount < 0)
                {
                    throw new ArgumentException($"Transfer amount must be non-negative. Got: {transfer.Amount}");
                }
                
                var transferScript = await BuildTransferScript(from, transfer.To, transfer.Amount, transfer.Data);
                await scriptBuilder.Emit(transferScript);
            }
            
            return new TransactionBuilder(EpicChainUnity).SetScript(await scriptBuilder.ToArray());
        }
        
        #endregion
        
        #region Unity Integration Methods
        
        /// <summary>
        /// Gets the balance formatted for display with proper decimal places and symbol.
        /// </summary>
        /// <param name="scriptHash">The script hash to get balance for</param>
        /// <returns>Formatted balance string (e.g., "1.50 GAS")</returns>
        public async Task<string> GetFormattedBalance(Hash160 scriptHash)
        {
            var balance = await GetBalanceOf(scriptHash);
            return await FormatAmount(balance, true);
        }
        
        /// <summary>
        /// Checks if an account has sufficient balance for a transfer.
        /// </summary>
        /// <param name="account">The account to check</param>
        /// <param name="amount">The amount to check in fractions</param>
        /// <returns>True if the account has sufficient balance</returns>
        public async Task<bool> HasSufficientBalance(Account account, long amount)
        {
            var balance = await GetBalanceOf(account);
            return balance >= amount;
        }
        
        /// <summary>
        /// Estimates the total cost of a transfer including network fees.
        /// Note: This is an approximation and actual fees may vary.
        /// </summary>
        /// <param name="from">The sender</param>
        /// <param name="to">The recipient</param>
        /// <param name="amount">The transfer amount</param>
        /// <returns>Estimated total cost in EpicPulse fractions</returns>
        public async Task<long> EstimateTransferCost(Hash160 from, Hash160 to, long amount)
        {
            try
            {
                var transferScript = await BuildTransferScript(from, to, amount);
                var transaction = new TransactionBuilder(EpicChainUnity).SetScript(transferScript);
                
                // Build unsigned transaction to get actual fee calculation
                var unsignedTransaction = await transaction.GetUnsignedTransaction();
                
                // Return calculated system fee + network fee
                return unsignedTransaction.SystemFee + unsignedTransaction.NetworkFee;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FungibleToken] Failed to estimate transfer cost: {ex.Message}");
                throw new ContractException($"Failed to estimate transfer cost: {ex.Message}", ex);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Represents a token transfer for multi-transfer operations.
    /// </summary>
    [System.Serializable]
    public class TokenTransfer
    {
        /// <summary>The recipient script hash</summary>
        [SerializeField]
        public Hash160 To { get; set; }
        
        /// <summary>The amount to transfer in token fractions</summary>
        [SerializeField]
        public long Amount { get; set; }
        
        /// <summary>Optional data to pass to the recipient if it's a contract</summary>
        [SerializeField]
        public ContractParameter Data { get; set; }
        
        /// <summary>
        /// Creates a new token transfer.
        /// </summary>
        /// <param name="to">The recipient script hash</param>
        /// <param name="amount">The amount in fractions</param>
        /// <param name="data">Optional contract data</param>
        public TokenTransfer(Hash160 to, long amount, ContractParameter data = null)
        {
            To = to ?? throw new ArgumentNullException(nameof(to));
            Amount = amount;
            Data = data;
        }
    }
}