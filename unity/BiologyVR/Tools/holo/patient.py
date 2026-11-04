"""Patient hologram for the prologue lab (03.11): a smooth human figure (skin-modifier mannequin), the
circulatory system (arteries red, veins blue - UV.x = metres along the vessel from the heart for the
flow pulses), the heart and two faint lungs. Blender z-up, the figure faces -Y, feet at z = 0.
-> patient.blend, Patient.fbx (meshes: PatientBody, PatientArteries, PatientVeins, PatientHeart, PatientLungs)"""
import bpy, bmesh, math, os
from mathutils import Vector, Matrix

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene


def link(o): sc.collection.objects.link(o); return o


# ---------------------------------------------------------------- body (skin modifier)
J = {  # name: (x, y, z, rx, ry)
    "pelvis": (0, 0.005, 0.96, 0.150, 0.100), "waist": (0, 0.0, 1.10, 0.130, 0.090), "chest": (0, -0.008, 1.26, 0.160, 0.105),
    "upper": (0, 0.0, 1.39, 0.172, 0.098), "neck0": (0, 0.012, 1.48, 0.060, 0.058), "neck1": (0, 0.014, 1.54, 0.054, 0.054),
    "head0": (0, 0.0, 1.61, 0.084, 0.096), "head1": (0, 0.005, 1.695, 0.090, 0.102), "head2": (0, 0.012, 1.765, 0.062, 0.068),
}
for s, n in ((1, "L"), (-1, "R")):
    J["sho" + n] = (s * 0.200, 0.005, 1.415, 0.062, 0.058)
    J["bic" + n] = (s * 0.232, 0.012, 1.29, 0.052, 0.050)
    J["elb" + n] = (s * 0.262, 0.018, 1.155, 0.044, 0.042)
    J["frm" + n] = (s * 0.285, 0.01, 1.05, 0.042, 0.038)
    J["wri" + n] = (s * 0.312, 0.0, 0.93, 0.032, 0.024)
    J["pal" + n] = (s * 0.326, -0.006, 0.86, 0.040, 0.018)
    J["han" + n] = (s * 0.332, -0.01, 0.775, 0.028, 0.013)
    J["hip" + n] = (s * 0.092, 0.008, 0.89, 0.092, 0.090)
    J["thi" + n] = (s * 0.098, 0.0, 0.72, 0.078, 0.078)
    J["kne" + n] = (s * 0.102, -0.006, 0.505, 0.056, 0.056)
    J["cal" + n] = (s * 0.104, 0.012, 0.36, 0.056, 0.058)
    J["ank" + n] = (s * 0.106, 0.012, 0.09, 0.036, 0.036)
    J["toe" + n] = (s * 0.112, -0.105, 0.035, 0.040, 0.022)
E = [("pelvis", "waist"), ("waist", "chest"), ("chest", "upper"), ("upper", "neck0"), ("neck0", "neck1"), ("neck1", "head0"), ("head0", "head1"), ("head1", "head2")]
for n in ("L", "R"):
    E += [("upper", "sho" + n), ("sho" + n, "bic" + n), ("bic" + n, "elb" + n), ("elb" + n, "frm" + n), ("frm" + n, "wri" + n), ("wri" + n, "pal" + n), ("pal" + n, "han" + n),
          ("pelvis", "hip" + n), ("hip" + n, "thi" + n), ("thi" + n, "kne" + n), ("kne" + n, "cal" + n), ("cal" + n, "ank" + n), ("ank" + n, "toe" + n)]
names = list(J)
me = bpy.data.meshes.new("PatientBody")
me.from_pydata([J[k][:3] for k in names], [(names.index(a), names.index(b)) for a, b in E], [])
body = link(bpy.data.objects.new("PatientBody", me))
sk = body.modifiers.new("skin", 'SKIN'); sk.use_smooth_shade = True
sv = me.skin_vertices[0].data
for i, k in enumerate(names):
    sv[i].radius = (J[k][3], J[k][4])
sv[names.index("pelvis")].use_root = True
sub = body.modifiers.new("sub", 'SUBSURF'); sub.levels = 2; sub.render_levels = 2
bpy.context.view_layer.objects.active = body; body.select_set(True)
bpy.ops.object.modifier_apply(modifier="skin"); bpy.ops.object.modifier_apply(modifier="sub")
sm = body.modifiers.new("smooth", 'SMOOTH'); sm.factor = 0.5; sm.iterations = 3
bpy.ops.object.modifier_apply(modifier="smooth")
for p in body.data.polygons: p.use_smooth = True
print("body", len(body.data.vertices), "verts", len(body.data.polygons), "faces")

# ---------------------------------------------------------------- vessels (tubes, UV.x = metres from the heart)
HEART = Vector((0.022, -0.03, 1.29))


def tube(name, paths, r0, r1, sides=5):
    bm = bmesh.new(); uv = bm.loops.layers.uv.new("UVMap")
    for pts, start_len in paths:
        P = [Vector(p) for p in pts]
        # resample along a Catmull-Rom spline
        S = []
        for i in range(len(P) - 1):
            p0 = P[max(i - 1, 0)]; p1 = P[i]; p2 = P[i + 1]; p3 = P[min(i + 2, len(P) - 1)]
            for k in range(6):
                t = k / 6; t2, t3 = t * t, t * t * t
                S.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
        S.append(P[-1])
        L = [start_len]
        for i in range(1, len(S)): L.append(L[-1] + (S[i] - S[i - 1]).length)
        total = L[-1] - start_len
        rings = []
        up = Vector((0, 0, 1))
        for i, p in enumerate(S):
            T = (S[min(i + 1, len(S) - 1)] - S[max(i - 1, 0)]).normalized()
            a = up if abs(T.dot(up)) < 0.9 else Vector((1, 0, 0))
            B = T.cross(a).normalized(); N = B.cross(T).normalized()
            f = (L[i] - start_len) / max(total, 1e-6)
            r = r0 + (r1 - r0) * f
            rings.append([bm.verts.new(p + (B * math.cos(2 * math.pi * k / sides) + N * math.sin(2 * math.pi * k / sides)) * r) for k in range(sides)])
        for i in range(len(rings) - 1):
            for k in range(sides):
                k2 = (k + 1) % sides
                fc = bm.faces.new((rings[i][k], rings[i + 1][k], rings[i + 1][k2], rings[i][k2]))
                for l, (u, v) in zip(fc.loops, ((L[i], k / sides), (L[i + 1], k / sides), (L[i + 1], (k + 1) / sides), (L[i], (k + 1) / sides))):
                    l[uv].uv = (u, v)
    bm.normal_update()
    m = bpy.data.meshes.new(name); bm.to_mesh(m); bm.free()
    for p in m.polygons: p.use_smooth = True
    return link(bpy.data.objects.new(name, m))


def mirror(path, s): return [(s * abs(x) if x != 0 else 0, y, z) if True else None for x, y, z in path]


arch = [HEART, (0.012, -0.022, 1.345), (0.0, -0.012, 1.395), (-0.012, 0.004, 1.405), (0.0, 0.02, 1.39), (0.012, 0.03, 1.33)]
desc = [(0.012, 0.03, 1.33), (0.012, 0.032, 1.20), (0.008, 0.028, 1.06), (0.0, 0.02, 0.975)]
A = []
A.append((arch + desc[1:], 0.0))
L_arch = 0.27  # approx length to the branch points (for continuous pulses)
for s in (1, -1):
    A.append(([(s * 0.004, -0.008, 1.40), (s * 0.03, -0.012, 1.48), (s * 0.034, -0.016, 1.56), (s * 0.03, -0.01, 1.64), (s * 0.026, 0.0, 1.71)], 0.16))      # carotid
    A.append(([(s * 0.006, -0.004, 1.402), (s * 0.08, -0.006, 1.425), (s * 0.15, 0.0, 1.425), (s * 0.20, 0.008, 1.385), (s * 0.235, 0.014, 1.27),
               (s * 0.262, 0.012, 1.155), (s * 0.288, 0.004, 1.04), (s * 0.312, -0.008, 0.93), (s * 0.33, -0.014, 0.80)], 0.16))                                  # subclavian -> radial
    A.append(([(0.0, 0.02, 0.975), (s * 0.055, 0.012, 0.93), (s * 0.085, -0.008, 0.82), (s * 0.095, -0.012, 0.66), (s * 0.10, 0.004, 0.50),
               (s * 0.102, 0.012, 0.30), (s * 0.104, 0.012, 0.11), (s * 0.106, -0.04, 0.045), (s * 0.108, -0.08, 0.035)], 0.78))                              # iliac -> femoral -> foot
art = tube("PatientArteries", A, 0.0085, 0.0045)
V = []
VH = Vector((-0.02, -0.02, 1.30))
V.append(([VH, (-0.022, -0.005, 1.36), (-0.02, 0.0, 1.41)], 0.0))                                                    # superior vena cava
V.append(([VH, (-0.02, 0.02, 1.22), (-0.018, 0.035, 1.08), (-0.012, 0.03, 0.98)], 0.0))                             # inferior vena cava
for s in (1, -1):
    V.append(([(-0.02, 0.0, 1.41), (s * 0.045, 0.004, 1.47), (s * 0.05, 0.004, 1.56), (s * 0.045, 0.01, 1.64), (s * 0.04, 0.02, 1.71)], 0.08))             # jugular
    V.append(([(-0.02, 0.0, 1.41), (s * 0.09, 0.006, 1.415), (s * 0.155, 0.014, 1.41), (s * 0.20, 0.022, 1.375), (s * 0.245, 0.028, 1.26),
               (s * 0.272, 0.03, 1.15), (s * 0.298, 0.022, 1.03), (s * 0.322, 0.012, 0.93), (s * 0.338, 0.004, 0.81)], 0.08))                              # arm vein
    V.append(([(-0.012, 0.03, 0.98), (s * 0.06, 0.026, 0.93), (s * 0.092, 0.006, 0.82), (s * 0.103, 0.004, 0.66), (s * 0.112, 0.02, 0.50),
               (s * 0.114, 0.026, 0.30), (s * 0.116, 0.026, 0.11), (s * 0.118, -0.03, 0.05)], 0.33))                                                       # leg vein
veins = tube("PatientVeins", V, 0.008, 0.005)
print("arteries", len(art.data.polygons), "veins", len(veins.data.polygons))

# ---------------------------------------------------------------- heart (a deformed sphere: two lobes, an apex)
hb_ = bmesh.new(); bmesh.ops.create_uvsphere(hb_, u_segments=28, v_segments=18, radius=1.0)
for v in hb_.verts:
    x, y, z = v.co
    if z < 0: k = 1 + z * 0.78; x *= k; y *= k                       # tapers to the apex
    else:
        x *= 1.0 + 0.22 * z
        z -= 0.42 * math.exp(-(x * x) / 0.05) * z * z                  # the notch between the two lobes
    y *= 0.78
    v.co = Vector((x * 0.050, y * 0.050, z * 0.058))
hm = bpy.data.meshes.new("PatientHeart"); hb_.to_mesh(hm); hb_.free()
heart = link(bpy.data.objects.new("PatientHeart", hm))
heart.matrix_world = Matrix.Translation(HEART) @ Matrix.Rotation(math.radians(-25), 4, 'Y') @ Matrix.Rotation(math.radians(10), 4, 'X')
for p in heart.data.polygons: p.use_smooth = True
print("heart", len(heart.data.vertices), "verts")

# ---------------------------------------------------------------- lungs
lb = bmesh.new()
for s in (1, -1):
    m_ = Matrix.Translation((s * 0.075, 0.012, 1.30)) @ Matrix.Diagonal((0.055, 0.052, 0.115, 1))
    bmesh.ops.create_uvsphere(lb, u_segments=16, v_segments=10, radius=1.0, matrix=m_)
lm = bpy.data.meshes.new("PatientLungs"); lb.to_mesh(lm); lb.free()
for p in lm.polygons: p.use_smooth = True
lungs = link(bpy.data.objects.new("PatientLungs", lm))

# apply transforms
for o in (body, art, veins, heart, lungs):
    o.data.transform(o.matrix_world); o.matrix_world = Matrix.Identity(4)
# the heart beats about its own centre
hc = sum((v.co for v in heart.data.vertices), Vector()) / len(heart.data.vertices)
heart.data.transform(Matrix.Translation(-hc)); heart.location = hc
# tiny markers for the builder: the entry point on the right forearm artery (the patient's right = -X)
mk = bmesh.new(); bmesh.ops.create_icosphere(mk, subdivisions=1, radius=0.004)
mm = bpy.data.meshes.new("PatientEntryMark"); mk.to_mesh(mm); mk.free()
entry = link(bpy.data.objects.new("PatientEntryMark", mm)); entry.location = (-0.29, 0.0, 1.04)
here = os.path.dirname(os.path.abspath(__file__))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(here, "patient.blend"))
bpy.ops.export_scene.fbx(filepath=os.path.join(here, "Patient.fbx"), use_selection=False, object_types={'MESH'}, apply_unit_scale=True,
                         apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
for o in (body, art, veins, heart, lungs, entry): print(o.name, len(o.data.vertices), "verts", sum(len(p.vertices) - 2 for p in o.data.polygons), "tris", tuple(round(c, 3) for c in o.location))
