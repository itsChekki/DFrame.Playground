using DFrame;
using DFramePlayground.Orleans.Grains.Grains;
using DFramePlayground.Shared;
using FluentAssertions;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Orleans;

namespace DFramePlayground.Worker;

public class MyTest : Workload
{
    private readonly ILogger<MyTest> _logger;
    private readonly Random _random;

    public MyTest(ILogger<MyTest> logger)
    {
        _logger = logger;
        _random = new Random();
    }

    public override Task ExecuteAsync(WorkloadContext context)
    {
        _logger.LogInformation("MyTest was called");
        if (_random.Next(100) % 2 == 0)
            throw new TimeoutException("Took to long to get a response");

        return Task.CompletedTask;
    }
}

public class MyTest2 : Workload, IReportingObserver
{
    private readonly IClusterClient _clusterClient;
    private SemaphoreSlim _semaphore;
    private QueueEvent _message;
    private bool _received;
    private readonly string _id;

    public MyTest2(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
        _semaphore = new SemaphoreSlim(0, 1);
        _id = Guid.NewGuid().ToString();
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
        _message = new QueueEvent()
        {
            Id = _id,
            Text = "Hello World!",
            Language = "English",
        };

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