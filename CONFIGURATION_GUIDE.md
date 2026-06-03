# 難度系統配置指南

## 快速設置步驟

### 步驟 1：添加 DifficultyManager 到場景

1. 在場景中創建空 GameObject，命名為 `DifficultyManager`
2. 添加 `DifficultyManager.cs` Script 組件
3. 在 Spawner 中指定 Difficulty 欄位（預設為 Easy）

### 步驟 2：配置 FruitOption（水果選項）

在 DifficultyManager Inspector 中，設定 `allFruits[]` 陣列。每個 FruitOption 需要：

- **Fruit Name**: 唯一識別名稱（例：`apple`, `banana`, `grape_red`, `grape_purple`）
- **Display Name**: UI 顯示名稱（例：`紅蘋果`, `香蕉`, `紅葡萄`）
- **Prefab**: 水果對應的 GameObject prefab
- **Color**: 水果顏色（例：`Red`, `Yellow`, `Purple`, `Green`）
- **Shape**: 水果形狀（例：`Round`, `Cluster`, `Oval`）
- **Tags**: 特徵標籤陣列（例：`Rotten`, `Small` 等）

### 步驟 3：配置 Spawner

1. **Fruit Pool**: 配置 `FruitPrefab[]` 陣列
   - `Fruit Name`: 對應 DifficultyManager 中的 FruitName
   - `Prefab`: 水果 GameObject（可留空，系統會尋找 allFruits 中的 prefab）

2. **Difficulty**: 選擇預設難度（Easy/Medium/Hard）

3. 其他設定保持不變

### 步驟 4：確認遊戲流程

在 GameManager 中：
- 確保 `showStartMenu = true`
- 系統會在開始時顯示三個難度按鈕
- 點擊按鈕後遊戲開始

## 水果配置範例

```
Fruit 1:
- Fruit Name: apple_red
- Display Name: 紅蘋果
- Prefab: Prefabs/RedApple
- Color: Red
- Shape: Round
- Tags: []

Fruit 2:
- Fruit Name: apple_green
- Display Name: 綠蘋果
- Prefab: Prefabs/GreenApple
- Color: Green
- Shape: Round
- Tags: []

Fruit 3:
- Fruit Name: tomato_red
- Display Name: 紅番茄
- Prefab: Prefabs/RedTomato
- Color: Red
- Shape: Round
- Tags: []

Fruit 4:
- Fruit Name: grape_red
- Display Name: 紅葡萄
- Prefab: Prefabs/RedGrape
- Color: Red
- Shape: Cluster
- Tags: []

Fruit 5:
- Fruit Name: grape_purple
- Display Name: 紫葡萄
- Prefab: Prefabs/PurpleGrape
- Color: Purple
- Shape: Cluster
- Tags: []

... 更多水果
```

## 屬性過濾說明

### Easy 難度的選擇邏輯
```
1. 隨機選擇 1 個水果作為目標
2. 選擇 2 個干擾水果，滿足：
   - 顏色不同於目標水果
   - 形狀不同於目標水果
   
例：目標是紅蘋果(Red+Round)
   干擾可以是：紫葡萄(Purple+Cluster)、香蕉(Yellow+Oval)
```

### Medium 難度的選擇邏輯
```
1. 隨機選擇 1 個水果作為目標1
2. 選擇與目標1同形狀但不同顏色的水果作為目標2
3. 選擇 5 個干擾水果（可同形狀或完全不同）

例：目標1是紅葡萄(Red+Cluster)
   目標2可以是紫葡萄(Purple+Cluster)
   干擾可以是：蘋果、香蕉等任何其他水果
```

### Hard 難度的選擇邏輯
```
1. 隨機選擇 1 個水果作為目標1
2. 選擇與目標1同顏色且同形狀但名稱不同的水果作為目標2
3. 再選擇 1 個同顏色同形狀但名稱不同的水果作為目標3
4. 選擇 7 個干擾水果

例：目標1是紅蘋果(Red+Round)
   目標2可以是紅番茄(Red+Round)
   目標3需要找到第三種紅色圓形水果
   干擾是其他所有水果
```

## 屬性命名約定

### 常見顏色
- Red（紅）
- Green（綠）
- Yellow（黃）
- Purple（紫）
- Orange（橙）
- Blue（藍）

### 常見形狀
- Round（圓形）
- Cluster（束狀）
- Oval（橢圓）
- Elongated（細長）
- Heart（心形）

### 常見標籤
- Rotten（腐爛）
- Small（小個）
- Large（大個）
- Spotted（有斑點）
- Striped（有條紋）

## 調試技巧

1. **檢查水果是否正確生成**：在 Spawner.SpawnWave() 中添加 Debug.Log
2. **驗證難度設定**：在 DifficultyManager.GenerateLevelFruits() 中添加 Debug
3. **檢查屬性匹配**：測試 PropertyFilter.Matches() 方法

## 常見問題

### Q: 水果沒有生成
A: 檢查 DifficultyManager 的 `allFruits[]` 是否已配置

### Q: 難度按鈕沒有顯示
A: 確保 GameManager 的 `showStartMenu = true`

### Q: 選擇了難度但遊戲沒有開始
A: 檢查 Spawner 是否正確關聯到場景中

### Q: Go/NoGo 判定不正確
A: 確保所有水果都有正確的 `fruitName`，並檢查目標水果清單是否正確生成
