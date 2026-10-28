"""Professor hair fix: a fitted short-sided hair cap grown from the scalp (covers the bald temples,
runs into sideburns that meet the beard, wraps around the ears, ends at the nape) + the original swept-up
crest lowered and slimmed so it sits on the cap. Writes professor_v2.blend."""
import bpy, bmesh, math, mathutils, os, sys, numpy as np
from collections import deque
src = "professor_relaxed.blend"; dst = "professor_v2.blend"
CREST_SZ, CREST_SY, CREST_DZ, CREST_DY = 0.72, 0.92, -0.003, -0.006
bpy.ops.wm.open_mainfile(filepath=os.path.abspath(src))
head = bpy.data.objects["Head"]; M = head.matrix_world; Mi = M.inverted()
EAR = [mathutils.Vector((s * 0.106, 0.027, 1.662)) for s in (1, -1)]
H = [(0, 1.772), (30, 1.766), (45, 1.750), (55, 1.730), (65, 1.705), (75, 1.680), (82, 1.648), (89, 1.636), (115, 1.640), (140, 1.638), (180, 1.605)]
def hairline(phi):
    for (a0, h0), (a1, h1) in zip(H, H[1:]):
        if phi <= a1: return h0 + (h1 - h0) * (phi - a0) / (a1 - a0)
    return H[-1][1]
def phi_of(p): return math.degrees(math.atan2(abs(p.x), -p.y))
def inside(p):
    if p.z < hairline(phi_of(p)): return False
    for e in EAR:
        d = p - e; d.z *= 0.85
        if d.length < 0.047: return False
    return True
bm = bmesh.new(); bm.from_mesh(head.data); bm.faces.ensure_lookup_table()
keep = [f for f in bm.faces if inside(M @ f.calc_center_median())]
cap_bm = bmesh.new(); vmap = {}
for f in keep:
    vs = []
    for v in f.verts:
        if v not in vmap: vmap[v] = cap_bm.verts.new(M @ v.co)
        vs.append(vmap[v])
    try: cap_bm.faces.new(vs)
    except ValueError: pass
cap_bm.verts.ensure_lookup_table(); cap_bm.normal_update()
# distance from the open border (graph distance in metres)
dist = {v: 1e9 for v in cap_bm.verts}; q = deque()
for v in cap_bm.verts:
    if v.is_boundary: dist[v] = 0.0; q.append(v)
while q:
    v = q.popleft()
    for e in v.link_edges:
        w = e.other_vert(v); nd = dist[v] + e.calc_length()
        if nd < dist[w] - 1e-6: dist[w] = nd; q.append(w)
def ss(a, b, x): t = min(1, max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
for v in cap_bm.verts:
    p = v.co.copy(); n = v.normal.copy()
    phi = phi_of(p)
    T = 0.0075 + 0.0075 * ss(1.72, 1.80, p.z) + 0.004 * ss(120, 180, phi) * ss(1.62, 1.70, p.z) + 0.003 * (1 - ss(10, 50, phi)) * ss(1.74, 1.79, p.z)
    w = ss(0.0, 0.022, dist[v])
    v.co = p + n * (0.0012 + (T - 0.0012) * w)
# spherical UVs: u around the head, v from the border up to the crown -> strands run up and back
uv = cap_bm.loops.layers.uv.new("UVMap")
for f in cap_bm.faces:
    for l in f.loops:
        p = l.vert.co; ang = math.atan2(p.x, -p.y)
        el = math.atan2(p.z - 1.60, math.hypot(p.x, p.y))
        l[uv].uv = (ang / (2 * math.pi) + 0.5, el / (math.pi / 2))
me = bpy.data.meshes.new("HairCap"); cap_bm.to_mesh(me); cap_bm.free(); bm.free()
for p in me.polygons: p.use_smooth = True
cap = bpy.data.objects.new("HairCap", me); bpy.context.scene.collection.objects.link(cap)
# streaked grey hair texture (strands along v), slightly darker toward the border
N = 512; rng = np.random.default_rng(7)
u = np.linspace(0, 1, N, endpoint=False)
def noise1(freq, amp):
    ph = rng.uniform(0, 2 * np.pi, 6); return sum(np.sin(2 * np.pi * u * freq * (k + 1) * 1.37 + ph[k]) / (k + 1) for k in range(6)) * amp
stripe = noise1(37, 0.025) + noise1(160, 0.018) + (rng.random(N) - 0.5) * 0.03
img = np.zeros((N, N, 4), np.float32)
vv = np.linspace(0, 1, N)[:, None]
base = 0.74 + stripe[None, :] - 0.10 * np.clip(1 - vv * 3, 0, 1) + 0.05 * np.clip(vv - 0.6, 0, 1)
img[..., 0] = base * 0.98; img[..., 1] = base * 0.98; img[..., 2] = base * 1.02; img[..., 3] = 1
img = np.clip(img, 0, 1)
im = bpy.data.images.new("Prof_HairCap_BaseColor", N, N); im.pixels = img.ravel().tolist()
im.filepath_raw = os.path.abspath("Prof_HairCap_BaseColor.png"); im.file_format = 'PNG'; im.save()
mat = bpy.data.materials.new("Prof_HairCap"); mat.use_nodes = True
bsdf = mat.node_tree.nodes["Principled BSDF"]; tex = mat.node_tree.nodes.new("ShaderNodeTexImage"); tex.image = im
mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"]); bsdf.inputs["Roughness"].default_value = 0.65
me.materials.append(mat)
# crest: lower and slimmer, still swept up
hair = bpy.data.objects["Hair"]
for v in hair.data.vertices:
    p = hair.matrix_world @ v.co
    p.z = 1.70 + (p.z - 1.70) * CREST_SZ + CREST_DZ
    p.y = p.y * CREST_SY + CREST_DY
    v.co = hair.matrix_world.inverted() @ p
# the cap must not poke through the crest/beard: nothing else to do, the crest sits on top
print("cap faces", len(me.polygons), "verts", len(me.vertices))
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(dst))
