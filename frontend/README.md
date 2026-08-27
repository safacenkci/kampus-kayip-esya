# Kampüs Kayıp-Eşya — Frontend

Aksaray Üniversitesi kampüs panosu. Angular arayüz. API sözleşmesi kök `README.md` dosyasındadır.

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

Uygulama `http://localhost:4200` adresinde açılır.

## API

Geliştirme ortamı tabanı: `http://localhost:5080/api` (`src/environments/environment.ts`).

Backend’in `http://localhost:5080` üzerinde ayakta olması gerekir. CORS `http://localhost:4200` için açıktır.

`GET /api/locations` ve `GET /api/items/{id}/matches` yoksa arayüz yumuşak düşer (sabit konum listesi ve istemci eşleşmesi).

## Ekranlar

- `/` — kart listesi, arama, tür/kategori/konum/durum filtreleri, boş durum
- `/kaybettim` — kayıp ilanı sihirbazı (tür seçimi yok)
- `/buldum` — bulunan ilanı sihirbazı (tür seçimi yok)
- `/:id` — detay, durum (açık → sahiplenildi → kapandı), durum geçmişi, eşleşen ilanlar
- `/:id/duzenle` — kayıp veya bulunan akışına göre düzenleme

İletişim, durum `claimed` veya `closed` olana kadar gösterilmez. Fotoğraf alanı düz metin URL’dir; dosya yükleme yoktur.
