using FluentAssertions;
using Linn.Common.Messaging.RabbitMQ;
using NUnit.Framework;
using RabbitMQ.Client;

namespace Linn.Common.Messaging.RabbitMQ.Tests;

public class RabbitChannelConfigurationTests
{
    [SetUp]
    public void SetUp()
    {
        Environment.SetEnvironmentVariable("RABBIT_SERVER", "localhost");
        Environment.SetEnvironmentVariable("RABBIT_USERNAME", "guest");
        Environment.SetEnvironmentVariable("RABBIT_PASSWORD", "guest");
        Environment.SetEnvironmentVariable("RABBIT_PORT", "5672");
    }

    [Test]
    public void ShouldRequestTheConfiguredHeartbeat()
    {
        var config = new RabbitChannelConfiguration("q", ["rk"], "ex")
                         {
                             RequestedHeartbeat = TimeSpan.FromSeconds(15)
                         };

        var factory = config.CreateConnectionFactory();

        factory.RequestedHeartbeat.Should().Be(TimeSpan.FromSeconds(15));
    }

    [Test]
    public void ShouldRejectAHeartbeatUnderASecond()
    {
        var act = () => new RabbitChannelConfiguration("q", ["rk"], "ex")
                            {
                                RequestedHeartbeat = TimeSpan.FromMilliseconds(500)
                            };

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ShouldCreateTheProducerChannelWithTrackedConfirmsWhenAsked()
    {
        var config = new RabbitChannelConfiguration("q", ["rk"], "ex")
                         {
                             PublisherConfirms = true
                         };

        var options = config.ProducerChannelOptions();

        options.Should().NotBeNull();
        options!.PublisherConfirmationsEnabled.Should().BeTrue();
        options.PublisherConfirmationTrackingEnabled.Should().BeTrue();
    }

    // Binary compatibility: assemblies compiled against 5.0-6.1 call this exact constructor.
    [Test]
    public void ShouldKeepItsOriginalConstructorSignature()
    {
        var ctor = typeof(RabbitChannelConfiguration).GetConstructor(
            new[]
                {
                    typeof(string), typeof(string[]), typeof(string), typeof(bool), typeof(string),
                    typeof(string), typeof(bool), typeof(bool), typeof(string), typeof(ExchangeBinding[])
                });
        ctor.Should().NotBeNull();
        typeof(RabbitChannelConfiguration).GetConstructors().Should().HaveCount(1);
    }
}
