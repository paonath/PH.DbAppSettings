using System.Security.Cryptography;
using System.Text;

namespace PH.DbAppSettings.Encryption;

/// <summary>
/// Implementazione di IValueEncryptor basata su AES-GCM 256-bit.
/// Formato output Base64: nonce (12 byte) + tag (16 byte) + ciphertext.
/// </summary>
public sealed class AesGcmValueEncryptor : IValueEncryptor, IDisposable
{
    private readonly byte[] _key;

    /// <param name="secret">Secret da cui derivare la chiave AES-256 (SHA-256 del secret).</param>
    public AesGcmValueEncryptor(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public string Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];     // 16 bytes
        var cipherText = new byte[plainBytes.Length];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, tag.Length);
        aes.Encrypt(nonce, plainBytes, cipherText, tag);

        var result = new byte[nonce.Length + tag.Length + cipherText.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherText, 0, result, nonce.Length + tag.Length, cipherText.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherTextBase64)
    {
        ArgumentNullException.ThrowIfNull(cipherTextBase64);

        var data = Convert.FromBase64String(cipherTextBase64);

        const int nonceSize = 12;
        const int tagSize = 16;

        if (data.Length < nonceSize + tagSize)
            throw new ArgumentException("Invalid encrypted data length.", nameof(cipherTextBase64));

        var nonce = data[..nonceSize];
        var tag = data[nonceSize..(nonceSize + tagSize)];
        var cipherBytes = data[(nonceSize + tagSize)..];
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, tagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_key);
    }
}
