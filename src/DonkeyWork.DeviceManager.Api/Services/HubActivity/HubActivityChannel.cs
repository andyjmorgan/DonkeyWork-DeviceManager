namespace DonkeyWork.DeviceManager.Api.Services.HubActivity;

using System.Threading.Channels;

/// <summary>
/// Service that manages a channel for publishing and subscribing to hub activities.
/// </summary>
public interface IHubActivityChannel
{
    /// <summary>
    /// Publishes a hub activity to the channel.
    /// </summary>
    ValueTask PublishAsync(HubActivity activity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the reader for subscribing to hub activities.
    /// </summary>
    ChannelReader<HubActivity> Reader { get; }
}

/// <summary>
/// Implementation of the hub activity channel using System.Threading.Channels.
/// </summary>
public class HubActivityChannel : IHubActivityChannel
{
    private readonly Channel<HubActivity> _channel;

    public HubActivityChannel()
    {
        // Create an unbounded channel with single reader/multiple writers
        _channel = Channel.CreateUnbounded<HubActivity>(new UnboundedChannelOptions
        {
            SingleReader = true, // Only one background service will read
            AllowSynchronousContinuations = false // Prevent blocking publishers
        });
    }

    public ChannelReader<HubActivity> Reader => _channel.Reader;

    public async ValueTask PublishAsync(HubActivity activity, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(activity, cancellationToken);
    }
}
