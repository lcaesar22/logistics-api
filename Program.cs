using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using FluentValidation;
using Logistics.Api.Common.Auth;
using Logistics.Api.Common.Endpoints;
using Logistics.Api.Common.Errors;
using Logistics.Api.Common.OpenApi;
using Logistics.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, logger) =>
{
    logger.ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Logistics.Api");
    if (builder.Environment.IsDevelopment())
    {
        logger.WriteTo.Console();
    }

    logger.WriteTo.Console(new RenderedCompactJsonFormatter());
});
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecurityTransformer>());
builder.Services.AddAuth();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<LogisticsDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("LogisticsDb"))
    .UseSnakeCaseNamingConvention());
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Validation service
ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();  
}

app.UseHttpsRedirection();

app.MapGet("/", () => "Logistics API is running");
app.MapGroup("/api/v1").MapEndpoints();


app.Run();
