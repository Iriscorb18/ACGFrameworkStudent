#include "material.h"
#include "application.h"

#include <istream>
#include <fstream>
#include <algorithm>


FlatMaterial::FlatMaterial(glm::vec4 color)
{
	this->color = color;
	this->shader = Shader::Get("res/shaders/basic.vs", "res/shaders/flat.fs");
}

FlatMaterial::~FlatMaterial() { }

void FlatMaterial::setUniforms(Camera* camera, glm::mat4 model)
{
	//upload node uniforms
	this->shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	this->shader->setUniform("u_camera_position", camera->eye);
	this->shader->setUniform("u_model", model);

	this->shader->setUniform("u_color", this->color);
}

void FlatMaterial::render(Mesh* mesh, glm::mat4 model, Camera* camera)
{
	if (mesh && this->shader) {
		// enable shader
		this->shader->enable();

		// upload uniforms
		setUniforms(camera, model);

		// do the draw call
		mesh->render(GL_TRIANGLES);

		this->shader->disable();
	}
}

void FlatMaterial::renderInMenu()
{
	ImGui::ColorEdit3("Color", (float*)&this->color);
}

WireframeMaterial::WireframeMaterial()
{
	this->color = glm::vec4(1.f);
	this->shader = Shader::Get("res/shaders/basic.vs", "res/shaders/flat.fs");
}

WireframeMaterial::~WireframeMaterial() { }

void WireframeMaterial::render(Mesh* mesh, glm::mat4 model, Camera* camera)
{
	if (this->shader && mesh)
	{
		glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
		glDisable(GL_CULL_FACE);

		//enable shader
		this->shader->enable();

		//upload material specific uniforms
		setUniforms(camera, model);

		//do the draw call
		mesh->render(GL_TRIANGLES);

		glEnable(GL_CULL_FACE);
		glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
	}
}

StandardMaterial::StandardMaterial(glm::vec4 color)
{
	this->color = color;
	this->base_shader = Shader::Get("res/shaders/basic.vs", "res/shaders/basic.fs");
	this->normal_shader = Shader::Get("res/shaders/basic.vs", "res/shaders/normal.fs");
	this->shader = this->base_shader;
}

StandardMaterial::~StandardMaterial() { }

void StandardMaterial::setUniforms(Camera* camera, glm::mat4 model)
{
	//upload node uniforms
	this->shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	this->shader->setUniform("u_camera_position", camera->eye);
	this->shader->setUniform("u_model", model);

	this->shader->setUniform("u_color", this->color);

	if (this->texture) {
		this->shader->setUniform("u_texture", this->texture);
	}
}

void StandardMaterial::render(Mesh* mesh, glm::mat4 model, Camera* camera)
{
	bool first_pass = true;
	if (mesh && this->shader)
	{
		// enable shader
		this->shader->enable();

		// Multi pass render
		int num_lights = Application::instance->light_list.size();
		for (int nlight = -1; nlight < num_lights; nlight++)
		{
			if (nlight == -1) { nlight++; } // hotfix

			// upload uniforms
			setUniforms(camera, model);

			// upload light uniforms
			if (!first_pass) {
				glBlendFunc(GL_SRC_ALPHA, GL_ONE);
				glDepthFunc(GL_LEQUAL);
			}
			this->shader->setUniform("u_ambient_light", Application::instance->ambient_light * (float)first_pass);

			if (num_lights > 0) {
				Light* light = Application::instance->light_list[nlight];
				light->setUniforms(this->shader, model);
			}
			else {
				// Set some uniforms in case there is no light
				this->shader->setUniform("u_light_intensity", 1.f);
				this->shader->setUniform("u_light_shininess", 1.f);
				this->shader->setUniform("u_light_color", glm::vec4(0.f));
			}

			// do the draw call
			mesh->render(GL_TRIANGLES);

			first_pass = false;
		}

		// disable shader
		this->shader->disable();
	}
}

void StandardMaterial::renderInMenu()
{
	if (ImGui::Checkbox("Show Normals", &this->show_normals)) {
		if (this->show_normals) {
			this->shader = this->normal_shader;
		}
		else {
			this->shader = this->base_shader;
		}
	}

	if (!this->show_normals) ImGui::ColorEdit3("Color", (float*)&this->color);
}

// VolumeMaterial implementation
VolumeMaterial::VolumeMaterial(glm::vec4 color)
{
	this->color = color;
	this->shaderType = ABSORPTION;
	this->volumeType = HOMOGENEOUS;
	this->shader = Shader::Get("res/shaders/basic.vs", "res/shaders/absorption.fs");

	// Default values for material properties
	this->absorptionCoefficient = 0.5f;
	this->stepLength = 0.001f;
	this->noiseScale = 2.0f;
	this->noiseDetail = 0;
	this->emissiveColor = glm::vec4(0.1f, 0.1f, 0.9f, 1.f);
	this->emissiveIntensity = 0.02f;
}

VolumeMaterial::~VolumeMaterial() { }

void VolumeMaterial::setUniforms(Camera* camera, glm::mat4 model)
{
	glm::mat4 inverseModel = glm::inverse(model);
	glm::vec4 temp = glm::vec4(camera->eye, 1.0);
	glm::vec3 local_camera_pos = glm::vec3((inverseModel * temp) / temp.w);

	this->shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	this->shader->setUniform("u_camera_position", camera->eye);
	this->shader->setUniform("u_localcamera_position", local_camera_pos);
	this->shader->setUniform("u_model", model);

	this->shader->setUniform("u_box_min", this->boxMin);
	this->shader->setUniform("u_box_max", this->boxMax);

	this->shader->setUniform("u_background", Application::instance->ambient_light);
	this->shader->setUniform("u_absorption_coefficient", this->absorptionCoefficient);

	this->shader->setUniform("u_volume_type", this->volumeType);	

	this->shader->setUniform("u_step_length", this->stepLength);
	this->shader->setUniform("u_noise_scale", this->noiseScale);
	this->shader->setUniform("u_noise_detail", this->noiseDetail);

	if (shaderType == ABSORPTION_EMISSION) {
		this->shader->setUniform("u_emission_color", this->emissiveColor);
		this->shader->setUniform("u_emission_intensity", this->emissiveIntensity);
	}
}

void VolumeMaterial::render(Mesh* mesh, glm::mat4 model, Camera* camera)
{
	if (!mesh || !this->shader) return;

	this->shader->enable();

	this->boxMin = mesh->aabb_min;
	this->boxMax = mesh->aabb_max;

	setUniforms(camera, model);
	mesh->render(GL_TRIANGLES);
	this->shader->disable();
}

void VolumeMaterial::renderInMenu()
{
	// Update shader based on the selected shader type
	if (ImGui::Combo("Shader Type", (int*)&shaderType, "Absorption\0Absorption-Emission\0")) {
		if (shaderType == ABSORPTION) {
			this->shader = Shader::Get("res/shaders/basic.vs", "res/shaders/absorption.fs");
		}
		else if (shaderType == ABSORPTION_EMISSION) {
			this->shader = Shader::Get("res/shaders/basic.vs", "res/shaders/absorption_emission.fs");
			// automatic changes to make better the visualization
			this->volumeType = HETEROGENEOUS; // as the homogeneous doesn't have emission light
			Application::instance->ambient_light = glm::vec4(0.1f);
		}
	}

	// Volume Type
	ImGui::Combo("Volume Type", (int*)&volumeType, "Homogeneous\0Heterogeneous\0");

	// Common parameters
	ImGui::SliderFloat("Absorption Coefficient", &this->absorptionCoefficient, 0.0f, 2.0f);

	if (volumeType == HETEROGENEOUS) {
		ImGui::SliderFloat("Step Length", &this->stepLength, 0.001f, 0.1f);
		ImGui::SliderFloat("Noise Scale", &this->noiseScale, 1.0f, 5.0f);
		ImGui::SliderInt("Noise Detail", &this->noiseDetail, 0, 5);
	}

	// Only show emissive parameters for Absorption-Emission Shader
	if (shaderType == ABSORPTION_EMISSION && volumeType == HETEROGENEOUS) {
		ImGui::ColorEdit3("Emission Color", (float*)&this->emissiveColor);
		ImGui::SliderFloat("Emission Intensity", &this->emissiveIntensity, 0.0f, 1.0f);
	}
}