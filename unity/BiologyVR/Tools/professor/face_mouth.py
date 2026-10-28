"""Talking mouth for the professor: cut the lips open along the lip line, add a mouth cavity and two rows of
teeth (joined into the head), and shape keys MouthOpen (jaw rotates about the jaw joint), MouthO (rounded
lips), MouthWide (stretched lips) on the head; MouthOpen also on the beard (it follows the chin) and a small
lift on the moustache. professor_v2.blend -> professor_v3.blend"""
import bpy, bmesh, math, mathutils, os, heapq
from mathutils import Vector, Matrix
SRC, DST = "professor_v2.blend", "professor_v3.blend"
bpy.ops.wm.open_mainfile(filepath=os.path.abspath(SRC))
head = bpy.data.objects["Head"]; M = head.matrix_world; Mi = M.inverted()
assert M.is_identity or True
def ss(a, b, x):
    if a == b: return 1.0 if x >= b else 0.0
    t = min(1.0, max(0.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
def zline(x): return 1.5899 + 0.0012 * min(1.0, (x / 0.03) ** 2)
PIVOT = Vector((0, 0.005, 1.645)); ANGLE = math.radians(13.0)
def jaw_w(p, hint=None):
    if hint is False: return 0.0
    ax = abs(p.x); dz = zline(min(ax, 0.03)) - p.z
    if hint is True: dz = max(dz, 0.004)
    inner = 1 - ss(0.026, 0.040, ax)
    band = 0.002 * inner + 0.016 * (1 - inner)
    w = ss(-0.0005, band, dz)
    w *= 1 - ss(-0.02, 0.03, p.y)
    w *= ss(1.472, 1.512, p.z)
    return w
def rot(p, w):
    a = ANGLE * w; d = p - PIVOT
    y = d.y * math.cos(a) - d.z * math.sin(a); z = d.y * math.sin(a) + d.z * math.cos(a)
    return PIVOT + Vector((d.x, y, z))
def lip_lift(p, hint):
    if hint is True: return 0.0
    ax = abs(p.x); dz = zline(min(ax, 0.03)) - p.z
    if dz > 0.0005: return 0.0
    inner = 1 - ss(0.026, 0.040, ax)
    return 0.0016 * inner * (1 - ss(0.0, 0.008, -dz)) * (1 if p.y < -0.09 else 0)
MC = Vector((0, -0.112, 1.590))
def mouth_f(p):
    d = Vector(((p.x - MC.x) / 0.046, (p.y - MC.y) / 0.05, (p.z - MC.z) / 0.024))
    return 1 - ss(0.35, 1.0, d.length)

# ---------------------------------------------------------------- 1. open the lips
bm = bmesh.new(); bm.from_mesh(head.data); bm.verts.ensure_lookup_table()
W = lambda v: M @ v.co
region = [v for v in bm.verts if abs(W(v).x) < 0.036 and 1.583 < W(v).z < 1.597 and W(v).y < -0.098]
rs = set(region)
def corner(sign):
    c = [v for v in region if 0.026 < sign * W(v).x < 0.033]
    return min(c, key=lambda v: abs(W(v).z - zline(0.03)) + 0.2 * abs(abs(W(v).x) - 0.030))
L, R = corner(-1), corner(1)
dist = {L: 0.0}; prev = {}; pq = [(0.0, id(L), L)]
while pq:
    d, _, v = heapq.heappop(pq)
    if v is R: break
    if d > dist.get(v, 1e9): continue
    for e in v.link_edges:
        u = e.other_vert(v)
        if u not in rs: continue
        mid = (W(u) + W(v)) / 2
        c = e.calc_length() * (1 + 600 * abs(mid.z - zline(abs(mid.x))) / 0.001 * 0.001 + 0.0)
        c *= 1 + 8.0 * max(0.0, -0.1105 - mid.y) / 0.002  # prefer the groove (deepest y)
        nd = d + c
        if nd < dist.get(u, 1e9): dist[u] = nd; prev[u] = v; heapq.heappush(pq, (nd, id(u), u))
path = [R]
while path[-1] is not L: path.append(prev[path[-1]])
path.reverse()
edges = [bm.edges.get((a, b)) for a, b in zip(path, path[1:])]
print("lip path verts", len(path), "x from", round(W(L).x, 4), "to", round(W(R).x, 4), "z", [round(W(v).z, 4) for v in path])
res = bmesh.ops.split_edges(bm, edges=edges)
bm.verts.ensure_lookup_table(); bm.normal_update()
# which side of the slit is each path vertex on?
side = {}
for v in bm.verts:
    if abs(W(v).x) < 0.034 and abs(W(v).z - zline(abs(W(v).x))) < 0.0012 and W(v).y < -0.10:
        fz = [(M @ f.calc_center_median()).z for f in v.link_faces]
        if fz: side[v.index] = (sum(fz) / len(fz)) < zline(abs(W(v).x))
bm.to_mesh(head.data); bm.free()
print("split edges", len(edges), "slit verts classified", len(side))

# ---------------------------------------------------------------- 2. cavity + teeth (joined into the head)
def add_mesh(name, verts, faces, mat):
    me = bpy.data.meshes.new(name); me.from_pydata([Vector(v) for v in verts], [], faces); me.update()
    o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o)
    if mat: me.materials.append(mat)
    for p in me.polygons: p.use_smooth = True
    return o
def make_mat(name, rgb):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]; b.inputs["Base Color"].default_value = (*rgb, 1); b.inputs["Roughness"].default_value = 0.5
    return m
mat_mouth = make_mat("Prof_Mouth", (0.16, 0.035, 0.04)); mat_teeth = make_mat("Prof_Teeth", (0.86, 0.83, 0.76))
# cavity: ellipsoid behind the lips
cv, cf = [], []; nu, nv = 16, 10; c = Vector((0, -0.083, 1.586)); r = Vector((0.025, 0.021, 0.019))
for i in range(nv + 1):
    th = math.pi * i / nv
    for j in range(nu):
        ph = 2 * math.pi * j / nu
        cv.append((c.x + r.x * math.sin(th) * math.cos(ph), c.y + r.y * math.sin(th) * math.sin(ph), c.z + r.z * math.cos(th)))
for i in range(nv):
    for j in range(nu):
        a = i * nu + j; b = i * nu + (j + 1) % nu
        cf.append((a, a + nu, b + nu, b))
cav = add_mesh("Cavity", cv, cf, mat_mouth)
# teeth: two curved strips following the arch
def arch(zt, zb, rad, cy, n=14, spread=48):
    vs, fs = [], []
    for k in range(n + 1):
        a = math.radians(-spread + 2 * spread * k / n); x = rad * math.sin(a); y = cy - rad * math.cos(a)
        for (zz, dr) in ((zt, 0), (zb, 0), (zb, -0.002), (zt, -0.002)):
            vs.append((x * (1 + dr / rad), cy - (rad + dr) * math.cos(a), zz))
    for k in range(n):
        for s in range(4):
            a0 = k * 4 + s; a1 = k * 4 + (s + 1) % 4
            fs.append((a0, a1, a1 + 4, a0 + 4))
    return vs, fs
tv, tf = arch(1.5922, 1.5858, 0.0215, -0.0855); upper = add_mesh("TeethUpper", tv, tf, mat_teeth)
tv, tf = arch(1.5835, 1.5770, 0.0200, -0.0845); lower = add_mesh("TeethLower", tv, tf, mat_teeth)
for o in (cav, upper, lower):
    o.data.materials.clear(); o.data.materials.append(mat_mouth if o is cav else mat_teeth)
# join into head (keeps the head's shape keys; joined parts get basis positions in every key)
bpy.ops.object.select_all(action='DESELECT')
for o in (cav, upper, lower): o.select_set(True)
head.select_set(True); bpy.context.view_layer.objects.active = head
bpy.ops.object.join()
part_of = {}  # vertex index -> 'cav'/'tu'/'tl'/None, by position (joined verts are appended at the end)
me = head.data; nbase = len(me.vertices) - (len(cv) + 2 * len(tv) + 0)
# ---------------------------------------------------------------- 3. shape keys
keys = me.shape_keys.key_blocks; basis = keys["Basis"]
def new_key(name):
    if name in keys: head.shape_key_remove(keys[name])
    return head.shape_key_add(name=name, from_mix=False)
kO, kOpen, kW = new_key("MouthOpen"), new_key("MouthO"), new_key("MouthWide")
kOpen, kO = kO, kOpen  # names: kOpen=MouthOpen, kO=MouthO
names = [m.name if m else "" for m in me.materials]
part = {}
for poly in me.polygons:
    n = names[poly.material_index]
    for vi in poly.vertices:
        if n.startswith("Prof_Mouth"): part[vi] = "cav"
        elif n.startswith("Prof_Teeth"): part[vi] = "tu" if (M @ basis.data[vi].co).z > 1.5840 else "tl"
print("parts", {k: sum(1 for x in part.values() if x == k) for k in ("cav", "tu", "tl")})
for i, v in enumerate(me.vertices):
    p = M @ basis.data[i].co; pt = part.get(i)
    if pt == "tl": w, hint = 1.0, True                     # lower teeth ride with the jaw
    elif pt == "tu": w, hint = 0.0, False                  # upper teeth stay
    elif pt == "cav": w, hint = ss(1.590, 1.578, p.z), None  # lower half of the cavity opens with the jaw
    else:
        hint = side.get(i, None); w = jaw_w(p, hint)
    q = rot(p, w); q.z += lip_lift(p, hint) if pt is None else 0.0
    kOpen.data[i].co = Mi @ q
    f = mouth_f(p) if pt is None else 0.0
    po = p.copy(); po.x = p.x * (1 - 0.38 * f); po.y = p.y - 0.006 * f * (1 - ss(0.0, 0.04, abs(p.x)))
    kO.data[i].co = Mi @ po
    pw = p.copy(); pw.x = p.x * (1 + 0.13 * f); pw.y = p.y + 0.0015 * f; pw.z = p.z + 0.0008 * f * ss(0.01, 0.03, abs(p.x))
    kW.data[i].co = Mi @ pw
print("head keys", [k.name for k in me.shape_keys.key_blocks], "verts", len(me.vertices))
# beard follows the chin; moustache lifts a little
for name in ("Beard", "Mustache"):
    o = bpy.data.objects[name]; om = o.matrix_world; omi = om.inverted()
    if not o.data.shape_keys: o.shape_key_add(name="Basis", from_mix=False)
    k = o.shape_key_add(name="MouthOpen", from_mix=False)
    for i, v in enumerate(o.data.vertices):
        p = om @ v.co
        if name == "Beard": q = rot(p, jaw_w(p))
        else: q = p.copy(); q.z += 0.0012 * (1 - ss(0.01, 0.045, abs(p.x)))
        k.data[i].co = omi @ q
    print(name, "keys", [kk.name for kk in o.data.shape_keys.key_blocks])
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(DST))
