"""Tàng Kinh Các Level 5 ornamental jade crystal prop: a faceted jade crystal in a carved gold frame.

Separate static prop (the Level 5 GLB is never touched). Built procedurally in world units at the approved door
scale (door 0.52): frame about 0.70 wide x 0.95 tall (0.85 of the first prototype). Three objects so Unity can give
each its own material:
    JadeCrystal  faceted eight-sided crystal (bipyramid with a short waist)
    GoldFrame    oval ring with eight carved lobes, a ruyi cloud crest on top and a knot below
    Backplate    thin red-lacquer oval behind the crystal (wall mount)
Origin at the frame centre. The crystal faces +Y in Blender, which the FBX export (forward -Z) turns into Unity -Z,
the building front; the backplate is on the wall side.
Usage: blender -b --factory-startup -P Tools/Blender/jade_crystal_prop.py -- <out_dir>
"""
import math
import os
import sys

import bmesh
import bpy

out = os.path.abspath(sys.argv[sys.argv.index("--") + 1])
SCALE = 0.85  # the first prototype read too heavy over the first-roof eave
os.makedirs(out, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)


def crystal():
    bm = bmesh.new()
    sides, r, waist, tip = 8, 0.26, 0.12, 0.40
    ring_top = [bm.verts.new((r * math.cos(2 * math.pi * i / sides), 0, waist + r * 0 * i)) for i in range(sides)]
    ring_bot = [bm.verts.new((r * math.cos(2 * math.pi * i / sides), 0, -waist)) for i in range(sides)]
    # Ellipse in X/Y so the crystal is shallower front-to-back (wall mounted).
    for ring in (ring_top, ring_bot):
        for i, v in enumerate(ring):
            a = 2 * math.pi * i / sides
            v.co.x, v.co.y = r * math.cos(a), 0.45 * r * math.sin(a)
    top, bot = bm.verts.new((0, 0, waist + tip)), bm.verts.new((0, 0, -waist - tip))
    for i in range(sides):
        j = (i + 1) % sides
        bm.faces.new((ring_top[i], ring_top[j], top))
        bm.faces.new((ring_bot[j], ring_bot[i], bot))
        bm.faces.new((ring_top[j], ring_top[i], ring_bot[i], ring_bot[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("JadeCrystal")
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new("JadeCrystal", me)
    bpy.context.scene.collection.objects.link(obj)
    obj.location.y = 0.02
    return obj


def frame():
    parts = []
    # Oval ring
    bpy.ops.mesh.primitive_torus_add(major_radius=0.36, minor_radius=0.045, major_segments=48, minor_segments=10,
                                     rotation=(math.radians(90), 0, 0))
    ring = bpy.context.object
    ring.scale = (1, 1, 1.28)
    parts.append(ring)
    # Eight carved lobes around the ring
    for i in range(8):
        a = 2 * math.pi * i / 8 + math.pi / 8
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.055, segments=12, ring_count=8,
                                             location=(0.40 * math.cos(a), -0.01, 0.40 * 1.28 * math.sin(a)))
        lobe = bpy.context.object
        lobe.scale = (1, 0.55, 1)
        parts.append(lobe)
    # Ruyi cloud crest on top: three small ring scrolls
    for dx, r in ((-0.09, 0.06), (0.09, 0.06), (0, 0.075)):
        bpy.ops.mesh.primitive_torus_add(major_radius=r, minor_radius=0.02, major_segments=24, minor_segments=8,
                                         location=(dx, 0.0, 0.50 + (0.04 if dx == 0 else 0)), rotation=(math.radians(90), 0, 0))
        parts.append(bpy.context.object)
    # Knot below
    bpy.ops.mesh.primitive_cylinder_add(radius=0.035, depth=0.10, location=(0, 0, -0.52))
    parts.append(bpy.context.object)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.045, segments=12, ring_count=8, location=(0, 0, -0.60))
    parts.append(bpy.context.object)
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = obj.data.name = "GoldFrame"
    return obj


def backplate():
    # Thin red-lacquer oval on the wall side so the frame reads as mounted, not floating
    bpy.ops.mesh.primitive_cylinder_add(radius=0.34, depth=0.02, vertices=40, location=(0, -0.12, 0), rotation=(math.radians(90), 0, 0))
    obj = bpy.context.object
    obj.scale = (1, 1.28, 1)
    obj.name = obj.data.name = "Backplate"
    return obj


c, f, p = crystal(), frame(), backplate()
for o in (c, f, p):
    o.scale = o.scale * SCALE
    o.location = o.location * SCALE
for o in (c, f, p):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.object.shade_flat() if o is c else bpy.ops.object.shade_smooth()
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(filepath=os.path.join(out, "SonMon_JadeCrystal_L05.fbx"), use_selection=True,
                         axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS", bake_space_transform=True)
tris = sum(len(o.data.polygons) for o in (c, f, p))
print(f"JADE_PROP exported faces={tris} -> {out}")
