using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GeoArbeitsvorbereitung.Models;
using GeoArbeitsvorbereitung.Services;

namespace GeoArbeitsvorbereitung.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ISettingsService _settingsService;
    private readonly IGeoFileService _geoFileService;
    private readonly IDialogService _dialogService;

    private string _searchRoot = string.Empty;
    private string _outputRoot = string.Empty;
    private string _drawingNumber = string.Empty;
    private GeoFileInfo? _foundFile;
    private string _newQuantity = string.Empty;
    private string _statusLog = string.Empty;
    private bool _settingsExpanded = true;

    public MainViewModel(
        ISettingsService settingsService,
        IGeoFileService geoFileService,
        IDialogService dialogService)
    {
        _settingsService = settingsService;
        _geoFileService = geoFileService;
        _dialogService = dialogService;

        var settings = _settingsService.Load();
        _searchRoot = settings.SearchRoot;
        _outputRoot = settings.OutputRoot;

        BrowseSearchRootCommand = new RelayCommand(_ => BrowseSearchRoot());
        BrowseOutputRootCommand = new RelayCommand(_ => BrowseOutputRoot());
        SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
        SearchCommand = new RelayCommand(
            _ => Search(),
            _ => !string.IsNullOrWhiteSpace(DrawingNumber));
        CreateCopyCommand = new RelayCommand(
            _ => CreateCopy(),
            _ => FoundFile != null && int.TryParse(NewQuantity, out int q) && q > 0);
        ToggleSettingsCommand = new RelayCommand(_ => SettingsExpanded = !SettingsExpanded);
    }

    // ── Settings ─────────────────────────────────────────────────────────────

    public string SearchRoot
    {
        get => _searchRoot;
        set { _searchRoot = value; OnPropertyChanged(); }
    }

    public string OutputRoot
    {
        get => _outputRoot;
        set { _outputRoot = value; OnPropertyChanged(); }
    }

    public bool SettingsExpanded
    {
        get => _settingsExpanded;
        set { _settingsExpanded = value; OnPropertyChanged(); OnPropertyChanged(nameof(SettingsToggleArrow)); }
    }

    public string SettingsToggleArrow => _settingsExpanded ? "▼" : "▶";

    // ── Search ────────────────────────────────────────────────────────────────

    public string DrawingNumber
    {
        get => _drawingNumber;
        set { _drawingNumber = value; OnPropertyChanged(); }
    }

    public GeoFileInfo? FoundFile
    {
        get => _foundFile;
        set
        {
            _foundFile = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasFoundFile));
        }
    }

    public bool HasFoundFile => _foundFile != null;

    // ── Modify ────────────────────────────────────────────────────────────────

    public string NewQuantity
    {
        get => _newQuantity;
        set { _newQuantity = value; OnPropertyChanged(); }
    }

    // ── Log ───────────────────────────────────────────────────────────────────

    public string StatusLog
    {
        get => _statusLog;
        set { _statusLog = value; OnPropertyChanged(); }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand BrowseSearchRootCommand { get; }
    public ICommand BrowseOutputRootCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand CreateCopyCommand { get; }
    public ICommand ToggleSettingsCommand { get; }

    // ── Implementations ───────────────────────────────────────────────────────

    private void BrowseSearchRoot()
    {
        var path = _dialogService.BrowseFolder("Suchordner wählen");
        if (path != null) SearchRoot = path;
    }

    private void BrowseOutputRoot()
    {
        var path = _dialogService.BrowseFolder("Speicherordner wählen");
        if (path != null) OutputRoot = path;
    }

    private void SaveSettings()
    {
        _settingsService.Save(new AppSettings
        {
            SearchRoot = SearchRoot,
            OutputRoot = OutputRoot,
        });
        Log("Einstellungen gespeichert.");
    }

    private void Search()
    {
        try
        {
            FoundFile = null;

            if (string.IsNullOrWhiteSpace(SearchRoot))
            {
                Log("Fehler: Kein Suchordner angegeben.");
                return;
            }

            var result = _geoFileService.FindNewest(SearchRoot, DrawingNumber.Trim());
            if (result == null)
            {
                Log($"Keine GEO-Datei für Zeichnungsnummer \"{DrawingNumber}\" gefunden.");
            }
            else
            {
                FoundFile = result;
                Log($"Gefunden: {result.FileName}  |  {result.LastWriteTime:dd.MM.yyyy HH:mm:ss}");
            }
        }
        catch (Exception ex)
        {
            Log($"Fehler bei der Suche: {ex.Message}");
        }
    }

    private void CreateCopy()
    {
        try
        {
            if (FoundFile == null) return;

            if (!int.TryParse(NewQuantity, out int qty) || qty <= 0)
            {
                Log("Fehler: Bitte eine gültige Stückzahl (> 0) eingeben.");
                return;
            }

            if (string.IsNullOrWhiteSpace(OutputRoot))
            {
                Log("Fehler: Kein Speicherordner angegeben.");
                return;
            }

            var newFileName = _geoFileService.BuildNewFileName(FoundFile, qty);
            var targetPath = _geoFileService.BuildTargetPath(OutputRoot, FoundFile, newFileName);

            if (File.Exists(targetPath))
            {
                var targetFolder = Path.GetDirectoryName(targetPath) ?? OutputRoot;
                var confirmedName = _dialogService.ShowTargetExistsDialog(targetFolder, newFileName);
                if (confirmedName == null)
                {
                    Log("Abgebrochen.");
                    return;
                }

                newFileName = confirmedName;
                targetPath = _geoFileService.BuildTargetPath(OutputRoot, FoundFile, newFileName);
            }

            _geoFileService.CopyFile(FoundFile.FullPath, targetPath);
            Log($"Kopie erstellt: {targetPath}");
        }
        catch (Exception ex)
        {
            Log($"Fehler beim Erstellen der Kopie: {ex.Message}");
        }
    }

    private void Log(string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}]  {message}";
        StatusLog = string.IsNullOrEmpty(StatusLog) ? entry : entry + Environment.NewLine + StatusLog;
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
