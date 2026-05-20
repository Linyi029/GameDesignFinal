# GameDesignFinal

這是一個 Unity 2D Go / No-Go 反應遊戲。玩家需要在 `Go` target 進入 action zone 時點擊它，並避免點擊 `No-Go` target。遊戲會記錄分數與 accuracy，並在玩家 accuracy 超過指定 threshold 時提高難度。

## 遊戲玩法

- 當 `Go` target 位於 action zone 內時點擊，會判定為 hit。
- 沒有點到 `Go` target，或在 action zone 外點擊 `Go` target，會判定為 miss。
- 不要點擊 `No-Go` target。
- 讓 `No-Go` target 離開畫面，會判定為 correct rejection。
- 點擊 `No-Go` target，會判定為 false alarm。

## 計分規則

- Hit：`+100`
- Correct rejection：`+100`
- False alarm：`-100`
- Miss：`-50`

Accuracy 計算方式：

```text
(Hit + Correct Rejection) / (Hit + Correct Rejection + Miss + False Alarm)
```

## 難度提升

`GameManager` 會使用 `maxClicks` 限制每一輪可以點擊的次數。

當玩家用完所有 clicks：

- 如果 `Accuracy > threshold`，遊戲會啟用進階生成模式：
  - target 會以 wave 的方式一次生成多顆
  - 每一波剛好只有一顆 `Go` target
  - 同一波其他 target 都是 `No-Go`
  - target 速度會依照 `speedIncreaseMultiplier` 提升
  - click 數量會重設，進入下一輪
- 如果 `Accuracy <= threshold`，遊戲會使用 `Time.timeScale = 0f` 暫停。

## 主要腳本

### `GameManager.cs`

負責分數、click 次數限制、accuracy、action zone 判斷，以及難度提升。

重要 Inspector 欄位：

- `Spawner`：target spawner 參考。如果沒有指定，腳本會自動尋找場景中的 spawner。
- `Action Zone`：可點擊區域的中心 transform。
- `Radius X`：action zone 的水平半徑。
- `Radius Y`：action zone 的垂直半徑。
- `Max Clicks`：檢查 accuracy 前，每一輪允許的 click 次數。
- `Threshold`：提升難度需要達到的 accuracy。例如 `0.8` 代表 80%。
- `Speed Increase Multiplier`：超過 threshold 後套用到 target 速度上的倍率。

### `Spawner.cs`

負責 target 生成、Go / No-Go 比例、生成時間、wave 數量，以及 target 速度。

重要 Inspector 欄位：

- `Go Prefab`：Go target 使用的 prefab。
- `No Go Prefab`：No-Go target 使用的 prefab。
- `No Go Chance`：單顆模式下生成 No-Go 的機率。
- `Min Spawn Wait`：兩波 target 之間的最短等待時間。
- `Max Spawn Wait`：兩波 target 之間的最長等待時間。
- `Spawn Multiple Targets`：是否每一波生成多顆 target。
- `Min Targets Per Wave`：多顆模式下，每一波最少生成幾顆 target。
- `Max Targets Per Wave`：多顆模式下，每一波最多生成幾顆 target。
- `Same Wave Lane Spacing`：同一波 target 之間建議保持的最小 Y 軸距離。
- `Lane Jitter`：當一波 target 數量超過 lane 數量時，用來減少重疊的隨機 Y 軸偏移。
- `Base Target Speed`：新生成 target 的基礎速度。
- `Speed Multiplier`：套用到基礎速度上的目前倍率。

### `Target.cs`

負責移動每一顆 target，並在 target 離開遊戲區域時回報 miss 或 correct rejection。

重要 Inspector 欄位：

- `Type`：`Go` 或 `NoGo`。
- `Speed`：target 移動速度，通常由 `Spawner` 指定。
- `Move Direction`：target 的移動方向。
- `Destroy X Limit`：target 超過這個 X 邊界時，會被判定為 miss 或 correct rejection。

## 注意事項

- 在多顆模式下，`noGoChance` 會被忽略，因為每一波會固定限制為一顆 `Go` target。
- `sameWaveLaneSpacing` 和 `laneJitter` 可以減少重疊，但 prefab 的 collider 大小仍然會影響結果。如果 target 生成時仍然碰撞，可以提高 spacing/jitter，或增加更多 lane。
- `Accuracy` 在 `GameManager` 中有序列化，因此可以在 Unity play mode 時從 Inspector 觀察數值變化。
