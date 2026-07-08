using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Infrastructure.Security;

public class FieldEncryptionService(IOptions<FieldEncryptionOptions> options) : IFieldEncryptionService
{
    private const string Prefix = "ENC1:";
    private readonly byte[] _key = DeriveKey(options.Value.Key, 32);
    private readonly byte[] _hashKey = DeriveKey(options.Value.HashKey, 32);

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext) || IsEncrypted(plaintext))
        {
            return plaintext;
        }

        var nonce = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        var payload = new byte[nonce.Length + tag.Length + cipher.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipher, 0, payload, nonce.Length + tag.Length, cipher.Length);

        return Prefix + Convert.ToBase64String(payload);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext) || !IsEncrypted(ciphertext))
        {
            return ciphertext;
        }

        var payload = Convert.FromBase64String(ciphertext[Prefix.Length..]);
        var nonce = payload[..12];
        var tag = payload[12..28];
        var cipher = payload[28..];
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }

    public bool IsEncrypted(string value) => value.StartsWith(Prefix, StringComparison.Ordinal);

    public string HashForLookup(string plaintext)
    {
        using var hmac = new HMACSHA256(_hashKey);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(plaintext))).ToLowerInvariant();
    }

    private static byte[] DeriveKey(string source, int size)
    {
        var material = string.IsNullOrWhiteSpace(source)
            ? "APSMedCore-Dev-Encryption-Key-Change-In-Production!!"
            : source;

        return SHA256.HashData(Encoding.UTF8.GetBytes(material))[..size];
    }
}
