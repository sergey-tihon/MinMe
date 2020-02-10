namespace MinMe.Analyzers.Model
{
    public class SlideInfo
    {
        public SlideInfo(int number, string fileName, string title)
        {
            Number = number;
            FileName = fileName;
            Title = title;
        }
        public int Number { get; }
        public string FileName { get; }
        public string Title { get; }
    }
}