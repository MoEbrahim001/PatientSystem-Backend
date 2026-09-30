using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PatientSystem.Application.Interfaces;
using PatientSystem.Application.Services;
using PatientSystem.Infrastructure.Data;
using PatientSystem.Infrastructure.Repositories;
using PatientSystem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);


// ============================
// Controllers + Swagger
// ============================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();


// ============================
// Database
// ============================

builder.Services.AddDbContext<PatientSystemDbContext>(
    options =>
    {
        var connectionString =
            builder.Configuration
                .GetConnectionString(
                    "DefaultConnection"
                );

        if (string.IsNullOrWhiteSpace(
            connectionString))
        {
            throw new InvalidOperationException(
                "DefaultConnection is missing."
            );
        }

        options.UseSqlServer(
            connectionString
        );
    }
);


// ============================
// Dependency Injection
// ============================

builder.Services.AddScoped<
    IPatientRepository,
    PatientRepository
>();

builder.Services.AddScoped<
    IPatientService,
    PatientService
>();


// ============================
// Python Face Recognition API
// ============================

builder.Services.AddHttpClient<
    IFaceEncodingService,
    FaceEncodingService
>(
    (serviceProvider, client) =>
    {
        var configuration =
            serviceProvider
                .GetRequiredService<
                    IConfiguration
                >();

        var baseUrl =
            configuration[
                "FaceRecognition:BaseUrl"
            ];

        if (string.IsNullOrWhiteSpace(
            baseUrl))
        {
            throw new InvalidOperationException(
                "FaceRecognition:BaseUrl is missing."
            );
        }

        client.BaseAddress =
            new Uri(baseUrl);

        client.Timeout =
            TimeSpan.FromSeconds(60);
    }
);


// ============================
// CORS
// ============================

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "AllowAll",
            policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }
        );
    }
);


var app = builder.Build();


// ============================
// Swagger
// ============================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// ============================
// Middleware
// ============================

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();


// ============================
// Images
// ============================

var imagesPath =
    Path.Combine(
        app.Environment.ContentRootPath,
        "images"
    );

Directory.CreateDirectory(
    imagesPath
);

app.UseStaticFiles(
    new StaticFileOptions
    {
        FileProvider =
            new PhysicalFileProvider(
                imagesPath
            ),

        RequestPath =
            "/images"
    }
);


// ============================
// Controllers
// ============================

app.MapControllers();

app.Run();