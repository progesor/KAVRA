# 08 — Proje yol haritası / milestone ve kabul koşulları

## M0 — Bootstrap + fizik fizibilite deneyleri

**Bu paket ile hazır (statik kontrol):** repo düzeni, dokümantasyon, UPM,
C# definition/validator, Wizard temel iskeleti.

**Unity'de doğrulanacak:**
- [ ] Proje Unity 6.3 LTS'de Console Error olmadan açılıyor.
- [ ] UPM embedded package görünüyor.
- [ ] Wizard sahneye 3/4/6/8/20 tekerlek anchor layout'u ekliyor.
- [ ] ID/geometry validation yanlış konfigürasyonları engelliyor.
- [ ] `M0_Scaffold.unity` commit edildi.

**M0 fizik deneyleri (henüz başlanmadı):**
- [ ] `Raycast`, `SphereCast` ve gerekli çoklu temas yaklaşımını karşılaştır.
- [ ] Dikey yay–amortisör tek wheel test fixture çalışıyor.
- [ ] Basit boylam/yanal lastik kuvvet modeli ve wheel angular speed.
- [ ] Tek gövdeli dört tekerli düz zeminde ilk dinamik sürüş.
- [ ] Statik wheel load ≈ `m*g` testinin toleransı belirlendi.
- [ ] Fizik telemetri logger ve grafik debug eklendi.
- [ ] En az 3W, 4W, 8W tek gövdeli smoke test.

**M0 çıkış ölçütü:** Wheel dynamics ve Rigidbody entegrasyonunun teknik
fizibilitesi ölçülmüş olacak; temel modelin birim/kararlılık hataları takip
edilebilecek. İlk sürümde tam motor/şanzıman gerekmez; kontrollü wheel torque
girdiyle çalışabilir.

## M1 — Genel amaçlı sürülebilir tek gövdeli sistem

- Değişken wheel count, aks grupları, steering, servis freni.
- Tire/suspension model parametre varlıkları.
- Kontrolcü soyutlaması, bir default input adapter.
- Statik/dinamik ağırlık transferi ve devrilme testleri.
- Basit playable lab aracı + physics performance ölçümleri.

## M2 — Aktarma organları ve sürüş modları

- ICE/elektrik power source, torque curve, inertia.
- Manuel/otomatik vites, clutch, open/locked/LSD diferansiyel.
- ABS/TCS tasarım ve temel uygulama.
- Torque graph validation.

## M3 — Vehicle Wizard üretim sürümü

- Mesh detection, wheel pivot calibration, scene gizmos.
- Input/visual binders, runtime prefab generation.
- Quick setup ve Engineering Mode, settings inspectors.
- Undo, prefab variant ve asset migration politika testleri.

## M4 — Çok gövdeli / ağır taşıt desteği

- Çekici+römork: ayrı Rigidbody/coupling ve römork frenleri.
- Çift tekerlek, farklı aks topolojileri, lift axle, bogie.
- Forklift arka direksiyon ve yük merkezi örneği.

## M5 — Zemin etkileşimi ve arcade erişilebilirlik

- Surface provider, test patch, soft soil kuvvet modeli.
- MudRunner'dan esinlenilmiş ayrı görsel iz/deformasyon adapter'ı.
- Simulation/Sport/Arcade profilleri ve ESC/rollover yardım testleri.

## M6 — Kalibrasyon ve performans

- Lastik modellerinde seçenekler ve referans testleri.
- Profiling, Jobs/Burst yalnızca bottleneck görüldüğünde.
- Çok araçlı sahne, kalite düzeyleri ve replay/regression.

## M7+ — Hasar ve ileri sistemler

- Fonksiyonel lastik/fren/şanzıman hasarı.
- Detachable physical parts, kütle güncelleme.
- İsteğe bağlı geometry deformation / soft-body adapter.
- Platform export, release package / semver sözleşmeleri.

## Sürekli çalışma disiplinleri

- Her milestone için issue, checklist, telemetri ekran görüntüsü ve test kaydı.
- `0.x` boyunca API deneysel; önemli rename'lerde serialized migration.
- Fizik modülleri erken benchmark olmadan optimize edilmez.
- Önce M0 kabul, ardından M1. İlk sürüme M4/M7'nin sahte kodu eklenmez.
