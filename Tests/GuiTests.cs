using Project.CLI.Facade;
using Project.Core.Characters.Models;
using Project.Core.Characters.Services;
using Project.GUI.Common;
using Project.GUI.Services;
using Project.GUI.ViewModels;
using Project.Infra.Caching;
using Project.Infra.Data.Repositories;
using Project.Infra.Data.SaveSystem;
using Project.Infra.Remote;
using Tests.Doubles;

namespace Tests;

/// <summary>
/// Unit test suite verifying Practical Work 16 Graphical User Interface (GUI):
/// - MVVM patterns: ObservableObject, RelayCommand, MainViewModel, CharacterCardViewModel
/// - Catalog Composite Card data binding and portrait resolution
/// - Text Game Console simulation, log routing via GuiDisplayer, and Save/Load/Continue integration
/// </summary>
public class GuiTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _cacheDir;
    private readonly string _savesDir;

    public GuiTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "pk16_tests_" + Guid.NewGuid().ToString("N"));
        _cacheDir = Path.Combine(_testDir, "cache", "portraits");
        _savesDir = Path.Combine(_testDir, "saves");
        Directory.CreateDirectory(_cacheDir);
        Directory.CreateDirectory(_savesDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void RelayCommand_ExecutesActionAndRespectsCanExecute()
    {
        // Arrange
        var executed = false;
        var canRun = true;
        var cmd = new RelayCommand(() => executed = true, () => canRun);

        // Assert initial
        Assert.True(cmd.CanExecute(null));

        // Act 1: Execute
        cmd.Execute(null);
        Assert.True(executed);

        // Act 2: Disable
        canRun = false;
        Assert.False(cmd.CanExecute(null));
    }

    [Fact]
    public void RelayCommandT_ExecutesWithTypedParameter()
    {
        // Arrange
        string? receivedParam = null;
        var cmd = new RelayCommand<string>(p => receivedParam = p, p => !string.IsNullOrEmpty(p));

        // Assert
        Assert.False(cmd.CanExecute(null));
        Assert.True(cmd.CanExecute("raiden"));

        cmd.Execute("raiden");
        Assert.Equal("raiden", receivedParam);
    }

    [Fact]
    public void CharacterCardViewModel_InitializesStatsAndBindsAttributes()
    {
        // Arrange
        var sword = new Item(1, "Skyward_Blade", 20, 30, armorMod: 10, speedMod: 5);
        var ability = new Ability(1, "GaleBlade", 50, 0, "Anemo slice");
        var character = new Character(1, "Jean", 120, 40, 0.2, armor: 15, speed: 110);
        character.AddItem(sword);
        character.AddAbility(ability);

        var cacheService = new LocalFileAssetCacheService(_cacheDir);

        // Act
        var card = new CharacterCardViewModel(character, null, cacheService);

        // Assert
        Assert.Equal(1, card.Id);
        Assert.Equal("Jean", card.Name);
        Assert.Equal("120 / 120 HP", card.HealthDisplay);
        Assert.Equal(100.0, card.HealthPercent);
        Assert.Equal(40, card.Power);
        Assert.Equal(15, card.Armor);
        Assert.Equal(110, card.Speed);
        Assert.Equal(0.2, card.Resistance);
        Assert.Single(card.EquippedItems);
        Assert.Single(card.LearnedAbilities);
        Assert.False(card.IsCached);
    }

    [Fact]
    public void CharacterCardViewModel_ResolvesCachedPortraitPath()
    {
        // Arrange
        var character = new Character(2, "Diluc", 100, 50, 0.1);
        var cacheService = new LocalFileAssetCacheService(_cacheDir);

        // Simulate local cache entry
        var cachedFile = cacheService.GetLocalFilePath("Diluc");
        File.WriteAllBytes(cachedFile, [0x89, 0x50, 0x4E, 0x47]);

        // Act
        var card = new CharacterCardViewModel(character, null, cacheService);

        // Assert
        Assert.True(card.IsCached);
        Assert.Equal(cachedFile, card.PortraitPath);
    }

    [Fact]
    public void MainViewModel_InitializesWithSeededCatalogAndSaveSlots()
    {
        // Arrange
        var (mainVm, game) = CreateTestMainViewModel();

        // Assert
        Assert.Equal(2, mainVm.Characters.Count);
        Assert.Equal("Hero1", mainVm.Characters[0].Name);
        Assert.Equal("Hero2", mainVm.Characters[1].Name);
        Assert.NotNull(mainVm.SelectedCharacter);
        Assert.NotNull(mainVm.SelectedDuelTarget);
        Assert.Contains("Modular RPG Platform", mainVm.ConsoleOutput);
    }

    [Fact]
    public void MainViewModel_AppendLog_AccumulatesOutput()
    {
        // Arrange
        var (mainVm, _) = CreateTestMainViewModel();

        // Act
        mainVm.AppendLog("Test event log message line.");

        // Assert
        Assert.Contains("Test event log message line.", mainVm.ConsoleOutput);
    }

    [Fact]
    public void MainViewModel_StartDuel_ExecutesTurnBasedCombatAndLogsSummary()
    {
        // Arrange
        var (mainVm, _) = CreateTestMainViewModel();
        mainVm.SelectedCharacter = mainVm.Characters[0];
        mainVm.SelectedDuelTarget = mainVm.Characters[1];

        // Act
        mainVm.StartDuelCommand.Execute(null);

        // Assert
        Assert.Contains("[Duel] Starting turn-based duel", mainVm.ConsoleOutput);
        Assert.Contains("[Duel] Battle ended! Winner:", mainVm.ConsoleOutput);
        Assert.Contains("ROUND", mainVm.ConsoleOutput); // ASCII table from BattleHistory
    }

    [Fact]
    public void MainViewModel_SaveAndContinue_ResumesSessionThroughGui()
    {
        // Arrange
        var (mainVm, game) = CreateTestMainViewModel();

        // Act 1: Save slot
        mainVm.SaveGameCommand.Execute("gui_save_slot_1");
        Assert.Contains("gui_save_slot_1", mainVm.AvailableSaveSlots);
        Assert.Contains("successfully saved to slot", mainVm.ConsoleOutput);

        // Act 2: Modify character health
        mainVm.Characters[0].Model.SetCurrentHealth(15);
        Assert.Equal(15, mainVm.Characters[0].CurrentHealth);

        // Act 3: Continue from latest save
        mainVm.ContinueGameCommand.Execute(null);

        // Assert: Restored health back to original 100
        Assert.Contains("Resumed game from most recent save", mainVm.ConsoleOutput);
        Assert.Equal(100, mainVm.Characters[0].CurrentHealth);
    }

    [Fact]
    public void MainViewModel_ExecuteCommand_RunsCliFromGuiInputBox()
    {
        // Arrange
        var (mainVm, _) = CreateTestMainViewModel();

        // Act: Type CLI command into GUI terminal input box
        mainVm.CommandInput = "chars";
        mainVm.ExecuteCommandInput.Execute(null);

        // Assert
        Assert.Contains("> chars", mainVm.ConsoleOutput);
        Assert.Empty(mainVm.CommandInput);
    }

    [Fact]
    public void GuiDisplayer_RoutesDomainMessagesToLogSink()
    {
        // Arrange
        var logged = new List<string>();
        var displayer = new GuiDisplayer(msg => logged.Add(msg));

        // Act
        displayer.Display("Domain level battle notification.");

        // Assert
        Assert.Single(logged);
        Assert.Equal("Domain level battle notification.", logged[0]);
    }

    private (MainViewModel Vm, Game Game) CreateTestMainViewModel()
    {
        var saveManager = new GameSaveManager(_savesDir);
        var cacheService = new LocalFileAssetCacheService(_cacheDir);
        var repository = new JsonRepositoryService(Path.Combine(_testDir, "repo.json"));
        var apiClient = new GenshinApiClient(new HttpClient());

        var game = new Game(new FakeDisplayer())
        {
            SaveManager = saveManager,
            PortraitCache = cacheService
        };

        game.CreateCharacter("Hero1", 100, 30, 0.1);
        game.CreateCharacter("Hero2", 100, 25, 0.1);

        var cliFacade = new CliFacade(game, repository, apiClient);
        var mainVm = new MainViewModel(game, cliFacade, cacheService, saveManager, apiClient);

        return (mainVm, game);
    }
}
