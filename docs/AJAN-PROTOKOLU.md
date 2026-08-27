# Ajan Çalışma Protokolü

Bu doküman, [`docs/YOL-HARITASI.md`](./YOL-HARITASI.md) görevlerini paralel çalışan birden çok
ajana (veya geliştiriciye) dağıtan koordinatör bot ve görevi üstlenen ajanlar içindir.
Makine okunur görev listesi: [`docs/gorevler.json`](./gorevler.json).

---

## 1. Koordinatör bota talimat

Görevleri dağıtırken şu sırayla karar ver:

1. **`docs/gorevler.json`'u oku.** Her görevin `id`, `lane`, `dependsOn`, `estimateHours`,
   `files`, `dod` alanları vardır.
2. **Yalnız bağımlılıkları `done` olan görevleri dağıt.** `dependsOn` listesindeki her kimlik
   tamamlanmadan görev başlatılamaz.
3. **Aynı anda aynı kulvarda en fazla 1 ajan çalışsın** (dosya çakışmasını önler). Farklı
   kulvarlar sınırsız paralel çalışabilir.
4. **`lane: "DB"` görevleri için küresel kilit uygula:** aynı anda yalnız **bir** ajan
   migration üretebilir. Sıradaki DB görevi, öncekinin PR'ı `main`'e girmeden başlamaz.
5. **Kritik görevleri öne al:** `priority: "critical"` olanlar (`F0-SEC-01`, `F0-BE-04`,
   `F3-BE-03`) diğer her şeyden önce gelir.
6. **Bir ajana bir görev ver.** Görev kartını (`docs/YOL-HARITASI.md` içindeki tam metin) ve
   bu protokolü birlikte ilet. Görevi bölme; kart zaten atomiktir.
7. **Blokaj bildiren ajanı bekletme:** görevi `blocked` işaretle, gerekçesini
   "Açık sorular" tablosuna ekle, ajana sıradaki uygun görevi ver.

Ajana verilecek istem şablonu bölüm 8'dedir.

---

## 2. Görev yaşam döngüsü

```
todo → in_progress → in_review → done
                  ↘ blocked ↗
```

- **in_progress'e geçerken:** ajan dalı açar ve ilk commit'i atar.
- **in_review'a geçerken:** PR açılır, CI yeşildir, kart BT maddeleri PR açıklamasında
  işaretlenmiştir.
- **done'a geçerken:** PR `main`'e birleşti ve CI `main` üzerinde yeşil.
- **blocked:** eksik karar, dış bağımlılık veya bağımlı görevin tamamlanmamış olması.
  Gerekçe PR/issue'da yazılır; ajan başka göreve geçer.

---

## 3. Dal, commit, PR

**Dal adı**
```
feat/<GOREV-ID>-<kisa-slug>      örn. feat/F1-BE-02-jwt-refresh
fix/<GOREV-ID>-<kisa-slug>
```
Ana dal `main`. Doğrudan `main`'e push **yok**.

**Commit mesajı** (Conventional Commits + görev kimliği)
```
feat(F1-BE-02): refresh token rotasyonu ve yeniden kullanım tespiti

- TokenService: 7 gün ömürlü, her kullanımda dönen refresh token
- Yeniden kullanım tespitinde kullanıcının tüm oturumları iptal edilir
- HttpOnly + Secure + SameSite=Strict çerez
```
Tip listesi: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `perf`, `build`, `ci`.
Kapsam **her zaman** görev kimliğidir.

**PR kuralları**
- Bir PR = bir görev kartı. 400 satırdan büyük PR bölünür.
- Başlık: `<GOREV-ID>: <kartın adı>`
- Açıklamada: ne yapıldı · nasıl doğrulandı (komut + çıktı) · BT kontrol listesi işaretli ·
  ekran görüntüsü (arayüz değişikliği varsa) · kırıcı değişiklik notu.
- CI yeşil olmadan inceleme istenmez.
- İnceleme yorumları ya uygulanır ya da neden uygulanmadığı yazılır; sessizce kapatılmaz.

---

## 4. Çatışma önleme

### 4.1 Dosya sahipliği

| Kulvar | Yazma hakkı olan yollar |
|---|---|
| `BE` | `backend/Controllers/**`, `backend/Services/**`, `backend/Domain/**`, `backend/Application/**`, `backend/Models/*Dto.cs`, `backend/Models/*Mapper.cs` |
| `DB` | `backend/Migrations/**`, `backend/Data/**`, `backend/Models/<Varlık>.cs` |
| `FE` | `frontend/src/**`, `frontend/angular.json`, `frontend/package.json` |
| `INF` | `.github/**`, `Dockerfile*`, `docker-compose*.yml`, `ops/**`, `*.sln`, `Directory.Build.props`, kök `.editorconfig` |
| `QA` | `backend/tests/**`, `e2e/**`, `perf/**`, `**/*.spec.ts` |
| `SEC` | `backend/Authorization/**`, `docs/GUVENLIK.md`, `docs/KVKK.md` |
| `DOC` | `docs/**`, `README.md`, `CONTRIBUTING.md` |

Başka kulvarın dosyasına dokunman gerekiyorsa: PR açıklamasında gerekçelendir ve değişikliği
mümkün olan en küçük hâlde tut. `Program.cs` ortak alandır — dokunmadan önce o dosyaya
dokunan açık PR var mı kontrol et.

### 4.2 Migration kilidi

1. Migration üretmeden önce `git fetch origin main && git log --oneline origin/main -5` ile
   son durumu al.
2. `backend/Migrations/` altında birleşmemiş başka migration varsa **bekle**.
3. Ad biçimi: `dotnet ef migrations add <YYYYMMDDHHmm>_<Konu>` (ör. `202609031430_AddClaims`).
4. Çakışma olursa migration **elle düzenlenmez**: `dotnet ef migrations remove` ile silinir,
   `main` alınır, yeniden üretilir.
5. Her migration'ın `Down` metodu çalışır durumda olmalı ve bir kez denenmiş olmalıdır.

### 4.3 Sözleşme önce

API değişikliği şu sırayla yapılır:
1. `docs/API.md` ve OpenAPI şeması güncellenir → **ayrı commit**.
2. Backend uygular.
3. `npm run api:gen` ile ön yüz tipleri yeniden üretilir.
4. Ön yüz uyarlanır.

Ön yüz, yayımlanmamış bir sözleşmeye göre kodlanmaz. Geçici olarak sahte veri (mock) kullanılabilir
ama PR'da `TODO(<GOREV-ID>)` ile işaretlenir.

---

## 5. Çalıştırma ve doğrulama komutları

```bash
# Veritabanı
docker compose up -d postgres

# Backend
dotnet build                                   # kökte, sln üzerinden — uyarısız olmalı
dotnet test                                    # birim + entegrasyon
dotnet run --project backend -- --migrate      # yalnız şema güncelle
dotnet run --project backend                   # API :5080

# Frontend
cd frontend
npm ci
npm run lint          # 0 hata 0 uyarı
npm run format:check
npm test -- --run     # Vitest
npm run build -- --configuration production

# Uçtan uca (F7 sonrası)
npx playwright test

# Duman testi (canlı API'ye karşı)
./backend/run-smoke-tests.sh
```

**PR açmadan önce en az şunlar yeşil olmalı:** `dotnet build`, `dotnet test`,
`npm run lint`, `npm test -- --run`, `npm run build`.

---

## 6. Kod standartları

**C#**
- Dosya kapsamlı `namespace`, `sealed` varsayılan, `record` DTO'lar için.
- Denetleyici (controller) ince: doğrulama + servis çağrısı + sonuç eşleme. İş kuralı servistedir.
- İş kuralları `Domain` katmanında ve saf (I/O yok) → hızlı birim testi.
- `async` uçtan uca; her genel `async` metot `CancellationToken` alır.
- İstisnalar kontrol akışı için kullanılmaz; beklenen hatalar sonuç tipi veya doğrulama ile döner.
- `DateTime.UtcNow` kullanılır; sunucu daima UTC saklar, biçimlendirme ön yüzde
  (`Europe/Istanbul`).

**TypeScript / Angular**
- Standalone bileşen, `ChangeDetectionStrategy.OnPush`, signal tabanlı durum.
- Servis enjeksiyonu `inject()` ile; constructor enjeksiyonu yeni kodda yok.
- Tipli reaktif formlar; `any` yasak, `unknown` + daraltma serbest.
- RxJS abonelikleri `takeUntilDestroyed()` ile sonlandırılır.
- Özellik rotaları tembel yüklenir (`loadComponent` / `loadChildren`).
- Kullanıcıya görünen metin `core/i18n/tr.ts`'te; şablonda sabit metin yok.

**SQL / EF**
- Tablo ve kolon adları `snake_case`.
- Filtrelenen ve sıralanan her kolon indeksli; indeks kararı PR'da gerekçelendirilir.
- Ham SQL yalnız gerekçeli ve parametreli.

---

## 7. Asla yapma

1. **Testi devre dışı bırakma, atlama (`Skip`) veya silme** — kırmızıyı gizleme.
2. **Sır commit'leme** — parola, token, bağlantı dizesi. Sızarsa: anahtarı iptal et, sonra temizle.
3. **`main`'e doğrudan push, `--no-verify`, başkasının dalına `--force`.**
4. **Kapsam/bütçe eşiklerini düşürerek CI'ı yeşile boyama.**
5. **API sözleşmesini habersiz değiştirme** (bölüm 4.3).
6. **Migration'ı elle düzenleme** veya birleşmiş migration'ı değiştirme.
7. **Kişisel veriyi günlüğe yazma** — e-posta, telefon, ad-soyad maskelenir.
8. **`contact` alanını `ContactVisibilityPolicy` dışında bir yoldan döndürme.**
9. **Kart kapsamını genişletme** — fark ettiğin ek iş yeni görev olarak önerilir, aynı PR'a girmez.
10. **"Çalışıyor" demeden önce çalıştırmama** — her BT maddesinin kanıtı komut çıktısıdır.

---

## 8. Ajan istem şablonu

Koordinatör bot her ajana aşağıdaki bloğu, `<...>` yerlerini doldurarak gönderir:

```text
Proje: Aksaray Üniversitesi Kampüs Kayıp Eşya Platformu
Depo: https://github.com/safacenkci/kampus-kayip-esya
Yığın: Angular 21 + .NET 8 + PostgreSQL 16

Görevin: <GOREV-ID> — <görev adı>
Kulvar: <BE|FE|DB|INF|QA|SEC|DOC>
Bağımlılıklar (tamamlandı): <id listesi>

--- GÖREV KARTI (docs/YOL-HARITASI.md'den birebir) ---
<kartın tam metni: Yapılacak, Dosyalar, Bitti tanımı>
------------------------------------------------------

Kurallar:
1. Önce docs/AJAN-PROTOKOLU.md ve docs/YOL-HARITASI.md bölüm 2 (mimari) ile bölüm 8
   (global bitti tanımı) dosyalarını oku.
2. Yalnız kulvarına ait dosyalara yaz (protokol bölüm 4.1). Başka dosyaya dokunman
   gerekiyorsa PR'da gerekçelendir.
3. Dal: feat/<GOREV-ID>-<slug>. Commit: <tip>(<GOREV-ID>): <özet>.
4. Kart kapsamının dışına çıkma. Fark ettiğin ek işleri PR açıklamasında "Öneri" başlığı
   altında listele, uygulama.
5. PR açmadan önce protokol bölüm 5'teki tüm komutları çalıştır ve çıktılarını PR'a yapıştır.
6. Bitti tanımının her maddesini PR açıklamasında kutucukla işaretle ve kanıtını ver.
7. Karara bağlanmamış bir şeyle karşılaşırsan uydurma: görevi "blocked" bildir, soruyu
   net biçimde yaz ve dur.

Çıktın: açılmış bir PR ve bitti tanımı kanıtları.
```

---

## 9. Kalite barı hatırlatması (her PR'da geçerli)

README'deki 9 madde **regresyon korumasıdır** — hiçbir görev bunları bozamaz:

1. Ayrı Kaybettim / Buldum akışları
2. Konum seçimi sabit listeden
3. Kategori seçimi sabit listeden
4. `open → claimed(reserved) → closed` durumları ve durum geçmişi
5. Arama + tip/kategori/konum/durum filtreleri, kart UI, boş durum
6. Karşıt ilan eşleşme önerisi
7. **İletişim bilgisi yetkisiz kişiye asla görünmez**
8. Türkçe arayüz, gerçekçi tohum verisi
9. PostgreSQL kalıcılığı (yenilemede veri durur)

Bir görev bu maddelerden birini değiştiriyorsa (ör. `F3` madde 4 ve 7'yi derinleştirir),
README ve e2e senaryoları **aynı PR'da** güncellenir.
