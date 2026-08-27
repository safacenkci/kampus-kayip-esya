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
            new Item
            {
                Title = "Siyah mont",
                Description = "Kütüphane 2. kat, okuma salonunun yanında asılı duruyordu.",
                Location = "Merkez Kütüphane",
                Category = "giyim",
                Contact = "safa@example.com",
                PhotoUrl = "https://picsum.photos/seed/mont/400/300",
                Kind = ItemRules.KindLost,
                Status = ItemRules.StatusOpen,
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 8, 27, 8, 0, 0), DateTimeKind.Utc)
            },
            new Item
            {
                Title = "USB bellek 32GB",
                Description = "Siyah Kingston USB, yemekhane kasa sırasında bulundu.",
                Location = "Merkez Yemekhane",
                Category = "elektronik",
                Contact = "kayip@kampus.edu.tr",
                PhotoUrl = "https://picsum.photos/seed/usb/400/300",
                Kind = ItemRules.KindFound,
                Status = ItemRules.StatusOpen,
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 8, 26, 14, 30, 0), DateTimeKind.Utc)
            },
            new Item
            {
                Title = "Öğrenci kartı",
                Description = "Mühendislik Fakültesi girişinde düşmüş, isim: A. Yılmaz.",
                Location = "Mühendislik Fakültesi",
                Category = "belgeler",
                Contact = "guvenlik@kampus.edu.tr",
                PhotoUrl = "https://picsum.photos/seed/kart/400/300",
                Kind = ItemRules.KindFound,
                Status = ItemRules.StatusClaimed,
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 8, 25, 9, 15, 0), DateTimeKind.Utc)
            },
            new Item
            {
                Title = "Kablosuz kulaklık",
                Description = "Beyaz kulaklık, spor salonu soyunma dolabında unutulmuş olabilir.",
                Location = "Spor Salonu",
                Category = "elektronik",
                Contact = "ayse@example.com",
                PhotoUrl = "https://picsum.photos/seed/kulaklik/400/300",
                Kind = ItemRules.KindLost,
                Status = ItemRules.StatusOpen,
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 8, 24, 18, 45, 0), DateTimeKind.Utc)
            },
            new Item
            {
                Title = "Mavi termos",
                Description = "Lacivert metal termos, rektörlük önü bankta unutuldu.",
                Location = "Rektörlük",
                Category = "aksesuar",
                Contact = "mehmet@example.com",
                PhotoUrl = "https://picsum.photos/seed/termos/400/300",
                Kind = ItemRules.KindLost,
                Status = ItemRules.StatusOpen,
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 8, 23, 11, 20, 0), DateTimeKind.Utc)
            },
            new Item
            {
                Title = "Calculus kitabı",
                Description = "Stewart Calculus, A Blok 203 dersliğinde bırakılmış.",
                Location = "A Blok 203",
                Category = "kitap",
                Contact = "kutuphane@kampus.edu.tr",
                PhotoUrl = "https://picsum.photos/seed/kitap/400/300",
                Kind = ItemRules.KindFound,
                Status = ItemRules.StatusClosed,
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 8, 22, 16, 0, 0), DateTimeKind.Utc)
            }
        );

        await db.SaveChangesAsync(cancellationToken);
    }
}
