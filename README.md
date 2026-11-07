# Hazırla

Windows için görselleri yerel olarak hazırlayan hafif WPF uygulaması.

- JPG, PNG ve WebP dönüşümü.
- Dosya başına hedef boyuta küçültme.
- Fotoğrafları sıralayarak tek PDF oluşturma.
- Önce/sonra önizlemesi ve boyut karşılaştırması.
- Hatalı dosyaları atlayan toplu işlem ve yeniden deneme.
- Sistem tepsisi ve son kullanılan ayarları hatırlama.

## Geliştirme
Windows ve .NET 8 SDK gerekir.

```powershell
dotnet run --project Hazirla/Hazirla.csproj
```

## Kontroller
```powershell
dotnet run --project Hazirla.Checks/Hazirla.Checks.csproj
```

## Dağıtım
Windows x64 için .NET dosyalarını içeren paket ve Inno Setup yükleyici üretimi: packaging/Build-Release.ps1.
Inno Setup derleyicisinin yolu parametreyle verilebilir. Lisans koşulları ticari kullanım öncesinde değerlendirilmelidir.

## Sınırlar
Hedef boyuta ulaşmak için görüntü kalitesi ve çözünürlük azaltılabilir.
En fazla 40 megapiksel; hareketli görsel ve HEIC desteği yoktur.
PDF modu fotoğraflardan yeni PDF üretir; mevcut PDF dosyalarını düzenlemez.
Büyük görsellerin işlenmesi sırasında bellek tüketimi artar; sabit RAM garantisi yoktur.

Görseller buluta gönderilmez. Orijinal dosyalar korunur.
