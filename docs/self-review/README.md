# self-review

このフォルダは、AIセルフレビューの結果を置く場所です。

## 目的

Claude Code と Codex にそれぞれセルフレビューをさせ、指摘内容を比較・統合し、修正を繰り返すための記録場所です。

モデルごとに指摘の傾向が異なるため、片方だけでは拾いにくい問題を補完することを目的とします。

## 基本フロー

```text
実装
↓
Claude Code にセルフレビューさせる
↓
Codex にセルフレビューさせる
↓
レビュー内容を比較する
↓
妥当な指摘をマージする
↓
修正する
↓
必要なら再レビューする
```

## 置くもの

- Claude Code のレビュー結果
- Codex のレビュー結果
- レビュー指摘の統合メモ
- 修正方針
- 再レビュー結果
- 採用しなかった指摘と理由

## 置かないもの

- ゲーム仕様: `docs/design/`
- 汎用レビュー手順やプロンプト: `docs/guidelines/`
- 実装ロードマップ: `docs/roadmap/`

## 命名例

```text
YYYY-MM-DD_<target>_claude.md
YYYY-MM-DD_<target>_codex.md
YYYY-MM-DD_<target>_merged.md
```

例:

```text
2026-05-31_battle_ui_claude.md
2026-05-31_battle_ui_codex.md
2026-05-31_battle_ui_merged.md
```
