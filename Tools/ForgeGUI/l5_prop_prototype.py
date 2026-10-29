"""Level 5 optional-prop prototype (Blender, read-only on every source GLB).

Builds, in memory only:
  * two ground-floor annex props cut from the approved Level 3 model's side rooms (bisected copies; the Level 3 GLB
    itself is never written), at the Level 3 door scale so their doors match the 0.52 convention;
  * a larger jade ornament prop (faceted jade jewel in a gold frame) placed on the Level 5 second-floor facade;
and renders Level 4 | Level 5 attempt 2 | Level 5 attempt 2 + props side by side at door 0.52 (front, 45, top), plus
an annex close-up. The derived props are also exported as GLB for review (never into Assets/).
Usage: blender -b --factory-startup -P Tools/ForgeGUI/l5_prop_prototype.py -- <downloads_dir> <out_dir>
"""
import json
import math
import os
import sys

import bpy
from mathutils import Vector

downloads, out = sys.argv[sys.argv.index("--") + 1:][:2]
out = os.path.abspath(out)
os.makedirs(out, exist_ok=True)
L3 = os.path.join(downloads, "scripture_L03_forge_56f7f2dd.glb")
L4 = os.path.join(downloads, "scripture_L04_forge_de46799c.glb")
L5 = os.path.join(downloads, "scripture_L05_attempt2_forge_fbb9a937.glb")
SCALE = {"L3": 2.4471, "L4": 2.645, "L5": 2.60}  # 0.52 / measured door (MEASUREMENTS.json)
CUT = 0.49  # L3 side room ends this far from the L3 centre line, in source units (front render)
ANNEX_TOP = 1.62  # world height just above the side-room ridge ornaments, below L3's main-roof eave tips (~1.8)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene


def load(path, scale):
    """Import, join into one object, bake scale, centre on x/y, base at z = 0."""
    before = set(scene.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in scene.objects if o not in before]
    meshes = [o for o in new if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.parent = None
    for o in new:
        if o is not obj and o.name in bpy.data.objects:
            bpy.data.objects.remove(o)
    obj.scale = Vector(obj.scale) * scale
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    lo, hi = bounds(obj)
    obj.location = Vector((-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z))
    bpy.ops.object.transform_apply(location=True)
    return obj


def bounds(obj):
    cs = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    return (Vector((min(c.x for c in cs), min(c.y for c in cs), min(c.z for c in cs))),
            Vector((max(c.x for c in cs), max(c.y for c in cs), max(c.z for c in cs))))


def bisect(obj, co, no, keep_negative):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.bisect(plane_co=co, plane_no=no, clear_inner=not keep_negative, clear_outer=keep_negative, use_fill=True)
    bpy.ops.object.mode_set(mode="OBJECT")


def mat(name, rgba, metallic=0.0, rough=0.4, emit=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = rgba
    b.inputs["Metallic"].default_value = metallic
    b.inputs["Roughness"].default_value = rough
    if emit:
        b.inputs["Emission Color"].default_value = rgba
        b.inputs["Emission Strength"].default_value = emit
    return m


# ---- models
l4 = load(L4, SCALE["L4"])
l5 = load(L5, SCALE["L5"])
l5p = load(L5, SCALE["L5"])
report = {}
for name, o in (("L4", l4), ("L5", l5)):
    lo, hi = bounds(o)
    report[name] = [round(v, 2) for v in hi - lo]

# ---- annexes cut from the Level 3 side rooms (cut at the side-room inner wall, scaled by the L3 door factor)
annexes = []
for side in (-1, 1):
    a = load(L3, SCALE["L3"])
    bisect(a, (side * CUT * SCALE["L3"], 0, 0), (1, 0, 0), keep_negative=(side < 0))
    # Drop the tips of the Level 3 main-hall eaves that overhang the side room (they sit above the side-room roof).
    bisect(a, (0, 0, ANNEX_TOP), (0, 0, 1), keep_negative=True)
    lo, hi = bounds(a)
    a.name = f"Annex_{'L' if side < 0 else 'R'}"
    annexes.append(a)
alo, ahi = bounds(annexes[0])
report["annexSize"] = [round(v, 2) for v in ahi - alo]

# Place each annex against the Level 5 hall side, front faces aligned with the ground-floor front wall.
plo, phi = bounds(l5p)
front_wall_y = plo.y + 0.30 * (phi.y - plo.y)  # behind the projecting stair and terrace lip
for side, a in zip((-1, 1), annexes):
    lo, hi = bounds(a)
    dx = (plo.x - hi.x + 0.05) if side < 0 else (phi.x - lo.x - 0.05)  # slight overlap: attached, no gap
    a.location = Vector((dx, front_wall_y - lo.y, 0))

# ---- jade ornament: faceted jewel in a gold frame on the second-floor facade (ray-cast for the wall depth)
deps = bpy.context.evaluated_depsgraph_get()
z2 = 0.43 * (phi.z - plo.z)  # second-floor band, measured from the front render (banner centre ~1.70 of 4.16)
hit, loc, *_ = scene.ray_cast(deps, Vector((0, plo.y - 5, z2)), Vector((0, 1, 0)))
wall_y = loc.y if hit else plo.y
# Mounted on the second-floor balcony front (forward of the wall) so the first-roof eave does not hide it.
mount_y = wall_y - 0.35
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.27, location=(0, mount_y - 0.06, z2))
gem = bpy.context.object
gem.scale = (1, 0.45, 1.25)
gem.data.materials.append(mat("Jade", (0.10, 0.62, 0.52, 1), rough=0.15, emit=0.2))
bpy.ops.mesh.primitive_torus_add(major_radius=0.36, minor_radius=0.045, location=(0, mount_y, z2), rotation=(math.radians(90), 0, 0))
frame = bpy.context.object
frame.scale = (1, 1, 1.25)
frame.data.materials.append(mat("Gold", (0.72, 0.55, 0.25, 1), metallic=1.0, rough=0.3))
report["jewel"] = {"heightAboveGround": round(z2, 2), "frameSize": [0.81, 1.01], "gemSize": [0.54, 0.24, 0.68], "wallHit": bool(hit), "forwardOfWall": 0.35}

# ---- layout: L4 | L5 | L5 + props, same door scale
spacing = 8.0
for o, x in ((l4, -spacing), (l5, 0.0)):
    o.location.x += x
for o in [l5p, gem, frame] + annexes:
    o.location.x += spacing
for o in (l4, l5, l5p):
    pass
lo = Vector((min(bounds(o)[0].x for o in [l5p] + annexes), 0, 0))
hi = Vector((max(bounds(o)[1].x for o in [l5p] + annexes), 0, 0))
report["L5WithAnnexesWidth"] = round(hi.x - lo.x, 2)

# Human reference 0.85 and ground
person = mat("Human", (0.85, 0.35, 0.2, 1))
for x in (-spacing, 0, spacing):
    bpy.ops.mesh.primitive_cylinder_add(radius=0.1, depth=0.85, location=(x - 3.0, -3.0, 0.425))
    bpy.context.object.data.materials.append(person)
bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, -0.005))
bpy.context.object.data.materials.append(mat("Ground", (0.6, 0.65, 0.55, 1)))

world = bpy.data.worlds.new("W")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.86, 0.88, 0.88, 1)
scene.world = world
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
sun.data.energy = 3.0
sun.rotation_euler = (math.radians(50), 0, math.radians(30))
scene.collection.objects.link(sun)
scene.render.engine = "BLENDER_EEVEE_NEXT"
scene.render.resolution_x, scene.render.resolution_y = 1800, 700
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
scene.collection.objects.link(cam)
scene.camera = cam
cam.data.type = "ORTHO"
mid = Vector((0, 0, 2.2))
for view, d, rise, ortho in (("front", Vector((0, -1, 0)), 0.0, 24.5), ("45", Vector((0.45, -1, 0)).normalized(), 14.0, 24.5), ("top", None, 0, 24.5)):
    cam.data.ortho_scale = ortho
    if d is None:
        cam.location = Vector((0, 0, 40))
        cam.rotation_euler = (0, 0, 0)
    else:
        cam.location = mid + d * 40 + Vector((0, 0, rise))
        cam.rotation_euler = (mid - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(out, f"L4_L5_L5props_{view}.png")
    bpy.ops.render.render(write_still=True)
# Close-up of L5 + props, 45 degrees
scene.render.resolution_x, scene.render.resolution_y = 1400, 1000
cam.data.ortho_scale = 9.0
focus = Vector((spacing, 0, 2.0))
cam.location = focus + Vector((0.6, -1, 0)).normalized() * 30 + Vector((0, 0, 12))
cam.rotation_euler = (focus - cam.location).to_track_quat("-Z", "Y").to_euler()
scene.render.filepath = os.path.join(out, "L5_props_closeup_45.png")
bpy.ops.render.render(write_still=True)

# Export the derived props for review (outside Assets/; the source GLBs are untouched)
for objs, name in ((annexes[:1], "annex_left_from_L3"), (annexes[1:], "annex_right_from_L3"), ([gem, frame], "jade_jewel_prop")):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.gltf(filepath=os.path.join(out, name + ".glb"), use_selection=True, export_format="GLB")
json.dump(report, open(os.path.join(out, "prop_prototype.json"), "w"), indent=1)
print("L5_PROPS " + json.dumps(report))
