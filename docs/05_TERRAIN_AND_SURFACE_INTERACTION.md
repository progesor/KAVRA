# 05 — Zemin etkileşimi, MudRunner benzeri çamur mimarisi

## Tasarım ilkesi

**Zemin fizik tepkisi** ve **zemin görüntüsü** ayrıdır. Grafik izi olması
mutlaka fizik deformasyonu olduğunu göstermez; fizik deformasyonu olan bir
sistemde shader yüksekliği ile collision yüksekliği ayrıca uzlaştırılmalıdır.

## Surface data

Zemin kimliği `SurfaceProfile` üzerinden başlatılır:

- `longitudinalGrip`, `lateralGrip` (lastik modelinde çarpan/parametre)
- `rollingResistance` (yuvarlanma direnci)
- `softness`, `wetness` (zemin hassasiyeti, ilk deneme verisi)
- Sonraki: `sinkageExponent`, `soilResistance`, `rutRecovery`, `shearResponse`,
  `penetrationLimit` (empirik parametreler)

**v0.0.4 M0:** `SurfaceZone` ile Collider'a atanan `SurfaceProfile` değerleri
tekerlek lastiğinin boyuna/yanal tutunma limitini ve yuvarlanma direncini
çarpan olarak değiştiriyor. Ayarı olmayan yüzeyde eski varsayılan değerler
korunuyor. `Softness` ve `Wetness` M0'da hâlâ yalnızca ilerideki toprak
modülü için metadata; **gömülme, çamur izi, kalıcı deformasyon yok.**
Bu katsayılar gerçek ölçümle henüz kalibre edilmedi.

## Fizik çözücü taslağı

**1. Contact query:** Tekerlek hangi yüzey parçasına temas ediyor, normali ve
yüzeyin fizik malzemesi ne?

**2. Normal force:** Süspansiyon yükünü ve gömülme eğilimini hesapla.

**3. Traction:** Yüzey türü, slip ratio/slip angle ve lastik parametreleri
üzerinden izin verilen çekişi hesapla.

**4. Soft soil response:** Tekerleğin yere batma derinliği için basit empirik
yay/direnç benzeri model; düşük hızda çamurdan kurtulamama deneyimini
üretebilmeli. `sinkage` artışı normal yük, patinaj ve yüzey türüne bağlıdır.

**5. Rolling/bulldozing resistance:** Çamurun tekerlek önünde birikmesiyle
ortaya çıkan ek kuvveti (basitleştirilmiş) uygula.

**6. Optional terrain state:** İz bırakılan patch'in kalıcı gömülme veya
sıkışma durumu, ikinci araç geçtiğinde fizik hesabına aktarılabilir.

## Görselleştirme adapter'ları

- Asfalt/kuru zemin: projeksiyon veya iz şeridi.
- Kum/çamur: parçacık, splash, normal/decal/heightfield deformasyonu.
- Şasi: temiz/kirli yüzey maskesi, optional material parametreleri.
- Su: float/buoyancy ayrı sistem; splash yalnızca grafik değildir, direnç
  de fizik adapter'ından gelebilir.

Bu modüller URP/HDRP'ye özel bağımlılık getirmeden ayrı `Presentation`
assembly'sinde tutulmalıdır.

## Patch bazlı çalışma

`TerrainPatch` yüklü / aktif / uyku durumları. Örnek yaklaşım:

- Aktif araç yakınındaki patch'lerde çözüm ve kirletme olaylarını işle.
- Uzak patch'lerde yalnızca metadata sakla veya görseli düşük çözünürlükte tut.
- Render için GPU atlası/RT kullanılabilir, ama CPU wheel collider o
  çıktıyı birebir okumak zorunda değildir.
- 16 m gibi parça büyüklüğü MudRunner'dan örnek; KAVRA için başlangıç
  sabiti olarak **kararlaştırılmadı**. Profiling sonucu seçilecek.

## Kabul testleri (M5 sonrası)

1. Asfalt–çamur geçişinde ani sayısal kuvvet patlaması yok.
2. Eşit girişte ağır araç aynı yumuşak zeminde daha çok gömülme eğilimi gösterir.
3. Tekerlek patinajı ile hareket direnci ve görsel iz tutarlı yönde değişir.
4. Görsel efekti kapatmak çekiş hesabını değiştirmez.
5. Çamur sistemi olmayan bir host projede core bağımsız çalışır.
6. Bir araç tekerleği şasiye yanlışlıkla temas raporu vermez.
7. İkinci araç birinci aracın izi üzerinden giderse, fiziksel etki opsiyonel,
   tutarlı ve testle doğrulanabilir.

## Kaynak

Pavel Zagrebelnyy (2017):
https://www.gamedeveloper.com/programming/mud-and-water-of-spintires-mudrunner
