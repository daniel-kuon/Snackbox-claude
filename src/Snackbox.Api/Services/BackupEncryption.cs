using System.Security.Cryptography;
using System.Text;

namespace Snackbox.Api.Services;

/// <summary>
/// Encrypts backups before they leave the machine (OneDrive): AES-256-GCM with a key derived from
/// the admin's passphrase by PBKDF2-SHA256. Authenticated, so a wrong passphrase or a tampered
/// file fails to decrypt instead of producing garbage SQL.
/// File: "SNACKBOXENC1" | salt (16) | iterations (int32 LE) | nonce (12) | tag (16) | ciphertext.
/// </summary>
public static class BackupEncryption
{
    private static readonly byte[] Magic = "SNACKBOXENC1"u8.ToArray();
    private const int SaltSize = 16, NonceSize = 12, TagSize = 16, KeySize = 32;
    private const int Iterations = 600_000; // OWASP's current recommendation for PBKDF2-SHA256
    private static readonly int HeaderSize = Magic.Length + SaltSize + sizeof(int) + NonceSize + TagSize;

    public const string FileExtension = ".enc";

    public static byte[] Encrypt(byte[] plain, string passphrase)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var key = DeriveKey(passphrase, salt, Iterations);

        var output = new byte[HeaderSize + plain.Length];
        var span = output.AsSpan();
        Magic.CopyTo(span);
        salt.CopyTo(span[Magic.Length..]);
        BitConverter.TryWriteBytes(span.Slice(Magic.Length + SaltSize, sizeof(int)), Iterations);
        nonce.CopyTo(span[(Magic.Length + SaltSize + sizeof(int))..]);
        var tag = span.Slice(Magic.Length + SaltSize + sizeof(int) + NonceSize, TagSize);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plain, span[HeaderSize..], tag);
        CryptographicOperations.ZeroMemory(key);
        return output;
    }

    public static bool IsEncrypted(ReadOnlySpan<byte> data) => data.StartsWith(Magic);

    /// <exception cref="CryptographicException">Wrong passphrase, or the file was changed.</exception>
    public static byte[] Decrypt(byte[] encrypted, string passphrase)
    {
        if (encrypted.Length < HeaderSize || !IsEncrypted(encrypted))
            throw new CryptographicException("Not an encrypted Snackbox backup");

        var span = encrypted.AsSpan();
        var salt = span.Slice(Magic.Length, SaltSize);
        var iterations = BitConverter.ToInt32(span.Slice(Magic.Length + SaltSize, sizeof(int)));
        if (iterations is < 100_000 or > 10_000_000) throw new CryptographicException("Not an encrypted Snackbox backup");
        var nonce = span.Slice(Magic.Length + SaltSize + sizeof(int), NonceSize);
        var tag = span.Slice(Magic.Length + SaltSize + sizeof(int) + NonceSize, TagSize);

        var key = DeriveKey(passphrase, salt.ToArray(), iterations);
        var plain = new byte[encrypted.Length - HeaderSize];
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, span[HeaderSize..], tag, plain);
            return plain;
        }
        catch (AuthenticationTagMismatchException ex)
        {
            throw new CryptographicException("Wrong passphrase, or the backup file is damaged", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] DeriveKey(string passphrase, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(passphrase), salt, iterations, HashAlgorithmName.SHA256, KeySize);
}
