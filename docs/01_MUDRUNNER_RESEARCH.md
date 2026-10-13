# 01 — Spintires: MudRunner araştırması ve KAVRA'ya aktarılacak dersler

**Kaynak sınıfı:** Geliştirici yazısı ve geliştirici röportajı (2017–2019).
**Bu bölüm oyunun kaynak koduna erişim iddiası taşımaz.**

## Önemli bulgu: yalnızca grafik efekti değil

MudRunner baş geliştiricisi Pavel Zagrebelnyy, fizik altyapısının tekerlek–
zemin temasını ve çekişini hesapladığını; çamur katmanı veya aşırı patinajda
gerçek çamur fiziğine yaklaşan kuvvet modelleri uyguladığını açıklıyor.
Tam fiziksel bir toprak simülasyonu yapılmıyor; tutarlı bir **yaklaşık model**
çalışıyor. Görsel çamur hareketi GPU tarafında temsil ediliyor.

Geliştiricinin yazısına göre oyunda üçüncü taraf *oyun motoru* kullanılmıyor,
ancak genel çarpışma/rigid-body temeli **Havok**; çamur etkileşimi ek özel
hesaplamalar gerektiriyor. Bunu "MudRunner sadece shader hilesi" olarak
özetlemek yanlış olur.

### Belgelendirilmiş teknik ayrımlar

1. Harita, örnek olarak **16×16 m** parçalara bölünüyor.
2. Yakındaki/aktif parçalarda çamur, su ve çevre nesneleri işleniyor.
3. CPU'daki çamur–araç fizik verisi ile GPU'daki görsel çamur verisi farklı
   çözünürlük ve yapılarda tutuluyor.
4. Fizik sistemi tekerleğin teması, hızı ve çamura girişine göre yaklaşık
   direnç/çekiş hesaplıyor.
5. Görüntüde çamur yükseklik alanı, tekerlek izleri ve decals birleşiyor;
   yüksek görsel ayrıntı **aynı çözünürlükte fizik gerektirmiyor**.
6. Küçük çamur parçacıklarının önemli bölümü görsel; bazı büyük parçalar
   gerçek rigid-body ile etkileşiyor.
7. Su yüzeyi ve dalgalar için de parametrik/görsel yaklaşımlar tercih ediliyor.

## KAVRA'daki karşılıkları

| MudRunner yaklaşımı | KAVRA mimari kararı |
|---|---|
| CPU temas ve çekiş | WheelContact + TireDynamics + TerrainResponse |
| Görsel veri ayrımı | `TerrainVisuals` ayrı renderer adapter |
| Çamur gömülme etkisi | Batma derinliği ve zeminin normal/çekiş direnci |
| Patinajın toprağa etkisi | Slip/normal yük üzerinden deformasyon olayı |
| Yakındaki aktif bölgeler | Terrain patch/chunk bazlı aktiflik ve LOD |
| Tekerlek izi | Shader decal/mesh/trail opsiyonel çıktısı |
| Ağır çamur parçaları | Çok sonra, yakın temaslı görsel Rigidbody efekti |

## Önerilen daha ekonomik yaklaşım

**Aşama A**: Zemin `SurfaceProfile` (asfalt, çakıl, kum, kuru çamur, ıslak
çamur), temas tespiti ve zemin etkili lastik kuvvetleri. Görsel izler sadece
decal veya mesh; zemin kolizyonu **değişmez**.

**Aşama B**: Çamurlu bölgede her tekerlek için yaklaşık sinkage (gömülme),
bulldozing/rolling resistance ve patinaja bağlı ek direnç. İlk olarak ufak
zamansal hafızalı yumuşak zemin hücreleri kullanılabilir; performans ve
oynanış doğrulanmalı.

**Aşama C**: Sadece ihtiyacı olan bölgelerde yerel heightfield/mesh temas
güncellemesi. Bu sistem tüm haritanın her karede yeniden MeshCollider
pişirilmesini gerektirmemeli.

## Yapılmaması gerekenler

- Tüm Unity Terrain yüksekliklerini bütün tekerleklerde her FixedUpdate'te
  yeniden düzenlemek.
- GPU'da görünen oluk derinliği ile çarpışma geometrisinin otomatik olarak
  aynı olduğunu varsaymak.
- Yalnızca sürtünme katsayısı düşürerek derin çamurda batmayı taklit etmek;
  **ek gömülme ve hareket direnci** ihtiyacını modellemek.
- Çamur oyunu yapmak için framework'ün temelini bu sisteme bağımlı kılmak;
  asfalt ve pist araçlarında TerrainResponse tamamen kapatılabilmeli.

## Açık sorular

- Fizik yüzey hafızası: araç başına mı, global patch bazında mı?
- Çamur izi fiziksel olarak başka araçları da etkileyecek mi?
- Arazi deformasyonu scene save/load içine girecek mi?
- Fizik sorgusu hangi geometriye bakacak; mevcut Unity Terrain, height
  patch veya custom local collision?

**İlk karar:** Terrain Interaction **çekirdeğe eklenecek bir arayüz**;
çalışan çamur deformasyonu M0'ın kabul şartı değildir.

## Birincil kaynaklar

- Pavel Zagrebelnyy, *Mud and Water of Spintires:MudRunner* (16.11.2017):
  https://www.gamedeveloper.com/programming/mud-and-water-of-spintires-mudrunner
- *MudRunner: Porting a Successful Title to Console*, Pavel Zagrebelny röportajı
  (04.01.2019): https://80.lv/articles/mudrunner-porting-a-successful-title-to-console
