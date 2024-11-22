namespace DFramePlayground.Shared;

[GenerateSerializer]
public class QueueEvent
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public string Text { get; set; }
    [Id(2)]
    public string Language { get; set; }
}