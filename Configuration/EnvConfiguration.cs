namespace AxpigeonApp.Configuration;

public static class EnvConfiguration
{
    public static void LoadDotEnv()
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }
    }

    public static Dictionary<string, string?> BuildConfigurationOverrides()
    {
        var overrides = new Dictionary<string, string?>();

        var host = Environment.GetEnvironmentVariable("DB_HOST");
        var port = Environment.GetEnvironmentVariable("DB_PORT");
        var database = Environment.GetEnvironmentVariable("DB_NAME");
        var username = Environment.GetEnvironmentVariable("DB_USERNAME");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

        if (!string.IsNullOrWhiteSpace(host)
            && !string.IsNullOrWhiteSpace(port)
            && !string.IsNullOrWhiteSpace(database)
            && !string.IsNullOrWhiteSpace(username)
            && password is not null)
        {
            overrides["ConnectionStrings:DefaultConnection"] =
                $"Host={host};Port={port};Database={database};Username={username};Password={password};SslMode=Disable;";
        }

        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
        if (!string.IsNullOrWhiteSpace(jwtKey))
            overrides["Jwt:Key"] = jwtKey;

        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
        if (!string.IsNullOrWhiteSpace(jwtIssuer))
            overrides["Jwt:Issuer"] = jwtIssuer;

        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        if (!string.IsNullOrWhiteSpace(jwtAudience))
            overrides["Jwt:Audience"] = jwtAudience;

        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(apiBaseUrl))
            overrides["AxpigeonApi:BaseUrl"] = apiBaseUrl.TrimEnd('/');

        return overrides;
    }
}
