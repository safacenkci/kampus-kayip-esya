using KampusKayipEsya.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KampusKayipEsya.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Items.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Items.AddRange(
            Create(
                title: "Aksaray Üniversitesi öğrenci kartı",
                description: "Yemekhane kasa sırasında düşmüş. Kartta AÜ logosu ve isim: E. Demir görünüyor.",
                location: "yemekhane",
                category: "öğrenci kartı",
                contact: "guvenlik@aksaray.edu.tr",
                photoUrl: "https://picsum.photos/seed/au-kart/400/300",
                kind: ItemRules.KindFound,
                status: ItemRules.StatusOpen,
                createdAt: Utc(2026, 8, 27, 8, 10)),
            Create(
                title: "Kayıp öğrenci kimlik kartı",
                description: "Merkez yemekhanede öğle arasında kaybettim. Mavi kılıflı Aksaray Üniversitesi kartı.",
                location: "yemekhane",
                category: "öğrenci kartı",
                contact: "elif.demir@aksaray.edu.tr",
                photoUrl: "https://picsum.photos/seed/au-kart-kayip/400/300",
                kind: ItemRules.KindLost,
                status: ItemRules.StatusOpen,
                createdAt: Utc(2026, 8, 27, 7, 40)),
            Create(
                title: "Siyah iPhone 13",
                description: "Kütüphane 2. kat sessiz çalışma salonunda unuttum. Siyah kılıflı, arka kamerada çizik var.",
                location: "kütüphane",
                category: "telefon",
                contact: "mehmet.yilmaz@aksaray.edu.tr",
                photoUrl: "https://picsum.photos/seed/au-iphone/400/300",
                kind: ItemRules.KindLost,
                status: ItemRules.StatusOpen,
                createdAt: Utc(2026, 8, 26, 19, 15)),
            Create(
                title: "Bulunan telefon (kütüphane)",
                description: "Merkez kütüphane girişindeki güvenlik masasında teslim edildi. Siyah akıllı telefon.",
                location: "kütüphane",
                category: "telefon",
                contact: "kutuphane@aksaray.edu.tr",
                photoUrl: "https://picsum.photos/seed/au-telefon/400/300",
                kind: ItemRules.KindFound,
                status: ItemRules.StatusOpen,
                createdAt: Utc(2026, 8, 26, 20, 5)),
            Create(
                title: "Yurt oda anahtarı",
                description: "Kız yurdu A blok 2. kat koridorunda kaybettim. Yeşil anahtarlık, küçük metal anahtar.",
                location: "yurt",
                category: "anahtar",
                contact: "zeynep.kaya@aksaray.edu.tr",
                photoUrl: "https://picsum.photos/seed/au-anahtar/400/300",
                kind: ItemRules.KindLost,
                status: ItemRules.StatusOpen,
                createdAt: Utc(2026, 8, 25, 21, 30)),
            Create(
                title: "Siyah sırt çantası",
                description: "Mühendislik Fakültesi B blok 102 dersliği önünde bulundu. Üzerinde AÜ sticker'ı var.",
                location: "mühendislik",
                category: "çanta",
                contact: "muhendislik@aksaray.edu.tr",
                photoUrl: "https://picsum.photos/seed/au-canta/400/300",
                kind: ItemRules.KindFound,
                status: ItemRules.StatusClaimed,
                createdAt: Utc(2026, 8, 24, 11, 0),
                laterStatusAt: Utc(2026, 8, 24, 16, 45)),
            Create(
                title: "Beyaz kablosuz kulaklık",
                description: "Spor salonu soyunma dolabında unuttum. Şarj kutusu da kulaklıkla birlikte kayıp.",
                location: "spor salonu",
                category: "kulaklık",
                contact: "ayse.celik@aksaray.edu.tr",
                photoUrl: "https://picsum.photos/seed/au-kulaklik/400/300",
                kind: ItemRules.KindLost,
                status: ItemRules.StatusOpen,
                createdAt: Utc(2026, 8, 23, 18, 20)),
            Create(
                title: "Lacivert yağmurluk",
                description: "Rektörlük tarafı merkez durağındaki bankta unutulmuş. İç cebinde yedek şemsiye var.",
                location: "merkez",
                category: "kıyafet",
                contact: "kampus.guvenlik@aksaray.edu.tr",
                photoUrl: "https://picsum.photos/seed/au-mont/400/300",
                kind: ItemRules.KindFound,
                status: ItemRules.StatusClosed,
                createdAt: Utc(2026, 8, 22, 9, 50),
                laterStatusAt: Utc(2026, 8, 22, 14, 10))
        );

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Item Create(
        string title,
        string description,
        string location,
        string category,
        string contact,
        string photoUrl,
        string kind,
        string status,
        DateTime createdAt,
        DateTime? laterStatusAt = null)
    {
        var item = new Item
        {
            Title = title,
            Description = description,
            Location = location,
            Category = category,
            Contact = contact,
            PhotoUrl = photoUrl,
            Kind = kind,
            CreatedAt = createdAt
        };

        ItemRules.RecordStatus(item, ItemRules.StatusOpen, createdAt);
        if (status != ItemRules.StatusOpen)
        {
            ItemRules.RecordStatus(item, status, laterStatusAt ?? createdAt);
        }

        return item;
    }

    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        DateTime.SpecifyKind(new DateTime(year, month, day, hour, minute, 0), DateTimeKind.Utc);
}
