using System.Text;
using System.Text.Json.Serialization;
using AdoptionAgency.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Services;
using MySqlConnector;
using System.Data;

var builder = WebApplication.CreateBuilder(args);
var mysqlConnection = ResolveMySqlConnectionString(builder.Configuration);

if (args.Contains("--clear"))
{
    builder.Services.AddDbContext<AdoptionDbContext>(options =>
        options.UseMySql(mysqlConnection, ServerVersion.AutoDetect(mysqlConnection)));
    var host = builder.Build();
    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AdoptionDbContext>();
    await DbSeeder.ClearAnimalsAndApplicantsAsync(db);
    Console.WriteLine("Cleared animals and applicants. Employee user(s) kept.");
    return;
}

builder.Services.AddDbContext<AdoptionDbContext>(options =>
    options.UseMySql(mysqlConnection, ServerVersion.AutoDetect(mysqlConnection)));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PublicIdGenerator>();
builder.Services.AddScoped<AnimalsService>();
builder.Services.AddScoped<IntakesService>();
builder.Services.AddScoped<PlacementsService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ApplicationsService>();
builder.Services.AddScoped<ApplicantSelfService>();
builder.Services.AddHttpClient("DeepSeek", client => client.Timeout = TimeSpan.FromSeconds(90));
builder.Services.AddScoped<DeepSeekNarrativeService>();
builder.Services.AddScoped<MatchingService>();
builder.Services.AddScoped<NameSuggestionService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<OutreachService>();
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key required")))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5500",
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:8080",
                "http://127.0.0.1:5500",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5173",
                "http://127.0.0.1:8080")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdoptionDbContext>();
    var runMigrations = app.Configuration.GetValue<bool>("RunMigrationsOnStartup");
    if (runMigrations && await ShouldRunMigrationsAsync(db))
        await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, app.Configuration);
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowFrontend");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.MapGet("/", () => Results.Ok(new { status = "ok", app = "Tuscaloosa Rescue League API" }));
app.MapControllers();

app.Run();

static string ResolveMySqlConnectionString(IConfiguration config)
{
    var jawsUrl = config["JAWSDB_URL"];
    if (!string.IsNullOrWhiteSpace(jawsUrl))
        return ConvertHerokuMySqlUrl(jawsUrl);

    var configured = config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("MySQL connection not configured (DefaultConnection or JAWSDB_URL).");

    if (configured.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase) ||
        configured.StartsWith("mysql2://", StringComparison.OrdinalIgnoreCase))
        return ConvertHerokuMySqlUrl(configured);

    return configured;
}

static string ConvertHerokuMySqlUrl(string url)
{
    var normalized = url.StartsWith("mysql2://", StringComparison.OrdinalIgnoreCase)
        ? "mysql://" + url["mysql2://".Length..]
        : url;
    var uri = new Uri(normalized);
    var userInfo = uri.UserInfo.Split(':', 2);
    var csb = new MySqlConnectionStringBuilder
    {
        Server = uri.Host,
        Port = (uint)uri.Port,
        UserID = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
        Database = uri.AbsolutePath.TrimStart('/'),
        SslMode = MySqlSslMode.Required
    };
    return csb.ConnectionString;
}

static async Task<bool> ShouldRunMigrationsAsync(AdoptionDbContext db)
{
    // If DB is empty/new, run migrations.
    // If tables already exist but migration history is missing, skip startup migration
    // to avoid "table already exists" crash on legacy/provisioned databases.
    var conn = db.Database.GetDbConnection();
    if (conn.State != ConnectionState.Open)
        await conn.OpenAsync();

    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
SELECT
  (SELECT COUNT(*) FROM information_schema.tables
   WHERE table_schema = DATABASE() AND table_name = '__EFMigrationsHistory') AS HistExists,
  (SELECT COUNT(*) FROM information_schema.tables
   WHERE table_schema = DATABASE() AND table_name = 'Animals') AS AnimalsExists;";
    await using var reader = await cmd.ExecuteReaderAsync();
    await reader.ReadAsync();
    var histExists = reader.GetInt32(0) > 0;
    var animalsExists = reader.GetInt32(1) > 0;
    await reader.CloseAsync();

    if (!histExists && animalsExists)
        return false;

    return true;
}
