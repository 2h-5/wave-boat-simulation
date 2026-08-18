Shader "Custom/GerstnerWater"
{
    Properties
    {
        // Set up the water colours.
        _ShallowColor ("Shallow Color", Color) = (0.2, 0.6, 0.8, 0.8)
        _DeepColor ("Deep Color", Color) = (0.05, 0.2, 0.4, 0.95)
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        
        // Set up the lightings.
        _Ambient ("Ambient Intensity", Range(0, 1)) = 0.15
        _Diffuse ("Diffuse Intensity", Range(0, 1)) = 0.6
        _Specular ("Specular Intensity", Range(0, 2)) = 1.2
        _Shininess ("Shininess", Range(1, 256)) = 64
        _FresnelPower ("Fresnel Power", Range(0.5, 5)) = 2.5
        _FresnelBias ("Fresnel Bias", Range(0, 1)) = 0.1
        
        // Set up the texture details.
        _DetailTex ("Detail Texture (R=height)", 2D) = "gray" {}
        _DetailTiling ("Detail Tiling", Vector) = (6, 6, 0, 0)
        _DetailScroll ("Detail Scroll Speed", Vector) = (0.05, 0.03, 0, 0)
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.08
        
        // Normalize for surface variation.
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalTiling ("Normal Tiling", Vector) = (4, 4, 0, 0)
        _NormalScroll ("Normal Scroll Speed", Vector) = (0.02, 0.015, 0, 0)
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.5
        
        _FoamThreshold ("Foam Threshold (height)", Range(0, 2)) = 0.5
        _FoamColor ("Foam Color", Color) = (0.9, 0.95, 1, 1)
        _FoamIntensity ("Foam Intensity", Range(0, 1)) = 0.4
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
     
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Back
        
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            
            // Set the max number of waves.
            #define MAX_WAVES 8
            
            // Set the initial parameters of waves.
            float4 _WaveDirs[MAX_WAVES];
            float _WaveAmps[MAX_WAVES];
            float _WaveLens[MAX_WAVES];
            float _WaveSpeeds[MAX_WAVES];
            float _WaveSteep[MAX_WAVES];
            float _WavePhases[MAX_WAVES];
            int _WaveCount;
            float _WaterTime;
            
            // Set the initial parameter of colours.
            float4 _ShallowColor;
            float4 _DeepColor;
            float4 _SpecularColor;
            
            // Set the initial parameter of lightings
            float _Ambient;
            float _Diffuse;
            float _Specular;
            float _Shininess;
            float _FresnelPower;
            float _FresnelBias;
            
            // Set the initial parameter of textures.
            sampler2D _DetailTex;
            float2 _DetailTiling;
            float2 _DetailScroll;
            float _DetailStrength;
            
            // Set the initial parameter of map normalization.
            sampler2D _NormalMap;
            float2 _NormalTiling;
            float2 _NormalScroll;
            float _NormalStrength;
            
            // Set the initial parameter of foam.
            float _FoamThreshold;
            float4 _FoamColor;
            float _FoamIntensity;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float heightOffset : TEXCOORD3;
            };
            
            float3 GerstnerWave(float2 dir, float amplitude, float wavelength, float speed, 
                               float steepness, float phase, float2 worldXZ, float time, 
                               inout float3 tangent, inout float3 binormal)
            {
                // Calculate the wave number.
                float k = 6.28318530718 / wavelength;
                
                // Calculate the angular frequency.
                float omega = sqrt(9.8 * k);
                float f = k * dot(dir, worldXZ) + speed * omega * time + phase;
                
                // Calculate the steepness.
                float Q = steepness;
                
                // Calculate the displacement.
                float cosF = cos(f);
                float sinF = sin(f);
                
                float3 displacement;
                displacement.x = Q * amplitude * dir.x * cosF;
                displacement.y = amplitude * sinF;
                displacement.z = Q * amplitude * dir.y * cosF;
                
                // Calculate the derivatives of the Gerstner wave.
                float WA = omega * amplitude;
                float S = sinF;
                float C = cosF;
                
                tangent += float3(
                    -Q * dir.x * dir.x * WA * S,
                    dir.x * WA * C,
                    -Q * dir.x * dir.y * WA * S
                );
                
                binormal += float3(
                    -Q * dir.x * dir.y * WA * S,
                    dir.y * WA * C,
                    -Q * dir.y * dir.y * WA * S
                );
                
                return displacement;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                
                // Get the world position.
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float2 worldXZ = worldPos.xz;
                
                // Set up the initial displacement and frame.
                float3 totalDisplacement = float3(0, 0, 0);
                float3 tangent = float3(1, 0, 0);
                float3 binormal = float3(0, 0, 1);
                
                // Sum up all the Gerstner waves.
                for (int i = 0; i < _WaveCount && i < MAX_WAVES; i++)
                {
                    float2 dir = normalize(_WaveDirs[i].xy);
                    float amplitude = _WaveAmps[i];
                    float wavelength = _WaveLens[i];
                    float speed = _WaveSpeeds[i];
                    float steepness = _WaveSteep[i];
                    float phase = _WavePhases[i];
                    
                    // Skip for exception.
                    if (amplitude <= 0 || wavelength <= 0) continue;
                    
                    totalDisplacement += GerstnerWave(dir, amplitude, wavelength, speed, 
                                                      steepness, phase, worldXZ, _WaterTime,
                                                      tangent, binormal);
                }
                
                // Apply the texture detail.
                float2 detailUV = worldXZ * _DetailTiling + _DetailScroll * _WaterTime;
                float detailHeight = tex2Dlod(_DetailTex, float4(detailUV, 0, 0)).r;
                totalDisplacement.y += (detailHeight - 0.5) * 2.0 * _DetailStrength;
                
                // Apply displacement.
                worldPos += totalDisplacement;
                
                // Calculate the normal.
                float3 normal = normalize(cross(binormal, tangent));
                if (normal.y < 0) normal = -normal;
                
                // Store the outputs.
                o.worldPos = worldPos;
                o.worldNormal = normal;
                o.uv = v.uv;
                o.heightOffset = totalDisplacement.y;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                // Sample the normal map.
                float2 normalUV = i.worldPos.xz * _NormalTiling + _NormalScroll * _WaterTime;
                float3 normalMapSample = UnpackNormal(tex2D(_NormalMap, normalUV));
                float3 normal = i.worldNormal;
                normal.xz += normalMapSample.xy * _NormalStrength;
                normal = normalize(normal);
                
                // Calculate the direction.
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                
                // Fresnel term.
                float fresnel = _FresnelBias + (1.0 - _FresnelBias) * pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);
                
                // Ambient term.
                float3 ambient = _Ambient * _ShallowColor.rgb;
                
                // Diffuse term.
                float NdotL = max(0, dot(normal, lightDir));
                float3 diffuse = _Diffuse * NdotL * lerp(_DeepColor.rgb, _ShallowColor.rgb, 0.5);
                
                // Specular term.
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = max(0, dot(normal, halfDir));
                float specularTerm = pow(NdotH, _Shininess);
                float3 specular = _Specular * specularTerm * _SpecularColor.rgb * _LightColor0.rgb;
                
                // Blend the water colour based on the three colours we defined.
                float3 waterColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, fresnel);
                
                // Combine the lighting for final colour calculation.
                float3 finalColor = ambient + diffuse * waterColor + specular;
                
                // Add the foam.
                float foamFactor = smoothstep(_FoamThreshold * 0.7, _FoamThreshold, i.heightOffset);
                finalColor = lerp(finalColor, _FoamColor.rgb, foamFactor * _FoamIntensity);
               
                float alpha = lerp(_DeepColor.a, _ShallowColor.a, fresnel);
                
                return float4(finalColor, alpha);
            }
            
            ENDCG
        }
    }
    
    FallBack "Transparent/Diffuse"
}