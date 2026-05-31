$ErrorActionPreference = 'Stop'

$managerPath = 'Assets/Scripts/Battle/BattleUIManager.cs'
$shaderPath = 'Assets/Shaders/UIAlphaSilhouette.shader'

if (!(Test-Path $managerPath)) { throw "Required file not found: $managerPath" }
if (!(Test-Path $shaderPath)) { throw "Required file not found: $shaderPath" }

$shader = Get-Content -Path $shaderPath -Raw -Encoding UTF8

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

# Add outline properties.
$shader = ReplaceOptional $shader @'
        _Color ("Tint", Color) = (0.55, 0.68, 0.72, 1)
'@ @'
        _Color ("Tint", Color) = (0.5, 0.5, 0.5, 1)
        _OutlineColor ("Outline Color", Color) = (0.25, 0.25, 0.25, 1)
        _OutlineSize ("Outline Size", Float) = 1
'@ 'shader outline properties'

# Add texel size and outline uniforms.
$shader = ReplaceOptional $shader @'
            sampler2D _MainTex;
            fixed4 _Color;
            float4 _MainTex_ST;
            float4 _ClipRect;
'@ @'
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineSize;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _ClipRect;
'@ 'shader outline uniforms'

# Replace fragment with outline-aware version.
$shader = ReplaceOptional $shader @'
            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.texcoord);
                fixed4 color = IN.color;
                color.a *= tex.a;
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                return color;
            }
'@ @'
            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.texcoord);
                float alpha = tex.a;

                float2 offset = _MainTex_TexelSize.xy * max(0.0, _OutlineSize);
                float outlineAlpha = alpha;
                outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x, 0)).a);
                outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x, 0)).a);
                outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2(0,  offset.y)).a);
                outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2(0, -offset.y)).a);
                outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x,  offset.y)).a);
                outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x,  offset.y)).a);
                outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x, -offset.y)).a);
                outlineAlpha = max(outlineAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x, -offset.y)).a);

                fixed4 fillColor = IN.color;
                fixed4 outlineColor = _OutlineColor;
                outlineColor.a *= IN.color.a;

                fixed4 color = alpha > 0.001 ? fillColor : outlineColor;
                color.a *= alpha > 0.001 ? alpha : outlineAlpha;
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                return color;
            }
'@ 'shader outline fragment'

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

$settings = @'
        [SerializeField] private Color skillHoverSilhouetteOutlineColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] private float skillHoverSilhouetteOutlineSize = 1f;

'@
$text = InsertBeforeIfMissing $text 'skillHoverSilhouetteOutlineColor' '        [SerializeField] private float skillHoverSilhouetteOverlapAlpha' $settings 'skill hover silhouette outline settings'

$text = ReplaceOptional $text @'
            _skillHoverSilhouetteMaterial = new Material(shader)
            {
                name = "SkillHoverSilhouetteMaterial_Runtime"
            };
            return _skillHoverSilhouetteMaterial;
'@ @'
            _skillHoverSilhouetteMaterial = new Material(shader)
            {
                name = "SkillHoverSilhouetteMaterial_Runtime"
            };
            ApplySkillHoverSilhouetteMaterialSettings(_skillHoverSilhouetteMaterial);
            return _skillHoverSilhouetteMaterial;
'@ 'apply outline settings when creating material'

$text = ReplaceOptional $text @'
            Material material = GetSkillHoverSilhouetteMaterial();
            if (material != null)
            {
                image.material = material;
            }
'@ @'
            Material material = GetSkillHoverSilhouetteMaterial();
            if (material != null)
            {
                ApplySkillHoverSilhouetteMaterialSettings(material);
                image.material = material;
            }
'@ 'refresh outline settings when applying silhouette material'

$helper = @'
        private void ApplySkillHoverSilhouetteMaterialSettings(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_OutlineColor"))
            {
                material.SetColor("_OutlineColor", skillHoverSilhouetteOutlineColor);
            }

            if (material.HasProperty("_OutlineSize"))
            {
                material.SetFloat("_OutlineSize", Mathf.Max(0f, skillHoverSilhouetteOutlineSize));
            }
        }

'@
$text = InsertBeforeIfMissing $text 'private void ApplySkillHoverSilhouetteMaterialSettings(Material material)' '        private Material GetSkillHoverSilhouetteMaterial()' $helper 'ApplySkillHoverSilhouetteMaterialSettings helper'

Set-Content -Path $managerPath -Value $text -Encoding UTF8
Write-Host 'Patched skill hover silhouette shader and manager outline settings.'
