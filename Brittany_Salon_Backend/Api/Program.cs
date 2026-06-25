using Brittany_Salon_Backend.Application.Services;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Application.Settings;
using Brittany_Salon_Backend.Application.Tools;
using Brittany_Salon_Backend.Application.Tools.Interfaces;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Brittany_Salon_Backend.Infrastructure.Services;
using Brittany_Salon_Backend.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Resend;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); // Habilita Controllers

builder.Services.AddEndpointsApiExplorer(); // Permite descubrir endpoints para Swagger
builder.Services.AddSwaggerGen(); // Genera Swagger UI

//CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuración JWT
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.Configure<RecoveryCodeSettings>(builder.Configuration.GetSection(RecoveryCodeSettings.SectionName));

// Autenticación con JWT
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
jwtSettings.Validate();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IDevLogger, DevLogger>();
}
else
{
    builder.Services.AddScoped<IDevLogger, NullDevLogger>();
}

builder.Services.AddScoped<IServiceService, ServiceService>(); // Inyecci�n de dependencias
builder.Services.AddScoped<IEmployeeService, EmployeeService>(); // Inyecci�n de dependencias Employee
builder.Services.AddScoped<IImageService, ImageService>(); // Inyecci�n de dependencias Image
builder.Services.AddScoped<IAppointmentService, AppointmentService>(); //Inyecci�n de dependencias Appointment
builder.Services.AddScoped<IClientService, ClientService>(); // Inyecci�n de dependencias Client
builder.Services.AddScoped<ITokenService, TokenService>(); // Inyecci�n de dependencias Token (JWT)
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddResend(o =>
{
    o.ApiToken = builder.Configuration["ResendSettings:ApiKey"]
        ?? throw new InvalidOperationException("Resend API key not configured. Set it via User Secrets or environment variable: ResendSettings__ApiKey");
});
builder.Services.Configure<ResendSettings>(builder.Configuration.GetSection(ResendSettings.SectionName));
builder.Services.AddScoped<IRecoveryCodeGenerator, RecoveryCodeGenerator>();
builder.Services.AddSingleton<IRecoveryCodeStore, InMemoryRecoveryCodeStore>();
builder.Services.AddSingleton<IRecoveryCodeRateLimiter, RecoveryCodeRateLimiter>();
builder.Services.AddScoped<IRecoveryCodeValidator, RecoveryCodeValidator>();
builder.Services.AddScoped<IRecoveryCodeSender, RecoveryCodeSender>();
builder.Services.AddScoped<IRecoveryFlow, RecoveryFlow>();
builder.Services.AddScoped<IPaymentService, PaymentService>(); // Inyecci�n de dependencias Payment
builder.Services.AddScoped<IProductService, ProductService>(); // Inyecci�n de dependencias Product
builder.Services.AddScoped<ICategoryService, CategoryService>(); // Inyecci�n de dependencias Category
builder.Services.AddScoped<IReviewService, ReviewService>(); // Inyecci�n de dependencias Review
builder.Services.AddScoped<IInventoryService, InventoryService>(); // Inyecci�n de dependencias Inventory
builder.Services.AddScoped<IReportService, ReportService>(); // Inyecci�n de dependencias Report

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Habilita Swagger
    app.UseSwaggerUI(); // Habilita Swagger UI
}

app.UseHttpsRedirection(); // Redirige HTTP -> HTTPS

// Habilita CORS
app.UseCors("AllowFrontend");

// Autenticación JWT
app.UseAuthentication();
app.UseAuthorization();

// Habilita acceso a archivos est�ticos desde /imageUser
var publicPath = Path.Combine(builder.Environment.ContentRootPath, "public");
if (!Directory.Exists(publicPath))
{
    Directory.CreateDirectory(publicPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(publicPath),
    RequestPath = ""
});

app.MapControllers(); // Mapea rutas de Controllers

app.Run(); // Inicia la API
