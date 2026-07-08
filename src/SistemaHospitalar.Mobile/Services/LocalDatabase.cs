using SQLite;
using SQLitePCL;
using SistemaHospitalar.Mobile.Models;

namespace SistemaHospitalar.Mobile.Services;

public class LocalDatabase
{
    private SQLiteAsyncConnection? _db;
    private bool _initialized;

    public async Task InitializeAsync(string encryptionKey)
    {
        if (_initialized)
        {
            return;
        }

        Batteries_V2.Init();
        raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlcipher());

        var path = Path.Combine(FileSystem.AppDataDirectory, "apsmedcore-maqueiro.db3");
        var flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;
        var connectionString = new SQLiteConnectionString(path, flags, storeDateTimeAsTicks: false, key: encryptionKey);
        _db = new SQLiteAsyncConnection(connectionString);

        await _db.CreateTableAsync<SyncMetadata>();
        await _db.CreateTableAsync<OutboxMutation>();
        await _db.CreateTableAsync<CachedTransportRequest>();
        _initialized = true;
    }

    private SQLiteAsyncConnection Db => _db ?? throw new InvalidOperationException("Banco local não inicializado.");

    public Task<List<OutboxMutation>> GetPendingMutationsAsync()
        => Db.Table<OutboxMutation>().Where(m => m.Status == "Pending").OrderBy(m => m.ClientTimestamp).ToListAsync();

    public Task<int> InsertMutationAsync(OutboxMutation mutation)
        => Db.InsertAsync(mutation);

    public Task<int> UpdateMutationAsync(OutboxMutation mutation)
        => Db.UpdateAsync(mutation);

    public Task ReplaceTransportsAsync(IEnumerable<CachedTransportRequest> items)
    {
        return Db.RunInTransactionAsync(conn =>
        {
            conn.DeleteAll<CachedTransportRequest>();
            conn.InsertAll(items);
        });
    }

    public Task<List<CachedTransportRequest>> GetActiveTransportsAsync()
        => Db.Table<CachedTransportRequest>()
            .Where(t => t.Status != "Completed" && t.Status != "Cancelled")
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.RequestedAt)
            .ToListAsync();

    public async Task<CachedTransportRequest?> GetTransportAsync(Guid id)
        => await Db.Table<CachedTransportRequest>().Where(t => t.Id == id).FirstOrDefaultAsync();

    public Task<int> UpsertTransportAsync(CachedTransportRequest item)
        => Db.InsertOrReplaceAsync(item);

    public async Task<string?> GetMetaAsync(string key)
    {
        var row = await Db.Table<SyncMetadata>().Where(m => m.Key == key).FirstOrDefaultAsync();
        return row?.Value;
    }

    public async Task SetMetaAsync(string key, string value)
    {
        await Db.InsertOrReplaceAsync(new SyncMetadata { Key = key, Value = value });
    }
}
