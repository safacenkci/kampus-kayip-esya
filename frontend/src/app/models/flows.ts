import { ItemKind } from './item';
import { FALLBACK_CATEGORIES, FALLBACK_LOCATIONS } from './catalog';

export interface FlowStepCopy {
  title: string;
  hint: string;
}

export interface FlowCopy {
  kind: ItemKind;
  route: string;
  badge: string;
  title: string;
  lede: string;
  editTitle: string;
  editLede: string;
  cta: string;
  ctaBusy: string;
  steps: [FlowStepCopy, FlowStepCopy, FlowStepCopy];
  titleLabel: string;
  titlePlaceholder: string;
  descriptionLabel: string;
  descriptionPlaceholder: string;
  categoryLabel: string;
  locationLabel: string;
  locationHint: string;
  contactLabel: string;
  contactPlaceholder: string;
  contactHint: string;
  photoLabel: string;
  photoHint: string;
  reviewKind: string;
  defaultLocation: string;
  defaultCategory: string;
}

export const LOST_FLOW: FlowCopy = {
  kind: 'lost',
  route: '/kaybettim',
  badge: 'Kaybettim',
  title: 'Bir şeyini mi kaybettin?',
  lede: 'Üç kısa adımda ilanını aç. Aynı yerde ve aynı kategoride bulunmuş açık ilanlar detay sayfanda karşına çıkar.',
  editTitle: 'Kayıp ilanını güncelle',
  editLede: 'Bilgileri düzelt ve kaydet. Tür değişmez; bu bir kayıp ilanı.',
  cta: 'İlanı yayınla',
  ctaBusy: 'Yayınlanıyor…',
  steps: [
    {
      title: 'Ne kaybettin?',
      hint: 'Kart, anahtar, kulaklık… Sahibini tanıtacak kadar net yaz.',
    },
    {
      title: 'En son nerede gördün?',
      hint: 'Kampüs konumunu seç. Daha ayrıntılı yeri açıklamaya da yazabilirsin.',
    },
    {
      title: 'Sana nasıl ulaşsınlar?',
      hint: 'Bu bilgi ilan açıkken gizli; yalnızca sahiplenilince görünür.',
    },
  ],
  titleLabel: 'İlan başlığı',
  titlePlaceholder: 'Örn. Mavi öğrenci kartı',
  descriptionLabel: 'Nasıl kaybettin?',
  descriptionPlaceholder: 'En son gördüğün yer, saat ve ayırt edici bir ayrıntı…',
  categoryLabel: 'Kategori seç',
  locationLabel: 'Kampüs konumu',
  locationHint: 'Konum listeden seçilir; serbest adres yazılmaz.',
  contactLabel: 'İletişim bilgin',
  contactPlaceholder: 'e-posta ya da telefon',
  contactHint: 'Bulan kişi bunu, ilan sahiplenilene kadar göremez.',
  photoLabel: 'Fotoğraf bağlantısı',
  photoHint: 'Dosya yükleme henüz yok. Varsa görselin bağlantısını yapıştır.',
  reviewKind: 'Kayıp ilanı',
  defaultLocation: FALLBACK_LOCATIONS[1],
  defaultCategory: FALLBACK_CATEGORIES[0],
};

export const FOUND_FLOW: FlowCopy = {
  kind: 'found',
  route: '/buldum',
  badge: 'Buldum',
  title: 'Bir şey mi buldun?',
  lede: 'Bulduğunu panoya as, sahibi sana ulaşsın. Aynı yerde ve kategoride kaybedilmiş açık ilanlar detay sayfanda listelenir.',
  editTitle: 'Bulunan ilanı güncelle',
  editLede: 'Teslim bilgilerini düzelt ve kaydet. Tür değişmez; bu bir bulunan ilanı.',
  cta: 'İlanı yayınla',
  ctaBusy: 'Yayınlanıyor…',
  steps: [
    {
      title: 'Ne buldun?',
      hint: 'Sahibinin tanıyacağı kadar yaz ama her ayrıntıyı verme; içindeki belgeleri okuma.',
    },
    {
      title: 'Nerede buldun, nereye bıraktın?',
      hint: 'Kampüs konumunu seç. Güvenlik, danışma ya da kasa gibi teslim noktasını açıklamaya yaz.',
    },
    {
      title: 'Sahibi sana nasıl ulaşsın?',
      hint: 'Bu bilgi ilan açıkken gizli; yalnızca sahiplenilince görünür.',
    },
  ],
  titleLabel: 'İlan başlığı',
  titlePlaceholder: 'Örn. Siyah kulaklık kılıfı',
  descriptionLabel: 'Nereye teslim ettin?',
  descriptionPlaceholder: 'Bulduğun yer ve bıraktığın nokta (güvenlik / danışma / kasa)…',
  categoryLabel: 'Kategori seç',
  locationLabel: 'Kampüs konumu',
  locationHint: 'Yalnızca kampüs konumları listelenir.',
  contactLabel: 'İletişim bilgin',
  contactPlaceholder: 'e-posta ya da dahili telefon',
  contactHint: 'Sahibi bunu, ilan sahiplenilince görür.',
  photoLabel: 'Fotoğraf bağlantısı',
  photoHint: 'Dosya seçici yok; yalnızca bağlantı yapıştır.',
  reviewKind: 'Bulunan ilan',
  defaultLocation: FALLBACK_LOCATIONS[0],
  defaultCategory: FALLBACK_CATEGORIES[1],
};

export function flowFor(kind: ItemKind): FlowCopy {
  return kind === 'found' ? FOUND_FLOW : LOST_FLOW;
}
