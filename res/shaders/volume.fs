#version 450 core

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec3 u_camera_position;

uniform vec4 u_color;
uniform vec3 u_background_color;          // Color behind the volume
uniform float u_absorption_coefficient;   // Controls how much light is absorbed

out vec4 FragColor;

void main()
{
    vec3 N = normalize(v_normal);
    vec3 V = normalize(u_camera_position - v_world_position);

    // Calculate the light absorption effect based on distance and absorption coefficient
    float distance = length(u_camera_position - v_world_position);
    float absorption = exp(-u_absorption_coefficient * distance);

    // Blend the volume color with the background color using the absorption factor
    vec3 finalColor = mix(u_background_color, u_color.rgb, absorption);

    // Apply the color to the final fragment color
    FragColor = vec4(finalColor, u_color.a);
}
