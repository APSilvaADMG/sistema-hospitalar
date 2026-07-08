using System.Security.Cryptography;

namespace SistemaHospitalar.Mobile.Services;

public class SecureDatabaseService
{
    private const string KeyName = "db_encryption_key";

    public async Task<string> GetOrCreateKeyAsync()
    {
        var existing = await SecureStorage.Default.GetAsync(KeyName);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await SecureStorage.Default.SetAsync(KeyName, key);
        return key;
    }
}
