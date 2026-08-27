# Kampüs Kayıp-Eşya

Aksaray Üniversitesi kampüs kayıp/bulunan eşya panosu. Angular + .NET + PostgreSQL.
GitHub tek kaynak: https://github.com/safacenkci/kampus-kayip-esya

## Kalite barı (bugün kapanış, hepsi şart)

Generic CRUD yetmez. Bitti = tarayıcıda bu 9 madde kanıtlanmış:

1. İki ayrı akış: **Kaybettim** / **Buldum** (tek generic form değil).
2. Konum seçmeli: `merkez`, `kütüphane`, `yemekhane`, `mühendislik`, `yurt`, `spor salonu`.
3. Kategori: `öğrenci kartı`, `anahtar`, `telefon`, `çanta`, `kıyafet`, `kulaklık`, `diğer`.
4. Durum: `open` → `claimed` → `closed` (açık / sahiplenildi / kapandı). Detayda kısa durum geçmişi.
5. Liste: arama + tip/kategori/konum/durum filtre; kart UI; boş state.
6. Eşleşme: detayda aynı kategori+konumda açık karşıt ilan önerisi (`lost`↔`found`).
7. İletişim, durum `claimed` olana kadar gizli (API de `open` iken `contact` dönmez).
8. Türkçe UI. 6–8 gerçekçi seed ilan.
9. PostgreSQL: refresh sonrası veri durur.

Kapsam dışı: gerçek auth, dosya yükleme, harita SDK, chat. `photoUrl` string yeter.

## API (`/api`)

```
GET    /api/items?q=&kind=&category=&location=&status=
POST   /api/items
GET    /api/items/{id}
PUT    /api/items/{id}
PATCH  /api/items/{id}/status   body: { "status": "claimed" }
DELETE /api/items/{id}
GET    /api/items/{id}/matches
GET    /api/categories
GET    /api/locations
```

`kind`: `lost` | `found`. JSON camelCase. CORS: `http://localhost:4200` → `http://localhost:5080`.

## Çalıştırma

```bash
docker compose up -d postgres
cd backend && dotnet run          # :5080
cd frontend && npm i && npx ng serve --port 4200
```
