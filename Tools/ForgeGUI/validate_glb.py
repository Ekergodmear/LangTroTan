"""Validate a ForgeGUI GLB in Blender and render review previews (front / rear / side / top / 45-degree).

The GLB is only read; nothing is written next to it. Output in <out_dir>:
    <name>_front.png, <name>_rear.png, <name>_side.png, <name>_top.png, <name>_45.png
    <name>_report.json   mesh/material/texture stats, rig/animation presence, bounds, checks

Usage:
    blender -b --factory-startup -P Tools/ForgeGUI/validate_glb.py -- <source.glb> <out_dir> <name> [front_axis] [asset=<id>] [door=<h>]

front_axis is the side the entrance faces in Blender after glTF import: -Y (default), +Y, -X or +X.
asset=<manifest id> switches the shape checks to that level's approved stage proportions
(asset_manifest.json -> targetSizeAtDoor0_52, three-stage policy) instead of the generic square-tower heuristics.
door=<measured door height in source units> additionally checks the door-normalised (0.52) size against the
target ranges. Measure the door from the front render first, then re-run with door=.
"""
import hashlib
import json
import math
import os
import sys

import bpy
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
src, out_dir, name = args[:3]
front_axis = args[3] if len(args) > 3 and "=" not in args[3] else "-Y"
options = dict(a.split("=", 1) for a in args[3:] if "=" in a)
DOOR = 0.52
MANIFEST = os.path.join(os.path.dirname(os.path.abspath(__file__)), "asset_manifest.json")
target = None
if "asset" in options:
    with open(MANIFEST, encoding="utf-8") as f:
        target = next(a for a in json.load(f)["assets"] if a["id"] == options["asset"])["targetSizeAtDoor0_52"]
out_dir = os.path.abspath(out_dir)
os.makedirs(out_dir, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
scene = bpy.context.scene
objects = list(scene.objects)
meshes = [o for o in objects if o.type == "MESH"]

# ---------------------------------------------------------------- stats and checks
depsgraph = bpy.context.evaluated_depsgraph_get()
tris = 0
for o in meshes:
    mesh = o.evaluated_get(depsgraph).to_mesh()
    mesh.calc_loop_triangles()
    tris += len(mesh.loop_triangles)
    o.evaluated_get(depsgraph).to_mesh_clear()

corners = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
lo = Vector((min(c.x for c in corners), min(c.y for c in corners), min(c.z for c in corners)))
hi = Vector((max(c.x for c in corners), max(c.y for c in corners), max(c.z for c in corners)))
size = hi - lo
materials = sorted({s.material.name for o in meshes for s in o.material_slots if s.material})
images = [{"name": i.name, "size": list(i.size)} for i in bpy.data.images if i.size[0] > 0]
armatures = [o.name for o in objects if o.type == "ARMATURE"]
broken_images = [i.name for i in bpy.data.images if i.size[0] == 0 or not i.has_data]
textured_materials = [m.name for o in meshes for s in o.material_slots if (m := s.material) and m.use_nodes
                      and any(n.type == "TEX_IMAGE" and n.image for n in m.node_tree.nodes)]
degenerate = 0
for o in meshes:
    degenerate += sum(1 for p in o.data.polygons if p.area < 1e-10)
# Plan axes after import: front_axis along Y means frontage is X.
frontage, depth = (size.x, size.y) if front_axis in ("-Y", "+Y") else (size.y, size.x)

checks = {
    "has_geometry": tris > 0,
    "no_rig": not armatures,                        # rigging is disabled for buildings
    "no_animation": len(bpy.data.actions) == 0,
    "has_textures": len(images) > 0,
    "triangles_under_60k": tris <= 60000,           # mobile-friendly budget for one building
    "textures_loaded": not broken_images,          # every image decodes with pixel data
    "materials_textured": bool(textured_materials),
    "degenerate_faces_under_1pct": degenerate <= 0.01 * max(1, tris),
}
stage = None
if target:
    # Scale-free proportions from the approved stage envelope, 10% tolerance for measurement and eaves.
    (w0, w1), (d0, d1), (h0, h1) = target["width"], target["depth"], target["height"]
    stage = {"frontage_over_depth": [round(w0 / d1 * 0.9, 3), round(w1 / d0 * 1.1, 3)],
             "height_over_frontage": [round(h0 / w1 * 0.9, 3), round(h1 / w0 * 1.1, 3)]}
    fd, hf = frontage / depth, size.z / frontage
    checks["stage_plan_proportion"] = stage["frontage_over_depth"][0] <= fd <= stage["frontage_over_depth"][1]
    checks["stage_height_proportion"] = stage["height_over_frontage"][0] <= hf <= stage["height_over_frontage"][1]
    stage["measured"] = {"frontage_over_depth": round(fd, 3), "height_over_frontage": round(hf, 3)}
    if "door" in options:
        k = DOOR / float(options["door"])
        world = {"width": frontage * k, "depth": depth * k, "height": size.z * k}
        stage["doorHeightSource"] = float(options["door"])
        stage["scaleToDoor"] = round(k, 4)
        stage["atDoor0_52"] = {a: round(v, 3) for a, v in world.items()}
        for axis in ("width", "depth", "height"):  # strict: the approved range, no tolerance
            lo_t, hi_t = target[axis]
            checks[f"door_normalised_{axis}_in_range"] = lo_t <= round(world[axis], 2) <= hi_t
else:
    checks["footprint_roughly_square"] = size.x > 0 and size.y > 0 and 0.6 <= size.x / size.y <= 1.67
    checks["taller_than_wide_or_close"] = size.z >= 0.7 * max(size.x, size.y)
with open(src, "rb") as f:
    sha = hashlib.sha256(f.read()).hexdigest()
report = {
    "source": os.path.abspath(src),
    "sha256": sha,
    "bytes": os.path.getsize(src),
    "meshes": len(meshes),
    "triangles": tris,
    "materials": materials,
    "images": images,
    "armatures": armatures,
    "actions": [a.name for a in bpy.data.actions],
    "brokenImages": broken_images,
    "degenerateFaces": degenerate,
    "stageProportions": stage,
    "bounds_size_xyz": [round(v, 4) for v in size],
    "checks": checks,
    "passed": all(checks.values()),
}
with open(os.path.join(out_dir, name + "_report.json"), "w", encoding="utf-8") as f:
    json.dump(report, f, indent=2)
print("FORGE_VALIDATE " + json.dumps({"passed": report["passed"], "triangles": tris, "size": report["bounds_size_xyz"]}))

# ---------------------------------------------------------------- previews
center = (lo + hi) / 2
radius = max(size.x, size.y, size.z)
scene.render.engine = "BLENDER_EEVEE_NEXT"
scene.render.resolution_x = 1024
scene.render.resolution_y = 1024
scene.render.film_transparent = False
scene.view_settings.view_transform = "Standard"
world = bpy.data.worlds.new("Review")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.78, 0.82, 0.82, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.9
scene.world = world
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
sun.data.energy = 3.2
sun.rotation_euler = (math.radians(50), 0, math.radians(35))
scene.collection.objects.link(sun)
ground = bpy.data.meshes.new("Ground")
ground.from_pydata([(-1, -1, 0), (1, -1, 0), (1, 1, 0), (-1, 1, 0)], [], [(0, 1, 2, 3)])
ground_obj = bpy.data.objects.new("Ground", ground)
ground_obj.scale = (radius * 3, radius * 3, 1)
ground_obj.location = (center.x, center.y, lo.z - 0.001)
scene.collection.objects.link(ground_obj)

cam = bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
cam.data.type = "ORTHO"
cam.data.ortho_scale = radius * 1.25
scene.collection.objects.link(cam)
scene.camera = cam

axes = {"-Y": Vector((0, -1, 0)), "+Y": Vector((0, 1, 0)), "-X": Vector((-1, 0, 0)), "+X": Vector((1, 0, 0))}
front = axes[front_axis]
side = Vector((front.y, -front.x, 0))  # front rotated 90 degrees
views = {"front": front, "rear": -front, "side": side, "top": None, "45": (front + side).normalized()}
for view, direction in views.items():
    if direction is None:  # straight down, entrance at the bottom of the image
        cam.location = center + Vector((0, 0, radius * 3))
        cam.rotation_euler = (0, 0, math.atan2(front.x, -front.y))
    else:
        rise = radius * (2.1 if view == "45" else 0.25)  # 45-degree elevation for the hero view
        cam.location = center + direction * radius * 3 + Vector((0, 0, rise))
        cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(out_dir, f"{name}_{view}.png")
    bpy.ops.render.render(write_still=True)
print("FORGE_PREVIEWS " + out_dir)
