using System;
using System.IO;
using System.Linq;
using System.Windows;
using DebitExpress.VatRelief.Models;
using DebitExpress.VatRelief.Utils;
using MaterialDesignThemes.Wpf;

namespace DebitExpress.VatRelief;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private string _filePath = string.Empty;
    private readonly SnackbarMessageQueue _messageQueue;
    private readonly SnackbarMessageQueue _errorQueue;

    public MainWindow()
    {
        InitializeComponent();
        Container.Drop += ContainerOnDrop;
        Container.DragOver += OnDragOver;
        Container.DragLeave += (_, _) => DragIndicator.Opacity = 1;

        _messageQueue = new SnackbarMessageQueue { DiscardDuplicates = true };
        _errorQueue = new SnackbarMessageQueue { DiscardDuplicates = true };
        SnackBar.MessageQueue = _messageQueue;
        ErrorSnackBar.MessageQueue = _errorQueue;

        GenerateButton.Click += OnGenerate;
    }

    #region Drag and drop

    private void ContainerOnDrop(object sender, DragEventArgs e)
    {
        try
        {
            var files = e.Data?.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>();
            var path = files.FirstOrDefault();

            if (string.IsNullOrEmpty(path)) return;

            var filename = Path.GetFileName(path);

            var extension = Path.GetExtension(path);
            if (extension != ".xlsx")
            {
                _messageQueue.Clear();
                _errorQueue.Enqueue("Invalid uploaded file", "×", () => { });
                return;
            }

            _filePath = path;
            DragIcon.Kind = PackIconKind.FileExcel;
            FileName.Text = filename;
            DragIndicator.Opacity = 1;
        }
        finally
        {
            GenerateButton.IsEnabled = true;
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        DragIndicator.Opacity = 0.65;
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    #endregion

    private async void OnGenerate(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                NotifyErrorResult("File not found");
                return;
            }
            GenerateButton.IsEnabled = false;

            var reader = new VatTemplateReader();
            var result = await reader.ReadAsync(Environment.ExpandEnvironmentVariables(_filePath));
            if (result.IsFaulted)
            {
                NotifyErrorResult(result.ToString());
                return;
            }

            const string loc = @"%USERPROFILE%\Desktop";
            var path = Environment.ExpandEnvironmentVariables(loc);
            ExcelData data = result;

            var generator = new DatFileGenerator();
            var generateResult = await generator.GenerateAsync(data, path);
            if (generateResult.IsFaulted)
            {
                NotifyErrorResult(generateResult.ToString());
                return;
            }

            var reconWriter = new ExcelReconWriter();
            var writeResult = reconWriter.WriteReconciliationReport(data, path);

            if (writeResult.IsSuccess)
                NotifyResult("Files generated successfully");
            else
                NotifyErrorResult(writeResult.ToString());
        }
        finally
        {
            GenerateButton.IsEnabled = true;
        }
    }

    private void NotifyErrorResult(string message)
    {
        _messageQueue.Clear();
        _errorQueue.Enqueue(message, "×", () => { });
    }

    private void NotifyResult(string message)
    {
        _errorQueue.Clear();
        _messageQueue.Enqueue(message, "×", () => { });
    }
}