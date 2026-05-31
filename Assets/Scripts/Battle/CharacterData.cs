using System.Collections.Generic;
using UnityEngine;

namespace GameKari.Battle
{
    // 敵AIが選ぶ行動候補。
    // Skillには実行する技、Weightには選ばれやすさを入れる。
    // 例: Weight 70 と 30 なら、おおよそ 7:3 の比率で選ばれる。
    [System.Serializable]
    public class EnemyActionSlot
    {
        public SkillData Skill;
        public int Weight = 1;
    }

    [CreateAssetMenu(
        fileName = "CharacterData",
        menuName = "GameKari/Battle/Character Data")]
    public class CharacterData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public int MaxHP = 100;
        public int MaxMP = 4;

        // 行動順計算に使う速さ。
        public int Speed = 10;

        // 顔アイコン用。現在の戦闘盤面Sprite表示では直接使わない。
        public Sprite FaceIcon;

        // 立ち絵用。現在の戦闘盤面Sprite表示では直接使わない。
        public Sprite StandingSprite;

        // 戦闘盤面のマスに表示する1枚絵。
        // これが未設定の場合、盤面にはキャラ名テキストを表示する。
        public Sprite BattleSprite;

        // BattleSpriteの表示倍率。1が標準。
        // 小さい敵を大きく見せたい場合などにInspectorで調整する。
        public float BattleSpriteScale = 1f;

        // BattleSpriteの表示位置補正。
        // Xで左右、Yで上下にずらす。
        public Vector2 BattleSpriteOffset;

        // 味方キャラがコマンドとして持つ技一覧。
        // 敵は原則ここを空にして、EnemyActionSlotsを使う。
        public bool OverrideFloatingHPBarOffset;
        public Vector2 FloatingHPBarOffset = new Vector2(0f, 52f);

        public bool OverrideDamagePopupOffset;
        public Vector2 DamagePopupOffset = new Vector2(0f, 64f);

        public List<SkillData> DefaultSkills = new();

        // 敵AI専用の行動候補一覧。
        // Skill + Weight によって敵行動を重み付きで選ぶ。
        public List<EnemyActionSlot> EnemyActionSlots = new();
    }
}







