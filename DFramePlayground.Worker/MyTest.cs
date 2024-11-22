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
    private readonly ILogger<MyTest2> _logger;
    private readonly IClusterClient _grainFactory;
    private SemaphoreSlim _semaphore;
    private QueueEvent _message;
    private bool _received;
    private string Id;

    public MyTest2(ILogger<MyTest2> logger, IClusterClient grainFactory)
    {
        _logger = logger;
        _grainFactory = grainFactory;
        _semaphore = new SemaphoreSlim(0, 1);
    }

    public override async Task SetupAsync(WorkloadContext context)
    {
        Id = $"{context.WorkloadIndex}{context.ExecuteCount}";
        _logger.LogInformation($"Calculated is {Id}");
        var grain = _grainFactory.GetGrain<IBackchannelReportingGrain>(Id);
        var reference = _grainFactory.CreateObjectReference<IReportingObserver>(this);
        await grain.Subscribe(reference);
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        _logger.LogInformation($"MyTest2 was called with id {Id}");
        
        _message = new QueueEvent()
        {
            Id = Id,
            Text = "Hello World!",
            Language = "English",
        };

        var publishPost = await "http://localhost:5222/"
            .AppendPathSegment("publish")
            .AppendPathSegment("PublishToRedis")
            .AppendPathSegment(Id)
            .PostJsonAsync(_message);

        publishPost.StatusCode.Should().Be(200);
        await _semaphore.WaitAsync(TimeSpan.FromSeconds(4));

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