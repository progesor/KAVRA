# 08 — Proje yol haritası / milestone ve kabul koşulları

## M0 — Bootstrap + fizik fizibilite deneyleri

**M0 v0.0.2 kodu önerildi, Unity doğrulaması bekleniyor:** repo, UPM, Wizard temel iskeleti; ilk dinamik raycast tekerlek, süspansiyon, basit lastik, lab generator, testler.

**Unity'de doğrulanacak:**
- [ ] Proje Unity 6.3 LTS'de Console Error olmadan açılıyor.
- [ ] UPM embedded package görünüyor.
- [ ] Wizard sahneye 3/4/6/8/20 tekerlek anchor layout'u ekliyor.
- [ ] ID/geometry validation yanlış konfigürasyonları engelliyor.
- [ ] `M0_Scaffold.unity` commit edildi.

**M0 fizik deneyleri (kod var, Unity fizik/derleme kabulü bekliyor):**
- [ ] `Raycast`, `SphereCast` ve gerekli çoklu temas yaklaşımını karşılaştır.
- [ ] Dikey yay–amortisör tek wheel test fixture Unity Test Runner'da geçiyor (kaynak eklendi).
- [ ] Basit boylam/yanal lastik kuvvet modeli ve wheel angular speed Unity'de doğrulandı (kaynak eklendi).
- [ ] Tek gövdeli dört tekerli düz zeminde ilk dinamik sürüş Play Mode'da doğrulandı (lab generator eklendi).
- [ ] Statik wheel load ≈ `m*g` testinin toleransı belirlendi.
- [ ] Telemetri grafiği ve uzun süreli CSV logger eklendi (Inspector snapshot ve debug ray var).
- [ ] En az 3W, 4W, 8W tek gövdeli Play Mode smoke test.

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
