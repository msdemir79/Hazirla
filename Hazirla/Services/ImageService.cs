using System.IO;
using SkiaSharp;
namespace Hazirla.Services;
public sealed record ImageResult(byte[] Bytes, int Width, int Height, int Quality);
public sealed class ImageService
{
    // Yön bilgisi uygulanır; hareketli görseller sessizce tek kareye çevrilmez.
    private static SKBitmap Load(string path, int? maxSide = null)
    {
        using var stream = File.OpenRead(path);
        using var codec = SKCodec.Create(stream) ?? throw new InvalidDataException("Görsel okunamadı.");
        if (codec.FrameCount > 1) throw new InvalidDataException("Hareketli görseller desteklenmiyor.");
        var info = codec.Info;
        if ((long)info.Width * info.Height > 40_000_000) throw new InvalidDataException("En fazla 40 megapiksel destekleniyor.");
                var dimensions = maxSide is int side ? codec.GetScaledDimensions(Math.Min(1f, (float)side / Math.Max(info.Width,info.Height))) : new SKSizeI(info.Width,info.Height);
        var raw = new SKBitmap(new SKImageInfo(dimensions.Width,dimensions.Height,SKColorType.Bgra8888,info.AlphaType == SKAlphaType.Opaque ? SKAlphaType.Opaque : SKAlphaType.Premul));
        if (codec.GetPixels(raw.Info,raw.GetPixels()) != SKCodecResult.Success) { raw.Dispose(); throw new InvalidDataException("Görsel çözümlenemedi."); }
        if (codec.EncodedOrigin == SKEncodedOrigin.TopLeft) return raw;
        using var rawOwner = raw;
        var origin = codec.EncodedOrigin;
        bool swap = (int)origin >= 5;
        var bitmap = new SKBitmap(new SKImageInfo(swap ? raw.Height : raw.Width, swap ? raw.Width : raw.Height, SKColorType.Bgra8888, raw.AlphaType));
        using var canvas = new SKCanvas(bitmap);
        switch (origin)
        {
            case SKEncodedOrigin.TopRight: canvas.Translate(raw.Width,0); canvas.Scale(-1,1); break;
            case SKEncodedOrigin.BottomRight: canvas.Translate(raw.Width,raw.Height); canvas.RotateDegrees(180); break;
            case SKEncodedOrigin.BottomLeft: canvas.Translate(0,raw.Height); canvas.Scale(1,-1); break;
            case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(1,-1); break;
            case SKEncodedOrigin.RightTop: canvas.Translate(raw.Height,0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom: canvas.Translate(raw.Height,raw.Width); canvas.RotateDegrees(90); canvas.Scale(-1,1); break;
            case SKEncodedOrigin.LeftBottom: canvas.Translate(0,raw.Width); canvas.RotateDegrees(270); break;
        }
        canvas.DrawBitmap(raw,0,0); return bitmap;
    }
    private static SKData EncodeData(SKBitmap bitmap, SKEncodedImageFormat format, int quality)
    {
        // JPG saydamlığı desteklemez; siyah arka plan yerine beyaza birleştir.
        using var flattened = format == SKEncodedImageFormat.Jpeg && bitmap.AlphaType != SKAlphaType.Opaque ? new SKBitmap(bitmap.Width,bitmap.Height) : null;
        if (flattened is not null) using (var canvas = new SKCanvas(flattened)) { canvas.Clear(format == SKEncodedImageFormat.Jpeg ? SKColors.White : SKColors.Transparent); canvas.DrawBitmap(bitmap,0,0); }
        using var image = SKImage.FromBitmap(flattened ?? bitmap);
        return image.Encode(format,quality) ?? throw new IOException("Görsel kodlanamadı.");
    }
    public ImageResult Convert(string path, SKEncodedImageFormat format, long? limit, CancellationToken ct)
    {
        using var source = Load(path);
        int width=source.Width, height=source.Height;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            using var resized = width == source.Width && height == source.Height ? null : source.Resize(new SKImageInfo(width,height), SKFilterQuality.High) ?? throw new IOException("Boyutlandırılamadı.");
            var scaled = resized ?? source;
            foreach (int quality in format == SKEncodedImageFormat.Png ? new[]{100} : new[]{95,85,75,65,55,45,35})
            {
                ct.ThrowIfCancellationRequested();
                using var encoded=EncodeData(scaled,format,quality); if (limit is null || encoded.Size <= limit) return new(encoded.ToArray(),width,height,quality);
            }
            if (width == 1 && height == 1) throw new IOException("Hedef boyuta ulaşılamadı.");
            width=Math.Max(1,(int)(width*0.8)); height=Math.Max(1,(int)(height*0.8));
        }
    }
        public byte[] Thumbnail(string path)
    {
        using var decoded=Load(path,480);
        var ratio=Math.Min(1.0,480.0/Math.Max(decoded.Width,decoded.Height));
        using var small=decoded.Resize(new SKImageInfo(Math.Max(1,(int)(decoded.Width*ratio)),Math.Max(1,(int)(decoded.Height*ratio))),SKFilterQuality.Medium);
        using var encoded=EncodeData(small ?? decoded,SKEncodedImageFormat.Png,100);
        return encoded.ToArray();
    }
    public void CreatePdf(IEnumerable<string> paths, string output, CancellationToken ct, Action<string,Exception?>? report = null)
    {
        using var stream=File.Create(output);
        using var document=SKDocument.CreatePdf(stream) ?? throw new IOException("PDF oluşturulamadı.");
        foreach (var path in paths)
        {
            ct.ThrowIfCancellationRequested();
                        SKBitmap loaded;
            try { loaded=Load(path); }
            catch(Exception ex) when(ex is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException)
            { if(report is null)throw; report(path,ex);continue; }
            using var bitmap=loaded;
            const float pageWidth=595, pageHeight=842, margin=24;
            var scale=Math.Min((pageWidth-margin*2)/bitmap.Width,(pageHeight-margin*2)/bitmap.Height);
            float w=bitmap.Width*scale,h=bitmap.Height*scale;
            var canvas=document.BeginPage(pageWidth,pageHeight);
            canvas.Clear(SKColors.White);
            using var paint=new SKPaint { FilterQuality=SKFilterQuality.High, IsAntialias=true };
            canvas.DrawBitmap(bitmap,new SKRect((pageWidth-w)/2,(pageHeight-h)/2,(pageWidth+w)/2,(pageHeight+h)/2),paint);
            document.EndPage(); report?.Invoke(path,null);
        }
        document.Close();
    }
}







