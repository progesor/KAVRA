# 00 — Vizyon, kapsam ve mühendislik ilkeleri

**Tarih:** 2026-10-13 · **Durum:** başlangıç tasarım kararı

## Vizyon

KAVRA, Unity 6 üzerinde yeniden kullanılabilir bir **tekerlekli araç dinamiği
framework'ü** ve **Vehicle Wizard** geliştirmeyi amaçlar. Başlangıç odağı PC;
araç türleri 3 tekerli yapılardan tek gövdeli çok akslı özel araçlara ve çok
gövdeli çekici–römork kombinasyonlarına uzanır. Uzun vadede 20 teker ve üzeri
düzenler veri modeli nedeniyle engellenmez.

## Gerçekçilik yaklaşımı

**Hedef:** fiziksel olarak anlamlı büyüklüklerden inandırıcı ve ölçülebilir
sonuçlar. **Hedef olmayan:** lastik kauçuğunun her deformasyonunu çözmek veya
tüm süspansiyon bağlantılarını çok cisimli dinamik olarak simüle etmek.

Önem sırası: (1) doğru kütle ve yük dağılımı, (2) lastik–zemin teması ve
sürtünme kuvvetleri, (3) tekerlek dönme dinamiği, (4) yay/sönüm kuvvetleri,
(5) motor–şanzıman–diferansiyel tork akışı, (6) sürücü kontrolü ve yardımcılar,
(7) görsel tekerlek/zemin/kaporta efektleri.

## Kabul edilen sınırlar

- Ana fizik modeli kendi **tekerlek kuvveti** hesaplamasına dayanır; Unity
  `WheelCollider` yalnızca karşılaştırma/opsiyonel adapter olabilir.
- Unity 3D Rigidbody, çarpışma ve genel gövde hareketinin sahibi olmaya devam eder.
- M0 tekerlek modeli basit ama test edilebilir; daha gelişmiş model değiştirilebilir.
- **Simülasyon/arcade** farklı fizik motorları değil; aynı çekirdeğin farklı
  giriş işleme ve sürüş yardım profilleridir.
- **Bir araç gövdesi = bir Rigidbody**; gerçek çekici–römork çok gövdeli, eklemli
  bir yapı olarak temsil edilir.
- Görsel parçaların pozisyonu fiziğin sonucu olur; fiziksel kuvvet üretimini
  tek başına yüksek detaylı render geometrisi belirlemez.
- Motor, ses, kontrol cihazı, kamera, UI ve render pipeline bağımsız adapter'lar.

## Araç sınıfı matrisi

| Sınıf | Örnek | İlgili mekanik | Beklenen aşama |
|---|---|---|---|
| 3 teker | Delta / tadpole | Tek/çift yönlenir aks, devrilme | M1 |
| 4 teker | Otomobil / SUV | Önden/arkadan/4x4, ABS/ESC | M1–M5 |
| 6–10 teker | Kamyon, 6x6, 8x8 | Çok aks, kilitli diff, ağır yük | M3–M5 |
| Çok gövdeli | Çekici+römork, otobüs körüğü | Ayrı Rigidbody, mafsal, fren bağlantısı | M4 |
| İş makinesi | Forklift, traktör | Arka direksiyon, yük merkezi | M4–M6 |
| Özel | Omni/Mecanum, çok aks yönlendirme | Özel kinematik ve lastik force plugin | M6+ |
| 2 teker | Motosiklet/bisiklet | Yatış/denge/kontra direksiyon | Araştırma, sonraki kapsam |

## Fizik kalite seviyeleri (tasarım, uygulanmış değil)

- `High / Simulation`: gelişmiş temas, birleşik kayma, yüksek örnekleme.
- `Balanced`: daha az temas sorgusu, korunmuş dinamizm, makul CPU.
- `Arcade`: yardımcı kontrol ve stabilite; mutlaka ayrı fizik motoru değil.
- `Far / Background`: AI ve uzak araçlara özel sadeleştirilmiş hesaplama.

Aynı üretim varlığı farklı projelerde kullanılabilir; kullanıcı tanımlı
kalibrasyon verileri araca özel sürüş karakterini belirler.

## Kalite kriteri

"Gerçekçi görünüyor" tek başına kabul değildir. Test pistinde fren mesafesi,
ağırlık transferi, düşük hızda çekiş ve çok akslı statik yük gibi metrikler
ölçülür. Değerler kullanılan model ve lastik verilerinin doğruluğuna bağlıdır;
ölçülmemiş sonuçlar simülasyon doğruluğu iddiası taşımayacaktır.
