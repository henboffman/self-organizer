using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;
using SelfOrganizer.Server.Data;
using SelfOrganizer.Server.Services;
using SelfOrganizer.Server.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

// Check if Azure AD is configured
var azureAdConfig = builder.Configuration.GetSection("AzureAd");
var clientId = azureAdConfig["ClientId"];
var hasAzureAdConfig = !string.IsNullOrEmpty(clientId);

if (hasAzureAdConfig)
{
    // Add Microsoft Identity Web authentication
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(azureAdConfig)
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddInMemoryTokenCaches();

    builder.Services.AddAuthorization();
}
else
{
    // For development/testing without Azure AD, allow all requests
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization(options =>
    {
        // Set fallback policy to allow anonymous when no auth scheme is configured
        options.FallbackPolicy = null;
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true) // Always passes
            .Build();
    });
}

// Add Entity Framework Core with SQL Server
builder.Services.AddDbContext<SelfOrganizerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpContextAccessor for auth service
builder.Services.AddHttpContextAccessor();

// Add auth service
builder.Services.AddScoped<IServerAuthService, ServerAuthService>();

// Add generic repository factory
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

// Add services
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Register LLM proxy service
builder.Services.AddScoped<ILlmProxyService, LlmProxyService>();

// Note: Google Calendar proxy service removed - using Outlook via Entra

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

// Authentication middleware - must come before authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
