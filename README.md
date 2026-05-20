# GameDesignFinal

Unity 2D Go / No-Go reaction game. The player clicks Go targets inside the action zone and avoids No-Go targets. The game tracks score, accuracy, and can increase difficulty when the player's accuracy passes a configurable threshold.

## Gameplay

- Click a `Go` target while it is inside the action zone to score a hit.
- Missing a `Go` target or clicking it outside the action zone counts as a miss.
- Do not click `No-Go` targets.
- Letting a `No-Go` target leave the screen counts as a correct rejection.
- Clicking a `No-Go` target counts as a false alarm.

## Scoring

- Hit: `+100`
- Correct rejection: `+100`
- False alarm: `-100`
- Miss: `-50`

Accuracy is calculated as:

```text
(Hit + Correct Rejection) / (Hit + Correct Rejection + Miss + False Alarm)
```

## Difficulty Progression

`GameManager` tracks a limited number of clicks per round using `maxClicks`.

When the player uses all clicks:

- If `Accuracy > threshold`, the game enables advanced spawning:
  - targets spawn in waves of multiple targets
  - each wave has exactly one `Go` target
  - all other targets in that wave are `No-Go`
  - target speed increases by `speedIncreaseMultiplier`
  - click count resets for the next round
- If `Accuracy <= threshold`, the game pauses with `Time.timeScale = 0f`.

## Main Scripts

### `GameManager.cs`

Controls scoring, click limits, accuracy, action-zone checks, and difficulty progression.

Important Inspector fields:

- `Spawner`: target spawner reference. If empty, the script finds one in the scene.
- `Action Zone`: transform used as the center of the clickable action zone.
- `Radius X`: horizontal action-zone radius.
- `Radius Y`: vertical action-zone radius.
- `Max Clicks`: number of clicks allowed before checking accuracy.
- `Threshold`: accuracy required to increase difficulty. Example: `0.8` means 80%.
- `Speed Increase Multiplier`: multiplier applied to target speed after passing the threshold.

### `Spawner.cs`

Controls target generation, Go / No-Go balance, spawn timing, wave size, and target speed.

Important Inspector fields:

- `Go Prefab`: prefab used for Go targets.
- `No Go Prefab`: prefab used for No-Go targets.
- `No Go Chance`: No-Go spawn chance in single-target mode only.
- `Min Spawn Wait`: minimum time between spawn waves.
- `Max Spawn Wait`: maximum time between spawn waves.
- `Spawn Multiple Targets`: whether each wave spawns multiple targets.
- `Min Targets Per Wave`: minimum targets in a multi-target wave.
- `Max Targets Per Wave`: maximum targets in a multi-target wave.
- `Same Wave Lane Spacing`: minimum preferred Y spacing between targets in the same wave.
- `Lane Jitter`: random Y offset used to reduce overlap when a wave has more targets than lanes.
- `Base Target Speed`: base speed assigned to new targets.
- `Speed Multiplier`: current multiplier applied to base speed.

### `Target.cs`

Moves each target and reports misses or correct rejections when targets leave the play area.

Important Inspector fields:

- `Type`: `Go` or `NoGo`.
- `Speed`: target movement speed. Usually assigned by `Spawner`.
- `Move Direction`: direction the target moves.
- `Destroy X Limit`: X boundary where the target is counted as missed or correctly rejected.

## Notes

- In multi-target mode, `noGoChance` is ignored because each wave is intentionally limited to one `Go` target.
- `sameWaveLaneSpacing` and `laneJitter` reduce overlap, but prefab collider size still matters. If targets still collide at spawn, increase spacing/jitter or add more lanes.
- Accuracy is serialized in `GameManager`, so it can be watched in the Unity Inspector during play mode.
