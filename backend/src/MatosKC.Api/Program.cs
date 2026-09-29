using MatosKC.Api.Endpoints;
using MatosKC.Api.ErrorHandling;
using MatosKC.Application;
using MatosKC.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();

string connectionString =
    builder.Configuration.GetConnectionString(
        "MatosKCDatabase"
    )
    ?? throw new InvalidOperationException(
        "Connection string 'MatosKCDatabase' was not found."
    );

// Add services to the container.
builder.Services.AddApplication()
                .AddInfrastructure(connectionString);

builder.Services.Configure<RouteHandlerOptions>(
    options => options.ThrowOnBadRequest = true
);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs");
}

app.MapEquipmentCategoryEndpoints();
app.MapEquipmentEndpoints();

app.Run();
