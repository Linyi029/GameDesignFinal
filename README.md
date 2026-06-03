# TODO

## 🔴 高優先度

### 1. 修正 Could not pick a fruit for spawn

原因：

- Fruit Pool 數量不足
- DifficultyConfig 要求水果種類超過實際資料數量

需檢查：

- DifficultyConfig
- GenerateLevelFruits()
- Fruit Pool

避免：

需要 3 種水果
但實際只有 2 種水果

---

### 2. 驗證每種水果需求數量

確認：

requiredHitsByFruit

與

currentHitsByFruit

更新正確。

需驗證：

- 顯示數量
- 實際完成數量
- 通關判定

完全一致。

---

### 3. 驗證升級關卡後的資料重置

確認：

- requiredHitsByFruit.Clear()
- currentHitsByFruit.Clear()
- roundHits 重置
- health 重置
- accuracy 重置

避免上一關資料殘留。

---

## 🟡 中優先度

### 4. Intro 黑板 UI 美化

依照設計稿：

- 木框黑板背景
- 水果圖示
- 任務列表
- START 按鈕
- 限制步數

---

### 5. 升級關卡後重新顯示 Intro

流程：

Win
↓
Difficulty++
↓
Generate New Targets
↓
Show Intro Panel
↓
Start Next Level

---

### 6. 遊戲中 HUD 任務追蹤

即時顯示：

香蕉 2 / 5
蘋果 1 / 3

---



# 系統架構
## DifficultyManager

負責：

難度設定
水果資料池
關卡水果生成

主要資料：

DifficultyConfig[]
FruitOption[]

## Spawner

負責：

- 生成水果
- Go / No-Go 分配
- 波次管理

流程：

Difficulty
↓
GenerateLevelFruits()
↓
Target Fruits
↓
Spawn Wave
↓
Spawn Target

## GameManager

負責：

- 分數管理
- 生命值管理
- 通關判定
- Intro UI
- 難度進階

流程：

Start Game
↓
Show Intro Panel
↓
Begin Gameplay
↓
Check Win / Lose


## Target

負責：

水果移動
點擊判定
超出畫面判定



## 🟢 低優先度

### 8. 音效

- 點擊成功
- 點擊失敗
- 通關
- 失敗

---

### 9. 動畫

- 水果爆開效果
- 通關動畫
- Level Up 動畫

---

### 10. 遊戲結果畫面

顯示：

- Score
- Accuracy
- Miss
- False Alarm
- Correct Rejection