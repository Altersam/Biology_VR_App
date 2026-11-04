"""Neck fix (02.11): the throat flap of the head mesh hung down over the shirt (stretched skin texture
in the shirt's V), and the shirt's own skin-coloured neck stump poked out at the collar.
1) shirt stump faces -> shirt fabric colour (the V and the collar stand become dark shirt),
   its upper ring hugs the neck so no flat-cut edge shows;
2) head faces below the collar line in front (the flap) removed, the rest of the neck base pulled in.
professor_v4.blend -> professor_v5.blend (head group 1.2 cm lower first)"""
import bpy, bmesh, math, os, numpy as np
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath=os.path.abspath("professor_v4.blend"))
O = bpy.data.objects
# 0) a shorter neck: the whole head group sits 1.2 cm lower (the concept's beard nearly touches the collar)
DROP = 0.012
for n in ("Head", "Hair", "HairCap", "Glasses", "Beard", "Mustache"): O[n].location.z -= DROP
bpy.context.view_layer.update()
def tex_px(o):
    img = [n.image for n in o.data.materials[0].node_tree.nodes if n.type == 'TEX_IMAGE'][0]
    W, H = img.size; return np.array(img.pixels[:]).reshape(H, W, 4), W, H
# ---- head neck profile (basis shape): per height and angle the outer radius around the neck axis
head = O["Head"]; HM = head.matrix_world; HMi = HM.inverted()
AX = Vector((0.0, 0.012))   # neck axis (x, y) between z 1.46 and 1.52
def polar(p): d = Vector((p.x - AX.x, p.y - AX.y)); return d.length, math.atan2(d.x, -d.y)  # angle 0 = front
NA = 24
prof = {}
for v in head.data.vertices:
    p = HM @ v.co
    if not (1.44 <= p.z < 1.53): continue
    r, a = polar(p); k = (int((p.z - 1.44) / 0.005), int((a + math.pi) / (2 * math.pi) * NA) % NA)
    prof[k] = max(prof.get(k, 0), r)
def r_head(z, a):
    zb = int((z - 1.44) / 0.005); ab = int((a + math.pi) / (2 * math.pi) * NA) % NA
    best = None
    for dz in (0, 1, -1, 2, -2):
        for da in (0, 1, -1):
            r = prof.get((zb + dz, (ab + da) % NA))
            if r: best = max(best or 0, r)
        if best: return best
    return None
# ---- 1. shirt stump -> fabric, upper ring hugs the neck
sh = O["Shirt"]; SM = sh.matrix_world; SMi = SM.inverted(); px, W, H = tex_px(sh)
bm = bmesh.new(); bm.from_mesh(sh.data); uv = bm.loops.layers.uv.active
FABRIC = Vector((0.392, 0.360))
skin = []
for f in bm.faces:
    c = SM @ f.calc_center_median()
    if c.z < 1.37: continue
    u = sum((l[uv].uv for l in f.loops), Vector((0, 0))) / len(f.loops)
    col = px[min(H - 1, max(0, int(u.y * H))), min(W - 1, max(0, int(u.x * W))), :3]
    def skinny(col): return col[0] > 0.22 and col[0] > col[1] * 1.2 and col[1] >= col[2] * 0.95
    cols = [px[min(H - 1, max(0, int(l[uv].uv.y * H))), min(W - 1, max(0, int(l[uv].uv.x * W))), :3] for l in f.loops] + [col]
    if any(skinny(c) for c in cols) and polar(c)[0] < 0.1: skin.append(f)
for f in skin:
    for l in f.loops: l[uv].uv = FABRIC + Vector(((hash(l.index) % 7) * 0.002, (hash(l.index * 3) % 7) * 0.002))
hug = 0
for v in {v for f in skin for v in f.verts}:
    p = SM @ v.co
    if p.z < 1.462: continue
    r, a = polar(p); rh = r_head(p.z, a)
    if rh is None or r <= 1e-4: continue
    t = (rh + 0.004) / r
    if abs(t - 1) < 0.02: continue
    q = Vector((AX.x + (p.x - AX.x) * t, AX.y + (p.y - AX.y) * t, p.z))
    v.co = SMi @ q; hug += 1
# a clean top edge of the collar stand: lower at the front, a little higher at the back
for v in {v for f in skin for v in f.verts}:
    p = SM @ v.co
    r, a = polar(p); top = 1.474 + 0.012 * (1 - math.cos(a)) / 2
    if p.z > top:
        rh = r_head(top, a); q = Vector((p.x, p.y, top))
        if rh and r > 1e-4:
            t = (rh + 0.004) / r; q = Vector((AX.x + (p.x - AX.x) * t, AX.y + (p.y - AX.y) * t, top))
        v.co = SMi @ q
# the rest of the shirt top (its own collar points poked over the coat collar): hug the neck as well
tuck = 0
for v in bm.verts:
    p = SM @ v.co
    if p.z < 1.458: continue
    r, a = polar(p); top = 1.474 + 0.012 * (1 - math.cos(a)) / 2
    rh = r_head(min(p.z, top), a)
    if rh is None or r < rh + 0.006: continue
    t = (rh + 0.004) / r
    v.co = SMi @ Vector((AX.x + (p.x - AX.x) * t, AX.y + (p.y - AX.y) * t, min(p.z, top))); tuck += 1
print("shirt collar verts tucked", tuck)
bm.to_mesh(sh.data); bm.free()
print("shirt stump faces recoloured", len(skin), "ring verts hugged", hug)
# ---- 2. head: remove the throat flap below the collar line in front; pull the remaining base in
def cut_z(p):   # collar line: low at the front centre, rising to the sides, high at the back (hidden there)
    r, a = polar(p); fa = max(0.0, math.cos(a))   # 1 front .. 0 sides/back
    return 1.466 + 0.02 * (1 - fa)
bm = bmesh.new(); bm.from_mesh(head.data)
kl = [bm.verts.layers.shape[k.name] for k in head.data.shape_keys.key_blocks]
bm.verts.ensure_lookup_table()
dead = [f for f in bm.faces if all((HM @ v.co).z < cut_z(HM @ v.co) for v in f.verts) and (HM @ f.calc_center_median()).y < 0.0]
nv0 = len(bm.verts)
bmesh.ops.delete(bm, geom=dead, context='FACES')
loose = [v for v in bm.verts if not v.link_faces]; bmesh.ops.delete(bm, geom=loose, context='VERTS')
bm.to_mesh(head.data); bm.free(); head.data.update()
print("head faces removed", len(dead), "verts", nv0, "->", len(head.data.vertices))
for kb in head.data.shape_keys.key_blocks: kb.value = 0.0
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath("professor_v5.blend"))
