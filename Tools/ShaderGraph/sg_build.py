"""Reproducible generator for the Tàng Kinh Các Level 6 v4 Shader Graph assets.

Shader Graph has no public scripting API for building graphs, so this writes the .shadergraph (multi-JSON) files
directly. Every node, slot, property and target object is cloned from Unity's own Shader Graph assets in the local
package cache (Library/PackageCache/**/*.shadergraph) and only ids, positions, values and connections are changed,
so the serialized schema always matches the installed Shader Graph version. The resulting graphs open and edit in the
Shader Graph window like hand-made ones; Unity validates them on import (see Assets/Editor/SpiritShaderGraphCheck.cs).

Usage: python Tools/ShaderGraph/sg_build.py   (from the project root; re-run after changing a graph below)
"""
import copy
import glob
import json
import os
import uuid

ROOT = os.getcwd()
OUT = os.path.join(ROOT, "Assets", "Art", "SonMon", "VFX", "L6", "Shaders")


# ---------------------------------------------------------------- template harvest
def read_objects(path):
    text = open(path, encoding="utf-8").read()
    dec, i, out = json.JSONDecoder(), 0, []
    while i < len(text):
        while i < len(text) and text[i].isspace():
            i += 1
        if i >= len(text):
            break
        o, i = dec.raw_decode(text, i)
        out.append(o)
    return out


def harvest():
    t = {}
    for f in sorted(glob.glob(os.path.join(ROOT, "Library", "PackageCache", "**", "*.shadergraph"), recursive=True)):
        try:
            objs = read_objects(f)
        except Exception:
            continue
        by = {o.get("m_ObjectId"): o for o in objs}
        for o in objs:
            ty = o.get("m_Type", "")
            key = ty
            if ty.endswith(".BlockNode"):
                key = ty + "|" + o.get("m_SerializedDescriptor", "")
            elif ty.endswith(".PropertyNode"):
                prop = by.get(o.get("m_Property", {}).get("m_Id"))
                if prop is None:
                    continue
                key = ty + "|" + prop["m_Type"].split(".")[-1]
            elif ty.endswith(".UniversalTarget"):
                key = ty + "|" + str(o.get("m_SurfaceType"))
            if key in t:
                continue
            slots = [by.get(s["m_Id"]) for s in o.get("m_Slots", [])]
            if any(s is None for s in slots):
                continue
            t[key] = {"obj": o, "slots": slots}
    return t


T = harvest()
NS = "UnityEditor.ShaderGraph."


def new_id():
    return uuid.uuid4().hex


class Graph:
    def __init__(self, name):
        self.name = name
        self.objects = []      # serialized objects other than GraphData
        self.nodes = []        # node ids (incl. blocks)
        self.edges = []
        self.props = []
        self.slot_index = {}   # node id -> {display name: slot id}
        self.col = 0
        self.vertex_blocks = []
        self.fragment_blocks = []

    # -- nodes
    def node(self, kind, values=None, x=None, y=None, key=None):
        tpl = T[key or NS + kind]
        o = copy.deepcopy(tpl["obj"])
        o["m_ObjectId"] = new_id()
        self.col += 1
        o["m_DrawState"]["m_Position"]["x"] = x if x is not None else -1400 + (self.col % 8) * 170
        o["m_DrawState"]["m_Position"]["y"] = y if y is not None else -400 + (self.col // 8) * 160
        o["m_PreviewExpanded"] = False
        names, refs = {}, []
        for s in tpl["slots"]:
            s = copy.deepcopy(s)
            s["m_ObjectId"] = new_id()
            names[s["m_DisplayName"]] = s["m_Id"]
            if values and s["m_DisplayName"] in values:
                set_value(s, values[s["m_DisplayName"]])
            self.objects.append(s)
            refs.append({"m_Id": s["m_ObjectId"]})
        o["m_Slots"] = refs
        self.objects.append(o)
        self.nodes.append(o["m_ObjectId"])
        self.slot_index[o["m_ObjectId"]] = names
        return o["m_ObjectId"]

    def block(self, descriptor, vertex):
        nid = self.node("BlockNode", key=NS + "BlockNode|" + descriptor, x=0, y=0)
        (self.vertex_blocks if vertex else self.fragment_blocks).append(nid)
        return nid

    def link(self, out_node, out_slot, in_node, in_slot):
        so = self.slot_index[out_node]
        si = self.slot_index[in_node]
        out_id = so[out_slot] if isinstance(out_slot, str) else out_slot
        in_id = si[in_slot] if isinstance(in_slot, str) else in_slot
        self.edges.append({"m_OutputSlot": {"m_Node": {"m_Id": out_node}, "m_SlotId": out_id},
                           "m_InputSlot": {"m_Node": {"m_Id": in_node}, "m_SlotId": in_id}})

    # -- properties
    def prop(self, kind, display, reference, value):
        tpl = T[NS + "Internal." + kind]
        p = copy.deepcopy(tpl["obj"])
        p["m_ObjectId"] = new_id()
        p["m_Guid"] = {"m_GuidSerialized": str(uuid.uuid4())}
        p["m_Name"] = display
        p["m_RefNameGeneratedByDisplayName"] = display
        p["m_DefaultReferenceName"] = reference
        p["m_OverrideReferenceName"] = reference
        if kind == "Vector1ShaderProperty":
            p["m_Value"] = value
            p["m_FloatType"] = 0
        elif kind == "ColorShaderProperty":
            p["m_Value"] = {"r": value[0], "g": value[1], "b": value[2], "a": value[3]}
            p["m_ColorMode"] = 1 if max(value[:3]) > 1 else 0
        elif kind == "Texture2DShaderProperty":
            p["m_DefaultType"] = 0
            p["isMainTexture"] = reference in ("_MainTex", "_BaseMap")
        self.objects.append(p)
        self.props.append(p["m_ObjectId"])
        # property node reading it
        nid = self.node("PropertyNode", key=NS + "PropertyNode|" + kind)
        node = next(o for o in self.objects if o.get("m_ObjectId") == nid)
        node["m_Property"] = {"m_Id": p["m_ObjectId"]}
        return nid, list(self.slot_index[nid].keys())[0]

    # -- output
    def write(self, alpha_mode, render_face=0):
        sub = copy.deepcopy(T["UnityEditor.Rendering.Universal.ShaderGraph.UniversalUnlitSubTarget"]["obj"])
        sub["m_ObjectId"] = new_id()
        tgt = copy.deepcopy(T["UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget|1"]["obj"])
        tgt["m_ObjectId"] = new_id()
        tgt["m_ActiveSubTarget"] = {"m_Id": sub["m_ObjectId"]}
        tgt.update({"m_SurfaceType": 1, "m_AlphaMode": alpha_mode, "m_RenderFace": render_face, "m_ZWriteControl": 2,
                    "m_ZTestMode": 4, "m_CastShadows": False, "m_ReceiveShadows": False, "m_AllowMaterialOverride": False,
                    "m_AlphaClip": False})
        cat = copy.deepcopy(T[NS + "CategoryData"]["obj"])
        cat["m_ObjectId"] = new_id()
        cat["m_Name"] = ""
        cat["m_ChildObjectList"] = [{"m_Id": p} for p in self.props]
        g = copy.deepcopy(T[NS + "GraphData"]["obj"])
        g.update({
            "m_ObjectId": new_id(),
            "m_Properties": [{"m_Id": p} for p in self.props],
            "m_Keywords": [], "m_Dropdowns": [],
            "m_CategoryData": [{"m_Id": cat["m_ObjectId"]}],
            "m_Nodes": [{"m_Id": n} for n in self.nodes],
            "m_GroupDatas": [], "m_StickyNoteDatas": [],
            "m_Edges": self.edges,
            "m_VertexContext": {"m_Position": {"x": 0.0, "y": -300.0}, "m_Blocks": [{"m_Id": b} for b in self.vertex_blocks]},
            "m_FragmentContext": {"m_Position": {"x": 0.0, "y": 0.0}, "m_Blocks": [{"m_Id": b} for b in self.fragment_blocks]},
            "m_Path": "SonMon/Spirit",
            "m_OutputNode": {"m_Id": ""},
            "m_SubDatas": [],
            "m_ActiveTargets": [{"m_Id": tgt["m_ObjectId"]}],
        })
        g.pop("m_ConcretePrecision", None)
        os.makedirs(OUT, exist_ok=True)
        path = os.path.join(OUT, self.name + ".shadergraph")
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            for o in [g, cat, tgt, sub] + self.objects:
                f.write(json.dumps(o, indent=4) + "\n\n")
        return path


def set_value(slot, v):
    cur = slot.get("m_Value")
    if isinstance(cur, dict) and "e00" in cur:  # dynamic value slot: a scalar fills every component
        for k in cur:
            cur[k] = float(v)
    elif isinstance(cur, dict) and "x" in cur:
        vals = v if isinstance(v, (list, tuple)) else [v] * 4
        for i, k in enumerate(("x", "y", "z", "w")):
            if k in cur:
                cur[k] = float(vals[min(i, len(vals) - 1)])
    else:
        slot["m_Value"] = float(v)


def standard_blocks(g):
    for d in ("VertexDescription.Position", "VertexDescription.Normal", "VertexDescription.Tangent"):
        g.block(d, True)
    return g.block("SurfaceDescription.BaseColor", False), g.block("SurfaceDescription.Alpha", False)


# ---------------------------------------------------------------- A. Spirit Mist (particles)
def spirit_mist():
    g = Graph("SG_SpiritMist")
    base, alpha = standard_blocks(g)
    tex, tex_o = g.prop("Texture2DShaderProperty", "Energy Texture", "_MainTex", None)
    tint, tint_o = g.prop("ColorShaderProperty", "Tint", "_Tint", (0.55, 1.0, 0.9, 1.0))
    emis, emis_o = g.prop("Vector1ShaderProperty", "Emission Boost", "_EmissionBoost", 0.6)
    dstr, dstr_o = g.prop("Vector1ShaderProperty", "Distortion Strength", "_DistortStrength", 0.06)
    dsc, dsc_o = g.prop("Vector1ShaderProperty", "Distortion Scale", "_DistortScale", 5.0)
    spd, spd_o = g.prop("Vector1ShaderProperty", "Noise Scroll Speed", "_ScrollSpeed", 0.12)
    tiles, tiles_o = g.prop("Vector1ShaderProperty", "Flipbook Tiles (edge fade cells)", "_FlipbookTiles", 1.0)
    soft, soft_o = g.prop("Vector1ShaderProperty", "Edge Softness", "_EdgeSoftness", 0.32)

    uv = g.node("UVNode")
    sp = g.node("SplitNode"); g.link(uv, "Out", sp, "In")
    uv2 = g.node("CombineNode"); g.link(sp, "R", uv2, "R"); g.link(sp, "G", uv2, "G")
    time = g.node("TimeNode")
    ts = g.node("MultiplyNode"); g.link(time, "Time", ts, "A"); g.link(spd, spd_o, ts, "B")
    nuv = g.node("AddNode"); g.link(uv2, "RG", nuv, "A"); g.link(ts, "Out", nuv, "B")
    noise = g.node("GradientNoiseNode"); g.link(nuv, "Out", noise, "UV"); g.link(dsc, dsc_o, noise, "Scale")
    centred = g.node("SubtractNode", {"B": 0.5}); g.link(noise, "Out", centred, "A")
    off = g.node("MultiplyNode"); g.link(centred, "Out", off, "A"); g.link(dstr, dstr_o, off, "B")
    duv = g.node("AddNode"); g.link(uv2, "RG", duv, "A"); g.link(off, "Out", duv, "B")
    smp = g.node("SampleTexture2DNode"); g.link(tex, tex_o, smp, "Texture"); g.link(duv, "Out", smp, "UV")
    # soft circular fade inside each flipbook cell: never a visible square
    cell_scale = g.node("MultiplyNode"); g.link(uv2, "RG", cell_scale, "A"); g.link(tiles, tiles_o, cell_scale, "B")
    cell = g.node("FractionNode"); g.link(cell_scale, "Out", cell, "In")
    centre = g.node("Vector2Node", {"X": 0.5, "Y": 0.5})
    dist = g.node("DistanceNode"); g.link(cell, "Out", dist, "A"); g.link(centre, "Out", dist, "B")
    inner = g.node("SubtractNode", {"A": 0.5}); g.link(soft, soft_o, inner, "B")
    fade = g.node("SmoothstepNode", {"Edge1": 0.5}); g.link(inner, "Out", fade, "Edge2"); g.link(dist, "Out", fade, "In")
    vc = g.node("VertexColorNode")
    vcs = g.node("SplitNode"); g.link(vc, "Out", vcs, "In")
    tints = g.node("SplitNode"); g.link(tint, tint_o, tints, "In")
    a1 = g.node("MultiplyNode"); g.link(smp, "A", a1, "A"); g.link(vcs, "A", a1, "B")
    a2 = g.node("MultiplyNode"); g.link(a1, "Out", a2, "A"); g.link(fade, "Out", a2, "B")
    a3 = g.node("MultiplyNode"); g.link(a2, "Out", a3, "A"); g.link(tints, "A", a3, "B")
    sat = g.node("SaturateNode"); g.link(a3, "Out", sat, "In")
    g.link(sat, "Out", alpha, "Alpha")
    c1 = g.node("MultiplyNode"); g.link(vc, "Out", c1, "A"); g.link(tint, tint_o, c1, "B")
    c2 = g.node("MultiplyNode"); g.link(c1, "Out", c2, "A"); g.link(smp, "RGBA", c2, "B")
    boost = g.node("AddNode", {"A": 1.0}); g.link(emis, emis_o, boost, "B")
    c3 = g.node("MultiplyNode"); g.link(c2, "Out", c3, "A"); g.link(boost, "Out", c3, "B")
    g.link(c3, "Out", base, "Base Color")
    return g.write(alpha_mode=0)


# ---------------------------------------------------------------- B. Spirit Formation (rune band / seal meshes)
def spirit_formation():
    g = Graph("SG_SpiritFormation")
    base, alpha = standard_blocks(g)
    mask, mask_o = g.prop("Texture2DShaderProperty", "Rune Mask", "_RuneMask", None)
    tint, tint_o = g.prop("ColorShaderProperty", "Jade Tint", "_Tint", (0.32, 0.92, 0.78, 0.6))
    emis, emis_o = g.prop("Vector1ShaderProperty", "Emission", "_Emission", 1.0)
    tiling, tiling_o = g.prop("Vector1ShaderProperty", "Rune Repeats", "_Tiling", 40.0)
    act, act_o = g.prop("Vector1ShaderProperty", "Activation", "_Activation", 0.0)
    asoft, asoft_o = g.prop("Vector1ShaderProperty", "Activation Softness", "_ActivationSoftness", 0.08)
    basel, basel_o = g.prop("Vector1ShaderProperty", "Base Level", "_BaseLevel", 0.25)
    lit, lit_o = g.prop("Vector1ShaderProperty", "Lit Level", "_LitLevel", 1.2)
    dis, dis_o = g.prop("Vector1ShaderProperty", "Dissolve", "_Dissolve", 0.0)
    dsc, dsc_o = g.prop("Vector1ShaderProperty", "Dissolve Scale", "_DissolveScale", 30.0)

    uv = g.node("UVNode")
    sp = g.node("SplitNode"); g.link(uv, "Out", sp, "In")
    ut = g.node("MultiplyNode"); g.link(sp, "R", ut, "A"); g.link(tiling, tiling_o, ut, "B")
    idx = g.node("FloorNode"); g.link(ut, "Out", idx, "In")
    runeu = g.node("DivideNode"); g.link(idx, "Out", runeu, "A"); g.link(tiling, tiling_o, runeu, "B")
    muv = g.node("CombineNode"); g.link(ut, "Out", muv, "R"); g.link(sp, "G", muv, "G")
    smp = g.node("SampleTexture2DNode"); g.link(mask, mask_o, smp, "Texture"); g.link(muv, "RG", smp, "UV")
    # activation threshold per rune: band = rune position along u (vertex colour G = 1, R = 0);
    # seals = their own threshold in vertex colour R (G = 0)
    vc = g.node("VertexColorNode")
    vcs = g.node("SplitNode"); g.link(vc, "Out", vcs, "In")
    th1 = g.node("MultiplyNode"); g.link(runeu, "Out", th1, "A"); g.link(vcs, "G", th1, "B")
    th = g.node("AddNode"); g.link(th1, "Out", th, "A"); g.link(vcs, "R", th, "B")
    th2 = g.node("AddNode"); g.link(th, "Out", th2, "A"); g.link(asoft, asoft_o, th2, "B")
    active = g.node("SmoothstepNode"); g.link(th, "Out", active, "Edge1"); g.link(th2, "Out", active, "Edge2"); g.link(act, act_o, active, "In")
    litk = g.node("MultiplyNode"); g.link(active, "Out", litk, "A"); g.link(lit, lit_o, litk, "B")
    level = g.node("AddNode"); g.link(basel, basel_o, level, "A"); g.link(litk, "Out", level, "B")
    # dissolve / fade transition
    noise = g.node("GradientNoiseNode"); g.link(muv, "RG", noise, "UV"); g.link(dsc, dsc_o, noise, "Scale")
    dlo = g.node("SubtractNode", {"B": 0.12}); g.link(dis, dis_o, dlo, "A")
    keep = g.node("SmoothstepNode"); g.link(dlo, "Out", keep, "Edge1"); g.link(dis, dis_o, keep, "Edge2"); g.link(noise, "Out", keep, "In")
    tints = g.node("SplitNode"); g.link(tint, tint_o, tints, "In")
    a1 = g.node("MultiplyNode"); g.link(smp, "A", a1, "A"); g.link(level, "Out", a1, "B")
    a2 = g.node("MultiplyNode"); g.link(a1, "Out", a2, "A"); g.link(keep, "Out", a2, "B")
    a3 = g.node("MultiplyNode"); g.link(a2, "Out", a3, "A"); g.link(tints, "A", a3, "B")
    a4 = g.node("MultiplyNode"); g.link(a3, "Out", a4, "A"); g.link(vcs, "A", a4, "B")
    sat = g.node("SaturateNode"); g.link(a4, "Out", sat, "In")
    g.link(sat, "Out", alpha, "Alpha")
    whiten = g.node("MultiplyNode", {"B": 0.5}); g.link(active, "Out", whiten, "A")
    gain = g.node("AddNode"); g.link(emis, emis_o, gain, "A"); g.link(whiten, "Out", gain, "B")
    col = g.node("MultiplyNode"); g.link(tint, tint_o, col, "A"); g.link(gain, "Out", col, "B")
    g.link(col, "Out", base, "Base Color")
    return g.write(alpha_mode=2)  # additive


# ---------------------------------------------------------------- C. Scripture Glyphs (particles)
def scripture_glyphs():
    g = Graph("SG_ScriptureGlyph")
    base, alpha = standard_blocks(g)
    atlas, atlas_o = g.prop("Texture2DShaderProperty", "Glyph Atlas", "_MainTex", None)
    edgec, edgec_o = g.prop("ColorShaderProperty", "Dissolve Edge", "_EdgeColor", (1.0, 0.96, 0.78, 1.0))
    glow, glow_o = g.prop("Vector1ShaderProperty", "Glow Boost", "_GlowBoost", 0.5)
    fin, fin_o = g.prop("Vector1ShaderProperty", "Fade In (age)", "_FadeIn", 0.18)
    dst, dst_o = g.prop("Vector1ShaderProperty", "Dissolve Start (age)", "_DissolveStart", 0.6)
    dsc, dsc_o = g.prop("Vector1ShaderProperty", "Dissolve Scale", "_DissolveScale", 18.0)

    uv = g.node("UVNode")  # xy = flipbook UV, z = AgePercent, w = StableRandom.x (custom vertex streams)
    sp = g.node("SplitNode"); g.link(uv, "Out", sp, "In")
    uv2 = g.node("CombineNode"); g.link(sp, "R", uv2, "R"); g.link(sp, "G", uv2, "G")
    smp = g.node("SampleTexture2DNode"); g.link(atlas, atlas_o, smp, "Texture"); g.link(uv2, "RG", smp, "UV")
    fade = g.node("SmoothstepNode", {"Edge1": 0.0}); g.link(fin, fin_o, fade, "Edge2"); g.link(sp, "B", fade, "In")
    num = g.node("SubtractNode"); g.link(sp, "B", num, "A"); g.link(dst, dst_o, num, "B")
    den = g.node("OneMinusNode"); g.link(dst, dst_o, den, "In")
    t0 = g.node("DivideNode"); g.link(num, "Out", t0, "A"); g.link(den, "Out", t0, "B")
    t = g.node("SaturateNode"); g.link(t0, "Out", t, "In")
    roff = g.node("MultiplyNode", {"B": 7.0}); g.link(sp, "A", roff, "A")
    nuv = g.node("AddNode"); g.link(uv2, "RG", nuv, "A"); g.link(roff, "Out", nuv, "B")
    noise = g.node("GradientNoiseNode"); g.link(nuv, "Out", noise, "UV"); g.link(dsc, dsc_o, noise, "Scale")
    lo = g.node("SubtractNode", {"B": 0.07}); g.link(t, "Out", lo, "A")
    hi = g.node("AddNode", {"B": 0.07}); g.link(t, "Out", hi, "A")
    keep = g.node("SmoothstepNode"); g.link(lo, "Out", keep, "Edge1"); g.link(hi, "Out", keep, "Edge2"); g.link(noise, "Out", keep, "In")
    # ember edge only while dissolving: keep * (1 - keep) * 4, gated by t
    om = g.node("OneMinusNode"); g.link(keep, "Out", om, "In")
    e1 = g.node("MultiplyNode"); g.link(keep, "Out", e1, "A"); g.link(om, "Out", e1, "B")
    e2 = g.node("MultiplyNode", {"B": 4.0}); g.link(e1, "Out", e2, "A")
    gate = g.node("MultiplyNode", {"B": 20.0}); g.link(t, "Out", gate, "A")
    gates = g.node("SaturateNode"); g.link(gate, "Out", gates, "In")
    edge = g.node("MultiplyNode"); g.link(e2, "Out", edge, "A"); g.link(gates, "Out", edge, "B")
    vc = g.node("VertexColorNode")
    vcs = g.node("SplitNode"); g.link(vc, "Out", vcs, "In")
    col = g.node("LerpNode"); g.link(vc, "Out", col, "A"); g.link(edgec, edgec_o, col, "B"); g.link(edge, "Out", col, "T")
    boost = g.node("AddNode", {"A": 1.0}); g.link(glow, glow_o, boost, "B")
    colb = g.node("MultiplyNode"); g.link(col, "Out", colb, "A"); g.link(boost, "Out", colb, "B")
    g.link(colb, "Out", base, "Base Color")
    a1 = g.node("MultiplyNode"); g.link(smp, "A", a1, "A"); g.link(fade, "Out", a1, "B")
    a2 = g.node("MultiplyNode"); g.link(a1, "Out", a2, "A"); g.link(keep, "Out", a2, "B")
    a3 = g.node("MultiplyNode"); g.link(a2, "Out", a3, "A"); g.link(vcs, "A", a3, "B")
    sat = g.node("SaturateNode"); g.link(a3, "Out", sat, "In")
    g.link(sat, "Out", alpha, "Alpha")
    return g.write(alpha_mode=2)  # additive


if __name__ == "__main__":
    for build in (spirit_mist, spirit_formation, scripture_glyphs):
        print("wrote", os.path.relpath(build(), ROOT))
