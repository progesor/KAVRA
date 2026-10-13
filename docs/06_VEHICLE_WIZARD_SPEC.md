# 06 — Vehicle Wizard tasarımı ve mevcut M0 iskeleti

## Ürün hedefi

3D model (FBX/GLB içeri aktarıldıktan sonra Unity sahnesi) ve seçilen araç
mekanikleriyle tek bir akıştan yüksek kaliteli fizik prefab'ı üretmek.

## Kullanıcı akışı — plan

1. **Vehicle Class:** 3W, car, heavy, industrial veya custom.
2. **Model Import/Assignment:** Mesh root, chassis collider, wheel mesh ve
   pivotlar. Otomatik isim/bounds tahmini; kullanıcı düzeltmesi.
3. **Axle Layout:** Ekranda aks ekleme, silme, taşıma; tekerlek adedi,
   dual tire, lift axle, driven/steered/parking brake bayrakları.
4. **Suspension:** Eşdeğer kinematik, spring/damper/stroke, anti-roll.
5. **Powertrain:** Power source, clutch, transmission, torque graph, diff.
6. **Surface/Tire:** Tire force model, yüzey profil grubu, tire radius.
7. **Behavior:** Simulation/Sport/Arcade, input/control aid profil.
8. **Validation & Preview:** Scene gizmos, statik yük ve geometri uyarıları,
   otomatik test sürüşü, telemetry.
9. **Build Prefab:** Ayrı data assets ve host input/visual adapter ile
   kullanılabilir fizik prefab'ı.

## M0 paketinde GERÇEKTEN çalışan özellikler

Menü: `Tools > KAVRA > Vehicle Setup Wizard`

- Çalışan `VehicleDefinition` ScriptableObject varlığı oluşturma.
- `ThreeWheelDelta`, `PassengerCar4`, `Truck6x6`, `Truck8x8`,
  `SingleChassis20WheelStressTest` hazır temel yerleşimleri.
- Axle ve wheel ID doğrulaması, boyut / kütle kontrolleri.
- Kinematic Rigidbody + önizleme şasi küpü + tekerlek silindirleri +
  konumlandırılmış `VehicleWheelAnchor` nesneleri.
- Wheel Anchor seçildiğinde gizmo üzerinde radius ve suspension travel gösterimi.

**M0 sınırları:** İçe aktarılan modelin wheel mesh'lerini otomatik bulmuyor,
süspansiyon/tire solver çalıştırmıyor, aracı sürmüyor, prefab `.prefab`
asset'i oluşturmuyor (şimdilik **Scene** GameObject üretiyor).

### Kullanıcı için M0 test protokolü

1. `Tools > KAVRA > Vehicle Setup Wizard` aç.
2. `PassengerCar4` seç ve Assets'e tanımı kaydet.
3. Definition Inspector'da dört wheel, iki axle olmalı.
4. `Build non-drivable scene scaffold` bas.
5. Hierarchy'de `KAVRA_Passenger Car 4W`, dört `Wheel_*` nesnesi olmalı.
6. Rigidbody'nin `isKinematic=true` olduğunu doğrula. Play sırasında kendi
   kendine gitmesini bekleme.
7. 3W ve 20W stress layout ile 3/20 wheel anchor kontrol et.
8. Wheel ID'yi kopyalayarak duplicate oluştur; Wizard Error vermeli ve build
   düğmesi devre dışı kalmalı.

## Editör UX notları

- Tanımlayıcı güçlü uyarılar; birim etiketleri.
- Gelen modelin ölçeği yanlışsa (mm/cm → m) otomatik düzeltmek yerine
  kullanıcıya anlaşılır uyarı.
- Tekerlek mesh merkezini, fizik temas noktasını ve görsel dönüş pivotunu
  ayrı alanlar olarak ele al.
- Yalnızca seçili tekerlek için detay gizmo (büyük sahnelerde karışıklık yok).
- Heavy preset doğrudan "18 Wheeler" diye adlandırılmayacak; çünkü
  tipik 18 tekerli çekici+römork **çok gövdeli** bir sistem.

## Gelecek veri sınıfları

`VehicleAssembly`, `ChassisDefinition`, `AxleGroupDefinition`,
`WheelDefinition`, `SuspensionDefinition`, `TireDefinition`,
`PowertrainGraphAsset`, `BehaviorProfile`, `VehiclePrefabManifest`.

Kısmen çakışan `VehicleDefinition` mevcut M0 asset yapısını kırmadan
migration planı ile evrimleştirilmeli.
