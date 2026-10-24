# Workflow tools

## Bulk import

**Tools > Resonance > Bulk Import Clips** (or **Bulk Import** in the Studio) turns a folder of clips into Sound Events
in one step.

- Clips that differ only by a trailing number are variations of one event with a Random container:
  `door_open_01`, `door_open_02` and `door_open_03` become the event `door_open`.
- Pick a **Switch Group** and clips whose name ends with one of its values are grouped by it: `step_grass_01`,
  `step_grass_02` and `step_stone_01` become one event `step` with a Switch container on the group, a Random container
  for grass and one for stone. Clips that match no value become the switch's fallback.
- New events get a little volume and pitch randomization when they have several clips.

The window shows what will be created before you confirm.

## Audit

The **Audit** tab checks the whole project:

| Problem | Severity |
| --- | --- |
| A Sound Event plays nothing, a clip node has no clip, a switch has no group, a blend has no parameter | Error |
| A Music Track has no segment, a segment has no clip, a link or a "next" points to a segment that does not exist | Error |
| A bus is its own ancestor | Error |
| An event has no bus, a container is empty, a switch child plays for a value its group does not have | Warning |
| A Sound Event is not referenced by any scene, prefab or asset | Info (only with **Check With References**) |

Click a problem to open the asset in the Studio. The tab also lists the clips whose import settings can improve (see
[Memory and loading](memory.md)) with a button to apply them all.

## Loudness

The Audit tab measures the loudness of every clip (gated RMS over 400 ms blocks, the gating used by broadcast loudness
meters, without frequency weighting) and lists the events whose variations differ by more than 6 dB: one footstep much
louder than the others, a quiet impact in a set of loud ones. **Balance** sets each clip node's volume so every
variation sounds as loud as the middle one; Undo works. A clip's loudness and peak are also shown under its waveform.

## Used By

The **Used By** button of the Sound Event editor lists every scene, prefab and asset that references the event. Click
one to show it in the Project window. Events played only from code by a script field are found too, through the
prefab or scene that holds the script.
