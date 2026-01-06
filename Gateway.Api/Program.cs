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

var jwtKey = builder.Configuration["Jwt:Key"]!;
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

// Redis
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

// HttpClient para proxy reverso
builder.Services.AddHttpClient("AuthService", client =>
{
    client.BaseAddress = new Uri("http://auth-api.auth-service.svc.cluster.local");
});

builder.Services.AddHttpClient("BillingService", client =>
{
    client.BaseAddress = new Uri("http://billing-api.billing-service.svc.cluster.local");
});

builder.Services.AddHttpClient("CrmService", client =>
{
    client.BaseAddress = new Uri("http://crm-api.crm-service.svc.cluster.local");
});

builder.Services.AddSingleton<IProxyService, ProxyService>();

var app = builder.Build();

// Health Checks
//app.MapHealthEndpoints();

app.UseMiddleware<DDoSProtectionMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Pipeline de Middlewares
app.UseMiddleware<TokenValidationMiddleware>();
app.UseMiddleware<TenantValidationMiddleware>();
app.UseMiddleware<BillingAccessMiddleware>();
app.UseMiddleware<SubscriptionValidationMiddleware>();
app.UseMiddleware<AccessLevelValidationMiddleware>();
app.UseMiddleware<ProxyMiddleware>();

app.Run();