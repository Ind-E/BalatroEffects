namespace Skew;

public static class ShaderCode
{
    public const string Code =
        @"
shader_type canvas_item;

uniform float fov : hint_range(1, 179) = 90;
uniform bool cull_back = true;
uniform float y_rot : hint_range(-180, 180) = 0.0;
uniform float x_rot : hint_range(-180, 180) = 0.0;
uniform float inset : hint_range(0, 1) = 0.0;

// 0: None, 1: Foil, 2: Negative, 3: Polychrome, 4: Holographic
uniform int effect_mode = 0;

varying flat vec2 o;
varying vec3 p;

void vertex(){
    float sin_b = sin(y_rot / 180.0 * PI);
    float cos_b = cos(y_rot / 180.0 * PI);
    float sin_c = sin(x_rot / 180.0 * PI);
    float cos_c = cos(x_rot / 180.0 * PI);
    mat3 inv_rot_mat;
    inv_rot_mat[0][0] = cos_b;
    inv_rot_mat[0][1] = 0.0;
    inv_rot_mat[0][2] = -sin_b;
    inv_rot_mat[1][0] = sin_b * sin_c;
    inv_rot_mat[1][1] = cos_c;
    inv_rot_mat[1][2] = cos_b * sin_c;
    inv_rot_mat[2][0] = sin_b * cos_c;
    inv_rot_mat[2][1] = -sin_c;
    inv_rot_mat[2][2] = cos_b * cos_c;
    float t = tan(fov / 360.0 * PI);
    p = inv_rot_mat * vec3((UV - 0.5), 0.5 / t);
    float v = (0.5 / t) + 0.5;
    p.xy *= v * inv_rot_mat[2].z;
    o = v * inv_rot_mat[2].xy;
    VERTEX += (UV - 0.5) / TEXTURE_PIXEL_SIZE * t * (1.0 - inset);
}

void fragment(){
    if (cull_back && p.z <= 0.0) discard;
    vec2 uv = (p.xy / p.z).xy - o;

    // Sample the base texture with the skewed UVs
    vec4 tex = texture(TEXTURE, uv + 0.5);

    // Placeholder logic for effects
    if (effect_mode == 1) {
        // Foil: Brighten and tint slightly silver/blue
        tex.rgb *= vec3(1.1, 1.1, 1.4);
    }
    else if (effect_mode == 2) {
        // Negative: Invert RGB values
        tex.rgb = 1.0 - tex.rgb;
    }
    else if (effect_mode == 3) {
        // Polychrome: Strong neon purple/pink tint
        tex.rgb *= vec3(1.5, 0.5, 1.5);
    }
    else if (effect_mode == 4) {
        // Holographic: High contrast cyan tint with slight transparency
        tex.rgb *= vec3(0.5, 1.5, 1.5);
        tex.a *= 0.8;
    }

    COLOR = tex;

    // Keep the card border clipping math
    COLOR.a *= step(max(abs(uv.x), abs(uv.y)), 0.5);
}";
}
