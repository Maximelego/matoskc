using MatosKC.Api.Endpoints;
using MatosKC.Api.ErrorHandling;
using MatosKC.Application.EquipmentCategories.Create;
using MatosKC.Application.EquipmentCategories.Get;
using MatosKC.Application.EquipmentCategories.Ports;
using MatosKC.Application.Equipments.Create;
using MatosKC.Application.Equipments.Ports;
using MatosKC.Infrastructure.Persistence;
using MatosKC.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
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

builder.Services.AddDbContext<MatosKCDbContext>(
    options => options.UseNpgsql(connectionString)
);

builder.Services.AddScoped<
    IEquipmentCategoryRepository,
    EquipmentCategoryRepository
>();

builder.Services.AddScoped<
    IEquipmentRepository,
    EquipmentRepository
>();

builder.Services.AddScoped<
    CreateEquipmentCategoryUseCase
>();

builder.Services.AddScoped<
    GetEquipmentCategoryUseCase
>();

builder.Services.AddScoped<CreateEquipmentUseCase>();

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
