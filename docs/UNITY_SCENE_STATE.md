# Unity Scene State Notes

このファイルは、ChatGPT側から直接確認できないUnity Editor上のScene / Hierarchy / Inspector状態を記録するためのメモです。
ユーザーの報告・スクリーンショットをもとに、必要に応じて適宜更新する。

## 運用方針

- Unity Editor上の状態は、GitHub上のコードだけでは確認できない。
- Scene / Hierarchy / Inspector / Prefab化状況 / SerializeField参照状況は、このファイルに記録する。
- ユーザーからスクリーンショットや状況報告があった場合、必要に応じてこのmdへ追記する。
- 既存の戦闘UIはすでにScene上で組まれており、Sprite紐づけも行われているため、原則として再生成しない。
- `Tools > Create Battle UI` は既存UIを壊す可能性があるため、現在の正規UIには使わない。
- `BattleUICreator` は初期雛形生成用として扱い、既存Sceneの戦闘UI調整には基本使用しない。

## 現在のScene

確認日: 2026-05-24

Scene名:

```text
BattleTest
```

## 以前のHierarchy概略

2026-05-24時点のスクリーンショットでは、戦闘UI要素はCanvas直下に並んでいた。

```text
BattleTest
├── Main Camera
├── Directional Light
├── Canvas
│   ├── BattleBackground
│   ├── TruckRoot
│   ├── BossNamePlate
│   ├── TopActionPanel
│   ├── CommandPanel
│   ├── EnemyGridPanel
│   ├── AllyGridPanel
│   ├── EnemyStatusPanel
│   ├── AllyStatusPanel
│   └── RotateButton
├── EventSystem
└── BattleUIManager
```

## 現在のHierarchy状態

更新日: 2026-05-24

- ユーザー報告により、Canvas配下の既存戦闘UI部品は親Objectにまとめ済み。
- ユーザー報告により、まとめた後も問題なし。
- ルートObject名は未記録。想定名は `BattleUIRoot`。
- `BattleUIManager` はScene直下に残す方針。
- `BattleUIManager` を戦闘UI親ObjectのPrefabには含めない方針。

想定構造:

```text
BattleTest
├── Main Camera
├── Directional Light
├── Canvas
│   └── BattleUIRoot または既存UI親Object
│       ├── BattleBackground
│       ├── TruckRoot
│       ├── BossNamePlate
│       ├── TopActionPanel
│       ├── CommandPanel
│       ├── EnemyGridPanel
│       ├── AllyGridPanel
│       ├── EnemyStatusPanel
│       ├── AllyStatusPanel
│       └── RotateButton
├── EventSystem
└── BattleUIManager
```

## BattleUIManagerの状態

- `BattleUIManager` はCanvas配下ではなく、Scene直下に存在している。
- 既存のSerializeField参照はかなり埋まっている。
- `CommandPanel`, `RotateButton`, 各Gridラベル, `EnemyStatusPanel`, `AllyStatusPanel` などは既に紐づいている。
- `Ui References` は現時点では `None` でも問題ない。
- 既存の個別SerializeField参照を正として扱う。
- 戦闘UI部品を親Objectにまとめた後も問題なしと報告済み。

## 現在の戦闘UI方針

- 既存の戦闘画面UIを正規UIとして扱う。
- 背景、キャラSprite、敵Sprite、HP/MPバー、CommandPanel、Grid、StatusPanelの既存紐づけは壊さない。
- 戦闘UIを作り直さない。
- `BattleUIReferences` は将来的に参照をまとめるための補助として使うが、現時点で必須ではない。

## Prefab化方針

推奨:

```text
Scene
├── BattleUIManager   // Scene直下に残す
└── Canvas
    └── BattleUIRoot  // 戦闘UI一式。Prefab化候補
```

Prefab化する場合は、いきなり作り直すのではなく、既存UIを親ObjectにまとめてからPrefab化する。

候補構造:

```text
Canvas
└── BattleUIRoot
    ├── BattleBackground
    ├── TruckRoot
    ├── BossNamePlate
    ├── TopActionPanel
    ├── CommandPanel
    ├── EnemyGridPanel
    ├── AllyGridPanel
    ├── EnemyStatusPanel
    ├── AllyStatusPanel
    └── RotateButton
```

Prefab保存先候補:

```text
Assets/Prefabs/UI/BattleUIRoot.prefab
```

更新日: 2026-05-24

- ユーザー報告により、`Assets/Prefabs/UI/` は作成済み。
- 空フォルダだけではGitに記録されない可能性がある。
- `BattleUIRoot.prefab` などのPrefab実体を作成した時点で、Git管理対象になる。

注意:

- `BattleUIManager` はPrefabに含めない。
- `BattleUIManager` はScene側に残す。
- `BattleUIRoot` をPrefab化する場合、Scene上の実体は消さずにそのままPrefab Instance化する。
- Prefab化後、`BattleUIManager` のSerializeField参照が外れていないか確認する。

## 現時点でやらないこと

- `Tools > Create Battle UI` の再実行
- 戦闘UIの再生成
- `BattleUIManager` の既存SerializeField削除
- `BattleUIReferences` への強制移行
- Canvas全体のPrefab化
- `BattleUIManager` をPrefabに含めること

## 今後の更新対象

以下の状態が分かったら追記する。

- 戦闘UI親Objectの正式名
- `BattleUIRoot` をPrefab化したか
- Prefab保存先
- `BattleUIManager` の参照が維持されたか
- `BattleUIReferences` を使うかどうか
- Movement / Preparation / Result UIをどの親Object配下に置くか
- Scene上で手調整したUI変更点
