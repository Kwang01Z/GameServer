using Microsoft.AspNetCore.Mvc;
using MemoryPack;
using Server.Models;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api")]
public class GameDataController : ControllerBase
{
    private readonly MongoDbService _mongoDbService;
    private readonly string _adminPassword;

    public GameDataController(MongoDbService mongoDbService, IConfiguration config)
    {
        _mongoDbService = mongoDbService;
        _adminPassword = config["AdminPassword"] ?? "admin123"; // Default if not set
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] GameData gameData)
    {
        gameData.LastUpdated = DateTime.UtcNow;
        await _mongoDbService.CreateAsync(gameData);
        return Ok(new ApiResponse("Dữ liệu game đã được lưu", gameData.Id));
    }

    [HttpGet("get/{playerId}")]
    public async Task<ActionResult<GameData?>> Get(string playerId)
    {
        var gameData = await _mongoDbService.GetAsync(playerId);
        if (gameData == null)
        {
            return NotFound(new ApiResponse("Không tìm thấy dữ liệu cho người chơi này"));
        }
        return Ok(gameData);
    }

    [HttpDelete("delete/{playerId}")]
    public async Task<IActionResult> Delete(string playerId, [FromQuery] string password)
    {
        if (password != _adminPassword)
        {
            return Unauthorized(new ApiResponse("Mật khẩu không chính xác"));
        }

        // Logic xóa (cần thêm method vào MongoDbService)
        var deleted = await _mongoDbService.DeleteAsync(playerId);
        if (!deleted)
        {
            return NotFound(new ApiResponse("Không tìm thấy dữ liệu để xóa"));
        }

        return Ok(new ApiResponse("Dữ liệu đã được xóa thành công"));
    }

    [HttpGet("generate")]
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateRandom()
    {
        var random = new Random();
        var randomBytes = new byte[32]; // Tạo 32 bytes ngẫu nhiên
        random.NextBytes(randomBytes);

        var gameData = new GameData
        {
            PlayerId = "User_" + Guid.NewGuid().ToString().Substring(0, 8),
            Data = randomBytes,
            LastUpdated = DateTime.UtcNow
        };

        await _mongoDbService.CreateAsync(gameData);
        return Ok(new GenerateResponse("Dữ liệu game ngẫu nhiên đã được tạo và lưu", gameData));
    }
}

[MemoryPackable]
public partial record ApiResponse(string Message, string? Id = null);

[MemoryPackable]
public partial record GenerateResponse(string Message, GameData GameData);
