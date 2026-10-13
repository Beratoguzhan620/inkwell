using BlogApp.Core.Security;
using BlogApp.Data.Constants;
using BlogApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Data.Seeders;

/// <summary>
/// Sample authors, posts and comments for local development and demos. Runs only on an
/// empty posts table and only when enabled by configuration (Seed:DemoContent).
/// </summary>
public static class DemoContentSeeder
{
    public const string DemoPassword = "Demo123!";

    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.BlogPosts.AnyAsync())
            return;

        var categories = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
        if (categories.Count == 0)
            return;

        var authors = new[]
        {
            CreateUser("Elif", "Demir", "elif@example.com"),
            CreateUser("Kerem", "Aksoy", "kerem@example.com"),
            CreateUser("Zeynep", "Arslan", "zeynep@example.com"),
        };

        foreach (var author in authors)
        {
            if (await context.Users.AnyAsync(u => u.Email == author.Email))
                return;
        }

        context.Users.AddRange(authors);
        await context.SaveChangesAsync();

        context.UserRoles.AddRange(authors.Select(a => new UserRole { UserId = a.Id, RoleId = RoleConstants.VerifiedUserId }));

        var start = new DateTime(2026, 6, 2, 8, 30, 0, DateTimeKind.Utc);
        var posts = Posts.Select((p, i) => new BlogPost
        {
            Title = p.Title,
            Slug = p.Slug,
            Excerpt = p.Excerpt,
            Content = p.Content,
            CategoryId = categories.TryGetValue(p.Category, out var id) ? id : categories.Values.First(),
            AuthorId = authors[p.Author].Id,
            Status = p.Draft ? BlogPostStatus.Draft : BlogPostStatus.Approved,
            CreatedAt = start.AddDays(i * 6),
            PublishedAt = p.Draft ? null : start.AddDays(i * 6).AddHours(2),
        }).ToList();

        context.BlogPosts.AddRange(posts);
        await context.SaveChangesAsync();

        var comments = new (int Post, int Author, string Text)[]
        {
            (0, 1, "Ekipte tam olarak bu tartışmayı yapıyorduk. Küçük PR kuralını deneyeceğiz."),
            (0, 2, "Gözden geçirme süresini ölçmek iyi fikir; biz ortalamanın iki güne çıktığını fark etmemiştik."),
            (1, 0, "Index'i eklemeden önce sorgu planına bakmak gerçekten çok şey değiştiriyor."),
            (2, 1, "Grafik çok net anlatmış, teşekkürler."),
            (4, 0, "Erişilebilirlik kontrol listesini ekibimizle paylaştım."),
            (4, 2, "Klavye ile gezinme maddesi en çok atlanan kısım bence."),
        };

        context.Comments.AddRange(comments.Select((c, i) => new Comment
        {
            PostId = posts[c.Post].Id,
            UserId = authors[c.Author].Id,
            Content = c.Text,
            CreatedAt = posts[c.Post].PublishedAt!.Value.AddHours(5 + i),
        }));

        await context.SaveChangesAsync();
    }

    private static User CreateUser(string firstName, string lastName, string email)
    {
        var hash = HashingHelper.CreatePasswordHash(DemoPassword);
        return new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = hash.Hash,
            PasswordSalt = hash.Salt,
            IsActive = true,
            IsEmailVerified = true,
        };
    }

    private sealed record DemoPost(string Title, string Slug, string Category, int Author, string Excerpt, string Content, bool Draft = false);

    private static readonly DemoPost[] Posts =
    [
        new(
            "Küçük pull request'ler neden daha hızlı birleşir?",
            "kucuk-pull-requestler-neden-daha-hizli-birlesir",
            "Yazılım",
            0,
            "Ekibimizde ortalama inceleme süresi iki güne çıkınca PR boyutlarına baktık. Değişiklikleri küçültmek hem incelemeyi hem de hataları bulmayı kolaylaştırdı.",
            """
            Ekibimizde ortalama inceleme süresi iki güne çıkınca PR boyutlarına baktık. Değişiklikleri küçültmek hem incelemeyi hem de hataları bulmayı kolaylaştırdı.

            ## Ne ölçtük?

            Son üç ayın birleşen PR'larını değişen satır sayısına göre gruplandırdık:

            | Boyut | Ortalama bekleme | Geri alınan değişiklik |
            |---|---|---|
            | 0–100 satır | 4 saat | %1 |
            | 100–400 satır | 19 saat | %3 |
            | 400+ satır | 2,3 gün | %9 |

            Büyük PR'lar yalnızca daha geç birleşmiyor, daha sık da geri alınıyor. İnceleyen kişi 600 satırlık bir farkı dikkatle okuyamıyor; göz gezdirip onaylıyor.

            ## Neyi değiştirdik?

            - Bir PR tek bir mantıksal değişiklik içeriyor. Yeniden adlandırma ile davranış değişikliği aynı PR'a girmiyor.
            - Yarım kalan özellikler **özellik bayrağı** arkasında birleşiyor.
            - Açıklamada "nasıl test edilir" bölümü zorunlu.

            > İnceleme, kodun doğruluğundan çok ekibin ortak anlayışı için yapılır.

            İki ay sonra ortalama bekleme süresi 6 saate indi. En büyük kazanç ise toplantılarda "bu değişiklik ne yapıyordu?" sorusunun azalması oldu.
            """),
        new(
            "Yavaş sorguyu bulmak: EF Core ile bir haftalık iz sürme",
            "yavas-sorguyu-bulmak-ef-core-ile-bir-haftalik-iz-surme",
            "Yazılım",
            1,
            "Ürün listesi sayfası bazen 3 saniyede açılıyordu. Sorun tek bir sorgu değil, sayfa başına çalışan 41 sorguydu.",
            """
            Ürün listesi sayfası bazen 3 saniyede açılıyordu. Sorun tek bir sorgu değil, sayfa başına çalışan 41 sorguydu.

            ## Belirti

            Loglarda tek tek bakınca her sorgu hızlıydı: 10–20 ms. Ama sayfa yüklemesi boyunca aynı sorgu farklı parametrelerle defalarca çalışıyordu. Klasik **N+1** problemi.

            ```csharp
            // Her ürün için kategori ayrı sorguyla yükleniyordu
            foreach (var product in products)
                names.Add(product.Category.Name);
            ```

            ## Çözüm

            Listeyi doğrudan ihtiyaç duyulan alanlara izdüşürdük:

            ```csharp
            var rows = await db.Products
                .Select(p => new { p.Name, Category = p.Category.Name })
                .ToListAsync();
            ```

            Sorgu sayısı 41'den 2'ye, sayfa süresi 3 saniyeden 180 ms'ye indi. Ardından `CategoryId` için eksik olan index'i ekledik.

            ## Ders

            Tek tek hızlı görünen sorgular toplamda yavaş olabilir. Ölçerken sorgu süresine değil, **istek başına sorgu sayısına** da bakın.
            """),
        new(
            "Gece gökyüzünde neden bu kadar az yıldız görüyoruz?",
            "gece-gokyuzunde-neden-bu-kadar-az-yildiz-goruyoruz",
            "Bilim",
            2,
            "Karanlık bir köyde binlerce yıldız görülebilirken şehirde birkaç düzineyi zor sayarız. Işık kirliliği hem gözlemi hem de canlıların ritmini etkiliyor.",
            """
            Karanlık bir köyde binlerce yıldız görülebilirken şehirde birkaç düzineyi zor sayarız. Işık kirliliği hem gözlemi hem de canlıların ritmini etkiliyor.

            ## Gökyüzü parlaklığı

            Şehir ışıklarının bir kısmı doğrudan yukarı, bir kısmı da yerden yansıyarak atmosfere ulaşır. Atmosferdeki moleküller ve toz bu ışığı her yöne saçar. Sonuç, gökyüzünün arka planının parlaması ve sönük yıldızların kaybolmasıdır.

            ## Sadece gökbilimcilerin sorunu değil

            - Göçmen kuşlar yönlerini şaşırabiliyor.
            - Deniz kaplumbağası yavruları denize değil, ışığa doğru ilerliyor.
            - İnsanlarda melatonin salgısı gece ışığından etkileniyor.

            ## Ne yapılabilir?

            Aşağı doğru yönlendirilmiş, sıcak renkli ve gerektiğinde kısılan aydınlatma; hem enerji tasarrufu sağlıyor hem de gökyüzünü geri kazandırıyor. Bazı şehirler "karanlık gökyüzü parkı" ilan ederek bu alanları koruma altına aldı.
            """),
        new(
            "Ev ağınızı 20 dakikada daha güvenli hale getirin",
            "ev-aginizi-20-dakikada-daha-guvenli-hale-getirin",
            "Teknoloji",
            1,
            "Modemin varsayılan şifresi, güncellenmeyen yazılım ve tanımadığınız cihazlar. Birkaç ayarla ev ağınızı çok daha zor hedef yapabilirsiniz.",
            """
            Modemin varsayılan şifresi, güncellenmeyen yazılım ve tanımadığınız cihazlar. Birkaç ayarla ev ağınızı çok daha zor hedef yapabilirsiniz.

            1. **Yönetim şifresini değiştirin.** Modemin arayüzüne giriş şifresi, Wi-Fi şifresinden farklı olmalı.
            2. **Yazılımı güncelleyin.** Çoğu modemde otomatik güncelleme seçeneği kapalı gelir.
            3. **WPA3 veya en az WPA2 kullanın.** WEP ve WPS'i kapatın.
            4. **Misafir ağı açın.** Akıllı prizler ve misafirler ana ağınıza değil, ayrı ağa bağlansın.
            5. **Bağlı cihaz listesine bakın.** Tanımadığınız bir cihaz görüyorsanız şifreyi değiştirin.

            Bu adımların hiçbiri teknik bilgi gerektirmiyor; modemin arayüzünde genellikle "Kablosuz" ve "Yönetim" menülerinde bulunuyor.
            """),
        new(
            "Erişilebilir formlar için kısa bir kontrol listesi",
            "erisilebilir-formlar-icin-kisa-bir-kontrol-listesi",
            "Yazılım",
            0,
            "Form, kullanıcının sitenizle en çok uğraştığı yerdir. Etiketler, hata mesajları ve klavye ile kullanım küçük ama belirleyici ayrıntılar.",
            """
            Form, kullanıcının sitenizle en çok uğraştığı yerdir. Etiketler, hata mesajları ve klavye ile kullanım küçük ama belirleyici ayrıntılar.

            ## Etiketler

            Her alanın görünür bir `<label>` etiketi olmalı. Placeholder etiketin yerini tutmaz: yazmaya başlayınca kaybolur ve çoğu zaman düşük kontrastlıdır.

            ## Hata mesajları

            - Hata, ilgili alanın hemen altında gösterilmeli.
            - Mesaj ne yapılması gerektiğini söylemeli: "Geçersiz" yerine "E-posta adresi @ işareti içermeli".
            - Renk tek başına bilgi taşımamalı; ikon veya metin de eklenmeli.

            ## Klavye

            Formu fareye dokunmadan doldurup gönderebiliyor musunuz? Odak sırası mantıklı mı, odaklanan alan görünür mü? Bu iki soru, hataların büyük kısmını yakalar.
            """),
        new(
            "Bakteriler antibiyotiğe nasıl direnç kazanıyor?",
            "bakteriler-antibiyotige-nasil-direnc-kazaniyor",
            "Bilim",
            2,
            "Direnç, bakterinin \"alışması\" değil; ilacın hayatta kalan birkaç bireyi seçmesidir. Tedaviyi yarıda bırakmak bu seçilimi hızlandırır.",
            """
            Direnç, bakterinin "alışması" değil; ilacın hayatta kalan birkaç bireyi seçmesidir. Tedaviyi yarıda bırakmak bu seçilimi hızlandırır.

            ## Seçilim

            Milyonlarca bakteriden oluşan bir toplulukta rastgele mutasyonlar sürekli ortaya çıkar. Antibiyotik geldiğinde duyarlı olanlar ölür, mutasyonu sayesinde dayanıklı olan birkaç tanesi hayatta kalır ve çoğalır.

            ## Gen paylaşımı

            Bakteriler direnç genlerini **plazmit** adı verilen küçük DNA halkalarıyla birbirine aktarabilir. Bu yüzden direnç, farklı türler arasında bile yayılabilir.

            ## Bireysel olarak ne yapabiliriz?

            - Antibiyotiği yalnızca hekim önerdiğinde kullanmak.
            - Önerilen süreyi tamamlamak.
            - Viral enfeksiyonlarda (grip, soğuk algınlığı) antibiyotik beklememek.
            """),
        new(
            "Not alma uygulamamı neden üç kez değiştirdim",
            "not-alma-uygulamami-neden-uc-kez-degistirdim",
            "Teknoloji",
            0,
            "Taslak: Araç değiştirmek üretkenlik gibi hissettiriyor ama asıl sorun notları hiç tekrar okumamaktı.",
            """
            Taslak: Araç değiştirmek üretkenlik gibi hissettiriyor ama asıl sorun notları hiç tekrar okumamaktı.

            Haftalık gözden geçirme alışkanlığı üzerine bir bölüm eklenecek.
            """,
            Draft: true),
    ];
}
