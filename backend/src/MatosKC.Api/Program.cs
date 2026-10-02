using System.Text.Json.Serialization;
using MatosKC.Api.Endpoints;
using MatosKC.Api.ErrorHandling;
using MatosKC.Application;
using MatosKC.Infrastructure;
using MatosKC.Infrastructure.Persistence;
using MatosKC.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);


builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter()
    );
});

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
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();

    MatosKCDbContext dbContext =
        scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();

    await dbContext.Database.MigrateAsync();

    DevelopmentDataSeeder dataSeeder =
        scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();

    await dataSeeder.SeedAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs");
}

app.MapEquipmentCategoryEndpoints();
app.MapEquipmentEndpoints();

app.Run();
