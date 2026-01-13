using Gateway.Api.Handlers;
using Gateway.Api.Middlewares;
using Gateway.Api.Services;
using Gateway.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.BuildingBlocks.Messaging.RabbitMq;
using Shared.Contracts.Billing;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:SecretKey"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["access_token"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var redisConnection = ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddSingleton<ISubscriptionCache, SubscriptionCache>();

var rabbitMqOptions = new RabbitMqOptions
{
    Host = builder.Configuration["RabbitMq:Host"]!,
    Port = int.Parse(builder.Configuration["RabbitMq:Port"]!),
    Username = builder.Configuration["RabbitMq:Username"]!,
    Password = builder.Configuration["RabbitMq:Password"]!,
    Exchange = "billing.events",
    Queue = "gateway.subscriptions",
    DeadLetterExchange = "billing.events.dlx"
};

builder.Services.AddRabbitMq(rabbitMqOptions);
builder.Services.AddRabbitMqConsumer<TenantPaymentConfirmedEvent, TenantPaymentConfirmedHandler>();
builder.Services.AddRabbitMqConsumer<TenantGracePeriodGrantedEvent, TenantGracePeriodGrantedHandler>();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
builder.Services.AddSingleton<IDDoSProtectionService, DDoSProtectionService>();

var app = builder.Build();

//app.UseMiddleware<DDoSProtectionMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<TokenValidationMiddleware>();
app.UseMiddleware<TenantValidationMiddleware>();
app.UseMiddleware<BillingAccessMiddleware>();
app.UseMiddleware<SubscriptionValidationMiddleware>();
app.UseMiddleware<AccessLevelValidationMiddleware>();

app.MapReverseProxy();

app.Run();