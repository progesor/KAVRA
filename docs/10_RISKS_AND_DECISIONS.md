# 10 — Karar kayıtları, riskler ve ertelenen işler

> **Eski ADR/risk matrisi arşiv değerindedir.** Bazı 13 Ekim 2026
> riskleri (örneğin "multi-body çok erken") o tarihte gelecekteydi,
> fakat 17 Ekim itibarıyla gerçek çok gövdeli VehicleAssembly ve
> M1.5 ordered chains **kaynakta uygulanmış** durumda.
> Güncel karar, öncelik ve Unity kabulü için
> [docs/00](00_SCOPE_AND_GOALS.md),
> [docs/08](08_ROADMAP_AND_MILESTONES.md),
> [docs/25](25_2026-10-17_CURRENT_STATE_AND_HANDOVER.md) esas alınır.
> Özellikle **otomatik devrilme düzeltmesi yok; sadece manuel R**.

## Architecture Decision Records

### ADR-001 — Kendi wheel tire force modelimiz

**Karar:** Unity WheelCollider ana temel olmayacak. Kendi contact provider ve
tire force modelimiz, Rigidbody üzerine kuvvet aktaracak.
**Gerekçe:** çok aks/özel tekerlek, kaldırım/çukur, zemin ve tire modülerliği.
**Bedel:** daha uzun fizik kalibrasyonu, test ve nümerik risk.
**Alternatif:** WheelCollider adapter'ı hızlı karşılaştırma referansı.

### ADR-002 — Varsayılan yüksek performanslı yaklaşık mekanikler

**Karar:** ilk süspansiyon eşdeğer yay–amortisör, ilk lastik force modeli
basit/Brush-Fiala benzeri, tam FEM veya mafsal ağı zorunlu değil.
**Gerekçe:** kullanıcı hedefi inandırıcı ve sağlam fiziğe öncelik veriyor.
**Takas:** özel süspansiyon ayrıntıları ve mesleki tasarım seviyesinde
karakterizasyon için ileri modüller gerekecek.

### ADR-003 — Bir fizik çekirdeği, isteğe bağlı arcade profili

**Karar:** arcade modu ayrı sabit kuvvetle karakter sürükleyen controller
yerine giriş işleme/sürüş destek modülü olur.
**İstisna:** bilinçli ve etiketli arcade stabilizer torkları olabilir.

### ADR-004 — Unity 6.3 LTS, URP host

**Karar:** ilk PC Lab projesi 6.3 LTS Universal 3D.
**Açıklama:** runtime çekirdeği render pipeline'dan bağımsız.

### ADR-005 — Embedded UPM ve repo düzeni

**Karar:** önce `Packages/com.progesor.kavra`; kullanıcı mevcut Unity
projesini GitHub'a yükleyecek. Sonra UPM Git monorepo path veya ayrı repo
kararı verilecek.

### ADR-006 — Multi-body, tire count'tan ayrı kavram

**Karar:** 18 tekerli çekici+römork tek Rigidbody değildir. Tek-chassis
20W test preset'i physics doğrulaması değil, data path testidir.

### ADR-007 — MudRunner'dan mimari ders

**Karar:** çamur **yalnızca görsel değil**; yerel tekerlek–zemin kuvvetleri
ve ayrı görsel deformasyon. Ancak başlangıç sürümünde tüm arazi collider
mesh'ini gerçek zamanlı deforme etmeyeceğiz.

## Risk matrisi

| Risk | Etki | Önlem |
|---|---|---|
| Karmaşık lastik modelinde kararsızlık | Yüksek | Sıfır hız testleri, substep, limitler |
| Rigidbody ile custom solver uyuşmazlığı | Yüksek | ForceAtPosition, tek integrator, telemetry |
| Farklı wheel count'ta hidden assumptions | Yüksek | 3W ve 20W M0 veri testi |
| Multi-body yapı fazla erken | Orta | İleri M4, composite tasarım notu |
| Editor Asset serialization kırılması | Yüksek | Stabil ID, migration belgeleri |
| Bursts/Jobs erken karmaşıklık | Orta | Önce managed prototip, profiler bazlı |
| Visual mud ile physics uyumsuzluğu | Orta | Ayrı sistem ve açık coupling sözleşmesi |
| Performansın erken vaat edilmesi | Yüksek | Benchmark öncesi sayı taahhüdü yok |
| Hedef paket lisansı belirsizliği | Orta | GitHub public/private + lisans kararı |
| PhysX cross-platform determinism | Orta | tolerans bazlı replay; determinism iddiası yok |

## Henüz verilmemiş kararlar

- First physics M0 contact: raycast + spherecast mi, yoksa baştan çok noktalı mı?
- Pure C# solver vs Jobs/Burst geçiş eşiği.
- Tire modelin fiziksel parametrelerinin varsayılan veri seti.
- Packaging: tek GitHub mono-repo mu, core için ayrı UPM repo mu?
- Lisans (MIT/Apache-2.0/kapalı kaynak).
- Çamur fizik durumunun save/load'a dahil edilmesi.
- Çok gövdeli araçlar için controller orchestration / joints dizilimi.

## Kapsam kontrolü

İlgili ilk issue onaylanmadan şu özellikleri kodlayarak projeyi şişirmeyelim:
kaporta node-beam hasarı, gerçek lastik FEM, raylı araçlar, tam çok
bağlantılı süspansiyon, ağ determinism ve full-scale volumetric mud.
