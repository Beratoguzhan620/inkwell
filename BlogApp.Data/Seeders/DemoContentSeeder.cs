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

        foreach (var name in ExtraCategories)
        {
            if (!await context.Categories.AnyAsync(c => c.Name == name))
                context.Categories.Add(new Category { Name = name });
        }
        await context.SaveChangesAsync();

        var categories = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);

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

        context.UserRoles.AddRange(authors.Select(a => new UserRole { UserId = a.Id, RoleId = RoleConstants.AuthorId }));

        var reader = CreateUser("Mert", "Yıldız", "okur@example.com");
        context.Users.Add(reader);
        await context.SaveChangesAsync();
        context.UserRoles.Add(new UserRole { UserId = reader.Id, RoleId = RoleConstants.ReaderId });

        await DbSeeder.SeedAdminAsync(context, "admin@example.com", DemoPassword, "Selin", "Kara");

        var start = new DateTime(2026, 5, 4, 8, 30, 0, DateTimeKind.Utc);
        var posts = Posts.Select((p, i) => new BlogPost
        {
            Title = p.Title,
            Slug = p.Slug,
            Excerpt = p.Excerpt,
            Content = p.Content,
            CategoryId = categories.TryGetValue(p.Category, out var id) ? id : categories.Values.First(),
            AuthorId = authors[p.Author].Id,
            Status = p.Draft ? BlogPostStatus.Draft : BlogPostStatus.Approved,
            CreatedAt = start.AddDays(i * 7),
            PublishedAt = p.Draft ? null : start.AddDays(i * 7).AddHours(2),
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
            (6, 2, "Yüksek sesle düşünme kısmı çok doğru, ben de aynı geri bildirimi almıştım."),
            (7, 1, "Kontrast eklentisini tasarım ekibimize önerdim."),
            (8, 0, "Commit mesajı örneği tam aradığım şeydi."),
            (10, 2, "Etiket fikrini bu hafta ekipte deniyoruz."),
            (12, 2, ".dockerignore'u unutmuşuz, imaj boyutu bir anda yarıya indi."),
            (14, 1, "Gün sonu notu alışkanlığını ben de edindim, sabahları çok işe yarıyor."),
            (16, 0, "SQLite ile test fikri güzel; bizde de mock'lar gerçek hatayı gizliyordu."),
            (19, 2, "İlk haftamda bu yazıyı okumuş olmayı isterdim."),
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

    private static readonly string[] ExtraCategories = ["Kariyer", "Tasarım"];

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
            "İlk teknik mülakatımdan öğrendiklerim",
            "ilk-teknik-mulakatimdan-ogrendiklerim",
            "Kariyer",
            1,
            "Soruyu hemen çözmeye çalışmak yerine yüksek sesle düşünmek, doğru cevabı bulmaktan daha çok puan getirdi.",
            """
            Soruyu hemen çözmeye çalışmak yerine yüksek sesle düşünmek, doğru cevabı bulmaktan daha çok puan getirdi.

            ## Soruyu netleştirin

            İlk beş dakikayı sınır durumlarını sormaya ayırdım: boş liste gelebilir mi, sayılar negatif olabilir mi, girdi ne kadar büyük? Mülakatçı bu soruların çoğunu bekliyordu.

            ## Önce basit çözüm

            Kaba kuvvet çözümü yazıp karmaşıklığını söyledim, sonra iyileştirdim. Hiç çalışan kod yazmadan en iyi çözümü aramak, süre bitince elinizde hiçbir şey bırakmıyor.

            ## Takıldığınızda

            "Burada takıldım, şunu deniyorum" demek sessiz kalmaktan iyidir. Mülakat, birlikte çalışmanın küçük bir provası.
            """),
        new(
            "Renk seçerken kontrast oranını unutmayın",
            "renk-secerken-kontrast-oranini-unutmayin",
            "Tasarım",
            0,
            "Açık gri metin şık görünür ama güneşte telefon ekranında okunmaz. WCAG'in 4,5:1 oranı keyfi bir sayı değil.",
            """
            Açık gri metin şık görünür ama güneşte telefon ekranında okunmaz. WCAG'in 4,5:1 oranı keyfi bir sayı değil.

            ## Oran ne demek?

            Kontrast oranı, metin ve arka plan rengi arasındaki parlaklık farkıdır. Normal metin için en az **4,5:1**, büyük başlıklar için **3:1** önerilir.

            ## Pratik öneriler

            - Tasarım araçlarındaki kontrast eklentileriyle her renk çiftini kontrol edin.
            - İkincil metni gri yapmak yerine biraz küçültmek çoğu zaman yeterli.
            - Butonların devre dışı hali bile okunabilir olmalı.

            Kontrastı yüksek bir arayüz yalnızca görme sorunu olanlar için değil, herkes için daha rahat okunur.
            """),
        new(
            "Git geçmişini temiz tutmanın üç basit kuralı",
            "git-gecmisini-temiz-tutmanin-uc-basit-kurali",
            "Yazılım",
            2,
            "Altı ay sonra bir hatanın nereden geldiğini ararken iyi yazılmış commit mesajları en büyük yardımcınız olur.",
            """
            Altı ay sonra bir hatanın nereden geldiğini ararken iyi yazılmış commit mesajları en büyük yardımcınız olur.

            ## 1. Bir commit, bir değişiklik

            Biçimlendirme düzeltmesi ile yeni özelliği aynı commit'e koymayın. `git bisect` ancak commit'ler küçükse işe yarar.

            ## 2. Mesaj "ne"yi değil "neden"i anlatsın

            Kod zaten ne değiştiğini gösteriyor. Mesaj, değişikliğin nedenini anlatmalı:

            ```text
            fix(cart): keep the coupon when the quantity changes

            The coupon was cleared on every cart update, so customers had to enter it again.
            ```

            ## 3. Paylaşılmış geçmişi yeniden yazmayın

            Kendi branch'inizde istediğiniz kadar düzenleyin; `main`'e girdikten sonra geçmiş sabittir.
            """),
        new(
            "Elektrikli araç bataryaları soğukta neden zorlanır?",
            "elektrikli-arac-bataryalari-sogukta-neden-zorlanir",
            "Bilim",
            2,
            "Kışın menzilin yüzde yirmi ile otuz arasında düşmesi normaldir. Sebep, lityum iyonlarının soğukta yavaşlaması ve ısıtmanın enerji tüketmesi.",
            """
            Kışın menzilin yüzde yirmi ile otuz arasında düşmesi normaldir. Sebep, lityum iyonlarının soğukta yavaşlaması ve ısıtmanın enerji tüketmesi.

            ## Kimya tarafı

            Batarya içindeki elektrolit soğukta koyulaşır. İyonlar elektrotlar arasında daha yavaş hareket eder, iç direnç artar ve kullanılabilir enerji azalır.

            ## Isıtma tarafı

            Benzinli araçlar motorun atık ısısıyla kabini ısıtır. Elektrikli araçta bu ısı yoktur; ısıtma doğrudan bataryadan beslenir.

            ## Ne yapılabilir?

            - Aracı şarjdayken önceden ısıtmak.
            - Kabin yerine koltuk ve direksiyon ısıtmasını kullanmak.
            - Isı pompalı modelleri tercih etmek.
            """),
        new(
            "Kod incelemesinde nazik ama net olmak",
            "kod-incelemesinde-nazik-ama-net-olmak",
            "Kariyer",
            0,
            "\"Bu yanlış\" yerine \"Bu durumda liste boş gelirse ne olur?\" demek hem hatayı gösterir hem de kişiyi savunmaya geçirmez.",
            """
            "Bu yanlış" yerine "Bu durumda liste boş gelirse ne olur?" demek hem hatayı gösterir hem de kişiyi savunmaya geçirmez.

            ## Yorumları etiketleyin

            Ekibimizde yorumların başına kısa etiketler koyuyoruz:

            - **engel:** birleşmeden önce düzeltilmeli.
            - **öneri:** yazarın kararına kalmış.
            - **soru:** anlamak için soruyorum.

            Bu küçük alışkanlık, hangi yorumun gerçekten önemli olduğunu tartışmayı bitirdi.

            ## Övgüyü de yazın

            İyi bir çözüm gördüğünüzde bunu söylemek, sadece hataları yazmaktan çok daha fazla şey öğretir.
            """),
        new(
            "Boş durum ekranları neden önemli?",
            "bos-durum-ekranlari-neden-onemli",
            "Tasarım",
            1,
            "Kullanıcı bir listeyi ilk açtığında çoğu zaman hiçbir şey görmez. O ekran, ürünün ne işe yaradığını anlatmak için en iyi fırsattır.",
            """
            Kullanıcı bir listeyi ilk açtığında çoğu zaman hiçbir şey görmez. O ekran, ürünün ne işe yaradığını anlatmak için en iyi fırsattır.

            ## İyi bir boş durumun üç parçası

            1. **Ne olduğunu söyleyin:** "Henüz yazınız yok."
            2. **Neden önemli olduğunu anlatın:** "Yazılarınız burada, en yenisi üstte listelenir."
            3. **Tek bir eylem önerin:** "İlk yazınızı yazın."

            ## Arama sonucu boşsa

            Boş arama sonucu farklıdır: kullanıcı bir şey arıyor ve bulamıyor. Filtreleri temizleme bağlantısı ve yazım önerisi burada daha işe yarar.
            """),
        new(
            "Docker imajlarını küçültmenin yolları",
            "docker-imajlarini-kucultmenin-yollari",
            "Yazılım",
            1,
            "1,2 GB'lık imajı çok aşamalı derlemeyle 220 MB'a indirdik. Hem dağıtım hızlandı hem de güvenlik taramasındaki bulgular azaldı.",
            """
            1,2 GB'lık imajı çok aşamalı derlemeyle 220 MB'a indirdik. Hem dağıtım hızlandı hem de güvenlik taramasındaki bulgular azaldı.

            ## Çok aşamalı derleme

            SDK yalnızca derleme aşamasında gerekir. Çalışma aşamasında sadece runtime imajı ve yayınlanan dosyalar kalır:

            ```dockerfile
            FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
            WORKDIR /src
            COPY . .
            RUN dotnet publish -c Release -o /app

            FROM mcr.microsoft.com/dotnet/aspnet:10.0
            COPY --from=build /app .
            ENTRYPOINT ["dotnet", "App.dll"]
            ```

            ## .dockerignore

            `bin`, `obj` ve `.git` klasörlerini dışarıda bırakmak derleme bağlamını küçültür ve önbelleğin daha sık işe yaramasını sağlar.
            """),
        new(
            "Uyku ve hafıza: Beyin gece ne yapıyor?",
            "uyku-ve-hafiza-beyin-gece-ne-yapiyor",
            "Bilim",
            2,
            "Gün içinde öğrendiklerimiz uykuda yeniden oynatılıyor ve kalıcı hafızaya aktarılıyor. Sınavdan önceki gece uykusuz kalmak bu yüzden ters tepiyor.",
            """
            Gün içinde öğrendiklerimiz uykuda yeniden oynatılıyor ve kalıcı hafızaya aktarılıyor. Sınavdan önceki gece uykusuz kalmak bu yüzden ters tepiyor.

            ## Derin uyku

            Derin uyku evresinde hipokampüs, gün içindeki deneyimleri tekrar tekrar "oynatır". Bu sırada bilgiler beyin kabuğuna aktarılır.

            ## REM uykusu

            REM evresi, farklı bilgiler arasında bağlantı kurmakla ilişkilendiriliyor. Bir problemin çözümünün sabah aklınıza gelmesi tesadüf olmayabilir.

            ## Pratikte

            Düzenli uyku saati, ekransız son bir saat ve öğrendiklerinizi uyumadan önce kısaca gözden geçirmek fark yaratır.
            """),
        new(
            "Uzaktan çalışırken odaklanmayı korumak",
            "uzaktan-calisirken-odaklanmayi-korumak",
            "Kariyer",
            0,
            "Bildirimleri kapatmak tek başına yetmedi. Asıl fark, takvimde odak bloklarını toplantı gibi korumakla geldi.",
            """
            Bildirimleri kapatmak tek başına yetmedi. Asıl fark, takvimde odak bloklarını toplantı gibi korumakla geldi.

            ## Odak blokları

            Her sabah iki saatlik bir bloğu takvimde "meşgul" olarak işaretliyorum. Bu sürede mesajlara bakmıyorum; acil bir durum varsa telefonla aranabileceğimi ekip biliyor.

            ## Yazılı iletişim

            Soruyu tek mesajda, bağlamıyla birlikte yazmak karşı tarafın da odağını korur. "Müsait misin?" diye başlayan mesajlar iki kişinin de zamanını alır.

            ## Gün sonu notu

            Günü bitirirken yarın ilk yapacağım işi yazıyorum. Sabah nereden başlayacağımı düşünmek zorunda kalmıyorum.
            """),
        new(
            "Tipografide satır uzunluğu",
            "tipografide-satir-uzunlugu",
            "Tasarım",
            1,
            "Geniş ekranda tüm genişliğe yayılan metin okunmaz. Satır başına 60 ile 75 karakter, gözün satır sonundan yeni satıra rahatça dönmesini sağlar.",
            """
            Geniş ekranda tüm genişliğe yayılan metin okunmaz. Satır başına 60 ile 75 karakter, gözün satır sonundan yeni satıra rahatça dönmesini sağlar.

            ## CSS ile sınırlamak

            ```css
            .article {
              max-width: 68ch;
              line-height: 1.7;
            }
            ```

            `ch` birimi, yazı tipindeki "0" karakterinin genişliğidir; satır uzunluğunu karakter sayısıyla düşünmeyi kolaylaştırır.

            ## Satır aralığı

            Uzun satırlar daha geniş satır aralığı ister. Gövde metni için 1,5 ile 1,7 arası çoğu yazı tipinde iyi sonuç verir.
            """),
        new(
            "Testleri hızlı tutmak için veritabanını taklit etmeyin",
            "testleri-hizli-tutmak-icin-veritabanini-taklit-etmeyin",
            "Yazılım",
            2,
            "Sahte repository'lerle yazdığımız testler geçiyordu ama üretimde benzersiz index hatası alıyorduk. Bellek içi SQLite ile gerçek SQL çalıştırmak bu açığı kapattı.",
            """
            Sahte repository'lerle yazdığımız testler geçiyordu ama üretimde benzersiz index hatası alıyorduk. Bellek içi SQLite ile gerçek SQL çalıştırmak bu açığı kapattı.

            ## Sahte nesnelerin sınırı

            Mock, yalnızca sizin beklediğiniz davranışı taklit eder. Benzersiz index, yabancı anahtar ve sorgu çevirisi gibi veritabanı kuralları testte hiç çalışmaz.

            ## Bellek içi SQLite

            Her test için açılan bir SQLite bağlantısı milisaniyeler içinde hazır olur ve EF Core sorgularını gerçekten SQL'e çevirir. Testler hâlâ hızlı, ama artık gerçeğe çok daha yakın.

            ## Ne zaman yetmez?

            SQL Server'a özgü özellikler (ör. tam metin arama) için gerçek bir veritabanıyla çalışan birkaç entegrasyon testi yine gerekli.
            """),
        new(
            "Okyanuslar neden tuzlu?",
            "okyanuslar-neden-tuzlu",
            "Bilim",
            2,
            "Yağmur suyu kayalardaki mineralleri çözerek denize taşır. Su buharlaşır, tuz kalır; milyonlarca yılda okyanuslar bugünkü tuzluluğa ulaşır.",
            """
            Yağmur suyu kayalardaki mineralleri çözerek denize taşır. Su buharlaşır, tuz kalır; milyonlarca yılda okyanuslar bugünkü tuzluluğa ulaşır.

            ## Tuzun kaynağı

            Yağmur, havadaki karbondioksitle hafifçe asidik hale gelir ve kayaları aşındırır. Açığa çıkan sodyum ve diğer iyonlar nehirlerle denize ulaşır. Klor ise büyük ölçüde deniz tabanındaki volkanik faaliyetlerden gelir.

            ## Neden giderek tuzlanmıyor?

            Tuz girişi kadar çıkışı da var: bazı mineraller deniz tabanında çökelir, bazıları canlıların kabuklarına bağlanır. Bu denge sayesinde okyanusların tuzluluğu uzun süredir yaklaşık yüzde 3,5'te sabit.
            """),
        new(
            "Telefonunuzun pil ömrünü uzatan ayarlar",
            "telefonunuzun-pil-omrunu-uzatan-ayarlar",
            "Teknoloji",
            1,
            "Pil sağlığını en çok yüksek sıcaklık ve sürekli yüzde yüze şarj etmek yıpratır. İki ayar, bataryanın birkaç yıl daha dayanmasını sağlayabilir.",
            """
            Pil sağlığını en çok yüksek sıcaklık ve sürekli yüzde yüze şarj etmek yıpratır. İki ayar, bataryanın birkaç yıl daha dayanmasını sağlayabilir.

            ## Optimize şarj

            Çoğu telefonda "optimize edilmiş şarj" veya "şarjı yüzde 80'de durdur" seçeneği var. Gece boyunca yüzde yüzde beklemek yerine, kalkmanıza yakın şarjı tamamlar.

            ## Sıcaktan uzak tutun

            Şarj olurken oyun oynamak veya telefonu güneşte bırakmak bataryayı ısıtır. Kalın kılıflar da şarj sırasında ısıyı hapseder.

            ## Asıl tüketen ne?

            Ayarlar menüsündeki pil kullanımı listesi, arka planda en çok çalışan uygulamaları gösterir. Genellikle tek bir uygulama günün yarısını tüketir.
            """),
        new(
            "Yeni bir kod tabanını okumaya nereden başlamalı?",
            "yeni-bir-kod-tabanini-okumaya-nereden-baslamali",
            "Kariyer",
            0,
            "İlk hafta her dosyayı okumaya çalıştım ve hiçbir şey aklımda kalmadı. Tek bir isteği baştan sona izlemek çok daha iyi sonuç verdi.",
            """
            İlk hafta her dosyayı okumaya çalıştım ve hiçbir şey aklımda kalmadı. Tek bir isteği baştan sona izlemek çok daha iyi sonuç verdi.

            ## Bir isteği takip edin

            Uygulamadaki basit bir sayfayı seçin ve tarayıcıdan veritabanına kadar izleyin: hangi controller, hangi servis, hangi sorgu? Bu yol, mimarinin özetini verir.

            ## Testleri okuyun

            Testler, kodun nasıl kullanılması gerektiğini gösteren en güncel belgedir.

            ## Not alın ve paylaşın

            Anlamakta zorlandığınız yerleri yazın. Bir sonraki yeni ekip arkadaşı için en değerli belge, sizin ilk haftanızın notları olabilir.
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
