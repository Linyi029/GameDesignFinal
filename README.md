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
- 通關動畫
- Level Up 動畫

---

### 遊戲結果畫面

顯示：Score/ Stars
