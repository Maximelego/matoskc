using MatosKC.Application.EquipmentCategories.Create;
using MatosKC.Application.EquipmentCategories.Get;
using MatosKC.Application.EquipmentCategories.Ports;
using MatosKC.Application.Equipments.Create;
using MatosKC.Application.Equipments.Ports;
using MatosKC.Infrastructure.Persistence;
using MatosKC.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString =
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

builder.Services.AddScoped<CreateEquipmentCategoryUseCase>();
builder.Services.AddScoped<GetEquipmentCategoryUseCase>();
builder.Services.AddScoped<CreateEquipmentUseCase>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
