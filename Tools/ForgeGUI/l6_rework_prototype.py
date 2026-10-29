"""Level 6 non-destructive rework prototype (Blender; the source GLB is only read, never written).

Feasibility study for the user's Level 6 review (2026-10-29). At the approved door scale (×3.444, door 0.52):
  1. Cut the model with planes at the two short galleries between the main hall and the wings (the mesh is ~5,600
     disconnected fragments, so it cannot be split by loose parts), move each wing outward by WING_SHIFT and fill the
     gap with duplicated slices of the existing gallery (original geometry, UVs and texture; no stretching).
  2. Raise the whole complex onto a new two-tier terrace (2 x 0.47, the model's own plinth and first-flight height)
     so the building gains height without any scaling: the door stays 0.52.
  3. Kitbash the terrace fronts and stairs from the model's own geometry (a carved wing-plinth slice and the first
     flight of the central stair): a central flight recessed into tier 1 that branches into two flights to tier 2.
     Four jade-capped corner pillars and the plain tier bodies are simple new meshes whose UVs point at ivory /
     jade / gold texels of the model's atlas, so in Unity everything stays one material. Writes before/after renders and an "after" GLB for isolated QA.
Usage: blender -b --factory-startup -P Tools/ForgeGUI/l6_rework_prototype.py -- <downloads_dir> <out_dir>
"""
import json
import math
import os
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Vector

downloads, out = sys.argv[sys.argv.index("--") + 1:][:2]
out = os.path.abspath(out)
os.makedirs(out, exist_ok=True)
L5 = os.path.join(downloads, "scripture_L05_attempt2_forge_fbb9a937.glb")
L6 = os.path.join(downloads, "scripture_L06_forge_d31673e5.glb")
S5, S6 = 2.60, 3.444
CUT_L, CUT_R = -1.53, 1.52               # gallery mid-planes (world units after centring), from the height profile
SLICE_L, SLICE_R = (-1.575, -1.485), (1.44, 1.60)  # gallery-only slices (max height 1.2-1.3, below the eaves)
WING_SHIFT = 0.30                        # per side

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene


def load(path, scale):
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
    lo, hi = bounds([obj])
    obj.location = Vector((-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z))
    bpy.ops.object.transform_apply(location=True)
    return obj


def bounds(objs):
    bpy.context.view_layer.update()  # matrix_world is stale after location edits until the view layer updates
    cs = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    return (Vector((min(c.x for c in cs), min(c.y for c in cs), min(c.z for c in cs))),
            Vector((max(c.x for c in cs), max(c.y for c in cs), max(c.z for c in cs))))


def copy(obj, name):
    c = obj.copy()
    c.data = obj.data.copy()
    c.name = name
    scene.collection.objects.link(c)
    return c


def keep(obj, x0=None, x1=None):
    """Bisect away everything outside x0 < x < x1 (filled caps off: the cut faces stay open like the gallery ends)."""
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    if x0 is not None:
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.mesh.bisect(plane_co=(x0, 0, 0), plane_no=(1, 0, 0), clear_inner=True)
    if x1 is not None:
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.mesh.bisect(plane_co=(x1, 0, 0), plane_no=(1, 0, 0), clear_outer=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    return obj


# ---------------------------------------------------------------- models
l5 = load(L5, S5)
before = load(L6, S6)
before.name = "L6_before"
src = copy(before, "L6_src")
left = keep(copy(src, "L6_wing_left"), None, CUT_L)
centre = keep(copy(src, "L6_centre"), CUT_L, CUT_R)
right = keep(copy(src, "L6_wing_right"), CUT_R, None)
slices = []
for (x0, x1), side in ((SLICE_L, -1), (SLICE_R, 1)):
    base = keep(copy(src, f"gallery_slice_{side}"), x0, x1)
    width = x1 - x0
    n = math.ceil(WING_SHIFT / (width * 0.8))
    for i in range(1, n + 1):
        s = copy(base, f"gallery_fill_{side}_{i}")
        s.location.x += side * WING_SHIFT * i / n
        slices.append(s)
    slices.append(base)
bpy.data.objects.remove(src)
left.location.x -= WING_SHIFT
right.location.x += WING_SHIFT
building = [left, centre, right] + slices
blo, bhi = bounds(building)

# ---------------------------------------------------------------- atlas sampling for new meshes
mat = before.data.materials[0]
img = next(n.image for n in mat.node_tree.nodes if n.type == "TEX_IMAGE" and n.image and "normal" not in n.image.name.lower()
           and n.image.colorspace_settings.name.lower() != "non-color")
w, h = img.size
px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[..., :3]
r, g, b = px[..., 0], px[..., 1], px[..., 2]
mx, mn = px.max(axis=2), px.min(axis=2)


def texel(mask):
    ys, xs = np.nonzero(mask)
    if len(xs) == 0:
        return None
    # The densest cluster: pick the masked texel whose 9x9 neighbourhood is most completely masked.
    k = np.ones((9, 9))
    from numpy.lib.stride_tricks import sliding_window_view
    pad = np.pad(mask.astype(np.float32), 4)
    score = sliding_window_view(pad, (9, 9)).sum(axis=(2, 3))
    y, x = np.unravel_index(np.argmax(score * mask), mask.shape)
    return ((x + .5) / w, (y + .5) / h), px[y, x].tolist()


uv = {
    "ivory": texel((mn > .78) & (mx - mn < .07)),
    "stone_dark": texel((mn > .55) & (mx < .8) & (mx - mn < .06)),
    "jade": texel((g > r + .25) & (b > r + .12) & (g > .45) & (g < .85)),
    "gold": texel((r > .62) & (g > .45) & (g < .7) & (b < .38) & (r - b > .3)),
}
report = {"texels": {k: (None if v is None else {"uv": [round(c, 4) for c in v[0]], "rgb": [round(c, 3) for c in v[1]]}) for k, v in uv.items()}}


def solid(name, verts_faces, kind):
    """Build a mesh from (verts, faces) with every UV on the sampled texel of `kind`; uses the model's material."""
    verts, faces = verts_faces
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    me.uv_layers.new(name=before.data.uv_layers[0].name)
    u, v = uv[kind][0]
    for loop_uv in me.uv_layers[0].data:
        loop_uv.uv = (u, v)
    me.materials.append(mat)
    ob = bpy.data.objects.new(name, me)
    scene.collection.objects.link(ob)
    return ob


def box(x0, x1, y0, y1, z0, z1):
    v = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
    f = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return v, f


def merge(*vfs):
    verts, faces = [], []
    for v, f in vfs:
        o = len(verts)
        verts += v
        faces += [tuple(i + o for i in face) for face in f]
    return verts, faces


def stairs(x0, x1, y_front, z0, z1, run=.16, toward=-1):
    """Straight flight from z0 (at y_front) up to z1, climbing in +y (toward=-1 means the foot is at the front)."""
    n = max(1, round((z1 - z0) / STEP))
    parts = []
    for i in range(n):
        zt = z0 + (z1 - z0) * (i + 1) / n
        ya = y_front + i * run
        parts.append(box(x0, x1, ya, ya + (n - i) * run, z0, zt))
    return merge(*parts), n * run


# ---------------------------------------------------------------- terrace, stairs, pillars (kitbashed)
# Pieces are duplicated from the original model so they carry its own carved texture:
#   PLINTH  a 0.6-wide slice of a wing's rear plinth wall (carved stone band, 0.47 high) tiled along the tier fronts
#   FLIGHT  the first flight of the original central stair (0.47 rise, 1.17 run): central flight and the two branches
# Tier heights equal the original plinth / first-flight rise, so steps stay exactly the model's own size.


def keep3(obj, x=(None, None), y=(None, None), z=(None, None)):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    for axis, (a, b) in enumerate((x, y, z)):
        no = [0, 0, 0]
        no[axis] = 1
        for val, inner in ((a, True), (b, False)):
            if val is None:
                continue
            co = [0, 0, 0]
            co[axis] = val
            bpy.ops.mesh.select_all(action="SELECT")
            bpy.ops.mesh.bisect(plane_co=co, plane_no=no, clear_inner=inner, clear_outer=not inner)
    bpy.ops.object.mode_set(mode="OBJECT")
    return obj


TIER = 0.47
# Rear plinth wall of the left wing (the wing front carries its stair); turned 180 degrees to face front.
PLINTH_H = 0.34  # below the wing's red walls; the plinth band sits at the top of each 0.47 tier
PLINTH = keep3(copy(before, "kit_plinth"), x=(-2.6, -2.0), y=(1.40, None), z=(None, PLINTH_H))
FLIGHT = keep3(copy(before, "kit_flight"), x=(-0.45, 0.45), y=(None, -1.10), z=(None, TIER + .005))
PLINTH_W, FLIGHT_RUN, FLIGHT_W = 0.6, 1.17, 0.9


def place(kit, name, dx, dy, dz, rot_z=0.0):
    o = copy(kit, name)
    kl, kh = bounds([kit])
    # Move the kit's own reference corner (min x, min y, base) to the target point, after rotating about Z.
    o.rotation_euler = (0, 0, rot_z)
    bpy.context.view_layer.update()
    lo, hi = bounds([o])
    o.location += Vector((dx - lo.x, dy - lo.y, dz - lo.z))
    return o


raise_by = 2 * TIER
for o in building:
    o.location.z += raise_by
blo, bhi = bounds(building)
stair_front = -2.27 - .10                     # the original stair foot (now on tier 2) needs support
t2 = (blo.x - .05, bhi.x + .05, stair_front, bhi.y + .05)
apron = FLIGHT_W + .12                        # tier-1 strip in front of tier 2 carrying the branch flights
t1 = (t2[0] - .10, t2[1] + .10, t2[2] - apron, t2[3] + .10)
kit_parts = []
# Tier fronts: tiled plinth slices (tier 1 leaves a notch for the recessed central flight)
for (x0, x1, yf), z, notch in ((t1[:2] + (t1[2],), 0.0, True), (t2[:2] + (t2[2],), TIER, False)):
    # Whole tiles only, centred between the tier corners (no overrun past the corners).
    n = int((x1 - x0) // PLINTH_W)
    start = x0 + ((x1 - x0) - n * PLINTH_W) / 2
    for i in range(n):
        x = start + i * PLINTH_W
        if notch and x < FLIGHT_W / 2 and x + PLINTH_W > -FLIGHT_W / 2:
            continue  # the recessed central flight occupies this bay
        kit_parts.append(place(PLINTH, f"plinth_{'t1' if notch else 't2'}_{i}", x, yf, z + TIER - PLINTH_H, math.pi))
# Central flight recessed into the tier-1 front; branches on the tier-1 apron climbing outward to tier 2
kit_parts.append(place(FLIGHT, "flight_central", -FLIGHT_W / 2, t1[2], 0.0))
kit_parts.append(place(FLIGHT, "flight_branch_left", -FLIGHT_W / 2 - .05 - FLIGHT_RUN, t1[2] + .06, TIER, math.radians(90)))
kit_parts.append(place(FLIGHT, "flight_branch_right", FLIGHT_W / 2 + .05, t1[2] + .06, TIER, math.radians(-90)))
bpy.data.objects.remove(PLINTH)
bpy.data.objects.remove(FLIGHT)

# Tier bodies (tops, sides, back) and the notch floor: plain ivory from the atlas, behind the textured fronts
notch = (-FLIGHT_W / 2, FLIGHT_W / 2, t1[2], t1[2] + FLIGHT_RUN)
tier_boxes = merge(
    box(t1[0], notch[0], t1[2] + .40, t1[3], 0, TIER), box(notch[1], t1[1], t1[2] + .40, t1[3], 0, TIER),
    box(notch[0], notch[1], notch[3], t1[3], 0, TIER),
    box(t2[0], t2[1], t2[2] + .40, t2[3], TIER, raise_by))
apron_top = merge(box(t1[0], notch[0], t1[2] + .30, t2[2] + .45, TIER - .02, TIER),
                  box(notch[1], t1[1], t1[2] + .30, t2[2] + .45, TIER - .02, TIER))
pillars, caps, rings = [], [], []
for x in (t1[0] + .14, t1[1] - .14):
    for y in (t1[2] + .14, t1[3] - .14):
        pillars.append(box(x - .11, x + .11, y - .11, y + .11, TIER, TIER + 1.45))
        rings.append(box(x - .14, x + .14, y - .14, y + .14, TIER + 1.40, TIER + 1.48))
        c = TIER + 1.48
        caps.append(([(x, y, c + .34), (x - .13, y, c + .12), (x, y - .13, c + .12), (x + .13, y, c + .12), (x, y + .13, c + .12), (x, y, c)],
                     [(0, 1, 2), (0, 2, 3), (0, 3, 4), (0, 4, 1), (5, 2, 1), (5, 3, 2), (5, 4, 3), (5, 1, 4)]))
new = kit_parts + [solid("terrace_bodies", tier_boxes, "stone_dark"), solid("terrace_apron", apron_top, "ivory"),
                   solid("corner_pillars", merge(*pillars), "ivory"), solid("pillar_rings", merge(*rings), "gold"),
                   solid("pillar_jade_caps", merge(*caps), "jade")]
after = building + new
alo, ahi = bounds(after)
olo, ohi = bounds([before])
report["before"] = [round(v, 2) for v in ohi - olo]
report["after"] = [round(v, 2) for v in ahi - alo]
report["buildingOnlyWidth"] = round(bhi.x - blo.x, 2)
report["raisedBy"] = raise_by
report["wingShiftEach"] = WING_SHIFT

# ---------------------------------------------------------------- export "after" (one material) and render
bpy.ops.object.select_all(action="DESELECT")
for o in after:
    o.select_set(True)
bpy.ops.export_scene.gltf(filepath=os.path.join(out, "scripture_L06_rework_prototype.glb"), use_selection=True, export_format="GLB")

spacing = 9.0
l5.location.x -= spacing
for o in after:
    o.location.x += spacing
person = bpy.data.materials.new("Human")
person.use_nodes = True
person.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (.85, .35, .2, 1)
for x in (-spacing, 0, spacing):
    bpy.ops.mesh.primitive_cylinder_add(radius=.1, depth=.85, location=(x - 4.2, -3.6, .425))
    bpy.context.object.data.materials.append(person)
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
mid = Vector((0, 0, 2.3))
for view, d, rise in (("front", Vector((0, -1, 0)), 0.0), ("45", Vector((0.45, -1, 0)).normalized(), 14.0), ("top", None, 0), ("side", "side", 0)):
    cam.data.ortho_scale = 27.5
    if d is None:
        cam.location = Vector((0, 0, 40))
        cam.rotation_euler = (0, 0, 0)
    elif d == "side":
        continue
    else:
        cam.location = mid + d * 40 + Vector((0, 0, rise))
        cam.rotation_euler = (mid - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(out, f"L5_L6before_L6after_{view}.png")
    bpy.ops.render.render(write_still=True)
# Side views, one model at a time (turned so the right side faces the camera)
scene.render.resolution_x, scene.render.resolution_y = 1000, 700
cam.data.ortho_scale = 9.0
for label, objs, x in (("L6before", [before], 0.0), ("L6after", after, spacing)):
    focus = Vector((x, 0, 2.3))
    cam.location = focus + Vector((40, 0, 0))
    cam.rotation_euler = (focus - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(out, f"{label}_side.png")
    bpy.ops.render.render(write_still=True)
# 45-degree close-up of the reworked base
scene.render.resolution_x, scene.render.resolution_y = 1400, 1000
cam.data.ortho_scale = 10.5
focus = Vector((spacing, 0, 1.8))
cam.location = focus + Vector((0.6, -1, 0)).normalized() * 30 + Vector((0, 0, 12))
cam.rotation_euler = (focus - cam.location).to_track_quat("-Z", "Y").to_euler()
scene.render.filepath = os.path.join(out, "L6after_closeup_45.png")
bpy.ops.render.render(write_still=True)
focus = Vector((0, 0, 1.8))
cam.location = focus + Vector((0.6, -1, 0)).normalized() * 30 + Vector((0, 0, 12))
cam.rotation_euler = (focus - cam.location).to_track_quat("-Z", "Y").to_euler()
scene.render.filepath = os.path.join(out, "L6before_closeup_45.png")
bpy.ops.render.render(write_still=True)
json.dump(report, open(os.path.join(out, "rework_report.json"), "w"), indent=1)
print("L6_REWORK " + json.dumps(report))
