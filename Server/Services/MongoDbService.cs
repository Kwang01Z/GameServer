using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Server.Models;

namespace Server.Services;

public class MongoDbService
{
    private readonly IMongoDatabase _database;
    private readonly HashSet<string> _supportedGames;
    private readonly ConcurrentDictionary<string, IMongoCollection<GameData>> _collections = new();

    public MongoDbService(IConfiguration config)
    {
        var mongoClient = new MongoClient(config.GetConnectionString("MongoDb"));
        var dbName = config["MongoDb:DatabaseName"] ?? "GameServerDb";
        _database = mongoClient.GetDatabase(dbName);
        
        // Lấy danh sách game hỗ trợ từ config
        var games = config.GetSection("MongoDb:SupportedGames").Get<string[]>() ?? [];
        _supportedGames = new HashSet<string>(games, StringComparer.OrdinalIgnoreCase);
    }

    private IMongoCollection<GameData> GetCollection(string gameName)
    {
        // Kiểm tra xem game có nằm trong danh sách hỗ trợ không
        if (!_supportedGames.Contains(gameName))
        {
            throw new ArgumentException($"Game '{gameName}' không được hỗ trợ.");
        }

        // Trả về từ cache hoặc khởi tạo mới
        return _collections.GetOrAdd(gameName, name =>
        {
            var collection = _database.GetCollection<GameData>($"G_{name}");

            // Tạo Index duy nhất cho PlayerId nếu chưa có
            var indexKeys = Builders<GameData>.IndexKeys.Ascending(x => x.PlayerId);
            var indexOptions = new CreateIndexOptions { Unique = true };
            collection.Indexes.CreateOne(new CreateIndexModel<GameData>(indexKeys, indexOptions));

            return collection;
        });
    }

    public async Task CreateAsync(string gameName, GameData gameData)
    {
        var collection = GetCollection(gameName);
        await collection.ReplaceOneAsync(
            x => x.PlayerId == gameData.PlayerId, 
            gameData, 
            new ReplaceOptions { IsUpsert = true });
    }

    public async Task<GameData?> GetAsync(string gameName, string playerId)
    {
        var collection = GetCollection(gameName);
        return await collection.Find(x => x.PlayerId == playerId).FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteAsync(string gameName, string playerId)
    {
        var collection = GetCollection(gameName);
        var result = await collection.DeleteManyAsync(x => x.PlayerId == playerId);
        return result.DeletedCount > 0;
    }
    
    public IEnumerable<string> GetSupportedGames() => _supportedGames;
}
