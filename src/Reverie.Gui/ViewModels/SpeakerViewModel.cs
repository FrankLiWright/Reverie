using Reverie.Core.Models;

namespace Reverie.Gui.ViewModels;

public sealed class SpeakerViewModel : ViewModelBase
{
    private double _x;
    private double _y;
    private bool _isSelected;

    public SpeakerViewModel(SpeakerPosition position)
    {
        Id = position.Id;
        _x = position.X;
        _y = position.Y;
        HeightTier = position.HeightTier;
    }

    public ChannelId Id { get; }
    public int HeightTier { get; }
    public string Label => Id.ToDisplay();
    public string ShortLabel => Id.ToString();

    public double X
    {
        get => _x;
        set
        {
            if (SetProperty(ref _x, Math.Clamp(value, 0.02, 0.98)))
                Raise(nameof(PixelHint));
        }
    }

    public double Y
    {
        get => _y;
        set
        {
            if (SetProperty(ref _y, Math.Clamp(value, 0.02, 0.98)))
                Raise(nameof(PixelHint));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string PixelHint => $"{X * 100:F0}% , {Y * 100:F0}%";
}
