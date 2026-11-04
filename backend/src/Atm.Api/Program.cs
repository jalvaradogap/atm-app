using System.Text.Json.Serialization;
using Atm.Api.Errors;
using Atm.Application.Accounts;
using Atm.Infrastructure;
using Atm.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Atm")
    ?? $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "atm.db")}";

builder.Services
    .AddControllers()
    // Enums go out as "Deposit" rather than 0, so the frontend types read clearly.
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<AccountService>();

var app = builder.Build();

// Creates the database file on first run and seeds the two accounts if they are missing.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AtmDbContext>();
    await DatabaseInitializer.InitializeAsync(db, CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseCors();
app.MapControllers();

app.Run();

// Lets the integration test project host this app with WebApplicationFactory.
public partial class Program { }
