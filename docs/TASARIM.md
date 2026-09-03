# Tasarım Sistemi

Kampüs Kayıp-Eşya arayüzünün görsel dili. Hedef kitle **üniversite öğrencileri**:
yüksek kontrast, iri dokunma hedefleri, mobil öncelikli düzen, samimi ve kısa Türkçe.

Tek kaynak: `frontend/src/styles.css`. Bileşen dosyaları yalnızca bu jetonları kullanır;
hiçbir bileşende doğrudan renk kodu yazılmaz.

---

## 1. Kurucu fikir

Uygulamanın bilgi mimarisi **iki karşıt akıştan** oluşur: Kaybettim ve Buldum.
Bu yüzden renk, süs değil **bilgi taşıyıcıdır**: her akışın kendi rengi vardır ve bu renk
kartlarda, sihirbazda, süzgeçlerde ve alt sekme çubuğunda tutarlı biçimde tekrarlanır.

| Rol | Renk | Nerede |
|---|---|---|
| Kaybettim | vermilyon | Kayıp rozeti, kayıp akışı sihirbazı, kayıp süzgeci, kayıp CTA |
| Buldum | çamurlu turkuaz | Bulunan rozeti, bulunan akışı, bulunan süzgeci, bulunan CTA |
| Birincil eylem | mürekkep (siyaha yakın) | Nötr bağlamdaki ana düğmeler, seçili süzgeç |
| Vurgu | fosforlu sarı | Marka işareti, eşleşme sayacı — çok az yerde |

Vermilyon ve turkuaz, renk körlüğünde de ayrışan bir çifttir. Yine de renk **hiçbir zaman
tek başına anlam taşımaz**: her rozetin metni de vardır.

---

## 2. Jetonlar

### Yüzey ve metin

| Jeton | Açık | Koyu | Kullanım |
|---|---|---|---|
| `--bg` | `#f1f2ef` | `#0e1012` | Sayfa zemini |
| `--surface` | `#ffffff` | `#16191d` | Kart, panel, girdi |
| `--surface-2` | `#f7f8f5` | `#1b1f23` | İç kutucuk, arama alanı |
| `--surface-3` | `#eef0ec` | `#22272b` | Görsel yeri, pasif rozet |
| `--ink` | `#14171a` | `#eceeea` | Ana metin, birincil düğme zemini |
| `--ink-2` | `#414850` | `#b6bcbe` | İkincil metin |
| `--muted` | `#6c747c` | `#868d92` | Üçüncül metin, ikon |
| `--on-ink` | `#f7f8f5` | `#14171a` | Mürekkep zemin üstündeki metin |
| `--line` / `--line-strong` | `#e2e4df` / `#cdd1ca` | `#272c31` / `#394045` | Kenarlık |

### Akış ve durum

`--lost` (metin güvenli) · `--lost-vivid` (zemin) · `--lost-tint` (yumuşak zemin) · `--lost-edge` (kenar)
Aynı dörtlü `--found` için de vardır. Durum renkleri: `--open`, `--claimed`, `--closed`, `--danger`
ve her birinin `-tint` eşi.

### Biçim

`--r-sm: 10px` · `--r: 16px` · `--r-lg: 22px` · `--r-full: 999px`
Gölge: `--shadow-sm` (kart), `--shadow` (havalanan yüzey), `--shadow-lg` (üstüne gelince).

---

## 3. Tipografi

| Rol | Yüzey | Ağırlık | Kullanım |
|---|---|---|---|
| Başlık | **Outfit** | 700–800 | h1–h4, marka, sayaç rakamları |
| Gövde | **Plus Jakarta Sans** | 400–800 | Paragraf, etiket, düğme |
| Veri | **DM Mono** | 400–500 | Zaman damgası, adım sayacı, küçük büyük harfli etiketler |

Başlıklarda `letter-spacing: -0.02em`…`-0.035em` ve `text-wrap: balance`.
Küçük büyük harfli etiketlerde (`.eyebrow`, `.filter-label`) `letter-spacing: 0.06em`…`0.1em`.
Yazı tipleri `index.html` içinde `<link>` ile yüklenir; her birinin gerçek bir yedek yığını vardır.

---

## 4. Tema

Üç durum vardır ve üçü de tasarlanmıştır:

1. **Sistem (varsayılan)** — kökte işaret yok, `prefers-color-scheme` geçerli.
2. **Açık** — `<html data-theme="light">`.
3. **Koyu** — `<html data-theme="dark">`.

CSS düzeni buna göre kurulur: tam açık palet çıplak `:root` içinde; koyu palet hem
`@media (prefers-color-scheme: dark)` altında `:root:not([data-theme='light'])` ile hem de
`:root[data-theme='dark']` ile yeniden tanımlanır. **Hiçbir renk yalnızca medya sorgusu
içinde tanımlanmaz** — aksi hâlde işaretsiz durumda sayfa bir temanın metnini diğerinin
zemininde gösterir.

Tercih `ThemeService` ile yönetilir ve `localStorage`'a yazılır. `index.html` içindeki küçük
satır içi betik, tercihi **ilk boyamadan önce** uygular; böylece tema atlaması olmaz.
Depolama kapalıysa sessizce sistem tercihine düşülür.

---

## 5. Bileşen kuralları

- **Dokunma hedefi:** düğme ve girdilerde en az `48px` yükseklik; süzgeç etiketlerinde `40px`.
- **Odak:** her etkileşimli öğede `:focus-visible` ile `3px` mürekkep halka. Asla kaldırılmaz.
- **Kart:** `--r-lg` köşe, hairline kenar, üstüne gelince 4px yükselme + görselde `1.045` yakınlaşma.
- **Süzgeç etiketi (`.pill`):** `button` öğesidir, durumu `aria-pressed` ile taşır — `.is-active`
  gibi yalnız görsel sınıflarla değil.
- **Yatay şerit (`.scroller`):** kaydırma çubuğu gizli, `scroll-snap` açık. Dar ekranda süzgeç
  satırları böyle akar; sayfa gövdesi hiçbir zaman yatay kaymaz.
- **Alt sekme çubuğu:** yalnız `≤720px`. Üst çubuktaki gezinme orada gizlenir; ikisi aynı anda
  görünmez. `env(safe-area-inset-bottom)` ile çentikli ekranlara uyar.
- **Buzlu cam:** `@supports` ile korunur; desteklemeyen tarayıcıda çubuklar tam opak kalır.
- **Hareket:** yalnız 120–320 ms arası küçük geçişler. `prefers-reduced-motion: reduce`
  altında tüm animasyon ve geçişler kapanır.

---

## 6. Metin dili

- **Sen dili.** "Kaybettiğin eşyayı bildir", "Ne buldun?", "Sana nasıl ulaşsınlar?"
  Resmî "siz" kullanılmaz.
- **Kısa ve somut.** Düğme ne yapacağını söyler: "İlanı yayınla", "Süzgeçleri temizle".
- **Boş ve hata durumları yol gösterir**, özür dilemez: "Buna uyan ilan yok — süzgeçleri
  biraz gevşetmeyi dene."
- Kullanıcıya görünen tüm metinler Türkçedir. Terim sözlüğü: **ilan**, **pano**, **eşleşme**,
  **sahiplenildi**, **kaybettim**, **buldum**.

---

## 7. Erişilebilirlik

- Sayfa başında "İçeriğe geç" bağlantısı; yalnız odaklanınca görünür.
- Kategori ve konum ikonları `aria-hidden`, anlam her zaman metinde.
- Sihirbaz ilerlemesi `role="progressbar"` ve `aria-valuenow` ile duyurulur; etkin adımda
  `aria-current="step"` vardır.
- Sihirbazın son adımındaki canlı önizleme `inert`'tir: tıklanamaz, odak almaz.
- Hata kutuları `role="alert"`; sonuç sayacı `aria-live="polite"`.

---

## 8. Değiştirirken

1. Önce jetona bak. Yeni bir renk gerekiyorsa jeton ekle, bileşene sabit kod yazma.
2. Yeni bir renk eklerken **üç tema bloğunun üçünü de** güncelle.
3. Bir bileşen `4kB` CSS'i aşıyorsa ortak deseni `styles.css`'e taşımayı düşün
   (üretim bütçesi: uyarı `8kB`, hata `12kB`).
4. Değişikliği açık ve koyu temada, `390px` ve `1280px` genişlikte gör.
