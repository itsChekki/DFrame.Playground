using DFrame;
using DFramePlayground.Orleans.Grains.Grains;
using DFramePlayground.Shared;
using FluentAssertions;
using Flurl;
using Flurl.Http;
using Orleans;

namespace DFramePlayground.Worker;

public class TriggerPublishViaApiWithFixedIds : Workload, IReportingObserver
{
    private readonly IClusterClient _clusterClient;
    private readonly SemaphoreSlim _semaphore;
    private readonly Dictionary<string, string> _report;
    private IReportingObserver? _observerReference;
    
    private QueueEvent? _message;
    private bool _received;
    private string? _id;

    public TriggerPublishViaApiWithFixedIds(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
        _semaphore = new SemaphoreSlim(0, 1);
        _observerReference = _clusterClient.CreateObjectReference<IReportingObserver>(this);
        _report = new Dictionary<string, string>();
    }

    public override async Task SetupAsync(WorkloadContext context)
    {
        var idGrain = _clusterClient.GetGrain<IIdGeneratorGrain>(context.WorkloadIndex % 10); 
        _id = await idGrain.GetId();
        
        var subGrain = _clusterClient.GetGrain<IBackchannelReportingGrain>(_id);
        await subGrain.Subscribe(_observerReference!);
        
        _message = new QueueEvent()
        {
            Id = _id,
            Text = "Hello World!",
            Language = "English",
        };
    }

    public override async Task TeardownAsync(WorkloadContext context)
    {
        var grain = _clusterClient.GetGrain<IBackchannelReportingGrain>(_id);
        await grain.Unsubscribe(_observerReference!);
        
        var idGrain = _clusterClient.GetGrain<IIdGeneratorGrain>(context.WorkloadIndex % 10);
        await idGrain.FreeId(_id!);
    }

    public override Dictionary<string, string>? Complete(WorkloadContext context)
    {
        Console.WriteLine("Complete reached");

        return _report;
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
        
        _report.Add("QueueEventId"+context.ExecuteCount, _id!);
        _received.Should().NotBe(false, "The message should have been received but a timeout happened");
    }

    public void ReportProgress(QueueEvent @event)
    {
        @event.Should().BeEquivalentTo(_message);
        _received = true;
        _semaphore.Release();
    }
}