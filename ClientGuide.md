# Hướng dẫn tạo Client API gửi và nhận dữ liệu Game

Tài liệu này hướng dẫn cách xây dựng một lớp Client đơn giản trong C# (Unity hoặc .NET) để giao tiếp với Game Server của bạn.

## 1. Chuẩn bị Model phía Client
Bạn cần cài đặt thư viện `MemoryPack` phía Client và định nghĩa model giống như Server:

```csharp
using MemoryPack;
using System;

[MemoryPackable]
public partial class GameData
{
    // Không cần field Id vì server tự sinh
    [MemoryPackOrder(0)]
    public string PlayerId { get; set; } = string.Empty;

    [MemoryPackOrder(1)]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    [MemoryPackOrder(2)]
    public DateTime LastUpdated { get; set; }
}
```

## 2. Lớp GameApiClient
Dưới đây là lớp helper để bạn thực hiện các thao tác Save, Get và Delete.

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using MemoryPack;

public class GameApiClient
{
    private static readonly HttpClient client = new HttpClient();
    private string _baseUrl;

    public GameApiClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    // 1. Lưu dữ liệu (Nén MemoryPack)
    public async Task SaveData(string playerId, byte[] rawData)
    {
        var gameData = new GameData { PlayerId = playerId, Data = rawData };
        byte[] binary = MemoryPackSerializer.Serialize(gameData);

        var content = new ByteArrayContent(binary);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-memorypack");

        var response = await client.PostAsync($"{_baseUrl}/api/save", content);
        response.EnsureSuccessStatusCode();
        Console.WriteLine("Lưu dữ liệu thành công!");
    }

    // 2. Lấy dữ liệu (Nhận JSON từ Swagger hoặc Binary nếu cấu hình thêm)
    public async Task<GameData> GetData(string playerId)
    {
        var response = await client.GetAsync($"{_baseUrl}/api/get/{playerId}");
        response.EnsureSuccessStatusCode();

        // Mặc định server trả JSON cho trình duyệt/client thông thường
        string json = await response.Content.ReadAsStringAsync();
        return Newtonsoft.Json.JsonConvert.DeserializeObject<GameData>(json);
    }

    // 3. Xóa dữ liệu (Yêu cầu mật khẩu)
    public async Task DeleteData(string playerId, string password)
    {
        var response = await client.DeleteAsync($"{_baseUrl}/api/delete/{playerId}?password={password}");
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Xóa dữ liệu thành công!");
        }
        else
        {
            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Lỗi xóa: {error}");
        }
    }
}
```

## 3. Cách sử dụng
```csharp
var api = new GameApiClient("https://localhost:7103");

// Lưu
await api.SaveData("Player_001", new byte[] { 1, 2, 3, 4 });

// Lấy
var data = await api.GetData("Player_001");

// Xóa (Mật khẩu mặc định là admin123)
await api.DeleteData("Player_001", "admin123");
```

---
**Lưu ý:**
- **Endpoint mới:** `/api/save`, `/api/get/{playerId}`, `/api/delete/{playerId}`.
- **Mật khẩu xóa:** Có thể thay đổi trong code Server hoặc `appsettings.json`.
