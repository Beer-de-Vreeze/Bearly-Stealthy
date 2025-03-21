Shader "Custom/ASCIIObjectShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ASCIITex ("ASCII Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _MainTex;
        sampler2D _ASCIITex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal;
            float3 viewDir;
        };

        float GetGrayScale(float3 color)
        {
            return dot(color, float3(0.299, 0.587, 0.114)); // Standard grayscale formula
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float3 color = tex2D(_MainTex, IN.uv_MainTex).rgb;
            float gray = GetGrayScale(color); // Convert object color to grayscale

            // Map grayscale value to ASCII texture coordinates
            float asciiIndex = gray * 15; // Assuming 16 ASCII brightness levels
            float2 asciiUV = float2(asciiIndex / 16.0, 0.5); // Sample ASCII texture row

            fixed4 asciiChar = tex2D(_ASCIITex, asciiUV); // Get ASCII character from texture
            o.Albedo = asciiChar.rgb; // Apply ASCII effect
        }
        ENDCG
    }
}
