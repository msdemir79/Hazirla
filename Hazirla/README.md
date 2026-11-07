# Hazırla
Windows üzerinde yerel görsel hazırlama uygulaması. .NET 8 WPF + SkiaSharp.

## Başlat
Baslat.cmd dosyasına çift tıkla. İlk kurulum .NET SDK ve NuGet bağlantısı gerektirir.
Sonraki görsel işlemleri internet bağlantısı kullanmaz.

## Kullanım
1. JPG, PNG veya WebP dosyalarını ekle ya da pencereye bırak.
2. İşlemi seç: hedef boyut, biçim dönüştürme veya tek PDF.
3. Hedef boyutta MB ondalıktır: 1 MB = 1.000.000 bayt; sınır dosya başınadır.
4. PDF için yukarı/aşağı düğmeleriyle sıralama yap.
5. Hazırla ve kaydet ile sonuç klasörünü seç.

Çıktılar ayrı, benzersiz bir alt klasöre kaydedilir. Orijinaller değiştirilmez.
Hedef boyuta ulaşmak için önce kalite, ardından piksel boyutları düşürülür.
PNG kalite kaydırıcısı yerine boyut küçültme kullanır; hedefe ulaşmak belirgin çözünürlük kaybı oluşturabilir.
PDF A4 dikey sayfalarda, her görsel bir sayfa olacak şekilde hazırlanır; boyut sınırı uygulanmaz.
JPG saydamlığı beyaza dönüştürür. EXIF yönü uygulanır; çıktı orijinal metadata'yı taşımaz.
40 megapiksel sınırı vardır. Hareketli görseller desteklenmez. HEIC ve mevcut PDF dosyaları desteklenmez.
İptalde o ana kadar tamamlanan çıktılar korunur; devam ettirme yoktur.

## Doğrulama
dotnet run --project ..\Hazirla.Checks\Hazirla.Checks.csproj

## Önce / sonra
Listeden bir görsel seçip Seçileni önizle düğmesine bas. İşlem ayarlarıyla gerçek çıktı üretilir; Önce / Sonra penceresinde bu sonuç gösterilir. Bu sonucu kaydet aynı üretilen dosyayı kaydeder. PDF önizlemesi bu sürümde yoktur.
Dosya bazında önceki/sonraki boyut, küçülme veya büyüme oranı ve çıktı piksel boyutu gösterilir.

## Kaynak kullanımı
Görseller seri işlenir; dosya listesi tam çözünürlüklü görselleri tutmaz.
Önizleme uzun kenarı en fazla 480 piksel; iki görüntü yaklaşık 1,8 MiB ham piksel alanı gerektirir (arayüz ek yükü hariç).
Önizlenen tam çıktı geçici dosyadadır ve pencere kapatılınca silinir. Süreç zorla sonlanırsa geçici dosya kalabilir.
Normal yöndeki görselde gereksiz yön kopyası, aynı boyutta gereksiz yeniden boyutlandırma ve PNG/WebP için düzleştirme tamponu kaldırıldı.
JPEG için yalnız saydamlık varsa beyaz arka plan tamponu oluşturulur.
Kaynak görselin tam çözünürlükte çözülmesi hâlâ gerekir; sabit RAM garantisi yoktur.
Örnek doğrulama: 3 ardışık düz renkli 4000x3000 JPG → WebP işlemi; test süreci tepe working set 188,4 MiB, sonunda 36,1 MiB. Bu ölçüm WPF arayüzünü içermez ve gerçek fotoğraf/40 MP performansını temsil etmez.

## Arayüz ve sistem tepsisi
Dosyalar solda, seçilen işleme ait ayarlar sağda, önizleme ve sonuçlar alttadır.
PDF modunda biçim/boyut alanları gizlenir. İşlem sırasında dosya listesi ve ayarlar kilitlenir.
Pencerenin X düğmesi uygulamayı sistem tepsisine gizler. H ikonuna çift tıklayarak açabilirsin.
Tam kapatma: tepsi ikonuna sağ tık → Çıkış. Devam eden işlem varsa önce iptal et veya bitmesini bekle.
Arayüz yerel WPF kontrolleridir; animasyon, bulanıklık ve ek arayüz kütüphanesi kullanılmaz.

## Toplu işlemde hatalar
Bozuk, kayıp veya okunamayan dosyalar başarısız olarak listelenir; sonraki dosyaya devam edilir.
Sonuçlar dosya başına tamamlandı/başarısız ve hata açıklamasını gösterir.
Başarısızları yeniden dene son çalışmadaki başarısız dosyaları aynı işlem ayarlarıyla yeni sonuç klasörüne işler.
PDF yeniden denemesinde yalnız başarısız görsellerden ayrı bir PDF üretilir; önceki PDF birleştirilmez.
İptalde henüz başlanmayan dosyalar başarısız sayılmaz ve yeniden deneme listesine eklenmez.
PDF yazma gibi genel bir çıktı hatası tüm PDF işlemini durdurur; dosya okuma hataları atlanır.

## Kompakt düzen
Varsayılan pencere 820×620. Sonuç ayrıntıları kapalı başlar; Sonuçlar başlığından açılır.
Yeniden deneme düğmesi yalnız başarısız dosya varsa görünür.

## Hatırlanan ayarlar
İşlem türü, çıktı biçimi, son geçerli MB hedefi ve son seçilen kayıt klasörü yerel olarak hatırlanır.
Ayar dosyası: %LOCALAPPDATA%\Hazirla\settings.json.
Dosya listesi ve görsel içerikleri ayar dosyasına kaydedilmez.
Kayıt klasörü artık yoksa Resimler klasörü kullanılır; bozuk ayarlarda varsayılan değerlere dönülür.
Ayar kaydı değişikliklerde yapılır; arka plan zamanlayıcısı veya ek işlem yoktur.

## 0.1.0 dağıtım
Kurulum dosyası: dist\Hazirla-0.1.0-win-x64-Setup.exe
Taşınabilir paket: dist\Hazirla-0.1.0-win-x64-portable.zip
ZIP içindeki klasörü tamamen çıkar ve Hazirla.exe dosyasını aç.
Aynı Windows kullanıcısı/oturumunda ikinci açılış mevcut örneğe haber verir.
Yükleyici ve kaldırma akışı test klasöründe doğrulandı; ayrıntılar packaging/TEST-RESULTS.md dosyasında.

