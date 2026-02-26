using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

const string QueueName = "pedidos-cafeteria";

var settings = AppSettings.Load();
var factory = new ConnectionFactory { HostName = settings.RabbitMq.HostName };

Console.WriteLine("╔════════════════════════════════════════╗");
Console.WriteLine("║   CAFETERIA - FUNCIONÁRIO (Consumer)   ║");
Console.WriteLine("╚════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("Aguardando pedidos na fila... (Ctrl+C para encerrar)");
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
    //Controla como o consumidor recebe mensagem na fila (tamanho maximo da mensagem em bytes, quantas mensagens o consumidor pode receber sem confirmar (ack), regra vale apenas para este consumidor)
    channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

    var consumer = new EventingBasicConsumer(channel);
    consumer.Received += (_, ea) =>
    {
        var body = ea.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);

        try
        {
            var pedido = JsonSerializer.Deserialize<Pedido>(json);
            if (pedido is null)
            {
                Console.WriteLine("  [Erro] Mensagem inválida.");
                channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            var total = pedido.PrecoUnitario * pedido.Quantidade;
            Console.WriteLine($"  ─────────────────────────────────");
            Console.WriteLine($"  Pedido recebido: {pedido.Item} x {pedido.Quantidade}");
            Console.WriteLine($"  Total: R$ {total:F2} | {pedido.DataHora}");
            Console.WriteLine($"  Preparando...");
            Thread.Sleep(1500);
            Console.WriteLine($"  ✓ Pedido pronto!\n");
        }
        catch
        {
            Console.WriteLine($"  [Erro] Não foi possível processar: {json}");
        }

        channel.BasicAck(ea.DeliveryTag, false);
    };

    channel.BasicConsume(
        queue: QueueName,
        autoAck: false,
        consumer: consumer);

    Thread.Sleep(Timeout.Infinite);
}
catch (Exception ex)
{
    Console.WriteLine($"Erro: {ex.Message}");
    Console.WriteLine("Verifique se o RabbitMQ está rodando (ex.: docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:3-management).");
    return 1;
}

return 0;

record Pedido(string Item, int Quantidade, decimal PrecoUnitario, string DataHora);
