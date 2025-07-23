using Asp.Versioning;
using Serilog;
using UASystem.Api.Application.Clocking;
using UASystem.Api.Application.ILoggingService;
using UASystem.Api.Extensions;
using UASystem.Api.Filters;
using UASystem.Api.Infrastructure.Data.DataWrapperFactory;
using UASystem.Api.Infrastructure.Data.IDataWrapperFactory;
using UASystem.Api.Infrastructure.LoggingService;
using UASystem.Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration) // Load from appsettings.json
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("UASystem", "UASystem.Api");
});

// Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionHandler>();
});
builder.Services.AddSingleton(typeof(IAppLogger<>), typeof(AppLogger<>));
builder.Services.AddSingleton<ISystemClocking, SystemClocking>();
builder.Services.AddSingleton<IDatabaseConnectionFactory, SqlDatabaseConnectionFactory>();
// Add Repositories and services 
builder.Services.AddRegistrationServices();


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseSerilogRequestLogging(); // Logs HTTP requests
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.Items["CorrelationId"]?.ToString());
    };
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
