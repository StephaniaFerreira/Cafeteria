using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

const string QueueName = "pedidos-cafeteria";

var settings = AppSettings.Load();
var factory = new ConnectionFactory { HostName = settings.RabbitMq.HostName };

Console.WriteLine("╔════════════════════════════════════════╗");
Console.WriteLine("║     CAFETERIA - CLIENTE (Producer)     ║");
Console.WriteLine("╚════════════════════════════════════════╝");
Console.WriteLine();

try
{
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();
    channel.QueueDeclare(
        queue: QueueName,
        durable: false,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    var itens = new Dictionary<int, MenuItem>
    {
        [1] = new("Café", 5.00m),
        [2] = new("Café com leite", 6.50m),
        [3] = new("Sanduíche natural", 12.00m),
        [4] = new("Suco de laranja", 8.00m),
        [5] = new("Bolo", 7.00m)
    };

    while (true)
    {
        Console.WriteLine("  Cardápio:");
        foreach (var kvp in itens)
            Console.WriteLine($"    {kvp.Key}. {kvp.Value.Nome} - R$ {kvp.Value.Preco:F2}");
        Console.WriteLine("    0. Sair");
        Console.Write("\n  Escolha o número do item: ");
        var entrada = Console.ReadLine()?.Trim() ?? "";

        if (entrada == "0")
        {
            Console.WriteLine("Até mais!");
            break;
        }

        if (!int.TryParse(entrada, out var codigo) || !itens.TryGetValue(codigo, out var item))
        {
            Console.WriteLine("  Opção inválida.\n");
            continue;
        }

        Console.Write("  Quantidade: ");
        if (!int.TryParse(Console.ReadLine(), out var quantidade) || quantidade < 1)
        {
            Console.WriteLine("  Quantidade inválida.\n");
            continue;
        }

        var pedido = new
        {
            Item = item.Nome,
            Quantidade = quantidade,
            PrecoUnitario = item.Preco,
            DataHora = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        var json = JsonSerializer.Serialize(pedido);
        var body = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(
            exchange: "",
            routingKey: QueueName,
            basicProperties: null,
            body: body);

        Console.WriteLine($"  ✓ Pedido enviado: {item.Nome} x {quantidade} (R$ {(item.Preco * quantidade):F2})\n");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Erro: {ex.Message}");
    Console.WriteLine("Verifique se o RabbitMQ está rodando (ex.: docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:3-management).");
    return 1;
}

return 0;

record MenuItem(string Nome, decimal Preco);
