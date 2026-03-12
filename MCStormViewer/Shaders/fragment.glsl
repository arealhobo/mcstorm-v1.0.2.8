#version 330 core

in vec4 vColor;
in vec3 vNormal;
in float vDist;
in vec3 vWorldPos;

uniform bool uFogEnabled;
uniform vec3 uFogColor;
uniform float uFogStart;
uniform float uFogEnd;
uniform vec3 uSunDir;

out vec4 FragColor;

void main()
{
    // Directional sun lighting
    float ambient = 0.4;
    float diffuse = max(dot(vNormal, uSunDir), 0.0) * 0.6;
    float light = ambient + diffuse;

    vec3 color = vColor.rgb * light;
    float alpha = vColor.a;

    // Procedural glass pattern for transparent blocks
    if (alpha < 1.0)
    {
        // Determine the two UV axes from the face normal
        vec3 absN = abs(vNormal);
        vec2 uv;
        if (absN.y > 0.5)
            uv = fract(vWorldPos.xz); // top/bottom face
        else if (absN.x > 0.5)
            uv = fract(vWorldPos.zy); // left/right face
        else
            uv = fract(vWorldPos.xy); // front/back face

        float border = 0.0625; // 1/16

        // Border frame
        bool isBorder = uv.x < border || uv.x > (1.0 - border) ||
                        uv.y < border || uv.y > (1.0 - border);

        // Thin cross streaks near center
        float crossWidth = 0.03;
        bool isCross = (abs(uv.x - 0.5) < crossWidth) || (abs(uv.y - 0.5) < crossWidth);

        if (isBorder)
        {
            alpha = 0.7;
        }
        else if (isCross)
        {
            alpha = 0.3;
            color *= 1.1; // slightly brighter streaks
        }
        else
        {
            alpha = 0.15;
        }
    }

    // Fog
    if (uFogEnabled)
    {
        float fogFactor = clamp((uFogEnd - vDist) / (uFogEnd - uFogStart), 0.0, 1.0);
        color = mix(uFogColor, color, fogFactor);
    }

    FragColor = vec4(color, alpha);
}
