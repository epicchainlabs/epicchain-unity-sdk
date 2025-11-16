using System;
using System.ComponentModel;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicChain.Unity.SDK.Types
{
    /// <summary>
    /// Represents the execution state of the EpicChain Virtual Machine.
    /// Corresponds to the VM state values used in EpicChain blockchain operations.
    /// </summary>
    [Serializable]
    public enum EpicChainVMStateType : byte
    {
        /// <summary>
        /// No state or uninitialized state.
        /// </summary>
        [Description("NONE")]
        None = 0,

        /// <summary>
        /// Execution completed successfully.
        /// </summary>
        [Description("HALT")]
        Halt = 1,

        /// <summary>
        /// Execution encountered a fault or error.
        /// </summary>
        [Description("FAULT")]
        Fault = 2, // 1 << 1

        /// <summary>
        /// Execution was interrupted or paused.
        /// </summary>
        [Description("BREAK")]
        Break = 4  // 1 << 2
    }

    /// <summary>
    /// Extension methods for EpicChainVMStateType enum operations.
    /// </summary>
    public static class EpicChainVMStateTypeExtensions
    {
        /// <summary>
        /// Gets the JSON string representation of the VM state.
        /// </summary>
        /// <param name="state">The VM state.</param>
        /// <returns>The JSON-compatible string representation.</returns>
        public static string GetJsonValue(this EpicChainVMStateType state)
        {
            return state switch
            {
                EpicChainVMStateType.None => "NONE",
                EpicChainVMStateType.Halt => "HALT",
                EpicChainVMStateType.Fault => "FAULT",
                EpicChainVMStateType.Break => "BREAK",
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Invalid VM state type")
            };
        }

        /// <summary>
        /// Gets the integer representation of the VM state.
        /// </summary>
        /// <param name="state">The VM state.</param>
        /// <returns>The integer value of the state.</returns>
        public static int GetIntValue(this EpicChainVMStateType state)
        {
            return (int)state;
        }

        /// <summary>
        /// Parses a VM state from its JSON string representation.
        /// </summary>
        /// <param name="jsonValue">The JSON string value.</param>
        /// <returns>The corresponding VM state, or None if parsing fails.</returns>
        public static EpicChainVMStateType FromJsonValue(string jsonValue)
        {
            if (string.IsNullOrEmpty(jsonValue))
                return EpicChainVMStateType.None;

            return jsonValue.ToUpperInvariant() switch
            {
                "NONE" => EpicChainVMStateType.None,
                "HALT" => EpicChainVMStateType.Halt,
                "FAULT" => EpicChainVMStateType.Fault,
                "BREAK" => EpicChainVMStateType.Break,
                _ => EpicChainVMStateType.None
            };
        }

        /// <summary>
        /// Parses a VM state from its integer representation.
        /// </summary>
        /// <param name="intValue">The integer value.</param>
        /// <returns>The corresponding VM state, or None if parsing fails.</returns>
        public static EpicChainVMStateType FromIntValue(int intValue)
        {
            return intValue switch
            {
                0 => EpicChainVMStateType.None,
                1 => EpicChainVMStateType.Halt,
                2 => EpicChainVMStateType.Fault,
                4 => EpicChainVMStateType.Break,
                _ => EpicChainVMStateType.None
            };
        }

        /// <summary>
        /// Determines if the VM state represents a successful execution.
        /// </summary>
        /// <param name="state">The VM state.</param>
        /// <returns>True if the state represents success, false otherwise.</returns>
        public static bool IsSuccess(this EpicChainVMStateType state)
        {
            return state == EpicChainVMStateType.Halt;
        }

        /// <summary>
        /// Determines if the VM state represents an error condition.
        /// </summary>
        /// <param name="state">The VM state.</param>
        /// <returns>True if the state represents an error, false otherwise.</returns>
        public static bool IsError(this EpicChainVMStateType state)
        {
            return state == EpicChainVMStateType.Fault;
        }
    }

    /// <summary>
    /// JSON converter for EpicChainVMStateType that handles both string and integer representations.
    /// </summary>
    public class EpicChainVMStateTypeJsonConverter : JsonConverter<EpicChainVMStateType>
    {
        public override void WriteJson(JsonWriter writer, EpicChainVMStateType value, JsonSerializer serializer)
        {
            writer.WriteValue(value.GetJsonValue());
        }

        public override EpicChainVMStateType ReadJson(JsonReader reader, Type objectType, EpicChainVMStateType existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return EpicChainVMStateType.None;

            return reader.Value switch
            {
                string stringValue => EpicChainVMStateTypeExtensions.FromJsonValue(stringValue),
                long longValue => EpicChainVMStateTypeExtensions.FromIntValue((int)longValue),
                int intValue => EpicChainVMStateTypeExtensions.FromIntValue(intValue),
                _ => EpicChainVMStateType.None
            };
        }
    }
}