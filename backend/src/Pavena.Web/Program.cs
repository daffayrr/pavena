using Pavena.Web.Services.Auth;
using Pavena.Web.Services.Profile;
using Pavena.Web.Services.Theme;
using Pavena.Web.Services.Toast;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// UI & Theme services
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IScholarSessionService, ScholarSessionService>();

// Backend API Client
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5181";
builder.Services.AddHttpClient<IAcademicProfileClient, AcademicProfileClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Pavena.Web.App>()
    .AddInteractiveServerRenderMode();

app.Run();
