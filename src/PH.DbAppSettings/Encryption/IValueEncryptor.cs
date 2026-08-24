namespace PH.DbAppSettings.Encryption;

public interface IValueEncryptor
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
