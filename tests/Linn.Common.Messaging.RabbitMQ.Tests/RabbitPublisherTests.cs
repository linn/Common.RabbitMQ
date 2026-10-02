using System.Text;
using FluentAssertions;
using Linn.Common.Messaging.RabbitMQ;
using NSubstitute;
using NUnit.Framework;
using RabbitMQ.Client;

namespace Linn.Common.Messaging.RabbitMQ.Tests;

public class RabbitPublisherTests
{
    private IChannel channel = null!;
    private BasicProperties? captured;

    [SetUp]
    public void SetUp()
    {
        this.channel = Substitute.For<IChannel>();
        this.captured = null;
        this.channel.BasicPublishAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Do<BasicProperties>(p => this.captured = p),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
    }

    private Task Publish(Message m) => new RabbitPublisher(this.channel, "ex").PublishAsync(m);

    private static Message Msg(string? contentType, int? mode) => new()
    {
        RoutingKey = "rk",
        Body = Encoding.UTF8.GetBytes("{}"),
        ContentType = contentType,
        DeliveryMode = mode
    };

    [Test]
    public async Task ShouldSetContentTypeAndPersistent()
    {
        await this.Publish(Msg("application/json", 2));
        this.captured.Should().NotBeNull();
        this.captured!.ContentType.Should().Be("application/json");
        this.captured.DeliveryMode.Should().Be(DeliveryModes.Persistent);
    }

    [Test]
    public async Task ShouldSetTransient()
    {
        await this.Publish(Msg(null, 1));
        this.captured!.DeliveryMode.Should().Be(DeliveryModes.Transient);
    }

    [Test]
    public async Task ShouldLeaveUnsetWhenNull()
    {
        await this.Publish(Msg(null, null));
        this.captured!.ContentType.Should().BeNull();
        this.captured.IsDeliveryModePresent().Should().BeFalse();
        this.captured.IsContentTypePresent().Should().BeFalse();
    }

    [Test]
    public async Task ShouldKeepHeaders()
    {
        var m = new Message
        {
            RoutingKey = "rk",
            Headers = new Dictionary<string, object> { ["a"] = "b" }
        };
        await this.Publish(m);
        this.captured!.Headers.Should().ContainKey("a");
    }

    [Test]
    public async Task JsonPublisherPassesContentTypeAndPersistence()
    {
        var p = new JsonMessagePublisher<string>(
            new RabbitPublisher(this.channel, "ex"), "rk", contentType: "application/json", persistent: true);
        await p.PublishAsync("x");
        this.captured!.ContentType.Should().Be("application/json");
        this.captured.DeliveryMode.Should().Be(DeliveryModes.Persistent);
    }
}
