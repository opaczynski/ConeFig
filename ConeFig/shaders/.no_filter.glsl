#version 330 core
out vec4 FragColor;
in vec2 TexCoords;
uniform sampler2D screenTexture;

void main() {
    vec4 tex_color = texture(screenTexture, TexCoords);
    FragColor = vec4(tex_color);
}