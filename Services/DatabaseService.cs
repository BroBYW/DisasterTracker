using SQLite;
using FinalAssignment.Models;

namespace FinalAssignment.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        async Task Init()
        {
            if (_database != null) return;
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "DisasterLogs_vFinal.db3");
            _database = new SQLiteAsyncConnection(dbPath);
            await _database.CreateTableAsync<IncidentLog>();
        }

        public async Task AddLogAsync(IncidentLog log)
        {
            await Init();
            await _database.InsertAsync(log);
        }

        public async Task<List<IncidentLog>> GetLogsAsync()
        {
            await Init();
            return await _database.Table<IncidentLog>().OrderByDescending(x => x.Timestamp).ToListAsync();
        }
    }
}