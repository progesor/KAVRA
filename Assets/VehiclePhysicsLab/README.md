# VehiclePhysicsLab host project folder

This is the **host Unity project's** lab workspace, separate from the
reusable package under `Packages/com.progesor.kavra/`.

Recommended later structure:

```
Assets/VehiclePhysicsLab/
  Scenes/M0_Scaffold.unity
  Scenes/T01_StaticLoad.unity
  Scenes/T02_SpringDamper.unity
  Scenes/T03_StraightBrake.unity
  Scenes/T04_Skidpad.unity
  Prefabs/
  Materials/
  Tests/
```

The M0 ZIP intentionally contains no `.unity` scene: create/save the first
scene from the Wizard in the chosen Unity 6.3 Editor to obtain genuine,
Editor-generated scene data and `.meta` GUIDs.
