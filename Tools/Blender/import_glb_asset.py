"""Convert an external GLB (e.g. a ForgeGUI-generated building) into a Unity-ready FBX + URP textures.

The source file is only read. Output:
    <out_dir>/<name>.fbx              mesh, origin at footprint centre, base on the ground, Y-up baked
    <out_dir>/<name>_albedo.png       glTF base colour
    <out_dir>/<name>_normal.png       tangent-space normal (OpenGL)
    <out_dir>/<name>_mask.png         URP Lit metallic map: R = metallic, A = smoothness (1 - roughness)

Usage:
    blender -b --factory-startup -P Tools/Blender/import_glb_asset.py -- <source.glb> <out_dir> <name>
"""
import os
import sys

import bpy
import numpy as np
from mathutils import Matrix, Vector

src, out_dir, name = sys.argv[sys.argv.index("--") + 1:]
out_dir = os.path.abspath(out_dir)  # Blender resolves relative image paths against the .blend, not the CWD
os.makedirs(out_dir, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]

# Bake object transforms, then move the origin to the footprint centre with the base at z = 0.
bpy.ops.object.select_all(action="DESELECT")
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
if len(meshes) > 1:
    bpy.ops.object.join()
obj = bpy.context.view_layer.objects.active
lo = Vector((min(v.co[i] for v in obj.data.vertices) for i in range(3)))
hi = Vector((max(v.co[i] for v in obj.data.vertices) for i in range(3)))
obj.data.transform(Matrix.Translation((-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z)))
print(f"ASSET {name} dims={tuple(round(v, 3) for v in hi - lo)} tris={sum(len(p.vertices) - 2 for p in obj.data.polygons)}")

# Textures: find the images wired into the Principled BSDF.
mat = obj.data.materials[0]
nodes = mat.node_tree.nodes
bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")


def upstream_image(socket):
    for link in mat.node_tree.links:
        if link.to_socket == socket:
            node = link.from_node
            if node.type == "TEX_IMAGE":
                return node.image
            for inp in node.inputs:
                img = upstream_image(inp)
                if img:
                    return img
    return None


def save(image, path, pixels=None):
    img = bpy.data.images.new(os.path.basename(path), image.size[0], image.size[1], alpha=True)
    img.pixels.foreach_set(pixels if pixels is not None else np.array(image.pixels[:], dtype=np.float32))
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()


albedo = upstream_image(bsdf.inputs["Base Color"])
metal_rough = upstream_image(bsdf.inputs["Roughness"])
normal = upstream_image(bsdf.inputs["Normal"])
save(albedo, os.path.join(out_dir, f"{name}_albedo.png"))
save(normal, os.path.join(out_dir, f"{name}_normal.png"))
mr = np.array(metal_rough.pixels[:], dtype=np.float32).reshape(-1, 4)
mask = np.zeros_like(mr)
mask[:, 0] = mr[:, 2]          # metallic (glTF B)
mask[:, 1] = mr[:, 2]
mask[:, 2] = mr[:, 2]
mask[:, 3] = 1.0 - mr[:, 1]    # smoothness = 1 - roughness (glTF G)
save(metal_rough, os.path.join(out_dir, f"{name}_mask.png"), mask.ravel())

obj.data.materials.clear()
obj.data.materials.append(bpy.data.materials.new(name + "_Material"))
obj.name = name
bpy.ops.object.select_all(action="DESELECT")
obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(out_dir, f"{name}.fbx"), use_selection=True,
                         apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
                         bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False)
print("EXPORTED", os.path.join(out_dir, f"{name}.fbx"))
