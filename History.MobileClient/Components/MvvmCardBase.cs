using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNetCore.Components;
using System.ComponentModel;

namespace History.MobileClient.Components;

// Blazor card base that mirrors MAUI's BindingContext behavior for ObservableObject view
// models: subscribes to PropertyChanged and re-renders on any property change. List items
// pass their view models as a parameter instead of resolving them from DI.
public abstract class MvvmCardBase<TViewModel> : ComponentBase, IDisposable
    where TViewModel : ObservableObject
{
    private TViewModel _viewModel;

    [Parameter, EditorRequired]
    public TViewModel ViewModel { get; set; }

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_viewModel, ViewModel)) return;

        if (_viewModel != null) _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = ViewModel;
        if (_viewModel != null) _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        OnViewModelChanged(_viewModel);
    }

    // Hook for derived components that must subscribe to nested observable items.
    protected virtual void OnViewModelChanged(TViewModel viewModel) { }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public virtual void Dispose()
    {
        if (_viewModel != null) _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        GC.SuppressFinalize(this);
    }
}
