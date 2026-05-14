using PH.DbAppSettings.Encryption;

namespace PH.DbAppSettings.Tests;

public class EncryptionTests
{
    [Fact]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginalValue()
    {
        using var encryptor = new AesGcmValueEncryptor("my-test-secret");
        var original = "Hello, World!";

        var encrypted = encryptor.Encrypt(original);
        var decrypted = encryptor.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Encrypt_ProducesDifferentOutputEachTime()
    {
        using var encryptor = new AesGcmValueEncryptor("my-test-secret");
        var original = "same-value";

        var enc1 = encryptor.Encrypt(original);
        var enc2 = encryptor.Encrypt(original);

        Assert.NotEqual(enc1, enc2);
    }

    [Fact]
    public void Decrypt_WithWrongSecret_ThrowsException()
    {
        using var encryptor1 = new AesGcmValueEncryptor("secret-1");
        using var encryptor2 = new AesGcmValueEncryptor("secret-2");

        var encrypted = encryptor1.Encrypt("sensitive-value");

        Assert.ThrowsAny<Exception>(() => encryptor2.Decrypt(encrypted));
    }

    [Theory]
    [InlineData("simple string")]
    [InlineData("true")]
    [InlineData("42")]
    [InlineData("3.14")]
    [InlineData("2024-01-01T00:00:00Z")]
    [InlineData("")]
    public void EncryptDecrypt_VariousValueTypes_RoundTrip(string value)
    {
        using var encryptor = new AesGcmValueEncryptor("test-secret-key");

        var encrypted = encryptor.Encrypt(value);
        var decrypted = encryptor.Decrypt(encrypted);

        Assert.Equal(value, decrypted);
    }
}
