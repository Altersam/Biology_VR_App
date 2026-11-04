"""Professor v8 (03.11): the head sits IN the clothes and the hair grows from the head.
professor_v5.blend -> professor_v8.blend (+ tex_v8/Prof_{HairLocks,HairCap,Collar,ShirtCollar,Strap,Hair}_BaseColor.png)

Layers around the neck, inside -> out (each one closes the one under it):
1) neck: thicker toward its base (an older man's neck);
2) ShirtCollar (new): dark-green stand with a folded leaf around the neck, open at the front V;
   the shirt's old ragged collar beside/behind the neck is removed (the front V stays);
3) Collar (new): the lab coat's lime collar - stands at the back, flares down onto the coat, its ends sink
   into the lapels; the old low collar strip stays underneath (tucked where it would poke out);
   both collars leave room for the head turning +-48 deg about the neck base (Bvr2Professor);
4) Strap (new): the amulet hangs on a leather strap that goes from the medallion up over the collarbones and
   AROUND the back of the neck on top of the coat collar (the old stiff U loop under the chin is removed);
5) hair: the smooth "helmet" islands at the back are removed, the scalp cap regenerated thin, and
   HairLocks (new): two layers of tapered locks combed from under the front crest back over the crown and
   down to the hairline above the ears and at the nape; the crest's darkest baked creases are lifted.
"""
import bpy, bmesh, math, os, random
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from collections import deque
from PIL import Image

random.seed(7)
bpy.ops.wm.open_mainfile(filepath=os.path.abspath("professor_v5.blend"))
O = bpy.data.objects
TEX = os.path.abspath("tex_v8"); os.makedirs(TEX, exist_ok=True)
# the project's professor textures (read: coat collar colour test, hair texture); BVR2_PROF_TEX overrides
_here = os.path.dirname(os.path.abspath(__file__))
PROF_TEX = os.environ.get("BVR2_PROF_TEX") or next((p for p in (
    os.path.join(_here, "..", "..", "Assets", "BiologyVR", "BiologyVR2", "Art", "Professor", "Textures"),
    "/home/claude/bvr2/Assets/BiologyVR/BiologyVR2/Art/Professor/Textures") if os.path.isdir(p)), ".")


def ss(a, b, x):
    t = min(1.0, max(0.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


AX = Vector((0.0, 0.012))


def polar(p):
    d = Vector((p.x - AX.x, p.y - AX.y))
    return d.length, math.degrees(math.atan2(d.x, -d.y))      # angle 0 = front, +-180 = back


def at_polar(r, a, z):
    t = math.radians(a)
    return Vector((AX.x + r * math.sin(t), AX.y - r * math.cos(t), z))


head = O["Head"]; HM = head.matrix_world.copy(); HMi = HM.inverted(); keys = head.data.shape_keys.key_blocks
basis = keys["Basis"]

# ---------------------------------------------------------------- 1. neck
thick = 0
for i in range(len(head.data.vertices)):
    p = HM @ basis.data[i].co
    if p.z > 1.61: continue
    r, a = polar(p)
    f = 1.0 + 0.13 * (1 - ss(1.465, 1.545, p.z))
    if f <= 1.001: continue
    q = Vector((AX.x + (p.x - AX.x) * f, AX.y + (p.y - AX.y) * f, p.z))
    delta = HMi.to_3x3() @ (q - p)
    for kb in keys: kb.data[i].co = kb.data[i].co + delta
    head.data.vertices[i].co = head.data.vertices[i].co + delta; thick += 1
head.data.update()
print("neck verts thickened", thick)

head_bvh = BVHTree.FromPolygons([HM @ v.co for v in head.data.vertices], [list(p.vertices) for p in head.data.polygons])
_rn = {}


def r_neck(z, a):
    """outer surface of the head/neck at height z, angle a (ray cast inward from outside)"""
    key = (round(z, 3), round(a))
    if key in _rn: return _rn[key]
    d = at_polar(1.0, a, 0) - Vector((AX.x, AX.y, 0)); d.z = 0; d.normalize()
    o = Vector((AX.x, AX.y, z)) + d * 0.3
    hit = head_bvh.ray_cast(o, -d, 0.3)
    r = (hit[0] - Vector((AX.x, AX.y, z))).length if hit[0] is not None else 0.0
    _rn[key] = r
    return r


def r_env(z, a, turn=48):
    """the neck surface swept by the head turning +-turn degrees about the neck base"""
    return max(r_neck(z, a + d) for d in range(-turn, turn + 1, 4))


# shirt collar stand hugs the thicker neck
sh = O["Shirt"]; SM = sh.matrix_world; SMi = SM.inverted(); hug = 0
for v in sh.data.vertices:
    p = SM @ v.co
    if p.z < 1.455: continue
    r, a = polar(p)
    if r > 0.13 or r < 1e-4 or abs(a) > 40: continue       # the front V of the shirt; the rest is the new ShirtCollar
    rh = r_neck(p.z, a) + 0.004
    if abs(r - rh) > 1e-4:
        v.co = SMi @ at_polar(rh, a, p.z); hug += 1
print("shirt collar verts re-fitted", hug)

# ---------------------------------------------------------------- 2. collars + strap
# layers around the neck, inside -> out: neck -> ShirtCollar (dark green, stand + folded leaf) ->
# Collar (the lab coat's lime collar: stands at the back, flares down onto the coat) -> Strap (the
# amulet's leather strap: from the medallion up over the collarbones and around the back of the neck).
# Both collars leave room for the head turning +-48 deg about the neck base (Bvr2Professor).
coat = O["Coat"]; CM = coat.matrix_world
med = O["Medallion"]; MM = med.matrix_world
COAT_PNG = os.path.join(PROF_TEX, "Prof_Coat_BaseColor.png")
coat_img = np.asarray(Image.open(COAT_PNG).convert('RGB')).astype(int)
CH, CW = coat_img.shape[:2]


def bvh_world(objs):
    vs, ps = [], []
    for o in objs:
        M = o.matrix_world; base = len(vs)
        vs += [M @ v.co for v in o.data.vertices]
        ps += [[base + i for i in p.vertices] for p in o.data.polygons]
    return BVHTree.FromPolygons(vs, ps)


def lime_uv(uv):
    x = min(CW - 1, max(0, int((uv[0] % 1.0) * CW))); y = min(CH - 1, max(0, int((1 - (uv[1] % 1.0)) * CH)))
    r, g, b = coat_img[y, x]
    return g > r + 25 and g > b + 60


def outer_r(bvh, z, a, start=0.35):
    """outermost surface radius at height z, angle a (horizontal ray from outside toward the axis)"""
    d = at_polar(1.0, a, 0) - Vector((AX.x, AX.y, 0)); d.z = 0; d.normalize()
    hit = bvh.ray_cast(Vector((AX.x, AX.y, z)) + d * start, -d, start)
    if hit[0] is None: return None, None
    n = hit[1] if hit[1].dot(d) > 0 else -hit[1]
    return (hit[0] - Vector((AX.x, AX.y, z))).length, n


S_END, A_END, LEAF = 26.0, 60.0, 0.022

# the coat's old lime collar behind the lapels goes (the new Collar replaces it); remember how low it reached
bm = bmesh.new(); bm.from_mesh(coat.data); bm.faces.ensure_lookup_table(); uvl = bm.loops.layers.uv.active
old_base = {}; cut = []
for f in bm.faces:
    c = CM @ f.calc_center_median(); r, a = polar(c)
    if r > 0.16 or c.z < 1.33 or abs(a) < A_END + 3: continue
    uvc = sum((l[uvl].uv for l in f.loops), Vector((0, 0))) / len(f.loops)
    if not lime_uv(uvc): continue
    cut.append(f)
    for v in f.verts:
        p = CM @ v.co; rr, aa = polar(p); k = int(round(aa / 6.0))
        old_base[k] = min(old_base.get(k, 9.0), p.z)
cut_coat_verts = {v.index for f in cut for v in f.verts}; bm.free()   # the old collar stays underneath (no holes): the new one is built outside it
print("coat: old back collar faces", len(cut), "lowest", round(min(old_base.values()), 3) if old_base else None)

# the shirt's own ragged collar beside and behind the neck goes (the front V stays)
bm = bmesh.new(); bm.from_mesh(sh.data); bm.faces.ensure_lookup_table(); cut = []
for f in bm.faces:
    c = SM @ f.calc_center_median(); r, a = polar(c)
    if c.z > 1.442 and r < 0.115 and abs(a) > S_END + 3: cut.append(f)
bmesh.ops.delete(bm, geom=cut, context='FACES'); bm.to_mesh(sh.data); bm.free(); sh.data.update()
print("shirt: old collar faces removed", len(cut))


def z_old_raw(a):
    k = int(round(a / 6.0)); vals = [old_base[j] for j in range(k - 2, k + 3) if j in old_base]
    return min(vals) if vals else None


def z_old(a):
    vals = [z_old_raw(a + d) for d in range(-18, 19, 6)]
    vals = [v for v in vals if v is not None]
    return sum(vals) / len(vals) - 0.004 if vals else None


def shirt_end(a): return ss(S_END, S_END + 16, abs(a))          # 0 at the collar points .. 1 behind them
def z_shirt_top(a): return 1.498 + 0.038 * ss(40, 125, abs(a)) - 0.008 * (1 - shirt_end(a))
def leaf_len(a): return LEAF + 0.020 * (1 - shirt_end(a))             # the leaf runs down into the collar points
def leaf_flare(a): return 0.005 + 0.010 * (1 - shirt_end(a))
def z_coat_top(a): return 1.474 + 0.042 * (ss(64, 125, abs(a)) - 0.12 * ss(150, 180, abs(a)))


def r_si(z, a):                     # shirt collar, inner surface
    return r_env(max(z, 1.47), a) + 0.003


def r_so(z, a):                     # shirt collar, outer surface (stand + folded leaf)
    zt = z_shirt_top(a); s = (zt - z) / leaf_len(a)
    if 0 <= s <= 1: return r_si(z, a) + 0.0065 + leaf_flare(a) * s
    return r_si(z, a) + 0.0045


def band(name, angles, profile_fn, mat_name):
    """closed per-angle profiles [(r, z, v)] swept around the neck; UV u along the band, v along the profile"""
    bmb = bmesh.new(); uvb = bmb.loops.layers.uv.new("UVMap"); cols = []
    for a in angles:
        cols.append([(bmb.verts.new(at_polar(r, a, z)), v) for r, z, v in profile_fn(a)])
    n = len(cols[0]); NJb = len(angles)
    for j in range(NJb - 1):
        for k in range(n):
            k2 = (k + 1) % n
            f = bmb.faces.new((cols[j][k][0], cols[j + 1][k][0], cols[j + 1][k2][0], cols[j][k2][0]))
            v2 = cols[j][k2][1] if k2 else 1.0
            for l, uvv in zip(f.loops, ((j, cols[j][k][1]), (j + 1, cols[j][k][1]), (j + 1, v2), (j, v2))):
                l[uvb].uv = (uvv[0] / (NJb - 1), uvv[1])
    for col in (cols[0], cols[-1]):
        cen = bmb.verts.new(sum((v.co for v, _ in col), Vector()) / n)
        for k in range(n):
            f = bmb.faces.new((cen, col[k][0], col[(k + 1) % n][0]))
            for l in f.loops: l[uvb].uv = (0.5, 0.3)
    bmesh.ops.recalc_face_normals(bmb, faces=bmb.faces)
    mesh = bpy.data.meshes.new(name); bmb.to_mesh(mesh); bmb.free()
    for poly in mesh.polygons: poly.use_smooth = True
    mesh.materials.append(bpy.data.materials.new(mat_name))
    ob = bpy.data.objects.new(name, mesh); bpy.context.scene.collection.objects.link(ob)
    return ob


def shirt_profile(a):
    zt = z_shirt_top(a); zb = min(1.445, zt - leaf_len(a) - 0.012); pts = []
    for k in range(7):                                   # inner face, bottom -> top
        z = zb + (zt - zb) * k / 6; pts.append((r_si(z, a), z, 0.62 + 0.38 * (1 - k / 6)))
    ri, ro = r_si(zt, a), r_so(zt - 1e-4, a)
    for k in range(1, 4):                                # rounded fold
        t = math.pi * k / 4; c = (ri + ro) / 2; rad = (ro - ri) / 2
        pts.append((c - rad * math.cos(t), zt + rad * 0.9 * math.sin(t), 0.62 - 0.12 * k / 4))
    for k in range(5):                                   # the folded leaf, top -> its lower edge
        z = zt - leaf_len(a) * k / 4; pts.append((r_so(z, a), z, 0.5 - 0.3 * k / 4))
    zl = zt - leaf_len(a)
    pts.append((r_si(zl, a) + 0.0045, zl + 0.0012, 0.17))    # under the leaf's edge
    for k in range(1, 4):                                # the stand below the leaf (hidden by the coat collar)
        z = zl - (zl - zb) * k / 3; pts.append((r_si(z, a) + 0.0045, z, 0.15 - 0.15 * k / 3))
    return pts


bvh_coat = bvh_world([coat])
COAT_PROF = []


def coat_profile(a):
    zt = z_coat_top(a)
    zo = z_old(a); zb = zo if zo is not None else zt - 0.05
    zb = min(max(zb, zt - 0.12 + 0.07 * (1 - ss(70, 100, abs(a)))), zt - 0.03)
    tap = ss(A_END, A_END + 14, abs(a))                 # the ends sink into the lapels
    zt = zb + (zt - zb) * (0.35 + 0.65 * tap)
    NZ = 9; zs = [zt - (zt - zb) * k / (NZ - 1) for k in range(NZ)]          # top -> bottom
    ci = [r_so(z, a) + 0.0025 for z in zs]
    co = []
    for k, z in enumerate(zs):
        r = ci[k] + 0.006 * (0.4 + 0.6 * tap) + 0.10 * (zt - z)
        rc, _ = outer_r(bvh_coat, z, a)
        if rc is not None and rc < ci[k] + 0.12: r = max(r, rc + 0.004)
        co.append(r)
    for k in range(1, NZ): co[k] = max(co[k], co[k - 1])                   # flares down, never pinches in
    co = [co[0]] + [(co[k - 1] + 2 * co[k] + co[k + 1]) / 4 for k in range(1, NZ - 1)] + [co[-1]]
    COAT_PROF.append((a, zt, zs, co))
    pts = []
    for k in range(NZ - 1, -1, -1):                      # outer face, bottom -> top
        pts.append((co[k], zs[k], 0.5 * (1 - k / (NZ - 1))))
    c = (co[0] + ci[0]) / 2; rad = (co[0] - ci[0]) / 2
    for k in range(1, 4):                                # the fold line
        t = math.pi * k / 4; pts.append((c + rad * math.cos(t), zt + rad * 0.8 * math.sin(t), 0.5 + 0.12 * k / 4))
    for k in range(NZ):                                  # inner face, top -> bottom
        pts.append((ci[k], zs[k], 0.62 + 0.38 * k / (NZ - 1)))
    return pts


def sweep(a0, a1, n):
    out = []
    for j in range(n):
        a = a0 + (a1 - a0) * j / (n - 1)
        out.append(a if a <= 180 else a - 360)
    return out


shirt_collar = band("ShirtCollar", sweep(S_END, 360 - S_END, 56), shirt_profile, "Prof_ShirtCollar")
collar = band("Collar", sweep(A_END, 360 - A_END, 46), coat_profile, "Prof_Collar")
print("ShirtCollar", len(shirt_collar.data.vertices), "Collar", len(collar.data.vertices), "verts")


def collar_outer(z, a):
    """outer radius of the new coat collar at (z, a) and its top height; None outside it"""
    if abs(a) < A_END: return None, None
    best = min(COAT_PROF, key=lambda e: abs(((e[0] - a + 180) % 360) - 180))
    _, zt, zs, co = best
    if z > zt: return co[0], zt
    if z < zs[-1]: return None, zt
    for k in range(len(zs) - 1):
        if zs[k + 1] <= z <= zs[k]:
            w = (zs[k] - z) / max(zs[k] - zs[k + 1], 1e-6); return co[k] * (1 - w) + co[k + 1] * w, zt
    return co[-1], zt


# the old collar (lime faces behind the lapels) is tucked under the new one wherever it would poke out
CMi = CM.inverted(); tuck = 0
for i in cut_coat_verts:
    v = coat.data.vertices[i]; p = CM @ v.co; r, a = polar(p)
    ro, zt = collar_outer(p.z, a)
    if ro is None: continue
    z = min(p.z, zt - 0.004)
    if r > ro - 0.0025 or p.z > zt - 0.004:
        v.co = CMi @ at_polar(min(r, ro - 0.0035), a, z); tuck += 1
coat.data.update()
print("old coat collar verts tucked under the new collar", tuck)

# ---- the amulet strap
bm = bmesh.new(); bm.from_mesh(med.data); bm.faces.ensure_lookup_table()
Z_CUT = 1.335
cut = [f for f in bm.faces if (MM @ f.calc_center_median()).z > Z_CUT]
bmesh.ops.delete(bm, geom=cut, context='FACES')
bm.verts.ensure_lookup_table()
ends = {}
for s in (1, -1):
    bd = [MM @ v.co for v in bm.verts if v.is_boundary and (MM @ v.co).z > Z_CUT - 0.03 and (MM @ v.co).x * s > 0.01]
    lo = [MM @ v.co for v in bm.verts if (MM @ v.co).x * s > 0.01 and Z_CUT - 0.06 < (MM @ v.co).z < Z_CUT - 0.035]
    if not bd: bd = [Vector((s * 0.045, -0.140, 1.33))]
    c = sum(bd, Vector()) / len(bd)
    tdir = (c - sum(lo, Vector()) / len(lo)).normalized() if lo else Vector((s * 0.022, 0.010, 0.04)).normalized()
    if tdir.z < 0.5: tdir = Vector((s * 0.022, 0.010, 0.04)).normalized()
    ends[s] = (c, tdir)
bm.to_mesh(med.data); bm.free(); med.data.update()
print("medallion: strap loop above", Z_CUT, "removed", len(cut), "faces; leg ends", {s: tuple(round(x, 3) for x in ends[s][0]) for s in ends})

ST_W, ST_T = 0.013, 0.0038
surf = bvh_world([coat, sh, collar, shirt_collar, med])


def z_strap(a): return 1.452 + 0.046 * ss(40, 150, abs(a))


def on_surface(z, a, lift=ST_T / 2 + 0.0012):
    r, n = outer_r(surf, z, a)
    if r is None: r, n = r_env(1.5, a) + 0.03, (at_polar(1, a, 0) - Vector((AX.x, AX.y, 0))).normalized()
    return at_polar(r + lift, a, z)


guide = []
c, t = ends[1]; guide += [c - t * 0.004, c + t * 0.022]
guide += [on_surface(z_strap(a), a) for a in (48, 62, 80, 100, 125, 152)]
guide += [on_surface(z_strap(180), 180)]
guide += [on_surface(z_strap(-a), -a) for a in (152, 125, 100, 80, 62, 48)]
c, t = ends[-1]; guide += [c + t * 0.022, c - t * 0.004]


def catmull(P, n_per):
    out = []
    for i in range(len(P) - 1):
        p0 = P[max(i - 1, 0)]; p1 = P[i]; p2 = P[i + 1]; p3 = P[min(i + 2, len(P) - 1)]
        for k in range(n_per):
            t = k / n_per; t2, t3 = t * t, t * t * t
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    out.append(P[-1]); return out


path = catmull(guide, 10)
L = [0.0]
for i in range(1, len(path)): L.append(L[-1] + (path[i] - path[i - 1]).length)
step = 0.006; res = []; j = 0
for k in range(int(L[-1] / step) + 1):
    s = k * step
    while j < len(L) - 2 and L[j + 1] < s: j += 1
    w = (s - L[j]) / max(L[j + 1] - L[j], 1e-9); res.append(path[j] * (1 - w) + path[j + 1] * w)
res.append(path[-1]); path = res


def push_out(P):
    out = []
    for i, p in enumerate(P):
        if i < 3 or i > len(P) - 4: out.append(p); continue          # the ends sit inside the leg stubs
        r, a = polar(p); ro, _ = outer_r(surf, p.z, a)
        need = (ro + ST_T / 2 + 0.0012) if ro is not None else r
        out.append(at_polar(max(r, need), a, p.z) if r < need else p)
    return out


for _ in range(10):
    path = push_out(path)
    for _k in range(2):
        path = [path[0]] + [(path[i - 1] + path[i] * 2 + path[i + 1]) / 4 for i in range(1, len(path) - 1)] + [path[-1]]
path = push_out(path)

stb = bmesh.new(); suv = stb.loops.layers.uv.new("UVMap")
c_ = 0.0012
ring_def = [(-ST_W / 2, 0.0, 0.0), (-ST_W / 2 + c_, ST_T / 2, 0.08), (ST_W / 2 - c_, ST_T / 2, 0.92), (ST_W / 2, 0.0, 1.0),
            (ST_W / 2 - c_, -ST_T / 2, 0.92), (-ST_W / 2 + c_, -ST_T / 2, 0.08)]
rings = []; Lacc = 0.0
for i, p in enumerate(path):
    T = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
    r, a = polar(p); _, n = outer_r(surf, p.z, a)
    if n is None: n = (at_polar(1, a, 0) - Vector((AX.x, AX.y, 0))).normalized()
    if i < 3 or i > len(path) - 4:                      # in the legs: face forward like the leg
        n = Vector((0, -1, 0.2)).normalized()
    B = T.cross(n).normalized(); N = B.cross(T).normalized()
    if i: Lacc += (p - path[i - 1]).length
    rings.append(([stb.verts.new(p + B * x + N * y) for x, y, _ in ring_def], Lacc))
for i in range(len(rings) - 1):
    (r0, l0), (r1, l1) = rings[i], rings[i + 1]
    for k in range(len(ring_def)):
        k2 = (k + 1) % len(ring_def)
        f = stb.faces.new((r0[k], r1[k], r1[k2], r0[k2]))
        for l, uvv in zip(f.loops, ((ring_def[k][2], l0), (ring_def[k][2], l1), (ring_def[k2][2], l1), (ring_def[k2][2], l0))):
            l[suv].uv = (uvv[0], uvv[1] / 0.06)
bmesh.ops.recalc_face_normals(stb, faces=stb.faces)
me = bpy.data.meshes.new("Strap"); stb.to_mesh(me); stb.free()
for poly in me.polygons: poly.use_smooth = True
me.materials.append(bpy.data.materials.new("Prof_Strap"))
strap = bpy.data.objects.new("Strap", me); bpy.context.scene.collection.objects.link(strap)
print("strap", len(path), "samples, length", round(Lacc, 3), "m")

# ---------------------------------------------------------------- 3. hair
hair = O["Hair"]; RM = hair.matrix_world
bm = bmesh.new(); bm.from_mesh(hair.data); bm.faces.ensure_lookup_table()
seen = set(); kill = []
for f in bm.faces:
    if f.index in seen: continue
    st = [f]; seen.add(f.index); comp = []
    while st:
        g = st.pop(); comp.append(g)
        for e in g.edges:
            for h in e.link_faces:
                if h.index not in seen: seen.add(h.index); st.append(h)
    area = sum(g.calc_area() for g in comp) or 1
    cy = sum((RM @ g.calc_center_median()).y * g.calc_area() for g in comp) / area
    if cy > 0.02: kill += comp
bmesh.ops.delete(bm, geom=kill, context='FACES')
bm.to_mesh(hair.data); bm.free(); hair.data.update()
print("hair: back helmet faces removed", len(kill))

C = Vector((0.0, 0.015, 1.653))       # skull centre (head group already dropped 12 mm in v5)
H = [(0, 1.760), (30, 1.754), (45, 1.738), (55, 1.718), (65, 1.693), (75, 1.668), (82, 1.636), (89, 1.624), (115, 1.618), (140, 1.596), (160, 1.574), (180, 1.566)]


def hairline(phi):
    phi = abs(phi)
    for (a0, h0), (a1, h1) in zip(H, H[1:]):
        if phi <= a1: return h0 + (h1 - h0) * (phi - a0) / (a1 - a0)
    return H[-1][1]


EAR = [Vector((s * 0.106, 0.027, 1.650)) for s in (1, -1)]


def cap_T(z):
    return 0.003 + 0.005 * ss(1.70, 1.80, z)


# 3a. thin scalp cap (the shade between the locks)
old = O["HairCap"]; mat_cap = old.data.materials[0]; bpy.data.objects.remove(old, do_unlink=True)


def phi_of(p): return math.degrees(math.atan2(abs(p.x), -p.y))


def inside(p):
    if p.z < hairline(phi_of(p)): return False
    for e in EAR:
        d = p - e; d.z *= 0.85
        if d.length < 0.047: return False
    return True


bm = bmesh.new(); bm.from_mesh(head.data); bm.faces.ensure_lookup_table()
keep = [f for f in bm.faces if f.material_index == 0 and inside(HM @ f.calc_center_median())]
cb = bmesh.new(); vmap = {}
for f in keep:
    vs = []
    for v in f.verts:
        if v not in vmap: vmap[v] = cb.verts.new(HM @ v.co)
        vs.append(vmap[v])
    try: cb.faces.new(vs)
    except ValueError: pass
cb.verts.ensure_lookup_table(); cb.normal_update()
dist = {v: 1e9 for v in cb.verts}; q = deque()
for v in cb.verts:
    if v.is_boundary: dist[v] = 0.0; q.append(v)
while q:
    v = q.popleft()
    for e in v.link_edges:
        w = e.other_vert(v); nd = dist[v] + e.calc_length()
        if nd < dist[w] - 1e-6: dist[w] = nd; q.append(w)
for v in cb.verts:
    p = v.co.copy(); n = v.normal.copy()
    w = ss(0.0, 0.02, dist[v])
    v.co = p + n * (0.0012 + (cap_T(p.z) - 0.0012) * w)
uv = cb.loops.layers.uv.new("UVMap")
for f in cb.faces:
    for l in f.loops:
        p = l.vert.co; ang = math.atan2(p.x, -p.y); el = math.atan2(p.z - 1.55, math.hypot(p.x, p.y))
        l[uv].uv = (ang / (2 * math.pi) + 0.5, el / (math.pi / 2))
me = bpy.data.meshes.new("HairCap"); cb.to_mesh(me); cb.free(); bm.free()
for poly in me.polygons: poly.use_smooth = True
me.materials.append(mat_cap)
cap = bpy.data.objects.new("HairCap", me); bpy.context.scene.collection.objects.link(cap)
print("cap", len(me.vertices), "verts")


# 3b. locks
def dir_of(phi, th):
    p, t = math.radians(phi), math.radians(th)
    return Vector((math.sin(p) * math.cos(t), -math.cos(p) * math.cos(t), math.sin(t)))


def angles_of(d):
    d = d.normalized()
    return math.degrees(math.atan2(d.x, -d.y)), math.degrees(math.asin(max(-1, min(1, d.z))))


def skull(d):
    """skull surface point + outward normal along direction d from C"""
    hit = head_bvh.ray_cast(C, d.normalized(), 0.5)
    if hit[0] is None: return C + d.normalized() * 0.1, d.normalized()
    n = hit[1] if hit[1].dot(d) > 0 else -hit[1]
    n = (n * 0.5 + d.normalized() * 0.5).normalized()          # smoother than the mesh normal
    return hit[0], n


def theta_for(phi, z):
    lo, hi = -60.0, 85.0
    for _ in range(30):
        mid = (lo + hi) / 2
        if skull(dir_of(phi, mid))[0].z < z: lo = mid
        else: hi = mid
    return (lo + hi) / 2


def slerp(a, b, t):
    a = a.normalized(); b = b.normalized(); om = math.acos(max(-1, min(1, a.dot(b))))
    if om < 1e-5: return a
    return (a * math.sin((1 - t) * om) + b * math.sin(t * om)) / math.sin(om)


def tip_z(phi):
    z = hairline(phi) - 0.008
    if 76 <= abs(phi) <= 120:                                 # over the ear: end above it
        z = hairline(phi) + (1.700 - hairline(phi)) * min(ss(72, 82, abs(phi)), 1 - ss(112, 124, abs(phi)))
    return z


lb = bmesh.new(); luv = lb.loops.layers.uv.new("UVMap")
SEG, RING = 13, 6
n_locks = 0


def lock(phiE, width, thick_max, layer, shorten=0.0, sway=0.0, start_jit=(0, 0), lift_amt=0.011, under=0.0):
    global n_locks
    zE = tip_z(phiE) + shorten
    dE = dir_of(phiE, theta_for(phiE, zE))
    hz = Vector((math.sin(math.radians(phiE)), -math.cos(math.radians(phiE)), 0))
    dS = (dir_of(start_jit[0], 78 + start_jit[1]) + hz * 0.22).normalized()
    rings = []
    for i in range(SEG + 1):
        t = i / SEG
        d = slerp(dS, dE, t ** 0.92)
        ph, th = angles_of(d); ph += sway * math.sin(math.pi * t); d = dir_of(ph, th)
        S, N = skull(d)
        w = width * (0.62 + 0.38 * ss(0.0, 0.3, t)) * (1 - ss(0.5, 1.0, t)) ** 0.75
        w = max(w, 0.0004)
        thk = min(0.42 * w, thick_max) + 0.001
        lift = lift_amt * (1 - ss(0.05, 0.7, t))
        h = cap_T(S.z) + lift + under + thk / 2 + 0.0008
        rings.append((S + N * h, N, w, thk, t))
    # below the widest point of the back of the head the lock falls (gravity) instead of hugging the
    # hollow under the occiput - only the tip comes back to the neck at the nape
    rh = [math.hypot(P.x - C.x, P.y - C.y) for P, *_ in rings]
    ip = max(range(len(rings)), key=lambda i: rh[i])
    for i in range(ip + 1, len(rings)):
        P, N, w, thk, t = rings[i]
        need = rh[ip] - 0.42 * (rings[ip][0].z - P.z)
        if rh[i] < need and rh[i] > 1e-4:
            k = need / rh[i]; P = Vector((C.x + (P.x - C.x) * k, C.y + (P.y - C.y) * k, P.z))
            rings[i] = (P, N, w, thk, t); rh[i] = need
    verts = []
    for i, (P, N, w, thk, t) in enumerate(rings):
        T = (rings[min(i + 1, SEG)][0] - rings[max(i - 1, 0)][0]).normalized()
        B = N.cross(T).normalized(); Nn = T.cross(B).normalized()
        ring = []
        for k in range(RING):
            al = 2 * math.pi * k / RING
            ring.append(lb.verts.new(P + B * (w / 2) * math.cos(al) + Nn * (thk / 2) * math.sin(al)))
        verts.append(ring)
    v0 = layer * 0.5
    for i in range(SEG):
        for k in range(RING):
            a0, a1 = verts[i][k], verts[i][(k + 1) % RING]; b0, b1 = verts[i + 1][k], verts[i + 1][(k + 1) % RING]
            f = lb.faces.new((a0, b0, b1, a1))
            u0, u1 = k / RING, (k + 1) / RING; t0, t1 = i / SEG, (i + 1) / SEG
            for l, (uu, vv) in zip(f.loops, ((u0, t0), (u0, t1), (u1, t1), (u1, t0))):
                l[luv].uv = (uu, v0 + 0.5 * vv * 0.98 + 0.005)
    c = lb.verts.new(rings[0][0]); r0 = verts[0]
    for k in range(RING):
        f = lb.faces.new((c, r0[k], r0[(k + 1) % RING]))
        for l in f.loops: l[luv].uv = (0.25, v0 + 0.006)
    n_locks += 1


for s in (1, -1):
    for pe in (56, 63):
        lock(s * pe, 0.032, 0.009, 0, shorten=0.0, sway=s * 4, start_jit=(s * 8, -4), lift_amt=0.008)
    for pe in (62, 70):
        lock(s * pe, 0.026, 0.008, 1, shorten=0.012, sway=s * 3, start_jit=(s * 4, -10), under=0.0075, lift_amt=0.010)
    for i, pe in enumerate(np.linspace(70, 176, 11)):
        lock(s * (pe + random.uniform(-2, 2)), 0.037 + random.uniform(-0.003, 0.003), 0.010, 0,
             shorten=random.uniform(-0.004, 0.007), sway=s * random.uniform(-5, 7), start_jit=(s * random.uniform(0, 14), random.uniform(-6, 3)))
    for i, pe in enumerate(np.linspace(78, 170, 8)):
        lock(s * (pe + random.uniform(-3, 3)), 0.027 + random.uniform(-0.003, 0.004), 0.008, 1,
             shorten=random.uniform(0.006, 0.026), sway=s * random.uniform(-4, 9), start_jit=(s * random.uniform(0, 16), random.uniform(-8, 0)), under=0.0075)
lock(180, 0.036, 0.010, 0, shorten=-0.002, sway=0)
lock(180, 0.026, 0.008, 1, shorten=0.014, sway=4, under=0.0075)
bmesh.ops.recalc_face_normals(lb, faces=lb.faces)
lb.normal_update()
me = bpy.data.meshes.new("HairLocks"); lb.to_mesh(me); lb.free()
for poly in me.polygons: poly.use_smooth = True
mat_l = bpy.data.materials.new("Prof_HairLocks"); me.materials.append(mat_l)
locks = bpy.data.objects.new("HairLocks", me); bpy.context.scene.collection.objects.link(locks)
# shading normals lean toward the head's own roundness: the locks read as one combed mass (and the
# hologram rim does not outline every lock), their relief stays in the silhouette and the texture
me.update()
cn = []
for v in me.vertices:
    rad = (v.co - C).normalized()
    cn.append((v.normal * 0.3 + rad * 0.7).normalized())
me.normals_split_custom_set_from_vertices(cn)
print("locks", n_locks, "verts", len(me.vertices), "tris", sum(len(p.vertices) - 2 for p in me.polygons))

# ---------------------------------------------------------------- textures
rng = np.random.default_rng(3)


def smooth1(x, k):
    ker = np.ones(k) / k
    return np.convolve(np.concatenate([x[-k:], x, x[:k]]), ker, 'same')[k:-k]


def locks_tex(Wt=256, Ht=512):
    img = np.zeros((Ht, Wt, 3))
    u = (np.arange(Wt) + 0.5) / Wt
    across = 0.74 + 0.26 * np.clip(np.cos(2 * np.pi * (u - 0.25)), -0.3, 1)
    for layer, base in ((0, np.array([186, 178, 177])), (1, np.array([206, 199, 197]))):
        streak = 1 + 0.05 * smooth1(rng.normal(0, 1, Wt), 9) + 0.025 * smooth1(rng.normal(0, 1, Wt), 3)
        for y in range(Ht // 2):
            t = (y + 0.5) / (Ht // 2)
            root = 0.74 + 0.26 * ss(0.0, 0.2, t)
            tip = 1 + 0.06 * ss(0.75, 1.0, t)
            row = base[None, :] / 255 * (across * streak * root * tip)[:, None]
            img[layer * (Ht // 2) + y] = row
    img = np.clip(img, 0, 1)
    return Image.fromarray((img[::-1] * 255).astype(np.uint8))      # image rows run top-down; UV v runs bottom-up


def cap_tex(Wt=256, Ht=256):
    base = np.array([166, 159, 158]) / 255
    streak = 1 + 0.10 * smooth1(rng.normal(0, 1, Wt), 3)
    img = np.tile((base[None, :] * streak[:, None])[None], (Ht, 1, 1))
    return Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8))


def collar_tex(Wt=64, Ht=128):
    lime = np.array([140, 184, 42]) / 255
    img = np.zeros((Ht, Wt, 3))
    for y in range(Ht):
        v = (y + 0.5) / Ht
        if v < 0.5: k = 0.82 + 0.2 * ss(0.0, 0.5, v)                     # outer: darker where it meets the coat
        elif v < 0.62: k = 1.04                                          # the fold
        else: k = 0.78 - 0.18 * ss(0.62, 1.0, v)                         # inside, in shade
        img[y] = lime * k
    return Image.fromarray((np.clip(img[::-1], 0, 1) * 255).astype(np.uint8))


def shirt_collar_tex(Wt=64, Ht=128):
    base = np.array([50, 66, 54]) / 255; img = np.zeros((Ht, Wt, 3))
    for y in range(Ht):
        v = (y + 0.5) / Ht
        if v < 0.17: k = 0.7                                             # stand, under the coat collar
        elif v < 0.5: k = 0.9 + 0.22 * ss(0.17, 0.5, v)                  # the leaf, lighter toward the fold
        elif v < 0.62: k = 1.2                                           # the fold
        else: k = 0.85 - 0.25 * ss(0.62, 1.0, v)                         # inside
        img[y] = base * k
        if 0.2 < v < 0.215: img[y] = base * 1.35                         # stitch line along the leaf edge
    return Image.fromarray((np.clip(img[::-1], 0, 1) * 255).astype(np.uint8))


def strap_tex(Wt=64, Ht=64):
    base = np.array([112, 68, 44]) / 255; img = np.zeros((Ht, Wt, 3))
    grain = 1 + 0.05 * rng.normal(0, 1, (Ht, Wt))
    for x in range(Wt):
        u = (x + 0.5) / Wt
        k = 0.78 + 0.3 * math.sin(math.pi * min(1, max(0, (u - 0.04) / 0.92)))
        img[:, x] = base * k
        if 0.17 < u < 0.21 or 0.79 < u < 0.83:                           # stitching
            for y in range(Ht):
                if (y // 4) % 2 == 0: img[y, x] = np.array([196, 170, 130]) / 255
    img *= grain[..., None]
    return Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8))


for name, im in (("Prof_HairLocks", locks_tex()), ("Prof_HairCap", cap_tex()), ("Prof_Collar", collar_tex()),
                 ("Prof_ShirtCollar", shirt_collar_tex()), ("Prof_Strap", strap_tex())):
    path = os.path.join(TEX, name + "_BaseColor.png"); im.save(path)
    m = bpy.data.materials.get(name)
    m.use_nodes = True; nt = m.node_tree
    bsdf = [n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED'][0]
    for n in [n for n in nt.nodes if n.type == 'TEX_IMAGE']: nt.nodes.remove(n)
    tn = nt.nodes.new('ShaderNodeTexImage'); tn.image = bpy.data.images.load(path, check_existing=False)
    nt.links.new(tn.outputs['Color'], bsdf.inputs['Base Color'])
    bsdf.inputs['Roughness'].default_value = 0.7
HAIR_PNG = os.path.join(_here, "Prof_Hair_BaseColor_orig.png")     # the crest texture BEFORE the lift (shipped next to the script)
src = np.asarray(Image.open(HAIR_PNG).convert('RGB')).astype(float)
lum = src.mean(axis=2, keepdims=True)
lift = np.clip(1 - lum / 190.0, 0, 1) * 0.45
out = src + (np.array([196, 188, 186]) - src) * lift
hp = os.path.join(TEX, "Prof_Hair_BaseColor.png"); Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(hp)
mh = bpy.data.materials["Prof_Hair"]
for n in mh.node_tree.nodes:
    if n.type == 'TEX_IMAGE': n.image = bpy.data.images.load(hp, check_existing=False)
for kb in keys:
    if kb.name != "Basis": kb.value = 0.0
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath("professor_v8.blend"))
