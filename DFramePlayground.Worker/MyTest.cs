using DFrame;
using DFramePlayground.Orleans.Grains.Grains;
using DFramePlayground.Shared;
using FluentAssertions;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Orleans;

namespace DFramePlayground.Worker;

public class TriggerPublishViaApi : Workload, IReportingObserver
{
    private readonly IClusterClient _clusterClient;
    private readonly SemaphoreSlim _semaphore;
    private readonly QueueEvent _message;
    private bool _received;
    private readonly string _id;

    public TriggerPublishViaApi(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
        _id = Guid.NewGuid().ToString();
        _message = new QueueEvent()
        {
            Id = _id,
            Text = "Hello World!",
            Language = "English",
        };
        _semaphore = new SemaphoreSlim(0, 1);
    }

    public override async Task SetupAsync(WorkloadContext context)
    {
        var grain = _clusterClient.GetGrain<IBackchannelReportingGrain>(_id);
        var reference = _clusterClient.CreateObjectReference<IReportingObserver>(this);
        await grain.Subscribe(reference);
    }

    public override async Task TeardownAsync(WorkloadContext context)
    {
        var grain = _clusterClient.GetGrain<IBackchannelReportingGrain>(_id);
        var reference = _clusterClient.CreateObjectReference<IReportingObserver>(this);
        await grain.Unsubscribe(reference);
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        var publishPost = await "http://localhost:5222/"
            .AppendPathSegment("publish")
            .AppendPathSegment("PublishToRedis")
            .AppendPathSegment(_id)
            .PostJsonAsync(_message);

        publishPost.StatusCode.Should().Be(200);
        await _semaphore.WaitAsync(TimeSpan.FromSeconds(10));
        
        _received.Should().NotBe(false, "The message should have been received but a timeout happened");
    }

    public void ReportProgress(QueueEvent @event)
    {
        @event.Should().BeEquivalentTo(_message);
        _received = true;
        _semaphore.Release();
    }
}

public class MyTestSettings
{
    public int Delay { get; set; }
}