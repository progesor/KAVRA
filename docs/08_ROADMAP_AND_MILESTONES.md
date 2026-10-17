# KAVRA — Güncel yol haritası, teslim hedefleri ve kabul kriterleri

**Tek güncel roadmap — 17 Ekim 2026**  
**Aktif dal:** `feat/m15-multitrailer-chain` · **Kaynak paket:** v0.1.6  
**Bağlantılar:** [Vizyon](00_SCOPE_AND_GOALS.md) ·
[Yeni sohbet/devir](25_2026-10-17_CURRENT_STATE_AND_HANDOVER.md)

> **Hedef değişimi:** Tek tek küçük lastik/süspansiyon ayrıntılarını sonsuza
> kadar incelemek yerine **modüler ve kullanılabilir ana Vehicle Framework**'ü
> tamamla. Fiziksel olarak bariz hatalı veya derlemeyi engelleyen meseleler
> istisnadır; bunlar gecikmeden düzeltilir.
>
> **Durum anahtarı:** **Kaynakta var** = kod yazılıp commitlenmiş;
> **Kullanıcı tarafından gözlendi** = yerel bir davranışa ilişkin doğrudan
> geribildirim/CSV var; **Unity kabulü bekliyor** = gerçek derleme, test veya
> Play Mode sonucu elde edilmedi. İşaretli kaynak görevi,
> Unity acceptance PASS anlamına **gelmez**.

## 0. Güncel karar tablosu

| Düzey | Ne yapılacak? | Bu aşamanın önüne geçmeyecekler |
|---|---|---|
| **P0 — hemen** | M1.5.1 son Unity/asmdef derleme kabulü, EditMode, checker/hitch görünümü, eski sistem regresyonu | Yeni zincir grafiği, karmaşık shader ve fizikte ince tune |
| **P1 — temel mimariyi sağlamlaştır** | Core/runtime/host sözleşmeleri, powertrain & assembly API, sabit kimlikler, veri doğrulama, prefab roundtrip | Tam diferansiyel kilit mekaniği, lastik FEM |
| **P2 — Wizard ve görsel model bağlama** | Imported mesh/pivot/seçilebilir modeller, kalıcı prefab, kullanım akışı, üretim sahnesi | Sanat içeriğinde sınırsız çeşit, gerçek mühendislik amortisörü |
| **P3 — oyun için gerekli mekanikler** | Engine/gearbox/diff/brake temel kalitesi, yük transferi, ağır araç/aktif aks seçenekleri | Şimdiden tüm araç markalarının birebir doğruluğu |
| **P4 — genişleyen modül topolojisi** | Daha genel hitch portları, zincirden modül kompozisyonuna evrilme, hedef gerçek 18W | Dallanan bağlantı grafiğini P0 tamamlanmadan yazmak |
| **P5 — performans ve ürünleşme** | 4W–20W+ ölçüm, çok gövde profil, allocation, Unity UPM release/örnekler | Ölçülmemiş FPS/araç sayısı vaatleri |
| **P6 — sonraki opsiyonel eklentiler** | Soil, deformasyon, hasar, özel araçlar, opt-in Arcade, ağ adapterleri | Core'u bu sistemlere bağımlı kılmak |

Bunlar **iş sırası**, teslim tarihi/hafta taahhüdü değil. İşin
ilk sırası gerçek Unity feedback'ine göre değişebilir.

## 1. Şimdiye kadar neler üretildi?

| Faz | Kodda olan | Kullanıcı/Unity delili | Açık kalan |
|---|---|---|---|
| **M0 0.0.1–0.0.4** | Unity UPM, tekerlek/aks veri modeli, yay/sönüm, temas, fren, R manual recovery, PhysicsLab | İlk araç Play Mode ve M0 asset/sahne kayıtları | Sistematik tüm teker sayıları ve zemin acceptance |
| **M0.5 0.0.5–0.0.6** | Ackermann-benzeri steering, slip telemetry, tester, düşük hız fren impuls limiti | **4 eski CSV + 1 A/B CSV**, düz asfalt fren salınımı çok azaldı | Farklı lastik/zemin/aks kompozisyonlarında regresyon |
| **M1 0.1.0–0.1.1** | SimplePowertrain motor eğrisi, vites, aks bazlı tork paylaşımı, Wizard M1/prefab, 3–20W örnekler | Kaynak/test dosyaları var | Gerçek her layout'un TestRunner + Play kabulü |
| **M1.2 0.1.2** | Vites state machine, manuel/otomatik, wheel rig / shock görselleri | Kaynak var | Editor mesh-binding/prefab smoke |
| **M1.3–M1.4 0.1.3–0.1.4** | İki Rigidbody, gerçek ConfigurableJoint, H attach/detach, breakForce/torque, trailer brakes, R whole-body | Kullanıcı **bağlama/ayırma/kopmanın çalıştığını** bildirdi | Reimport/sistematik test, farklı kütle/joint stabilitesi |
| **M1.5 0.1.5** | 1–16 linkli zincir, N-body root, frenlerin bağlı zincirle sınırlanması, R connected prefix, 14W lab | Kaynak + EditMode testleri | Gerçek 14W towing, R ve kırılan link testi |
| **M1.5.1 v0.1.6** | 0.5m/55° H, checksum değil asset migration, checker texture, renkler, hitch guide, FOV, asmdef fix | Hata geri bildirimi üzerine **NRE e30c** ve **CS0246 48d85** kaynakta düzeltildi | **Son commit sonrası Unity compile/TestRunner ve görsel kabul** |

**PR bağımlılık sırası:**
[PR #2](https://github.com/progesor/KAVRA/pull/2) (M0 → `main`)
→ [PR #8](https://github.com/progesor/KAVRA/pull/8) (M1 → M0)
→ [PR #13](https://github.com/progesor/KAVRA/pull/13) (M1.4 → M1)
→ [PR #16](https://github.com/progesor/KAVRA/pull/16) (M1.5 → M1.4).

Tüm PR'lar **Draft**, henüz merge yok. Akıllı test/merge
sırası dışına çıkılmamalı.

## 2. P0 — Kırmızı derleme engellerini kapat, sürüşünü doğrula

**Çıkış ölçütü:** v0.1.6 son kaynak, Unity 6000.3.18f1'de açılıp
testlerden geçiyor ve lab sürüşünde fonksiyonel görünüyor.

- [x] `e30c32c`: `VehicleLabHitchGuide.Setup()` önce
  `MaterialPropertyBlock` oluşturacak şekilde düzeltildi.
- [x] `48d8528`: `VehicleLabHitchGuide` bağımsız
  `Progesor.Kavra.Lab.HitchVisuals.asmdef` assembly'sine taşındı;
  script GUID korundu, EditMode test referansı eklendi.
- [ ] Unity Console'da `CS0246`, `NullReferenceException` veya başka
  derleme/başlatma hatası yok.
- [ ] EditMode Test Runner: `KavraPhysicsEditModeTests`,
  `KavraAssemblyEditModeTests`,
  `KavraHitchLifecycleEditModeTests`,
  `KavraAssemblyChainEditModeTests` PASS.
- [ ] `Tools > KAVRA > Create M1.5 Two-Trailer Lab Scene` üretirken
  checker zemin, 15 m referanslar, ayrı araç renkleri ve
  seçili hitch marker/ring/line görünüyor.
- [ ] Geri düşük hızda H ile yaklaşık 0.40 m mesafeden bağlanma;
  0.5 m dışı, fazla açı veya fazla hızda reddetme.
- [ ] J/L seçimi, H manuel linki ayırıp tekrar bağlama,
  K gerçek solver kopması; bağlı fren akışı/doğru kopma.
- [ ] R yalnızca çekiciye bağlı grup için, kullanıcının isteğiyle;
  römork kopmuşsa kopuk bölüm sabit kalır.
- [ ] Aynı branch'te M1.4 tek römork laboratuvarı ve eski M0
  test sahnesi bozulmadan açılır.
- [ ] **Yalnızca test delili varsa** sonucu docs/25 ve issue #17'ye işleyip
  P0'ı kapat. Tekrar tüm eski CSV'leri isteme.

İlgili: [#17](https://github.com/progesor/KAVRA/issues/17) ·
[docs/24](24_M151_PHYSICSLAB_VISIBILITY.md) ·
[docs/25](25_2026-10-17_CURRENT_STATE_AND_HANDOVER.md).

## 3. P1 — Çekirdeği ve modül arabirimlerini sağlamlaştır

**Ürün hedefi:** Başka bir Unity projesi KAVRA'yı ekleyip, kopyalanmış
host test sınıflarına mecbur kalmadan mevcut otomobil/çok akslı
araç/çekici prefabını kullanabilmeli.

- [ ] Runtime/Editor/host asmdef sınırlarını ve referans grafiğini test et;
  her yeni test sınıfını yanlışlıkla `Assembly-CSharp`a bağlama.
- [ ] `VehicleDefinition`, `SimplePowertrainDefinition`,
  `VehicleAssemblyDefinition`, zincir asset'leri için
  migration/validator ve düzenleme güvenliği kabul testleri.
- [ ] Basit PowertrainRuntime → WheelSolver tork API'si net;
  3W, 4W, 6W, 8W, 20W teker sayısı koduna sabit varsayım ekleme.
- [ ] `VehicleAssemblyRoot` iki-gövde ve
  `VehicleAssemblyChainRoot` çok-gövde uyumunu koruyarak
  daha ortak `VehicleAssembly` kontrol API'si tasarla.
- [ ] `H`/kopma/yeniden bağlama, yerinde kalmış stale joint,
  Linked Rigidbody ve prefab serileştirme sorunları için
  küçük tekrarlanabilir kabul senaryosu.
- [ ] Başarılı kabulden sonra PR zincirinin merge planı
  (M0 → M1 → M1.4 → M1.5); hiçbiri körlemesine `main`e itilmez.

**Çıkış:** En az bir yeni temiz Unity proje/sahnede reused prefab
assetleri bozulmadan çalışıyor; Unity/UPM bağımlılıkları belgeli.

### P1 kaynak ilerlemesi — 17 Ekim 2026

- **Kaynakta eklendi:** `IVehicleAssemblyController`; iki gövdeli ve
  sıralı çok-römork köklerinin ortak, geriye uyumlu link/attach API'si.
- **Kaynakta eklendi:** iki EditMode regresyon testi, yeni Python
  asmdef/GUID preflight kontrolü ve GitHub Actions statik doğrulama.
- **Açık:** gerçek Unity derleme, EditMode PASS, prefab yeniden importu,
  sahne görünümü ve Play Mode. P0 kabulü ve issue #17 kapanmadı;
  PR #16 Draft. Ayrıntı: [docs/26](26_P1_COMMON_ASSEMBLY_CONTROL_API.md).

## 4. P2 — Vehicle Wizard üretim akışı ve model bağlama

Kullanıcı isteği: ileride gerçek bir **açık şasi** otomobil, 4x4 ve
6x6/8x8 araçlar bulalım; farklı tekerlek/diferansiyel/süspansiyon
parçaları görsel olarak hareket etsin. **Düşük öncelik modeller**
[#9](https://github.com/progesor/KAVRA/issues/9).

- [ ] Wizard'da yeni araç türü + aks/süspansiyon setup, monte edilen
  pivotların görsel önizlemesi ve hızlı hata açıklaması.
- [ ] FBX/GLB/Blender mesh import: ön/arka/right/left wheel pivot,
  yerel +X/+Y/+Z konvansiyonları ve drive/suspension kalibrasyonu.
- [ ] Son kullanıcı meshleri ile mevcut
  `TravelPivot/SteeringPivot/SpinPivot` ve
  `VehicleSuspensionVisualLink` esnek bağlaması.
- [ ] Prefab, prefab variant, ScriptableObject referanslarının sahne
  değiştirip yeniden import edildiğinde korunması.
- [ ] Renderer/URP/HDRP ve Input System bağımsız core; lab art
  `Assets/VehiclePhysicsLab` altında kalmalı.
- [ ] Model lisanslarının tekrar dağıtım yetkisi; lisanslı raw
  assetleri açık repoya gömmeme.

**Çıkış:** Kullanıcı kendi modelini içe alıp makul, sınırlı sayıda
Wizard adımıyla tekerlekli çalışan araç prefabı üretebiliyor.

## 5. P3 — Basit tahrikten genişletilebilir araç mekaniğine

- [ ] Mevcut `SimplePowertrainRuntime`ın yanına gerçek
  clutch/torque-converter/gear response için değiştirilebilir
  aktarma organı sözleşmesi (önce sürülebilirlik blockerları).
- [ ] Gerçek open / kilitli / limited-slip diferansiyel, hız
  kısıtı/reaksiyonları açısından açık ayrı modüller.
- [ ] ABS/TCS (opsiyonel), servis/park freni, römorklarda
  basit fren gecikmesi/komut iletimi, yüklü araç ivme/fren testi.
- [ ] CoM, yük, trailer ağırlık dağılımı ve farklı axle counts.
- [ ] Optional sim/arcade ayar profili; **gizli oto-upright yok**.

**Çıkış:** Sim/Road/Truck profillerinin neyin fizik, neyin kullanıcı
kontrol/yardım olduğunu ayrıştırması; kabul testleri kaynak ve Play Mode.

## 6. P4 — Ağır araç ve daha genel bağlantı topolojisi

- [ ] Gerçek 18 tekerli articulated rig; teker ve aks sayıları
  gerçek iki/çok gövdeye dağıtılacak, **tek 18W rigidbody gibi gösterilmeyecek**.
- [ ] M1.5 lineer zincir üzerinden seçilebilir hitch portları ve
  farklı trailer çekme başlıkları; gerçekten gerekirse branching graph.
- [ ] Çoklu trailer tipi, modüler fren/güç/sinyal bağlantısı
  (basit network modeli daha sonra).
- [ ] Kingpin/drawbar ayrımı, uygun limit/bağlantı gücü validasyonu.
- [ ] Gerçek loader/tractor/forklift yönlendirme varyantları.

**Çıkış:** İkinci bir gövdeli araç tipi, düzgün ayrılma/kopma
ve prefab yeniden kullanımından başarıyla geçiyor.

## 7. P5 — Ölçüm, paket kalitesi ve ilk sürüm adayı

- [ ] Karşılaştırmalı Unity Profiler: 4W, 6W, 20W tek gövde ve
  çok trailer; tek araç ve çoğaltılmış lab senaryoları.
- [ ] Contact query ve FixedUpdate CPU bütçesi, GC/frame
  allocation ve component lifecycle hataları raporlanır.
- [ ] EditMode+PlayMode regresyon altyapısı, mümkünse Unity batchmode
  veya GitHub Actions CI. CI kurulmadan `PASS` iddia edilmez.
- [ ] Unity 6.3+ paket/version/asmdef/UPM dağıtım kalitesi, örnek prefab
  ve örnek host'ların ayrılığı.
- [ ] Release-notes, basit tutorial, kontroller,
  her özellik için bilinen sınırlamalar.

**Çıkış:** Test runner temiz, örnek projelerde kurulum tariflenebilir,
ölçülmüş (uydurulmamış) performans; güvenli release candidate.

## 8. P6 — bilinçli olarak şimdilik ertelenenler

- Derin lastik deformasyon / yüksek ayrıntı brush/FEM,
  gerçek zamanlı toprak çamuru deformasyonu.
- Komple mekanik suspension-joint grafı ve soft-body hasar.
- Tam network replication/multiplayer deterministic lockstep.
- Her araç modelinin bitmiş animasyon/ses ve marka/model dataları.
- Tam donanım platform matrisi ve PC dışı yüksek ölçekli benchmark.

Bu maddeler gerçek ürün ihtiyacı doğurursa ayrı milestone/issue
olarak açılabilir; çekirdek bağımlılığına zorunlu olarak girmez.

## 9. Definition of Done — her kilometre taşında

Bir görev **tamamlandı** diye işaretlenebilmesi için:

1. Hedeflenen feature ve öncesinin kaynakları güncel feature dalına
   commitlenmiştir; kullanıcı assetleri ve önceki veri kaydı korunur.
2. EditMode/API seviyesinde anlamlı regresyon testleri kaynakta vardır.
3. Unity derleyici/API uyumluluğu gerçek Unity ortamında doğrulanmış,
   Console kırmızı hata yoktur.
4. Gerekiyorsa örnek sahnede Play Mode davranışı gözlenmiş ve
   zorlanan senaryoda başarısızlık açıkça yazılmıştır.
5. Doküman ve kullanım tarifinin karşılığı çalışan menü/komutla
   eşleşmektedir. PR hâlâ doğrulanmamışsa **Draft** kalır.
6. Sürüm numarası, kaynak README, changelog ve bu roadmap tutarlıdır.
7. Yeni bir dal açılmadan önce önceki kabul engeli bilerek
   kapatılmış veya açık risk olarak devredilmiştir.

Mevcut **source complete** kutuları, gerçek Unity kabulünün yerine
geçmez.

## 10. Takip kaynakları ve tarihçe

- Ürün vizyonu: [docs/00](00_SCOPE_AND_GOALS.md)
- Kapsam/ilk gereksinimler: [docs/02](02_REQUIREMENTS_AND_USE_CASES.md)
- Gerçek M0 fren telemetrisi: [docs/17](17_M05_TELEMETRY_REVIEW_2026-10-14.md)
- Fizik kalite planı: [docs/09](09_VALIDATION_AND_BENCHMARKS.md)
- M1.5 kaynak ayrıntısı: [docs/23](23_M15_MULTI_TRAILER_CHAIN.md)
- M1.5.1 checker/hitch + Unity hotfix: [docs/24](24_M151_PHYSICSLAB_VISIBILITY.md)
- En güncel devir: [docs/25](25_2026-10-17_CURRENT_STATE_AND_HANDOVER.md)
- M1.4 [Issue #14](https://github.com/progesor/KAVRA/issues/14),
  M1.5 [#15](https://github.com/progesor/KAVRA/issues/15),
  M1.5.1 [#17](https://github.com/progesor/KAVRA/issues/17)
- Önceki art arda eklemeli milestone notları kaybolmasın diye
  `docs/08_ROADMAP_ARCHIVE_TO_2026-10-17.md` altında arşivlendi.

**Özet:** Bir sonraki gerçek adım yeni feature kodu değil,
**son M1.5.1 assembly düzeltmesini Unity Editor'da kabul etmek**.
Sonrasında P1/P2 alanlarının öncelikli geliştirmelerine dönülecek.
