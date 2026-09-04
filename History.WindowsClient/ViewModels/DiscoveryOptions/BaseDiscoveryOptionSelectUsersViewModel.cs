using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;

namespace History.WindowsClient.ViewModels.DiscoveryOptions;

// Base view model for user selection dialogs handling filter, selection, and presets.
public abstract partial class BaseDiscoveryOptionSelectUsersViewModel : BaseViewModel
{
    private const string PresetsSettingKey = "DiscoveryOptionSelectUsersPresets";

    public BaseViewModel BaseViewModel { get; }

    public ObservableCollection<BaseSelectUserViewModel> SelectedUsers { get; } = [];
    public ObservableCollection<BaseSelectUserViewModel> FilteredUsers { get; } = [];
    public ObservableCollection<DiscoveryOptionPresetItemViewModel> Presets { get; } = [];

    protected List<BaseSelectUserViewModel> AllUsers { get; } = [];

    public List<string> SelectedUserIds =>
    [.. SelectedUsers.Select(user => user.UserId)];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool HasSelectedUsers { get; set; }

    [ObservableProperty]
    public partial int SelectedCount { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasPresets { get; set; }

    [ObservableProperty]
    public partial string NewPresetName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PresetStatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasPresetStatusMessage { get; set; }

    protected BaseDiscoveryOptionSelectUsersViewModel(BaseViewModel baseViewModel)
    {
        BaseViewModel = baseViewModel;
        LoadPresets();
    }

    public abstract Task InitializeAsync();

    protected abstract bool MatchesFilter(BaseSelectUserViewModel user, string query);

    protected void SetAllUsers(IEnumerable<BaseSelectUserViewModel> users)
    {
        foreach (var user in AllUsers) user.SelectionChanged -= OnUserSelectionChanged;
        AllUsers.Clear();
        SelectedUsers.Clear();

        foreach (var user in users)
        {
            user.SelectionChanged += OnUserSelectionChanged;
            if (user.IsSelected) SelectedUsers.Add(user);
            AllUsers.Add(user);
        }

        UpdateSelectedState();
        ApplyFilter(SearchText);
    }

    private void OnUserSelectionChanged(object sender, bool isSelected)
    {
        if (sender is not BaseSelectUserViewModel user) return;

        if (isSelected)
        {
            if (!SelectedUsers.Contains(user))
            {
                SelectedUsers.Add(user);
            }
        }
        else SelectedUsers.Remove(user);

        UpdateSelectedState();
    }

    private void UpdateSelectedState()
    {
        SelectedCount = SelectedUsers.Count;
        HasSelectedUsers = SelectedUsers.Count > 0;
    }

    public void ApplyFilter(string query)
    {
        var trimmedQuery = query?.Trim() ?? string.Empty;
        FilteredUsers.Clear();

        var matches = string.IsNullOrEmpty(trimmedQuery) ? AllUsers : AllUsers.Where(user => MatchesFilter(user, trimmedQuery));
        foreach (var user in matches) FilteredUsers.Add(user);

        IsEmpty = FilteredUsers.Count == 0;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter(value);

    [RelayCommand]
    public void RemoveSelectedUser(BaseSelectUserViewModel user)
    {
        if (user != null)
        {
            user.IsSelected = false;
        }
    }

    [RelayCommand]
    public void ClearAllSelectedUsers()
    {
        foreach (var user in SelectedUsers.ToList())
        {
            user.IsSelected = false;
        }
    }

    public void LoadPresets()
    {
        Presets.Clear();
        var presetsDictionary = GetPresetsDictionary();
        foreach (var pair in presetsDictionary) Presets.Add(new DiscoveryOptionPresetItemViewModel(pair.Key, pair.Value));
        HasPresets = Presets.Count > 0;
    }

    [RelayCommand]
    public void SaveCurrentAsPreset()
    {
        var trimmedName = NewPresetName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ShowPresetStatus("프리셋 이름을 입력해주세요.");
            return;
        }

        if (trimmedName.Length > 20)
        {
            ShowPresetStatus("프리셋 이름은 20자 이하로 입력해주세요.");
            return;
        }

        if (SelectedUsers.Count == 0)
        {
            ShowPresetStatus("프리셋을 저장하려면 최소 한 명 이상의 친구를 선택해야 합니다.");
            return;
        }

        var presetsDictionary = GetPresetsDictionary();
        if (presetsDictionary.Count >= 10)
        {
            ShowPresetStatus("프리셋은 최대 10개까지 저장할 수 있습니다.");
            return;
        }

        if (presetsDictionary.ContainsKey(trimmedName))
        {
            ShowPresetStatus($"'{trimmedName}' 이름의 프리셋이 이미 존재합니다.");
            return;
        }

        var currentIds = SelectedUserIds;
        if (presetsDictionary.Values.Any(list => !list.Except(currentIds).Any() && !currentIds.Except(list).Any()))
        {
            ShowPresetStatus("동일한 친구 구성의 프리셋이 이미 존재합니다.");
            return;
        }

        presetsDictionary[trimmedName] = currentIds;
        SavePresetsDictionary(presetsDictionary);
        NewPresetName = string.Empty;
        LoadPresets();
        ShowPresetStatus($"'{trimmedName}' 프리셋이 저장되었습니다.");
    }

    [RelayCommand]
    public void LoadPreset(DiscoveryOptionPresetItemViewModel preset)
    {
        if (preset == null) return;

        foreach (var user in AllUsers) user.IsSelected = preset.UserIds.Contains(user.UserId);
        ShowPresetStatus($"'{preset.Name}' 프리셋을 불러왔습니다.");
    }

    [RelayCommand]
    public void DeletePreset(DiscoveryOptionPresetItemViewModel preset)
    {
        if (preset == null) return;

        var presetsDictionary = GetPresetsDictionary();
        if (presetsDictionary.Remove(preset.Name))
        {
            SavePresetsDictionary(presetsDictionary);
            LoadPresets();
            ShowPresetStatus($"'{preset.Name}' 프리셋이 삭제되었습니다.");
        }
    }

    private void ShowPresetStatus(string message)
    {
        PresetStatusMessage = message;
        HasPresetStatusMessage = true;
    }

    private static Dictionary<string, List<string>> GetPresetsDictionary()
    {
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(PresetsSettingKey, out var value) && value is string json && !string.IsNullOrWhiteSpace(json)) return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? [];
        }
        catch { }

        return [];
    }

    private static void SavePresetsDictionary(Dictionary<string, List<string>> dictionary)
    {
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[PresetsSettingKey] = JsonSerializer.Serialize(dictionary);
        }
        catch { }
    }
}
