using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;
using OmarchyBackgrounds.Catalog;

namespace OmarchyBackgrounds_App;

public sealed class BackgroundItemVm : INotifyPropertyChanged
{
    public BackgroundItemVm(BackgroundImage background)
    {
        Background = background;
        FileName = background.FileName;
    }

    public BackgroundImage Background { get; }

    public string FileName { get; }

    private BitmapImage? _thumbnail;

    public BitmapImage? Thumbnail
    {
        get => _thumbnail;
        private set
        {
            if (!ReferenceEquals(_thumbnail, value))
            {
                _thumbnail = value;
                OnPropertyChanged();
            }
        }
    }

    public async Task LoadThumbnailAsync(CancellationToken cancellationToken = default)
    {
        var localPath = await AppServices.BackgroundStore.GetLocalPathAsync(Background, cancellationToken);

        await using var stream = File.OpenRead(localPath);
        var ras = stream.AsRandomAccessStream();
        var bitmap = new BitmapImage
        {
            DecodePixelWidth = 320,
        };
        await bitmap.SetSourceAsync(ras);
        Thumbnail = bitmap;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
