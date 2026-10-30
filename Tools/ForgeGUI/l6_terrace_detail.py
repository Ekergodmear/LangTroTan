"""Level 6 refined rework: the approved composition of l6_rework_prototype.py plus a terrace detail pass.

The source GLB is only read. The building (three-storey hall, two-storey wings, roofs, round jade ornament) is the
original geometry with its original UVs and texture, cut at the galleries and moved exactly as in the approved
prototype; door 0.52, no scaling. Only the new two-tier terrace and its details are authored here:
    * tier bodies with bevelled edges, carved cornice and base trim bands, raised panel frames on the sides and rear
    * carved stone balustrades (posts, rails, jade-stone inlay panels) along the sides and rear, entrance kept clear
    * hand-modelled branching staircase: a central flight recessed into tier 1 with a jade-stone carved ramp, and two
      symmetrical side flights from tier 1 to tier 2 with jade-stone riser inlays and an open-side balustrade
      (all steps 0.094 high, 0.17 deep)
    * four corner spirit-stone pillars: base, framed shaft, antique-gold rings, capital and faceted jade cap
Materials are procedural (ivory stone with tile seams and variation, darker trim stone, jade stone, jade, gold) and
are baked to a texture. The model uses two materials: the building and kitbashed pieces keep the ORIGINAL material,
UVs and 2048 textures untouched; the terrace has its own small set (2048 albedo, 512 metallic/smoothness, no normal
map: the terrace is flat-shaded geometry). Unity-ready FBX pair + textures are written to <out_dir>/unity_export/.
Writes renders, a QA report and the refined GLB for isolated Unity QA.
Usage: blender -b --factory-startup -P Tools/ForgeGUI/l6_terrace_detail.py -- <downloads_dir> <out_dir>
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
CUT_L, CUT_R = -1.53, 1.52
SLICE_L, SLICE_R = (-1.575, -1.485), (1.44, 1.60)
WING_SHIFT = 0.30
TIER = 0.47                 # the model's own plinth / first-flight height
SINK = 0.01                 # building base sits 1 cm into the upper tier: no coplanar faces at the join
BAKE = 2048

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene


# ---------------------------------------------------------------- helpers (as in l6_rework_prototype.py)
def bounds(objs):
    bpy.context.view_layer.update()
    cs = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    return (Vector((min(c.x for c in cs), min(c.y for c in cs), min(c.z for c in cs))),
            Vector((max(c.x for c in cs), max(c.y for c in cs), max(c.z for c in cs))))


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


def copy(obj, name):
    c = obj.copy()
    c.data = obj.data.copy()
    c.name = name
    scene.collection.objects.link(c)
    return c


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


def place(kit, name, dx, dy, dz, rot_z=0.0):
    o = copy(kit, name)
    o.rotation_euler = (0, 0, rot_z)
    lo, hi = bounds([o])
    o.location += Vector((dx - lo.x, dy - lo.y, dz - lo.z))
    return o


# ---------------------------------------------------------------- building (approved composition)
l5 = load(L5, S5)
before = load(L6, S6)
before.name = "L6_original"
orig_mat = before.data.materials[0]
src = copy(before, "L6_src")
left = keep3(copy(src, "L6_wing_left"), x=(None, CUT_L))
centre = keep3(copy(src, "L6_centre"), x=(CUT_L, CUT_R))
right = keep3(copy(src, "L6_wing_right"), x=(CUT_R, None))
slices = []
for (x0, x1), side in ((SLICE_L, -1), (SLICE_R, 1)):
    base = keep3(copy(src, f"gallery_slice_{side}"), x=(x0, x1))
    n = math.ceil(WING_SHIFT / ((x1 - x0) * 0.8))
    for i in range(1, n + 1):
        s = copy(base, f"gallery_fill_{side}_{i}")
        s.location.x += side * WING_SHIFT * i / n
        slices.append(s)
    slices.append(base)
bpy.data.objects.remove(src)
left.location.x -= WING_SHIFT
right.location.x += WING_SHIFT
building = [left, centre, right] + slices
raise_by = 2 * TIER
for o in building:
    o.location.z += raise_by - SINK
blo, bhi = bounds(building)

# One carved panel of the main hall's rear plinth (clean band, divider at each end), turned to face front.
PLINTH_H = 0.44
PLINTH = keep3(copy(before, "kit_plinth"), x=(-0.43, 0.43), y=(1.85, None), z=(None, PLINTH_H))
PLINTH_W, FLIGHT_RUN, FLIGHT_W = 0.86, 0.85, 0.9  # five 0.094 x 0.17 steps per flight
stair_front = -2.27 - .10
t2 = (blo.x - .05, bhi.x + .05, stair_front, bhi.y + .05)
apron = FLIGHT_W + .12
t1 = (t2[0] - .10, t2[1] + .10, t2[2] - apron, t2[3] + .10)
kit = []
front_fills = []  # (x0, x1, front y, base z): leftover front lengths closed with plain stone later
# Tier 1: tiles laid outward from both sides of the recessed central flight so they meet it exactly.
# Tier 2: tiles centred. Leftover lengths at the ends become stone fill blocks.
segments = [(t1[0], -FLIGHT_W / 2, t1[2], 0.0, "out_left"), (FLIGHT_W / 2, t1[1], t1[2], 0.0, "out_right"),
            (t2[0], t2[1], t2[2], TIER, "centred")]
for x0, x1, yf, z, mode in segments:
    n = int((x1 - x0) // PLINTH_W)
    if mode == "out_left":
        starts = [x1 - (i + 1) * PLINTH_W for i in range(n)]
        fills = [(x0, x1 - n * PLINTH_W)]
    elif mode == "out_right":
        starts = [x0 + i * PLINTH_W for i in range(n)]
        fills = [(x0 + n * PLINTH_W, x1)]
    else:
        st = x0 + ((x1 - x0) - n * PLINTH_W) / 2
        starts = [st + i * PLINTH_W for i in range(n)]
        fills = [(x0, st), (st + n * PLINTH_W, x1)]
    for i, x in enumerate(starts):
        kit.append(place(PLINTH, f"plinth_{mode}_{i}", x, yf, z + TIER - PLINTH_H, math.pi))
    front_fills += [(f0, f1, yf, z) for f0, f1 in fills if f1 - f0 > .005]
# The generated mesh has some inconsistently oriented faces; on the turned plinth copies they face the camera and
# vanish under Unity's back-face culling. Make only these copies double-sided (duplicated faces, reversed winding).
for o in kit:
    bm = bmesh.new()
    bm.from_mesh(o.data)
    dup = bmesh.ops.duplicate(bm, geom=list(bm.faces))
    bmesh.ops.reverse_faces(bm, faces=[g for g in dup["geom"] if isinstance(g, bmesh.types.BMFace)])
    bm.to_mesh(o.data)
    bm.free()
bpy.data.objects.remove(PLINTH)


# ---------------------------------------------------------------- terrace detail geometry
def emission_material(name, albedo, rough, metal, kind):
    """Procedural material whose Emission carries either the albedo network ('albedo') or (0, rough, metal) ('mr')."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    outn = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(em.outputs[0], outn.inputs[0])
    m["albedo_rgb"] = albedo
    m["kind"] = kind
    m["rough"], m["metal"] = rough, metal
    tc = nt.nodes.new("ShaderNodeTexCoord")
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(tc.outputs["Object"], sep.inputs[0])
    nsep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(geo.outputs["Normal"], nsep.inputs[0])
    absz = nt.nodes.new("ShaderNodeMath")
    absz.operation = "ABSOLUTE"
    nt.links.new(nsep.outputs["Z"], absz.inputs[0])
    top = nt.nodes.new("ShaderNodeMath")
    top.operation = "GREATER_THAN"
    top.inputs[1].default_value = .5
    nt.links.new(absz.outputs[0], top.inputs[0])
    # Projection: tops use (x, y); sides use (x + y, z) (faces are axis aligned, so one of x / y is constant)
    xy = nt.nodes.new("ShaderNodeCombineXYZ")
    nt.links.new(sep.outputs["X"], xy.inputs[0])
    nt.links.new(sep.outputs["Y"], xy.inputs[1])
    add = nt.nodes.new("ShaderNodeMath")
    add.operation = "ADD"
    nt.links.new(sep.outputs["X"], add.inputs[0])
    nt.links.new(sep.outputs["Y"], add.inputs[1])
    side = nt.nodes.new("ShaderNodeCombineXYZ")
    nt.links.new(add.outputs[0], side.inputs[0])
    nt.links.new(sep.outputs["Z"], side.inputs[1])
    proj = nt.nodes.new("ShaderNodeMix")
    proj.data_type = "VECTOR"
    nt.links.new(top.outputs[0], proj.inputs["Factor"])
    nt.links.new(side.outputs[0], proj.inputs[4])
    nt.links.new(xy.outputs[0], proj.inputs[5])
    base = Vector(albedo)
    if kind in ("ivory", "trim", "jadestone"):
        brick = nt.nodes.new("ShaderNodeTexBrick")
        tile = .45 if kind == "ivory" else .30
        brick.inputs["Scale"].default_value = 1.0 / tile
        brick.inputs["Mortar Size"].default_value = .012 if kind == "ivory" else .008
        brick.inputs["Mortar Smooth"].default_value = .2
        brick.offset = .5
        brick.inputs["Color1"].default_value = (*(base * 1.03), 1)
        brick.inputs["Color2"].default_value = (*(base * .92), 1)
        brick.inputs["Mortar"].default_value = (*(base * .58), 1)
        nt.links.new(proj.outputs[1], brick.inputs["Vector"])
        noise = nt.nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 6.0
        noise.inputs["Detail"].default_value = 6.0
        nt.links.new(tc.outputs["Object"], noise.inputs["Vector"])
        ramp = nt.nodes.new("ShaderNodeMapRange")
        ramp.inputs[1].default_value, ramp.inputs[2].default_value = .3, .7
        ramp.inputs[3].default_value, ramp.inputs[4].default_value = .90, 1.05
        nt.links.new(noise.outputs["Fac"], ramp.inputs[0])
        mul = nt.nodes.new("ShaderNodeMix")
        mul.data_type = "RGBA"
        mul.blend_type = "MULTIPLY"
        mul.inputs["Factor"].default_value = 1.0
        nt.links.new(brick.outputs["Color"], mul.inputs[6])
        nt.links.new(ramp.outputs[0], mul.inputs[7])
        color_out = mul.outputs[2]
    else:
        noise = nt.nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 18.0
        nt.links.new(tc.outputs["Object"], noise.inputs["Vector"])
        ramp = nt.nodes.new("ShaderNodeMapRange")
        ramp.inputs[3].default_value, ramp.inputs[4].default_value = .9, 1.06
        nt.links.new(noise.outputs["Fac"], ramp.inputs[0])
        mul = nt.nodes.new("ShaderNodeMix")
        mul.data_type = "RGBA"
        mul.blend_type = "MULTIPLY"
        mul.inputs["Factor"].default_value = 1.0
        mul.inputs[6].default_value = (*base, 1)
        nt.links.new(ramp.outputs[0], mul.inputs[7])
        color_out = mul.outputs[2]
    m["color_socket_node"] = color_out.node.name
    nt.links.new(color_out, em.inputs["Color"])
    return m


# Tones: ivory slightly darker than the brightest atlas texel so the terrace supports, not outshines, the hall
MAT = {
    # Warm, soft jade-white (a touch dimmer than before) so the roofs and red timber stay the focus
    "ivory": emission_material("T_Ivory", (.66, .66, .59), .62, 0, "ivory"),
    "trim": emission_material("T_Trim", (.57, .57, .50), .58, 0, "trim"),
    "jadestone": emission_material("T_JadeStone", (.52, .66, .61), .45, 0, "jadestone"),
    "jade": emission_material("T_Jade", (.12, .56, .49), .22, 0, "jade"),
    "gold": emission_material("T_Gold", (.64, .50, .30), .35, 1, "gold"),
}
parts = []


def part(name, verts, faces, kind, bevel=0.0):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    scene.collection.objects.link(ob)
    ob.data.materials.append(MAT[kind])
    if bevel:
        mod = ob.modifiers.new("bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = "ANGLE"
    parts.append(ob)
    return ob


def box(name, x0, x1, y0, y1, z0, z1, kind, bevel=0.0):
    v = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
    f = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return part(name, v, f, kind, bevel)


notch = (-FLIGHT_W / 2, FLIGHT_W / 2, t1[2], t1[2] + FLIGHT_RUN)
# Tier bodies (front faces stay behind the kitbashed carved plinth bands)
box("tier1_left", t1[0], notch[0], t1[2] + .40, t1[3], 0, TIER, "ivory", .015)
box("tier1_right", notch[1], t1[1], t1[2] + .40, t1[3], 0, TIER, "ivory", .015)
box("tier1_back_of_notch", notch[0], notch[1], notch[3], t1[3], 0, TIER, "ivory", .015)
box("tier2", t2[0], t2[1], t2[2] + .40, t2[3], TIER, raise_by, "ivory", .015)
box("apron_left", t1[0], notch[0], t1[2] + .30, t2[2] + .45, TIER - .02, TIER + .002, "ivory", .006)
box("apron_right", notch[1], t1[1], t1[2] + .30, t2[2] + .45, TIER - .02, TIER + .002, "ivory", .006)
# Close the leftover front lengths (tile ends) with plain stone the full tier height, flush with the carved bands
for k, (f0, f1, yf, z) in enumerate(front_fills):
    box(f"front_fill_{k}", f0, f1, yf, yf + .42, z, z + TIER, "trim", .008)

# Cornice and base bands on the sides and rear of both tiers (fronts carry the carved plinth bands)
for tag, (x0, x1, y0, y1), zb in (("t1", t1, 0.0), ("t2", t2, TIER)):
    zt = zb + TIER
    yb = y0 + .40
    for zz0, zz1, ov, kind, nm in ((zt - .06, zt + .006, .03, "trim", "cornice"), (zb, zb + .08, .025, "trim", "base")):
        box(f"{tag}_{nm}_left", x0 - ov, x0 + .02, yb, y1 + ov, zz0, zz1, kind, .008)
        box(f"{tag}_{nm}_right", x1 - .02, x1 + ov, yb, y1 + ov, zz0, zz1, kind, .008)
        box(f"{tag}_{nm}_rear", x0 - ov, x1 + ov, y1 - .02, y1 + ov, zz0, zz1, kind, .008)
    # Raised panel frames between cornice and base on the sides and rear
    ph0, ph1 = zb + .12, zt - .10
    def frames(axis_vals, fixed, face_out, along):
        for a0, a1 in axis_vals:
            for (u0, u1, w0, w1) in ((a0, a1, ph1 - .03, ph1), (a0, a1, ph0, ph0 + .03), (a0, a0 + .03, ph0, ph1), (a1 - .03, a1, ph0, ph1)):
                if along == "y":
                    box(f"{tag}_frame", fixed if face_out > 0 else fixed - .012, fixed + .012 if face_out > 0 else fixed,
                        u0, u1, w0, w1, "trim")
                else:
                    box(f"{tag}_frame", u0, u1, fixed if face_out > 0 else fixed - .012, fixed + .012 if face_out > 0 else fixed,
                        w0, w1, "trim")
    def spans(a0, a1, w=.55, gap=.12):
        n = max(1, int((a1 - a0) // (w + gap)))
        st = a0 + ((a1 - a0) - n * (w + gap) + gap) / 2
        return [(st + i * (w + gap), st + i * (w + gap) + w) for i in range(n)]
    frames(spans(yb + .1, y1 - .1), x0, -1, "y")
    frames(spans(yb + .1, y1 - .1), x1, 1, "y")
    frames(spans(x0 + .1, x1 - .1), y1, 1, "x")


# Carved balustrades on the sides and rear of both tiers (entrance side left open)
def balustrade(tag, p0, p1, z, inset):
    (ax, ay), (bx, by) = p0, p1
    length = math.hypot(bx - ax, by - ay)
    n = max(1, int(length // .42))
    dx, dy = (bx - ax) / length, (by - ay) / length
    nx, ny = -dy * inset, dx * inset
    pts = [(ax + dx * length * i / n + nx, ay + dy * length * i / n + ny) for i in range(n + 1)]
    for i, (px, py) in enumerate(pts):
        box(f"{tag}_post_{i}", px - .035, px + .035, py - .035, py + .035, z, z + .27, "ivory", .006)
        box(f"{tag}_post_cap_{i}", px - .045, px + .045, py - .045, py + .045, z + .27, z + .30, "trim", .006)
    x0, y0 = pts[0]
    x1, y1 = pts[-1]
    half = .022
    if abs(dx) > abs(dy):
        box(f"{tag}_rail_top", min(x0, x1), max(x0, x1), y0 - half, y0 + half, z + .21, z + .25, "trim", .005)
        box(f"{tag}_rail_bottom", min(x0, x1), max(x0, x1), y0 - half, y0 + half, z + .02, z + .05, "trim", .005)
        for i in range(n):
            a, b = pts[i][0] + .05, pts[i + 1][0] - .05
            box(f"{tag}_panel_{i}", min(a, b), max(a, b), y0 - .012, y0 + .012, z + .07, z + .19, "jadestone")
    else:
        box(f"{tag}_rail_top", x0 - half, x0 + half, min(y0, y1), max(y0, y1), z + .21, z + .25, "trim", .005)
        box(f"{tag}_rail_bottom", x0 - half, x0 + half, min(y0, y1), max(y0, y1), z + .02, z + .05, "trim", .005)
        for i in range(n):
            a, b = pts[i][1] + .05, pts[i + 1][1] - .05
            box(f"{tag}_panel_{i}", x0 - .012, x0 + .012, min(a, b), max(a, b), z + .07, z + .19, "jadestone")


zt1, zt2 = TIER + .006, raise_by + .006
# Tier 1: sides from the front apron back to the rear, and the rear (behind tier 2, a narrow walk)
balustrade("t1_left", (t1[0] + .06, t1[2] + .45), (t1[0] + .06, t1[3] - .06), zt1, 0)
balustrade("t1_right", (t1[1] - .06, t1[2] + .45), (t1[1] - .06, t1[3] - .06), zt1, 0)
balustrade("t1_rear", (t1[0] + .06, t1[3] - .06), (t1[1] - .06, t1[3] - .06), zt1, 0)
# Tier 2: sides and rear, kept outside the building footprint
balustrade("t2_left", (t2[0] + .05, t2[2] + .50), (t2[0] + .05, t2[3] - .05), zt2, 0)
balustrade("t2_right", (t2[1] - .05, t2[2] + .50), (t2[1] - .05, t2[3] - .05), zt2, 0)
balustrade("t2_rear", (t2[0] + .05, t2[3] - .05), (t2[1] - .05, t2[3] - .05), zt2, 0)


# Hand-modelled branching staircase (replaces the rotated copies of the model's own stair)
STEPS = 5
RISE = TIER / STEPS          # 0.094: identical riser height on every flight
TREAD = FLIGHT_RUN / STEPS   # 0.17: identical tread depth on every flight
ORDER = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]


def sloped_x(name, xa, xb, y0, y1, za0, za1, zb0, zb1, kind, bevel=0.0):
    """Box whose bottom/top run from (xa: za0/za1) to (xb: zb0/zb1); xa < xb keeps the winding outward."""
    if xa > xb:
        xa, xb, za0, za1, zb0, zb1 = xb, xa, zb0, zb1, za0, za1
    v = [(xa, y0, za0), (xb, y0, zb0), (xb, y1, zb0), (xa, y1, za0), (xa, y0, za1), (xb, y0, zb1), (xb, y1, zb1), (xa, y1, za1)]
    return part(name, v, ORDER, kind, bevel)


def sloped_y(name, x0, x1, ya, yb, za0, za1, zb0, zb1, kind, bevel=0.0):
    v = [(x0, ya, za0), (x1, ya, za0), (x1, yb, zb0), (x0, yb, zb0), (x0, ya, za1), (x1, ya, za1), (x1, yb, zb1), (x0, yb, zb1)]
    return part(name, v, ORDER, kind, bevel)


# Central flight in the tier-1 notch, climbing +y from the tier-1 front to the landing in front of tier 2.
cx0, cx1, ramp = -FLIGHT_W / 2 + .02, FLIGHT_W / 2 - .02, .15
for i in range(STEPS):
    ya = t1[2] + i * TREAD
    for a0, a1, tag in ((cx0, -ramp, "l"), (ramp, cx1, "r")):
        box(f"cflight_{tag}_{i}", a0, a1, ya, notch[3], i * RISE, (i + 1) * RISE, "ivory", .004)
# Carved jade-stone ramp (imperial way) between the two step halves, framed by trim-stone edges
sloped_y("cflight_ramp", -ramp, ramp, t1[2], notch[3], 0, RISE * .6, 0, TIER, "jadestone")
for sx in (-1, 1):
    e0, e1 = sorted((sx * ramp, sx * (ramp + .025)))
    sloped_y(f"cflight_ramp_edge_{sx}", e0, e1, t1[2], notch[3], 0, RISE * .6 + .015, 0, TIER + .015, "trim")

# Two symmetrical side flights on the tier-1 landing: feet beside the central flight, climbing outward to tier 2.
yA, yB = t1[2] + .06, t2[2]
for side in (-1, 1):
    foot = side * (FLIGHT_W / 2 + .05)
    top = foot + side * FLIGHT_RUN
    for i in range(STEPS):
        xa = foot + side * i * TREAD
        x0, x1 = sorted((xa, top))
        box(f"bflight_{side}_{i}", x0, x1, yA, yB, TIER + i * RISE, TIER + (i + 1) * RISE, "ivory", .004)
        i0, i1 = sorted((xa, xa - side * .006))  # jade-stone inlay on each riser, facing the central flight
        box(f"bflight_inlay_{side}_{i}", i0, i1, yA + .24, yB - .24, TIER + i * RISE + .022, TIER + (i + 1) * RISE - .022, "jadestone")
    # Open-side balustrade along the flight edge facing the front: posts on the steps and a sloped rail
    yc0, yc1 = yA + .015, yA + .065
    for i in (0, 2, STEPS - 1):
        xp = foot + side * (i + .5) * TREAD
        base = TIER + (i + 1) * RISE
        box(f"bflight_post_{side}_{i}", xp - .03, xp + .03, yc0 - .005, yc1 + .005, base, base + .30, "ivory", .006)
        box(f"bflight_post_cap_{side}_{i}", xp - .04, xp + .04, yc0 - .015, yc1 + .015, base + .30, base + .33, "trim", .005)
    xs, xe = foot + side * .5 * TREAD, foot + side * (STEPS - .5) * TREAD
    zs, ze = TIER + RISE, TIER + STEPS * RISE
    sloped_x(f"bflight_rail_top_{side}", xs, xe, yc0, yc1, zs + .24, zs + .28, ze + .24, ze + .28, "trim", .004)
    sloped_x(f"bflight_rail_low_{side}", xs, xe, yc0, yc1, zs + .03, zs + .06, ze + .03, ze + .06, "trim", .004)
    sloped_x(f"bflight_panel_{side}", xs, xe, yc0 + .013, yc1 - .013, zs + .07, zs + .22, ze + .07, ze + .22, "jadestone")

for sx in (-1, 1):
    x = sx * (FLIGHT_W / 2 + .03)
    box(f"notch_wall_{sx}", x - .03, x + .03, t1[2], notch[3], 0, TIER + .02, "trim", .006)

# Corner spirit-stone pillars (lower-tier corners)
pillar_crystals = []
for i, x in enumerate((t1[0] + .16, t1[1] - .16)):
    for j, y in enumerate((t1[2] + .16, t1[3] - .16)):
        tag = f"pillar_{i}{j}"
        z = TIER + .006
        box(f"{tag}_base", x - .17, x + .17, y - .17, y + .17, z, z + .12, "trim", .012)
        box(f"{tag}_base2", x - .14, x + .14, y - .14, y + .14, z + .12, z + .18, "ivory", .01)
        box(f"{tag}_shaft", x - .11, x + .11, y - .11, y + .11, z + .18, z + 1.38, "ivory", .012)
        for k, (zz, hh) in enumerate(((z + .42, .05), (z + 1.30, .06))):
            box(f"{tag}_gold_ring_{k}", x - .125, x + .125, y - .125, y + .125, zz, zz + hh, "gold", .006)
        # Carved panel frames on the four shaft faces
        for sxx, syy in ((0, -1), (0, 1), (-1, 0), (1, 0)):
            for zz0, zz1 in ((z + .52, z + 1.22),):
                if sxx == 0:
                    yy = y + syy * .11
                    for (u0, u1, w0, w1) in ((x - .07, x + .07, zz1 - .025, zz1), (x - .07, x + .07, zz0, zz0 + .025),
                                             (x - .07, x - .045, zz0, zz1), (x + .045, x + .07, zz0, zz1)):
                        box(f"{tag}_frame", u0, u1, min(yy, yy + syy * .012), max(yy, yy + syy * .012), w0, w1, "trim")
                    box(f"{tag}_inlay", x - .045, x + .045, min(yy, yy + syy * .004), max(yy, yy + syy * .004), zz0 + .025, zz1 - .025, "jadestone")
                else:
                    xx = x + sxx * .11
                    for (u0, u1, w0, w1) in ((y - .07, y + .07, zz1 - .025, zz1), (y - .07, y + .07, zz0, zz0 + .025),
                                             (y - .07, y - .045, zz0, zz1), (y + .045, y + .07, zz0, zz1)):
                        box(f"{tag}_frame", min(xx, xx + sxx * .012), max(xx, xx + sxx * .012), u0, u1, w0, w1, "trim")
                    box(f"{tag}_inlay", min(xx, xx + sxx * .004), max(xx, xx + sxx * .004), y - .045, y + .045, zz0 + .025, zz1 - .025, "jadestone")
        box(f"{tag}_capital", x - .15, x + .15, y - .15, y + .15, z + 1.38, z + 1.46, "trim", .01)
        box(f"{tag}_gold_collar", x - .09, x + .09, y - .09, y + .09, z + 1.46, z + 1.50, "gold", .005)
        c = z + 1.50
        pillar_crystals.append((x, y, c + .20))
        sides, r = 8, .12
        ring = [(x + r * math.cos(2 * math.pi * s / sides), y + r * math.sin(2 * math.pi * s / sides), c + .12) for s in range(sides)]
        verts = ring + [(x, y, c + .40), (x, y, c)]
        faces = [(s, (s + 1) % sides, sides) for s in range(sides)] + [((s + 1) % sides, s, sides + 1) for s in range(sides)]
        part(f"{tag}_jade_cap", verts, faces, "jade")
        bpy.ops.mesh.primitive_uv_sphere_add(radius=.03, segments=10, ring_count=6, location=(x, y, c + .42))
        finial = bpy.context.object
        finial.name = f"{tag}_gold_finial"
        finial.data.materials.append(MAT["gold"])
        parts.append(finial)

# ---------------------------------------------------------------- QA before joining: floating parts
# A part is attached when its bounds, grown by 2 cm, touch another terrace part, the building or the kit pieces.
others = building + kit
boxes = {o.name: bounds([o]) for o in parts + others}
floating = []
for p in parts:
    lo, hi = boxes[p.name]
    if lo.z < .01:
        continue
    g = .02
    touching = any(n != p.name and lo.x - g <= h2.x and hi.x + g >= l2.x and lo.y - g <= h2.y and hi.y + g >= l2.y
                   and lo.z - g <= h2.z and hi.z + g >= l2.z for n, (l2, h2) in boxes.items())
    if not touching:
        floating.append(p.name)

# ---------------------------------------------------------------- join terrace, unwrap, bake
bpy.ops.object.select_all(action="DESELECT")
for p in parts:
    p.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
for p in parts:
    bpy.context.view_layer.objects.active = p
    for m in list(p.modifiers):
        bpy.ops.object.modifier_apply(modifier=m.name)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
terrace = bpy.context.view_layer.objects.active
terrace.name = "L6_terrace_detail"
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=.004, area_weight=0, scale_to_bounds=False)
bpy.ops.object.mode_set(mode="OBJECT")
# UV stretch metric: per-face UV area / 3D area relative to the median (1 = uniform texel density)
me = terrace.data
uvl = me.uv_layers.active.data
ratios = []
for poly in me.polygons:
    if poly.area < 1e-6:
        continue
    pts = [uvl[li].uv for li in poly.loop_indices]
    a = 0.0
    for k in range(1, len(pts) - 1):
        a += abs((pts[k] - pts[0]).cross(pts[k + 1] - pts[0])) / 2
    ratios.append(a / poly.area)
ratios = np.array(ratios)
density = {"median": float(np.median(ratios)), "p5": float(np.percentile(ratios, 5) / np.median(ratios)),
           "p95": float(np.percentile(ratios, 95) / np.median(ratios))}

scene.render.engine = "CYCLES"
scene.cycles.samples = 1
scene.cycles.device = "CPU"
bake_img = {}
for pass_name in ("albedo", "mr"):
    img = bpy.data.images.new(f"terrace_{pass_name}", BAKE, BAKE, alpha=False, float_buffer=False)
    if pass_name == "mr":
        img.colorspace_settings.name = "Non-Color"
    for m in MAT.values():
        nt = m.node_tree
        em = next(n for n in nt.nodes if n.type == "EMISSION")
        if pass_name == "mr":
            for l in list(em.inputs["Color"].links):
                nt.links.remove(l)
            em.inputs["Color"].default_value = (0, m["rough"], m["metal"], 1)
        tex = nt.nodes.get("bake_target") or nt.nodes.new("ShaderNodeTexImage")
        tex.name = "bake_target"
        tex.image = img
        nt.nodes.active = tex
    bpy.ops.object.select_all(action="DESELECT")
    terrace.select_set(True)
    bpy.context.view_layer.objects.active = terrace
    bpy.ops.object.bake(type="EMIT", margin=16)
    bake_img[pass_name] = np.array(img.pixels[:], dtype=np.float32).reshape(BAKE, BAKE, 4)

# ---------------------------------------------------------------- terrace material (original material untouched)
def find_image(socket_name):
    bsdf = next(n for n in orig_mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")

    def up(sock):
        for l in orig_mat.node_tree.links:
            if l.to_socket == sock:
                nd = l.from_node
                if nd.type == "TEX_IMAGE":
                    return nd.image
                for inp in nd.inputs:
                    r = up(inp)
                    if r:
                        return r
        return None
    return up(bsdf.inputs[socket_name])


def pixels(img):
    w, h = img.size
    return np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)


def save_png(name, arr, colorspace="sRGB"):
    h, w = arr.shape[:2]
    img = bpy.data.images.new(name, w, h, alpha=True)
    img.colorspace_settings.name = colorspace
    img.pixels.foreach_set(np.ascontiguousarray(arr, dtype=np.float32).ravel())
    img.filepath_raw = os.path.join(UNITY_OUT, name + ".png")
    img.file_format = "PNG"
    img.save()
    return img


def downsample(arr, f):
    h, w = arr.shape[:2]
    return arr.reshape(h // f, f, w // f, f, 4).mean(axis=(1, 3))


UNITY_OUT = os.path.join(out, "unity_export")
os.makedirs(UNITY_OUT, exist_ok=True)
UBASE = "ForgeGUI_ScripturePavilion_L06F"
o_alb, o_mr, o_nrm = find_image("Base Color"), find_image("Roughness"), find_image("Normal")
# Unbaked background (outside every UV island) is black; thin islands would bleed it in under mip-mapping.
# Fill it with the ivory stone tone / stone roughness so any bleed stays invisible.
empty = bake_img["albedo"][..., :3].sum(axis=2) < 1e-4
t_alb = bake_img["albedo"].copy()
t_alb[empty, :3] = MAT["ivory"]["albedo_rgb"]
t_alb[..., 3] = 1
t_mr = bake_img["mr"].copy()
t_mr[empty, :3] = (0, MAT["ivory"]["rough"], 0)
t_mr[..., 3] = 1
report_bleed_fill = float(empty.mean())
# Unity textures, same conventions as Tools/Blender/import_glb_asset.py (mask: RGB metallic, A smoothness)
save_png(f"{UBASE}_albedo", pixels(o_alb))
save_png(f"{UBASE}_normal", pixels(o_nrm), "Non-Color")
mr = pixels(o_mr)
mask = np.stack([mr[..., 2], mr[..., 2], mr[..., 2], 1 - mr[..., 1]], axis=-1)
save_png(f"{UBASE}_mask", mask, "Non-Color")
img_t_alb = save_png(f"{UBASE}_terrace_albedo", t_alb)
tm = downsample(t_mr, BAKE // 512)
save_png(f"{UBASE}_terrace_mask", np.stack([tm[..., 2], tm[..., 2], tm[..., 2], 1 - tm[..., 1]], axis=-1), "Non-Color")

tmat = bpy.data.materials.new("L6_terrace")
tmat.use_nodes = True
nt = tmat.node_tree
bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
tn = nt.nodes.new("ShaderNodeTexImage")
tn.image = img_t_alb
nt.links.new(tn.outputs["Color"], bsdf.inputs["Base Color"])
bsdf.inputs["Roughness"].default_value = MAT["ivory"]["rough"]
terrace.data.materials.clear()
terrace.data.materials.append(tmat)
refined = building + kit + [terrace]

# ---------------------------------------------------------------- QA numbers
rlo, rhi = bounds(refined)
olo, ohi = bounds([before])
report = {
    "original": [round(v, 2) for v in ohi - olo],
    "refined": [round(v, 2) for v in rhi - rlo],
    "buildingsOnlyWidth": round(bhi.x - blo.x, 2),
    "terraceTriangles": sum(len(p.vertices) - 2 for p in terrace.data.polygons),
    "totalTriangles": sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in refined),
    "floatingTerraceParts": floating,
    "terraceTexelDensity": density,
    "buildingSinkIntoTier": SINK,
    "bakeBackgroundFilled": round(report_bleed_fill, 3),
    "textures": "building: original 3 x 2048 (unchanged); terrace: 2048 albedo + 512 mask, no normal",
}

# ---------------------------------------------------------------- VFX anchors (Level 6 spirit-gathering effects)
# Frame: Blender, centred on the refined model's bounds (x right, y back, z up from the base). The Unity import
# re-centres the model the same way, so Unity local = (x, z, y) from the model's bottom-centre after the 180-degree
# entrance turn (entrance towards -Z).
deps = bpy.context.evaluated_depsgraph_get()
cxm, cym, bz = (rlo.x + rhi.x) / 2, (rlo.y + rhi.y) / 2, rlo.z
hlo, hhi = bounds([centre])
orn_z = raise_by - SINK + 0.99  # round jade screen on the hall's ground-floor centre bay (front render, door scale)
hit, loc, *_ = scene.ray_cast(deps, Vector(((hlo.x + hhi.x) / 2, rlo.y - 5, orn_z)), Vector((0, 1, 0)))
orn = loc if hit else Vector(((hlo.x + hhi.x) / 2, hlo.y, orn_z))


def c3(p):
    return [round(p[0] - cxm, 4), round(p[1] - cym, 4), round(p[2] - bz, 4)]


# The upper tier is stepped: tier2 (the generated box, 2 x TIER) is the walkable height of the front strip, but the
# hall's own plinth from the source model forms raised terraces either side of the hall. Probe their real top with
# downward rays midway between the hall and the tier edge, at mid depth (QA probe in Unity measured 1.178 / 1.195).
def surface_top(x, y):
    hit, loc, *_ = scene.ray_cast(deps, Vector((x, y, rhi.z + 5)), Vector((0, 0, -1)))
    return loc.z if hit else None


mid_y = (t2[2] + t2[3]) / 2
raised = [z for z in (surface_top((hlo.x + t2[0]) / 2, mid_y), surface_top((hhi.x + t2[1]) / 2, mid_y)) if z is not None]
tier2_raised_top = round(max(raised) - bz, 4) if raised else None


anchors = {
    "frame": "Blender centred on model bounds: x right, y back (+y away from the entrance), z up from the base; Unity local = (x, z, y)",
    "size": [round(v, 4) for v in rhi - rlo],
    "pillarCrystals": [c3(p) for p in pillar_crystals],
    "ornament": c3(orn), "ornamentHit": bool(hit),
    "hallMin": c3(hlo), "hallMax": c3(hhi),
    "tier1": [round(t1[0] - cxm, 4), round(t1[1] - cxm, 4), round(t1[2] - cym, 4), round(t1[3] - cym, 4), TIER],
    "tier2": [round(t2[0] - cxm, 4), round(t2[1] - cxm, 4), round(t2[2] - cym, 4), round(t2[3] - cym, 4), 2 * TIER],
    # new, separate field (tier2 keeps 2 x TIER for the front strip): top of the raised side terraces, or null
    "tier2RaisedTop": tier2_raised_top,
}
json.dump(anchors, open(os.path.join(out, "VFX_ANCHORS.json"), "w"), indent=1)
report["vfxAnchors"] = "VFX_ANCHORS.json"

# ---------------------------------------------------------------- export refined GLB
bpy.ops.object.select_all(action="DESELECT")
for o in refined:
    o.select_set(True)
bpy.ops.export_scene.gltf(filepath=os.path.join(out, "scripture_L06_refined.glb"), use_selection=True, export_format="GLB")

# Unity-ready pair (same re-centring as import_glb_asset.py: footprint centre, base on the ground), two FBX files
# so each keeps its own material in the isolated QA loader.
shift = Vector((-cxm, -cym, -bz))
groups = {UBASE: building + kit, UBASE + "_terrace": [terrace]}
for fname, objs in groups.items():
    dup = []
    for o in objs:
        c = o.copy()
        c.data = o.data.copy()
        scene.collection.objects.link(c)
        dup.append(c)
    bpy.ops.object.select_all(action="DESELECT")
    for c in dup:
        c.select_set(True)
    bpy.context.view_layer.objects.active = dup[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(dup) > 1:
        bpy.ops.object.join()
    j = bpy.context.view_layer.objects.active
    j.data.transform(__import__("mathutils").Matrix.Translation(shift))
    j.name = fname
    j.data.materials.clear()
    j.data.materials.append(bpy.data.materials.new(fname + "_Material"))
    bpy.ops.object.select_all(action="DESELECT")
    j.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(UNITY_OUT, fname + ".fbx"), use_selection=True,
                             apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False)
    bpy.data.objects.remove(j)

# ---------------------------------------------------------------- renders (same cameras as the earlier sheets)
orig_render = copy(before, "L6_original_render")
spacing = 9.0
l5.location.x -= spacing
for o in refined:
    o.location.x += spacing
bpy.data.objects.remove(before)  # the render copy stands in for it (unchanged geometry and material)
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
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
scene.collection.objects.link(cam)
scene.camera = cam
cam.data.type = "ORTHO"


def shot(name, focus, direction, rise, ortho, res=(1800, 700)):
    scene.render.resolution_x, scene.render.resolution_y = res
    cam.data.ortho_scale = ortho
    if direction is None:
        cam.location = focus + Vector((0, 0, 40))
        cam.rotation_euler = (0, 0, 0)
    else:
        cam.location = focus + direction.normalized() * 40 + Vector((0, 0, rise))
        cam.rotation_euler = (focus - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(out, name + ".png")
    bpy.ops.render.render(write_still=True)


mid = Vector((0, 0, 2.3))
shot("cmp_front", mid, Vector((0, -1, 0)), 0, 27.5)
shot("cmp_45", mid, Vector((.45, -1, 0)), 14, 27.5)
shot("cmp_top", mid, None, 0, 27.5)
for label, x in (("L5", -spacing), ("L6original", 0.0), ("L6refined", spacing)):
    shot(f"side_{label}", Vector((x, 0, 2.3)), Vector((1, 0, 0)), 0, 9.0, (1000, 700))
R = Vector((spacing, 0, 0))
shot("close_terrace_45", R + Vector((0, -.5, 1.3)), Vector((.6, -1, 0)), 12, 10.0, (1400, 1000))
shot("close_stairs_front", R + Vector((0, -3.0, .8)), Vector((0, -1, 0)), 3, 4.2, (1400, 1000))
shot("close_balustrade_side", R + Vector((t1[1] - .4, 0.6, .8)), Vector((1, -.6, 0)), 6, 4.0, (1400, 1000))
shot("close_pillar", R + Vector((t1[1] - .16, t1[2] + .16, 1.3)), Vector((.5, -1, 0)), 2.5, 2.6, (1000, 1200))
shot("close_material", R + Vector((t1[0] + .5, t1[2] + .7, .55)), Vector((-.4, -1, 0)), 2.0, 1.6, (1400, 1000))
json.dump(report, open(os.path.join(out, "refined_report.json"), "w"), indent=1)
print("L6_REFINED " + json.dumps(report))
