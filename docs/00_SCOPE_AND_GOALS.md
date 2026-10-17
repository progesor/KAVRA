# KAVRA — Güncel vizyon, kapsam ve başarı hedefleri

**Güncellendi:** 17 Ekim 2026  
**Aktif geliştirme:** M1.5.1 / UPM v0.1.6  
**Ana referans:** [Güncel devir raporu](25_2026-10-17_CURRENT_STATE_AND_HANDOVER.md) ·
[Yol haritası](08_ROADMAP_AND_MILESTONES.md)

## 1. Ürün vizyonu

**KAVRA**, Unity 6 üzerinde oyun veya simülasyon projelerine eklenebilen,
parametrik, modüler ve yeniden kullanılabilir bir **Vehicle Framework**'tür.

Bir geliştirici bir şasi, akslar, tekerlekler ve opsiyonel güç aktarımı
tanımlayarak 3W, 4W, 6x6, 8x8, tek gövdeli 20W gibi araçları kurabilmeli;
çekici ve römork ise **ayrı Rigidbody** gövdeleriyle birleştirilebilmelidir.
Sistemin profesyonel kullanımını belirleyen şey yalnızca gerçekçi görünen
sürüş değil, kolay kurulum, güvenilir bağımlılıklar, örnek sahneler,
test edilebilir hesaplar ve taşınabilir prefablardır.

**Platformun ilk hedefi:** Windows / Unity 6.3 LTS proje ekosistemi.
Core runtime UPM, Input System, URP/HDRP, Cinemachine, Blender veya
ücretli model paketlerini **zorunlu bağımlılık** yapmamalıdır.

## 2. Öncelik ilkesi — kullanıcı kararı

17 Ekim itibarıyla üretim stratejisi:
**önce kapsamlı ve düzgün çalışan ANA framework, sonra aşırı ince
fizik kalibrasyonu**. Aşağıdaki sıra tercih edilir:

1. Derlenen ve testten geçen güvenilir çekirdek.
2. Araç tanımları, aks/tekerlek/gövde ilişkileri ve modüler güç aktarımı.
3. N gövdeli bağlantı/hitch yaşam döngüsü, fren/komut aktarımı.
4. Wizard: asset üretimi, mesh pivot bağlama, prefab ve gövde montajı.
5. Farklı araçları açık, okunabilir bir PhysicsLab içinde sürerek doğrulama.
6. Profiler/performans ve sürüş hissindeki bariz problemleri giderme.
7. Gerekli olduğunda daha gelişmiş lastik, clutch, diferansiyel ve
   arazi mekanikleri.

Bir davranış **bariz şekilde yanlışsa veya kullanıcı tarafından
engelleyici hata olarak raporlandıysa**, çekirdek kabul kapsamında
hemen düzeltilir. Fakat küçük sürtünme sapmaları, görsel detayı çok
yüksek bağlantılar ve gerçek ölçümlere dayanmayan milimetrik ayarlar
Wizard ve modüllerin tamamlanmasının önüne geçirilmez.

## 3. Fizik ve kontrol sözleşmeleri

- **1 şasi = 1 Rigidbody**. Gerçek çekici+römork konfigürasyonu
  iki veya daha fazla ayrı fizik gövdesidir.
- WheelCollider ana kuvvet modeli yerine KAVRA'nın kendi ayrıştırılmış
  tekerlek temas ve lastik kuvveti çözümü kullanılır; Unity Rigidbody
  çarpışma ve şasi entegrasyonunun sahibidir.
- Konfigürasyonlar ScriptableObject; mutable per-vehicle runtime state
  ve telemetri ayrı tutulur.
- Fizik hesapları SI birimlerine dayanır. Metrikler ölçülmeden
  belirli otomobil markası için gerçek simülasyon iddiası yapılmaz.
- **Araç kendi kendine düzelmez veya devrilmekten kurtarılmaz.**
  Fizikte doğal yatma, tekerlek kaldırma ve devrilme mümkündür.
  `R` yalnızca kullanıcının bastığı, güvenli koşullarda çalışan
  manuel kurtarma komutudur. Bağlı araçlarda joint koparılmadan
  grup halinde ele alınmalıdır.
- `H` komutu olmadan römork kendiliğinden bağlanmaz. Gerçek joint
  aşırı kuvvette kopabilir; kopan bağlantı yeniden otomatik kurulmaz.
- Arcade/Simulation tercihi **ileride isteğe bağlı giriş/yardım profili**
  olabilir; varsayılan şasi fiziğine gizlice yapay denge kuvveti eklenmez.
- Unity Editor, fizik/host testleri ve package asmdef bağımlılıkları
  birbirinden temiz ayrılmalıdır.

## 4. Ürün özellikleri — istenen nihai sonuç

| Yetkinlik | Hedef | 17 Ekim durumu |
|---|---|---|
| Değişken tekerlek ve aks | 3W/4W/6W/8W/20W şasi verisi, sabit 4-corner indeksi yok | Çekirdekte prototip ve kaynak testler |
| Süspansiyon/lastik/zemin | Tekerlek bazlı fizik ve değiştirilebilir lastik/zemin profilleri | M0 prototipi, düz asfalt kullanıcı telemetrisiyle A/B |
| Güç aktarımı | Motor RPM/tork, gearbox, basit axle torque routing; ileri düzey aktarma takılabilir | M1 basit motor/gearbox kaynakta |
| Tek araç kurulumu | VehicleDefinition, WheelAnchor, hazır dinamik prefab | Wizard/prototip var; imported model akışı geliştirmeli |
| Articulated araçlar | N fizik gövdesi, güvenli hitch, kırılabilir bağlantı | M1.4 kullanıcıca işlevsel bulundu; M1.5 kaynakta |
| Fren/komut hattı | Ayrı bağlı araçlara doğru kontrol; kopunca izole | Basit relay kaynakta; hava freni modeli değil |
| Model/süspansiyon görseli | FBX/GLB tekerlek, mesafe/salıncak/rot pivot bağlama | Pivot+örnek amortisör var; üretim modeli henüz yok |
| Görsel test laboratuvarı | Hareket, kayma, hitch açıklığını kolay gözleme | Checker ve renkli marker M1.5.1 kaynakta; Unity kabul bekliyor |
| Paket taşınabilirliği | Projeye wizard üzerinden az manuel adımla ekleme | Embedded UPM var; bitmiş dağıtım akışı değil |
| Performans | 3–20+W, çoklu araçta GC ve CPU kontrolü | İlk cast ölçümleri var; sistematik profiler acceptance sonra |

**Not:** “Kaynakta var” ifadesi Unity derlemesi, Test Runner ve
Play Mode'un o sürümde başarıyla tamamlandığı anlamına gelmez.
Son M1.5.1 asmdef düzeltmesi özellikle kabul edilmelidir.

## 5. Ölçülebilir kalite / kabul hedefleri

Bunlar ürünün **hedef kabul kriterleri** olup hepsinin geçtiği
iddiası değildir:

- Unity 6000.3.x üzerinde örnek host projesi **Console C# error = 0**;
  Test Runner EditMode suiteleri PASS.
- Unity sahnelerinde 4W, 6x6, 8x8 ve en az bir gerçek çok gövdeli
  çekici/römork dizilimi sürülebilir, frenleyebilir, temel yönlendirme
  ve geri vites davranışı gözlenebilir.
- 6x6 + 4W + 4W römork zincirinde üç bağımsız Rigidbody ve iki
  gerçek ConfigurableJoint kalıcı/seri hale getirilebilir.
- Bağlantı kopması yalnızca gerçek joint solver eşiklerinden veya
  kullanıcının manuel H komutundan; otomatik yeniden bağlama yok.
- Varsayılan H bağlama toleransı 0.5m/55° ile erişilebilir; yüksek
  bağıl hızda reddedilir. Kullanıcı asset'e yazdığı özel değerler
  yeniden kurulumda kaybolmaz.
- Wizard gerçek araç modelini ve tekerlek pivotsunu analiz ederek
  doğru asset/prefab hiyerarşisi üretmeye evrilir.
- Repo içinde ve başka bir demo Unity projesinde prefab/assembly
  referansları kaybolmadan yeniden açılır.
- Önceden raporlanan fren salınımı gibi **bariz bozuk fizik** senaryoları
  telemetriyle karşılaştırılabilir şekilde takip edilir.
- Performans hedefleri ancak **aynı donanım, aynı fixed timestep
  ve aynı senaryo** ölçüldükten sonra bütçeye dönüştürülür.

## 6. Kapsam sınırları / bilinçli ertelemeler

**Bu fazın dışında:** lastik FEM, gerçek zamanlı hacimsel toprak
deformasyonu, tüm süspansiyon kol mafsallarının ayrı fizik çözümü,
node-beam/soft-body hasar, gerçek marka özel validasyon, uçtan uca
netcode/deterministic lockstep, tüm araçların bitmiş sanat içeriği,
tam otomatik debriyaj/torque converter, üretim kalitesinde air brake.

**İleride uygun olduğunda:** gerçek 18 tekerli (gövdelere bölünmüş)
tır ve farklı araç sınıfları; yük/COM dinamiği, doğru differential
constraint/ABS/TCS, off-road zemin eklentileri, araç seviyeleri/
CPU LOD, gerçekçi süspansiyon/tekerlek görselleri, oyun için
isteğe bağlı arcade parametre profili.

Ayrı düşük öncelikli iş:
[**#9 — gerçekçi demo araç modelleri**](https://github.com/progesor/KAVRA/issues/9).
Model araştırılırken ayrı wheel, suspension, axle, rod mesh ve pivotlar,
lisans/ücret ve FBX/Blender→Unity uyumu kontrol edilecek; satın alınan
kapalı lisanslı modelin raw dosyaları açık GitHub'a konmayacak.

## 7. Geliştirme disiplini

1. Bir değişiklik için tek aktif feature branch ve takip issue/PR.
2. Gerçek Unity Editor erişimi yoksa **statik kaynak testi ≠ Unity PASS**.
3. Yeni feature'a geçmeden önce son commitin Console ve Test Runner
   sonucu mümkün olduğunca alınmalı; mevcut kırmızı derleme hataları
   temizlenmeli.
4. M0/M1 kullanıcı .unity sahneleri, `docs/Telemetry/` CSV'leri
   ve geçerli özel ayarlar **silinmeden** korunur.
5. Mümkün olan her kullanıcı raporuna dar kapsamlı regresyon testi
   ve kısa hata nedeni eklenir.
6. PR zinciri (M0 → M1 → M1.4 → M1.5) kabul edilmeden `main`
   üzerinde doğrudan özellik geliştirilmez.
7. Tek kaynak güncel yol haritası: [docs/08](08_ROADMAP_AND_MILESTONES.md);
   yeni sohbet başlangıcı: [docs/25](25_2026-10-17_CURRENT_STATE_AND_HANDOVER.md).

_Önceki 13 Ekim vizyon ve kapsam metni tarihçe için
`docs/00_SCOPE_AND_GOALS_ARCHIVE_2026-10-13.md` altında saklanmıştır._
