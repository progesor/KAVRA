# KAVRA — 17 Ekim 2026 Proje Devir ve Güncel Durum Raporu

> **19 Ekim Unity test güncellemesi:** İlk gerçek EditMode sonucu
> **111/117 PASS, 6 FAIL**; statik zemin teması, EditMode joint-break
> callback testleri ve Rigidbody/Transform poz karşılaştırması hedefli
> düzeltildi. Unity test runner'da `-quit` kaldırıldı. Değişiklikler
> sonraki Unity testiyle doğrulanana kadar **P0 kapanmış değildir**.
> Test bitiminde bildirilen olağandışı Editor kapanmasının nedenini
> XML kanıtlamıyor; Editor/crash logu gerekli. Güncel kayıt:
> [docs/28](28_2026-10-19_P0_UNITY_TEST_RESULTS_AND_FIXES.md).
>

> **Yeni sohbet için ana referans belge.** Önce bunu oku; ardından
> [güncel yol haritası](08_ROADMAP_AND_MILESTONES.md),
> [ürün hedefleri](00_SCOPE_AND_GOALS.md) ve
> [son PhysicsLab notları](24_M151_PHYSICSLAB_VISIBILITY.md).
>
> Son GitHub kontrolü: **17 Ekim 2026**. Yeni kod geliştirmeden önce
> **son commitin Unity'de gerçekten derlenip test edildiğini doğrula**.

> **17 Ekim P1 devam güncellemesi (bu devir notundan sonra):**
> Aktif dalın yeni HEAD'i `f73629fa2ccf0860f76bbddf9fa0cb30e9a294d5`.
> M1.5.1 hata düzeltmelerini takiben ortak assembly kontrol API'si ve
> statik CI eklendi. Son HEAD için iki **kaynak-only** GitHub Actions
> kontrolü başarılıdır; **Unity compile/EditMode/Play Mode hâlâ kabul
> bekliyor**. Güncel ek: [docs/26](26_P1_COMMON_ASSEMBLY_CONTROL_API.md).
> Aşağıdaki `48d8528` bilgileri önceki M1.5.1 hotfix'in tarihsel
> kaydıdır; PR #16 Draft ve issue #17 açık kalır.
>
## 1. Tek bakışta

| Alan | Güncel durum |
|---|---|
| Repo | https://github.com/progesor/KAVRA |
| Aktif çalışma dalı | `feat/m15-multitrailer-chain` |
| Son özellik/hotfix **kod** commit'i (doküman commit'inden önce) | `48d85280d50142ab4aba031aed7d943dcf7f0c39` |
| Paket | `Packages/com.progesor.kavra`, **v0.1.6** |
| Unity sürümü | **6000.3.18f1 / Unity 6.3 LTS** |
| Aktif gelişim | M1.5.1: çok römorklu zincir + okunabilir PhysicsLab + hitch kullanım kolaylığı |
| GitHub PR | [#16](https://github.com/progesor/KAVRA/pull/16), **Draft** |
| En önemli açık takip | [#17](https://github.com/progesor/KAVRA/issues/17): son asmdef/Unity kabulü |
| Önceki işlevsel geribildirim | Kullanıcı M1.4 çekici-römork **bağlama, ayırma ve kopma** akışının güzel çalıştığını bildirdi |
| Henüz doğrulanmayan | `48d8528` sonrasındaki Unity derlemesi, yeni test assembly'si, M1.5.1 damalı sahne ve görsel doğrulama |
| `main` dalı | Etkilenmedi; feature PR'lar **merge edilmedi** |

**Tek cümlelik hedef:** KAVRA'yı 3–20+ tekerlek düzenleri, değişken akslar,
modüler motor/aktarma organları, ayrı Rigidbody kullanan çok gövdeli araçlar ve
Wizard üzerinden kullanılabilir prefab üretimi sunan, **yeniden kullanılabilir
bir Unity araç framework'ü** haline getirmek.

**Kritik kullanıcı kararı:** Doğal devrilmeler olabilir. **Otomatik düzeltme,
gizli stabilizasyon veya kendi kendine dik durma yok.** `R` yalnızca
kullanıcı tarafından talep edilen, düşük hız/zemin kontrolü olan manuel
kurtarmadır. Simülasyon kalitesi hedefi korunur ancak düşük seviyeli
fizik ayrıntıları ana sistemlerin ilerlemesini bloke etmeyecek.

## 2. Git dalı ve PR zinciri — yanlış dala commit atma

Açık PR'lar birbirini temel alıyor; doğrudan hepsi `main` üzerine
yazılmış bağımsız PR'lar değildir:

| Katman | Dal / PR | Başvuru tabanı | Durum |
|---|---|---|---|
| M0 fizik | `feat/m0-wheel-suspension-prototype` / [PR #2](https://github.com/progesor/KAVRA/pull/2) | `main` | Draft |
| M1 motor, vites, aks, görsel pivotlar | `feat/m1-core-powertrain-foundation` / [PR #8](https://github.com/progesor/KAVRA/pull/8) | M0 dalı | Draft |
| M1.3/M1.4 iki gövde, hitch yaşam döngüsü | `feat/m13-vehicle-assembly-coupling` / [PR #13](https://github.com/progesor/KAVRA/pull/13) | M1 dalı | Draft |
| M1.5/M1.5.1 çok römork, checker lab | `feat/m15-multitrailer-chain` / [PR #16](https://github.com/progesor/KAVRA/pull/16) | M1.4 dalı | Draft |

En son feature dalı HEAD: `48d8528`.
Önceki önemli commitler:

- `d98a683`: kullanıcı tarafından Unity test sahneleri ve **4 gerçek CSV** kaydı
  `docs/Telemetry/` altına eklendi.
- `b70c9e1`: düşük hız fren kuvveti salınımı için fizik düzeltmesi.
- `c278b42`: kullanıcıdan yeni fren A/B Unity kaydı.
- `c8266d0`: gerçek CSV karşılaştırmasının raporu.
- `9ddb779`: M1.2 vites ve wheel visual rig.
- `202461b`: Unity'de olmayan `ConfigurableJoint.enabled` kullanımının düzeltmesi.
- `b9cfae4`: M1.4 manuel bağla/ayır ve fiziksel kopma.
- `90490b4`: M1.5 N-gövdeli sıralı römork zinciri.
- `8798f17`: v0.1.6, 0.50 m / 55° hitch ve checker lab.
- `e30c32c`: `MaterialPropertyBlock` null başlatma sırası düzeltmesi.
- `48d8528`: `VehicleLabHitchGuide` EditMode test derleme/asmdef düzeltmesi.

**Uyarı:** PR'ları kabul testleri yapılmadan topluca merge etme; gerektiğinde
M0 → M1 → M1.4 → M1.5 sırasıyla taşı. Başka daldan gelen değişiklikle
çatışma olursa önce branch HEAD'i tekrar kontrol et.

## 3. Neler var? Uygulandı / test edildi ayrımı

### M0 — tekerlek ve süspansiyon çekirdeği

- Kullanıcıya özel tekerlek kuvveti hesabı; gövde entegrasyonu Unity `Rigidbody`.
- Raycast ve SphereCast fallback temas, lastik kayma/zemin katsayıları,
  yay–amortisör, bump stop, nominal CoM/statik yük, anti-roll aks çifti.
- Tekerlek telemetrisi, Inspector ve CSV logger; asfalt/çakıl/çamur/buz
  `SurfaceProfile` örnekleri.
- Sadece `R` ile manuel kurtarma.
- Eski `LegacyDirectWheelTorque` modu **varsayılan olarak korunuyor**.
- Kullanıcının kendi Unity Play Mode kayıtlarıyla dört tekerli düz asfalt
  fren düzeltmesi A/B ölçüldü. Rapor:
  [docs/17](17_M05_TELEMETRY_REVIEW_2026-10-14.md).

**Sayıyla bilinen bulgu:** düşük hızdaki dört-teker toplam Fx salınımının
%95 mutlak değeri karşılaştırılabilir kayıt penceresinde yaklaşık
**13.94 kN → 0.67 kN**; yön işareti değişimi **243 → 3**.
Bu, belirli düz-asfalt fren senaryosu için gerçek ölçümdür, tüm araçlarda
fizik doğruluğu sertifikası değildir.

### M1–M1.2 — şasi ve güç aktarımı

- `SimplePowertrainDefinition`: RPM–tork eğrisi, ileri/geri/boş vites,
  final drive ve verim.
- `SimplePowertrainRuntime`: tahrikli aks/tekerlek sayılarına göre
  tork paylaştırma; farklı tekerlek sayıları ve per-axle weighting.
- `VehicleGearboxController`: hedef/gerçek vites, manuel/temel otomatik,
  geçiş süresi ve tork kesme.
- `VehicleWheelVisualRig`: fizik sensörü dışındaki travel/steer/spin
  pivotları; opsiyonel `VehicleSuspensionVisualLink`.
- Wizard araç tanımı, deneysel powered prefab, çok akslı lab menüleri.
- **Gerçek clutch/senkromeç, şaft atalet geri beslemesi ve fiziki
  open/LSD/locked differansiyel henüz yok**; mevcut hız-duyarlı yaklaşım
  bir tork-komut paylaştırmasıdır.

### M1.3–M1.4 — çekici + römork

- Farklı `VehicleDefinition` ve bağımsız Rigidbody başına kendi
  `VehicleSimulationRoot`; bağlantı `ConfigurableJoint`.
- XYZ doğrusal eksenler kilitli, yaw serbest, pitch/roll limitli.
- Kullanıcının bilinçli `H` komutu ile düşük hızda ayır/yeniden bağla.
- Bağlantı gücü `Joint.breakForce` / `breakTorque`; gerçek
  `OnJointBreak` ile durum ve kopma sayaçları.
- Kopan bağlantı **kendiliğinden tekrar bağlanmaz**; ayrılan römokun eski
  fren komutları temizlenir.
- M1.4 için kullanıcı olumlu yerel çalışma geribildirimi verdi. Her
  EditMode testinin geçtiğine dair ayrı rapor/çıktı ise burada yok.

### M1.5 / M1.5.1 — gerçek çoklu römork zinciri ve okunabilir lab

- `VehicleAssemblyChainDefinition`: **1–16 bağlantılı sıralı zincir**
  (toplam 2–17 bağımsız gövde). Aynı şasi tanımıyla döngü ve yanlış
  sıralama doğrulayıcı tarafından reddedilir.
- `VehicleAssemblyChainRoot`: seçili bağlantıda `H` ayır/bağla,
  `J/L` ile bağlantı seç, `K` ile seçili joint'e **gerçek fiziksel
  laboratuvar darbesi** uygula. Ayrı bağlantılar ayrı kopar.
- Fren aktarımı yalnızca çekiciye bağlı **kesintisiz ön zincir** boyunca
  işler; kopuk arkasında eski fren komutları temizlenir.
- `R` yalnızca kullanıcı isteğinde çekiciye bağlı alt grubu birlikte
  düzeltir; ayrılmış arka gövdeler yerinde bırakılır.
- Örnek: **6x6 çekici + 4 teker römork A + 4 teker römork B =
  14 teker / 3 Rigidbody / 2 ConfigurableJoint**.
- Yeni H bağlanma varsayılanı **0.50 m ve 55°**; gövde ve hitch hız
  limitleri korunur, kullanıcı ayarları versiyonlu migration ile korunur.
- Yeni PhysicsLab: 3 m damalı yol hücresi, 15 m işaretler, mavi çekici,
  turuncu ve kırmızı römork, parlak işaretler, hizalama halkası, hızla
  hafif genişleyen opsiyonel kamera açısı.

**Bu katmanın Unity Editor derleme/görsel kabulü henüz doğrulanmadı.**
Kullanıcı iki gerçek hata bildirdi; ikisi kaynakta düzeltildi ancak
son düzeltmelerin yerel sonucu henüz paylaşılmadı (bir sonraki bölüm).

## 4. En son iki gerçek Unity hatası ve çözüm statüsü

### A — Editor sahne oluştururken NullReferenceException

Zincir: `VehicleAssemblyChainLabSceneBuilder.Create` →
`VehicleLabArtUtility.DecorateChain` →
`VehicleLabHitchGuide.Setup` → `TintRenderer`.
`MaterialPropertyBlock colorBlock` ilk işaret renklendirilirken henüz
oluşturulmamıştı.

**Kaynak düzeltmesi** `e30c32c`: `Setup` başında `EnsureArrays()`,
`TintRenderer` içinde bağımsız lazy oluşturma; regresyon testi eklendi.

### B — CS0246: VehicleLabHitchGuide tipi EditMode testlerinden görünmüyor

`KavraHitchLifecycleEditModeTests`, açık `.asmdef` içinde derlenirken
hitch guide varsayılan Unity `Assembly-CSharp` içinde kaldığı için test
tipi göremiyordu.

**Son kaynak düzeltmesi** `48d8528`:

- Script yolunu
  `Assets/VehiclePhysicsLab/Scripts/HitchVisuals/VehicleLabHitchGuide.cs`
  olarak taşıdı.
- Scriptin orijinal Unity GUID'si **korundu**:
  `ca1c5b2c99ab46b6b141000000000002`.
- `Progesor.Kavra.Lab.HitchVisuals.asmdef` oluşturuldu;
  `Progesor.Kavra.Runtime` referansına sahip ve autoReferenced.
- `Progesor.Kavra.Lab.EditModeTests.asmdef`, HitchVisuals
  assembly'sini artık **açıkça** referans alıyor.
- Eski script yolu/meta kaldırıldı; bu bir *move*, yeni bağımsız bir
  component GUID'si yaratma değil.

**Statik kontroller geçti**, fakat bu düzeltmenin Unity'de derleme ve
Test Runner sonucu **henüz alınmadı**. Yeni sohbetin ilk işi bu sonucu
görmek; aynı dosyayı körlemesine yeniden değiştirmemek.

## 5. Mimari konumlar ve dosya rehberi

| İhtiyaç | Yer |
|---|---|
| UPM çalışma zamanı | `Packages/com.progesor.kavra/Runtime/` |
| Tanımlar | `Runtime/Definitions/VehicleDefinition.cs`, `SimplePowertrainDefinition.cs`, `VehicleAssemblyDefinition.cs`, `VehicleAssemblyChainDefinition.cs` |
| Fizik/tork/gearbox | `Runtime/Physics/`, `Runtime/Components/VehicleSimulationRoot.cs` |
| Hitch ve zincir | `Runtime/Components/VehicleCoupling.cs`, `VehicleAssemblyRoot.cs`, `VehicleAssemblyChainRoot.cs` |
| Group recovery | `Runtime/Physics/VehicleAssemblyRecovery.cs`, `VehicleAssemblyChainRecovery.cs`, `VehicleManualRecovery.cs` |
| Wizard / sahne, prefab inşa | `Packages/com.progesor.kavra/Editor/` |
| Örnek host PhysicsLab | `Assets/VehiclePhysicsLab/` |
| Yeni checker / renklendirme | `Assets/VehiclePhysicsLab/Editor/VehicleLabArtUtility.cs` |
| Görsel hitch rehberi | `Assets/VehiclePhysicsLab/Scripts/HitchVisuals/VehicleLabHitchGuide.cs` |
| Lab sürüş ve UI | `Assets/VehiclePhysicsLab/Scripts/` |
| Unity testleri | `Assets/VehiclePhysicsLab/Tests/EditMode/` |
| Gerçek kullanıcı ölçümü | `docs/Telemetry/`, özet: `docs/17_...` |

Mimari sınır: core UPM `Runtime` yalnızca UnityEngine tarafına
bağımlı; host Input System, Shader/URP/HDRP, kamera ve test UI sınıfları
çekirdeği kirletmez. Unity EditMode test assembly'si hem core hem
ilgili örnek görsel assembly'sini açıkça referans alır.

## 6. Yeni sohbette İLK görev: mevcut kodu kabul et, önce derlemeyi doğrula

**Yapılacak iş sırası:**

1. GitHub dal HEAD ve PR #16'yı yeniden oku; yerel kullanıcı değişikliklerini
   silmeden güncel dalı esas al.
2. Unity'de `feat/m15-multitrailer-chain` dalında son kaynakları çek:
   `Pull origin`. Unity Console'da **0 derleme hatası** aranır.
3. Test Runner → EditMode:
   `KavraPhysicsEditModeTests`,
   `KavraAssemblyEditModeTests`,
   `KavraHitchLifecycleEditModeTests`,
   `KavraAssemblyChainEditModeTests`.
   Başarısızlık varsa **tam hata + test adı**, sonra hedefli düzeltme.
4. `Tools > KAVRA > Create M1.5 Two-Trailer Lab Scene`:
   damalı zemin, mavi/turuncu/kırmızı araçlar, cyan/sarı bağlantı
   markerları, renkli seçili rehber, 0.5m halkası görünmeli.
5. Play Mode: J/L seçimi, H düşük hızda 40–50cm civarında bağlantı,
   H çok uzak/açı/hız durumunda ret, K PhysX kopma denemesi, R manuel
   sadece bağlı grubun recovery'si. Mevcut M1.4 tek römork sahnesini
   kısa smoke test olarak tekrar aç.
6. M0 kalibrasyonun mevcut CSV kayıtlarını **yeniden üretmeyi isteme**.
   Sadece eski M0 sahnesi bozulmamış mı kontrol et.
7. Sonuç netleşince dokümanlardaki doğrulanma kutularını güncelle,
   ihtiyaç yoksa başka derin fiziğe dalma. Sonra PR'ların birleştirme
   sırasını planla.

**Kısa kullanıcı yükü:** Console çıktısı (varsa) veya EditMode toplam
PASS/fail özeti + damalı/hitch sahnesinden bir görüntü yeterli.
Yeni CSV zorunlu değil.

## 7. Bilinen teknik sınırlar / sakıncalı iddialar

- Mevcut powertrain ilk çalışır tork zinciri; mekanik clutch,
  torque converter, şaft/diferansiyel torque constraint çözücüsü değil.
- `SpeedBiasedApproximation` LSD fiziği değil, yalnızca sınırlı
  wheel tork-komut paylaşımıdır.
- Römork bağlantısı gerçek joint'tir ama mekanik kingpin/aşınma/hava
  frenlerinin ölçülü simülasyonu değildir.
- M1.5 tam grafik değil, **lineer zincir**; aynı asset'i çok
  düğümde tekrar kullanma sınırlandırılmıştır.
- Varsayılan 150000 N / 100000 Nm kopma eşiği örnek; gerçek
  üretici verisiyle kalibre edilmedi. Zero = kırılmaz sınır.
- Manual H 0.5m snap kullanım kolaylığıdır; tüm manevralarda
  fiziksel hizalama ve çarpışma problemi çözmez.
- PhysicsLab checker/shader code farklı URP/HDRP proje kurulumlarında
  yeni görsel hata üretebilir; önce gerçek Editor görünümünü ölç.
- R kurtarma primitive chassis box/zemin sorgusu varsayımlarına
  dayanır; karmaşık imported mesh ve yüzeyler için genişletilmeli.
- `ConfigurableJoint.enabled` **YOK** (önceden CS1061 oldu);
  `connectedBody=null` bir joint'i WORLD'e sabitleyebilir, detach
  için Component kaldırılır. Aynı kare reattach Destroy gecikmesi
  nedeniyle reddedilebilir.
- Ana PR'lar Draft ve hiçbiri `main`e merge edilmedi.
  **Compile testini araç erişimi olmadan PASS diye raporlama.**
- Gerçekçi açık şasi/tekerlek/süspansiyon modelleri [#9](https://github.com/progesor/KAVRA/issues/9)
  düşük öncelikli backlog; yerel kullanıcı lisanslı assetlerini
  açık repo içine otomatik kopyalama.
- **Automatic anti-flip/auto-upright kullanıcının istemediği davranış.**
  Sonradan arcade profil gelse bile bu ilke değiştirilmeden varsayılan
  açılmamalı.

## 8. Güncellenen ana hedefler ve gelecek aşamalar

| Öncelik | Hedef | Kabul göstergesi |
|---|---|---|
| **P0 şimdi** | Son M1.5.1 derleme ve test kabulü; kırmızı issue'ları kapatma | Unity Console 0 error, EditMode PASS, gerçek checker/hitch görünümü |
| **P1** | Modüler sistemleri tek tutarlı uygulama API'sinde bütünleme | 4W/6x6/8x8/çoklu trailer prefab iş akışı tekrar edilebilir |
| **P2** | İçe aktarılan model, mesh pivot ve süspansiyon görsel bağlama | FBX/GLB modeli Wizard ile fizik prefabına el müdahalesi minimum olarak bağlanır |
| **P3** | Daha gerçek aktarım/fren/aks mekanikleri (ihtiyaç halinde) | Real clutch/diff modülü prototipten ayrılır, testlerle sınanır |
| **P4** | Ağır araç & çok gövde ve yeniden kullanılabilir modül noktaları | Gerçek 18W articulated örneği; kopma/bağlanma/güvenli kurtarma testi |
| **P5** | Profiler, çok tekerlek ölçeklemesi, paket yayın kalitesi | Çeşitli wheel-count stres testleri, sıfıra yakın gereksiz allocation, güncel docs/semver |
| **P6 sonra** | Surface/soil, damage, optional Arcade/assist, netcode | Önceliğe göre ayrılmış optional modüller; core'u zorunlu bağımlı yapmaz |

Tam hedef/iş kırılımı: [docs/08](08_ROADMAP_AND_MILESTONES.md).
Bu hedeflerin tarihleri taahhüt değil; önce P0 kabulü gerekli.

## 9. Bir sonraki sohbete başlangıç talimatı (kopyalanabilir)

> KAVRA Unity Vehicle Framework projesine kaldığımız yerden devam edelim.
> Repo: https://github.com/progesor/KAVRA.
> Aktif dal: `feat/m15-multitrailer-chain`, son belgelenen HEAD
> `48d85280`, Draft PR #16, paket v0.1.6, Unity 6000.3.18f1.
> Önce `docs/25_2026-10-17_CURRENT_STATE_AND_HANDOVER.md`,
> `docs/08_ROADMAP_AND_MILESTONES.md`, `docs/00_SCOPE_AND_GOALS.md`
> belgelerini ve yeni GitHub HEAD'i oku. Son hata EditMode CS0246 idi;
> host `VehicleLabHitchGuide` bağımsız HitchVisuals asmdef'ine taşındı ve
> test referansı eklendi (commit `48d8528`). Unity'de PASS sonucu henüz
> doğrulanmadı. İlk öncelik derleme/Test Runner ve yeni checker/hitch
> laboratuvarının çalışmasını kabul etmek. M1.4 römork bağlama/ayırma/
> kopma kullanıcı tarafından çalışır bulundu. **Ana framework'ü doğru
> inşa et, düşük öncelikli fizik ayrıntılarında oyalanma.** Otomatik
> devrilme engelleme yok; sadece kullanıcı R ile manuel kurtarma.
> Kod değişikliklerini kullanıcının branch'ine uygula, eski sahneleri
> ve docs/Telemetry'yi koru, derlenmemiş Unity kodunu test edildi
> diye sunma ve PR zincirini gereksiz yere bozma.

## 10. İlgili dokümanlar ve kaynaklar

- [Vizyon ve hedefler](00_SCOPE_AND_GOALS.md)
- [Tek güncel roadmap](08_ROADMAP_AND_MILESTONES.md)
- [M0 gerçek telemetri / fren düzeltmesi](17_M05_TELEMETRY_REVIEW_2026-10-14.md)
- [M1 ilk motor/şanzıman](18_M1_POWERTRAIN_FOUNDATION.md)
- [M1.2 Gearbox/visual rig](20_M12_GEARBOX_AND_VISUAL_BINDING.md)
- [M1.3 gerçek iki-gövde](21_M13_MULTIBODY_ASSEMBLY.md)
- [M1.4 manual hitch/kopma](22_M14_HITCH_LIFECYCLE.md)
- [M1.5 çok römorklu zincir](23_M15_MULTI_TRAILER_CHAIN.md)
- [M1.5.1 checker/attach/son hotfix](24_M151_PHYSICSLAB_VISIBILITY.md)

**Not:** Bu belge, son commit ve konfigürasyonlarda eski bilgiyle ilerlememek
için hazırlanmıştır. Gerçek Unity test sonuçları geldikçe tarihli ekler
açılmalı; daha önce doğrulanmış kullanıcı testleri silinmemelidir.

## 11. Aynı gün P1 API/otomatik ön kontrol ek kaydı

- `743813f`: M1.4 iki-gövde ve M1.5 zincir için ortak,
  geri uyumlu `IVehicleAssemblyController`; iki yeni EditMode
  test kaynağı.
- `e553644`: yanlış `0.0.1` beklentisi olan bootstrap
  kontrolünü düzelten Python doğrulayıcı, assembly/MonoScript GUID
  koruması, yalnızca kaynak statik kontrolü yapan Actions workflow'u.
- `f73629f`: PhysicsLab `H` komutu artık yeni ortak API'yi kullanır;
  önceki H mesajları ve manuel `R` akışı korunur.
- Son commit `f73629f` için GitHub source-only CI 2/2 PASS;
  **bu bir Unity/C# compiler veya NUnit/PhysX PASS sonucu değildir**.
- Kullanıcının `.unity` sahneleri, prefab varlıkları,
  `docs/Telemetry/` CSV'leri değişmedi. PR #16 birleştirilmedi.
  Asıl P0 kabulü için kullanıcı Unity 6000.3.18f1 Console,
  dört EditMode suite ve M1.5.1/M1.4 görsel/Play testi gerekiyor.

## 12. 18 Ekim P1 veri doğrulama / prefab testleri (Unity kabul bekliyor)

Önceki 17 Ekim HEAD ve devir bölümleri tarihsel kayıttır. 18 Ekim'de
`feat/m15-multitrailer-chain` üzerinde yapılan çalışmalar:
- `bf139da`: VehicleConfigurationValidator finite numeric / stabil ID
  / powertrain kontrolleri; runtime alanları, eski assetler değiştirilmedi.
- `e2ab644`: hatalı veriye karşı NUnit doğrulama test kaynağı.
- `f0c8993`: 4W/20W, iki-gövde ve üç-gövde için geçici asset
  klasöründe prefab save/reimport/load regresyonları.
- `45bb3d8`: Windows Unity 6000.3.18f1 EditMode batch runner; sonuç
  dosyaları repo dışına ve benzersiz klasöre yazılır.
- **Kaynakta hazır, Unity testi henüz koşturulmadı.** Issue #17
  ve PR #16 Draft açık. Kabul planı [docs/27](27_2026-10-18_P1_VALIDATION_AND_PREFAB_ACCEPTANCE.md).


## 13. 19 Ekim 2026 — İlk gerçek Unity EditMode delili

Kullanıcı `TestResults.xml` sağladı: Unity 6000.3.18f1, 117 vaka,
111 başarılı, 6 başarısız. Bu sonuç KAVRA Test Runner assembly'sinin
gerçekten derlenip yüklenebildiğini ve 117 testin çalıştığını gösterir;
Unity Console'un tamamen hatasız olduğunu veya Play Mode'u kanıtlamaz.
Prefab roundtrip 4/4 ve yeni veri doğrulama 10/10 geçti. Altı açık
test vakası [docs/28](28_2026-10-19_P0_UNITY_TEST_RESULTS_AND_FIXES.md)
altında analiz edilip düzeltme commitleri kaydedildi. Güncel kaynakta
bu testler tekrar çalıştırılmadı; `docs/Telemetry/` korunuyor.

## 14. 19 Ekim 2026 native Unity Undo crash sonucu

Kullanıcı, yeni EditMode test turunda **bütün testlerin geçtiğini**,
ancak Unity'nin kapanırken tekrar crash verdiğini bildirdi. Yüklediği
`CrashReport2026-10-19_074234331.json.gz` doğrudan incelendi:
- Native crash gerçek; `has_native_crash: true`.
- Test sonuçları kaydedildikten ve `IPostBuildCleanup` bittikten
  sonra `Dereferencing NULL ImmediatePtr!` oluştu.
- Crash stack: `UnityEditor.Undo.RevertAllDownToGroup` →
  `TestJobRunner.ExecuteStep`. Son dosyada tam NUnit XML bulunmadığından
  sadece kullanıcı bildirimi ile tüm testler PASS olarak değerlendiriliyor.
- Çok sayıda Editor builder'da Undo ile oluşturulup test
  teardown'da `DestroyImmediate` ile silinen geçici objeler olası
  tetikleyici olarak saptandı; GUI Undo'yu koruyan
  `recordUndo = true` varsayılanıyla birlikte geçici kullanım için
  `false` modu eklendi. **Kaynak düzeltmesi, henüz Unity'de
  crash-free olarak kabul edilmedi.**
- Ana framework Physics/telemetry/scriptable definition ve bütün
  `.unity`, `.prefab`, `docs/Telemetry/` dosyaları korunmuştur.
- Yeni devir referansı: [docs/29](29_2026-10-19_NATIVE_EDITOR_UNDO_CRASH.md).

## 15. 19 Ekim P2 başlangıcı ve Undo crash kabulü

- Kullanıcı, EditMode testlerinin geçtiğini ve Unity Test Runner crash'inin
  çözüldüğünü bildirdi. Bu **kullanıcı gözlemi** olarak kabul edildi;
  final NUnit XML bu mesajda eklenmedi. Play Mode H/J/L/K/R ve görsel
  PhysicsLab kabulü bağımsız olarak hâlâ açık.
- P2.0 harici FBX/imported prefab mesh bağlama sistemi eklendi:
  `VehicleVisualBindingDefinition`, `VehicleImportedVisualRoot`,
  `VehicleImportedVisualValidator`, `VehicleImportedVisualBuilder`,
  `KavraImportedModelWizardWindow`.
- Kaynakta 4W/6x6/20W görsel/per-wheel mapping ve prefab roundtrip testleri,
  yeni yedinci EditMode suite ve kullanıcı yönergesi var.
- Yeni P2 C# kodunun Unity derlemesi ve gerçek FBX Play Mode sonucu
  henüz paylaşılmadı; kaynak statik CI'nin yerine geçmez.
- Ayrıntı: [docs/30](30_2026-10-19_P20_IMPORTED_MODEL_BINDING.md).
- `.unity` sahneler, `.prefab` kullanıcı varlıkları, eski
  `docs/Telemetry/` dosyaları, ana framework fizik ve manuel `R`
  korunmuştur. PR #16 Draft, ana dal merge edilmedi.
