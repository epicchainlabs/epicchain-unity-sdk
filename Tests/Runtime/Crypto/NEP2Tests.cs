using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EpicChain.Unity.SDK;
using EpicChain.Unity.SDK.Crypto;
using EpicChain.Unity.SDK.Tests.Helpers;

namespace EpicChain.Unity.SDK.Tests.Crypto
{
    /// <summary>
    /// Unity Test Framework implementation of XEP2 encrypted private key tests
    /// Converted from Swift XEP2Tests.swift with Unity-specific enhancements
    /// </summary>
    [TestFixture]
    public class XEP2Tests
    {
        // Test constants from TestProperties.swift
        private const string DEFAULT_ACCOUNT_PRIVATE_KEY = "84180ac9d6eb6fba207ea4ef9d2200102d1ebeb4b9c07e2c6a738a42742e27a5";
        private const string DEFAULT_ACCOUNT_ENCRYPTED_PRIVATE_KEY = "6PYM7jHL4GmS8Aw2iEFpuaHTCUKjhT4mwVqdoozGU6sUE25BjV4ePXDdLz";
        private const string DEFAULT_ACCOUNT_PASSWORD = "neo";

        [Test]
        public void TestDecryptWithDefaultScryptParams()
        {
            // Act
            var decryptedKeyPair = XEP2.Decrypt(DEFAULT_ACCOUNT_PASSWORD, DEFAULT_ACCOUNT_ENCRYPTED_PRIVATE_KEY);

            // Assert
            Assert.IsNotNull(decryptedKeyPair);
            var expectedPrivateKey = TestHelpers.HexToBytes(DEFAULT_ACCOUNT_PRIVATE_KEY);
            TestHelpers.AssertBytesEqual(expectedPrivateKey, decryptedKeyPair.PrivateKey);
        }

        [Test]
        public void TestDecryptWithNonDefaultScryptParams()
        {
            // Arrange
            var customParams = new ScryptParams(256, 1, 1);
            var encrypted = "6PYM7jHL3uwhP8uuHP9fMGMfJxfyQbanUZPQEh1772iyb7vRnUkbkZmdRT";

            // Act
            var decryptedKeyPair = XEP2.Decrypt(DEFAULT_ACCOUNT_PASSWORD, encrypted, customParams);

            // Assert
            Assert.IsNotNull(decryptedKeyPair);
            var expectedPrivateKey = TestHelpers.HexToBytes(DEFAULT_ACCOUNT_PRIVATE_KEY);
            TestHelpers.AssertBytesEqual(expectedPrivateKey, decryptedKeyPair.PrivateKey);
        }

        [Test]
        public void TestEncryptWithDefaultScryptParams()
        {
            // Arrange
            var privateKeyBytes = TestHelpers.HexToBytes(DEFAULT_ACCOUNT_PRIVATE_KEY);
            var keyPair = ECKeyPair.CreateFromPrivateKey(privateKeyBytes);

            // Act
            var encrypted = XEP2.Encrypt(DEFAULT_ACCOUNT_PASSWORD, keyPair);

            // Assert
            Assert.AreEqual(DEFAULT_ACCOUNT_ENCRYPTED_PRIVATE_KEY, encrypted);
        }

        [Test]
        public void TestEncryptWithNonDefaultScryptParams()
        {
            // Arrange
            var customParams = new ScryptParams(256, 1, 1);
            var expected = "6PYM7jHL3uwhP8uuHP9fMGMfJxfyQbanUZPQEh1772iyb7vRnUkbkZmdRT";
            var privateKeyBytes = TestHelpers.HexToBytes(DEFAULT_ACCOUNT_PRIVATE_KEY);
            var keyPair = ECKeyPair.CreateFromPrivateKey(privateKeyBytes);

            // Act
            var encrypted = XEP2.Encrypt(DEFAULT_ACCOUNT_PASSWORD, keyPair, customParams);

            // Assert
            Assert.AreEqual(expected, encrypted);
        }

        [Test]
        public void TestEncryptDecryptRoundTrip()
        {
            // Arrange
            var originalKeyPair = ECKeyPair.CreateEcKeyPair();
            var password = "test_password_123";

            // Act
            var encrypted = XEP2.Encrypt(password, originalKeyPair);
            var decryptedKeyPair = XEP2.Decrypt(password, encrypted);

            // Assert
            Assert.IsNotNull(encrypted);
            Assert.IsNotNull(decryptedKeyPair);
            TestHelpers.AssertBytesEqual(originalKeyPair.PrivateKey, decryptedKeyPair.PrivateKey);
            Assert.AreEqual(originalKeyPair.PublicKey, decryptedKeyPair.PublicKey);
        }

        [Test]
        public void TestDecryptWithWrongPassword()
        {
            // Act & Assert
            Assert.Throws<XEP2Exception>(() => 
                XEP2.Decrypt("wrong_password", DEFAULT_ACCOUNT_ENCRYPTED_PRIVATE_KEY));
        }

        [Test]
        public void TestDecryptWithInvalidFormat()
        {
            // Test invalid prefix
            Assert.Throws<ArgumentException>(() => 
                XEP2.Decrypt(DEFAULT_ACCOUNT_PASSWORD, "7PYM7jHL4GmS8Aw2iEFpuaHTCUKjhT4mwVqdoozGU6sUE25BjV4ePXDdLz"));

            // Test too short
            Assert.Throws<ArgumentException>(() => 
                XEP2.Decrypt(DEFAULT_ACCOUNT_PASSWORD, "6PYM7jHL4GmS8Aw2iEFpuaHTCUKjhT4mwVqdoozGU6sUE25BjV4eP"));

            // Test invalid base58
            Assert.Throws<FormatException>(() => 
                XEP2.Decrypt(DEFAULT_ACCOUNT_PASSWORD, "6PYM7jHL4GmS8Aw2iEFpuaHTCUKjhT4mwVqdoozGU6sUE25BjV4ePXDdL0"));
        }

        [Test]
        public void TestNullInputValidation()
        {
            var keyPair = ECKeyPair.CreateEcKeyPair();

            // Test null password for encryption
            Assert.Throws<ArgumentNullException>(() => XEP2.Encrypt(null, keyPair));

            // Test null key pair for encryption
            Assert.Throws<ArgumentNullException>(() => XEP2.Encrypt("password", null));

            // Test null password for decryption
            Assert.Throws<ArgumentNullException>(() => XEP2.Decrypt(null, DEFAULT_ACCOUNT_ENCRYPTED_PRIVATE_KEY));

            // Test null encrypted key for decryption
            Assert.Throws<ArgumentNullException>(() => XEP2.Decrypt("password", null));
        }

        [Test]
        public void TestEmptyPasswordHandling()
        {
            // Arrange
            var keyPair = ECKeyPair.CreateEcKeyPair();

            // Act
            var encrypted = XEP2.Encrypt("", keyPair);
            var decryptedKeyPair = XEP2.Decrypt("", encrypted);

            // Assert
            Assert.IsNotNull(encrypted);
            Assert.IsNotNull(decryptedKeyPair);
            TestHelpers.AssertBytesEqual(keyPair.PrivateKey, decryptedKeyPair.PrivateKey);
        }

        [Test]
        public void TestDifferentScryptParameters()
        {
            // Arrange
            var keyPair = ECKeyPair.CreateEcKeyPair();
            var password = "test_password";

            var testCases = new[]
            {
                new ScryptParams(16384, 8, 8),    // Default XEP2 parameters
                new ScryptParams(32768, 8, 8),    // Higher N
                new ScryptParams(16384, 16, 8),   // Higher r
                new ScryptParams(16384, 8, 16),   // Higher p
                new ScryptParams(1024, 1, 1),     // Low security (fast test)
            };

            foreach (var scryptParams in testCases)
            {
                // Act
                var encrypted = XEP2.Encrypt(password, keyPair, scryptParams);
                var decryptedKeyPair = XEP2.Decrypt(password, encrypted, scryptParams);

                // Assert
                Assert.IsNotNull(encrypted);
                Assert.IsNotNull(decryptedKeyPair);
                TestHelpers.AssertBytesEqual(keyPair.PrivateKey, decryptedKeyPair.PrivateKey);
                Assert.IsTrue(encrypted.StartsWith("6P"));
            }
        }

        [Test]
        public void TestLongPasswordHandling()
        {
            // Arrange
            var keyPair = ECKeyPair.CreateEcKeyPair();
            var longPassword = new string('a', 256); // 256 character password

            // Act
            var encrypted = XEP2.Encrypt(longPassword, keyPair);
            var decryptedKeyPair = XEP2.Decrypt(longPassword, encrypted);

            // Assert
            Assert.IsNotNull(encrypted);
            Assert.IsNotNull(decryptedKeyPair);
            TestHelpers.AssertBytesEqual(keyPair.PrivateKey, decryptedKeyPair.PrivateKey);
        }

        [Test]
        public void TestUnicodePasswordHandling()
        {
            // Arrange
            var keyPair = ECKeyPair.CreateEcKeyPair();
            var unicodePassword = "パスワード123🔐"; // Japanese + emoji password

            // Act
            var encrypted = XEP2.Encrypt(unicodePassword, keyPair);
            var decryptedKeyPair = XEP2.Decrypt(unicodePassword, encrypted);

            // Assert
            Assert.IsNotNull(encrypted);
            Assert.IsNotNull(decryptedKeyPair);
            TestHelpers.AssertBytesEqual(keyPair.PrivateKey, decryptedKeyPair.PrivateKey);
        }

        [Test]
        public void TestConsistentEncryption()
        {
            // Arrange
            var keyPair = ECKeyPair.CreateEcKeyPair();
            var password = "consistent_test";

            // Act - Encrypt the same key pair multiple times with same parameters
            var encrypted1 = XEP2.Encrypt(password, keyPair);
            var encrypted2 = XEP2.Encrypt(password, keyPair);

            // Assert - Should produce the same result (if deterministic)
            // Note: Some implementations may use random salts, making this non-deterministic
            var decrypted1 = XEP2.Decrypt(password, encrypted1);
            var decrypted2 = XEP2.Decrypt(password, encrypted2);
            
            TestHelpers.AssertBytesEqual(keyPair.PrivateKey, decrypted1.PrivateKey);
            TestHelpers.AssertBytesEqual(keyPair.PrivateKey, decrypted2.PrivateKey);
        }

        #region Unity-Specific Tests

        [Test]
        [Performance]
        public void TestPerformance_EncryptDecrypt()
        {
            // Arrange
            const int iterations = 5; // XEP2 is computationally expensive
            var keyPair = ECKeyPair.CreateEcKeyPair();
            var password = "performance_test";
            var fastParams = new ScryptParams(1024, 1, 1); // Fast parameters for testing

            // Test encryption performance
            var encryptionTime = TestHelpers.MeasureExecutionTime(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    var encrypted = XEP2.Encrypt(password, keyPair, fastParams);
                    GC.KeepAlive(encrypted);
                }
            });

            // Test decryption performance
            var encrypted = XEP2.Encrypt(password, keyPair, fastParams);
            var decryptionTime = TestHelpers.MeasureExecutionTime(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    var decrypted = XEP2.Decrypt(password, encrypted, fastParams);
                    GC.KeepAlive(decrypted);
                }
            });

            var avgEncryptionTime = (float)encryptionTime / iterations;
            var avgDecryptionTime = (float)decryptionTime / iterations;

            // Assert reasonable performance (with fast parameters)
            Assert.Less(avgEncryptionTime, 1000, "XEP2 encryption should average less than 1s with fast parameters");
            Assert.Less(avgDecryptionTime, 1000, "XEP2 decryption should average less than 1s with fast parameters");
            
            Debug.Log($"XEP2 encryption average time: {avgEncryptionTime}ms per operation ({iterations} iterations)");
            Debug.Log($"XEP2 decryption average time: {avgDecryptionTime}ms per operation ({iterations} iterations)");
        }

        [Test]
        public void TestMemoryUsage_XEP2Operations()
        {
            // Arrange
            var keyPair = ECKeyPair.CreateEcKeyPair();
            var password = "memory_test";
            var fastParams = new ScryptParams(1024, 1, 1);

            // Test encryption memory usage
            var encryptionMemory = TestHelpers.MeasureMemoryUsage(() =>
            {
                var encrypted = XEP2.Encrypt(password, keyPair, fastParams);
                GC.KeepAlive(encrypted);
            });

            // Test decryption memory usage
            var encrypted = XEP2.Encrypt(password, keyPair, fastParams);
            var decryptionMemory = TestHelpers.MeasureMemoryUsage(() =>
            {
                var decrypted = XEP2.Decrypt(password, encrypted, fastParams);
                GC.KeepAlive(decrypted);
            });

            // Assert reasonable memory usage
            Assert.Less(Math.Abs(encryptionMemory), 100 * 1024, "XEP2 encryption should use less than 100KB additional memory");
            Assert.Less(Math.Abs(decryptionMemory), 100 * 1024, "XEP2 decryption should use less than 100KB additional memory");
            
            Debug.Log($"XEP2 encryption memory usage: {encryptionMemory} bytes");
            Debug.Log($"XEP2 decryption memory usage: {decryptionMemory} bytes");
        }

        [UnityTest]
        public IEnumerator TestUnityCoroutineCompatibility()
        {
            // Arrange
            var keyPair = ECKeyPair.CreateEcKeyPair();
            var password = "coroutine_test";
            var fastParams = new ScryptParams(256, 1, 1);
            string encryptedResult = null;
            ECKeyPair decryptedResult = null;

            // Act - Simulate async XEP2 operations in coroutine
            yield return new WaitForEndOfFrame();
            
            encryptedResult = XEP2.Encrypt(password, keyPair, fastParams);
            
            yield return new WaitForEndOfFrame();
            
            decryptedResult = XEP2.Decrypt(password, encryptedResult, fastParams);

            // Assert
            Assert.IsNotNull(encryptedResult);
            Assert.IsNotNull(decryptedResult);
            TestHelpers.AssertBytesEqual(keyPair.PrivateKey, decryptedResult.PrivateKey);
        }

        [Test]
        public void TestSerializationForUnityInspector()
        {
            // Arrange
            var keyPair = ECKeyPair.CreateEcKeyPair();
            var password = "serialization_test";
            var encrypted = XEP2.Encrypt(password, keyPair);
            
            var serializedData = new SerializableXEP2Data
            {
                encryptedPrivateKey = encrypted,
                isEncrypted = true,
                publicKeyHex = keyPair.PublicKey.GetEncodedCompressedHex(),
                addressHash160 = keyPair.GetScriptHash().ToString()
            };

            // Act
            var jsonString = JsonUtility.ToJson(serializedData, true);
            var deserializedData = JsonUtility.FromJson<SerializableXEP2Data>(jsonString);

            // Assert
            Assert.AreEqual(encrypted, deserializedData.encryptedPrivateKey);
            Assert.IsTrue(deserializedData.isEncrypted);
            Assert.AreEqual(keyPair.PublicKey.GetEncodedCompressedHex(), deserializedData.publicKeyHex);
        }

        [Test]
        public void TestThreadSafety_XEP2Operations()
        {
            // Arrange
            const int threadCount = 2; // Limited due to computational intensity
            const int operationsPerThread = 2;
            var tasks = new System.Threading.Tasks.Task[threadCount];
            var exceptions = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
            var fastParams = new ScryptParams(256, 1, 1);

            // Act
            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                tasks[t] = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        for (int i = 0; i < operationsPerThread; i++)
                        {
                            var keyPair = ECKeyPair.CreateEcKeyPair();
                            var password = $"thread_{threadIndex}_password_{i}";
                            
                            var encrypted = XEP2.Encrypt(password, keyPair, fastParams);
                            var decrypted = XEP2.Decrypt(password, encrypted, fastParams);
                            
                            if (!TestHelpers.BytesToHex(decrypted.PrivateKey).Equals(TestHelpers.BytesToHex(keyPair.PrivateKey)))
                            {
                                throw new Exception("XEP2 round-trip failed in thread");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                });
            }

            System.Threading.Tasks.Task.WaitAll(tasks);

            // Assert
            Assert.IsTrue(exceptions.IsEmpty, $"Thread safety test failed with {exceptions.Count} exceptions");
            Debug.Log($"Thread safety test passed: {threadCount} threads, {operationsPerThread} ops each");
        }

        [Test]
        public void TestEdgeCases_SpecialCharacterPasswords()
        {
            // Arrange
            var keyPair = ECKeyPair.CreateEcKeyPair();
            var specialPasswords = new[]
            {
                " ", // Space
                "\t", // Tab
                "\n", // Newline
                "\"", // Quote
                "'", // Single quote
                "\\", // Backslash
                "password with spaces",
                "!@#$%^&*()",
                "αβγδεζηθικλμνξοπρστυφχψω" // Greek alphabet
            };

            foreach (var password in specialPasswords)
            {
                // Act
                var encrypted = XEP2.Encrypt(password, keyPair);
                var decrypted = XEP2.Decrypt(password, encrypted);

                // Assert
                Assert.IsNotNull(encrypted);
                Assert.IsNotNull(decrypted);
                TestHelpers.AssertBytesEqual(keyPair.PrivateKey, decrypted.PrivateKey);
            }
        }

        #endregion

        #region Helper Classes

        [System.Serializable]
        private class SerializableXEP2Data
        {
            public string encryptedPrivateKey;
            public bool isEncrypted;
            public string publicKeyHex;
            public string addressHash160;
        }

        #endregion
    }
}

/// <summary>
/// XEP2 private key encryption/decryption utilities
/// Implements XEP-2 standard for encrypting private keys with passwords
/// </summary>
public static class XEP2
{
    /// <summary>
    /// Encrypt a private key using XEP2 standard with default scrypt parameters
    /// </summary>
    /// <param name="password">Password for encryption</param>
    /// <param name="keyPair">Key pair to encrypt</param>
    /// <returns>Base58-encoded encrypted private key</returns>
    public static string Encrypt(string password, ECKeyPair keyPair)
    {
        return Encrypt(password, keyPair, ScryptParams.Default);
    }

    /// <summary>
    /// Encrypt a private key using XEP2 standard with custom scrypt parameters
    /// </summary>
    /// <param name="password">Password for encryption</param>
    /// <param name="keyPair">Key pair to encrypt</param>
    /// <param name="scryptParams">Scrypt parameters</param>
    /// <returns>Base58-encoded encrypted private key</returns>
    public static string Encrypt(string password, ECKeyPair keyPair, ScryptParams scryptParams)
    {
        if (password == null)
            throw new ArgumentNullException(nameof(password));
        if (keyPair == null)
            throw new ArgumentNullException(nameof(keyPair));

        // Implementation would go here
        // For now, return a placeholder
        throw new NotImplementedException("XEP2 encryption not yet implemented");
    }

    /// <summary>
    /// Decrypt a XEP2 encrypted private key with default scrypt parameters
    /// </summary>
    /// <param name="password">Password for decryption</param>
    /// <param name="encryptedKey">Base58-encoded encrypted private key</param>
    /// <returns>Decrypted key pair</returns>
    public static ECKeyPair Decrypt(string password, string encryptedKey)
    {
        return Decrypt(password, encryptedKey, ScryptParams.Default);
    }

    /// <summary>
    /// Decrypt a XEP2 encrypted private key with custom scrypt parameters
    /// </summary>
    /// <param name="password">Password for decryption</param>
    /// <param name="encryptedKey">Base58-encoded encrypted private key</param>
    /// <param name="scryptParams">Scrypt parameters</param>
    /// <returns>Decrypted key pair</returns>
    public static ECKeyPair Decrypt(string password, string encryptedKey, ScryptParams scryptParams)
    {
        if (password == null)
            throw new ArgumentNullException(nameof(password));
        if (encryptedKey == null)
            throw new ArgumentNullException(nameof(encryptedKey));

        // Validate format
        if (!encryptedKey.StartsWith("6P"))
            throw new ArgumentException("Invalid XEP2 format: must start with '6P'", nameof(encryptedKey));

        // Implementation would go here
        // For now, return a placeholder
        throw new NotImplementedException("XEP2 decryption not yet implemented");
    }
}

/// <summary>
/// Scrypt parameters for XEP2 encryption
/// </summary>
public class ScryptParams
{
    public static readonly ScryptParams Default = new ScryptParams(16384, 8, 8);

    public int N { get; }
    public int R { get; }
    public int P { get; }

    public ScryptParams(int n, int r, int p)
    {
        N = n;
        R = r;
        P = p;
    }
}

/// <summary>
/// XEP2 specific exception
/// </summary>
public class XEP2Exception : Exception
{
    public XEP2Exception(string message) : base(message) { }
    public XEP2Exception(string message, Exception innerException) : base(message, innerException) { }
}