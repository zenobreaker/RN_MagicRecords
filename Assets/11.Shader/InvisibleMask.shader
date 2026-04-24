Shader "Custom/InvisibleMask"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ColorMask 0 // 화면에 아무 색도 칠하지 않음! (투명 마스크)
            ZWrite Off  // 깊이도 건드리지 않음!
        }
    }
}