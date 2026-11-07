using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
namespace Hazirla;
// Yalnız iki küçük önizleme tutar; tam çözünürlüklü sonucu saklamaz.
public sealed class PreviewWindow : Window
{
    public PreviewWindow(byte[] before, byte[] after, string summary)
    {
        Title="Önce / Sonra";Width=850;Height=510;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var root=new DockPanel {Margin=new Thickness(16)};
        var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);
        footer.Children.Add(new TextBlock {Text=summary,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,12)});
        var save=new Button {Content="Bu sonucu kaydet",Padding=new Thickness(16,8,16,8),HorizontalAlignment=HorizontalAlignment.Right};
        save.Click+=(_,_)=>DialogResult=true;footer.Children.Add(save);root.Children.Add(footer);
        var grid=new Grid();grid.ColumnDefinitions.Add(new());grid.ColumnDefinitions.Add(new());
        Add(grid,"Önce",before,0);Add(grid,"Sonra",after,1);root.Children.Add(grid);Content=root;
    }
    private static void Add(Grid grid,string title,byte[] bytes,int column)
    {
        var panel=new DockPanel {Margin=new Thickness(8)};
        var label=new TextBlock {Text=title,FontSize=20};DockPanel.SetDock(label,Dock.Top);panel.Children.Add(label);
        using var stream=new MemoryStream(bytes);
        var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.EndInit();image.Freeze();
        panel.Children.Add(new Image {Source=image,Stretch=System.Windows.Media.Stretch.Uniform});Grid.SetColumn(panel,column);grid.Children.Add(panel);
    }
}
