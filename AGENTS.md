# Vehicle Framework BZ — API compatibility

This repository provides a framework/API consumed by independently compiled vehicle mods and serialized Unity assets. Compatibility is a binding requirement for future changes.

- Preserve the existing public and protected interface: type names, namespaces, member names, visibility, signatures, field/property distinctions, constructors, interfaces, virtual members and override behavior. Do not change these contracts without the user's explicit approval of the specific API change.
- Preserve the runtime assembly identity (`VehicleFramework`, assembly version `1.0.0.0`), `VehicleFramework.*` namespaces and existing BepInEx dependency GUID. Project folder and manifest names are separate from runtime identity.
- Preserve Unity serialization contracts: field names, types, visibility/attributes, reference ownership, prefab clone remapping and serialized event objects/targets. Do not turn fields into properties or otherwise alter serialized layouts as part of a port or cleanup.
- Preserve prefab IDs, equipment slot names, save keys and other externally consumed identifiers.
- Adapt BZ implementation details while retaining existing vehicle-facing contracts. Where a required game API difference prevents compatibility, report the concrete conflict and obtain explicit approval before changing the affected framework contract.
- Do not refactor unrelated code or narrow existing member visibility during porting. Existing API shape takes precedence over style preferences.
- Validate relevant changes against unchanged consumer binaries and prefab cloning/serialization, in addition to compilation. Compilation alone does not establish API or runtime compatibility.

For baseline evidence and regression checks, see `Docs/VehicleFrameworkBZ-BinaryCompatibility.md` and `Docs/VehicleFrameworkBZ-SerializationAudit.md` in the containing BelowZeroThunderKitMods project.
