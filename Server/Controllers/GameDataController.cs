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
        try
        {
            gameData.LastUpdated = DateTime.UtcNow;
            await _mongoDbService.CreateAsync(gameData.GameName, gameData);
            return Ok(new ApiResponse("Dữ liệu game đã được lưu", gameData.Id));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse(ex.Message));
        }
    }

    [HttpPost("get")]
    public async Task<ActionResult<GameData?>> Get([FromBody] GetRequest request)
    {
        try
        {
            var gameData = await _mongoDbService.GetAsync(request.GameName, request.PlayerId);
            if (gameData == null)
            {
                return NotFound(new ApiResponse("Không tìm thấy dữ liệu cho người chơi này"));
            }
            return Ok(gameData);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse(ex.Message));
        }
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
    {
        if (request.Password != _adminPassword)
        {
            return Unauthorized(new ApiResponse("Mật khẩu không chính xác"));
        }

        try
        {
            var deleted = await _mongoDbService.DeleteAsync(request.GameName, request.PlayerId);
            if (!deleted)
            {
                return NotFound(new ApiResponse("Không tìm thấy dữ liệu để xóa"));
            }
            return Ok(new ApiResponse("Dữ liệu đã được xóa thành công"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse(ex.Message));
        }
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateRandom()
    {
        try
        {
            var random = new Random();
            var randomBytes = new byte[32];
            random.NextBytes(randomBytes);

            var gameData = new GameData
            {
                GameName = "DefendersOfTheDawn",
                PlayerId = "User_" + Guid.NewGuid().ToString().Substring(0, 8),
                Data = randomBytes,
                LastUpdated = DateTime.UtcNow
            };

            await _mongoDbService.CreateAsync(gameData.GameName, gameData);
            return Ok(new GenerateResponse("Dữ liệu game ngẫu nhiên đã được tạo và lưu", gameData));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse(ex.Message));
        }
    }
}

[MemoryPackable]
public partial record GetRequest(string GameName, string PlayerId);

[MemoryPackable]
public partial record DeleteRequest(string GameName, string PlayerId, string Password);

[MemoryPackable]
public partial record GenerateRequest(string GameName);

[MemoryPackable]
public partial record ApiResponse(string Message, string? Id = null);

[MemoryPackable]
public partial record GenerateResponse(string Message, GameData GameData);
