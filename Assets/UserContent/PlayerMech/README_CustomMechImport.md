# Custom Player Mech Import

Put your custom player mech prefab here:

`Assets/UserContent/PlayerMech/PlayerMech.prefab`

The Phase 1 demo checks this exact path in Unity Editor Play Mode.

Rules:

- The file must be a Unity prefab.
- FBX can be used after Unity imports it and you make a prefab from it.
- GLB is not loaded directly in Phase 1; import it with a glTF pipeline first, then make a prefab.
- The prefab is only used as the visual model. Controls, health, weapons, and equipment sockets stay on `PlayerMechRoot`.

If your model has a different size or pivot, adjust the socket transforms under:

`PlayerMechRoot/Hardpoints`
