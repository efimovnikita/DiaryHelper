using DiaryHelper.Models;
using DiaryHelper.Services.Interfaces;
using SQLite;

namespace DiaryHelper.Services.Implementations;

public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            SQLitePCL.Batteries_V2.Init();

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "diaryhelper.db3");
            _database = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            await _database.CreateTableAsync<DiaryEntry>();
            await _database.CreateTableAsync<DiarySentence>();

            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }
        return _database!;
    }

    public async Task<List<DiaryEntry>> GetEntriesAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<DiaryEntry>()
                       .OrderByDescending(e => e.CreatedAt)
                       .ToListAsync();
    }

    public async Task<DiaryEntry?> GetEntryAsync(string id)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<DiaryEntry>()
                       .Where(e => e.Id == id)
                       .FirstOrDefaultAsync();
    }

    public async Task SaveEntryAsync(DiaryEntry entry)
    {
        var db = await GetDatabaseAsync();
        var existing = await db.Table<DiaryEntry>().Where(e => e.Id == entry.Id).FirstOrDefaultAsync();
        if (existing == null)
        {
            await db.InsertAsync(entry);
        }
        else
        {
            entry.UpdatedAt = DateTime.UtcNow;
            await db.UpdateAsync(entry);
        }
    }

    public async Task DeleteEntryAsync(string id)
    {
        var db = await GetDatabaseAsync();
        await db.RunInTransactionAsync(conn =>
        {
            conn.Table<DiarySentence>().Delete(s => s.EntryId == id);
            conn.Table<DiaryEntry>().Delete(e => e.Id == id);
        });
    }

    public async Task<List<DiarySentence>> GetSentencesAsync(string entryId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<DiarySentence>()
                       .Where(s => s.EntryId == entryId)
                       .OrderBy(s => s.OrderIndex)
                       .ToListAsync();
    }

    public async Task SaveSentencesAsync(string entryId, IEnumerable<DiarySentence> sentences)
    {
        var db = await GetDatabaseAsync();
        var sentenceList = sentences.ToList();

        // Assign order indices
        for (int i = 0; i < sentenceList.Count; i++)
        {
            sentenceList[i].EntryId = entryId;
            sentenceList[i].OrderIndex = i;
        }

        await db.RunInTransactionAsync(conn =>
        {
            conn.Table<DiarySentence>().Delete(s => s.EntryId == entryId);
            conn.InsertAll(sentenceList);
        });
    }

    public async Task DeleteSentenceAsync(string sentenceId)
    {
        var db = await GetDatabaseAsync();
        await db.Table<DiarySentence>().DeleteAsync(s => s.Id == sentenceId);
    }
}
