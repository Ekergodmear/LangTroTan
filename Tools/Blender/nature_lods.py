"""Generate decimated LOD1 meshes for the Quaternius nature models used by the Sơn Môn slice.

Usage:
    blender -b --factory-startup -P Tools/Blender/nature_lods.py -- <models_dir> <out_dir> <ratio> name [name ...]

Reads <models_dir>/<name>.fbx (Quaternius "FBX (Unity)"), applies a collapse decimation and writes
<out_dir>/<name>_LOD1.fbx with the same axis settings as the source, so the LOD sits on the LOD0 pivot.
"""
import os
import sys

import bpy

argv = sys.argv[sys.argv.index("--") + 1:]
models_dir, out_dir, ratio, names = argv[0], argv[1], float(argv[2]), argv[3:]
os.makedirs(out_dir, exist_ok=True)

for name in names:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(models_dir, name + ".fbx"))
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    before = after = 0
    for obj in meshes:
        before += sum(len(p.vertices) - 2 for p in obj.data.polygons)
        mod = obj.modifiers.new("lod", "DECIMATE")
        mod.ratio = ratio
        mod.use_collapse_triangulate = True
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
        after += sum(len(p.vertices) - 2 for p in obj.data.polygons)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(filepath=os.path.join(out_dir, name + "_LOD1.fbx"), use_selection=True,
                             apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, add_leaf_bones=False)
    print(f"LOD {name} {before} -> {after}")
