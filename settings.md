/*
COPILOT PROMPT — BUILD DONOR WRITE API (PostgreSQL, EF Core, Minimal API)

Goal:
Create a minimal .NET API that writes a "Donor" record to PostgreSQL.
Expose POST endpoint at /api/aaassddffee.
Use Program.cs (no Startup.cs). C# 7-compatible style where relevant.
Use EF Core + Npgsql provider. Validate input (email required & unique).

Project structure to generate:
- Models/Donor.cs
- Data/AppDbContext.cs
- Data/Migrations/ (EF Core migrations)
- DTOs/DonorCreateDto.cs
- Program.cs (configure services + map endpoints)
- appsettings.json (ConnectionStrings:Postgres)

Donor entity:
- Id: Guid (PK, generated)
- Email: string, required, unique, max 320
- FirstName: string?, max 100
- LastName: string?, max 100
- AmountMinor: int (e.g., 1234 = £12.34)
- Currency: enum { GBP, USD, EUR } default GBP
- CreatedAt: DateTime (UTC, default now)

DTO (DonorCreateDto):
- Email (required)
- FirstName (optional)
- LastName (optional)
- AmountMinor (required, >=1)
- Currency (optional: GBP|USD|EUR)

Behavior:
- POST /api/aaassddffee accepts JSON DonorCreateDto
- Validate ModelState; return 400 with errors if invalid
- Enforce Email unique (DB unique index); on conflict return 409
- On success, persist Donor and return 201 with { id, email, ... } and Location header

EF Core configuration:
- Use Npgsql (Microsoft.EntityFrameworkCore + Npgsql.EntityFrameworkCore.PostgreSQL)
- Connection string key: "ConnectionStrings:Postgres"
- Create unique index on Donor.Email
- Ensure CreatedAt stored in UTC
- Add DbContext in DI

Program.cs (minimal):
- builder.Services.AddDbContext<AppDbContext>(…)
- builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen();
- MapPost("/api/aaassddffee", …)
- app.UseSwagger(); app.UseSwaggerUI();

appsettings.json example (placeholder):
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=donations;Username=postgres;Password=postgres"
  }
}

Packages to add:
- dotnet add package Microsoft.EntityFrameworkCore
- dotnet add package Microsoft.EntityFrameworkCore.Design
- dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
- dotnet add package Swashbuckle.AspNetCore

Migrations:
- dotnet ef migrations add Init
- dotnet ef database update

Example request:
POST /api/aaassddffee
Content-Type: application/json
{
  "email": "donor@example.com",
  "firstName": "Ada",
  "lastName": "Lovelace",
  "amountMinor": 2500,
  "currency": "GBP"
}

Expected responses:
- 201 Created + body of created donor
- 400 Bad Request (validation)
- 409 Conflict (email duplicate)
- 500 Problem (unexpected)

Please generate all necessary files and code now, placing code in the indicated paths and wiring everything in Program.cs.
*/
