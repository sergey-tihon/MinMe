namespace MinMe.Analyzers.Model;

public class SlideInfo(int number, string fileName, string title)
{
    public int Number { get; } = number;
    public string FileName { get; } = fileName;
    public string Title { get; } = title;
}
