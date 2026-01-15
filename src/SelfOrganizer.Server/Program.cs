using SelfOrganizer.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Register LLM proxy service
builder.Services.AddScoped<ILlmProxyService, LlmProxyService>();

// Register Google Calendar proxy service
builder.Services.AddScoped<IGoogleCalendarProxyService, GoogleCalendarProxyService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
