using MemoryPack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Server.Models;

[MemoryPackable]
public partial class GameData
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [MemoryPackIgnore] // No need to serialize BsonId via MemoryPack
    public string? Id { get; set; }

    [MemoryPackOrder(0)]
    public string PlayerId { get; set; } = string.Empty;

    [MemoryPackOrder(1)]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    [MemoryPackOrder(2)]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
