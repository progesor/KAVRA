# 12 — Devir / yeni sohbette devam talimatları

## Proje

**KAVRA Vehicle Framework** (`com.progesor.kavra`) Unity 6.3 LTS, Universal 3D
lab host, PC başlangıç, fizik çekirdeği render/input bağımsız.

Kullanıcı bir Unity projesi açacak, `KAVRA_Unity_Starter` ZIP içeriğini
projenin köküne ekleyecek, GitHub repo olarak yayınlayacak ve yeni repo URL'si
ile geri dönecek.

## Başlangıç dosyalarının gerçek durumu

- `README.md`, 13 adet `docs/*.md` tasarım/araştırma belgeleri.
- Embedded UPM `Packages/com.progesor.kavra/package.json` v0.0.1.
- Runtime: VehicleDefinition, SurfaceProfile, VehicleRoot, VehicleWheelAnchor,
  VehicleConfigurationValidator.
- Editor: VehicleTemplateFactory, VehicleScaffoldBuilder, KavraWizardWindow.
- Şablonlar 3/4/6/8/20 teker single-chassis preview üretir.
- **Tekerlek/süspansiyon/motor fiziği henüz uygulanmadı.**
- Unity Editor derlemesi henüz yapılmadı; statik dosya kontrolü yapıldı.

## İlk kullanıcı geri bildirimi nasıl alınmalı?

1. Unity Hub Editor sürümü ve projenin tam sürümünü teyit et.
2. `Tools > KAVRA > Vehicle Setup Wizard` açılıyor mu?
3. 3W/4W/20W definition asset'leri ve scaffold oluşturuluyor mu?
4. Unity Console hatası veya import warning varsa tam log iste.
5. `Packages/com.progesor.kavra` altındaki `.meta` dosyaları commit edilmiş mi?
6. GitHub repo linkinden son commit ve dosya yapısını kontrol et.
7. Sonrasında M0 wheel/physics prototip issue'larını aç ve test sahnesi kur.

## Sonraki kodlama sırası (öneri)

- `WheelContactProvider`: raycast vs spherecast karşılaştırma ve layer mask.
- `SuspensionSolver`: spring/damper/bumpstop ve tire load telemetry.
- `WheelRotationalState`: wheel inertia ve drive/brake torque.
- `TireForceSolver`: basic combined-slip.
- `VehicleSimulationRoot`: FixedUpdate, Rigidbody AddForceAtPosition.
- `PhysicsLab`: test setup, static load, straight acceleration, skidpad.
- M0 kabul tamamlanmadan detaylı Wizard, arazi deformasyonu veya
  ağır hasar sistemine geçme.

## Kullanıcı tercihleri / projeye özel beklentiler

- Çok kapsamlı ama **pragmatik** gerçekçilik; FEM lastik veya full multibody
  suspension zorunlu değil.
- MudRunner örneği: fiziksel çekiş + görsel çamur ayrımı, ileride eklenebilir.
- Başka Unity oyunlarına entegre edilen modüler package sistemi.
- İsteğe bağlı arcade sürüş, çok farklı aks/tekerlek düzenleri, ileride hasar.
- Kullanıcı proje başlangıcında GitHub repo oluşturmayı kendi yapacak.

## Kapsam dışı iddialardan kaçın

- M0 Wizard bir **preview scaffold**, oynanabilir araç değildir.
- 20W tek gövdeli test, çekici+römork simulasyonu değildir.
- Kaynak linkleri örnek mimari sağlar; gerçek doğruluk/performans ölçülmedi.
- Unity derlenmesi kesin kabul edilmeden "hatasız çalışıyor" deneme.
