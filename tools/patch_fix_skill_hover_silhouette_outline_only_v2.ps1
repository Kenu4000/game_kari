$ErrorActionPreference = 'Stop'

$shaderPath = 'Assets/Shaders/UIAlphaSilhouette.shader'
if (!(Test-Path $shaderPath)) { throw "Required file not found: $shaderPath" }

$shader = Get-Content -Path $shaderPath -Raw -Encoding UTF8

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

                // Body uses fill color. Only the outside area around body uses outline color.
                float outlineOnlyAlpha = saturate(neighborAlpha - alpha);
                float visibleAlpha = max(alpha, outlineOnlyAlpha);

                fixed4 color = alpha > 0.001 ? IN.color : _OutlineColor;
                color.a *= IN.color.a;
                color.a *= visibleAlpha;
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                return color;
            }
'@

$pattern = '(?s)            fixed4 frag\(v2f IN\) : SV_Target\s*\{.*?\n            \}\s*\n            ENDCG'
$replacement = $newFrag + "            ENDCG"

$replaced = [regex]::Replace($shader, $pattern, $replacement, 1)
if ($replaced -eq $shader) {
    throw 'Patch anchor not found: frag function in UIAlphaSilhouette.shader'
}

Set-Content -Path $shaderPath -Value $replaced -Encoding UTF8
Write-Host 'Fixed silhouette shader: body uses fill color, outside edge uses outline color only.'
