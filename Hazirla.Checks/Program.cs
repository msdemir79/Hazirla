using Hazirla.Services;
using SkiaSharp;
var folder=Path.Combine(Path.GetTempPath(),"Hazirla-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
var source=Path.Combine(folder,"sample.png");
using(var bitmap=new SKBitmap(1200,800))
{
 using var canvas=new SKCanvas(bitmap);canvas.Clear(SKColors.Transparent);
 using var paint=new SKPaint {Color=SKColors.Blue};canvas.DrawCircle(600,400,300,paint);
 using var image=SKImage.FromBitmap(bitmap);using var data=image.Encode(SKEncodedImageFormat.Png,100);File.WriteAllBytes(source,data.ToArray());
}
var service=new ImageService();
foreach(var format in new[]{SKEncodedImageFormat.Jpeg,SKEncodedImageFormat.Png,SKEncodedImageFormat.Webp})
{
 var result=service.Convert(source,format,5000,CancellationToken.None);
 if(result.Bytes.Length>5000)throw new Exception("Boyut sınırı aşıldı");
 using var decoded=SKBitmap.Decode(result.Bytes);if(decoded is null)throw new Exception("Çıktı okunamadı");
 Console.WriteLine($"OK {format}: {result.Bytes.Length} bayt, {decoded.Width}x{decoded.Height}");
}
service.CreatePdf(new[]{source,source},Path.Combine(folder,"result.pdf"),CancellationToken.None);
if(!File.ReadAllText(Path.Combine(folder,"result.pdf")).StartsWith("%PDF"))throw new Exception("PDF geçersiz");
Console.WriteLine("OK PDF üretimi");
using var stop=new CancellationTokenSource();stop.Cancel();
try{service.Convert(source,SKEncodedImageFormat.Jpeg,1000,stop.Token);throw new Exception("İptal çalışmadı");}catch(OperationCanceledException){Console.WriteLine("OK iptal");}
if(!File.Exists(source))throw new Exception("Orijinal kayboldu");
Console.WriteLine("OK orijinal korundu. Test çıktısı: "+folder);
var thumbnail=service.Thumbnail(source);
using(var small=SKBitmap.Decode(thumbnail)) { if(small.Width>480||small.Height>480)throw new Exception("Önizleme sınırı aşıldı");Console.WriteLine($"OK önizleme {small.Width}x{small.Height}"); }
var large=Path.Combine(folder,"12mp.jpg");
using(var bitmap=new SKBitmap(4000,3000))
{ using var canvas=new SKCanvas(bitmap);canvas.Clear(SKColors.Coral);using var image=SKImage.FromBitmap(bitmap);using var data=image.Encode(SKEncodedImageFormat.Jpeg,95);File.WriteAllBytes(large,data.ToArray()); }
for(int i=0;i<3;i++){var result=service.Convert(large,SKEncodedImageFormat.Webp,200000,CancellationToken.None);if(result.Bytes.Length>200000)throw new Exception("Boyut hatası");}
using(var process=System.Diagnostics.Process.GetCurrentProcess()){process.Refresh();Console.WriteLine($"12 MP, 3 ardışık işlem: tepe çalışma belleği {process.PeakWorkingSet64/1048576.0:F1} MiB; mevcut {process.WorkingSet64/1048576.0:F1} MiB");}
var broken=Path.Combine(folder,"broken.jpg");File.WriteAllText(broken,"not an image");
var batchFolder=Path.Combine(folder,"batch");Directory.CreateDirectory(batchFolder);
var outcomes=new List<BatchItem>();
await new BatchService().RunAsync(new[]{source,broken,source},batchFolder,SKEncodedImageFormat.Jpeg,5000,outcomes.Add,CancellationToken.None);
if(outcomes.Count!=3||outcomes.Count(x=>x.Error is null)!=2||outcomes.Count(x=>x.Error is not null)!=1)throw new Exception("Kuyruk hatadan sonra devam etmedi");
if(Directory.GetFiles(batchFolder,"*.part").Length!=0)throw new Exception("Yarım dosya kaldı");
Console.WriteLine("OK hatalı dosya sonrası devam: 2 tamamlandı, 1 başarısız");
var pdfReports=new List<(string,Exception?)>();
service.CreatePdf(new[]{source,broken,source},Path.Combine(folder,"partial.pdf"),CancellationToken.None,(p,e)=>pdfReports.Add((p,e)));
if(pdfReports.Count!=3||pdfReports.Count(x=>x.Item2!=null)!=1)throw new Exception("PDF hata atlama çalışmadı");
Console.WriteLine("OK PDF hatalı görsel atlama");
var retryFolder=Path.Combine(folder,"retry");Directory.CreateDirectory(retryFolder);
var retryPaths=outcomes.Where(x=>x.Error is not null).Select(x=>x.Path).ToArray();File.Copy(source,broken,true);
var retryOutcomes=new List<BatchItem>();
await new BatchService().RunAsync(retryPaths,retryFolder,SKEncodedImageFormat.Jpeg,5000,retryOutcomes.Add,CancellationToken.None);
if(retryOutcomes.Count!=1||retryOutcomes[0].Error is not null)throw new Exception("Yeniden deneme başarısız");
Console.WriteLine("OK yalnız başarısız dosya yeniden denendi");
var settingsPath=Path.Combine(folder,"settings","settings.json");
var store=new SettingsStore(settingsPath);
if(store.Load()!=new UserSettings())throw new Exception("Varsayılan ayar hatası");
if(!store.Save(new(1,2,"1,5",folder)))throw new Exception("Ayar yazılamadı");
var saved=store.Load();if(saved.Mode!=1||saved.Format!=2||saved.Target!="1,5"||saved.Folder!=folder)throw new Exception("Ayarlar geri yüklenmedi");
File.WriteAllText(settingsPath,"broken json");if(store.Load()!=new UserSettings())throw new Exception("Bozuk ayar kurtarılamadı");
var normalized=SettingsStore.Normalize(new(99,-1,"NaN",Path.Combine(folder,"missing")));
if(normalized!=new UserSettings())throw new Exception("Ayar doğrulama hatası");
Console.WriteLine("OK ayar kaydetme, geri yükleme, bozuk JSON ve eksik klasör");
