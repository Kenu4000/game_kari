$ErrorActionPreference = 'Stop'

$managerPath = 'Assets/Scripts/Battle/BattleUIManager.cs'
$shaderPath = 'Assets/Shaders/UIAlphaSilhouette.shader'

if (!(Test-Path $managerPath)) { throw "Required file not found: $managerPath" }

$shaderDir = Split-Path $shaderPath -Parent
if (!(Test-Path $shaderDir)) {
    New-Item -ItemType Directory -Path $shaderDir | Out-Null
}

$shader = @'
Shader "GameKari/UIAlphaSilhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.55, 0.68, 0.72, 1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _MainTex_ST;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.texcoord);
                fixed4 color = IN.color;
                color.a *= tex.a;
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                return color;
            }
            ENDCG
        }
    }
}
'@
Set-Content -Path $shaderPath -Value $shader -Encoding UTF8

$text = Get-Content -Path $managerPath -Raw -Encoding UTF8

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }

    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }

    Write-Host "Inserted: $label"
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

$settings = @'
        [SerializeField] private float skillHoverSilhouetteOverlapAlpha = 0.45f;

'@
$text = InsertBeforeIfMissing $text 'skillHoverSilhouetteOverlapAlpha' '        [SerializeField] private float skillHoverOverlapTargetAlpha' $settings 'skill hover silhouette overlap alpha setting'

$field = @'
        private Material _skillHoverSilhouetteMaterial;

'@
$text = InsertBeforeIfMissing $text '_skillHoverSilhouetteMaterial' '        private readonly List<TMP_Text> _activeActionValuePopupLabels = new();' $field 'skill hover silhouette material field'

$oldInactive = @'
            if (unit == null || unit.IsDead || focusedUnits == null || !focusedUnits.Contains(unit))
            {
                image.color = skillHoverInactiveSpriteColor;
                return;
            }

            image.color = Color.white;
'@
$newInactive = @'
            if (unit == null || unit.IsDead || focusedUnits == null || !focusedUnits.Contains(unit))
            {
                ApplySkillHoverSilhouette(image, skillHoverInactiveSpriteColor.a);
                return;
            }

            ApplyNormalBoardSpriteMaterial(image);
            image.color = Color.white;
'@
$text = ReplaceOptional $text $oldInactive $newInactive 'use true silhouette material for inactive hover sprites'

$oldReset = @'
            image.color = unit != null && unit.IsDead
                ? new Color(1f, 1f, 1f, 0.45f)
                : Color.white;
'@
$newReset = @'
            ApplyNormalBoardSpriteMaterial(image);
            image.color = unit != null && unit.IsDead
                ? new Color(1f, 1f, 1f, 0.45f)
                : Color.white;
'@
$text = ReplaceOptional $text $oldReset $newReset 'reset silhouette material on preview clear'

# Replace the earlier target-overlap behavior with silhouette-only overlap transparency.
$text = ReplaceOptional $text '            // Target sprites stay at normal color. No extra overlap alpha/emphasis is applied.' '            ApplySkillHoverSilhouetteOverlapAlpha(focusedUnits);' 'apply overlap alpha only to silhouette sprites'
$text = ReplaceOptional $text '            ApplySkillHoverOverlapAlpha(targetIsAllyBoard, targetPositions);' '            ApplySkillHoverSilhouetteOverlapAlpha(focusedUnits);' 'replace target overlap alpha with silhouette overlap alpha'

$helpers = @'
        private void ApplySkillHoverSilhouette(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            Material material = GetSkillHoverSilhouetteMaterial();
            if (material != null)
            {
                image.material = material;
            }

            Color color = skillHoverInactiveSpriteColor;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        private Material GetSkillHoverSilhouetteMaterial()
        {
            if (_skillHoverSilhouetteMaterial != null)
            {
                return _skillHoverSilhouetteMaterial;
            }

            Shader shader = Shader.Find("GameKari/UIAlphaSilhouette");
            if (shader == null)
            {
                Debug.LogWarning("[Preview] Shader not found: GameKari/UIAlphaSilhouette. Falling back to normal Image tint.");
                return null;
            }

            _skillHoverSilhouetteMaterial = new Material(shader)
            {
                name = "SkillHoverSilhouetteMaterial_Runtime"
            };
            return _skillHoverSilhouetteMaterial;
        }

        private static void ApplyNormalBoardSpriteMaterial(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.material = null;
        }

        private void ApplySkillHoverSilhouetteOverlapAlpha(HashSet<BattleUnit> focusedUnits)
        {
            if (focusedUnits == null || focusedUnits.Count == 0)
            {
                return;
            }

            var focusedRects = new List<RectTransform>();
            AddFocusedSpriteRects(true, focusedUnits, focusedRects);
            AddFocusedSpriteRects(false, focusedUnits, focusedRects);

            if (focusedRects.Count == 0)
            {
                return;
            }

            ApplySilhouetteOverlapAlphaAt(true, GridPos.FrontTop, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(true, GridPos.BackTop, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(true, GridPos.FrontBottom, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(true, GridPos.BackBottom, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.FrontTop, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.BackTop, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.FrontBottom, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.BackBottom, focusedUnits, focusedRects);
        }

        private void AddFocusedSpriteRects(bool isAllyBoard, HashSet<BattleUnit> focusedUnits, List<RectTransform> focusedRects)
        {
            AddFocusedSpriteRectAt(isAllyBoard, GridPos.FrontTop, focusedUnits, focusedRects);
            AddFocusedSpriteRectAt(isAllyBoard, GridPos.BackTop, focusedUnits, focusedRects);
            AddFocusedSpriteRectAt(isAllyBoard, GridPos.FrontBottom, focusedUnits, focusedRects);
            AddFocusedSpriteRectAt(isAllyBoard, GridPos.BackBottom, focusedUnits, focusedRects);
        }

        private void AddFocusedSpriteRectAt(bool isAllyBoard, GridPos position, HashSet<BattleUnit> focusedUnits, List<RectTransform> focusedRects)
        {
            BattleUnit unit = _grid == null ? null : _grid.GetUnit(isAllyBoard, position);
            if (unit == null || unit.IsDead || focusedUnits == null || !focusedUnits.Contains(unit))
            {
                return;
            }

            RectTransform rect = GetBoardSpriteRect(isAllyBoard, position);
            if (rect != null && focusedRects != null && !focusedRects.Contains(rect))
            {
                focusedRects.Add(rect);
            }
        }

        private void ApplySilhouetteOverlapAlphaAt(bool isAllyBoard, GridPos position, HashSet<BattleUnit> focusedUnits, List<RectTransform> focusedRects)
        {
            BattleUnit unit = _grid == null ? null : _grid.GetUnit(isAllyBoard, position);
            if (unit == null || unit.IsDead || focusedUnits == null || focusedUnits.Contains(unit))
            {
                return;
            }

            RectTransform rect = GetBoardSpriteRect(isAllyBoard, position);
            if (rect == null || focusedRects == null)
            {
                return;
            }

            bool overlapsFocused = false;
            for (int i = 0; i < focusedRects.Count; i++)
            {
                RectTransform focusedRect = focusedRects[i];
                if (focusedRect != null && focusedRect != rect && RectTransformsOverlap(rect, focusedRect))
                {
                    overlapsFocused = true;
                    break;
                }
            }

            if (!overlapsFocused)
            {
                return;
            }

            Image image = GetBoardSpriteImage(isAllyBoard, position);
            if (image == null)
            {
                return;
            }

            ApplySkillHoverSilhouette(image, skillHoverSilhouetteOverlapAlpha);
        }

'@
$text = InsertBeforeIfMissing $text 'private void ApplySkillHoverSilhouette(Image image, float alpha)' '        private void ApplySkillHoverOverlapAlpha(bool targetIsAllyBoard, List<GridPos> targetPositions)' $helpers 'true silhouette preview helpers'

Set-Content -Path $managerPath -Value $text -Encoding UTF8
Write-Host 'Patched skill hover preview to use alpha-only silhouette shader.'
