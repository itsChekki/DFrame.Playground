namespace DFramePlayground.Orleans.Grains.Grains;

public interface IIdGeneratorGrain : IGrainWithIntegerKey
{
    Task<string> GetId();
    Task FreeId(string id);
}

public class IdGeneratorGrain : Grain, IIdGeneratorGrain
{
    private int _currentId = 0;
    private List<string> _freeIds = new();
    public Task<string> GetId()
    {
        if (_freeIds.Any())
        {
            var id = _freeIds.First();
            _freeIds.Remove(id);
            return Task.FromResult(id);
        }
        var currentIdWithZeros = _currentId.ToString("00000");
        _currentId++;
        return Task.FromResult($"{this.GetPrimaryKeyLong()}{currentIdWithZeros}");
    }

    public Task FreeId(string id)
    {
        _freeIds.Add(id);
        return Task.CompletedTask;
    }
}