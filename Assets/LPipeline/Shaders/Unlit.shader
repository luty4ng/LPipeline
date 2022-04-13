Shader "LPipeline/Unlit" {
    Properties
    {

    }
    
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            
            #pragma multi_compile_instancing 
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #include "UnlitPass.hlsl"

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}