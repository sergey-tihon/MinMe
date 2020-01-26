using System;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace MinMe.Avalonia.Views
{
    public class DragAndDropPage : UserControl
    {
        private TextBlock _dropState;
        private TextBlock _dragState;
        private Border _dragMe;
        private int _dragCount = 0;

        public DragAndDropPage()
        {
            InitializeComponent();

            _dragMe.PointerPressed += DoDrag;

            AddHandler(DragDrop.DropEvent, Drop);
            AddHandler(DragDrop.DragOverEvent, DragOver);
        }

        private async void DoDrag(object sender, global::Avalonia.Input.PointerPressedEventArgs e)
        {
            var dragData = new DataObject();
            dragData.Set(DataFormats.Text, $"You have dragged text {++_dragCount} times");

            var result = await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Copy);
            _dragState.Text = result switch
            {
                DragDropEffects.Copy => "The text was copied",
                DragDropEffects.Link => "The text was linked",
                DragDropEffects.None => "The drag operation was canceled",
                _ => _dragState.Text
            };
        }

        private void DragOver(object sender, DragEventArgs e)
        {
            // Only allow Copy or Link as Drop Operations.
            e.DragEffects = e.DragEffects & (DragDropEffects.Copy | DragDropEffects.Link);

            // Only allow if the dragged data contains text or filenames.
            if (!e.Data.Contains(DataFormats.Text) && !e.Data.Contains(DataFormats.FileNames))
                e.DragEffects = DragDropEffects.None;
        }

        private void Drop(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Text))
                _dropState.Text = e.Data.GetText();
            else if (e.Data.Contains(DataFormats.FileNames))
                _dropState.Text = string.Join(Environment.NewLine, e.Data.GetFileNames());
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _dropState = this.Find<TextBlock>("DropState");
            _dragState = this.Find<TextBlock>("DragState");
            _dragMe = this.Find<Border>("DragMe");
        }
    }
}