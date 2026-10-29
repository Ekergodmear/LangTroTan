"""Investigate (and optionally prototype) a local correction of one warped roof corner on a ForgeGUI GLB.

Read-only on the source. Reports the four roof-corner tips; with --fix <corner> it pulls the warped corner's vertices
toward the mirror image of the opposite corner (shrinkwrap to a mirrored copy, smooth radial falloff), keeps UVs and
topology untouched, renders before/after views and exports the prototype GLB to <out_dir>. Nothing in the Unity
project is touched.

Usage:
    blender -b --factory-startup -P Tools/ForgeGUI/roof_corner_probe.py -- <source.glb> <out_dir> [--fix +X+Y] [--radius 0.35]
"""
import json
import math
import os
import sys

import bpy
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
src, out_dir = args[0], os.path.abspath(args[1])
fix = args[args.index("--fix") + 1] if "--fix" in args else None
radius_frac = float(args[args.index("--radius") + 1]) if "--radius" in args else 0.35
os.makedirs(out_dir, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
mesh_obj = [o for o in bpy.context.scene.objects if o.type == "MESH"][0]
mw = mesh_obj.matrix_world
verts = [mw @ v.co for v in mesh_obj.data.vertices]
lo = Vector((min(v.x for v in verts), min(v.y for v in verts), min(v.z for v in verts)))
hi = Vector((max(v.x for v in verts), max(v.y for v in verts), max(v.z for v in verts)))
center = (lo + hi) / 2
height = hi.z - lo.z
roof_z = lo.z + 0.45 * height  # eaves sit above roughly the lower half

corners = {}
for sx in (-1, 1):
    for sy in (-1, 1):
        key = f"{'+' if sx > 0 else '-'}X{'+' if sy > 0 else '-'}Y"
        best = max((v for v in verts if v.z > roof_z), key=lambda v: sx * (v.x - center.x) + sy * (v.y - center.y))
        corners[key] = {"tip": [round(best.x - center.x, 4), round(best.y - center.y, 4), round(best.z - lo.z, 4)],
                        "reach": round(sx * (best.x - center.x) + sy * (best.y - center.y), 4)}
report = {"source": os.path.abspath(src), "bounds": [round(c, 4) for c in (hi - lo)], "corners": corners}
reaches = [c["reach"] for c in corners.values()]
heights = [c["tip"][2] for c in corners.values()]
report["reach_spread"] = round(max(reaches) - min(reaches), 4)
report["tip_height_spread"] = round(max(heights) - min(heights), 4)


def render(tag):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = scene.render.resolution_y = 768
    if scene.world is None:
        scene.world = bpy.data.worlds.new("Review")
        scene.world.use_nodes = True
        scene.world.node_tree.nodes["Background"].inputs[0].default_value = (0.78, 0.82, 0.82, 1)
    if "Sun" not in bpy.data.objects:
        sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
        sun.data.energy = 3.2
        sun.rotation_euler = (math.radians(50), 0, math.radians(35))
        scene.collection.objects.link(sun)
    cam = bpy.data.objects.get("Camera") or bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
    if cam.name not in scene.collection.objects:
        scene.collection.objects.link(cam)
    cam.data.type = "ORTHO"
    r = max(hi - lo)
    cam.data.ortho_scale = r * 1.25
    scene.camera = cam
    for view, d in {"45": Vector((1, 1, 0)).normalized(), "top": None, "rear45": Vector((-1, 1, 0)).normalized()}.items():
        if d is None:
            cam.location = center + Vector((0, 0, r * 3))
            cam.rotation_euler = (0, 0, 0)
        else:
            cam.location = center + d * r * 3 + Vector((0, 0, r * 2.1))
            cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(out_dir, f"{tag}_{view}.png")
        bpy.ops.render.render(write_still=True)


render("before")
if fix:
    sx = 1 if fix[0] == "+" else -1
    sy = 1 if fix[2] == "+" else -1
    # Mirror image of the healthy corner across the axis that separates it from the warped one (left/right).
    mirror = mesh_obj.copy()
    mirror.data = mesh_obj.data.copy()
    bpy.context.scene.collection.objects.link(mirror)
    for v in mirror.data.vertices:
        w = mw @ v.co
        w.x = 2 * center.x - w.x
        v.co = mw.inverted() @ w
    mirror.hide_render = True
    tip = Vector(corners[fix]["tip"]) + Vector((center.x, center.y, lo.z))
    radius = radius_frac * max(hi.x - lo.x, hi.y - lo.y)
    group = mesh_obj.vertex_groups.new(name="corner_fix")
    moved = 0
    for i, v in enumerate(verts):
        if v.z < roof_z:
            continue
        d = (v - tip).length
        if d < radius:
            group.add([i], 0.5 * (1 + math.cos(math.pi * d / radius)), "REPLACE")  # smooth falloff to 0 at the edge
            moved += 1
    mod = mesh_obj.modifiers.new("corner_fix", "SHRINKWRAP")
    mod.target = mirror
    mod.wrap_method = "NEAREST_SURFACEPOINT"
    mod.vertex_group = "corner_fix"
    bpy.context.view_layer.objects.active = mesh_obj
    bpy.ops.object.modifier_apply(modifier="corner_fix")
    after = [mw @ v.co for v in mesh_obj.data.vertices]
    disp = [(a - b).length for a, b in zip(after, verts)]
    report["fix"] = {"corner": fix, "radius": round(radius, 4), "vertices_in_region": moved,
                     "max_displacement": round(max(disp), 4), "mean_displacement_in_region": round(sum(d for d in disp if d > 0) / max(1, sum(1 for d in disp if d > 0)), 4),
                     "uv_and_topology": "unchanged (vertex positions only)"}
    bpy.data.objects.remove(mirror, do_unlink=True)
    render("after")
    bpy.ops.export_scene.gltf(filepath=os.path.join(out_dir, "corner_fix_prototype.glb"), export_format="GLB", use_selection=False)

with open(os.path.join(out_dir, "roof_corner_report.json"), "w", encoding="utf-8") as f:
    json.dump(report, f, indent=2)
print("ROOF_PROBE " + json.dumps(report))
