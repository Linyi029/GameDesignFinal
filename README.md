# TODO

## 高優先度

### 修正 Could not pick a fruit for spawn

原因：

- Fruit Pool 數量不足 (把全部的水果prefab填完)

需檢查：

- DifficultyConfig
- GenerateLevelFruits()
- Fruit Pool

---

### 驗證升級關卡後的資料重置

確認：

- requiredHitsByFruit.Clear()
- currentHitsByFruit.Clear()
- roundHits 重置
- health 重置
- accuracy 重置

避免上一關資料殘留。

---

## 中優先度

### UI 美化

依照設計稿拉背景

---

### 升級關卡後重新顯示 Intro

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

### 遊戲中 HUD 任務追蹤

即時顯示：

香蕉 2 / 5
蘋果 1 / 3
生命值

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

---


## Todo - 低優先度

### 音效

- 點擊成功
- 點擊失敗
- 通關
- 失敗

---

### 動畫

- 水果爆開效果
- 通關動畫
- Level Up 動畫

---

### 遊戲結果畫面

顯示：Score/ Stars