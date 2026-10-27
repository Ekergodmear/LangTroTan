"""Sơn Môn architecture kit generator (Blender 4.5, run headless).

Builds stylized Xianxia buildings from modular parts and exports one FBX per server
building type into Assets/Art/SonMon/Models. Material slot names are mapped to URP
materials by Assets/Editor/SonMonKitBuilder.cs:
    Roof, RoofTrim, RoofGrey, TrimGrey, Lacquer, Wood, Plaster, Stone, Marble, Gold, Paper, Glow, Soil, Leaf, Bamboo

Usage:
    blender -b --factory-startup -P Tools/Blender/sonmon_kit.py -- <out_dir> [type ...] [--preview <png>]

Units: 1 Blender unit = 1 Unity unit = 1 server grid cell. Origin = footprint centre at ground.
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

SLOTS = ["Roof", "RoofTrim", "RoofGrey", "TrimGrey", "Lacquer", "Wood", "Plaster", "Stone", "Marble", "Gold", "Paper", "Glow", "Soil", "Leaf", "Bamboo"]
PREVIEW_COLORS = {
    "Roof": (0.10, 0.30, 0.31, 1), "RoofTrim": (0.07, 0.20, 0.21, 1), "RoofGrey": (0.30, 0.31, 0.32, 1), "TrimGrey": (0.20, 0.21, 0.22, 1), "Lacquer": (0.50, 0.08, 0.06, 1),
    "Wood": (0.24, 0.12, 0.07, 1), "Plaster": (0.90, 0.85, 0.72, 1), "Stone": (0.55, 0.55, 0.52, 1),
    "Marble": (0.86, 0.85, 0.80, 1), "Gold": (0.85, 0.62, 0.24, 1), "Paper": (0.85, 0.22, 0.12, 1),
    "Glow": (0.45, 0.95, 0.85, 1), "Soil": (0.28, 0.18, 0.10, 1), "Leaf": (0.30, 0.55, 0.25, 1), "Bamboo": (0.55, 0.65, 0.30, 1),
}


# ----------------------------------------------------------------------------- scene helpers

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for name in SLOTS:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = PREVIEW_COLORS[name]
        bsdf.inputs["Roughness"].default_value = 0.65
        if name == "Glow":
            bsdf.inputs["Emission Color"].default_value = PREVIEW_COLORS[name]
            bsdf.inputs["Emission Strength"].default_value = 3.0


class Builder:
    """Accumulates parts into one bmesh with per-face material slots."""

    def __init__(self):
        self.bm = bmesh.new()

    def _slot(self, name):
        return SLOTS.index(name)

    def add_mesh(self, verts, faces, slot):
        base = [self.bm.verts.new(v) for v in verts]
        for f in faces:
            try:
                face = self.bm.faces.new([base[i] for i in f])
                face.material_index = self._slot(slot)
            except ValueError:
                pass

    def box(self, center, size, slot, rot_y=0.0):
        cx, cy, cz = center
        sx, sy, sz = (s * 0.5 for s in size)
        corners = [(-sx, -sy, -sz), (sx, -sy, -sz), (sx, sy, -sz), (-sx, sy, -sz),
                   (-sx, -sy, sz), (sx, -sy, sz), (sx, sy, sz), (-sx, sy, sz)]
        c, s = math.cos(rot_y), math.sin(rot_y)
        verts = [(cx + x * c - y * s, cy + x * s + y * c, cz + z) for x, y, z in corners]
        faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
        self.add_mesh(verts, faces, slot)

    def cylinder(self, base, height, radius, slot, sides=12, top_radius=None):
        top_radius = radius if top_radius is None else top_radius
        bx, by, bz = base
        verts = []
        for ring, (r, z) in enumerate(((radius, bz), (top_radius, bz + height))):
            for i in range(sides):
                a = 2 * math.pi * i / sides
                verts.append((bx + math.cos(a) * r, by + math.sin(a) * r, z))
        faces = [(i, (i + 1) % sides, sides + (i + 1) % sides, sides + i) for i in range(sides)]
        faces.append(tuple(reversed(range(sides))))
        faces.append(tuple(range(sides, 2 * sides)))
        self.add_mesh(verts, faces, slot)

    def tube(self, points, radius, slot, sides=8):
        """Swept circle along a polyline (ridges, hooks, rails)."""
        verts, faces = [], []
        n = len(points)
        for i, p in enumerate(points):
            p = Vector(p)
            d = (Vector(points[min(i + 1, n - 1)]) - Vector(points[max(i - 1, 0)])).normalized()
            up = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
            u = d.cross(up).normalized()
            v = d.cross(u).normalized()
            r = radius(i / (n - 1)) if callable(radius) else radius
            for k in range(sides):
                a = 2 * math.pi * k / sides
                verts.append(tuple(p + (u * math.cos(a) + v * math.sin(a)) * r))
        for i in range(n - 1):
            for k in range(sides):
                a, b = i * sides + k, i * sides + (k + 1) % sides
                faces.append((a, b, b + sides, a + sides))
        faces.append(tuple(reversed(range(sides))))
        faces.append(tuple(range((n - 1) * sides, n * sides)))
        self.add_mesh(verts, faces, slot)

    def finish(self, name):
        mesh = bpy.data.meshes.new(name)
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=0.0005)
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        self.bm.to_mesh(mesh)
        self.bm.free()
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.collection.objects.link(obj)
        for slot in SLOTS:
            obj.data.materials.append(bpy.data.materials[slot])
        return obj


# ----------------------------------------------------------------------------- architecture parts

def plinth(b, w, d, h, tiers=1, stair=True, rail=True):
    """Stepped stone platform with a front stair (front = -Y) and marble balustrade."""
    top = 0.0
    for t in range(tiers):
        inset = t * 0.35
        th = h / tiers
        b.box((0, 0, top + th / 2), (w - inset * 2, d - inset * 2, th), "Stone")
        b.box((0, 0, top + th - 0.03), (w - inset * 2 + 0.08, d - inset * 2 + 0.08, 0.06), "Marble")
        top += th
    if stair:
        steps = max(5, int(h / 0.12))
        sw = min(w * 0.36, 2.2)
        run = 0.26
        for i in range(steps):
            z = h * (i + 1) / steps
            y = -d / 2 - run * (steps - i) + run / 2
            b.box((0, y, z / 2), (sw, run, z), "Marble")
        length = run * steps
        for side in (-1, 1):  # sloped marble stair cheeks
            x = side * (sw / 2 + 0.07)
            b.add_mesh([(x - 0.07, -d / 2, 0), (x + 0.07, -d / 2, 0), (x + 0.07, -d / 2, h + 0.12), (x - 0.07, -d / 2, h + 0.12),
                        (x - 0.07, -d / 2 - length, 0), (x + 0.07, -d / 2 - length, 0), (x + 0.07, -d / 2 - length, 0.14), (x - 0.07, -d / 2 - length, 0.14)],
                       [(0, 1, 2, 3), (5, 4, 7, 6), (1, 5, 6, 2), (4, 0, 3, 7), (3, 2, 6, 7), (0, 4, 5, 1)], "Marble")
    if rail:
        rail_h = 0.42
        inset = (tiers - 1) * 0.35 + 0.12
        x0, y0 = w / 2 - inset, d / 2 - inset
        posts = []
        for x in frange(-x0, x0, 0.62):
            posts += [(x, -y0), (x, y0)]
        for y in frange(-y0, y0, 0.62):
            posts += [(-x0, y), (x0, y)]
        gap = min(w * 0.36, 2.2) / 2 + 0.2 if stair else 0
        for x, y in posts:
            if stair and abs(x) < gap and y < 0:
                continue
            b.box((x, y, h + rail_h / 2), (0.09, 0.09, rail_h), "Marble")
            b.box((x, y, h + rail_h + 0.04), (0.12, 0.12, 0.08), "Marble")
        for sx in (-1, 1):
            b.box((sx * x0, 0, h + rail_h * 0.8), (0.05, y0 * 2, 0.06), "Marble")
        for sy in (-1, 1):
            if stair and sy < 0:
                for side in (-1, 1):
                    span = x0 - gap
                    b.box((side * (gap + span / 2), sy * y0, h + rail_h * 0.8), (span, 0.05, 0.06), "Marble")
            else:
                b.box((0, sy * y0, h + rail_h * 0.8), (x0 * 2, 0.05, 0.06), "Marble")
    return top


def frange(a, b, step):
    n = max(1, int(round((b - a) / step)))
    return [a + (b - a) * i / n for i in range(n + 1)]


def colonnade(b, w, d, z, h, bays_x, bays_y, radius=0.13, pillar="Lacquer", band="Gold"):
    """Columns (red lacquer by default) on stone bases around a rectangle. Returns column XY list."""
    xs = frange(-w / 2, w / 2, w / bays_x)
    ys = frange(-d / 2, d / 2, d / bays_y)
    cols = [(x, y) for x in xs for y in ys if abs(x) > w / 2 - 1e-3 or abs(y) > d / 2 - 1e-3]
    for x, y in cols:
        b.cylinder((x, y, z), 0.12, radius * 1.6, "Stone", sides=10, top_radius=radius * 1.3)
        b.cylinder((x, y, z + 0.12), h - 0.12, radius, pillar, sides=12, top_radius=radius * 0.92)
    # architrave (lacquer beam with gold band) tying the columns
    for sy in (-1, 1):
        b.box((0, sy * d / 2, z + h - 0.12), (w + 0.2, 0.18, 0.24), pillar)
        b.box((0, sy * d / 2, z + h - 0.27), (w + 0.1, 0.16, 0.06), band)
    for sx in (-1, 1):
        b.box((sx * w / 2, 0, z + h - 0.12), (0.18, d + 0.2, 0.24), pillar)
        b.box((sx * w / 2, 0, z + h - 0.27), (0.16, d + 0.1, 0.06), band)
    return xs, ys


def walls(b, w, d, z, h, xs, ys, front_doors=True, inset=0.32):
    """Plaster walls set behind the columns, with lattice door panels on the front."""
    wi, di = w - inset * 2, d - inset * 2
    t = 0.12
    b.box((0, di / 2, z + h / 2), (wi, t, h), "Plaster")
    for sx in (-1, 1):
        b.box((sx * wi / 2, 0, z + h / 2), (t, di, h), "Plaster")
        # side lattice window
        b.box((sx * (wi / 2 + 0.01), 0, z + h * 0.6), (0.04, di * 0.4, h * 0.35), "Wood")
        for k in frange(-di * 0.2, di * 0.2, 0.16):
            b.box((sx * (wi / 2 + 0.035), k, z + h * 0.6), (0.03, 0.025, h * 0.35), "Lacquer")
    # front: lattice doors between each column bay
    y = -di / 2
    for i in range(len(xs) - 1):
        x0, x1 = xs[i], xs[i + 1]
        cx, span = (x0 + x1) / 2, (x1 - x0) * (wi / w)
        cx *= wi / w
        b.box((cx, y, z + h * 0.12), (span, t, h * 0.24), "Wood")  # dado
        b.box((cx, y, z + h * 0.62), (span - 0.04, t * 0.6, h * 0.76), "Paper" if front_doors else "Plaster")
        for gx in frange(cx - span / 2 + 0.06, cx + span / 2 - 0.06, 0.14):
            b.box((gx, y - 0.05, z + h * 0.62), (0.025, 0.03, h * 0.74), "Lacquer")
        for gz in frange(z + h * 0.26, z + h * 0.98, 0.16):
            b.box((cx, y - 0.05, gz), (span - 0.06, 0.03, 0.025), "Lacquer")
        b.box((cx, y - 0.02, z + h * 0.995), (span, t, 0.05), "Wood")


def bracket_band(b, w, d, z, step=0.42):
    """Dougong: stacked bracket blocks under the eaves, gold-capped."""
    for sx, sy, length, horizontal in ((0, -1, w, True), (0, 1, w, True), (-1, 0, d, False), (1, 0, d, False)):
        for t in frange(-length / 2, length / 2, step):
            x = t if horizontal else sx * w / 2
            y = sy * d / 2 if horizontal else t
            ox = 0 if horizontal else sx * 0.1
            oy = sy * 0.1 if horizontal else 0
            b.box((x, y, z + 0.06), (0.16, 0.16, 0.12), "Wood")
            b.box((x + ox, y + oy, z + 0.17), (0.3 if horizontal else 0.14, 0.14 if horizontal else 0.3, 0.1), "Lacquer")
            b.box((x + ox * 2, y + oy * 2, z + 0.27), (0.18, 0.18, 0.07), "RoofTrim")


def roof_height(x, y, hw, hd, ridge_half, rise, sag=1.55, lift=0.32):
    """Curved hip roof surface. hw/hd = half eave extents, ridge_half = half ridge length."""
    tz = abs(y) / hd
    tx = max(0.0, abs(x) - ridge_half) / max(1e-4, hw - ridge_half)
    m = min(1.0, max(tz, tx))
    z = rise * (1 - m) ** sag
    corner = (abs(x) / hw) ** 6 * (abs(y) / hd) ** 6
    edge = max(tz, tx) ** 8
    return z + lift * (corner * 1.2 + edge * 0.3)


def hip_roof(b, w, d, z, rise, overhang=0.7, ridge_ratio=0.45, thickness=0.14, ornaments=True, n=34,
             skirt=None, lanterns=True, ribs=0.24, roof="Roof", trim="RoofTrim", lift=0.32):
    """skirt=(w2, d2): lower eave of a double roof; ridges start at the upper storey's corners."""
    """Concave hip roof with thickness, ridge caps, hip ridges with upturned tips, and ridge beasts."""
    hw, hd = w / 2 + overhang, d / 2 + overhang
    ks = min(1.0, max(0.3, hd / 1.5))  # detail scale: small roofs get proportionally smaller trims
    lift *= ks
    # Ridge length: what remains after the hips rise from both short ends, plus a share of the width.
    ridge_half = max(0.05, (hw - hd) + hd * ridge_ratio * 0.4) if w >= d else 0.05
    verts, faces = [], []
    for j in range(n + 1):
        for i in range(n + 1):
            x = -hw + 2 * hw * i / n
            y = -hd + 2 * hd * j / n
            verts.append((x, y, z + roof_height(x, y, hw, hd, ridge_half, rise, lift=lift)))
    for j in range(n):
        for i in range(n):
            a = j * (n + 1) + i
            faces.append((a, a + 1, a + n + 2, a + n + 1))
    count = len(verts)
    verts += [(x, y, zz - thickness) for x, y, zz in verts]  # underside
    faces += [tuple(reversed([k + count for k in f])) for f in faces[: n * n]]
    rim = [j * (n + 1) for j in range(n + 1)] + [n * (n + 1) + i for i in range(1, n + 1)] + \
          [j * (n + 1) + n for j in range(n - 1, -1, -1)] + [i for i in range(n - 1, 0, -1)]
    for k in range(len(rim)):
        a, c = rim[k], rim[(k + 1) % len(rim)]
        faces.append((a, c, c + count, a + count))
    b.add_mesh(verts, faces, roof)

    surface = lambda x, y: z + roof_height(x, y, hw, hd, ridge_half, rise, lift=lift)
    if ribs:
        tile_ribs(b, hw, hd, ridge_half, surface, ribs, skirt, trim)
    top = z + rise + thickness * 0.5
    if skirt is None:
        b.tube([(-ridge_half - 0.1 * ks, 0, top), (ridge_half + 0.1 * ks, 0, top)], 0.11 * ks, trim, sides=10)
        b.box((0, 0, top - 0.02 * ks), (ridge_half * 2 + 0.2 * ks, 0.16 * ks, 0.18 * ks), trim)
    for sx in (-1, 1):
        for sy in (-1, 1):
            pts = []
            start = 0.0
            if skirt is not None:  # begin where the diagonal leaves the upper storey
                start = max(skirt[0] / 2 / hw, skirt[1] / 2 / hd) * 0.98
            for s in range(13):
                t = start + (1 - start) * s / 12
                x = sx * (ridge_half + (hw - ridge_half) * t * 0.97) if skirt is None else sx * hw * t * 0.97
                y = sy * hd * t * 0.97
                pts.append((x, y, surface(x, y) + 0.09 * ks))
            tip = Vector(pts[-1])
            pts.append(tuple(tip + Vector((sx * 0.10, sy * 0.10, 0.12)) * ks))
            b.tube(pts, lambda t: 0.08 * ks * (1 - 0.35 * t), trim, sides=8)
            if ornaments:
                b.cylinder(tuple(tip + Vector((sx * 0.12, sy * 0.12, 0.1)) * ks), 0.12 * ks, 0.05 * ks, "Gold", sides=6, top_radius=0.02 * ks)
    if lanterns:  # a pair hanging from the front eave, flanking the entrance
        for sx in (-1, 1):
            x, y = sx * w * 0.3, -(d / 2 + overhang * 0.55)
            lantern(b, (x, y, surface(x, y) - thickness - 0.04))
    if ornaments and skirt is None:
        for sx in (-1, 1):  # chiwen: curling ridge-end beasts
            base = Vector((sx * (ridge_half + 0.05), 0, top))
            curl = [(base.x, 0, base.z + (0.05 + 0.055 * i) * ks) for i in range(6)]
            curl += [(base.x - sx * 0.11 * ks * (1 - math.cos(a)), 0, base.z + (0.33 + 0.11 * math.sin(a)) * ks) for a in (0.6, 1.2, 1.8, 2.4, 3.0)]
            b.tube(curl, lambda t: 0.075 * ks * (1 - 0.45 * t), trim, sides=8)
            b.box((base.x - sx * 0.2 * ks, 0, base.z + 0.3 * ks), (0.05 * ks, 0.05 * ks, 0.1 * ks), "Gold")
        b.cylinder((0, 0, top + 0.08 * ks), 0.18 * ks, 0.12 * ks, "Gold", sides=10, top_radius=0.06 * ks)
        b.cylinder((0, 0, top + 0.26 * ks), 0.22 * ks, 0.05 * ks, "Gold", sides=8, top_radius=0.01 * ks)
    return top


def tile_ribs(b, hw, hd, ridge_half, surface, spacing, skirt, trim):
    """Round-tile (tongwa) ribs following the slope of each roof face, clipped at the hips."""
    def inside_upper(x, y):
        return skirt is not None and abs(x) < skirt[0] / 2 + 0.05 and abs(y) < skirt[1] / 2 + 0.05
    def face_of(x, y):
        tz = abs(y) / hd
        tx = max(0.0, abs(x) - ridge_half) / max(1e-4, hw - ridge_half)
        return "y" if tz >= tx else "x"
    lines = []
    for x in frange(-hw + spacing, hw - spacing, spacing):
        for sy in (-1, 1):
            lines.append([(x, sy * hd * k / 14) for k in range(15)])
    for y in frange(-hd + spacing, hd - spacing, spacing):
        for sx in (-1, 1):
            lines.append([(sx * (ridge_half + (hw - ridge_half) * k / 14), y) for k in range(15)])
    for line in lines:
        axis = "y" if line[0][0] == line[-1][0] else "x"
        run = [(x, y, surface(x, y) + 0.035) for x, y in line if face_of(x, y) == axis and not inside_upper(x, y)]
        if len(run) >= 3:
            b.tube(run, 0.034, trim, sides=5)


def lantern(b, at):
    x, y, z = at
    b.tube([(x, y, z + 0.3), (x, y, z)], 0.012, "Gold", sides=4)
    b.cylinder((x, y, z - 0.04), 0.04, 0.09, "Gold", sides=8)
    b.cylinder((x, y, z - 0.34), 0.30, 0.14, "Paper", sides=10, top_radius=0.14)
    b.cylinder((x, y, z - 0.38), 0.04, 0.09, "Gold", sides=8)


def name_board(b, at, width):
    x, y, z = at
    b.box((x, y, z), (width, 0.06, 0.34), "Lacquer")
    b.box((x, y - 0.035, z), (width - 0.1, 0.02, 0.24), "Gold")


# ----------------------------------------------------------------------------- buildings

def build_hall():
    """Chánh Điện: double-eave grand hall on a two-tier terrace (footprint 4x4)."""
    b = Builder()
    top = plinth(b, 4.6, 4.2, 0.62, tiers=2)
    xs, ys = colonnade(b, 3.6, 2.8, top, 1.55, bays_x=5, bays_y=3)
    walls(b, 3.6, 2.8, top, 1.43, xs, ys)
    bracket_band(b, 3.6, 2.8, top + 1.55)
    hip_roof(b, 3.6, 2.8, top + 1.85, rise=0.55, overhang=0.75, ornaments=True, n=30, skirt=(2.6, 1.9))
    # upper storey
    z2 = top + 2.1
    b.box((0, 0, z2 + 0.45), (2.6, 1.9, 0.9), "Plaster")
    xs2, _ = colonnade(b, 2.6, 1.9, z2, 0.9, bays_x=3, bays_y=2, radius=0.1)
    bracket_band(b, 2.6, 1.9, z2 + 0.9, step=0.38)
    name_board(b, (0, -1.02, z2 + 0.62), 0.9)
    hip_roof(b, 2.6, 1.9, z2 + 1.18, rise=1.05, overhang=0.8, n=34, lanterns=False)
    incense_ding(b, (0, -3.3, 0), 0.3)
    return b.finish("hall")


# ----------------------------------------------------------------------------- props

def incense_ding(b, at, r):
    """Bronze tripod incense burner (ding) with handles and a glowing ember."""
    x, y, z = at
    for k in range(3):
        a = k * 2 * math.pi / 3 + math.pi / 2
        b.cylinder((x + r * 0.6 * math.cos(a), y + r * 0.6 * math.sin(a), z), r * 0.9, r * 0.1, "Gold", sides=6, top_radius=r * 0.07)
    b.cylinder((x, y, z + r * 0.85), r * 0.35, r * 0.75, "Gold", sides=14, top_radius=r)
    b.cylinder((x, y, z + r * 1.2), r * 0.12, r * 1.05, "Gold", sides=14, top_radius=r * 1.05)
    b.cylinder((x, y, z + r * 1.3), r * 0.04, r * 0.85, "Glow", sides=12)
    for sx in (-1, 1):
        b.tube([(x + sx * r * 0.7, y, z + r * 1.3), (x + sx * r * 0.75, y, z + r * 1.7), (x + sx * r * 0.45, y, z + r * 1.75),
                (x + sx * r * 0.4, y, z + r * 1.3)], r * 0.07, "Gold", sides=6)


def crystal(b, at, h, r, tilt=(0.0, 0.0)):
    """Six-sided spirit crystal (Glow); negative h points down."""
    x, y, z = at
    tx, ty = tilt
    sides = 6
    verts = [(x, y, z)]
    for i in range(sides):
        a = 2 * math.pi * i / sides
        verts.append((x + r * math.cos(a) + tx * h * 0.35, y + r * math.sin(a) + ty * h * 0.35, z + h * 0.35))
    verts.append((x + tx * h, y + ty * h, z + h))
    faces = [(0, 1 + (i + 1) % sides, 1 + i) for i in range(sides)] + [(1 + i, 1 + (i + 1) % sides, sides + 1) for i in range(sides)]
    b.add_mesh(verts, faces, "Glow")


def fence(b, w, d, h=0.38, slot="Wood", gap_front=0.0):
    for x in frange(-w / 2, w / 2, 0.45):
        for sy in (-1, 1):
            if sy < 0 and abs(x) < gap_front / 2:
                continue
            b.box((x, sy * d / 2, h / 2), (0.06, 0.06, h), slot)
    for y in frange(-d / 2, d / 2, 0.45):
        for sx in (-1, 1):
            b.box((sx * w / 2, y, h / 2), (0.06, 0.06, h), slot)
    for zz in (h * 0.45, h * 0.85):
        for sy in (-1, 1):
            if sy < 0 and gap_front:
                span = w / 2 - gap_front / 2
                for side in (-1, 1):
                    b.box((side * (gap_front / 2 + span / 2), sy * d / 2, zz), (span, 0.04, 0.04), slot)
            else:
                b.box((0, sy * d / 2, zz), (w, 0.04, 0.04), slot)
        for sx in (-1, 1):
            b.box((sx * w / 2, 0, zz), (0.04, d, 0.04), slot)


def crop_rows(b, w, d, rows=5):
    b.box((0, 0, 0.06), (w, d, 0.12), "Soil")
    for i in range(rows):
        y = -d / 2 + d * (i + 0.5) / rows
        b.box((0, y, 0.16), (w * 0.9, d / rows * 0.45, 0.1), "Soil")
        for x in frange(-w * 0.42, w * 0.42, 0.22):
            b.cylinder((x, y, 0.2), 0.16, 0.07, "Leaf", sides=5, top_radius=0.02)


# ----------------------------------------------------------------------------- building archetypes

def small_hall(b, w, d, col_h=1.05, rise=0.6, bays=3, plinth_h=0.3, overhang=0.5, rustic=False, doors=True,
               lanterns=True, ornaments=True, grey=False):
    """Single-eave hall used by dwellings, storehouses and workshops."""
    top = plinth(b, w + 0.3, d + 0.3, plinth_h, tiers=1, rail=not rustic) if plinth_h > 0 else 0.0
    xs, ys = colonnade(b, w, d, top, col_h, bays_x=bays, bays_y=2, radius=0.08 if rustic else 0.09,
                         **(dict(pillar="Wood", band="Wood") if rustic else {}))
    walls(b, w, d, top, col_h - 0.1, xs, ys, front_doors=doors, inset=0.18)
    if not rustic:
        bracket_band(b, w, d, top + col_h, step=0.4)
    zr = top + col_h + (0.05 if rustic else 0.32)
    palette = dict(roof="RoofGrey", trim="TrimGrey") if grey or rustic else {}
    hip_roof(b, w, d, zr, rise=rise, overhang=overhang, n=26, lanterns=lanterns, ornaments=ornaments,
             ribs=0.24 if rustic else 0.2, lift=0.12 if rustic else (0.18 if grey else 0.26), **palette)
    return top


def pagoda(b, base, levels, storey_h=0.9, shrink=0.78, plinth_h=0.4):
    """Multi-storey pavilion tower (Tàng Kinh Các / watchtower)."""
    top = plinth(b, base + 0.5, base + 0.5, plinth_h, tiers=1)
    size, z = base, top
    for level in range(levels):
        xs, ys = colonnade(b, size, size, z, storey_h, bays_x=3, bays_y=3, radius=0.08)
        walls(b, size, size, z, storey_h - 0.08, xs, ys, front_doors=True, inset=0.16)
        bracket_band(b, size, size, z + storey_h, step=0.36)
        last = level == levels - 1
        hip_roof(b, size, size, z + storey_h + 0.28, rise=0.85 if last else 0.32, overhang=0.42, n=22,
                 ornaments=last, lanterns=level == 0, ribs=0.2, skirt=None if last else (size * shrink, size * shrink))
        if last:
            b.cylinder((0, 0, z + storey_h + 1.2), 0.5, 0.05, "Gold", sides=8, top_radius=0.02)
            for k in range(3):
                b.cylinder((0, 0, z + storey_h + 1.18 + k * 0.14), 0.04, 0.09 - k * 0.02, "Gold", sides=10)
        if level == 0:
            name_board(b, (0, -size / 2 - 0.08, z + storey_h - 0.25), size * 0.4)
        z += storey_h + 0.3
        size *= shrink


def build_scripture():
    b = Builder()
    pagoda(b, 2.0, 3)
    return b.finish("scripture")


def build_tower():
    b = Builder()
    pagoda(b, 1.3, 2, storey_h=0.95, shrink=0.85, plinth_h=0.25)
    return b.finish("tower")


def build_house():
    b = Builder()
    small_hall(b, 1.5, 1.3, col_h=1.15, rise=0.48, bays=3, plinth_h=0.2, overhang=0.38, lanterns=False, grey=True)
    return b.finish("house")


def build_store():
    b = Builder()
    small_hall(b, 1.6, 1.35, col_h=1.2, rise=0.42, bays=3, plinth_h=0.35, overhang=0.36, doors=False, lanterns=False,
               ornaments=False, grey=True)
    for x, y in ((-0.95, -0.85), (-0.7, -0.95), (0.95, -0.8)):  # crates
        b.box((x, y, 0.12), (0.24, 0.24, 0.24), "Wood")
    return b.finish("store")


def build_mission():
    b = Builder()
    small_hall(b, 1.6, 1.3, col_h=1.3, rise=0.6, bays=3, plinth_h=0.35, overhang=0.42)
    name_board(b, (0, -0.85, 1.25), 0.7)
    for sx in (-1, 1):  # banner poles
        b.cylinder((sx * 1.05, -1.0, 0), 1.9, 0.03, "Wood", sides=6)
        b.box((sx * 1.18, -1.0, 1.6), (0.24, 0.02, 0.55), "Paper")
    return b.finish("mission")


def build_alchemy():
    b = Builder()
    small_hall(b, 2.0, 1.6, col_h=1.3, rise=0.6, bays=3, plinth_h=0.3, overhang=0.45)
    incense_ding(b, (0, -1.45, 0), 0.32)
    b.cylinder((0.75, 0.55, 2.0), 0.6, 0.1, "Stone", sides=8)  # chimney through the roof
    return b.finish("alchemy")


def build_forge():
    b = Builder()
    small_hall(b, 2.0, 1.6, col_h=1.3, rise=0.5, bays=3, plinth_h=0.25, overhang=0.42, lanterns=False, grey=True)
    b.box((0.85, -1.3, 0.25), (0.5, 0.5, 0.5), "Stone")  # furnace
    b.box((0.85, -1.3, 0.48), (0.3, 0.3, 0.06), "Glow")
    b.box((-0.8, -1.3, 0.2), (0.35, 0.18, 0.12), "Stone")  # anvil
    b.cylinder((-0.8, -1.3, 0), 0.14, 0.1, "Wood", sides=8)
    b.cylinder((0.85, 0.5, 1.9), 0.9, 0.11, "Stone", sides=8)
    return b.finish("forge")


def build_barracks():
    """Diễn Võ Trường: paved training ground with a rear pavilion, weapon racks and a war drum."""
    b = Builder()
    b.box((0, 0, 0.05), (2.9, 2.9, 0.1), "Stone")
    b.box((0, 0, 0.11), (2.3, 2.3, 0.02), "Marble")
    pav = Builder()  # pavilion built at origin, then shifted to the rear edge
    colonnade(pav, 1.4, 0.9, 0.1, 1.0, bays_x=2, bays_y=1, radius=0.07)
    hip_roof(pav, 1.4, 0.9, 1.35, rise=0.45, overhang=0.35, n=20, lanterns=False, ribs=0.2)
    bmesh.ops.translate(pav.bm, verts=pav.bm.verts, vec=(0, 0.85, 0))
    pav_mesh = bpy.data.meshes.new("tmp")
    pav.bm.to_mesh(pav_mesh)
    pav.bm.free()
    b.bm.from_mesh(pav_mesh)
    for sx in (-1, 1):  # weapon racks
        b.box((sx * 1.2, -0.6, 0.45), (0.06, 0.8, 0.06), "Wood")
        for k in range(4):
            b.cylinder((sx * 1.2, -0.9 + k * 0.2, 0.1), 0.9, 0.015, "Wood", sides=4)
            b.cylinder((sx * 1.2, -0.9 + k * 0.2, 1.0), 0.12, 0.03, "Gold", sides=4, top_radius=0.0)
    b.cylinder((0, -1.15, 0.1), 0.4, 0.22, "Lacquer", sides=14)  # war drum
    return b.finish("barracks")


def build_meditation():
    """Tụ Linh Trận: circular dais with glowing formation rings, crystal pillars and a floating crystal."""
    b = Builder()
    b.cylinder((0, 0, 0), 0.18, 1.45, "Stone", sides=32)
    b.cylinder((0, 0, 0.18), 0.12, 1.15, "Marble", sides=32)
    for r in (1.0, 0.7, 0.42):
        ring = [(r * math.cos(a * math.pi / 24), r * math.sin(a * math.pi / 24), 0.315) for a in range(49)]
        b.tube(ring, 0.025, "Glow", sides=5)
    for k in range(8):  # trigram marks
        a = k * math.pi / 4
        b.box((0.85 * math.cos(a), 0.85 * math.sin(a), 0.31), (0.14, 0.04, 0.01), "Glow", rot_y=a)
    for k in range(4):
        a = k * math.pi / 2 + math.pi / 4
        x, y = 1.25 * math.cos(a), 1.25 * math.sin(a)
        b.cylinder((x, y, 0.18), 0.95, 0.07, "Marble", sides=8)
        b.cylinder((x, y, 1.13), 0.08, 0.11, "Gold", sides=8)
        crystal(b, (x, y, 1.2), 0.3, 0.06)
    crystal(b, (0, 0, 0.9), 0.75, 0.16)
    crystal(b, (0, 0, 0.9), -0.3, 0.12)
    return b.finish("meditation")


def build_spiritmine():
    b = Builder()
    for (x, y, sx, sy, sz) in ((0, 0.2, 1.3, 1.1, 0.75), (-0.5, -0.2, 0.7, 0.7, 0.5), (0.55, -0.3, 0.6, 0.6, 0.4)):
        b.box((x, y, sz / 2), (sx, sy, sz), "Stone", rot_y=x)
    for (x, y, h, tx, ty) in ((0.1, -0.35, 0.7, 0.1, -0.2), (-0.35, -0.55, 0.45, -0.3, -0.1), (0.5, -0.65, 0.35, 0.2, -0.2), (0.0, 0.3, 0.6, 0, 0.1)):
        crystal(b, (x, y, 0.35), h, 0.09, (tx, ty))
    b.box((-0.75, -0.85, 0.08), (0.4, 0.25, 0.16), "Wood")  # ore cart
    return b.finish("spiritmine")


def build_farm():
    b = Builder()
    crop_rows(b, 2.6, 2.6, rows=6)
    fence(b, 2.9, 2.9, gap_front=0.6)
    return b.finish("farm")


def build_herbgarden():
    b = Builder()
    crop_rows(b, 2.5, 2.5, rows=4)
    for (x, y) in ((-0.8, -0.8), (0.8, 0.8)):
        crystal(b, (x, y, 0.2), 0.25, 0.05)
    fence(b, 2.8, 2.8, h=0.3, slot="Marble", gap_front=0.6)
    return b.finish("herbgarden")


def build_wall():
    b = Builder()
    b.box((0, 0, 0.12), (1.0, 0.42, 0.24), "Stone")
    b.box((0, 0, 0.6), (1.0, 0.3, 0.75), "Plaster")
    hip_roof(b, 1.0, 0.3, 0.98, rise=0.16, overhang=0.12, n=8, ornaments=False, lanterns=False, ribs=None, thickness=0.06,
             roof="RoofGrey", trim="TrimGrey", lift=0.05)
    return b.finish("wall")


def build_gate():
    """Paifang: three-bay ceremonial gate."""
    b = Builder()
    for x in (-0.95, -0.35, 0.35, 0.95):
        b.box((x, 0, 0.12), (0.3, 0.3, 0.24), "Stone")
        b.cylinder((x, 0, 0.24), 1.55 if abs(x) < 0.5 else 1.2, 0.08, "Lacquer", sides=10)
    b.box((0, 0, 1.75), (1.0, 0.16, 0.18), "Lacquer")
    b.box((0, 0, 1.6), (0.95, 0.14, 0.06), "Gold")
    name_board(b, (0, -0.09, 1.5), 0.45)
    hip_roof(b, 0.9, 0.24, 1.9, rise=0.3, overhang=0.28, n=16, lanterns=False, ribs=0.15, thickness=0.07)
    for sx in (-1, 1):
        b.box((sx * 0.65, 0, 1.38), (0.75, 0.14, 0.12), "Lacquer")
        b.box((sx * 0.65, 0, 1.26), (0.7, 0.12, 0.05), "Gold")
        side = Builder()
        hip_roof(side, 0.6, 0.2, 1.5, rise=0.2, overhang=0.2, n=12, ornaments=False, lanterns=False, ribs=0.15, thickness=0.06)
        bmesh.ops.translate(side.bm, verts=side.bm.verts, vec=(sx * 0.65, 0, 0))
        tmp = bpy.data.meshes.new("tmp")
        side.bm.to_mesh(tmp)
        side.bm.free()
        b.bm.from_mesh(tmp)
    return b.finish("gate")


def build_well():
    b = Builder()
    b.cylinder((0, 0, 0), 0.35, 0.45, "Stone", sides=16)
    b.cylinder((0, 0, 0.35), 0.02, 0.36, "Glow", sides=16)
    for sx in (-1, 1):
        b.box((sx * 0.5, 0, 0.6), (0.08, 0.08, 1.2), "Wood")
    b.box((0, 0, 0.95), (1.1, 0.06, 0.06), "Wood")
    hip_roof(b, 1.0, 0.6, 1.25, rise=0.28, overhang=0.18, n=14, ornaments=False, lanterns=False, ribs=0.16, thickness=0.06)
    return b.finish("well")


def build_hearth():
    b = Builder()
    b.box((0, 0, 0.06), (1.6, 1.6, 0.12), "Stone")
    colonnade(b, 1.2, 1.2, 0.12, 0.95, bays_x=1, bays_y=1, radius=0.07)
    hip_roof(b, 1.2, 1.2, 1.3, rise=0.55, overhang=0.32, n=18, lanterns=True, ribs=0.2)
    b.cylinder((0, 0, 0.12), 0.25, 0.3, "Stone", sides=10, top_radius=0.34)
    b.cylinder((0, 0, 0.37), 0.06, 0.28, "Glow", sides=10)
    return b.finish("hearth")


def rustic_hut(name, prop):
    def build():
        b = Builder()
        small_hall(b, 1.3, 1.1, col_h=1.0, rise=0.38, bays=2, plinth_h=0.0, overhang=0.3, rustic=True, lanterns=False,
                   ornaments=False)
        prop(b)
        return b.finish(name)
    return build


def _logs(b):
    for k in range(3):
        y, z = -0.6 + k * 0.18, 0.09 + (k % 2) * 0.15
        b.tube([(-1.15, y, z), (-0.75, y, z)], 0.08, "Wood", sides=8)


def _stones(b):
    for (x, y, s) in ((-0.95, -0.6, 0.22), (-0.8, -0.85, 0.16), (0.95, -0.7, 0.2)):
        b.box((x, y, s / 2), (s, s, s), "Stone", rot_y=x * 3)


def _pelts(b):
    b.box((-0.95, -0.7, 0.4), (0.04, 0.5, 0.8), "Wood")
    b.box((-0.92, -0.7, 0.45), (0.02, 0.35, 0.45), "Soil")


def _saplings(b):
    for x in (-0.9, -0.7, 0.8):
        b.cylinder((x, -0.75, 0), 0.12, 0.09, "Soil", sides=8)
        b.cylinder((x, -0.75, 0.12), 0.3, 0.07, "Leaf", sides=6, top_radius=0.0)


def build_prop_lantern():
    """Stone garden lantern (scenery prop, not a server building type)."""
    b = Builder()
    b.box((0, 0, 0.06), (0.42, 0.42, 0.12), "Stone")
    b.cylinder((0, 0, 0.12), 0.55, 0.08, "Stone", sides=8, top_radius=0.065)
    b.box((0, 0, 0.72), (0.34, 0.34, 0.1), "Stone")
    b.box((0, 0, 0.9), (0.26, 0.26, 0.26), "Stone")
    b.box((0, 0, 0.9), (0.27, 0.16, 0.16), "Paper")
    b.box((0, 0, 0.9), (0.16, 0.27, 0.16), "Paper")
    b.cylinder((0, 0, 1.03), 0.16, 0.3, "Stone", sides=4, top_radius=0.04)
    b.cylinder((0, 0, 1.19), 0.08, 0.04, "Stone", sides=8, top_radius=0.0)
    return b.finish("prop_lantern")


def build_prop_bamboo():
    """Bamboo grove clump: jointed culms with drooping leaf sprays (scenery prop)."""
    import random
    rnd = random.Random(7)
    b = Builder()
    for i in range(11):
        a, r = rnd.uniform(0, math.tau), rnd.uniform(0, 0.55)
        x, y = math.cos(a) * r, math.sin(a) * r
        h = rnd.uniform(2.2, 3.4)
        lean = (rnd.uniform(-0.25, 0.25), rnd.uniform(-0.25, 0.25))
        pts = [(x + lean[0] * (t / 6) ** 2, y + lean[1] * (t / 6) ** 2, h * t / 6) for t in range(7)]
        b.tube(pts, 0.035, "Bamboo", sides=6)
        for t in range(1, 6):  # culm nodes
            px, py, pz = pts[t]
            b.cylinder((px, py, pz - 0.015), 0.03, 0.043, "Bamboo", sides=6)
        for k in range(4):  # leaf sprays near the top, drooping outward
            px, py, pz = pts[4 + k % 3]
            da = rnd.uniform(0, math.tau)
            for leaf in range(3):
                la = da + leaf * 0.5
                tip = (px + math.cos(la) * 0.45, py + math.sin(la) * 0.45, pz - 0.18)
                mid = (px + math.cos(la) * 0.22, py + math.sin(la) * 0.22, pz + 0.04)
                b.tube([(px, py, pz), mid, tip], lambda t: 0.045 * math.sin(math.pi * (0.15 + 0.85 * t)) + 0.004, "Leaf", sides=3)
    return b.finish("prop_bamboo")


BUILDERS = {
    "prop_bamboo": build_prop_bamboo,
    "prop_lantern": build_prop_lantern,
    "hall": build_hall, "scripture": build_scripture, "tower": build_tower, "house": build_house,
    "store": build_store, "mission": build_mission, "alchemy": build_alchemy, "forge": build_forge,
    "barracks": build_barracks, "meditation": build_meditation, "spiritmine": build_spiritmine,
    "farm": build_farm, "herbgarden": build_herbgarden, "wall": build_wall, "gate": build_gate,
    "well": build_well, "hearth": build_hearth,
    "wood": rustic_hut("wood", _logs), "stone": rustic_hut("stone", _stones),
    "hunter": rustic_hut("hunter", _pelts), "forester": rustic_hut("forester", _saplings),
}


# ----------------------------------------------------------------------------- export / preview

def uv_and_export(obj, path):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=2.0, scale_to_bounds=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(35))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             mesh_smooth_type="FACE", add_leaf_bones=False)
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print(f"EXPORTED {os.path.basename(path)} tris={tris}")


def render_preview(path):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x, scene.render.resolution_y = 1280, 900
    world = bpy.data.worlds.new("sky")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.74, 0.78, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.9
    scene.world = world
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = 3.5
    sun.rotation_euler = (math.radians(50), 0, math.radians(-35))
    bpy.context.collection.objects.link(sun)
    bpy.ops.mesh.primitive_plane_add(size=30)
    ground = bpy.context.active_object
    gm = bpy.data.materials.new("ground")
    gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.35, 0.45, 0.3, 1)
    ground.data.materials.append(gm)
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.lens = 42
    cam.location = (7.5, -9.5, 6.8)
    direction = Vector((0, 0, 1.8)) - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.collection.objects.link(cam)
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("PREVIEW", path)


def render_sheet(types, path):
    """All buildings side by side in one render, for reviewing the kit as a set."""
    reset_scene()
    cols = 6
    for i, t in enumerate(types):
        obj = BUILDERS[t]()
        obj.location = ((i % cols) * 5.2 - cols * 2.6 + 2.6, (i // cols) * 5.5, 0)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x, scene.render.resolution_y = 1920, 1080
    world = bpy.data.worlds.new("sky")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.74, 0.78, 1)
    scene.world = world
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = 3.5
    sun.rotation_euler = (math.radians(50), 0, math.radians(-35))
    bpy.context.collection.objects.link(sun)
    rows = (len(types) + cols - 1) // cols
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 33
    target = Vector((0, rows * 5.5 / 2 - 2.75, 0.8))
    cam.location = target + Vector((14, -20, 17))
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.collection.objects.link(cam)
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("SHEET", path)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    sheet = None
    if "--sheet" in argv:
        i = argv.index("--sheet")
        sheet = argv[i + 1]
        argv = argv[:i] + argv[i + 2:]
    preview = None
    if "--preview" in argv:
        i = argv.index("--preview")
        preview = argv[i + 1]
        argv = argv[:i] + argv[i + 2:]
    out_dir = argv[0]
    types = argv[1:] or list(BUILDERS)
    for t in types:
        reset_scene()
        obj = BUILDERS[t]()
        uv_and_export(obj, os.path.join(out_dir, f"SonMon_{t}.fbx"))
        if preview:
            render_preview(preview.replace("{type}", t))
    if sheet:
        render_sheet(types, sheet)


if __name__ == "__main__":
    main()
