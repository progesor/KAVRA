# KAVRA — 19 Ekim 2026 Unity P0 EditMode test inceleme ve düzeltmeler

**Kaynak kanıt:** Kullanıcı tarafından paylaşılan `TestResults.xml`,
NUnit/Unity Test Framework EditMode, 19 Ekim 2026 06:23:39–06:23:41 UTC.

| Ölçü | Gözlemlenen |
|---|---:|
| Unity Editor | 6000.3.18f1 (kullanıcı host projesi) |
| Toplam test vakası | **117** |
| PASS | **111** |
| FAIL | **6** |
| Skipped | 0 |
| Yeni definition validator suite | **10/10 PASS** |
| Prefab save/import/load suite | **4/4 PASS** |
| M1.5 chain suite | 9/10 |
| M1.3 assembly suite | 8/9 |
| Hitch lifecycle suite | 13/14 |
| Physics suite | 67/70 |

Bu ilk gerçek NUnit raporu **derleme engelinin test assembly'sini
çalıştırmayı önlemediğini** kanıtlar; kullanıcıda Unity Console tümüyle
0 hata / Play Mode / M1.5.1 checker görseli tamamlandı anlamına GELMEZ.

## Başarısız vakalar ve kod kök nedenleri

| Suite / test | XML kanıtı | Kaynakta yapılan işlem |
|---|---|---|
| Physics / `WheelContactKeepsDistanceAtStaticRideHeight(Raycast)` | `Contact for Raycast` -> false | `WheelContactProbe.FindClosestValid` null-ownBody filtresi |
| Physics / `WheelContactKeepsDistanceAtStaticRideHeight(SphereCastWithRayFallback)` | SphereCast fallback -> false | Aynı statik zemin filtre düzeltmesi |
| Physics / `RaisedProbeCanDetectContactAfterMountCrossesRoadSurface` | Gömülü mount için contact=false | Aynı statik zemin filtre düzeltmesi |
| Hitch / `PhysicsBreakNotificationSetsBrokenStateAndReleasesTrailerBrakes` | Unity `ShouldRunBehaviour()` assertion | EditMode `SendMessage` yerine özel callback'in reflection ile sınanması |
| Chain / `M15FrontJointBreakLeavesRearPairButCutsTractorBrakes` | Unity `ShouldRunBehaviour()` assertion | Aynı EditMode callback düzeltmesi |
| Assembly / `M13ManualRRecoversBothBodiesWithoutPullingTheJointApart` | `Transform.up` hâlâ yatık: -0.866 | EditMode fizik pose testi, `Rigidbody.rotation` üzerinden yapıldı |

### 1. Statik zemin teması — gerçek runtime düzeltmesi

`WheelContactProbe.FindClosestValid` başlangıçta şunu kontrol
ediyordu: `collider.attachedRigidbody == ownBody`.
`ownBody == null` olduğunda, **statik zeminin** attachedRigidbody
değeri de null olduğundan üç testin zemini yanlış eleniyordu. Artık
bu karşılaştırma sadece `ownBody != null` ise kullanılıyor.
Başka collider filtreleri korunuyor. Ek regression:
`NullOwnBodyDoesNotExcludeStaticRoadFromContactProbe`.

### 2. Joint break — sadece EditMode test yürütme yöntemi

Gerçek `OnJointBreak(float)` callback'i **production runtime'da
değiştirilmedi**. Unity EditMode, `SendMessage("OnJointBreak")`
yürütülürken `ShouldRunBehaviour()` engine assertion üretmiş.
Tests, joint'i `DestroyImmediate` ile kaldırdıktan sonra aynı
private callback'i reflection ile çağırıyor. Bu **fiziksel joint
overload** testi yerine geçmez; K / joint break Play Mode açık.

### 3. Manual R recovery — EditMode pose karşılaştırması

M1.3 testinde `VehicleManualRecovery.TryRecover` `Success` dönmüş,
fakat ilk `a.transform.up` assertion değerini -0.866 ölçmüş.
Unity'nin Rigidbody rotation API'si değişen dönüşün Transform'a
sonraki physics step'te yansıtabileceğini söylüyor. Test şimdi
`a.rotation * Vector3.up` / `b.rotation * Vector3.up` ile
**fizik pose** değerlerini kontrol ediyor. Runtime manual R
çalışma koşulları değiştirilmedi, otomatik upright eklenmedi.
Gerekirse Play Mode görsel senkron ayrı test edilmeli.

## Test sonrası Unity kapanması: açık analiz

XML bütün 117 vakanın tamamlandığını ve sonuçların kaydedildiğini
gösteriyor. Unity'nin sonradan **normal şekilde mi kapandığı**,
yoksa gerçekten native crash yaşayıp yaşamadığı XML'de yok.
Kullanıcı test bitince crash bildirdi, bu rapor sonucu reddetmez.

Önceki Windows runner yanlışlıkla `-runTests` ile `-quit`
kombinasyonu kullanıyordu. Unity 6000.3 Test Framework/Editor CLI
belgelerinde **birlikte kullanımı desteklenmiyor**; `-quit` testler
bitmeden kapanmayı tetikleyebilir. Betikten `-quit` çıkarıldı.
Bu hatanın kullanıcının bildirdiği crash'in kök nedeni olduğu
kanıtlanmış değildir.

- Batch runner log ve XML: `%TEMP%\\KAVRA-Unity-Acceptance\\<run-id>`
- GUI Unity Editor log: `%LOCALAPPDATA%\\Unity\\Editor\\Editor.log`
- Windows native crash report: `%TMP%\\Unity\\Editor\\Crashes`

Resmî Unity 6.3 CLI:
https://docs.unity.com/en-us/engine/6000.3/manual/programming-environment/test-framework-introduction/reference-command-line

## Tekrar kabul

1. Feature dalı güncellensin, Unity 6000.3.18f1 GUI kapatılsın.
2. `scripts/Run-KavraUnityEditMode.ps1` çalıştırılsın (betikte
   **-quit yok**); NUnit XML/log/Acceptance-summary.txt saklansın.
3. Yeni kaynak için tüm altı suite ve ek regresyonun PASS sonucu
   görülmeden P0 kapatılmasın.
4. Gerçek crash tekrarlanırsa XML ile beraber Editor.log ve mümkünse
   native crash report'un metin kısmı incelensin.
5. Görsel/Play Mode H/J/L/K/R, fiziki solver kopması, bağlı alt-grup R
   recovery'si ve eski M0/M1.4 sahne regresyonu ayrı kabul edilmeli.

**Koruma:** `.unity`, `.prefab`, `docs/Telemetry/` ve kullanıcı
konfigürasyonları değiştirilmedi. Versiyon v0.1.6, PR #16 Draft,
issue #17 açık, `main`e merge yok.
