namespace Linn.Common.Messaging.RabbitMQ;

public record ExchangeBinding(string ExchangeName, string[] RoutingKeys);
