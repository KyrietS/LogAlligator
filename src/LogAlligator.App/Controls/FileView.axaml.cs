using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LogAlligator.App.LineProvider;
using LogAlligator.App.Utils;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;

namespace LogAlligator.App.Controls;

public partial class FileView : UserControl
{
    private readonly Highlights _highlights = new();
    private readonly Bookmarks _bookmarks = new();

    private Task? _loadTask = null;
    private CancellationTokenSource? _loadTaskCancellationToken = null;
    private readonly Uri? _filePath;
    private LoadingDataDialog? _loadingDialog = null;
    private GrepDialog? _grepDialog = null;

    private LogView? SelectedLogView => RootLogView.GetSelectedView();

    public event EventHandler? RemovalRequested;
    public Uri FilePath
    {
        get => _filePath ?? throw new InvalidOperationException("File path is not set");
        init
        {
            if (!value.IsAbsoluteUri)
                value = new Uri(Path.GetFullPath(value.OriginalString));
            _filePath = value;
        }
    }

    public FileView()
    {
        InitializeComponent();

        _highlights.OnChange += (_, _) => SelectedLogView?.TextView.Refresh();
        BookmarksView.JumpToBookmark += OnJumpToBookmark;
    }

    public void AddHighlight()
    {
        var selection = SelectedLogView?.TextView.GetSelectedText();
        if (string.IsNullOrEmpty(selection))
            return;

        if (_highlights.Contains(selection.AsMemory()))
        {
            _highlights.Remove(selection.AsMemory());
            return;
        }

        _highlights.Add(selection.AsMemory());
        Log.Debug("Highlight {selection}", selection);
    }

    public async void AddGrep()
    {
        try
        {
            _grepDialog = new GrepDialog();
            var (pattern, inverted) = await _grepDialog.ShowDialog<(SearchPattern?, bool)>((this.VisualRoot as Window)!);
            Log.Debug("Grep pattern: {pattern}, inverted: {inverted}", pattern, inverted);
            if (pattern != null)
                SelectedLogView?.AddGrep(pattern, inverted);
        }
        catch (Exception)
        {
            Log.Warning("Exception caught from grep dialog");
        }
        finally
        {
            _grepDialog?.Close();
            _grepDialog = null;
        }
    }

    private void OnLoadDataCancel(object? sender, RoutedEventArgs e)
    {
        if (_loadTask == null)
            return;
        if (_loadTask.IsCompleted)
            return;

        _loadTaskCancellationToken?.Cancel();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Design.IsDesignMode) return;

        if (FilePath.IsFile && _loadTask == null)
        {
            _loadTask = LoadFile();
        }
    }

    private async Task LoadFile()
    {
        try
        {
            ShowLoadingDialog();
            await LoadData();
        }
        catch (TaskCanceledException)
        {
            Log.Information("Loading data was canceled");
            RequestRemovalFromView();
        }
        catch (Exception e)
        {
            Log.Warning("Error when tried to load a file: {FilePath}", FilePath);
            Log.Warning("Exception: {Exception}", e);
            await ShowMessageBoxFileCouldNotBeLoaded();
            RequestRemovalFromView();
        }
        finally
        {
            _loadingDialog?.Close();
        }
    }

    private async Task LoadData()
    {
        _loadTaskCancellationToken = new CancellationTokenSource();
        var lineProvider = new BufferedFileLineProvider(FilePath);

        var watch = Stopwatch.StartNew();
        await lineProvider.LoadData(OnLoadProgress, _loadTaskCancellationToken.Token);
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;

        Log.Debug("Loaded {Lines} lines from {FilePath}. It took {ElapsedMs} ms", lineProvider.Count, FilePath, elapsedMs);

        RootLogView.Initialize(lineProvider, _highlights, _bookmarks);
        HighlightsView.Initialize(_highlights);
        BookmarksView.Initialize(_bookmarks);
    }

    private void OnLoadProgress(int progress)
    {
        if (_loadingDialog != null)
            _loadingDialog.Info.Text = $"Loaded {progress} lines";
    }

    private void ShowLoadingDialog()
    {
        _loadingDialog = new LoadingDataDialog();
        _loadingDialog.CancelButton.Click += OnLoadDataCancel;
        _ = _loadingDialog.ShowDialog((this.VisualRoot as Window)!);
    }

    private async Task ShowMessageBoxFileCouldNotBeLoaded()
    {
        var box = MessageBoxManager
            .GetMessageBoxStandard("Error", "File could not be loaded\nCheck logs for more details", ButtonEnum.Ok,
                Icon.Error, WindowStartupLocation.CenterOwner);
        await box.ShowWindowDialogAsync(this.VisualRoot as Window);
    }

    private void OnJumpToBookmark(object? sender, int lineNumber)
    {
        Log.Debug("Jumping to line {LineNumber}", lineNumber);
        SelectedLogView?.TextView.GoToLineNumber(lineNumber);
        SelectedLogView?.TextView.Refresh();
    }

    private void RequestRemovalFromView()
    {
        (this.VisualRoot as Window)?.Activate();
        RemovalRequested?.Invoke(this, EventArgs.Empty);
    }
}
