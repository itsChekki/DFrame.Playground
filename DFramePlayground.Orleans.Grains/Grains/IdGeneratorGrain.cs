namespace DFramePlayground.Orleans.Grains.Grains;

public interface IIdGeneratorGrain : IGrainWithIntegerKey
{
    Task<string> GetId();
}

public class IdGeneratorGrain : Grain, IIdGeneratorGrain
{
    private int _currentId = 0;
    public Task<string> GetId()
    {
        var currentIdWithZeros = _currentId.ToString("00000");
        _currentId++;
        return Task.FromResult($"{this.GetPrimaryKeyLong()}{currentIdWithZeros}");
    }
}