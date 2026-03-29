using Server.Services;
using MemoryPack;
using MemoryPack.AspNetCoreMvcFormatter;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, new MemoryPack.AspNetCoreMvcFormatter.MemoryPackInputFormatter());
    options.OutputFormatters.Insert(0, new MemoryPack.AspNetCoreMvcFormatter.MemoryPackOutputFormatter());
});

builder.Services.AddSingleton<MongoDbService>();

// Swagger UI configuration
builder.Services.AddEndpointsApiExplorer(); // Add this for Swagger to find endpoints
builder.Services.AddSwaggerGen();

// Rate Limiting Configuration
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10); // Khoảng thời gian 10 giây
        opt.PermitLimit = 10;                  // Tối đa 10 requests trong cửa sổ này
        opt.QueueLimit = 0;                    // Không cho phép xếp hàng, từ chối ngay lập tức
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Cấu hình dựa trên IP của người dùng
    options.AddPolicy("ip-based", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromSeconds(10), // Cửa sổ 10 giây
                PermitLimit = 5,                  // Giới hạn 5 requests cho mỗi IP
                QueueLimit = 0
            }));

    // Cấu hình phản hồi khi bị chặn
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            await context.HttpContext.Response.WriteAsync(
                $"Too many requests. Please try again after {retryAfter.TotalSeconds} second(s).", token);
        }
        else
        {
            await context.HttpContext.Response.WriteAsync(
                "Too many requests. Please try again later.", token);
        }
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // Enable Swagger middleware (generates the json)
    app.UseSwaggerUI(); // Enable Swagger UI (interactive page)
}

app.UseHttpsRedirection();

app.UseRateLimiter(); // Use Rate Limiting middleware

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers().RequireRateLimiting("ip-based"); // Apply policy to all controllers

app.Run();