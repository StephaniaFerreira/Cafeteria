using System.Text.Json;

public sealed record AppSettings(RabbitMqSettings RabbitMq)
{
    public static AppSettings Load()
    {
        const string fileName = "cafeteria.config.json";
        var path = Path.Combine(AppContext.BaseDirectory, fileName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Arquivo de configuração não encontrado: {path}");
        }

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var settings = JsonSerializer.Deserialize<AppSettings>(json, options);

        return settings ?? throw new InvalidOperationException("Configuração inválida em cafeteria.config.json.");
    }
}

public sealed record RabbitMqSettings(string HostName);

