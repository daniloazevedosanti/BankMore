using System.Data;
using System.Data.SQLite;
using Dapper;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Shared;
using TransferService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Services.AddMediatR(typeof(Program));
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.Lifetime = ServiceLifetime.Scoped; // tempo de vida
    // ...
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings("BankMore","BankMore.Users","CHANGE_ME",240);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });

var dbPath = builder.Configuration.GetValue<string>("Database:Path") ?? "data/transfer.sqlite";
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
var connStr = new SQLiteConnectionStringBuilder { DataSource = dbPath }.ToString();
builder.Services.AddScoped<IDbConnection>(_ => new SQLiteConnection(connStr));

builder.Services.AddHttpClient("AccountService", client => client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("AccountService:BaseUrl") ?? "http://account-service:8080/"));

builder.Services.AddLogging();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    var sql = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "transferencia.sql"));
    db.Execute(sql);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
