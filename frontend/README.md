# Kampüs Kayıp-Eşya — Frontend

Aksaray kampüs kayıp/bulunan panosu. API sözleşmesi kök `README.md` dosyasındadır.

## Çalıştırma

```bash
cd frontend
npm install
npx ng serve --port 4200
```

veya:

```bash
npm start
```

Uygulama `http://localhost:4200` adresinde açılır. Backend’in `http://localhost:5080` üzerinde ayakta olması gerekir.

## API

Geliştirme ortamı tabanı: `http://localhost:5080/api` (`src/environments/environment.ts`).
CORS: `http://localhost:4200` → `http://localhost:5080`.

Kaynak gerçek API’dir; `localStorage` kaynak değildir. Sayfa yenilenince veri sunucudan yüklenir.

## Ekranlar

- `/` — tüm ilanlar, kart listesi, `q` + tür/kategori/konum/durum filtreleri
- `/kaybettim` — Kaybettim (kayıp) akışı: ayrı metin, CTA ve liste
- `/kaybettim/yeni` — kayıp ilan formu (tür kilitli, tür dropdown’u yok)
- `/buldum` — Buldum (bulunan) akışı
- `/buldum/yeni` — bulunan ilan formu
- `/yeni` — Kaybettim / Buldum seçimi
- `/ilan/:id` — detay, durum geçmişi, eşleşmeler, iletişim kapısı, durum değiştirme, silme
- `/ilan/:id/duzenle` — düzenleme (akış türü korunur)

Konum ve kategori açılır listedir. `GET /api/locations` ve `GET /api/categories` tercih edilir; boş veya yoksa sabit listeler kullanılır.

Eşleşmeler: `GET /api/items/{id}/matches`. Uç nokta yoksa aynı kategori+konumdaki açık karşıt ilanlar listeden süzülür.

Fotoğraf alanı düz metin URL’dir; dosya yükleme yoktur.
