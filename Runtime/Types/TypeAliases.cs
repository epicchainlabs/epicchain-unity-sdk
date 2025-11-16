using System;
using UnityEngine;
using EpicChain.Unity.SDK.Crypto;

namespace EpicChain.Unity.SDK.Types
{
    /// <summary>
    /// Type aliases that provide compatibility with EpicChain naming conventions
    /// and simplify common type usage throughout the SDK.
    /// </summary>
    public static class TypeAliases
    {
        // Note: These are provided as static readonly fields rather than type aliases
        // because C# doesn't support Swift-style typealias declarations.
        // Use these types for consistency with EpicChain conventions.
    }

    /// <summary>
    /// Represents a single byte value in EpicChain operations.
    /// Equivalent to Swift's UInt8/Byte type.
    /// </summary>
    [Serializable]
    public struct EpicChainByte : IEquatable<EpicChainByte>, IComparable<EpicChainByte>
    {
        [SerializeField]
        private byte _value;

        /// <summary>
        /// The underlying byte value.
        /// </summary>
        public byte Value => _value;

        /// <summary>
        /// Initializes a new instance of the EpicChainByte struct.
        /// </summary>
        /// <param name="value">The byte value.</param>
        public EpicChainByte(byte value)
        {
            _value = value;
        }

        /// <summary>
        /// Implicit conversion from byte to EpicChainByte.
        /// </summary>
        /// <param name="value">The byte value.</param>
        /// <returns>A new EpicChainByte instance.</returns>
        public static implicit operator EpicChainByte(byte value) => new(value);

        /// <summary>
        /// Implicit conversion from EpicChainByte to byte.
        /// </summary>
        /// <param name="epicchainByte">The EpicChainByte instance.</param>
        /// <returns>The underlying byte value.</returns>
        public static implicit operator byte(EpicChainByte epicchainByte) => epicchainByte._value;

        /// <summary>
        /// Converts the EpicChainByte to its hexadecimal string representation.
        /// </summary>
        /// <returns>A hexadecimal string representation.</returns>
        public override string ToString() => $"0x{_value:X2}";

        public bool Equals(EpicChainByte other) => _value == other._value;
        public override bool Equals(object obj) => obj is EpicChainByte other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public int CompareTo(EpicChainByte other) => _value.CompareTo(other._value);

        public static bool operator ==(EpicChainByte left, EpicChainByte right) => left.Equals(right);
        public static bool operator !=(EpicChainByte left, EpicChainByte right) => !left.Equals(right);
        public static bool operator <(EpicChainByte left, EpicChainByte right) => left.CompareTo(right) < 0;
        public static bool operator >(EpicChainByte left, EpicChainByte right) => left.CompareTo(right) > 0;
        public static bool operator <=(EpicChainByte left, EpicChainByte right) => left.CompareTo(right) <= 0;
        public static bool operator >=(EpicChainByte left, EpicChainByte right) => left.CompareTo(right) >= 0;
    }

    /// <summary>
    /// Represents a collection of bytes in EpicChain operations.
    /// Equivalent to Swift's [UInt8]/Bytes type.
    /// </summary>
    [Serializable]
    public class EpicChainBytes : IEquatable<EpicChainBytes>
    {
        [SerializeField]
        private byte[] _bytes;

        /// <summary>
        /// The underlying byte array.
        /// </summary>
        public byte[] Value => _bytes ?? Array.Empty<byte>();

        /// <summary>
        /// The length of the byte array.
        /// </summary>
        public int Length => _bytes?.Length ?? 0;

        /// <summary>
        /// Gets or sets the byte at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <returns>The byte at the specified index.</returns>
        public byte this[int index]
        {
            get => _bytes[index];
            set => _bytes[index] = value;
        }

        /// <summary>
        /// Initializes a new instance of the EpicChainBytes class.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        public EpicChainBytes(byte[] bytes = null)
        {
            _bytes = bytes?.ToArray() ?? Array.Empty<byte>();
        }

        /// <summary>
        /// Initializes a new instance of the EpicChainBytes class from a hex string.
        /// </summary>
        /// <param name="hexString">The hexadecimal string (with or without 0x prefix).</param>
        /// <returns>A new EpicChainBytes instance.</returns>
        public static EpicChainBytes FromHex(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return new EpicChainBytes();

            // Remove 0x prefix if present
            if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hexString = hexString.Substring(2);

            // Ensure even number of characters
            if (hexString.Length % 2 != 0)
                hexString = "0" + hexString;

            var bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return new EpicChainBytes(bytes);
        }

        /// <summary>
        /// Implicit conversion from byte array to EpicChainBytes.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <returns>A new EpicChainBytes instance.</returns>
        public static implicit operator EpicChainBytes(byte[] bytes) => new(bytes);

        /// <summary>
        /// Implicit conversion from EpicChainBytes to byte array.
        /// </summary>
        /// <param name="epicchainBytes">The EpicChainBytes instance.</param>
        /// <returns>The underlying byte array.</returns>
        public static implicit operator byte[](EpicChainBytes epicchainBytes) => epicchainBytes.Value;

        /// <summary>
        /// Converts the EpicChainBytes to its hexadecimal string representation.
        /// </summary>
        /// <param name="prefix">Whether to include the 0x prefix.</param>
        /// <returns>A hexadecimal string representation.</returns>
        public string ToHex(bool prefix = true)
        {
            if (_bytes == null || _bytes.Length == 0)
                return prefix ? "0x" : "";

            var hex = BitConverter.ToString(_bytes).Replace("-", "").ToLowerInvariant();
            return prefix ? "0x" + hex : hex;
        }

        /// <summary>
        /// Converts the EpicChainBytes to its hexadecimal string representation with 0x prefix.
        /// </summary>
        /// <returns>A hexadecimal string representation.</returns>
        public override string ToString() => ToHex();

        /// <summary>
        /// Creates a copy of the EpicChainBytes.
        /// </summary>
        /// <returns>A new EpicChainBytes instance with the same byte values.</returns>
        public EpicChainBytes Copy() => new(_bytes);

        /// <summary>
        /// Concatenates this EpicChainBytes with another.
        /// </summary>
        /// <param name="other">The other EpicChainBytes to concatenate.</param>
        /// <returns>A new EpicChainBytes containing the concatenated bytes.</returns>
        public EpicChainBytes Concat(EpicChainBytes other)
        {
            if (other == null || other.Length == 0)
                return Copy();

            var result = new byte[Length + other.Length];
            Array.Copy(_bytes, 0, result, 0, Length);
            Array.Copy(other._bytes, 0, result, Length, other.Length);
            return new EpicChainBytes(result);
        }

        /// <summary>
        /// Gets a slice of the EpicChainBytes.
        /// </summary>
        /// <param name="start">The starting index.</param>
        /// <param name="length">The length of the slice.</param>
        /// <returns>A new EpicChainBytes containing the sliced bytes.</returns>
        public EpicChainBytes Slice(int start, int length)
        {
            if (start < 0 || start >= Length || length <= 0)
                return new EpicChainBytes();

            length = Math.Min(length, Length - start);
            var result = new byte[length];
            Array.Copy(_bytes, start, result, 0, length);
            return new EpicChainBytes(result);
        }

        public bool Equals(EpicChainBytes other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            
            if (Length != other.Length) return false;
            
            for (int i = 0; i < Length; i++)
            {
                if (_bytes[i] != other._bytes[i])
                    return false;
            }
            
            return true;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is EpicChainBytes other && Equals(other));
        }

        public override int GetHashCode()
        {
            if (_bytes == null) return 0;
            
            int hash = 17;
            foreach (var b in _bytes)
            {
                hash = hash * 31 + b;
            }
            return hash;
        }

        public static bool operator ==(EpicChainBytes left, EpicChainBytes right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) == true);
        
        public static bool operator !=(EpicChainBytes left, EpicChainBytes right) => !(left == right);
    }
}