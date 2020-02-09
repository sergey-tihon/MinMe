using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Clippit;
using Clippit.PowerPoint;

namespace MinMe.Blazor.Services
{
    public class DocumentService
    {
        public DocumentService()
        {
        }

        public int PublishSlides(string fileName)
        {
            var presentation = new PmlDocument(fileName);
            var slides = PresentationBuilder.PublishSlides(presentation);

            var targetDir = new FileInfo(fileName).DirectoryName;

            var result = 0;
            foreach (var slide in slides)
            {
                var targetPath = Path.Combine(targetDir, Path.GetFileName(slide.FileName));
                slide.SaveAs(targetPath);
                result++;
            }

            return result;
        }
    }
}
