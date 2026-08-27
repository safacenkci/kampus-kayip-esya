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

        var kartLost = Item(
            "ASÜ öğrenci kartı kayıp",
            "Aksaray Üniversitesi öğrenci kimliğim kütüphane giriş turnikesinde düşmüş olabilir. Kartta isim: Elif Demir, Mühendislik Fakültesi.",
            "kütüphane",
            "öğrenci kartı",
            "elif.demir@aksaray.edu.tr",
            "https://picsum.photos/seed/asu-kart-lost/400/300",
            ItemRules.KindLost,
            ItemRules.StatusOpen,
            Utc(2026, 8, 27, 8, 10));

        var kartFound = Item(
            "Kütüphanede ASÜ öğrenci kartı bulundu",
            "Merkez Kütüphane 2. kat okuma salonunun masasında Aksaray Üniversitesi öğrenci kartı bulundu. Güvenlik masasına teslim edilebilir.",
            "kütüphane",
            "öğrenci kartı",
            "kutuphane@aksaray.edu.tr",
            "https://picsum.photos/seed/asu-kart-found/400/300",
            ItemRules.KindFound,
            ItemRules.StatusOpen,
            Utc(2026, 8, 27, 9, 5));

        var anahtarLost = Item(
            "Yurt oda anahtarı",
            "Kız Öğrenci Yurdu oda anahtarı, yemekhaneye giderken düşmüş olabilir. Anahtarlıkta küçük ASÜ rozeti var.",
            "yurt",
            "anahtar",
            "zeynep.kaya@aksaray.edu.tr",
            "https://picsum.photos/seed/asu-anahtar/400/300",
            ItemRules.KindLost,
            ItemRules.StatusOpen,
            Utc(2026, 8, 26, 19, 40));

        var telefonFound = Item(
            "iPhone 13 Mühendislik koridorunda",
            "Mühendislik Fakültesi A blok 1. kat koridorda siyah kılıflı iPhone 13 bulundu. Ekranda ASÜ duvar kâğıdı görünüyor.",
            "mühendislik",
            "telefon",
            "muhendislik.guvenlik@aksaray.edu.tr",
            "https://picsum.photos/seed/asu-telefon/400/300",
            ItemRules.KindFound,
            ItemRules.StatusClaimed,
            Utc(2026, 8, 25, 11, 20));
        Record(telefonFound, ItemRules.StatusOpen, ItemRules.StatusClaimed, Utc(2026, 8, 25, 16, 5));

        var cantaLost = Item(
            "Siyah sırt çantası",
            "ASÜ yemekhane kasa sırasında siyah sırt çantamı unuttum. İçinde Calculus defteri ve USB bellek vardı.",
            "yemekhane",
            "çanta",
            "mehmet.yilmaz@aksaray.edu.tr",
            "https://picsum.photos/seed/asu-canta/400/300",
            ItemRules.KindLost,
            ItemRules.StatusOpen,
            Utc(2026, 8, 26, 12, 15));

        var kiyafetFound = Item(
            "Lacivert eşofman üstü",
            "Spor salonu soyunma odasında lacivert ASÜ yazılı eşofman üstü bulundu. Askıdaki hırkanın yanında duruyordu.",
            "spor salonu",
            "kıyafet",
            "spor@aksaray.edu.tr",
            "https://picsum.photos/seed/asu-esofman/400/300",
            ItemRules.KindFound,
            ItemRules.StatusOpen,
            Utc(2026, 8, 24, 18, 30));

        var kulaklikLost = Item(
            "Beyaz kablosuz kulaklık",
            "Merkez kampüs durağında (rektörlük önü) beyaz kablosuz kulaklık kutusunu unuttum. AirPods tarzı, üzerinde çizik var.",
            "merkez",
            "kulaklık",
            "ayse.ozturk@aksaray.edu.tr",
            "https://picsum.photos/seed/asu-kulaklik/400/300",
            ItemRules.KindLost,
            ItemRules.StatusOpen,
            Utc(2026, 8, 23, 17, 50));

        var gozlukClosed = Item(
            "Siyah gözlük kılıfı",
            "Yemekhane girişindeki bankta siyah gözlük kılıfı bulundu, sahibi teslim aldı.",
            "yemekhane",
            "diğer",
            "yemekhane@aksaray.edu.tr",
            "https://picsum.photos/seed/asu-gozluk/400/300",
            ItemRules.KindFound,
            ItemRules.StatusClosed,
            Utc(2026, 8, 22, 13, 0));
        Record(gozlukClosed, ItemRules.StatusOpen, ItemRules.StatusClaimed, Utc(2026, 8, 22, 15, 10));
        Record(gozlukClosed, ItemRules.StatusClaimed, ItemRules.StatusClosed, Utc(2026, 8, 23, 9, 0));

        db.Items.AddRange(
            kartLost,
            kartFound,
            anahtarLost,
            telefonFound,
            cantaLost,
            kiyafetFound,
            kulaklikLost,
            gozlukClosed);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Item Item(
        string title,
        string description,
        string location,
        string category,
        string contact,
        string photoUrl,
        string kind,
        string status,
        DateTime createdAt) =>
        new()
        {
            Title = title,
            Description = description,
            Location = location,
            Category = category,
            Contact = contact,
            PhotoUrl = photoUrl,
            Kind = kind,
            Status = status,
            CreatedAt = createdAt
        };

    private static void Record(Item item, string from, string to, DateTime changedAt)
    {
        item.StatusHistory.Add(new StatusHistory
        {
            FromStatus = from,
            ToStatus = to,
            ChangedAt = changedAt
        });
    }

    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        DateTime.SpecifyKind(new DateTime(year, month, day, hour, minute, 0), DateTimeKind.Utc);
}
