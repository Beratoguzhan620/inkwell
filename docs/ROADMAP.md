# Inkwell — Yol Haritası

E-posta doğrulamalı üyelik, kategori ve yazar paneli olan çok katmanlı blog platformu.

- **Önerilen repo adı:** `inkwell`
- **Teknoloji:** ASP.NET Core MVC (.NET 10), EF Core, SQL Server, Bootstrap 5 + özel SCSS, xUnit
- **Kurallar:** `CONTRIBUTING.md` · **Tasarım:** `docs/DESIGN.md`

Her aşama ayrı bir branch ve PR olarak yapılır (`stage/<no>-<kisa-ad>`). Bir aşama ancak tüm maddeleri bitince, build/testler geçince ve ekran görüntüleri eklenince tamamlanmış sayılır. Biten maddeler `[x]` yapılır.

## Aşama 1 — Güvenlik ve temizlik
- [x] `appsettings.Development.json` içindeki SMTP şifresi kaldırılır → user-secrets; `appsettings.Development.example.json` eklenir
- [x] Proje adı/marka "Inkwell"; tasarım token'ları (`docs/DESIGN.md`) ile ortak CSS
- [x] Yeni layout: okuma odaklı, geniş satır aralığı, serif başlık (Source Serif) + Plex Sans gövde

## Aşama 2 — Yazı akışının tamamlanması
- [x] Yazı oluşturma: `CreateBlogPost` bağlanır, `authorId` claim'den gelir; kategori dropdown'u DB'den dolar
- [x] Markdown editörü (EasyMDE vb.) + kapak görseli yükleme
- [x] Yazı listesi (sayfalama, kategori filtresi, arama), yazı detayı (okuma süresi, yazar kartı)
- [x] Yorumlar (giriş yapmış kullanıcı), taslak/yayınla durumu, slug'lı URL
- [x] Generic Repository TODO'su: ya uygulanır ya da gerekçesiyle kapatılır

## Aşama 3 — Admin paneli ve hesap akışları
- [x] `BlogApp.Admin`: dashboard, yazılar, kategoriler, kullanıcılar, yorum moderasyonu
- [x] Rol bazlı yetki (Admin / Author / Reader)
- [x] Şifre sıfırlama (`VerificationCode.PasswordReset`) uçtan uca
- [x] Doğrulama e-postası gerçekten gönderilir (TODO kapanır)

## Aşama 4 — Test, yayın, README
- [ ] Test projesi (AuthService, BlogPostService) + GitHub Actions
- [ ] Seed: 3 yazar, 20 yazı, kategoriler, yorumlar
- [ ] Canlı demo + demo hesapları; ekran görüntüleri; README (TR + EN)
- [ ] README'de "Proje geçmişi / Background" bölümü — TR: "Bu proje bir bootcamp projesi olarak başladı, sonra baştan geliştirildi." EN: "This project started as a bootcamp project and was later rebuilt from the ground up." Altına neyin yeniden yapıldığı kısaca maddelenir (tasarım, özellikler, testler, CI).
