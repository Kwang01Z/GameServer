using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Server.Models;

namespace Server.Services;

public class MongoDbService
{
    private readonly IMongoCollection<GameData> _gameDataCollection;

    public MongoDbService(IConfiguration config)
    {
        var mongoClient = new MongoClient(config.GetConnectionString("MongoDb"));
        var database = mongoClient.GetDatabase(config["MongoDb:DatabaseName"] ?? "GameServerDb");
        _gameDataCollection = database.GetCollection<GameData>("GameData");

        // Tạo Index cho PlayerId và bắt buộc duy nhất (Unique) để tối ưu tìm kiếm
        var indexKeys = Builders<GameData>.IndexKeys.Ascending(x => x.PlayerId);
        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexModel = new CreateIndexModel<GameData>(indexKeys, indexOptions);
        _gameDataCollection.Indexes.CreateOne(indexModel);
    }

    public async Task CreateAsync(GameData gameData)
    {
        await _gameDataCollection.InsertOneAsync(gameData);
    }

    public async Task<GameData?> GetAsync(string playerId)
    {
        // Vì PlayerId đã có index và là duy nhất, không cần Sort nữa
        return await _gameDataCollection.Find(x => x.PlayerId == playerId)
            .Limit(1)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteAsync(string playerId)
    {
        var result = await _gameDataCollection.DeleteManyAsync(x => x.PlayerId == playerId);
        return result.DeletedCount > 0;
    }
}
