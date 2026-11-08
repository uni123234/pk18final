using Project.Core.Characters.Models;
using Project.GUI.Common;
using Project.Infra.Caching;
using Project.Infra.Remote.Models;

namespace Project.GUI.ViewModels;

/// <summary>
/// ViewModel representing an individual composite Character Card for the WPF scrollable catalog.
/// Encapsulates visual assets, rarity styling, elemental affinities, and aggregated stats.
/// </summary>
public class CharacterCardViewModel : ObservableObject
{
    private readonly Character _character;
    private readonly IAssetCacheService? _cacheService;
    private string _portraitPath;
    private bool _isCached;

    public int Id => _character.Id;
    public string Name => _character.Name;
    public string Element { get; }
    public string WeaponType { get; }
    public string Nation { get; }
    public string RarityStars { get; }

    public int CurrentHealth => _character.Health;
    public int MaxHealth => _character.MaxHealth;
    public string HealthDisplay => $"{_character.Health} / {_character.MaxHealth} HP";
    public double HealthPercent => _character.MaxHealth <= 0 ? 0 : (double)_character.Health / _character.MaxHealth * 100.0;

    public int Power => _character.Power;
    public int Armor => _character.Armor;
    public int Speed => _character.Speed;
    public double Resistance => _character.Resistance;
    public bool IsAlive => _character.IsAlive;

    public string PortraitPath
    {
        get => _portraitPath;
        set => SetProperty(ref _portraitPath, value);
    }

    public bool IsCached
    {
        get => _isCached;
        set => SetProperty(ref _isCached, value);
    }

    public IReadOnlyList<string> EquippedItems => _character.ActiveItems.Select(i => i.Name).ToList();
    public IReadOnlyList<string> LearnedAbilities => _character.AvailableAbilities.Select(a => a.Name).ToList();

    public Character Model => _character;

    public CharacterCardViewModel(Character character, RemoteCharacterData? remoteData = null, IAssetCacheService? cacheService = null)
    {
        _character = character ?? throw new ArgumentNullException(nameof(character));
        _cacheService = cacheService;

        Element = remoteData?.Vision ?? "Neutral";
        WeaponType = remoteData?.Weapon ?? "Sword";
        Nation = remoteData?.Nation ?? "Teyvat";

        var stars = remoteData?.Rarity ?? 4;
        RarityStars = new string('★', Math.Clamp(stars, 1, 5));

        var cleanName = character.Name.ToLowerInvariant().Replace(' ', '-');
        var remoteUrl = $"https://genshin.jmp.blue/characters/{cleanName}/icon";

        if (_cacheService != null && _cacheService.IsCached(character.Name))
        {
            _portraitPath = _cacheService.GetLocalFilePath(character.Name);
            _isCached = true;
        }
        else
        {
            _portraitPath = remoteUrl;
            _isCached = false;
        }
    }

    public void RefreshPortrait()
    {
        if (_cacheService != null && _cacheService.IsCached(_character.Name))
        {
            PortraitPath = _cacheService.GetLocalFilePath(_character.Name);
            IsCached = true;
        }
    }

    public void RefreshStats()
    {
        OnPropertyChanged(nameof(CurrentHealth));
        OnPropertyChanged(nameof(HealthDisplay));
        OnPropertyChanged(nameof(HealthPercent));
        OnPropertyChanged(nameof(Power));
        OnPropertyChanged(nameof(Armor));
        OnPropertyChanged(nameof(Speed));
        OnPropertyChanged(nameof(Resistance));
        OnPropertyChanged(nameof(IsAlive));
        OnPropertyChanged(nameof(EquippedItems));
        OnPropertyChanged(nameof(LearnedAbilities));
    }
}
