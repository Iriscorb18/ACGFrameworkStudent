#version 450 core

in vec3 a_vertex;
in vec3 a_normal;
in vec2 a_uv;
in vec4 a_color;

uniform mat4 u_model;
uniform mat4 u_viewprojection;

out vec3 v_position;
out vec3 v_world_position;
out vec3 v_normal;
out vec2 v_uv;
out vec4 v_color;

void main()
{	
	// Calculate the normal in camera space
	v_normal = (u_model * vec4(a_normal, 0.0)).xyz;
	
	// Calculate the vertex in object space
	v_position = a_vertex;
	v_world_position = (u_model * vec4(v_position, 1.0)).xyz;
	
	// Store the color in the varying var to use it from the pixel shader
	v_color = a_color;

	// Store the texture coordinates
	v_uv = a_uv;

	// Calculate the position of the vertex using the matrices
	gl_Position = u_viewprojection * vec4(v_world_position, 1.0);
}
