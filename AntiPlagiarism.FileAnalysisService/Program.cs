using System.Net;
using AntiPlagiarism.FileAnalysisService.Application.Interfaces;
using AntiPlagiarism.FileAnalysisService.Application.Services;
using AntiPlagiarism.FileAnalysisService.Domain.Interfaces;
using AntiPlagiarism.FileAnalysisService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Добавляем контроллеры
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Настройка базы данных
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Строка подключения 'DefaultConnection' не найдена.");
builder.Services.AddDbContext<AnalysisDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => 
    {
        npgsqlOptions.SetPostgresVersion(new Version(16, 0)); // Соответствует версии postgres:16
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "analysis"); // Схема для таблицы миграций
    })
    .EnableSensitiveDataLogging() // Для отладки SQL-запросов
);

// Настройка хранилища облаков слов
builder.Services.Configure<WordCloudStorageSettings>(
    builder.Configuration.GetSection("WordCloudStorage"));
builder.Services.AddSingleton<IWordCloudStorage, WordCloudStorage>();

// Регистрация репозиториев
builder.Services.AddScoped<IAnalysisResultRepository, FileAnalysisResultRepository>();
builder.Services.AddScoped<IPlagiarismCheckRepository, PlagiarismCheckRepository>();

// Настройка HTTP-клиента с политикой повторных попыток
builder.Services.AddHttpClient<IFileAnalysisService, FileAnalysisService>()
    .AddPolicyHandler(GetRetryPolicy())
    .ConfigureHttpClient(client =>
    {
        string fileStoringServiceUrl = builder.Configuration["Services:FileStoringService:Url"] 
            ?? "http://file-storing-service";
        client.BaseAddress = new Uri(fileStoringServiceUrl);
    });

// Регистрация основного сервиса
builder.Services.AddScoped<IFileAnalysisService, FileAnalysisService>();

WebApplication app = builder.Build();

// Настройка middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Миграции БД при разработке
    using IServiceScope scope = app.Services.CreateScope();
    AnalysisDbContext context = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
    context.Database.Migrate();
}

app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
return;

// Политика повторных попыток для HTTP-запросов между сервисами
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}