"""Body check fixes: hands into the cuffs (continuing the forearm), shirt tucked under the coat where it
poked through (back, shoulders), the flared neck base pulled into the collar, shape key values reset.
Also prints the skin tones (head vs hands) for the Unity tint. professor_v3.blend -> professor_v4.blend"""
import bpy, bmesh, os, math, mathutils, numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
bpy.ops.wm.open_mainfile(filepath=os.path.abspath("professor_v3.blend"))
O = bpy.data.objects
def tex_px(o):
    img = [n.image for n in o.data.materials[0].node_tree.nodes if n.type == 'TEX_IMAGE'][0]
    W, H = img.size; return np.array(img.pixels[:]).reshape(H, W, 4), W, H
def face_col(f, uv, px, W, H):
    u = sum((l[uv].uv for l in f.loops), Vector((0, 0))) / len(f.loops)
    return px[min(H - 1, max(0, int(u.y * H))), min(W - 1, max(0, int(u.x * W))), :3]
# ---- 1. cuffs (lime faces of the coat) -> opening centre + sleeve axis
coat = O["Coat"]; CM = coat.matrix_world
px, W, H = tex_px(coat); bm = bmesh.new(); bm.from_mesh(coat.data); uv = bm.loops.layers.uv.active
cuffs = {}
for side in (-1, 1):
    fs = []
    for f in bm.faces:
        c = CM @ f.calc_center_median()
        if side * c.x < 0.2 or c.z > 1.15: continue
        col = face_col(f, uv, px, W, H)
        if col[1] > 0.45 and col[1] > col[0] * 1.15 and col[2] < 0.45: fs.append(f)
    P = [CM @ v.co for v in {v for f in fs for v in f.verts}]
    zmin, zmax = min(p.z for p in P), max(p.z for p in P)
    lo = sum([p for p in P if p.z < zmin + 0.02], Vector()) / len([p for p in P if p.z < zmin + 0.02])
    hi = sum([p for p in P if p.z > zmax - 0.02], Vector()) / len([p for p in P if p.z > zmax - 0.02])
    cuffs[side] = (lo, (hi - lo).normalized())
bm.free()
for name, side in (("HandL", -1), ("HandR", 1)):
    h = O[name]; Mw = h.matrix_world.copy()
    P = [Mw @ v.co for v in h.data.vertices]
    top = max(p.z for p in P); bot = min(p.z for p in P)
    wrist = sum([p for p in P if p.z > top - 0.015], Vector()) / len([p for p in P if p.z > top - 0.015])
    tips = sum([p for p in P if p.z < bot + 0.03], Vector()) / len([p for p in P if p.z < bot + 0.03])
    up = (wrist - tips).normalized()
    lo, axis = cuffs[side]
    rot = up.rotation_difference(axis).to_matrix()
    goal = lo + axis * 0.035
    newP = [goal + rot @ (p - wrist) for p in P]
    inv = h.matrix_world.inverted()
    for v, p in zip(h.data.vertices, newP): v.co = inv @ p
    # pivot (object origin) at the new wrist, as the game rotates the hand there
    d = goal - h.matrix_world.translation
    for v in h.data.vertices: v.co = v.co - h.matrix_world.inverted().to_3x3() @ d
    h.location = h.location + d
    print(name, "wrist moved", tuple(round(a, 3) for a in (goal - wrist)), "tilt", round(math.degrees(up.angle(axis)), 1))
# ---- 2. shirt under the coat (back and shoulders)
dg = bpy.context.evaluated_depsgraph_get()
bvh = BVHTree.FromObject(coat, dg); CMi = CM.inverted()
sh = O["Shirt"]; SM = sh.matrix_world; SMi = SM.inverted(); moved = 0
for v in sh.data.vertices:
    p = SM @ v.co
    if p.y < -0.06 or p.z < 1.0 or (p.y <= 0.0 and p.z > 1.455): continue  # back: all under the coat (its collar covers); front/sides: the collar stays
    # ray from the body axis outward through the vertex: the shirt must stay under the first coat surface
    a = Vector((0, 0.0, p.z)); d = Vector((p.x, p.y, 0)); r = d.length
    if r < 1e-4: continue
    d.normalize()
    o_l = CMi @ a; d_l = (CMi.to_3x3() @ d).normalized()
    hit = bvh.ray_cast(o_l, d_l, 1.0)
    if hit[0] is None:
        if p.y > 0.0: v.co = SMi @ (a + d * r * 0.8); moved += 1   # a gap in the coarse coat: just pull it in
        continue
    hr = ((CM @ hit[0]) - a).length
    margin = 0.02 if p.y > 0.0 else 0.01   # coarse coat: faces bulge between vertices, keep a safe gap
    if r > hr - margin:
        v.co = SMi @ (a + d * max(0.03, hr - margin)); moved += 1
print("shirt verts tucked", moved)
# ---- 3. neck base: the back of the neck goes straight into the collar (scaled per height, no folds)
head = O["Head"]; HM = head.matrix_world; HMi = HM.inverted(); keys = head.data.shape_keys.key_blocks; basis = keys["Basis"]
def ss(a, b, x): t = min(1, max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
P = [HM @ basis.data[i].co for i in range(len(head.data.vertices))]
back = [i for i, p in enumerate(P) if p.y > -0.01 and p.z < 1.54]
bins = {}
for i in back:
    b = int((P[i].z - 1.43) / 0.005); r = Vector((P[i].x, P[i].y)).length
    bins[b] = max(bins.get(b, 0), r)
pulled = 0
for i in back:
    p = P[i]; b = int((p.z - 1.43) / 0.005); R = max(bins.get(b, 0), 1e-4)
    w = 1 - ss(1.505, 1.54, p.z)
    target = min(1.0, 0.07 / R)
    sc = 1 + (target - 1) * w
    if sc >= 0.999: continue
    q = p.copy(); q.x *= sc; q.y *= sc
    delta = HMi.to_3x3() @ (q - p)
    for kb in keys: kb.data[i].co = kb.data[i].co + delta
    head.data.vertices[i].co = head.data.vertices[i].co + delta; pulled += 1
print("neck verts scaled in", pulled)
for kb in keys: kb.value = 0.0 if kb.name != "Basis" else kb.value
for n in ("Beard", "Mustache"):
    for kb in O[n].data.shape_keys.key_blocks: kb.value = 0.0
# ---- 4. skin tones
def mean_col(o, filt=None):
    px, W, H = tex_px(o); bm = bmesh.new(); bm.from_mesh(o.data); uv = bm.loops.layers.uv.active; M = o.matrix_world; cols = []
    for f in bm.faces:
        if filt and not filt(M @ f.calc_center_median()): continue
        cols.append(face_col(f, uv, px, W, H))
    bm.free(); return np.sum(cols, 0) / max(1e-9, sum(1 for _ in cols))
hc = mean_col(head, lambda p: abs(p.x) > 0.035 and abs(p.x) < 0.075 and p.y < -0.08 and 1.60 < p.z < 1.66)
print("SKIN head cheeks (linear)", np.round(hc, 4))
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath("professor_v4.blend"))
