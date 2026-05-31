$ErrorActionPreference = 'Stop'

$shaderPath = 'Assets/Shaders/UIAlphaSilhouette.shader'
if (!(Test-Path $shaderPath)) { throw "Required file not found: $shaderPath" }

$shader = Get-Content -Path $shaderPath -Raw -Encoding UTF8

function ReplaceRequired($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        throw "Patch anchor not found: $label"
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

$oldFrag = @'
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
'@

$newFrag = @'
            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.texcoord);
                float alpha = tex.a;

                float2 offset = _MainTex_TexelSize.xy * max(0.0, _OutlineSize);
                float neighborAlpha = 0.0;
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x, 0)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x, 0)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2(0,  offset.y)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2(0, -offset.y)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x,  offset.y)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x,  offset.y)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x, -offset.y)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x, -offset.y)).a);

                float outlineOnlyAlpha = saturate(neighborAlpha - alpha);
                float visibleAlpha = max(alpha, outlineOnlyAlpha);

                fixed4 fillColor = IN.color;
                fixed4 outlineColor = _OutlineColor;
                outlineColor.a *= IN.color.a;

                fixed4 color = alpha > 0.001 ? fillColor : outlineColor;
                color.a *= visibleAlpha;
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                return color;
            }
'@

$shader = ReplaceRequired $shader $oldFrag $newFrag 'fix outline-only fragment shader'

Set-Content -Path $shaderPath -Value $shader -Encoding UTF8
Write-Host 'Fixed silhouette shader so outline color is only used outside the sprite body.'
