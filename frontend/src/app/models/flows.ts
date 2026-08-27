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
  title: 'Eşyamı kaybettim',
  lede:
    'Kampüste kaybettiğiniz eşyayı üç adımda bildirin. Aynı yer ve kategoride bulunan açık ilanlar detayda size önerilir.',
  editTitle: 'Kayıp ilanını güncelle',
  editLede: 'Kaybettiğiniz eşyanın bilgilerini düzeltin. Tür değişmez; bu bir kayıp ilanıdır.',
  cta: 'Kayıp ilanını yayınla',
  ctaBusy: 'Kayıp ilanı kaydediliyor…',
  steps: [
    {
      title: 'Ne kaybettiniz?',
      hint: 'Kart, anahtar, telefon gibi net bir başlık yazın.',
    },
    {
      title: 'Son nerede gördünüz?',
      hint: 'Kampüs konumunu listeden seçin. En son gördüğünüz yeri açıklamada da belirtebilirsiniz.',
    },
    {
      title: 'Size nasıl ulaşalım?',
      hint: 'İletişim, ilan açıkken gizli kalır; yalnızca sahiplenilince görünür.',
    },
  ],
  titleLabel: 'Kayıp eşyanın başlığı',
  titlePlaceholder: 'Örn. Mavi öğrenci kartı',
  descriptionLabel: 'Nasıl kaybettiniz?',
  descriptionPlaceholder: 'Son gördüğünüz yer, saat ve ayırt edici ayrıntı…',
  categoryLabel: 'Eşya kategorisi',
  locationLabel: 'Son görülen kampüs konumu',
  locationHint: 'Konum listeden seçilir; serbest metin yoktur.',
  contactLabel: 'Size ulaşılacak iletişim',
  contactPlaceholder: 'e-posta veya telefon',
  contactHint: 'Bulan kişi bunu ilan sahiplenilene kadar göremez.',
  photoLabel: 'Fotoğraf adresi (URL)',
  photoHint: 'Dosya yükleme yok. Varsa görsel bağlantısını yapıştırın.',
  reviewKind: 'Kayıp ilanı',
  defaultLocation: FALLBACK_LOCATIONS[1],
  defaultCategory: FALLBACK_CATEGORIES[0],
};

export const FOUND_FLOW: FlowCopy = {
  kind: 'found',
  route: '/buldum',
  badge: 'Buldum',
  title: 'Eşya buldum',
  lede:
    'Kampüste bulduğunuz eşyayı kaydedin. Aynı kategori ve konumdaki açık kayıp ilanları detayda önerilir.',
  editTitle: 'Bulunan ilanı güncelle',
  editLede: 'Teslim bilgilerini düzeltin. Tür değişmez; bu bir bulunan ilanıdır.',
  cta: 'Bulunan ilanı yayınla',
  ctaBusy: 'Bulunan ilan kaydediliyor…',
  steps: [
    {
      title: 'Ne buldunuz?',
      hint: 'Sahibinin tanıyacağı kadar net yazın; içindeki belgeleri okumayın.',
    },
    {
      title: 'Nerede buldunuz, nereye bıraktınız?',
      hint: 'Kampüs konumunu seçin. Güvenlik, danışma veya yemekhane kasa gibi teslim noktasını açıklayın.',
    },
    {
      title: 'Sahibi size nasıl ulaşsın?',
      hint: 'İletişim bilgisi açık ilanlarda gizli tutulur.',
    },
  ],
  titleLabel: 'Bulunan eşyanın başlığı',
  titlePlaceholder: 'Örn. Siyah kulaklık kılıfı',
  descriptionLabel: 'Nereye teslim ettiniz?',
  descriptionPlaceholder: 'Bulunduğu yer ve bıraktığınız nokta (güvenlik / danışma / kasa)…',
  categoryLabel: 'Eşya kategorisi',
  locationLabel: 'Bulunduğu kampüs konumu',
  locationHint: 'Yalnızca kampüs konumları. Serbest adres yazılmaz.',
  contactLabel: 'Teslim için iletişim',
  contactPlaceholder: 'e-posta veya dahili telefon',
  contactHint: 'Sahibi bunu ancak ilan sahiplenilince görür.',
  photoLabel: 'Fotoğraf adresi (URL)',
  photoHint: 'Dosya seçici yoktur; yalnızca bağlantı yapıştırın.',
  reviewKind: 'Bulunan ilan',
  defaultLocation: FALLBACK_LOCATIONS[0],
  defaultCategory: FALLBACK_CATEGORIES[1],
};

export function flowFor(kind: ItemKind): FlowCopy {
  return kind === 'found' ? FOUND_FLOW : LOST_FLOW;
}
