using DFrame;
using DFramePlayground.Orleans.Grains.Grains;
using DFramePlayground.Shared;
using FluentAssertions;
using Flurl;
using Flurl.Http;
using Orleans;

namespace DFramePlayground.Worker;

public class TriggerPublishViaApiRandomId : Workload, IReportingObserver
{
    private readonly IClusterClient _clusterClient;
    private readonly SemaphoreSlim _semaphore;
    private QueueEvent? _message;
    private bool _received;

    public TriggerPublishViaApiRandomId(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
        _semaphore = new SemaphoreSlim(0, 1);
    }

    public override async Task SetupAsync(WorkloadContext context)
    {
        _message = new QueueEvent()
        {
            Id = context.WorkloadId.ToString(),
            Text = "Hello World!",
            Language = "English",
        };
        var grain = _clusterClient.GetGrain<IBackchannelReportingGrain>(context.WorkloadId.ToString());
        var reference = _clusterClient.CreateObjectReference<IReportingObserver>(this);
        await grain.Subscribe(reference);
    }

    public override async Task TeardownAsync(WorkloadContext context)
    {
        var grain = _clusterClient.GetGrain<IBackchannelReportingGrain>(context.WorkloadId.ToString());
        var reference = _clusterClient.CreateObjectReference<IReportingObserver>(this);
        await grain.Unsubscribe(reference);
    }

    public override Dictionary<string, string>? Complete(WorkloadContext context)
    {
        Console.WriteLine("Complete reached");
        return base.Complete(context);
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        var publishPost = await "http://localhost:5222/"
            .AppendPathSegment("publish")
            .AppendPathSegment("PublishToRedis")
            .AppendPathSegment(context.WorkloadId.ToString())
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