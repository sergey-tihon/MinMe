using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using MinMe.Avalonia.ViewModels;

namespace MinMe.Avalonia.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            AddHandler(DragDrop.DropEvent, Drop);
            AddHandler(DragDrop.DragOverEvent, DragOver);
        }

        private void Drop(object? sender, DragEventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            if (e.Data.Contains(DataFormats.Text))
                vm.Greeting = e.Data.GetText();
            else if (e.Data.Contains(DataFormats.FileNames))
                vm.Greeting = string.Join(Environment.NewLine, e.Data.GetFileNames());
        }

        private void DragOver(object sender, DragEventArgs e)
        {
            // Only allow Copy or Link as Drop Operations.
            //e.DragEffects = e.DragEffects & (DragDropEffects.Copy | DragDropEffects.Link);

            // Only allow if the dragged data contains text or filenames.
            if (!e.Data.Contains(DataFormats.Text) && !e.Data.Contains(DataFormats.FileNames))
                e.DragEffects = DragDropEffects.None;
        }
    }
}