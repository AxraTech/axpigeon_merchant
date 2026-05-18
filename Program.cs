using AxpigeonApp.Configuration;
using AxpigeonApp.Extensions;
using OfficeOpenXml;

EnvConfiguration.LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

var envOverrides = EnvConfiguration.BuildConfigurationOverrides();
if (envOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(envOverrides);
}

var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            continue;
        var idx = trimmed.IndexOf('=');
        if (idx <= 0) continue;
        var key = trimmed[..idx].Trim();
        var value = trimmed[(idx + 1)..].Trim();
        Environment.SetEnvironmentVariable(key, value);
    }

    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "";
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "";
    var dbUser = Environment.GetEnvironmentVariable("DB_USERNAME") ?? "";
    var dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
    var connStr = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};SslMode=Disable;";

    builder.Configuration["ConnectionStrings:DefaultConnection"] = connStr;
    builder.Configuration["Jwt:Key"] = Environment.GetEnvironmentVariable("JWT_KEY") ?? "";
    builder.Configuration["Jwt:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "";
    builder.Configuration["Jwt:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "";
}

ExcelPackage.License.SetNonCommercialPersonal("AxpigeonApp");

builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAppRepositories();
builder.Services.AddAppServices();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddAuthentication("AxPCookieAuth")
    .AddCookie("AxPCookieAuth", options =>
    {
        options.Cookie.Name = "AxPCookieAuth";
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.ClaimsIssuer = "AxPIssuer";
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllers();

app.Run();
