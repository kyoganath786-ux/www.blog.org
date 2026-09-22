using System.Collections.ObjectModel;
using NickAI.Core.Models;
using NickAI.Core.Providers;

namespace NickAI.App.ViewModels;

/// <summary>Lists installed local models and lets the user pick the active one.</summary>
public sealed class ModelsViewModel : ObservableObject
{
    private readonly ModelManager _manager;
    private bool _isBusy;
    private string _status = "Not checked yet";

    public ModelsViewModel(ModelManager manager)
    {
        _manager = manager;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        UseModelCommand = new RelayCommand(parameter =>
        {
            if (parameter is ModelInfo info) _manager.SelectModel(info.Name);
        });

        _manager.Changed += (_, _) => Reload();
        Reload();
    }

    public ObservableCollection<ModelInfo> Items { get; } = new();

    public AsyncRelayCommand RefreshCommand { get; }

    public RelayCommand UseModelCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                RefreshCommand.RaiseCanExecuteChanged();
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string? ActiveModel => _manager.ActiveModel;

    public bool IsEmpty => Items.Count == 0;

    public async Task RefreshAsync()
    {
        IsBusy = true;
        Status = "Checking Ollama...";
        try
        {
            await _manager.RefreshAsync();
            Status = Items.Count == 0
                ? "No models found. Make sure Ollama is running and you have pulled a model."
                : $"{Items.Count} model(s) available.";
        }
        catch (Exception ex)
        {
            Status = $"Could not reach a model provider: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Reload()
    {
        Items.Clear();
        foreach (var model in _manager.Models) Items.Add(model);
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ActiveModel));
    }
}
