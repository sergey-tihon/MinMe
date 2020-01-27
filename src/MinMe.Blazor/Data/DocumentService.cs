using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Clippit;
using Clippit.PowerPoint;
using ElectronNET.API;
using ElectronNET.API.Entities;

namespace MinMe.Blazor.Data
{
    public class DocumentService
    {
        public DocumentService()
        {
        }

        public async Task<string> OpenFile()
        {
            var mainWindow = Electron.WindowManager.BrowserWindows.First();
            var options = new OpenDialogOptions
            {
                Properties = new OpenDialogProperty[] {
                    OpenDialogProperty.openFile
                },
                Filters = new FileFilter[]
                {
                    new FileFilter {
                        Name = "PowerPoint",
                        Extensions = new string[] {"pptx" }
                    }
                }
            };

            var files = await Electron.Dialog.ShowOpenDialogAsync(mainWindow, options);
            return files.FirstOrDefault();
            //Electron.IpcMain.Send(mainWindow, "select-directory-reply", files);
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
