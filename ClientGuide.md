# Hướng dẫn tạo Client API gửi và nhận dữ liệu Game (v2)

Tài liệu này hướng dẫn cách xây dựng Client để giao tiếp với Game Server (phiên bản bảo mật: dữ liệu truyền trong Body và hỗ trợ đa Game).

## 1. Chuẩn bị Model phía Client
Cài đặt thư viện `MemoryPack` và định nghĩa model giống như Server. Lưu ý thứ tự `MemoryPackOrder` cực kỳ quan trọng:

```csharp
using MemoryPack;
using System;

[MemoryPackable]
public partial class GameData
{
    [MemoryPackOrder(0)]
    public string GameName { get; set; } = string.Empty; // Tên game (ví dụ: DefendersOfTheDawn)

    [MemoryPackOrder(1)]
    public string PlayerId { get; set; } = string.Empty;

    [MemoryPackOrder(2)]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    [MemoryPackOrder(3)]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

// Request Data cho các phương thức khác
[MemoryPackable]
public partial record GetRequest(string GameName, string PlayerId);

[MemoryPackable]
public partial record DeleteRequest(string GameName, string PlayerId, string Password);

[MemoryPackable]
public partial record GenerateRequest(string GameName);
```

## 2. Lớp GameApiClient Pro
Lớp helper xử lý logic POST Body và các mã lỗi mới (như 429 - Quá tải request).

```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MemoryPack;
using Newtonsoft.Json;

public class GameApiClient
{
    private static readonly HttpClient _client = new HttpClient();
    private readonly string _baseUrl;

    public GameApiClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    // 1. Lưu dữ liệu (Nén binary qua MemoryPack)
    public async Task SaveData(string gameName, string playerId, byte[] rawData)
    {
        var gameData = new GameData { GameName = gameName, PlayerId = playerId, Data = rawData };
        byte[] binary = MemoryPackSerializer.Serialize(gameData);

        var content = new ByteArrayContent(binary);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-memorypack");

        var response = await _client.PostAsync($"{_baseUrl}/api/save", content);
        await HandleResponse(response);
    }

    // 2. Lấy dữ liệu (Dùng POST để gửi body kín)
    public async Task<GameData> GetData(string gameName, string playerId)
    {
        var request = new GetRequest(gameName, playerId);
        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"{_baseUrl}/api/get", content);
        await HandleResponse(response);

        string responseJson = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GameData>(responseJson);
    }

    // 3. Xóa dữ liệu (Kèm mật khẩu Admin)
    public async Task DeleteData(string gameName, string playerId, string password)
    {
        var request = new DeleteRequest(gameName, playerId, password);
        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"{_baseUrl}/api/delete", content);
        await HandleResponse(response);
    }

    private async Task HandleResponse(HttpResponseMessage response)
    {
        if (response.StatusCode == (HttpStatusCode)429)
        {
            string msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Rate Limited: {msg}");
        }
        
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Server Error ({response.StatusCode}): {error}");
        }
    }
}
```

## 3. Cách sử dụng

```csharp
var api = new GameApiClient("https://localhost:7103");
string myGame = "DefendersOfTheDawn";

try 
{
    // Lưu
    await api.SaveData(myGame, "User_99", new byte[] { 255, 128, 64 });

    // Lấy
    var data = await api.GetData(myGame, "User_99");

    // Xóa
    await api.DeleteData(myGame, "User_99", "admin@123");
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
```

---
### ⚠️ Lưu ý Quan trọng:
1.  **Chặn Request (Rate Limiting)**: Server giới hạn **5 requests / 10 giây** cho mỗi IP. Nếu vượt quá, bạn sẽ nhận lỗi HTTP 429. Hãy đảm bảo client không spam API.
2.  **Thông tin trong Body**: Không còn truyền dữ liệu nhạy cảm qua URL. Tất cả (GameName, PlayerId, Password) hiện đã nằm trong Request Body.
3.  **Game Name**: Tên game gửi lên phải khớp chính xác với danh sách `SupportedGames` trong cấu hình server.
