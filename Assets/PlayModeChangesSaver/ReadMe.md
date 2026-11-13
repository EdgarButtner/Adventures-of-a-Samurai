# Play Mode Changes Saver

**Play Mode Changes Saver** is an editor tool that captures, reviews and applies the changes you make during Play Mode. The workflow follows Unity's Prefab Overrides system, so no iteration is lost.

> **Need help?** For a visual guide, please check out the **[\[demo video\]](https://assetstore.unity.com/packages/slug/354984)** on the Unity Asset Store page. A detailed manual is included as `README.pdf` in this folder.

**Version:** 1.1.0 · **Requires:** Unity 2022.3 LTS or newer


## Key Features
* **Automatic Tracking:** Snapshots all GameObjects, components and names when you enter Play Mode. Nothing to configure.
* **Inspector Integration:** A "Play Mode Overrides" panel appears on every GameObject with detected changes, with *Apply All*, *Revert to Original* and *Revert to Saved*.
* **Side-by-Side Comparison:** Original values on the left, current Play Mode values on the right, per component.
* **Overrides Browser:** One window for all recorded changes of the open scenes, in Edit Mode and Play Mode (`Tools > Play Mode Overrides Browser`).
* **Guided Apply after Play Mode:** A dialog asks which changes to keep; changes in other scenes are applied scene by scene.
* **Prefab-Aware:** Changes on prefab instances are written to the right instance as regular prefab overrides. The prefab asset stays untouched.
* **Reliable Object Matching:** Objects are identified by their GlobalObjectId, with the hierarchy path as fallback, so renamed objects and objects with the same name are handled correctly.
* **Full Undo/Redo** for everything applied after Play Mode.

## What Is Tracked
* **Transform & RectTransform:** Position, rotation, scale, anchors, pivot, size and offsets.
* **Component Properties:** All serialized fields of built-in components and your own MonoBehaviours: numbers (including 64-bit), booleans, strings, enums and flag combinations, colors (including HDR), vectors, quaternions, rects, bounds, layer masks, gradients, animation curves, arrays and lists (including their size), nested classes and structs, object references.
* **Component State:** The enabled checkbox of components and the connected body of joints.
* **Renderer Materials:** Material assignments on renderers.
* **GameObject Names:** Name changes made in Play Mode.

## Limitations
* Fields marked `[HideInInspector]` are not tracked.
* Material instances created at runtime (`renderer.material`) are not saved; the material slot is left unchanged and a warning is logged.
* Objects created at runtime and changes to ScriptableObject assets are not tracked.
* Changes in an unsaved (untitled) scene cannot be applied, because the scene has no file yet. Changes in saved scenes loaded next to it are applied.

---


## How to Use

### A. Inspector Integration (Per Object)
1. **Enter Play Mode:** The tool snapshots the loaded scenes automatically.
2. **Make Changes:** Adjust objects, components or values as you normally would.
3. **Review Changes:** As soon as a change is detected, the **Play Mode Overrides** button in the Inspector header becomes active. Click it to see the changed components (and the GameObject name, if it changed).
   - **Quick Actions:** *Revert to Original*, *Revert to Saved* or *Apply All* for the whole GameObject.
   - **Detailed Comparison:** Click a component in the list to open the **Compare View**. It shows the original values (read-only) on the left and the current values on the right. Use *Apply*, *Revert to Original* or *Revert to Saved* for this component only.
4. **Keep in mind:** Only changes you **apply** during Play Mode are kept. Everything else is reverted by Unity when Play Mode ends, as usual.
5. **Exit Play Mode:** The applied changes are stored and the apply dialogs start.

### B. Post-Play Mode Workflow & Dialogs
After exiting Play Mode, the tool goes through every scene with applied changes, starting with the active scene:
* **Apply Overrides:** For each scene you choose *Apply* (write the changes into the scene) or *Discard* (drop them).
* **Loaded scenes** are applied where they are, so a multi-scene setup stays as it is.
* **Scenes that are not loaded** (for example a scene you switched to in Play Mode): the **Scene Switch** dialog asks to *Open* the scene. It is opened additively next to the open scenes, and then the *Apply Overrides* dialog follows. *Skip remaining* ends the flow; the remaining scenes are not changed.
* **Close Scenes:** At the end, scenes the tool opened can be closed again (*Close* / *Keep Open*). Unity asks whether to save them first.
* Applied changes are not saved automatically. Save the scenes as usual (`Ctrl+S` / `Cmd+S`), or undo them with `Ctrl+Z` / `Cmd+Z`.

### C. Overrides Browser
Open the browser via `Tools > Play Mode Overrides Browser` to keep an overview during both Edit and Play Mode:
* **Central Overview:** Lists every GameObject of the open scenes that has recorded overrides, grouped by scene.
* **Compare and Revert:** Click a component or the name entry to open the Compare View, for example to revert an applied change.
* **Refresh / Clear:** *Clear* removes all recorded overrides after a confirmation. Changes that were already applied to scenes stay in the scenes.


---

## Technical Specifications
* **Editor-Only:** No runtime code, no runtime overhead, nothing ends up in your builds.
* **Compatibility:** Unity 2022.3 LTS or newer. Tested on 2022.3.62f3, 6000.0.76f1 and 6000.6.4f1.
* **Render Pipelines:** Independent of the render pipeline (Built-in, URP, HDRP).
* **Enter Play Mode Options:** Works with Domain Reload and Scene Reload disabled.
* **Undo/Redo:** Everything applied after Play Mode is registered with Unity's Undo system (`Ctrl+Z` / `Cmd+Z`, redo with `Ctrl+Y` / `Cmd+Shift+Z`).
* **Multi-Scene Support:** Handles changes across additively loaded scenes and scenes loaded during Play Mode.
* **No Dependencies:** No third-party code or packages. The optional demo uses Unity UI (uGUI).
* **Storage:** Overrides are kept in ScriptableObject assets in the `Scriptable_Objects` folder of the package. The folder is created automatically, also when the package is moved (e.g. to `Assets/Plugins`). The stores are cleared when you enter Play Mode the next time.
* **Console Messages:** Warnings of the tool start with `[Play Mode Changes Saver]`, for example when a material slot cannot be resolved or a stored value no longer matches its field.


## Demo
The optional demo is in `DemoFiles` (two scenes, a test script with many field types and a few materials). A step-by-step tutorial is included as `DemoFiles/DemoTutorial.pdf`.
* The scene switch buttons work in the Editor without adding the demo scenes to the Build Settings; keep `Scene01` and `Scene02` together in `DemoFiles/Demo_Scenes`.
* The demo uses Unity UI (uGUI) with the legacy Input Manager (`StandaloneInputModule`).
* The demo materials are Built-in Standard materials. In URP or HDRP they appear pink; convert them with `Window > Rendering > Render Pipeline Converter`, or ignore the colors – the demo works either way.


## Known Issues
* **Compare View:** Text and number fields in the right column cannot be edited with the keyboard, because Unity popup windows do not accept keyboard focus. Checkboxes, object fields and other mouse-driven controls work. Edit the value in the Inspector instead and apply it there.


## Changelog
**1.1.0** (October 2026): Support for Unity 2022.3 LTS and Enter Play Mode Options; enabled checkbox, joint connected body and array sizes are tracked; prefab instances, renamed and same-named objects are matched reliably; multi-scene setups stay loaded during the apply flow; many fixes for value types, revert and material instances. See `README.pdf` for the full list.

**1.0.1** (May 2026): Fixes for prefab instances in the Compare View and the Browser, duplicate store entries, name changes and Revert to Saved.

**1.0** (February 2026): Initial release.


## Support & Contribution
If you encounter any issues, please do not hesitate to contact me!

* **GitHub:** [\[github.com/062Leo\]](https://github.com/062Leo)

*  [![Contact Me](https://img.shields.io/badge/Contact-Me-blue?style=for-the-badge&logo=maildotru)](https://tally.so/r/KYx5ak)


**Note:** This tool is provided for free. If you find it helpful, all I ask is that you leave a **positive rating and a review** on the Unity Asset Store. Your feedback helps a lot!