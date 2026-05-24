using DotNetEnv;
using System.Data;
using Microsoft.AspNetCore.Cors;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.Extensions.FileProviders;


// Load environment variables from .env file
Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:8081",          // Expo local dev
            "https://trackpulse-snowy.vercel.app")
        .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});


builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None; // required for cross-domain
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
});

// builder.Services.AddScoped<IDbConnection>(sp =>
// {
//     var conn = new NpgsqlConnection(
//         builder.Configuration.GetConnectionString("DefaultConnection"));
//     conn.Open();
//     return conn;
// });

// EF Core
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.CommandTimeout(30))
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
    );

// Dapper
builder.Services.AddSingleton<DapperContext>();

// Repositories
builder.Services.AddRepositories();

builder.Services.JWTAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
//builder.Services.AddHostedService<RaceSummaryRefresher>();

var app = builder.Build();


app.UseCors("AllowFrontend"); 


app.MapHub<OddsHub>("/OddsHub");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");


//app.ApplyDbMigrations(connectionString);

var uploadsRoot = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsRoot);
Directory.CreateDirectory(Path.Combine(uploadsRoot, "deposits"));

// 2. Serve files from "uploads" folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "uploads")),
    RequestPath = "/uploads"
});

// app.UseHttpsRedirection();

// app.UseAuthentication();   // ← Must come BEFORE UseAuthorization
// app.UseAuthorization();

app.MapControllers();

app.Run();
 record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
 {
     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
 }
