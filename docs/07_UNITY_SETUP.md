# 07 — Unity proje oluşturma, paketler ve proje ayarları

**Önerilen ana sürüm:** **Unity 6.3 LTS** (`6000.3.x`).
Ekim 2026 itibarıyla Unity'nin 6.3 serisini Aralık 2027'ye kadar destekleme
planı var. Resmî `6000.3.26f1` patch sayfası 8 Ekim 2026 tarihli; Hub'da
bu serinin daha güncel kararlı patch'i görünüyorsa onu kullanmak uygundur.

## Unity Hub'da oluştur

1. Unity Editor **6.3 LTS** kur.
2. `New Project > Universal 3D` şablonu seç (Universal 3D Sample değil).
3. Proje adı `KAVRA` veya `KAVRA-Lab`.
4. Platform Windows; şimdilik tek platform yeter.
5. Projenin açılıp derlenmesini bekle. Kapat.
6. Starter ZIP içeriğini proje köküne kopyala. `Packages/manifest.json` ve
   `ProjectSettings/` klasörünü **silme/ezme**.
7. Tekrar aç; UPM embedded package otomatik algılanır.

**Neden URP?** Demo/test sahnesi için yeterli görsel özellik, iyi kullanım
kolaylığı. Fizik çekirdeği URP'ye bağlı değildir; başka projelerde HDRP,
Built-in Render Pipeline veya başsız fizik çalışma biçimleri ayrı adapter
ile hedeflenebilir.

## Gerekli / isteğe bağlı paketler

| Paket | Durum | Not |
|---|---|---|
| Unity 3D Physics/Rigidbody | Yerleşik, zorunlu | Projenin ana gövde fiziği |
| Universal RP (`com.unity.render-pipelines.universal`) | Universal 3D şablonuyla | Lab render'ı; KAVRA core dependency değil |
| Input System (`com.unity.inputsystem`) | Lab için önerilir | Kurulu değilse Package Manager'dan ekle |
| UI Toolkit | Unity Editor'da mevcut | Gelecek Wizard UI için |
| Test Framework (`com.unity.test-framework`) | İlk fizik testleri öncesi | EditMode ve PlayMode test runner |
| Burst (`com.unity.burst`) | M0 için gereksiz | Profiling'den sonra optimizasyon |
| Mathematics (`com.unity.mathematics`) | İleri aşama | SIMD/Jobs math için |
| Collections (`com.unity.collections`) | İleri aşama | NativeArray/Jobs veri yönetimi |
| Cinemachine | İsteğe bağlı | Lab kamera; fiziğe bağımlı değil |

**Paket sürümlerini varsayımsal sabitleme.** Unity Hub/Package Manager'da
seçtiğin 6.3 patch'i ile doğrulanan paket sürümlerini kur; Unity'nin
`Packages/manifest.json` ve `packages-lock.json` dosyalarını commit et.
M0 UPM manifestinde **sıfır ek bağımlılık** vardır.

## Proje ayarları — ilk deney önerisi

| Unity menüsü | Ayar | Başlangıç |
|---|---|---|
| Project Settings > Editor | Version Control / Visible Meta Files | Meta dosyaları görünür olmalı |
| Project Settings > Editor | Asset Serialization | Force Text |
| Project Settings > Time | Fixed Timestep | `0.0166667` s (yaklaşık 60 Hz) |
| Project Settings > Time | Maximum Allowed Timestep | `0.1` s, ilk deney bütçesi |
| Project Settings > Physics | Gravity | `(0, -9.81, 0)` |
| Project Settings > Physics | Simulation Mode | Fixed Update / varsayılan otomatik |
| Project Settings > Physics | Solver Iterations | Önce varsayılan; sonra testlerle artır |
| Project Settings > Physics | Collision Layer Matrix | Body kendi wheel sensor query'sine çarpmasın |
| Project Settings > Player | Active Input Handling | Input System Package (New), kuruluysa |
| Player / Graphics | Color Space | Linear (3D görsel kalite için) |
| Scene | World scale | 1 Unity unit = 1 metre |

**Önemli:** Solver sayısını proje genelinde gelişigüzel 12/20 yapmak yerine
testlerle seç; yüksek değer CPU maliyetini artırır. Ağır römork zincirlerinde
per-body ayarlar denenebilir. `Auto Sync Transforms` varsayılanı korunmalı;
script ile Transform değiştirme + Physics query arasında senkronizasyon
sorunları gözlenirse bilinçli yönetilmeli.

## Fizik objeleri için proje standartları

- Ana chassis Rigidbody, üretimde **isKinematic=false** olacak; M0
  scaffold'ında `true` olması **bilinçli** çünkü solver yok.
- Fizik gövdesinde makul basit convex Box/Capsule/Convex Mesh Collider.
- Tekerlek render mesh'lerine ayrıca fizik sürtünme collider'ı bağlama;
  **double friction** veya yan etkili temas yaratabilir.
- Render objeleri ve fizik sensörleri için ayrı layer + Raycast LayerMask.
- Hızlı gövde çarpışmasında Interpolate / uygun CCD yalnızca testle seçilmeli.
- Motor torku, tire forces, raycasts gibi hesaplar fixed simulation loop'ta.
- Sahne objelerini zorla `transform.position` ile ışınlamak yerine Rigidbody
  API ve fizik tasarımına uygun hareket kullanılmalı.

## Git/GitHub sırası

1. Unity sahnesini `Assets/VehiclePhysicsLab/Scenes/M0_Scaffold.unity` kaydet.
2. `Visible Meta Files` ve `Force Text` kontrol et.
3. `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/`,
   `Assets/`, `Packages/com.progesor.kavra/`, `docs/`, `.gitignore` commit et.
4. `Library/`, `Temp/`, `Obj/`, `Build/`, `Logs/`, `UserSettings/` ekleme.
5. **İlk commit etiketi önerisi:** `chore: bootstrap KAVRA Unity project and M0 docs`.
6. İstersen ilk tag `v0.0.1` (sadece tasarım/iskelet aşaması).

## Kodu başka Unity projesine ekleme

**Şimdilik:** aynı embedded folder'ı hedef projenin `Packages/` altına kopyala.

**GitHub paketine dönüştürüldüğünde:** Unity Package Manager'da Git URL
ve monorepo alt yolunu kullanmayı düşünebiliriz; örneğin
`https://github.com/<owner>/<repo>.git?path=/Packages/com.progesor.kavra#v0.0.1`
(hedef deponun Git erişimi ve package metadata'sı gerekir). O zamana kadar
Embedded kullanmak kodu hızlı güncellemeyi kolaylaştırır.

## Hata olursa hızlı teşhis

- Menü yok → Package Manager'da embedded KAVRA görünüyor mu?
- Unity Console'da derleme hatası → tam log + Editor patch sürümünü kaydet.
- Paket görünmüyor → `Packages/com.progesor.kavra/package.json` tam yolda mı?
- `.asmdef` referansı hatası → Runtime ve Editor adlarına bak; Editor asmdef
  yalnızca Editor platformunda derlenmeli.
- KAVRA nesnesi hareket etmiyor → M0 scaffold'ında **beklenen durum**.
- 3W/20W preview tekerlekler fiziksel olarak taşımıyor → **beklenen durum**.

## Resmî referanslar

- Unity 6.3 LTS ve destek: https://unity.com/releases/unity-6/support
- 6000.3.26f1: https://unity.com/releases/editor/whats-new/6000.3.26f1
- Embedded package: https://docs.unity3d.com/Manual/upm-embed.html
- Assembly definitions: https://docs.unity3d.com/Manual/cus-asmdef.html
- Input System installation: https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/Installation.md
