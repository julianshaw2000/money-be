rompt – .NET API (server) for money-be
You are a senior .NET API engineer.  The `money-be` repository is currently empty except for a README:contentReference[oaicite:1]{index=1}.  Your task is to scaffold a fully functional ASP.NET Core Web API to power the “donations” feature used by the Angular app in a companion repo.

High-level goals
----------------
- Build an ASP.NET Core 7/8 API project in this repo.
- Use PostgreSQL via Npgsql.  Read the database connection from an environment variable called `DATABASE_URL` and convert it to a proper Npgsql connection string (host, port, username, password, database, SSL options).
- Expose a `POST /api/v1/donations` endpoint that accepts a donation payload and persists it to the database.
- Return RFC7807 problem details on validation errors.
- Enable CORS for `http://localhost:4200` to let the Angular dev server call the API.
- Configure JSON serialization so enums are represented as strings.

Repository structure
--------------------
Since the repo is empty:contentReference[oaicite:2]{index=2}, create the following structure:



money-be/
money_be.csproj
Program.cs
Data/
AppDbContext.cs
Models/
Donation.cs
DTOs/
DonationDtos.cs
Controllers/
DonationsController.cs
Properties/
launchSettings.json


Key requirements
----------------

1. **Database configuration**
   - In `Program.cs`, read `DATABASE_URL` from configuration:
     ```csharp
     string? databaseUrl = builder.Configuration["DATABASE_URL"];
     string? npgsqlConnectionString = null;
     if (!string.IsNullOrWhiteSpace(databaseUrl)) {
         var uri = new Uri(databaseUrl);
         var userInfo = uri.UserInfo.Split(':', 2);
         var username = Uri.UnescapeDataString(userInfo[0]);
         var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
         var host = uri.Host;
         var port = uri.IsDefaultPort ? 5432 : uri.Port;
         var database = uri.AbsolutePath.TrimStart('/');
         var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
         var sslMode = queryParams.ContainsKey("sslmode") ? queryParams["sslmode"] : "Require";
         var channelBinding = queryParams.ContainsKey("channel_binding") ? queryParams["channel_binding"] : null;
         var sb = new System.Text.StringBuilder();
         sb.Append($"Host={host};Port={port};Username={username};Password={password};Database={database};SslMode={sslMode};");
         if (!string.IsNullOrEmpty(channelBinding)) sb.Append($"ChannelBinding={channelBinding};");
         npgsqlConnectionString = sb.ToString();
     }
     ```
   - Register `AppDbContext` using `options.UseNpgsql(npgsqlConnectionString)`.

2. **Entity and DTOs**
   - Create a `Donation` entity with:
     ```csharp
     public Guid Id { get; set; }
     [Required, EmailAddress] public string Email { get; set; } = null!;
     public string? FirstName { get; set; }
     public string? LastName { get; set; }
     [Range(1,int.MaxValue)] public int AmountMinor { get; set; }
     public Currency Currency { get; set; } = Currency.USD;
     public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
     ```
   - DTOs in `DTOs/DonationDtos.cs`:
     ```csharp
     [JsonConverter(typeof(JsonStringEnumConverter))]
     public enum Currency { USD, GBP, EUR }

     public sealed class DonationCreateDto {
         [Required, EmailAddress] public string Email { get; set; } = null!;
         public string? FirstName { get; set; }
         public string? LastName { get; set; }
         [Range(1,int.MaxValue)] public int AmountMinor { get; set; }
         [Required] public Currency Currency { get; set; } = Currency.USD;
         public DateTime? CreatedAt { get; set; } // optional client timestamp
     }

     public sealed class DonationReadDto {
         public Guid Id { get; set; }
         public string Email { get; set; } = null!;
         public string? FirstName { get; set; }
         public string? LastName { get; set; }
         public int AmountMinor { get; set; }
         public Currency Currency { get; set; }
         public DateTime CreatedAt { get; set; }
     }
     ```

3. **DbContext**
   - Define `AppDbContext` with `DbSet<Donation> Donations`.  Configure entity properties (table name, keys) if necessary.

4. **Program.cs**
   - Add services:
     ```csharp
     builder.Services.AddControllers();
     builder.Services.AddEndpointsApiExplorer();
     builder.Services.AddSwaggerGen();
     builder.Services.Configure<JsonOptions>(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
     builder.Services.AddCors(o => o.AddPolicy("spa", p => p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
     ```
   - Register `AppDbContext` as above.
   - Build the app; call `app.UseSwagger()`, `app.UseSwaggerUI()`, `app.UseCors("spa")`, `app.UseRouting()`, `app.UseAuthorization()`, `app.MapControllers()`, `app.Run()`.

5. **Controller**
   - `DonationsController` with `[ApiController]` and `[Route("api/v1/[controller]")]`.
   - A `POST` action that:
     - Receives `DonationCreateDto`.
     - Validates `ModelState`.
     - Converts to `Donation` entity, overrides `CreatedAt` to `DateTime.UtcNow`, trims string properties.
     - Saves to DB.
     - Returns `CreatedAtAction` with `DonationReadDto`.
   - A `GET("{id:guid}")` action to retrieve a donation by id.

6. **Database initialization**
   - Add migration and create database automatically if using EF Core migrations (optional).  Alternatively, write a quick seeding script.

7. **Testing**
   - After scaffolding, run with `dotnet run` and test with:
     ```
     curl -X POST http://localhost:5229/api/v1/donations \
       -H "Content-Type: application/json" \
       -d '{ "email":"donor@example.com","firstName":"Ada","lastName":"Lovelace","amountMinor":5000,"currency":"GBP" }'
     ```
   - It should return 201 with an `id` and `createdAt`.

The result should be a fully working ASP.NET Core API that can persist do