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
- Simulation/Sport/Arcade profilleri, giriş yumuşatma ve fiziksel ESC testleri;
  kullanıcı talebiyle otomatik devrilme engelleme kullanılmayacak.
  Devrilme sonrası manuel kurtarma M0 v0.0.4'te eklendi.

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


### M0 ride-height follow-up — issue #3

- [x] Source for footprint/CoM nominal load estimate, bump-stop and anti-roll.
- [x] Source for ray/sphere contact switch with fallback and bottom-out metrics.
- [x] HUD and 20-second experimental lab CSV capture source.
- [x] Pure/EditMode regression test sources added.
- [ ] Unity 6000.3.18f1 compiles, all new tests PASS.
- [ ] Turning on flat ground: acceptable bank without persistent wheel clipping.
- [ ] Telemetry accepted across 3W / 4W / 8W / 20W.


### M0 yüzey sürtünmesi / manuel kurtarma / ölçüm — issue #4

- [x] `SurfaceZone` ve `SurfaceResponse` fizik entegrasyonu kaynak kodu.
- [x] Lab asfalt / çakıl / çamur / buz zemin şablonları ve yüzey metrikleri.
- [x] Kullanıcı tetiklemeli `R` kurtarma; otomatik anti-flip **yok**.
- [x] Ray/sphere cast sorgu maliyetini isteğe bağlı ölçecek sayaç ve 90s CSV.
- [x] Yüzey davranışları / kurtarma güvenliği için EditMode test kaynakları.
- [ ] Unity derlemesi, EditMode testleri ve Play Mode yerel kabul.
- [ ] Farklı yüzeylerde veri kayıtlarının nümerik karşılaştırması.
- [ ] Cast CPU mikrosaniye istatistikleri gerçek donanımda karşılaştırıldı.


### M0.5 steering/slip diagnostics / reproducible calibration — issue #5

- [x] Steering geometry source: legacy Parallel / ApproximateAckermann for
  variable wheel counts, rear steer, front steer and multi-axle layouts.
- [x] Optional TireTuningProfile per wheel with null = legacy defaults.
- [x] Bounded slip ratio / slip angle diagnostic output per wheel; M0 force
  model remains the same velocity-slip approximation.
- [x] Scripted M0 calibration-run source: brake, steering step and coast.
- [x] Extended Inspector/HUD and CSV telemetry with input/tire/steer fields.
- [x] Source for EditMode regression tests.
- [ ] Local Unity compilation, test runner acceptance and actual CSV plots.
- [ ] Calibration of low-speed tire force, braking and load-transfer against
  real recorded measurements or explicitly chosen reference behavior.


### M0.5 measured brake oscillation follow-up — issue #6

- [x] Four user telemetry CSVs inspected, numeric anomaly summarized in docs/17.
- [x] Source code for near-stop brake impulse limiting, opt-in configuration
  and per-wheel telemetry; 3 new EditMode regression tests authored.
- [x] Unity Play Mode produced real v0.0.6 CSV with new braking diagnostics.
- [ ] Unity Test Runner EditMode suite PASS evidence (not yet provided).
- [ ] Brake holding/creep on a slope verified (flat asphalt only so far).
- [x] New StraightBrake A/B CSV with limiter enabled confirms elimination of
  persistent near-stop high-force alternation on flat asphalt. User's T3
  commit `c278b42` / `132146.csv`, verified in docs/17 (common-window
  Fx p95 13.94 kN -> 0.67 kN, sign flips 243 -> 3).
- [ ] SteeringStep repeated and airborne wheel RPM/grounded behavior assessed;
  no automatic roll or flip correction will be introduced.
- [ ] Surface/multi-axle behavior acceptance, before M1 freeze.


## M1 — Reordered implementation focus: core vehicle power path

Based on the user's request, we now prioritize the **functioning reusable
vehicle system** before tiny near-limit physics tuning details.

- [x] Source: SimplePowertrainDefinition (torque/RPM + gears + final drive).
- [x] Source: reusable SimplePowertrainRuntime with axle-ID torque splits and
      no 4-wheel hardcoding; direct M0 drive mode remains optional/default.
- [x] Source: Wizard create/attach powertrain asset, build M1 chassis
      and save a placeholder reusable powered Unity prefab.
- [x] Source: M1 host scene generator, input/gear handling, HUD/CSV and tests.
- [ ] Unity 6000.3.x imports and compiles new M1 sources.
- [ ] EditMode tests pass, 3/4/6/8/20 wheels allocate motor torque correctly.
- [ ] M1 Play Mode W/S, Q/E, brakes, R manual recovery and gear RPM telemetry.
- [ ] Save actual M1 lab scene and minimum smoke results in repo.
- [ ] Confirm older M0 scenes and user telemetry behavior are unchanged.

Next: move forward with a basic explicit open differential and optional
multi-axle weighting, better Wizard prefab binding, then M2 clutch/gearbox
reaction models. Do NOT block main framework development on minuscule tire
force refinements. Natural rollover and R-only manual recovery remain.


### M1.1 reusable driveline and multi-axle demos — issue #10

- [x] Source: AxleDefinition extended with backward-compatible drive torque
  weighting and clearly labeled approximation mode.
- [x] Source: SimplePowertrainRuntime distributes total engine output by
  axle weights and wheel-level relative-speed bias when requested.
- [x] Source: Wizard can toggle powered axles and modify group torque modes.
- [x] Source: separate M1 labs for 3W/4W/6x6/8x8/20W (one rigid body each).
- [x] Source: six EditMode regression tests, old constructor retained.
- [ ] Unity 6000.3.x compile and EditMode tests actually PASS.
- [ ] M1 truck 6x6 and 8x8 scenes drive, reverse and show wheel torques.
- [ ] M0 .unity scenes and previously recorded user telemetry remain intact.
- [ ] Actual gear/torque behavior and performance evaluated in Play Mode.

Next foundational step: basic real gearbox state/shift control and simpler
reusable model/visual binding; full mechanical differential lock remains
deferred. Realistic mechanical demo 3D models are tracked separately #9.
