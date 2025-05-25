using Microsoft.EntityFrameworkCore;
using AntiPlagiarism.FileStoringService.Infrastructure;
using AntiPlagiarism.FileStoringService.Domain.Interfaces;
using AntiPlagiarism.FileStoringService.Application.Interfaces;
using AntiPlagiarism.FileStoringService.Application.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Добавление контроллеров
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Настройка CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Настройка хранилища файлов
builder.Services.Configure<LocalFileStorageSettings>(
    builder.Configuration.GetSection("LocalFileStorage"));
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

// Настройка базы данных
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Строка подключения 'DefaultConnection' не найдена.");
builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => 
    {
        npgsqlOptions.SetPostgresVersion(new Version(16, 0)); // Соответствует версии postgres:16
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "storage"); // Схема для таблицы миграций
    })
    .EnableSensitiveDataLogging() // Для отладки SQL-запросов
);

// Регистрация сервисов
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IFileService, FileService>();


WebApplication app = builder.Build();

// Настройка middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Миграции БД при разработке
    using IServiceScope scope = app.Services.CreateScope();
    FileDbContext context = scope.ServiceProvider.GetRequiredService<FileDbContext>();
    context.Database.Migrate();
}

app.UseCors("AllowAll");
app.MapControllers();

app.Run();