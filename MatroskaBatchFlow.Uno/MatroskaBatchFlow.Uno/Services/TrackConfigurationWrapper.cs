using System.ComponentModel;
using System.Runtime.CompilerServices;
using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core;

public class TrackConfigurationWrapper : INotifyPropertyChanged
{
    private readonly TrackConfiguration _model;

    public TrackConfigurationWrapper(TrackConfiguration model)
    {
        _model = model;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TrackType TrackType
    {
        get => _model.TrackType;
        set
        {
            if (_model.TrackType != value)
            {
                _model.TrackType = value;
                OnPropertyChanged();
            }
        }
    }

    public int Position
    {
        get => _model.Position;
        set
        {
            if (_model.Position != value)
            {
                _model.Position = value;
                OnPropertyChanged();
            }
        }
    }

    public string Name
    {
        get => _model.Name;
        set
        {
            if (_model.Name != value)
            {
                _model.Name = value;
                OnPropertyChanged();
            }
        }
    }

    public string Language
    {
        get => _model.Language;
        set
        {
            if (_model.Language != value)
            {
                _model.Language = value;
                OnPropertyChanged();
            }
        }
    }

    public bool Default
    {
        get => _model.Default;
        set
        {
            if (_model.Default != value)
            {
                _model.Default = value;
                OnPropertyChanged();
            }
        }
    }

    public bool Forced
    {
        get => _model.Forced;
        set
        {
            if (_model.Forced != value)
            {
                _model.Forced = value;
                OnPropertyChanged();
            }
        }
    }

    public bool Remove
    {
        get => _model.Remove;
        set
        {
            if (_model.Remove != value)
            {
                _model.Remove = value;
                OnPropertyChanged();
            }
        }
    }

    public TrackConfiguration Model => _model;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
