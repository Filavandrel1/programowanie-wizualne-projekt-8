using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace projekt8plsdzialaj.Views;

public partial class HiLoView : UserControl
{
    public HiLoView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
