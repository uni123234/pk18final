using System.Collections.ObjectModel;
using System.Windows.Input;
using Project.CLI.Facade;
using Project.Core.Characters.Models;
using Project.Core.Characters.Services;
using Project.GUI.Common;
using Project.Infra.Caching;
using Project.Infra.Data.SaveSystem;
using Project.Infra.Remote;

namespace Project.GUI.ViewModels;

/// <summary>
/// Root ViewModel orchestrating the Character Catalog and Text Game Console tabs.
/// Implements MVVM pattern, command binding, and real-time state synchronization.
/// </summary>
public class MainViewModel : ObservableObject
{
    private readonly Game _game;
    private readonly ICliFacade _cliFacade;
    private readonly IAssetCacheService _cacheService;
    private readonly IGameSaveManager _saveManager;
    private readonly IGenshinApiClient _apiClient;

    private CharacterCardViewModel? _selectedCharacter;
    private CharacterCardViewModel? _selectedDuelTarget;
    private string _consoleOutput = string.Empty;
    private string _commandInput = string.Empty;
    private string? _selectedSaveSlot;
    private string _statusMessage = "Ready.";
    private int _selectedTabIndex;
    private bool _isBusy;

    public ObservableCollection<CharacterCardViewModel> Characters { get; } = [];
    public ObservableCollection<string> AvailableSaveSlots { get; } = [];

    public CharacterCardViewModel? SelectedCharacter
    {
        get => _selectedCharacter;
        set => SetProperty(ref _selectedCharacter, value);
    }

    public CharacterCardViewModel? SelectedDuelTarget
    {
        get => _selectedDuelTarget;
        set => SetProperty(ref _selectedDuelTarget, value);
    }

    public string ConsoleOutput
    {
        get => _consoleOutput;
        set => SetProperty(ref _consoleOutput, value);
    }

    public string CommandInput
    {
        get => _commandInput;
        set => SetProperty(ref _commandInput, value);
    }

    public string? SelectedSaveSlot
    {
        get => _selectedSaveSlot;
        set => SetProperty(ref _selectedSaveSlot, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public int TotalCharactersCount => Characters.Count;

    // Commands
    public ICommand RefreshCatalogCommand { get; }
    public ICommand PrefetchPortraitsCommand { get; }
    public ICommand ImportFromApiCommand { get; }
    public ICommand SaveGameCommand { get; }
    public ICommand LoadGameCommand { get; }
    public ICommand ContinueGameCommand { get; }
    public ICommand StartDuelCommand { get; }
    public ICommand ExecuteCommandInput { get; }
    public ICommand ClearConsoleCommand { get; }

    public MainViewModel(
        Game game,
        ICliFacade cliFacade,
        IAssetCacheService cacheService,
        IGameSaveManager saveManager,
        IGenshinApiClient apiClient)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _cliFacade = cliFacade ?? throw new ArgumentNullException(nameof(cliFacade));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

        // Initialize Commands
        RefreshCatalogCommand = new RelayCommand(RefreshCatalog);
        PrefetchPortraitsCommand = new RelayCommand(async () => await PrefetchPortraitsAsync());
        ImportFromApiCommand = new RelayCommand<string>(async (id) => await ImportFromApiAsync(id ?? "raiden"));
        SaveGameCommand = new RelayCommand<string>(SaveGame);
        LoadGameCommand = new RelayCommand(LoadGame, () => !string.IsNullOrEmpty(SelectedSaveSlot));
        ContinueGameCommand = new RelayCommand(ContinueGame);
        StartDuelCommand = new RelayCommand(StartDuel, () => SelectedCharacter != null && SelectedDuelTarget != null && SelectedCharacter != SelectedDuelTarget);
        ExecuteCommandInput = new RelayCommand(ExecuteCommand, () => !string.IsNullOrWhiteSpace(CommandInput));
        ClearConsoleCommand = new RelayCommand(() => ConsoleOutput = string.Empty);

        // Initial Data Load
        RefreshCatalog();
        RefreshSaveSlots();
        AppendLog("=== Modular RPG Platform & Character Catalog GUI Initialized ===");
        AppendLog("System ready. Use top navigation tabs to inspect Catalog Cards or play in the Text Console.");
    }

    public void AppendLog(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        ConsoleOutput += message + Environment.NewLine;
    }

    public void RefreshCatalog()
    {
        Characters.Clear();
        foreach (var ch in _game.Characters)
        {
            var card = new CharacterCardViewModel(ch, null, _cacheService);
            Characters.Add(card);
        }

        if (Characters.Count > 0 && SelectedCharacter == null)
        {
            SelectedCharacter = Characters[0];
        }

        if (Characters.Count > 1 && SelectedDuelTarget == null)
        {
            SelectedDuelTarget = Characters[1];
        }

        OnPropertyChanged(nameof(TotalCharactersCount));
        StatusMessage = $"Catalog loaded with {Characters.Count} characters.";
    }

    public void RefreshSaveSlots()
    {
        AvailableSaveSlots.Clear();
        var saves = _saveManager.ListSaves();
        foreach (var s in saves)
        {
            AvailableSaveSlots.Add(s.SlotName);
        }

        if (AvailableSaveSlots.Count > 0 && string.IsNullOrEmpty(SelectedSaveSlot))
        {
            SelectedSaveSlot = AvailableSaveSlots[0];
        }
    }

    public async Task PrefetchPortraitsAsync()
    {
        IsBusy = true;
        StatusMessage = "Prefetching and caching portraits to local disk...";
        AppendLog("[Cache] Starting portrait batch prefetching...");

        var names = _game.Characters.Select(c => c.Name).ToList();
        var cachedPaths = await _cacheService.PrefetchPortraitsAsync(names);

        foreach (var card in Characters)
        {
            card.RefreshPortrait();
        }

        var stats = _cacheService.GetStatistics();
        AppendLog($"[Cache] Prefetch complete. Total cached: {stats.TotalCachedFiles} ({stats.TotalSizeBytes:N0} bytes).");
        StatusMessage = $"Cached {stats.TotalCachedFiles} portraits locally.";
        IsBusy = false;
    }

    public async Task ImportFromApiAsync(string characterId)
    {
        IsBusy = true;
        StatusMessage = $"Querying Genshin API for '{characterId}'...";
        AppendLog($"[API] Importing character profile '{characterId}'...");

        try
        {
            var remote = await _apiClient.GetCharacterAsync(characterId);
            if (remote != null)
            {
                var localPortrait = await _cacheService.GetOrDownloadPortraitAsync(remote.Name);
                var ch = _game.CreateCharacter(remote.Name, 120, 35, 0.15);
                var card = new CharacterCardViewModel(ch, remote, _cacheService);
                Characters.Add(card);
                SelectedCharacter = card;
                OnPropertyChanged(nameof(TotalCharactersCount));

                AppendLog($"[API] Successfully imported '{remote.Name}' (Rarity: {remote.Rarity}★, Vision: {remote.Vision}).");
                StatusMessage = $"Imported '{remote.Name}' successfully.";
            }
            else
            {
                AppendLog($"[API] Character '{characterId}' not found on remote server.");
                StatusMessage = "Import failed: Not found.";
            }
        }
        catch (Exception ex)
        {
            AppendLog($"[API] Error during import: {ex.Message}");
            StatusMessage = "Import failed: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SaveGame(string? slotName)
    {
        var slot = string.IsNullOrWhiteSpace(slotName) ? $"save_{DateTime.UtcNow:yyyyMMdd_HHmmss}" : slotName;
        try
        {
            var meta = _saveManager.Save(slot, _game);
            RefreshSaveSlots();
            SelectedSaveSlot = meta.SlotName;
            AppendLog($"[Save] Game state successfully saved to slot '{meta.SlotName}' ({meta.CharacterCount} characters, {meta.FileSizeBytes} bytes).");
            StatusMessage = $"Saved to slot '{meta.SlotName}'.";
        }
        catch (Exception ex)
        {
            AppendLog($"[Save] Error: {ex.Message}");
            StatusMessage = "Save failed: " + ex.Message;
        }
    }

    public void LoadGame()
    {
        if (string.IsNullOrEmpty(SelectedSaveSlot)) return;

        try
        {
            var snapshot = _saveManager.Load(SelectedSaveSlot);
            _game.ImportSave(snapshot);
            RefreshCatalog();
            AppendLog($"[Load] Successfully loaded state from '{snapshot.SaveName}' ({_game.Characters.Count} characters restored).");
            StatusMessage = $"Restored state from '{snapshot.SaveName}'.";
        }
        catch (Exception ex)
        {
            AppendLog($"[Load] Error: {ex.Message}");
            StatusMessage = "Load failed: " + ex.Message;
        }
    }

    public void ContinueGame()
    {
        try
        {
            var latest = _saveManager.GetLatestSave();
            if (latest == null)
            {
                AppendLog("[Continue] No existing save files found.");
                StatusMessage = "No save found to continue.";
                return;
            }

            var snapshot = _saveManager.ContinueFromLatest(_game);
            RefreshCatalog();
            RefreshSaveSlots();
            SelectedSaveSlot = latest.SlotName;

            AppendLog($"[Continue] Resumed game from most recent save '{latest.SlotName}' (Saved: {latest.Timestamp:yyyy-MM-dd HH:mm:ss} UTC).");
            StatusMessage = $"Continued from '{latest.SlotName}'.";
        }
        catch (Exception ex)
        {
            AppendLog($"[Continue] Error: {ex.Message}");
            StatusMessage = "Continue failed: " + ex.Message;
        }
    }

    public void StartDuel()
    {
        if (SelectedCharacter == null || SelectedDuelTarget == null) return;

        AppendLog($"[Duel] Starting turn-based duel: {SelectedCharacter.Name} vs {SelectedDuelTarget.Name}!");
        var history = _game.StartTurnBasedDuel(SelectedCharacter.Model, SelectedDuelTarget.Model);

        SelectedCharacter.RefreshStats();
        SelectedDuelTarget.RefreshStats();

        AppendLog(history.RenderTable());
        AppendLog($"[Duel] Battle ended! Winner: {history.WinnerName} in {history.TotalRounds} rounds.");
        StatusMessage = $"Duel finished. Winner: {history.WinnerName}.";
    }

    public void ExecuteCommand()
    {
        if (string.IsNullOrWhiteSpace(CommandInput)) return;

        var cmd = CommandInput.Trim();
        CommandInput = string.Empty;
        AppendLog($"> {cmd}");

        try
        {
            var result = _cliFacade.ExecCommand(cmd);
            if (!string.IsNullOrEmpty(result.Message))
            {
                AppendLog(result.Message);
            }

            RefreshCatalog();
            RefreshSaveSlots();
            StatusMessage = result.IsSuccess ? "Command executed." : "Command error: " + result.Message;
        }
        catch (Exception ex)
        {
            AppendLog($"[Error] {ex.Message}");
            StatusMessage = "Command failed: " + ex.Message;
        }
    }
}
