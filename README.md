# Kampüs Kayıp-Eşya

Kampüs kayıp/bulunan eşya ilan uygulaması. Küçük, bitirilebilir, tarayıcıda demo edilebilir.

## Stack

- Frontend: Angular (`frontend/`)
- Backend: ASP.NET Core Web API (`backend/`)
- Veritabanı: PostgreSQL (`docker-compose.yml`)

## MVP

Kayıp ve bulunan ilan CRUD:
- Alanlar: `title`, `description`, `location`, `category`, `contact`, `photoUrl` (string URL, dosya yükleme yok)
- `kind`: `lost` | `found`
- `status`: `open` | `claimed` | `closed`  (açık / sahiplenildi / kapandı)

Ekranlar: liste + arama/filtre, detay, durum değiştir, oluştur/düzenle/sil.

Kapsam dışı: gerçek auth, dosya yükleme, sohbet.

## API sözleşmesi (`/api`)

```
GET    /api/items?q=&category=&status=&kind=
POST   /api/items
GET    /api/items/{id}
PUT    /api/items/{id}
PATCH  /api/items/{id}/status   body: { "status": "claimed" }
DELETE /api/items/{id}
GET    /api/categories
```

Örnek gövde:

```json
{
  "id": 1,
  "title": "Siyah mont",
  "description": "Kütüphane 2. kat",
  "location": "Merkez Kütüphane",
  "category": "giyim",
  "contact": "safa@example.com",
  "photoUrl": "https://picsum.photos/seed/mont/400/300",
  "kind": "lost",
  "status": "open",
  "createdAt": "2026-08-27T08:00:00Z"
}
```

CORS: `http://localhost:4200` → API `http://localhost:5080`.

Seed: en az 5 örnek ilan.

## Bitti kriteri

Sayfa yenilenince veri duruyor. Tarayıcıda uçtan uca demo edilebiliyor.
