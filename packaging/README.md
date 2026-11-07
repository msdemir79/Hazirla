# Hazırla 0.1.0 dağıtım

Build-Release.ps1 Windows x64 bağımsız paket, taşınabilir ZIP ve Türkçe Inno Setup yükleyicisi üretir.
Önkoşul: .NET SDK ve Inno Setup 6.7.4. Derleyici yolu parametreyle değiştirilebilir.
Inno Setup lisans koşulları ticari dağıtım öncesinde değerlendirilmelidir; mevcut araç non-commercial modda çalışır.
Üretilen uygulama/yükleyici henüz kod imzalı değildir.
Tek örnek kilidi ve iletişim kanalı kullanıcıya ve Windows oturumuna özeldir.
Kanal yalnız aynı kullanıcıya açıktır; bekleme asenkron, sürekli sorgulama yoktur.
Geliştirme sürümleri dahil yeni sürümler aynı oturumda tek örnek paylaşır. Önceki tek-örnek desteği olmayan sürümleri testten önce kapat.
Kurulum mevcut kullanıcıya yapılır; yönetici izni istemez.
Başlat menüsü kısayolu her zaman, masaüstü kısayolu isteğe bağlı oluşturulur.
Kaldırma uygulama dosyalarını ve kurulumun oluşturduğu kısayolları kaldırır; kullanıcı ayarlarını ve çıktı görsellerini silmez.
