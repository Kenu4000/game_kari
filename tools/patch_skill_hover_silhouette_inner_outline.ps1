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

                float minNeighborAlpha = 1.0;
                minNeighborAlpha = min(minNeighborAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x, 0)).a);
                minNeighborAlpha = min(minNeighborAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x, 0)).a);
                minNeighborAlpha = min(minNeighborAlpha, tex2D(_MainTex, IN.texcoord + float2(0,  offset.y)).a);
                minNeighborAlpha = min(minNeighborAlpha, tex2D(_MainTex, IN.texcoord + float2(0, -offset.y)).a);
                minNeighborAlpha = min(minNeighborAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x,  offset.y)).a);
                minNeighborAlpha = min(minNeighborAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x,  offset.y)).a);
                minNeighborAlpha = min(minNeighborAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x, -offset.y)).a);
                minNeighborAlpha = min(minNeighborAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x, -offset.y)).a);

                // uGUI Image cannot reliably draw outside its own mesh.
                // Therefore, draw the outline inside the silhouette edge.
                float edge = alpha > 0.001 && minNeighborAlpha < 0.5 ? 1.0 : 0.0;

                fixed4 color = edge > 0.5 ? _OutlineColor : IN.color;
                color.a *= IN.color.a;
                color.a *= alpha;
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                return color;
            }
'@

$pattern = '(?s)            fixed4 frag\(v2f IN\) : SV_Target\s*\{.*?\n            \}\s*ENDCG'
$replacement = $newFrag + '            ENDCG'
$replaced = [regex]::Replace($shader, $pattern, $replacement, 1)

if ($replaced -eq $shader) {
    throw 'Patch anchor not found: frag function in UIAlphaSilhouette.shader'
}

Set-Content -Path $shaderPath -Value $replaced -Encoding UTF8
Write-Host 'Patched UIAlphaSilhouette shader to use visible inner outline.'
