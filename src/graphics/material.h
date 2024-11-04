#pragma once

#include <glm/vec3.hpp>
#include <glm/vec4.hpp>
#include <glm/matrix.hpp>

#include "../framework/camera.h"
#include "mesh.h"
#include "texture.h"
#include "shader.h"

class Material {
public:

	Shader* shader = NULL;
	Texture* texture = NULL;
	glm::vec4 color;

	virtual void setUniforms(Camera* camera, glm::mat4 model) = 0;
	virtual void render(Mesh* mesh, glm::mat4 model, Camera* camera) = 0;
	virtual void renderInMenu() = 0;
};

class FlatMaterial : public Material {
public:

	FlatMaterial(glm::vec4 color = glm::vec4(1.f));
	~FlatMaterial();

	void setUniforms(Camera* camera, glm::mat4 model);
	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
	void renderInMenu();
};

class WireframeMaterial : public FlatMaterial {
public:

	WireframeMaterial();
	~WireframeMaterial();

	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
};

class StandardMaterial : public Material {
public:

	bool first_pass = false;

	bool show_normals = false;
	Shader* base_shader = NULL;
	Shader* normal_shader = NULL;

	StandardMaterial(glm::vec4 color = glm::vec4(1.f));
	~StandardMaterial();

	void setUniforms(Camera* camera, glm::mat4 model);
	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
	void renderInMenu();
};
enum VolumetricType {
	HOMOGENEOUS, 
	HETEROGENEOUS
};

// VolumeMaterial class added for volumetric rendering
class VolumeMaterial : public Material {
public:
	VolumeMaterial(glm::vec4 color = glm::vec4(1.f));
	~VolumeMaterial();

	glm::vec4 backgroundColor;
	float absorptionCoefficient;
	glm::vec3 boxMin; 
	glm::vec3 boxMax;  
	float stepLength;
	float noiseScale;
	float noiseDetail;
	int volumeType; // Default HOMOGENEOUS 0
	float emissiveIntensity;

	glm::vec4 emissiveColor;
	void setUniforms(Camera* camera, glm::mat4 model);
	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
	void renderInMenu();
};