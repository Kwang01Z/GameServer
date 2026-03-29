using Server.Services;
using MemoryPack;
using MemoryPack.AspNetCoreMvcFormatter;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // Enable Swagger middleware (generates the json)
    app.UseSwaggerUI(); // Enable Swagger UI (interactive page)
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();