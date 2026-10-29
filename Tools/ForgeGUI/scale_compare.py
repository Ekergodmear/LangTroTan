"""Render a scale comparison of level models on their server footprints (Blender, read-only on the sources).

Each model is placed on its own 3x3-cell lot (1 cell = 1 Unity unit, grid lines every cell), scaled by the factor
given, next to a human reference of the given height. Writes <out>_front.png, <out>_45.png, <out>_top.png and
<out>_side.png (each model turned 90 degrees on its own lot so its right side faces the front camera).

Usage:
    blender -b --factory-startup -P Tools/ForgeGUI/scale_compare.py -- <out_prefix> <human_height> <glb>:<scale>:<label> [...]
"""
import math
import os
import sys

import bpy
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
out_prefix, human = os.path.abspath(args[0]), float(args[1])
items = []
for a in args[2:]:
    path, scale, label = a.rsplit(":", 2)
    items.append((path, float(scale), label))
os.makedirs(os.path.dirname(out_prefix), exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene


def mat(name, rgba, emit=0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = rgba
    if emit:
        b.inputs["Emission Color"].default_value = rgba
        b.inputs["Emission Strength"].default_value = emit
    return m


grass, line, person, _ = mat("Lot", (0.55, 0.62, 0.48, 1)), mat("Grid", (0.1, 0.1, 0.1, 1)), mat("Human", (0.85, 0.35, 0.2, 1)), mat("Text", (0.05, 0.05, 0.05, 1))
spacing = float(os.environ.get("SCALE_SPACING", "4.4"))  # lot-centre spacing; widen for oversized models
pivots = []  # one empty per model at its lot centre, turned for the side view
for i, (path, scale, label) in enumerate(items):
    ox = i * spacing
    before = set(scene.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in scene.objects if o not in before and o.type == "MESH"]
    for o in new:
        o.scale = (scale, scale, scale)
    bpy.context.view_layer.update()
    corners = [o.matrix_world @ Vector(c) for o in new for c in o.bound_box]
    lo = Vector((min(c.x for c in corners), min(c.y for c in corners), min(c.z for c in corners)))
    hi = Vector((max(c.x for c in corners), max(c.y for c in corners), max(c.z for c in corners)))
    shift = Vector((ox - (lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z))
    for o in new:
        o.location += shift
    size = hi - lo
    pivot = bpy.data.objects.new(f"Pivot {label}", None)
    pivot.location = (ox, 0, 0)
    scene.collection.objects.link(pivot)
    bpy.context.view_layer.update()
    for o in new:
        world_matrix = o.matrix_world.copy()
        o.parent = pivot
        o.matrix_world = world_matrix
    pivots.append(pivot)
    # 3x3 lot with cell grid
    bpy.ops.mesh.primitive_plane_add(size=3, location=(ox, 0, -0.005))
    bpy.context.object.data.materials.append(grass)
    for k in range(4):
        for horizontal in (True, False):
            bpy.ops.mesh.primitive_cube_add(size=1, location=(ox - 1.5 + k, 0, 0.0) if not horizontal else (ox, -1.5 + k, 0.0))
            c = bpy.context.object
            c.scale = (0.015, 3.0, 0.01) if not horizontal else (3.0, 0.015, 0.01)
            c.data.materials.append(line)
    # human reference beside the front-left corner of the lot
    bpy.ops.mesh.primitive_cylinder_add(radius=0.11, depth=human, location=(ox - 1.75, -1.75, human / 2))
    bpy.context.object.data.materials.append(person)
    print(f"SCALE_ITEM {label} | {size.x:.2f} w x {size.y:.2f} d x {size.z:.2f} h")

world = bpy.data.worlds.new("W")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.86, 0.88, 0.88, 1)
scene.world = world
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
sun.data.energy = 3.0
sun.rotation_euler = (math.radians(50), 0, math.radians(30))
scene.collection.objects.link(sun)
scene.render.engine = "BLENDER_EEVEE_NEXT"
scene.render.resolution_x, scene.render.resolution_y = 1800, 900
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
scene.collection.objects.link(cam)
scene.camera = cam
cam.data.type = "ORTHO"
mid = Vector(((len(items) - 1) * spacing / 2, 0, 1.2))
cam.data.ortho_scale = len(items) * spacing + 0.6
for view, d, rise in (("front", Vector((0, -1, 0)), 0.0), ("45", Vector((0.45, -1, 0)).normalized(), 14.0), ("top", None, 0.0),
                      ("side", Vector((0, -1, 0)), 0.0)):
    for pivot in pivots:  # right side (+X) towards the front camera (-Y) for the side view only
        pivot.rotation_euler = (0, 0, math.radians(-90) if view == "side" else 0)
    if d is None:  # straight down, entrance at the bottom of the image
        cam.location = Vector((mid.x, 0, 30))
        cam.rotation_euler = (0, 0, 0)
    else:
        cam.location = mid + d * 30 + Vector((0, 0, rise))
        cam.rotation_euler = (mid - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = f"{out_prefix}_{view}.png"
    bpy.ops.render.render(write_still=True)
print("SCALE_COMPARE " + out_prefix)
