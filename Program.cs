
using Microsoft.EntityFrameworkCore;
using money_be.Data;
using money_be.DTOs;
using money_be.Models;

var builder = WebApplication.CreateBuilder(args);

// Convert DATABASE_URL to Npgsql connection string using QueryHelpers
string? databaseUrl = builder.Configuration["DATABASE_URL"];
string? npgsqlConnectionString = null;
if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
    var host = uri.Host;
    var port = uri.IsDefaultPort ? 5432 : uri.Port;
    var database = uri.AbsolutePath.TrimStart('/');
    var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
    var sslMode = queryParams.ContainsKey("sslmode") ? queryParams["sslmode"].ToString() : "Require";
    var channelBinding = queryParams.ContainsKey("channel_binding") ? queryParams["channel_binding"].ToString() : string.Empty;
    var sb = new System.Text.StringBuilder();
    sb.Append($"Host={host};Port={port};Username={username};Password={password};Database={database};SslMode={sslMode};");
    if (!string.IsNullOrWhiteSpace(channelBinding)) sb.Append($"ChannelBinding={channelBinding};");
    npgsqlConnectionString = sb.ToString();
}
builder.Services.AddDbContext<money_be.Data.AppDbContext>(options =>
    options.UseNpgsql(npgsqlConnectionString));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o => o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddCors(o => o.AddPolicy("spa", p => p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("spa");
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.Run();

