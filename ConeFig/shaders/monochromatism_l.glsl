#version 330 core
out vec4 FragColor;
in vec2 TexCoords;
uniform sampler2D screenTexture;


float linearize(float c) {
    if (c <= 0.04045) {
        return c / 12.92;
    } else {
        return pow((c + 0.055) / 1.055, 2.4);
    }
}
float delinearize(float c) {
    if (c <= 0.0031308) {
        return c * 12.92;
    } else {
        return 1.055 * pow(c, 1.0 / 2.4) - 0.055;
    }
}

void main() {
    vec4 tex_color = texture(screenTexture, TexCoords);
    
    vec3 linearColor = vec3(linearize(tex_color.r), linearize(tex_color.g), linearize(tex_color.b));
    mat3 rgb_xyz = mat3(
        0.4124564, 0.2126729, 0.0193339,
        0.3575761, 0.7151522, 0.119192,
        0.1804375, 0.072175,  0.9503041
    );
    vec3 xyz = rgb_xyz * linearColor;
    mat3 xyz_lms = mat3(
        0.177, -0.1,    0.0,
        0.8124, 1.034,  0.0,
        0.0106, 0.066,  1.0
    );
    vec3 lms = xyz_lms * xyz;
    vec3 q_prime = vec3(lms[0]);
    mat3 lms_xyz = mat3(
        3.90401,   0.378132,  0.0,
        -3.070643,  0.669466,  0.0,
        0.166633, -0.047598,  1.0
    );
    vec3 xyz_sim = lms_xyz * q_prime;
    mat3 xyz_rgb = mat3(
        3.2404542, -0.969266,   0.0556434,
        -1.5371385,  1.8760108, -0.2040259,
        -0.4985314,  0.041556,   1.0572252
    );
    vec3 rgb_sim = xyz_rgb * xyz_sim;
    rgb_sim = clamp(rgb_sim, 0.0, 1.0);
    
    vec4 new_color = vec4(delinearize(rgb_sim[0]), delinearize(rgb_sim[1]), delinearize(rgb_sim[2]), tex_color.a);
    FragColor = vec4(new_color);
}