# 04 — Araç fizik modelleri ve yaklaşık çözüm yaklaşımı

## 1. Koordinatlar ve fiziksel birimler

- `+X` sağ, `+Y` yukarı, `+Z` ileri.
- Uzunluk `m`, kütle `kg`, zaman `s`, kuvvet `N`, tork `N·m`.
- Açılar iç hesapta `rad`/`rad·s⁻¹`, Editor'da derece gösterebilir.
- Dünya ölçeği: 1 Unity birimi = 1 metre.

## 2. Çekirdek gövde

Tek chassis bir `Rigidbody` ile çözülür. Şasiye doğru noktalarda kuvvet
uygulanır, kütle merkezi ve atalet tensörü aracın boyut/kütlesini yansıtmalıdır.
Araca gerçekte bulunmayan büyüklükte anti-roll torku verilmesi ayrı `Arcade`
yardımı olarak etiketlenir.

Statik yük testi: düz zeminde destek veren tekerleklerin dikey kuvvet
bileşenleri toplamı yaklaşık `m*g` olmalı; her tekerleğe otomatik `m*g/N`
atamak yalnızca basit simetrik ilk tahmindir, genel çözüm değildir.

## 3. Süspansiyon — M0/M1 hedefi

Başlangıç modeli bir yay–amortisör eşdeğeridir:

`F_spring = k * compression`
`F_damper = c_bump_or_rebound * compressionRate`
`F_total = clamp(F_spring + F_damper + bumpStop, >=0)`

Burada kuvvet işaretlerinin koordinat yönüne göre tam uygulaması testlerle
doğrulanır; negatif çekme kuvveti üreten basit raycast süspansiyonu hatasından
kaçınılır. Stroke limitleri, ön yük (preload), anti-roll bağlantısı, camber
ve steer açısı gelecekte ayrı parametrelerdir.

Gerçek bağlantı kolları için çoklu Rigidbody/joint çözümü zorunlu değildir.
Gerekli kinematik etkiler idealize eğri/fonksiyon üzerinden temsil edilebilir.

## 4. Lastik kuvvetleri

Başlangıçta her tekerlekte:

- Temas noktasının noktasal hızını `v + ω × r` ile hesapla.
- Hız ve dönen çevre hızı arasındaki uzunlamasına kaymayı üret.
- Temas düzleminde lateral hızdan slip angle üret.
- Statik/dinamik lastik yüküne, sürtünme parametrelerine ve combined slip'e
  göre `Fx` ve `Fy` hesapla.
- Sıfıra yakın hızlarda nümerik tekillik oluşmaması için koruma uygula.
- Tekerlek dönme momentini `I_w * domega/dt = T_drive - T_brake - Fx*r - T_rr`
  denklemiyle uygula (işaretler ve limitler kodda doğrulanacak).

Basitleştirilmiş sürtünme sınırı:

`(Fx / (mu_x * Fz))^2 + (Fy / (mu_y * Fz))^2 <= 1`

Bu **lastik modelinin tamamı değildir**; combined-slip limitleme için
örnek başlangıç yaklaşımıdır. Fiala / Brush, sonrasında kalibre edilmiş
Pacejka için `ITireForceModel` arayüzü kullanılır. Pacejka parametresi
olmayan bir lastiğe rastgele Magic Formula katsayısı atamak yüksek doğruluk
garanti etmez.

## 5. Yüzey teması

M0 karşılaştırma hedefi:

1. Tek `Raycast` (hızlı referans).
2. `SphereCast`/`CapsuleCast` ve özel geometrik düzeltme.
3. Gerektiğinde çoklu sorgu (tekerlek genişliğinde sol/orta/sağ örnekler).

Çoklu temasın normal ve temsil noktasının hesabı dikkat gerektirir.
Çarpışma katmanı şasiye/rulman görsellerine self-hit üretmemelidir.
Tekerlek görsellerinin sürtünmesi lastik solver'ın kuvvetine **eklenmemeli**.

## 6. Direksiyon

- Basit Ackermann benzeri hedef açı geometrisi.
- Her aksın isteğe bağlı bağımsız steering mapping'i.
- Hıza duyarlı giriş filtreleme / hız limiti.
- Forklift arka aks yönlendirme, 8x8 iki ön aks ve Crab steering ileri aşama.

## 7. Motor/aktarma — M2

- `Engine`: rpm–torque curve, inertia, idle, redline, starter/stall.
- `Clutch` ve/veya tork konvertörü.
- Manuel/otomatik/elektrikli aktarma; dişli oranları.
- Diferansiyel graph: open, limited slip, locked ve torque split.
- Elektrik motorlarında wheel-hub veya aks düzeyi çoklu tahrik grafiği.

Bağlantılı momentleri tek seferli hesaplamak her zaman stabil değildir;
tork aktarımı ile wheel angular speed iteratif/alt-adımlı çözülmelidir.

## 8. Frenler ve sürüş yardım profilleri

- Servis freni, el/park freni, motor freni, retarder (gelecek).
- ABS/TCS/ESC fiziksel kontrolleri isteğe bağlı, ayrı sistemler.
- Arcade steer/yaw stabilizer, rollover prevention ve input shaping,
  gerçek sürüş desteklerinden ayrı isimlendirilir.
- Profile geçişi hareket halindeyken de parametre interpolasyonuyla yapılabilir.

## 9. Zaman adımı / numerik kararlılık

Önerilen ilk deney: Unity `Fixed Timestep = 0.0166667 s` (~60 Hz).
Bu bir garanti veya fizik optimumu değildir. Sadece tekerlek/aktarma
hesabında daha çok substep kullanmak **Rigidbody çarpışmasını aynı
frekansa yükseltmez**. Gerektiğinde manual sim ve global timestep ayrı
benchmark gerektirir.

Başlangıçta hesapları determinism iddiası olmadan, tekrar oynatma kaydı ve
hata toleranslı regresyonla test edeceğiz.

## 10. Güvenlik / kullanıcıya açıklık

M0 fizik prototipi kaynak kodu (v0.0.2) artık spring/damper, raycast temas, 
tekerlek açısal dinamikleri ve basit hız-kayması lastik kuvvetlerini Rigidbody'ye
uygular. Ancak gerçek Unity derlemesi ve Play Mode doğrulaması henüz yapılmadı;
bu sürümde lastik modeli kalibre edilmemiştir. Detay: docs/13_M0_FIRST_PHYSICS_SLICE.md.
