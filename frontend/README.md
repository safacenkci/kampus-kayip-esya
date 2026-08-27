# Kampüs Kayıp-Eşya — Frontend

Angular arayüz. API sözleşmesi kök `README.md` dosyasındadır.

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

## Ekranlar

- `/` — ilan listesi, `q` / tür / durum / kategori filtreleri
- `/yeni` — yeni ilan
- `/:id` — detay, durum değiştirme (`PATCH`) ve silme
- `/:id/duzenle` — düzenleme

Fotoğraf alanı düz metin URL’dir; dosya yükleme yoktur.
