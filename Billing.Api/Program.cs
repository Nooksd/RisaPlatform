using System.Text;
using Billing.Api.BackgroundJobs;
using Billing.Api.Configuration;
using Billing.Api.Filters;
using Billing.Api.Services;
using Billing.Api.Validators;
using Billing.Data;
using Billing.Domain.Interfaces.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.BuildingBlocks.Messaging.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

// Mapster configuration
MapsterConfiguration.Configure();

// Database
builder.Services.AddBillingData(builder.Configuration);

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreatePaymentValidator>();
builder.Services.AddFluentValidationAutoValidation();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

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
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var rabbitMqConfig = builder.Configuration.GetSection("RabbitMq");
var rabbitMqOptions = new RabbitMqOptions
{
    Host = rabbitMqConfig["Host"]!,
    Port = int.Parse(rabbitMqConfig["Port"]!),
    Username = rabbitMqConfig["Username"]!,
    Password = rabbitMqConfig["Password"]!,
    Exchange = rabbitMqConfig["Exchange"]!,
    DeadLetterExchange = rabbitMqConfig["DeadLetterExchange"]!
};
builder.Services.AddRabbitMq(rabbitMqOptions);
builder.Services.AddSingleton<IDeletionEventPublisher, DeletionEventPublisher>();

// Stripe Payment Gateway
var stripeSettings = new StripeSettings
{
    SecretKey = builder.Configuration["Stripe:SecretKey"]!,
    PublishableKey = builder.Configuration["Stripe:PublishableKey"]!,
    WebhookSecret = builder.Configuration["Stripe:WebhookSecret"]!,
    SuccessUrl = builder.Configuration["Stripe:SuccessUrl"]!,
    CancelUrl = builder.Configuration["Stripe:CancelUrl"]!
};
builder.Services.AddSingleton(stripeSettings);
builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>();

// Services
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IEmailService, MockEmailService>();
builder.Services.AddScoped<IBillingService, BillingService>();

// Background Jobs
builder.Services.AddHostedService<SubscriptionCheckJob>();

// Controllers
builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Billing API",
        Version = "v1",
        Description = "API de Billing do RisaPlatform - Powered by Stripe"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var connectionString = builder.Configuration.GetConnectionString("BillingDb")!;

    await DatabaseInitializer.InitializeAsync(context, connectionString, logger);
    await DataSeeder.SeedAsync(context, logger);
}

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
