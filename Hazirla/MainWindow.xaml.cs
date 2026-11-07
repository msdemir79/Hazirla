using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using Microsoft.Win32;
using Hazirla.Services;
using SkiaSharp;
namespace Hazirla;
public sealed record InputFile(string Path)
{
    public string Name => System.IO.Path.GetFileName(Path);
    public string Details { get { try { return $"{new FileInfo(Path).Length / 1_000_000.0:F2} MB · {System.IO.Path.GetExtension(Path).ToUpperInvariant()}"; } catch(IOException) { return "Dosyaya erişilemiyor"; } } }
}
public partial class MainWindow : Window
{
    private readonly ObservableCollection<InputFile> files=new();
    private readonly ImageService service=new();
    private CancellationTokenSource? cancellation;
    private string? outputFolder;
    private readonly SettingsStore settingsStore=new();
    private string? lastFolder;
        private System.Windows.Forms.NotifyIcon? tray;
    private bool exiting;
    public MainWindow()
    {
                InitializeComponent(); Files.ItemsSource=files;
        var saved=settingsStore.Load();
        Mode.SelectedIndex=saved.Mode;Format.SelectedIndex=saved.Format;Target.Text=saved.Target;lastFolder=saved.Folder;
        Mode.SelectionChanged+=(_,_)=>SaveSettings();
        Format.SelectionChanged+=(_,_)=>SaveSettings();
        Target.LostKeyboardFocus+=(_,_)=>SaveSettings();
        files.CollectionChanged+=(_,_)=>UpdateControls();
        var menu=new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Hazırla’yı aç",null,(_,_)=>RestoreWindow());
        menu.Items.Add("Çıkış",null,(_,_)=>
        {
            if(cancellation is not null){Status.Text="Çıkmadan önce işlemi iptal et veya tamamlanmasını bekle.";RestoreWindow();return;}
            SaveSettings();exiting=true;Close();
        });
        var resource=System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/Hazirla.ico"));
        using var icon=new System.Drawing.Icon(resource.Stream);
        tray=new System.Windows.Forms.NotifyIcon { Text="Hazırla",Icon=(System.Drawing.Icon)icon.Clone(),ContextMenuStrip=menu,Visible=true };
        resource.Stream.Dispose();
        tray.DoubleClick+=(_,_)=>RestoreWindow();
                Closing+=(_,e)=>{if(!exiting){e.Cancel=true;HideToTray();}};
        StateChanged+=(_,_)=>{if(WindowState==WindowState.Minimized && !exiting)HideToTray();};
        tray.BalloonTipClicked+=(_,_)=>RestoreWindow();
        Closed+=(_,_)=>{if(tray is not null){tray.Visible=false;tray.Icon?.Dispose();tray.ContextMenuStrip?.Dispose();tray.Dispose();}};
        UpdateControls();
    }
        private void HideToTray()
    { SaveSettings();
        Hide();
        tray?.ShowBalloonTip(4000, "Hazırla hâlâ çalışıyor",
            "Hazırla sistem tepsisinde açık. Yeniden açmak için H ikonuna çift tıkla. Tam kapatmak için sağ tık → Çıkış.",
            System.Windows.Forms.ToolTipIcon.Info);
    }
    private void SaveSettings()
    {
        var previous=settingsStore.Load();
        var candidate=SettingsStore.Normalize(new UserSettings(Mode.SelectedIndex,Format.SelectedIndex,Target.Text,lastFolder));
        // Geçersiz giriş, önceki geçerli hedefi silmesin.
        if(!double.TryParse(Target.Text.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var mb)||!double.IsFinite(mb)||mb<0.01||mb>100)
            candidate=candidate with {Target=previous.Target};
        if(!settingsStore.Save(candidate))Status.Text="Ayarlar kaydedilemedi; bu oturumda çalışmaya devam edebilirsin.";
    }
    public void RestoreFromLaunch()=>RestoreWindow();
    private void RestoreWindow(){Show();WindowState=WindowState.Normal;Activate();}
    private void OnSelectionChanged(object s,System.Windows.Controls.SelectionChangedEventArgs e)=>UpdateControls();
    private void OnModeChanged(object s,System.Windows.Controls.SelectionChangedEventArgs e)=>UpdateControls();
    private void UpdateControls()
    {
        if(!IsInitialized || RunButton is null)return;
        bool busy=cancellation is not null;
        RunButton.IsEnabled=!busy&&files.Count>0;
        PreviewButton.IsEnabled=!busy&&Files.SelectedItem is InputFile&&Mode.SelectedIndex!=2;
        CancelButton.IsEnabled=busy; RetryButton.IsEnabled=!busy&&failedFiles.Count>0; RetryButton.Visibility=failedFiles.Count>0?Visibility.Visible:Visibility.Collapsed;
        OpenButton.IsEnabled=outputFolder is not null;
        SettingsPanel.IsEnabled=!busy;FileTools.IsEnabled=!busy;OrderTools.IsEnabled=!busy;Files.IsEnabled=!busy;
        RemoveButton.IsEnabled=Files.SelectedItem is InputFile;
        EmptyHint.Visibility=files.Count==0?Visibility.Visible:Visibility.Collapsed;
        FileCount.Text=$"{files.Count} dosya";
        TargetPanel.Visibility=Mode.SelectedIndex==0?Visibility.Visible:Visibility.Collapsed;
        FormatPanel.Visibility=Mode.SelectedIndex==2?Visibility.Collapsed:Visibility.Visible;
        ModeHint.Text=Mode.SelectedIndex==2?"Her görsel bir A4 sayfaya yerleşir. Sayfa sırasını oklarla değiştir. Boyut sınırı uygulanmaz.":Mode.SelectedIndex==1?"JPG saydam alanları beyaz yapar. PNG ve WebP saydamlığı korur.":"Hedef için kalite ve gerekirse çözünürlük azaltılır. Önizlemeyle kontrol edebilirsin.";
    }
    private void Add(IEnumerable<string> paths)
    {
        foreach(var path in paths)
            if (System.IO.File.Exists(path) && new[]{".jpg",".jpeg",".png",".webp"}.Contains(System.IO.Path.GetExtension(path).ToLowerInvariant()) && !files.Any(f=>string.Equals(f.Path,path,StringComparison.OrdinalIgnoreCase))) files.Add(new(path));
        Status.Text=$"{files.Count} görsel seçildi.";
    }
    private void OnAdd(object s,RoutedEventArgs e) { var d=new OpenFileDialog { Multiselect=true,Filter="Görseller|*.jpg;*.jpeg;*.png;*.webp" }; if(d.ShowDialog(this)==true) Add(d.FileNames); }
    private void OnDrop(object s,DragEventArgs e) { if(e.Data.GetData(DataFormats.FileDrop) is string[] paths) Add(paths); }
    private void OnDragOver(object s,DragEventArgs e) { e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true; }
    private void OnRemove(object s,RoutedEventArgs e) { if(Files.SelectedItem is InputFile f) files.Remove(f); }
    private void OnUp(object s,RoutedEventArgs e) { int i=Files.SelectedIndex;if(i>0){files.Move(i,i-1);Files.SelectedIndex=i-1;} }
    private void OnDown(object s,RoutedEventArgs e) { int i=Files.SelectedIndex;if(i>=0&&i<files.Count-1){files.Move(i,i+1);Files.SelectedIndex=i+1;} }
    private void OnCancel(object s,RoutedEventArgs e)=>cancellation?.Cancel();
    private void OnOpen(object s,RoutedEventArgs e) { if(outputFolder is not null) Process.Start(new ProcessStartInfo(outputFolder){UseShellExecute=true}); }
        private static string Summary(long before,long after,int width,int height)
    {
        double change=before==0?0:100.0*(before-after)/before;
        return $"{before/1_000_000.0:F2} MB → {after/1_000_000.0:F2} MB · %{Math.Abs(change):F1} {(change>=0?"küçüldü":"büyüdü")} · {width}×{height}";
    }
    private async void OnPreview(object s,RoutedEventArgs e)
    {
        if(cancellation is not null)return;
        if(Files.SelectedItem is not InputFile input){Status.Text="Önizleme için listeden bir görsel seç.";return;}
        if(Mode.SelectedIndex==2){Status.Text="Önce/sonra önizlemesi görsel işlemleri içindir.";return;}
        long? limit=null;
        if(Mode.SelectedIndex==0)
        {
            if(!double.TryParse(Target.Text.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var mb)||!double.IsFinite(mb)||mb<0.01||mb>100)
            {Status.Text="Hedef boyut 0,01–100 MB arasında olmalı.";return;}
            limit=(long)(mb*1_000_000);
        }
        int index=Format.SelectedIndex;
        var format=index==1?SKEncodedImageFormat.Png:index==2?SKEncodedImageFormat.Webp:SKEncodedImageFormat.Jpeg;
        string ext=index==1?"png":index==2?"webp":"jpg";
        var temp=Path.Combine(Path.GetTempPath(),"Hazirla-preview-"+Guid.NewGuid().ToString("N")+"."+ext);
        using var stop=new CancellationTokenSource();cancellation=stop;UpdateControls();
        try
        {
            Status.Text="Önizleme hazırlanıyor…";
            // Tam boyutlu baytlar yalnız bu görevde yaşar; modal açıkken diskte tutulur.
            var preview=await Task.Run(()=>
            {
                var result=service.Convert(input.Path,format,limit,stop.Token);
                stop.Token.ThrowIfCancellationRequested();File.WriteAllBytes(temp,result.Bytes);
                return (Before:service.Thumbnail(input.Path),After:service.Thumbnail(temp),Text:Summary(new FileInfo(input.Path).Length,result.Bytes.LongLength,result.Width,result.Height));
            });
            stop.Token.ThrowIfCancellationRequested();
            var window=new PreviewWindow(preview.Before,preview.After,preview.Text){Owner=this};
            if(window.ShowDialog()==true)
            {
                var save=new SaveFileDialog {InitialDirectory=Directory.Exists(lastFolder)?lastFolder:Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),Filter=$"{ext.ToUpperInvariant()}|*.{ext}",FileName=Path.GetFileNameWithoutExtension(input.Path)+"-hazir."+ext};
                if(save.ShowDialog(this)==true)
                {
                    if(string.Equals(Path.GetFullPath(save.FileName),Path.GetFullPath(input.Path),StringComparison.OrdinalIgnoreCase))throw new IOException("Orijinalin üzerine kaydetmek yerine farklı bir ad seç.");
                    File.Copy(temp,save.FileName,true);outputFolder=Path.GetDirectoryName(save.FileName);lastFolder=outputFolder;SaveSettings();
                    Status.Text="Önizlenen sonuç kaydedildi.";ResultsExpander.IsExpanded=false;Results.AppendText(input.Name+": "+preview.Text+Environment.NewLine);
                }
            }
            else Status.Text="Önizleme kapatıldı; sonuç kaydedilmedi.";
        }
        catch(OperationCanceledException){Status.Text="Önizleme iptal edildi.";}
        catch(Exception ex){Status.Text="Önizleme hazırlanamadı: "+ex.Message;}
        finally{try{File.Delete(temp);}catch(IOException){}cancellation=null;UpdateControls();}
    }
    private readonly List<string> failedFiles=new();
    private (int Mode,int Format,long? Limit)? retrySettings;
    private async void OnRun(object s,RoutedEventArgs e) => await RunBatchAsync(false);
    private async void OnRetry(object s,RoutedEventArgs e) => await RunBatchAsync(true);
    private async Task RunBatchAsync(bool retry)
    {
        if(cancellation is not null)return;
        var snapshot=retry?failedFiles.ToArray():files.Select(f=>f.Path).ToArray();
        if(snapshot.Length==0){Status.Text="İşlenecek dosya yok.";return;}
        int mode=retry?retrySettings!.Value.Mode:Mode.SelectedIndex;
        int formatIndex=retry?retrySettings!.Value.Format:Format.SelectedIndex;
        long? limit=retry?retrySettings!.Value.Limit:null;
        if(!retry&&mode==0)
        {
            if(!double.TryParse(Target.Text.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var mb)||!double.IsFinite(mb)||mb<0.01||mb>100)
            {Status.Text="Hedef boyut 0,01–100 MB arasında olmalı.";return;}
            limit=(long)(mb*1_000_000);
        }
        var dialog=new OpenFolderDialog {InitialDirectory=Directory.Exists(lastFolder)?lastFolder:Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),Title=retry?"Yeniden denenen dosyaların sonuç klasörünü seç":"Sonuç klasörünü seç"};
        if(dialog.ShowDialog(this)!=true)return;
        lastFolder=dialog.FolderName;SaveSettings();
        using var stop=new CancellationTokenSource();cancellation=stop;
        failedFiles.Clear();retrySettings=(mode,formatIndex,limit);
        UpdateControls();Results.Clear();ResultsExpander.IsExpanded=false;Progress.Value=0;
        int completed=0,processed=0;
        string? temp=null;
        try
        {
            outputFolder=Path.Combine(dialog.FolderName,"Hazirla-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..6]);
            Directory.CreateDirectory(outputFolder);
            if(mode==2)
            {
                Status.Text="PDF hazırlanıyor…";
                temp=Path.Combine(outputFolder,"Fotograflar.pdf.part");
                var accepted=new List<string>();
                await Task.Run(()=>service.CreatePdf(snapshot,temp,stop.Token,(path,error)=>
                {
                    Dispatcher.Invoke(()=>
                    {
                        processed++;
                        if(error is not null){failedFiles.Add(path);Results.AppendText($"Başarısız · {Path.GetFileName(path)}: {error.Message}\n");}
                        else accepted.Add(path);
                        Progress.Value=100.0*processed/snapshot.Length;
                    });
                }));
                stop.Token.ThrowIfCancellationRequested();
                if(accepted.Count>0)
                {
                    File.Move(temp,Path.Combine(outputFolder,"Fotograflar.pdf"));temp=null;
                    completed=accepted.Count;
                    foreach(var path in accepted)Results.AppendText($"Tamamlandı · {Path.GetFileName(path)} → Fotograflar.pdf\n");
                }
            }
            else
            {
                var format=formatIndex==1?SKEncodedImageFormat.Png:formatIndex==2?SKEncodedImageFormat.Webp:SKEncodedImageFormat.Jpeg;
                await new BatchService().RunAsync(snapshot,outputFolder,format,limit,item=>
                {
                    processed++;
                    if(item.Error is not null){failedFiles.Add(item.Path);Results.AppendText($"Başarısız · {Path.GetFileName(item.Path)}: {item.Error}\n");}
                    else {completed++;Results.AppendText($"Tamamlandı · {Path.GetFileName(item.Path)}: {item.Summary}\n");}
                    Progress.Value=100.0*processed/snapshot.Length;
                    Status.Text=$"{processed}/{snapshot.Length} · {completed} tamamlandı, {failedFiles.Count} başarısız";
                },stop.Token);
            }
            Progress.Value=100;Status.Text=$"Bitti · {completed} tamamlandı, {failedFiles.Count} başarısız.";
        }
        catch(OperationCanceledException){Status.Text=$"İptal edildi · {completed} tamamlandı, {failedFiles.Count} başarısız. Başlanmayan dosyalar yeniden deneme listesine eklenmedi.";}
        catch(Exception ex){Status.Text="İşlem durdu: "+ex.Message+" Tamamlanan çıktılar korundu.";}
        finally
        {
            if(temp is not null)try{File.Delete(temp);}catch(IOException){}
            cancellation=null;UpdateControls();
        }
    }
}



