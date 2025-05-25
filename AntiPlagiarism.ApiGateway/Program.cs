using System.Net;
using AntiPlagiarism.ApiGateway.Application.Interfaces;
using AntiPlagiarism.ApiGateway.Application.Services;
using Polly;
using Polly.Extensions.Http;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Добавление контроллеров
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS политика
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Настройка HTTP-клиентов для микросервисов
builder.Services.AddHttpClient("FileStoringService", client =>
{
    string url = builder.Configuration["Services:FileStoringService:Url"] 
                 ?? "http://antiplagiarism-file-storing-service";
    client.BaseAddress = new Uri(url);
}).AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHttpClient("FileAnalysisService", client =>
{
    string url = builder.Configuration["Services:FileAnalysisService:Url"] 
                 ?? "http://antiplagiarism-file-analysis-service";
    client.BaseAddress = new Uri(url);
}).AddPolicyHandler(GetRetryPolicy());

// Регистрация сервисов
builder.Services.AddScoped<IProxyService, ProxyService>();

WebApplication app = builder.Build();

// Настройка middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();

// Политика повторных попыток для HTTP-запросов
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}