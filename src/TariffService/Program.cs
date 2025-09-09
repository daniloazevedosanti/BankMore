using System.Data;
using System.Data.SQLite;
using Dapper;
using TariffService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dbPath = builder.Configuration.GetValue<string>("Database:Path") ?? "data/tariff.sqlite";
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
var connStr = new SQLiteConnectionStringBuilder { DataSource = dbPath }.ToString();
builder.Services.AddScoped<IDbConnection>(_ => new SQLiteConnection(connStr));

builder.Services.AddHostedService<TariffService.Services.TransferConsumerService>();
builder.Services.AddLogging();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    var sql = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tarifas.sql"));
    db.Execute(sql);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
