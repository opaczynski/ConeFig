#version 330 core
out vec4 FragColor;
in vec2 TexCoords;
uniform sampler2D screenTexture;

const vec2  SCREEN_RES     = vec2(1920.0, 1080.0); // Resolution of your screen
const float SCREEN_DIAG    = 34.0;  // The diagonal of your screen in inches
const float VIEW_DISTANCE  = 39.62; // How far away from the monitor are you sitting?
const float VISUAL_ACUITY  = 200.0; // A constant value for this vision defect (e.g., 20/200)
const float PI = 3.14159265359;

const int SAMPLE_COUNT = 32; 

float linearize(float c)
{
    return (c <= 0.04045)
        ? (c / 12.92)
        : pow((c + 0.055) / 1.055, 2.4);
}

float delinearize(float c)
{
    return (c <= 0.0031308)
        ? (c * 12.92)
        : (1.055 * pow(c, 1.0 / 2.4) - 0.055);
}

float rand(vec2 co) {
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

void main()
{
    vec2 texture_res = vec2(textureSize(screenTexture, 0));

    float aspect_ratio = SCREEN_RES.x / SCREEN_RES.y;
    float screen_height_inches = SCREEN_DIAG / sqrt(aspect_ratio * aspect_ratio + 1.0);

    float ppi = SCREEN_RES.y / screen_height_inches;

    float mar_minutes = (VISUAL_ACUITY / 20.0);
    float theta_degrees = mar_minutes / 60.0;
    float theta_radians = theta_degrees * (PI / 180.0);

    float blur_size_inches = 2.0 * VIEW_DISTANCE * tan(theta_radians / 2.0);
    float blur_radius_pixels = blur_size_inches * ppi;

    vec2 base_texel_size = 1.0 / texture_res;

    vec3 linearColorSum = vec3(0.0);
    float alphaSum = 0.0;
    float totalWeight = 0.0;

    float randomAngle = rand(TexCoords) * 2.0 * PI;
    mat2 rotationMatrix = mat2(cos(randomAngle), -sin(randomAngle), sin(randomAngle), cos(randomAngle));

    for (int i = 0; i < SAMPLE_COUNT; i++)
    {
        float theta = float(i) * 2.39996; 
        float radius = sqrt(float(i) / float(SAMPLE_COUNT)) * blur_radius_pixels;

        vec2 offsetPixels = vec2(cos(theta), sin(theta)) * radius;
        offsetPixels = rotationMatrix * offsetPixels; 
        
        vec2 offsetUV = offsetPixels * base_texel_size;
        vec4 texColor = texture(screenTexture, TexCoords + offsetUV);

        vec3 linColor = vec3(linearize(texColor.r), linearize(texColor.g), linearize(texColor.b));
        
        float weight = 1.0 - (radius / (blur_radius_pixels + 0.0001));
        weight = smoothstep(0.0, 1.0, weight);

        linearColorSum += linColor * weight;
        alphaSum += texColor.a * weight;
        totalWeight += weight;
    }

    if (totalWeight > 0.0) {
        linearColorSum /= totalWeight;
        alphaSum /= totalWeight;
    } else {
        vec4 fallback = texture(screenTexture, TexCoords);
        linearColorSum = vec3(linearize(fallback.r), linearize(fallback.g), linearize(fallback.b));
        alphaSum = fallback.a;
    }

    float scotopic_luminance = 
          0.058 * linearColorSum.r
        + 0.677 * linearColorSum.g
        + 0.265 * linearColorSum.b;

    vec3 rgb_sim = vec3(scotopic_luminance);
    rgb_sim = clamp(rgb_sim, 0.0, 1.0);

    FragColor = vec4(
        delinearize(rgb_sim.r),
        delinearize(rgb_sim.g),
        delinearize(rgb_sim.b),
        alphaSum
    );
}