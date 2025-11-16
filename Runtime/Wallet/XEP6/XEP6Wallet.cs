using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EpicChain.Unity.SDK.Crypto;
using EpicChain.Unity.SDK.Types;
using Newtonsoft.Json;

namespace EpicChain.Unity.SDK.Wallet
{
    /// <summary>
    /// Represents a XEP-6 wallet file format for standard compatibility.
    /// XEP-6 is the standard format for EpicChain wallet files used across the ecosystem.
    /// </summary>
    [System.Serializable]
    public class XEP6Wallet
    {
        #region Properties
        
        /// <summary>The name of the wallet</summary>
        [JsonProperty("name")]
        public string Name { get; set; }
        
        /// <summary>The version of the wallet format</summary>
        [JsonProperty("version")]
        public string Version { get; set; }
        
        /// <summary>The scrypt parameters for encryption</summary>
        [JsonProperty("scrypt")]
        public ScryptParams Scrypt { get; set; }
        
        /// <summary>The accounts in this wallet</summary>
        [JsonProperty("accounts")]
        public List<XEP6Account> Accounts { get; set; }
        
        /// <summary>Additional metadata (optional)</summary>
        [JsonProperty("extra")]
        public Dictionary<string, object> Extra { get; set; }
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a new XEP-6 wallet.
        /// </summary>
        /// <param name="name">The wallet name</param>
        /// <param name="version">The wallet version</param>
        /// <param name="scrypt">The scrypt parameters</param>
        /// <param name="accounts">The accounts</param>
        /// <param name="extra">Additional metadata</param>
        public XEP6Wallet(string name, string version, ScryptParams scrypt, List<XEP6Account> accounts, Dictionary<string, object> extra)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Scrypt = scrypt ?? throw new ArgumentNullException(nameof(scrypt));
            Accounts = accounts ?? new List<XEP6Account>();
            Extra = extra;
        }
        
        /// <summary>
        /// Default constructor for JSON deserialization.
        /// </summary>
        public XEP6Wallet()
        {
            Accounts = new List<XEP6Account>();
        }
        
        #endregion
        
        #region Equality
        
        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (obj is XEP6Wallet other)
            {
                return Name == other.Name &&
                       Version == other.Version &&
                       Scrypt.Equals(other.Scrypt) &&
                       Accounts.Count == other.Accounts.Count &&
                       Accounts.All(acc => other.Accounts.Contains(acc)) &&
                       ((Extra == null && other.Extra == null) ||
                        (Extra != null && other.Extra != null && Extra.Count == other.Extra.Count));
            }
            return false;
        }
        
        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            var hashCode = HashCode.Combine(Name, Version, Scrypt);
            
            foreach (var account in Accounts)
            {
                hashCode = HashCode.Combine(hashCode, account.GetHashCode());
            }
            
            return hashCode;
        }
        
        #endregion
        
        #region String Representation
        
        /// <summary>
        /// Returns a string representation of this XEP-6 wallet.
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            var defaultAccount = Accounts.FirstOrDefault(acc => acc.IsDefault);
            return $"XEP6Wallet(Name: {Name}, Version: {Version}, Accounts: {Accounts.Count}, Default: {defaultAccount?.Address ?? "None"})";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Represents a XEP-6 account within a wallet file.
    /// </summary>
    [System.Serializable]
    public class XEP6Account
    {
        #region Properties
        
        /// <summary>The EpicChain address of this account</summary>
        [JsonProperty("address")]
        public string Address { get; set; }
        
        /// <summary>The label/name for this account</summary>
        [JsonProperty("label")]
        public string Label { get; set; }
        
        /// <summary>Whether this is the default account in the wallet</summary>
        [JsonProperty("isDefault")]
        public bool IsDefault { get; set; }
        
        /// <summary>Whether this account is locked</summary>
        [JsonProperty("lock")]
        public bool Lock { get; set; }
        
        /// <summary>The encrypted private key (XEP-2 format)</summary>
        [JsonProperty("key")]
        public string Key { get; set; }
        
        /// <summary>The contract information for this account</summary>
        [JsonProperty("contract")]
        public XEP6Contract Contract { get; set; }
        
        /// <summary>Additional metadata</summary>
        [JsonProperty("extra")]
        public Dictionary<string, object> Extra { get; set; }
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a new XEP-6 account.
        /// </summary>
        /// <param name="address">The EpicChain address</param>
        /// <param name="label">The account label</param>
        /// <param name="isDefault">Whether this is the default account</param>
        /// <param name="lock">Whether the account is locked</param>
        /// <param name="key">The encrypted private key</param>
        /// <param name="contract">The contract information</param>
        /// <param name="extra">Additional metadata</param>
        public XEP6Account(string address, string label, bool isDefault, bool lock, string key, XEP6Contract contract, Dictionary<string, object> extra)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
            Label = label ?? address;
            IsDefault = isDefault;
            Lock = lock;
            Key = key;
            Contract = contract;
            Extra = extra;
        }
        
        /// <summary>
        /// Default constructor for JSON deserialization.
        /// </summary>
        public XEP6Account()
        {
        }
        
        #endregion
        
        #region Equality
        
        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (obj is XEP6Account other)
            {
                return Address == other.Address &&
                       Label == other.Label &&
                       IsDefault == other.IsDefault &&
                       Lock == other.Lock &&
                       Key == other.Key &&
                       ((Contract == null && other.Contract == null) ||
                        (Contract != null && Contract.Equals(other.Contract)));
            }
            return false;
        }
        
        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Address, Label, IsDefault, Lock, Key, Contract);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Represents contract information in a XEP-6 account.
    /// </summary>
    [System.Serializable]
    public class XEP6Contract
    {
        #region Properties
        
        /// <summary>The verification script in base64 format</summary>
        [JsonProperty("script")]
        public string Script { get; set; }
        
        /// <summary>The parameters for the contract</summary>
        [JsonProperty("parameters")]
        public List<XEP6Parameter> Parameters { get; set; }
        
        /// <summary>Whether the contract is deployed on the blockchain</summary>
        [JsonProperty("deployed")]
        public bool Deployed { get; set; }
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a new XEP-6 contract.
        /// </summary>
        /// <param name="script">The verification script in base64</param>
        /// <param name="parameters">The contract parameters</param>
        /// <param name="deployed">Whether the contract is deployed</param>
        public XEP6Contract(string script, List<XEP6Parameter> parameters, bool deployed)
        {
            Script = script;
            Parameters = parameters ?? new List<XEP6Parameter>();
            Deployed = deployed;
        }
        
        /// <summary>
        /// Default constructor for JSON deserialization.
        /// </summary>
        public XEP6Contract()
        {
            Parameters = new List<XEP6Parameter>();
        }
        
        #endregion
        
        #region Equality
        
        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (obj is XEP6Contract other)
            {
                return Script == other.Script &&
                       Deployed == other.Deployed &&
                       Parameters.Count == other.Parameters.Count &&
                       Parameters.All(param => other.Parameters.Contains(param));
            }
            return false;
        }
        
        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            var hashCode = HashCode.Combine(Script, Deployed);
            
            foreach (var param in Parameters)
            {
                hashCode = HashCode.Combine(hashCode, param.GetHashCode());
            }
            
            return hashCode;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Represents a parameter in a XEP-6 contract.
    /// </summary>
    [System.Serializable]
    public class XEP6Parameter
    {
        #region Properties
        
        /// <summary>The parameter name</summary>
        [JsonProperty("name")]
        public string Name { get; set; }
        
        /// <summary>The parameter type</summary>
        [JsonProperty("type")]
        public ContractParameterType Type { get; set; }
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a new XEP-6 parameter.
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <param name="type">The parameter type</param>
        public XEP6Parameter(string name, ContractParameterType type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
        }
        
        /// <summary>
        /// Default constructor for JSON deserialization.
        /// </summary>
        public XEP6Parameter()
        {
        }
        
        #endregion
        
        #region Equality
        
        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (obj is XEP6Parameter other)
            {
                return Name == other.Name && Type == other.Type;
            }
            return false;
        }
        
        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Type);
        }
        
        #endregion
    }
}