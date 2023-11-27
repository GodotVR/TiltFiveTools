shader_type spatial;
render_mode unshaded, depth_draw_always, blend_add;

uniform vec3 color : source_color = vec3(0.0, 0.0, 1.0);
uniform vec3 forward = vec3(0.0, -0.25, 0.0);
uniform vec3 target = vec3(0.0, -1.0, 1.0);
uniform vec3 target_up = vec3(0.0, 0.0, -0.25);

void vertex() {
	// (ab)use UV.y to ramp from start to finish
	float t = clamp(UV.y * 2.0, 0.0, 1.0);
	float m = 1.0 - t;

	VERTEX.y = 0.0;
	vec3 ov = VERTEX;

	// Rotate to face forward along path
	vec3 tangent = normalize(mix(forward, -target_up, t));
	vec3 bitangent = cross(tangent, vec3(1.0, 0.0, 0.0));
	VERTEX.y = ov.y * tangent.y + ov.z * tangent.z;
	VERTEX.z = ov.y * bitangent.y + ov.z * bitangent.z;

	// offset in world
	float a = m * m * m;
	float b = 3.0 * m * m * t;
	float c = 3.0 * m * t * t;
	float d = t * t * t;

	VERTEX += b * forward + c * (target + target_up) + d * target;
}

void fragment() {
	float a = clamp(UV.y * 2.0, 0.0, 1.0);
	ALBEDO = color;
	ALPHA = a;
}
