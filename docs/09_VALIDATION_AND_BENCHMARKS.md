# 09 — Fizik doğrulama, tekrar oynatma ve performans planı

## Test altyapısı

`VehiclePhysicsLab` sahneleri:

- `M0_Scaffold.unity`: yalnızca Wheel/axle setup görüntüleme.
- `T01_StaticLoad`: gerçek M0 fizik geldikten sonra statik yük.
- `T02_SpringDamper`: tek teker compression / rebound.
- `T03_StraightBrake`: sabit hızdan fren mesafesi/kayma.
- `T04_Skidpad`: sabit yarıçap ve kararlı dönüş.
- `T05_BumpObstacle`: kaldırım/kaya simülasyonu.
- `T06_MultiAxle`: 6x6, 8x8, 20W tek gövde CPU stress.
- `T07_TractorTrailer`: ayrı gövdeli 18 tekerli taslak (M4).
- `T08_SurfaceTransitions`: asfalt/çakıl/çamur (M5).

Bu sahneler hâlâ **planlanan** kabul sahneleridir. v0.0.2 ile `Tools > KAVRA > Create M0 Physics Lab Scene` menüsü çalışan sahne nesneleri üretir, kullanıcı tarafından kaydedilip Play Mode'da doğrulanmalıdır. Kaydedilmiş ve doğrulanmış test sahnesi henüz bulunmuyor.

## Ölçülecek temel büyüklükler

**Şasi:** position, velocity, yaw rate, pitch/roll, acceleration, mass,
center of mass, physics step duration.

**Tekerlek:** wheel RPM, angular speed, Fz, Fx, Fy, slip ratio, slip angle,
contact point/normal, suspension stroke/strokeRate.

**Aktarma:** engine RPM, input torque, output torque, clutch slip, gear,
differential distribution, brake torque.

**Arazi:** surface type, sinkage, rolling resistance, terrain patch ID.

## Otomatik test örnekleri

- **Statics:** zemin düz ve tüm tekerlekler temas ediyorsa toplam `Fz ≈ m*g`,
  yerçekimi vektörüne uygun projeksiyonla kıyaslanır. Tolerans parametrik.
- **No false wheel count assumption:** 3/4/6/8/20 wheel config kabul edilir;
  array index 4 varsayımı regresyonla yakalanır.
- **Identity:** duplicate axle/wheel ID üretilmez veya reddedilir.
- **Steering geometry:** dönüşte sağ/sol hedef açılar araç geometrisine
  anlamlı biçimde uygun; 3W farklı düzenleri test edilir.
- **Gear ratio:** aynı motor çıkış torkunda farklı final drive tekerlek
  torkunu öngörülebilir şekilde değiştirir.
- **Contact:** kaldırım geçişinde hiçbir collider self-hit yok; kayma
  oranı NaN/Inf üretmez.
- **Replay:** aynı sabit girdi dizisiyle aynı platform ve Editor sürümünde
  tolerans içinde benzer eğriler, **bit-level deterministic garanti yok**.

## Performans hedeflerini nasıl belirleyeceğiz?

Donanım ve araç karmaşıklığı bilinmeden "1000 araç simüle eder" gibi bir
iddiada bulunmayacağız. İlk gerçek benchmark için tablolar:

| Senaryo | Kontrol değişkenleri | Çıktı |
|---|---|---|
| 1×4W | contact casts 1/3/5; 60/120 Hz | ms/physics step |
| 1×20W | farklı suspension modeli ve force provider | ms/step ve alloc |
| 20×4W | araç LOD ve uyku | main thread ms |
| 2-body tractor/trailer | joint solver + farklı yük | stabilite ve ms |
| Soft soil patches | aktif patch sayısı / görsel LOD | CPU/GPU ayrı |

Belirlenen performans bütçesi (örn. CPU physics için 3–5 ms hedefi) kullanılan
PC ve oyun gereksinimiyle birlikte bir issue olarak belirlenecek, şimdiden
kabul kriteri ilan edilmeyecek.

## Referans ölçümler

- Düz hızlanma, sabit ivme/fren mesafesi.
- ISO 4138 benzeri sabit dönüş test kurgusu, uygulanabilir olduğunda.
- Araç kütlesi, lastik tipi ve gerçek referans ölçümü belirtilmeden
  ticari araç datasıyla doğrudan doğruluk iddiası yapılmaz.
- Fizik eğrisi sapmaları grafik ve sayısal sonuçlarla kaydedilir.

## Test kaydı önerisi

`tests/reports/YYYY-MM-DD/` içine küçük JSON/CSV özetleri ve performans
screenshot'ları. Büyük video ve binary test çıktıları Git LFS veya
GitHub Release'lere taşınır; normal Git tarihçesi şişirilmez.
