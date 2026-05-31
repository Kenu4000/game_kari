# roadmap

このフォルダは、ゲーム実装ロードマップを置く場所です。

## 親子関係

ロードマップは次の親子関係で管理します。

```text
roadmap
└── phase
    └── task
```

## 用語

### roadmap

ゲーム全体、または大きな機能単位の実装計画です。

例:

- Battle MVP
- Item System
- Quest Route
- UI Polish

### phase

`roadmap` 内の作業をまとまりごとに分割した単位です。

例:

- 戦闘システム
- アイテムシステム
- ルート進行
- 戦闘UI
- 演出

### task

`phase` を実装可能な粒度まで分割した作業単位です。

例:

- HPバー減少アニメーションを追加する
- SwapListをPrefab化する
- ActionOverlayに技名表示待機を追加する
- StatusPanelにFaceIconを表示する

## guidelinesとの関係

`guidelines` が安定すると、roadmap単位で丸ごと、一定品質の実装が納品される状態を目指します。

そのため、roadmapで発生した反省・再利用可能な手順・レビュー観点は、必要に応じて `docs/guidelines/` に昇格させます。

## 置くもの

- 実装ロードマップ
- phase一覧
- task一覧
- 作業順序
- 完了条件
- 保留理由
- 次に着手する作業

## 置かないもの

- ゲーム仕様そのもの: `docs/design/`
- 汎用実装ルール: `docs/guidelines/`
- AIセルフレビュー結果: `docs/self-review/`
