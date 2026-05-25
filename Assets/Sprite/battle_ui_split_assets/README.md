# Battle UI split assets

このフォルダは、添付された「ボタン枠デザイン案」をUnityで使いやすいように分解したPNG素材です。
全素材は文字なし・透明背景です。テキストはUnity側のTextMeshProで重ねる前提です。

## 推奨インポート設定
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Mesh Type: Full Rect
- Filter Mode: Point または Bilinear（UIの線を柔らかくしたいならBilinear）
- Compression: None
- Pixels Per Unit: 100
- Generate Mip Maps: Off

## 使い方の目安
- main_button_*: Fight / Swap / Item などメインコマンド用
- main_button_tail_*: 小しっぽ付きのSwap/Item/説明あり行動用
- skill_button_*: スキル一覧用。色ライン付きは属性区別用
- command_panel_frame: コマンドパネル全体の吹き出し枠
- ally_status_panel_*: 味方ステータス枠
- enemy_hp_panel_*: 敵名・HP表示枠
- turn_order_bar: ターン順バー
- hp/mp/enemy_hp bar: ゲージ表示用の部品

## Unityでの注意
枠を自由に伸縮したい場合は、Sprite EditorでBorderを設定し、Image TypeをSlicedにしてください。
ただし斜めカットやしっぽ部分は9-sliceで歪みやすいので、ボタンは固定サイズ運用の方が安全です。
