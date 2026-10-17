# KAVRA — Hızlı yeni sohbet devir notu (17 Ekim 2026)

> **Aktif dosya — eski M0'dan kalan ekleme yığını arşivlendi.**
> Tam teknik rapor:
> [docs/25_2026-10-17_CURRENT_STATE_AND_HANDOVER.md](25_2026-10-17_CURRENT_STATE_AND_HANDOVER.md).
> Güncel hedefler: [docs/00](00_SCOPE_AND_GOALS.md),
> [docs/08](08_ROADMAP_AND_MILESTONES.md).

## Şu anda neredeyiz?

- **Repo:** https://github.com/progesor/KAVRA
- **Unity:** 6.3 LTS, **6000.3.18f1**
- **Paket:** `com.progesor.kavra`, **v0.1.6**
- **Çalışma dalı:** `feat/m15-multitrailer-chain`
- **Son kod düzeltmesi:** `48d85280d50142ab4aba031aed7d943dcf7f0c39`
- **Aktif PR:** [#16](https://github.com/progesor/KAVRA/pull/16) (Draft);
  taban [PR #13](https://github.com/progesor/KAVRA/pull/13), önce
  [PR #8](https://github.com/progesor/KAVRA/pull/8), [PR #2](https://github.com/progesor/KAVRA/pull/2).
- **Tüm PR'lar Draft.** Henüz `main`e merge yok. Farklı feature
  dallarını karıştırma; son dal üzerinden devam et.

## Kullanıcının asıl isteği

KAVRA: yeniden kullanılabilir, **3W–20W+** değişken tekerlek/aks içeren,
modüler motor/şanzıman/diferansiyel, süspansiyon, araç Wizard'ı,
prefab ve ayrı Rigidbody'li çekici/römork kombinasyonları olan
**kapsamlı bir Unity Vehicle Framework**.

- **Önce ana omurgayı düzgün tamamla;** küçük lastik/süspansiyon
  ayrıntılarına takılma. Bariz fizik/derleme sorununu elbette düzelt.
- **DİKKAT:** Otomatik devrilme engelleme, gizli anti-flip veya
  otomatik dik konuma çekme **KULLANMA**. `R` kullanıcının bilinçli
  manual recovery komutu; doğal devrilme mümkün.
- Bağlantı `H` ile bilinçli kurulur/ayrılır, fiziksel kopma
  `ConfigurableJoint.breakForce/breakTorque` ve `OnJointBreak` ile.
  Otomatik yeniden bağlama yok.
- Gerçekçi açık şasi ve ayrı teker/süspansiyon parçaları ileride
  bakılacak **düşük öncelikli** [Issue #9](https://github.com/progesor/KAVRA/issues/9).

## Durum / kanıt

- M0: Kullanıcı gerçek Unity CSV'leri commitledi.
  M0.5 düşük hız fren kuvveti salınımında A/B sonucu
  **13.94 kN → 0.67 kN** (p95 mutlak toplam Fx), [docs/17](17_M05_TELEMETRY_REVIEW_2026-10-14.md).
- M1/M1.2: basic powertrain, vites state, axle distribution,
  tekerlek/şok görsel pivotu kaynakta.
- M1.3/M1.4: gerçek iki Rigidbody ve bağlantı yaşam döngüsü,
  kullanıcı tarafından **güzel çalışıyor** olarak geri bildirildi;
  resmi Test Runner raporu yok.
- M1.5: ordered N-body zincir, 6x6+4W+4W = 14W/3 Rigidbody/2 joint,
  J/L link seçimi, H attach/detach, K fiziksel yük, bağlı
  ön-gruba özel R recovery. Kaynak + testler commitlendi.
- M1.5.1: bağlama varsayılanı **50 cm/55°**,
  checker road/15 m referans çizgileri, mavi çekici,
  turuncu/kırmızı römork, seçilen hitch rehberi, hafif FOV.
  Kaynak hazır ama son Editor kabulü yok.

## Son iki Unity hatası — düzeltildi ama henüz kabul edilmedi

1. **NullReferenceException** `VehicleLabHitchGuide.TintRenderer`
   (`colorBlock` Setup'ta erken kullanılıyordu). `e30c32c`:
   erken `EnsureArrays` + lazy MaterialPropertyBlock + regresyon testi.
2. **CS0246** `KavraHitchLifecycleEditModeTests.cs` üç satırda
   `VehicleLabHitchGuide` göremiyordu; tanım `Assembly-CSharp`
   içindeydi, test `.asmdef` bağımsızdı.
   **`48d8528`**: rehber Script'i kendi
   `Assets/VehiclePhysicsLab/Scripts/HitchVisuals/` klasörüne
   ve `Progesor.Kavra.Lab.HitchVisuals.asmdef` assembly'sine
   taşıdı. EditModeTests assembly referansı eklendi.
   Script Mono GUID'si **aynı**: `ca1c5b2c99ab46b6b141000000000002`.

İkisinde de statik source/asmdef/GUID incelemesi geçti.
**Bu, Unity'nin gerçekten 0 error ile derlendiği anlamına gelmez.**
Kullanıcı son hotfix'ten sonra henüz test sonucu vermedi.

## Yeni sohbette hemen ne yapılmalı?

1. GitHub canlı HEAD'ini ve PR #16 değişikliklerini oku.
2. Kullanıcının Unity'de `feat/m15-multitrailer-chain` dalını
   çekip Console'da **0 compiler error** elde ettiğini
   doğrula; gerekirse tam Console logunu ele al.
3. Test Runner/EditMode: `KavraPhysicsEditModeTests`,
   `KavraAssemblyEditModeTests`,
   `KavraHitchLifecycleEditModeTests`,
   `KavraAssemblyChainEditModeTests`.
4. `Tools > KAVRA > Create M1.5 Two-Trailer Lab Scene`.
   Checker zemini/araç renkleri/hitch guide ve 0.5m yakalama
   halkasını doğrula; H/J/L/K/R kısa Play Mode smoke.
5. M1.4 iki gövde ve M0 eski sahne/regresyon kontrolü.
6. Bu testler geçmeden yeni ağır feature eklemeye acele etme.
   Kabul sonrası güncel [roadmap](08_ROADMAP_AND_MILESTONES.md) P1–P2:
   core modül API/Wizard/prefab, gerçek model pivot bağlama ve
   sistematik araç çeşitliliği.

**Kullanıcıyı aynı eski CSV deneylerini yeniden yapmaya zorlama.**
Sonuç varsa doküman checklist ve PR durumlarına güncel kanıt yaz.
Kodları yalnızca doğru feature dalına commit et; `main`i doğrudan
değiştirme.

## Yeni sohbet için kısa kopyala-yapıştır

> KAVRA Vehicle Framework projesine kaldığımız yerden devam edelim.
> https://github.com/progesor/KAVRA,
> aktif dal `feat/m15-multitrailer-chain`, son uygulama commit'i
> `48d8528`, Draft PR #16, Unity 6000.3.18f1, UPM v0.1.6.
> GitHub'da önce `docs/25_2026-10-17_CURRENT_STATE_AND_HANDOVER.md`,
> `docs/08_ROADMAP_AND_MILESTONES.md` ve
> `docs/00_SCOPE_AND_GOALS.md` dosyalarını incele.
> Son CS0246 test assembly hatası `48d8528` ile kaynakta
> düzeltildi; gerçek Unity kabulü bekliyor. Öncelik derleme,
> EditMode ve checker/hitch PhysicsLab'ın doğrulanması, ardından
> genel framework'ün Wizard ve prefab tarafı.
> M1.4 bağlama/ayırma/kopma çalışıyor diye kullanıcı geribildirimi var.
> Otomatik devrilme engelleme asla ekleme, R sadece manuel.
> M0/M1 sahneleri ve telemetri dosyalarını koru;
> gerçek Unity testini çalıştırmadan PASS deme.

### Eski devir geçmişi

M0→M1.5.1 arası biriken önceki uzun oturum notları kaybolmasın diye
`docs/12_HANDOVER_HISTORY_TO_2026-10-17.md` içinde arşivlendi.
