using System.Text.Json;
using DFramePlayground.Shared;
using StackExchange.Redis;

namespace DFramePlayground.Orleans.Grains.Grains;

public interface IReportingObserver : IGrainObserver
{
    void ReportProgress(QueueEvent @event);
}

public class ReportingObserver : IReportingObserver
{
    public void ReportProgress(QueueEvent @event)
    {
        throw new NotImplementedException();
    }
}

public interface IBackchannelReportingGrain : IGrainWithStringKey
{
    Task Subscribe(IReportingObserver observer);
    Task Unsubscribe(IReportingObserver observer);
}

public class BackchannelReportingGrain(IConnectionMultiplexer connectionMultiplexer) : Grain, IBackchannelReportingGrain
{
    private readonly List<IReportingObserver> _observers = [];

    public async Task Subscribe(IReportingObserver observer)
    {
        var sub = connectionMultiplexer.GetSubscriber();
        await sub.SubscribeAsync(this.GetPrimaryKeyString(), (_, value) =>  _observers.ForEach(toReport => toReport.ReportProgress(JsonSerializer.Deserialize<QueueEvent>(value!)!)));
        _observers.Add(observer);
    }

    public Task Unsubscribe(IReportingObserver observer)
    {
        _observers.Remove(observer);
        return Task.CompletedTask;
    }
}