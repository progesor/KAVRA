# 02 — Gereksinimler ve kullanıcı senaryoları

Etiketler: **MUST**, **SHOULD**, **LATER**. Her özellik geliştirme aşamasında
ölçülebilir kabul kriterine bağlanmalı.

## MUST — çekirdek

- [ ] Tekerlek sayısı veriyle tanımlanır; 3 ve 4 tekerde zorunlu indeks varsayımı yok.
- [ ] Fiziksel gövde(ler) Unity Rigidbody üzerinden çalışır.
- [ ] Lastik temas sorguları değiştirilebilir ve yüzey filtresi kullanır.
- [ ] Her tekerlek bağımsız çap, genişlik, montaj pozisyonu, dönme momenti,
      fren/tahrik/yönlendirme bayraklarını taşıyabilir.
- [ ] Aks, diferansiyel ve süspansiyon grupları tekerlekten ayrıştırılır.
- [ ] Giriş, kamera, ses ve görsel modeller fizik bağımlılığı değildir.
- [ ] Araç konfigürasyonu ScriptableObject, runtime durumu mutable struct/class.
- [ ] Tüm fizik hesapları sabit adımlı, SI birimli ve telemetri üretir.
- [ ] Geçersiz konfigürasyonlar Wizard tarafından kullanıcıya açıklanır.
- [ ] Herhangi bir projede URP/HDRP veya belirli bir input paketi zorunlu olmaz.

## SHOULD — ilerleyen sürümler

- [ ] 3W delta/tadpole, 4W, 6×6, 8×8, çift tekerli aks örnekleri.
- [ ] Tork yönlendirme ve merkez/ön/arka diferansiyel tipleri.
- [ ] Manuel/otomatik/CVT/elektrikli aktarma adapters.
- [ ] Fiziksel sürüş destekleri ve arcade giriş profilleri.
- [ ] Wheel/axle gizmos, kuvvet okları, grafikli telemetri, örnek laboratuvar.
- [ ] Römork bağlantıları, fren hattı ve çok gövdeli konfigürasyon.
- [ ] Terrain surface provider ve yerel deformasyon eklentileri.
- [ ] Wheel contact query sayısı / uyku / uzak araç kalite düzeyleri.

## LATER — kontrollü kapsam

- [ ] Kırılabilir mekanik parçalar, lastik patlaması, deformasyon.
- [ ] Fiziksel yay/salıncak linkage multibody çözümü.
- [ ] Çok yüksek hız/atlayış/sert darbe için özel zaman entegrasyonu.
- [ ] Multiplayer replikasyon adaptörü (tam determinism taahhüdü yok).
- [ ] Çeşitli platformlarda benchmark ve optimize motor.

## Kullanıcı senaryoları

**UC-01**: Unity'ye dört tekerlekli FBX model ekleyen geliştirici, tekerlek
transforms'larını Wizard'a tanıtır; 10 dk içinde sürüş için temel prefab
hazırlar (**M3 hedefi**, M0'da çalışmıyor).

**UC-02**: Bir simülasyon projesi 18 lastikli çekici + römork kurar. Çekicinin
ve römorkun ayrı Rigidbody ve aksları, beşinci teker/kingpin bağlantısı ve
frenleri vardır (**M4 hedefi**, tek gövdeye indirgenemez).

**UC-03**: Bir araç aynı fizik çekirdeğiyle `Simulation` ve `Arcade`
profile sahip olur; sahte kütle ataması yapılmaz.

**UC-04**: Bir geliştirici kendi `ISurfaceProvider` uygulamasıyla asfalt
lastikleri veya yumuşak çamur tepkisini entegre eder.

**UC-05**: Fizik geliştiricisi fren testini tekrarlayıp farklı tire solver'ları
arasında mesafe/kayma/yaw eğrilerini karşılaştırır.

## Temel kalite kuralları

- Her fizik özelliği için fiziksel birimler, beklenen aralıklar, hata sınırları
  ve regresyon testi yazılır.
- Metriklerin marka-model düzeyinde gerçeğe uygunluğu ancak gerçek referans
  verisi olduğunda iddia edilir.
- Geliştiricinin oluşturduğu paket, orijinal projenin başka Unity paketlerine
  (rendering/input) bağlanmasını **zorunlu tutmamalı**.
