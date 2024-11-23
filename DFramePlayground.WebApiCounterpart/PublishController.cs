using System.Text.Json;
using DFramePlayground.Shared;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace DFramePlayground.WebApiCounterpart;

[Route("[controller]")]
[ApiController]
public class PublishController : ControllerBase
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public PublishController(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    [HttpPost("[action]/{Id}")]
    public Task<ActionResult> PublishToRedis(string id, QueueEvent eventToPublish)
    {
        _ = Publish(id, eventToPublish);
        return Task.FromResult<ActionResult>(Ok());
    }

    private async Task Publish(string id, QueueEvent eventToPublish)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(1000)));
        var subscriber = _connectionMultiplexer.GetSubscriber();
        await subscriber.PublishAsync(id, JsonSerializer.Serialize(eventToPublish));
    }
}