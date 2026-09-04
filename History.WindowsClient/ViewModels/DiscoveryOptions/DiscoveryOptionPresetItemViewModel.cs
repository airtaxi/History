using CommunityToolkit.Mvvm.ComponentModel;

namespace History.WindowsClient.ViewModels.DiscoveryOptions;

// Represents a saved preset entry for discovery option user selection.
public sealed partial class DiscoveryOptionPresetItemViewModel(string name, List<string> userIds) : ObservableObject
{
    public string Name { get; } = name;
    public List<string> UserIds { get; } = userIds;
    public int Count => UserIds.Count;
    public string DisplayText => $"{Name} ({Count}명)";
}
