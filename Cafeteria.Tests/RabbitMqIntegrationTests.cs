using System.Text;
using RabbitMQ.Client;

namespace Cafeteria.Tests;

public class RabbitMqIntegrationTests
{
    [Fact]
    public void LoadSettings_And_PingRabbitMq()
    {
        // arrange
        var settings = AppSettings.Load();
        var factory = new ConnectionFactory { HostName = settings.RabbitMq.HostName };

        // act
        using var connection = factory.CreateConnection();

        // assert
        Assert.NotNull(connection);
        Assert.True(connection.IsOpen);
    }

    [Fact]
    public void Publish_And_Consume_Message_In_Queue()
    {
        // arrange
        var settings = AppSettings.Load();
        var factory = new ConnectionFactory { HostName = settings.RabbitMq.HostName };
        const string queueName = "cafeteria-integration-tests";
        const string payload = "mensagem de teste de integração";

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: true,     // limpa fila de teste automaticamente
            arguments: null);

        // act: publish
        var body = Encoding.UTF8.GetBytes(payload);
        channel.BasicPublish(
            exchange: "",
            routingKey: queueName,
            basicProperties: null,
            body: body);

        // act: consume (pega uma mensagem da fila)
        var result = channel.BasicGet(queueName, autoAck: true);

        // assert
        Assert.NotNull(result);
        var message = Encoding.UTF8.GetString(result.Body.ToArray());
        Assert.Equal(payload, message);
    }
}