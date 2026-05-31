$ErrorActionPreference = 'Stop'

$managerPath = 'Assets/Scripts/Battle/BattleUIManager.cs'
$shaderPath = 'Assets/Shaders/UIAlphaSilhouette.shader'

if (!(Test-Path $managerPath)) { throw "Required file not found: $managerPath" }
if (!(Test-Path $shaderPath)) { throw "Required file not found: $shaderPath" }

# Restore the silhouette shader to simple alpha-only fill. Outline is handled by duplicate Image layers.
$shader = @'
Shader "GameKari/UIAlphaSilhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Fill Color", Color) = (1, 1, 1, 1)
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

# Remove shader-outline-specific material settings calls if present. Duplicate images handle outline.
$text = ReplaceOptional $text '                ApplySkillHoverSilhouetteMaterialSettings(material);
' '' 'remove outline material refresh call'
$text = ReplaceOptional $text '            ApplySkillHoverSilhouetteMaterialSettings(_skillHoverSilhouetteMaterial);
' '' 'remove outline material creation settings call'

# Ensure setting names still exist and add pixel offset.
$offsetSetting = @'
        [SerializeField] private float skillHoverSilhouetteOutlinePixelOffset = 2f;

'@
$text = InsertBeforeIfMissing $text 'skillHoverSilhouetteOutlinePixelOffset' '        [SerializeField] private Color skillHoverSilhouetteOutlineColor' $offsetSetting 'skill hover outline pixel offset setting'

$oldApply = @'
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
'@
$newApply = @'
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
            SetSkillHoverSilhouetteOutlineVisible(image, true, color.a);
        }
'@
$text = ReplaceOptional $text $oldApply $newApply 'show duplicate-image outline when applying silhouette'

$oldNormal = @'
        private static void ApplyNormalBoardSpriteMaterial(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.material = null;
        }
'@
$newNormal = @'
        private void ApplyNormalBoardSpriteMaterial(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.material = null;
            SetSkillHoverSilhouetteOutlineVisible(image, false, 1f);
        }
'@
$text = ReplaceOptional $text $oldNormal $newNormal 'hide duplicate-image outline when restoring normal material'

$helpers = @'
        private void SetSkillHoverSilhouetteOutlineVisible(Image sourceImage, bool visible, float alpha)
        {
            if (sourceImage == null || sourceImage.transform == null || sourceImage.transform.parent == null)
            {
                return;
            }

            Transform parent = sourceImage.transform.parent;
            float offset = Mathf.Max(0f, skillHoverSilhouetteOutlinePixelOffset);
            Vector2[] offsets = new Vector2[]
            {
                new Vector2(-offset, 0f),
                new Vector2(offset, 0f),
                new Vector2(0f, -offset),
                new Vector2(0f, offset),
                new Vector2(-offset, -offset),
                new Vector2(-offset, offset),
                new Vector2(offset, -offset),
                new Vector2(offset, offset)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                Image outline = GetOrCreateSkillHoverSilhouetteOutlineImage(sourceImage, parent, i);
                if (outline == null)
                {
                    continue;
                }

                outline.gameObject.SetActive(visible && sourceImage.enabled && sourceImage.sprite != null);
                if (!visible)
                {
                    continue;
                }

                CopyRectTransform(sourceImage.rectTransform, outline.rectTransform, offsets[i]);
                outline.sprite = sourceImage.sprite;
                outline.type = sourceImage.type;
                outline.preserveAspect = sourceImage.preserveAspect;
                outline.raycastTarget = false;
                outline.material = GetSkillHoverSilhouetteMaterial();

                Color outlineColor = skillHoverSilhouetteOutlineColor;
                outlineColor.a *= Mathf.Clamp01(alpha);
                outline.color = outlineColor;

                int sourceIndex = sourceImage.transform.GetSiblingIndex();
                outline.transform.SetSiblingIndex(Mathf.Max(0, sourceIndex));
            }

            sourceImage.transform.SetAsLastSibling();
        }

        private Image GetOrCreateSkillHoverSilhouetteOutlineImage(Image sourceImage, Transform parent, int index)
        {
            string name = $"SkillHoverSilhouetteOutline_{index}";
            Transform existing = parent.Find(name);
            Image image = existing == null ? null : existing.GetComponent<Image>();
            if (image != null)
            {
                return image;
            }

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            image = obj.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.gameObject.SetActive(false);
            return image;
        }

        private static void CopyRectTransform(RectTransform source, RectTransform target, Vector2 anchoredOffset)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.sizeDelta = source.sizeDelta;
            target.offsetMin = source.offsetMin;
            target.offsetMax = source.offsetMax;
            target.localScale = source.localScale;
            target.localRotation = source.localRotation;
            target.anchoredPosition = source.anchoredPosition + anchoredOffset;
        }

'@
$text = InsertBeforeIfMissing $text 'private void SetSkillHoverSilhouetteOutlineVisible(Image sourceImage, bool visible, float alpha)' '        private Material GetSkillHoverSilhouetteMaterial()' $helpers 'duplicate-image silhouette outline helpers'

# Remove previous material-outline helper body if it exists by making it harmless; leave it if references remain from local partial state.
$text = [regex]::Replace($text, '(?s)\s*private void ApplySkillHoverSilhouetteMaterialSettings\(Material material\)\s*\{.*?\n        \}\s*\n', "`n")

Set-Content -Path $managerPath -Value $text -Encoding UTF8
Write-Host 'Patched skill hover silhouette outline to use duplicate offset images and restored simple silhouette shader.'
