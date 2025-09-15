using Correlate;
using Correlate.AspNetCore;
using Correlate.DependencyInjection;
using JsonPlaceholder.Sdk;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCorrelate();

builder.Host.UseSerilog((ctx, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(ctx.Configuration);
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddJsonPlaceholderApiService(builder.Configuration);

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseCorrelate();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/posts", async (IJsonPlaceholderApiService jsonPlaceholderService, [FromServices] IHttpContextAccessor httpContextAccessor, [FromServices] ICorrelationContextAccessor correlationContextAccessor) =>
{
    return await jsonPlaceholderService.GetPosts();
})
.WithName("GetPosts")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
