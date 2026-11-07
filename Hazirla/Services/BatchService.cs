using System.IO;
using SkiaSharp;
namespace Hazirla.Services;
public sealed record BatchItem(string Path, string? Output, string? Error, string? Summary);
public sealed class BatchService
{
    public async Task RunAsync(string[] paths,string folder,SKEncodedImageFormat format,long? limit,Action<BatchItem> report,CancellationToken ct)
    {
        var service=new ImageService();
        string extension=format==SKEncodedImageFormat.Png?"png":format==SKEncodedImageFormat.Webp?"webp":"jpg";
        for(int i=0;i<paths.Length;i++)
        {
            ct.ThrowIfCancellationRequested();
            string? temp=null;
            try
            {
                long before=new FileInfo(paths[i]).Length;
                var result=await Task.Run(()=>service.Convert(paths[i],format,limit,ct),ct);
                ct.ThrowIfCancellationRequested();
                var target=Path.Combine(folder,$"{i+1:D3}_{Path.GetFileNameWithoutExtension(paths[i])}.{extension}");
                temp=target+".part";
                await File.WriteAllBytesAsync(temp,result.Bytes,ct);
                File.Move(temp,target);temp=null;
                double change=before==0?0:100.0*(before-result.Bytes.LongLength)/before;
                report(new(paths[i],target,null,$"{before/1_000_000.0:F2} MB → {result.Bytes.LongLength/1_000_000.0:F2} MB · %{Math.Abs(change):F1} {(change>=0?"küçüldü":"büyüdü")} · {result.Width}×{result.Height}"));
            }
            catch(OperationCanceledException){throw;}
            catch(Exception ex) when(ex is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException)
            { report(new(paths[i],null,ex.Message,null)); }
            finally
            {
                if(temp is not null)try{File.Delete(temp);}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) { }
            }
        }
    }
}

