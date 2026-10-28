"""Sơn Môn creatures and characters (Blender 4.5, headless).

Builds rigged, optimised meshes for the visual slice:
    spiritdeer  - Bạch Ngọc Linh Lộc (White Jade Spirit Deer)
    disciple    - one anime-proportioned disciple prototype in cultivation robes

Technique: a named skeleton graph -> Skin modifier (smooth tubes with per-joint radii) -> Subdivision ->
applied mesh, an armature built from the same named joints, automatic (bone-heat) weights. Accessories
(antlers, robes, hair, ornaments) are separate meshes weighted to their bones. Bone names are stable so
Unity scripts can drive idle/walk motion procedurally.

Usage:
    blender -b --factory-startup -P Tools/Blender/sonmon_creatures.py -- <out_dir> [name ...] [--preview <png with {name}>]

Units: 1 unit = 1 Unity unit. Z up in Blender, creatures face -Y (exported with baked axes: Y up, faces -Z).
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

PALETTE = {
    "Ivory": (0.93, 0.91, 0.86, 1), "JadeGlow": (0.35, 0.95, 0.75, 1), "Marking": (0.45, 0.85, 0.75, 1),
    "Hoof": (0.35, 0.40, 0.42, 1), "Eye": (0.05, 0.10, 0.12, 1),
    "Skin": (0.98, 0.84, 0.74, 1), "Hair": (0.08, 0.09, 0.12, 1), "RobeWhite": (0.94, 0.95, 0.96, 1),
    "RobeJade": (0.22, 0.52, 0.50, 1), "Sash": (0.15, 0.30, 0.35, 1), "Gold": (0.85, 0.65, 0.30, 1),
    "Iris": (0.30, 0.55, 0.60, 1), "Boot": (0.12, 0.14, 0.16, 1), "Highlight": (1, 1, 1, 1),
    "Blush": (0.98, 0.62, 0.62, 1), "Mouth": (0.55, 0.25, 0.27, 1),
}


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for name, color in PALETTE.items():
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes["Principled BSDF"]
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = 0.6
        if name in ("JadeGlow", "Marking"):
            bsdf.inputs["Emission Color"].default_value = color
            bsdf.inputs["Emission Strength"].default_value = 2.0 if name == "JadeGlow" else 0.6


def material_index(obj, name):
    mats = [m.name for m in obj.data.materials]
    if name not in mats:
        obj.data.materials.append(bpy.data.materials[name])
        mats.append(name)
    return mats.index(name)


# ----------------------------------------------------------------------------- skeleton -> skinned body

def skin_body(name, joints, bones, radii, subdiv=2, root="pelvis"):
    """joints: {name: (x,y,z)}, bones: [(parent, child)], radii: {joint: (rx, ry)}."""
    order = list(joints)
    mesh = bpy.data.meshes.new(name + "_skel")
    mesh.from_pydata([joints[j] for j in order], [(order.index(a), order.index(b)) for a, b in bones], [])
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    skin = obj.modifiers.new("skin", "SKIN")
    skin.use_smooth_shade = True
    for i, j in enumerate(order):
        mesh.skin_vertices[0].data[i].radius = radii[j]
        mesh.skin_vertices[0].data[i].use_root = j == root
    sub = obj.modifiers.new("sub", "SUBSURF")
    sub.levels = subdiv
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    for m in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=m.name)
    return obj


def armature(name, joints, bones):
    arm_data = bpy.data.armatures.new(name + "_rig")
    rig = bpy.data.objects.new(name + "_rig", arm_data)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    made = {}
    root = bones[0][0]  # root bone at the first joint, pointing up, so accessories can bind to it
    made[root] = arm_data.edit_bones.new(root)
    made[root].head = joints[root]
    made[root].tail = Vector(joints[root]) + Vector((0, 0, 0.08))
    for parent, child in bones:  # one bone per edge, named after the child joint
        bone = arm_data.edit_bones.new(child)
        bone.head = joints[parent]
        bone.tail = joints[child]
        made[child] = bone
    for parent, child in bones:
        made[child].parent = made[parent]
        made[child].use_connect = parent != root and (made[parent].tail - made[child].head).length < 1e-4
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def bind(rig, objs):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")


def bind_rigid(rig, obj, bone):
    """Weight every vertex of an accessory fully to one bone."""
    obj.parent = rig
    group = obj.vertex_groups.new(name=bone)
    group.add(range(len(obj.data.vertices)), 1.0, "REPLACE")
    mod = obj.modifiers.new("rig", "ARMATURE")
    mod.object = rig


def assign_faces(obj, slot, predicate):
    idx = material_index(obj, slot)
    for poly in obj.data.polygons:
        if predicate(poly.center, poly.normal):
            poly.material_index = idx


def new_obj(name, bm, slot, two_sided=False):
    mesh = bpy.data.meshes.new(name)
    if not two_sided:  # Unity culls back faces: make every accessory shell face outward
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(bpy.data.materials[slot])
    for p in mesh.polygons:
        p.use_smooth = True
    return obj


def tube_bm(bm, points, radius, sides=6):
    n = len(points)
    rings = []
    for i, p in enumerate(points):
        p = Vector(p)
        d = (Vector(points[min(i + 1, n - 1)]) - Vector(points[max(i - 1, 0)])).normalized()
        up = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
        u = d.cross(up).normalized()
        v = d.cross(u).normalized()
        r = radius(i / (n - 1)) if callable(radius) else radius
        rings.append([bm.verts.new(p + (u * math.cos(a) + v * math.sin(a)) * r) for a in
                      [2 * math.pi * k / sides for k in range(sides)]])
    for a, b in zip(rings, rings[1:]):
        for k in range(sides):
            bm.faces.new((a[k], a[(k + 1) % sides], b[(k + 1) % sides], b[k]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])


def ribbon_bm(bm, points, width, normal_hint=(0, 0, 1)):
    """Flat double-sided strip along a polyline (markings, collar bands)."""
    n = len(points)
    left, right = [], []
    for i, p in enumerate(points):
        p = Vector(p)
        d = (Vector(points[min(i + 1, n - 1)]) - Vector(points[max(i - 1, 0)])).normalized()
        side = d.cross(Vector(normal_hint))
        if side.length < 1e-4:
            side = d.cross(Vector((1, 0, 0)))
        side = side.normalized() * width * 0.5 * (1 - 0.5 * abs(2 * i / (n - 1) - 1) ** 3)
        left.append(bm.verts.new(p + side))
        right.append(bm.verts.new(p - side))
    back_left = [bm.verts.new(v.co) for v in left]  # separate vertices for the back face
    back_right = [bm.verts.new(v.co) for v in right]
    for k in range(n - 1):
        bm.faces.new((left[k], right[k], right[k + 1], left[k + 1]))
        bm.faces.new((back_left[k + 1], back_right[k + 1], back_right[k], back_left[k]))


def ellipsoid_bm(bm, center, radii, segments=12, rings=8):
    bmesh.ops.create_uvsphere(bm, u_segments=segments, v_segments=rings, radius=1.0,
                              matrix=__import__("mathutils").Matrix.Translation(center) @
                              __import__("mathutils").Matrix.Diagonal((*radii, 1.0)))


# ----------------------------------------------------------------------------- Bạch Ngọc Linh Lộc

def build_spiritdeer():
    J = {
        "pelvis": (0, 0.34, 0.80), "spine": (0, 0.0, 0.84), "chest": (0, -0.30, 0.86),
        "neck": (0, -0.44, 1.05), "head": (0, -0.54, 1.26), "snout": (0, -0.73, 1.20),
        "tail": (0, 0.50, 0.90),
        "shoulder_l": (0.12, -0.30, 0.74), "elbow_l": (0.12, -0.31, 0.42), "hoof_l": (0.12, -0.30, 0.03),
        "shoulder_r": (-0.12, -0.30, 0.74), "elbow_r": (-0.12, -0.31, 0.42), "hoof_r": (-0.12, -0.30, 0.03),
        "hip_l": (0.11, 0.33, 0.72), "hock_l": (0.11, 0.42, 0.36), "hindhoof_l": (0.11, 0.36, 0.03),
        "hip_r": (-0.11, 0.33, 0.72), "hock_r": (-0.11, 0.42, 0.36), "hindhoof_r": (-0.11, 0.36, 0.03),
        "ear_l": (0.2, -0.49, 1.36), "ear_r": (-0.2, -0.49, 1.36),
    }
    B = [("pelvis", "spine"), ("spine", "chest"), ("chest", "neck"), ("neck", "head"), ("head", "snout"),
         ("pelvis", "tail"),
         ("chest", "shoulder_l"), ("shoulder_l", "elbow_l"), ("elbow_l", "hoof_l"),
         ("chest", "shoulder_r"), ("shoulder_r", "elbow_r"), ("elbow_r", "hoof_r"),
         ("pelvis", "hip_l"), ("hip_l", "hock_l"), ("hock_l", "hindhoof_l"),
         ("pelvis", "hip_r"), ("hip_r", "hock_r"), ("hock_r", "hindhoof_r"),
         ("head", "ear_l"), ("head", "ear_r")]
    R = {"pelvis": (0.21, 0.19), "spine": (0.21, 0.2), "chest": (0.2, 0.23), "neck": (0.085, 0.09),
         "head": (0.095, 0.1), "snout": (0.05, 0.055), "tail": (0.045, 0.035),
         "shoulder_l": (0.075, 0.08), "elbow_l": (0.037, 0.037), "hoof_l": (0.031, 0.031),
         "shoulder_r": (0.075, 0.08), "elbow_r": (0.037, 0.037), "hoof_r": (0.031, 0.031),
         "hip_l": (0.085, 0.1), "hock_l": (0.037, 0.037), "hindhoof_l": (0.031, 0.031),
         "hip_r": (0.085, 0.1), "hock_r": (0.037, 0.037), "hindhoof_r": (0.031, 0.031),
         "ear_l": (0.028, 0.06), "ear_r": (0.028, 0.06)}
    body = skin_body("spiritdeer", J, B, R, subdiv=2)
    body.data.materials.append(bpy.data.materials["Ivory"])
    assign_faces(body, "Hoof", lambda c, n: c.z < 0.07)

    # Spirit markings: thin ribbons laid onto the coat with Shrinkwrap so they follow the surface as
    # smooth curves (flank waves, shoulder swirls, a crescent on the brow).
    lines = []
    for side in (1, -1):
        lines.append(([(side * 0.3, -0.28 + 0.6 * t, 0.86 + 0.07 * math.sin(t * 7.0)) for t in [k / 24 for k in range(25)]], (1, 0, 0)))
        lines.append(([(side * 0.3, -0.2 + 0.4 * t, 0.74 + 0.05 * math.sin(t * 6.0 + 1.5)) for t in [k / 16 for k in range(17)]], (1, 0, 0)))
        lines.append(([(side * 0.2, -0.32 + 0.06 * math.cos(a), 0.66 + 0.08 * math.sin(a)) for a in [k / 10 * 4.0 for k in range(11)]], (1, 0, 0)))
    lines.append(([(0.05 * math.cos(a), -0.68, 1.31 + 0.035 * math.sin(a)) for a in [math.pi * (0.1 + 0.8 * k / 10) for k in range(11)]], (0, 1, 0)))
    bm = bmesh.new()
    for line, hint in lines:
        ribbon_bm(bm, line, 0.016, hint)
    markings = new_obj("spiritdeer_markings", bm, "Marking", two_sided=True)
    wrap = markings.modifiers.new("wrap", "SHRINKWRAP")
    wrap.target = body
    wrap.wrap_method = "NEAREST_SURFACEPOINT"
    wrap.offset = 0.004
    bpy.context.view_layer.objects.active = markings
    bpy.ops.object.modifier_apply(modifier="wrap")

    bm = bmesh.new()  # glossy dark eyes set into the sides of the head, with a small highlight
    for side in (1, -1):
        ellipsoid_bm(bm, (side * 0.072, -0.605, 1.285), (0.022, 0.026, 0.02), 10, 8)
    eyes = new_obj("spiritdeer_eyes", bm, "Eye")
    bm = bmesh.new()
    for side in (1, -1):
        ellipsoid_bm(bm, (side * 0.092, -0.618, 1.295), (0.004, 0.006, 0.006), 6, 4)
    shine = new_obj("spiritdeer_eyeshine", bm, "Highlight")

    # Glowing jade antlers: branching tines, rigidly bound to the head bone.
    bm = bmesh.new()
    for side in (1, -1):
        base = Vector((side * 0.05, -0.52, 1.34))
        main = [base + Vector((side * 0.04 * t, 0.05 * t, 0.42 * t)) + Vector((side * 0.10 * math.sin(t * 2.6), 0.06 * t * t, 0))
                for t in [k / 8 for k in range(9)]]
        tube_bm(bm, [tuple(p) for p in main], lambda t: 0.022 * (1 - 0.65 * t), sides=6)
        for (t, ang, length) in ((0.35, 0.9, 0.16), (0.6, -0.5, 0.14), (0.8, 0.6, 0.11)):
            start = main[int(t * 8)]
            dir = Vector((side * math.cos(ang) * 0.6, -math.sin(ang) * 0.5, 0.75)).normalized()
            pts = [start + dir * length * k / 4 + Vector((0, 0, 0.02 * (k / 4) ** 2)) for k in range(5)]
            tube_bm(bm, [tuple(p) for p in pts], lambda u: 0.014 * (1 - 0.7 * u), sides=5)
    antlers = new_obj("spiritdeer_antlers", bm, "JadeGlow")

    rig = armature("spiritdeer", J, B)
    bind(rig, [body, markings])
    for part in (antlers, eyes, shine):
        bind_rigid(rig, part, "head")
    return rig, [body, markings, antlers, eyes, shine]


# ----------------------------------------------------------------------------- disciple prototype

def build_disciple():
    """Anime-proportioned female disciple (~1.3 units, head ≈ 1/6.5 of height) in a white-and-jade robe."""
    J = {
        "pelvis": (0, 0, 0.66), "spine": (0, 0, 0.80), "chest": (0, 0, 0.94), "neck": (0, 0, 1.03),
        "head": (0, -0.005, 1.10),
        "shoulder_l": (0.10, 0, 1.01), "elbow_l": (0.15, 0.01, 0.84), "wrist_l": (0.17, -0.02, 0.69), "hand_l": (0.175, -0.03, 0.64),
        "shoulder_r": (-0.10, 0, 1.01), "elbow_r": (-0.15, 0.01, 0.84), "wrist_r": (-0.17, -0.02, 0.69), "hand_r": (-0.175, -0.03, 0.64),
        "thigh_l": (0.055, 0, 0.62), "knee_l": (0.06, -0.01, 0.34), "ankle_l": (0.06, 0.0, 0.05), "toe_l": (0.06, -0.08, 0.02),
        "thigh_r": (-0.055, 0, 0.62), "knee_r": (-0.06, -0.01, 0.34), "ankle_r": (-0.06, 0.0, 0.05), "toe_r": (-0.06, -0.08, 0.02),
    }
    B = [("pelvis", "spine"), ("spine", "chest"), ("chest", "neck"), ("neck", "head"),
         ("chest", "shoulder_l"), ("shoulder_l", "elbow_l"), ("elbow_l", "wrist_l"), ("wrist_l", "hand_l"),
         ("chest", "shoulder_r"), ("shoulder_r", "elbow_r"), ("elbow_r", "wrist_r"), ("wrist_r", "hand_r"),
         ("pelvis", "thigh_l"), ("thigh_l", "knee_l"), ("knee_l", "ankle_l"), ("ankle_l", "toe_l"),
         ("pelvis", "thigh_r"), ("thigh_r", "knee_r"), ("knee_r", "ankle_r"), ("ankle_r", "toe_r")]
    R = {"pelvis": (0.075, 0.06), "spine": (0.06, 0.05), "chest": (0.07, 0.055), "neck": (0.022, 0.022),
         "head": (0.02, 0.02),
         "shoulder_l": (0.028, 0.028), "elbow_l": (0.021, 0.021), "wrist_l": (0.016, 0.014), "hand_l": (0.02, 0.012),
         "shoulder_r": (0.028, 0.028), "elbow_r": (0.021, 0.021), "wrist_r": (0.016, 0.014), "hand_r": (0.02, 0.012),
         "thigh_l": (0.038, 0.038), "knee_l": (0.027, 0.027), "ankle_l": (0.019, 0.019), "toe_l": (0.02, 0.016),
         "thigh_r": (0.038, 0.038), "knee_r": (0.027, 0.027), "ankle_r": (0.019, 0.019), "toe_r": (0.02, 0.016)}
    body = skin_body("disciple", J, B, R, subdiv=2)
    body.data.materials.append(bpy.data.materials["Skin"])
    assign_faces(body, "Boot", lambda c, n: c.z < 0.12)

    parts = []
    # Head: large anime head (sphere slightly taller than wide, small chin), eyes, hair, bun and hairpin.
    bm = bmesh.new()
    ellipsoid_bm(bm, (0, -0.005, 1.20), (0.085, 0.082, 0.095), 16, 12)
    head = new_obj("disciple_head", bm, "Skin")
    for side in (1, -1):  # large anime eyes: dark iris ovals with a jade ring and a highlight
        bm = bmesh.new()
        ellipsoid_bm(bm, (side * 0.032, -0.078, 1.195), (0.017, 0.006, 0.023), 10, 6)
        eye = new_obj("disciple_eye", bm, "Eye")
        bm = bmesh.new()
        ellipsoid_bm(bm, (side * 0.032, -0.0815, 1.19), (0.011, 0.003, 0.014), 8, 6)
        iris = new_obj("disciple_iris", bm, "Iris")
        bm = bmesh.new()
        ellipsoid_bm(bm, (side * 0.037, -0.0845, 1.202), (0.0045, 0.002, 0.0055), 6, 4)
        shine = new_obj("disciple_eyeshine", bm, "Highlight")
        bm = bmesh.new()
        ellipsoid_bm(bm, (side * 0.05, -0.074, 1.163), (0.014, 0.003, 0.0065), 8, 4)
        blush = new_obj("disciple_blush", bm, "Blush")
        parts += [eye, iris, shine, blush]
    bm = bmesh.new()
    ellipsoid_bm(bm, (0, -0.083, 1.145), (0.008, 0.002, 0.0028), 8, 4)
    parts.append(new_obj("disciple_mouth", bm, "Mouth"))
    bm = bmesh.new()  # hair cap covering the back and top of the head
    ellipsoid_bm(bm, (0, 0.006, 1.215), (0.092, 0.09, 0.1), 16, 12)
    for v in list(bm.verts):
        if v.co.y < -0.03 and v.co.z < 1.24:
            bm.verts.remove(v)
    hair = new_obj("disciple_hair", bm, "Hair")
    bm = bmesh.new()  # bangs: a fringe of tapered strands over the forehead
    for x, length, sweep in ((-0.062, 0.07, -0.012), (-0.03, 0.095, -0.006), (0.0, 0.085, 0.004), (0.032, 0.1, 0.01), (0.062, 0.075, 0.014)):
        top = 1.305
        tube_bm(bm, [(x * 0.85, -0.064, top), (x + sweep * 0.5, -0.088, top - length * 0.5), (x + sweep, -0.091, top - length)],
                lambda t: 0.021 * (1 - 0.8 * t), sides=6)
    for side in (1, -1):  # long side locks framing the face
        tube_bm(bm, [(side * 0.08, -0.05, 1.27), (side * 0.092, -0.06, 1.18), (side * 0.085, -0.05, 1.07)], lambda t: 0.016 * (1 - 0.6 * t), sides=5)
    bangs = new_obj("disciple_bangs", bm, "Hair")
    bm = bmesh.new()  # high bun with a jade-and-gold hairpin, and a long ponytail flowing down the back
    ellipsoid_bm(bm, (0, 0.02, 1.33), (0.045, 0.045, 0.04), 12, 8)
    tube_bm(bm, [(0, 0.06, 1.30), (0, 0.10, 1.18), (0, 0.11, 1.02), (0, 0.10, 0.88)], lambda t: 0.032 * (1 - 0.75 * t), sides=7)
    bun = new_obj("disciple_bun", bm, "Hair")
    bm = bmesh.new()
    tube_bm(bm, [(-0.09, 0.02, 1.345), (0.09, 0.02, 1.345)], 0.005, sides=5)
    ellipsoid_bm(bm, (0.095, 0.02, 1.345), (0.012, 0.012, 0.012), 8, 6)
    pin = new_obj("disciple_hairpin", bm, "Gold")
    bm = bmesh.new()
    ellipsoid_bm(bm, (-0.095, 0.02, 1.345), (0.014, 0.014, 0.014), 8, 6)
    jade = new_obj("disciple_hairjade", bm, "JadeGlow")
    parts += [head, hair, bangs, bun, pin, jade]

    # Robe: inner white robe with a crossed collar (right over left), jade outer robe flaring to the
    # ankles, wide hanging sleeves, a dark sash with trailing ribbons.
    def flare(bm, z_top, z_bottom, r_top, r_bottom, depth_scale=0.85, segments=20, rings=8, open_front=0.0):
        rows = []
        for i in range(rings + 1):
            t = i / rings
            z = z_top + (z_bottom - z_top) * t
            r = r_top + (r_bottom - r_top) * (t ** 1.4)
            row = []
            for k in range(segments):
                a = 2 * math.pi * k / segments
                row.append(bm.verts.new((math.sin(a) * r, -math.cos(a) * r * depth_scale, z)))
            rows.append(row)
        for a, b in zip(rows, rows[1:]):
            for k in range(segments):
                if open_front and (k == 0):
                    continue
                bm.faces.new((a[k], a[(k + 1) % segments], b[(k + 1) % segments], b[k]))
    bm = bmesh.new()
    flare(bm, 1.05, 0.62, 0.078, 0.092, depth_scale=0.82)
    inner = new_obj("disciple_inner_robe", bm, "RobeWhite")
    bm = bmesh.new()
    flare(bm, 0.66, 0.06, 0.088, 0.17, depth_scale=0.82, rings=10)
    skirt = new_obj("disciple_robe_skirt", bm, "RobeJade")
    bm = bmesh.new()  # crossed collar band (jiaoling): two diagonal bands meeting at the waist
    # Jiaoling: the left panel crosses over the right, forming a "y" that closes under the sash.
    over = [(0.045, -0.052, 1.05), (0.025, -0.066, 0.98), (-0.02, -0.074, 0.88), (-0.055, -0.077, 0.76)]
    under = [(-0.045, -0.052, 1.05), (-0.02, -0.064, 0.98), (0.004, -0.07, 0.92)]
    ribbon_bm(bm, [(x, y - 0.004, z) for x, y, z in over], 0.032, (0, 1, 0))
    ribbon_bm(bm, under, 0.028, (0, 1, 0))
    collar = new_obj("disciple_collar", bm, "RobeJade", two_sided=True)
    bm = bmesh.new()
    for z, r in ((0.70, 0.084), (0.735, 0.08)):
        pass
    flare(bm, 0.75, 0.68, 0.097, 0.1, depth_scale=0.84, rings=2)
    sash = new_obj("disciple_sash", bm, "Sash")
    bm = bmesh.new()
    for x in (0.02, -0.02):
        tube_bm(bm, [(x, -0.075, 0.70), (x * 1.5, -0.085, 0.55), (x * 2, -0.09, 0.40)], lambda t: 0.012 * (1 + 0.4 * t), sides=4)
    ribbons = new_obj("disciple_sash_ribbons", bm, "Sash")
    parts += [inner, collar, sash]
    sleeves = []
    for side in (1, -1):  # wide sleeves: flaring cones hanging from the elbow
        bm = bmesh.new()
        tube_bm(bm, [(side * 0.10, 0, 1.01), (side * 0.15, 0.01, 0.86), (side * 0.18, 0.0, 0.70), (side * 0.19, -0.01, 0.60)],
                lambda t: 0.032 + 0.045 * t ** 1.6, sides=10)
        sleeves.append(new_obj("disciple_sleeve_" + ("l" if side > 0 else "r"), bm, "RobeWhite"))
        trim = bmesh.new()
        tube_bm(trim, [(side * 0.185, -0.005, 0.635), (side * 0.192, -0.01, 0.598)], 0.08, sides=10)
        sleeves.append(new_obj("disciple_cuff_" + ("l" if side > 0 else "r"), trim, "RobeJade"))

    rig = armature("disciple", J, B)
    bind(rig, [body, skirt, ribbons] + sleeves)
    for p in parts:
        bind_rigid(rig, p, "head" if p in (head, hair, bangs, bun, pin, jade) or p.name.startswith(("disciple_eye", "disciple_iris", "disciple_blush", "disciple_mouth"))
                   else "chest" if p in (inner, collar) else "pelvis")
    return rig, [body, skirt, ribbons] + sleeves + parts


BUILDERS = {"spiritdeer": build_spiritdeer, "disciple": build_disciple}


def export(rig, objs, path):
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    tris = 0
    for o in objs:
        o.select_set(True)
        tris += sum(len(p.vertices) - 2 for p in o.data.polygons)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, add_leaf_bones=False,
                             object_types={"ARMATURE", "MESH"}, mesh_smooth_type="FACE", bake_anim=False)
    print(f"EXPORTED {os.path.basename(path)} tris={tris} bones={len(rig.data.bones)}")


def preview(path, target, distance):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x, scene.render.resolution_y = 900, 900
    world = bpy.data.worlds.new("w")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.65, 0.68, 1)
    scene.world = world
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = 3
    sun.rotation_euler = (math.radians(55), 0, math.radians(-30))
    bpy.context.collection.objects.link(sun)
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.lens = 60
    t = Vector(target)
    cam.location = t + Vector((distance * 0.75, -distance, distance * 0.35))
    cam.rotation_euler = (t - cam.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.collection.objects.link(cam)
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("PREVIEW", path)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    prev = None
    if "--preview" in argv:
        i = argv.index("--preview")
        prev = argv[i + 1]
        argv = argv[:i] + argv[i + 2:]
    out, names = argv[0], argv[1:] or list(BUILDERS)
    for name in names:
        reset()
        rig, objs = BUILDERS[name]()
        export(rig, objs, os.path.join(out, f"SonMon_{name}.fbx"))
        if prev:
            preview(prev.replace("{name}", name), (0, -0.2, 0.9) if name == "spiritdeer" else (0, 0, 0.75),
                    2.6 if name == "spiritdeer" else 1.9)


if __name__ == "__main__":
    main()
