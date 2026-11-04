"""Hand tools for Biology VR 2 (03.11): the bio-scanner (left hand) and the BioTool (right hand).
Blender z-up, the tools point to -Y (the front), origin = the hand pivot. Every part is its own object with its
origin at its centre (spinners turn about their own Y axis). Names: <Tool>_<Part>_<Mat>, Mat = Body | Dark | Glow | Glass.
-> tools.blend, Tools.fbx"""
import bpy, bmesh, math, os
from mathutils import Vector, Matrix

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene


def obj(name, bm, loc=(0, 0, 0), smooth=True, sub=0, bevel=0.0, seg=3):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); sc.collection.objects.link(o); o.location = loc
    bpy.context.view_layer.objects.active = o
    if bevel > 0:
        m = o.modifiers.new("b", 'BEVEL'); m.width = bevel; m.segments = seg; m.limit_method = 'ANGLE'
    if sub:
        m = o.modifiers.new("s", 'SUBSURF'); m.levels = sub; m.render_levels = sub
    for m in list(o.modifiers):
        with bpy.context.temp_override(object=o): bpy.ops.object.modifier_apply(modifier=m.name)
    for p in o.data.polygons: p.use_smooth = smooth
    return o


def box(sx, sy, sz, taper=1.0):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=2.0)            # half extents
    for v in bm.verts:
        k = taper if v.co.y < 0 else 1.0                     # the front (-Y) end narrower
        v.co = Vector((v.co.x * sx * k, v.co.y * sy, v.co.z * sz * (0.5 + 0.5 * k)))
    return bm


def cyl(r, length, seg=18, r2=None, axis='Y', cap=True):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=cap, cap_tris=False, segments=seg, radius1=r, radius2=r2 if r2 is not None else r, depth=length)
    if axis == 'Y': bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(90), 3, 'X'))
    return bm


def arc_ring(R, r, a0, a1, seg=24, rseg=6, axis='Y'):
    """a torus arc (degrees a0..a1) around the Y axis"""
    bm = bmesh.new(); rings = []
    for i in range(seg + 1):
        a = math.radians(a0 + (a1 - a0) * i / seg)
        c = Vector((math.cos(a) * R, 0, math.sin(a) * R)); radial = c.normalized()
        ring = []
        for k in range(rseg):
            t = 2 * math.pi * k / rseg
            ring.append(bm.verts.new(c + radial * math.cos(t) * r + Vector((0, 1, 0)) * math.sin(t) * r))
        rings.append(ring)
    for i in range(seg):
        for k in range(rseg):
            bm.faces.new((rings[i][k], rings[i + 1][k], rings[i + 1][(k + 1) % rseg], rings[i][(k + 1) % rseg]))
    for ring in (rings[0], rings[-1]):
        bm.faces.new(ring if ring is rings[-1] else list(reversed(ring)))
    bm.normal_update(); bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def octa(r, ry):
    bm = bmesh.new()
    pts = [Vector((r, 0, 0)), Vector((-r, 0, 0)), Vector((0, 0, r)), Vector((0, 0, -r)), Vector((0, -ry, 0)), Vector((0, ry * 0.6, 0))]
    vs = [bm.verts.new(p) for p in pts]
    for a, b in ((0, 2), (2, 1), (1, 3), (3, 0)):
        bm.faces.new((vs[a], vs[b], vs[4])); bm.faces.new((vs[b], vs[a], vs[5]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def wedge(w, l, h):
    bm = bmesh.new()
    p = [(-w, 0, 0), (w, 0, 0), (0, -l, 0), (0, 0, h)]
    v = [bm.verts.new(x) for x in p]
    for f in ((0, 1, 2), (0, 2, 3), (1, 3, 2), (0, 3, 1)): bm.faces.new([v[i] for i in f])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def tube(pts, r0, r1, seg=8, cap=True):
    """a smooth tapered tube along a polyline (radius r0 -> r1), ends capped"""
    bm = bmesh.new(); rings = []; n = len(pts)
    pts = [Vector(p) for p in pts]
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
        a = Vector((0, 0, 1)) if abs(t.z) < 0.9 else Vector((1, 0, 0))
        u = t.cross(a).normalized(); v = t.cross(u).normalized()
        r = r0 + (r1 - r0) * i / (n - 1)
        rings.append([bm.verts.new(p + (u * math.cos(2 * math.pi * k / seg) + v * math.sin(2 * math.pi * k / seg)) * r) for k in range(seg)])
    for i in range(n - 1):
        for k in range(seg):
            bm.faces.new((rings[i][k], rings[i + 1][k], rings[i + 1][(k + 1) % seg], rings[i][(k + 1) % seg]))
    if cap:
        bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])
    bm.normal_update(); bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def bead(r, seg=10):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=seg // 2 + 1, radius=r)
    return bm


def claw(R0, R1, y0, y1, bend, k_ang):
    """a curved prong from radius R0 at y0 to R1 at y1 (front = -Y), bulging out by bend"""
    a = math.radians(k_ang); d = Vector((math.cos(a), 0, math.sin(a))); pts = []
    for i in range(7):
        t = i / 6; r = R0 + (R1 - R0) * t + bend * math.sin(math.pi * t)
        pts.append(d * r + Vector((0, y0 + (y1 - y0) * t, 0)))
    return pts, d * R1 + Vector((0, y1, 0))


def grip(h, w, d):
    """a short rounded pistol grip below the origin (top at z = 0), finger ridges on the front"""
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=2.0)
    for v in bm.verts:
        z = (v.co.z - 1) * 0.5 * h                               # 0 .. -h
        k = 1.0 - 0.18 * (-z / h)                                # slightly narrower at the bottom
        v.co = Vector((v.co.x * w * k, v.co.y * d, z))
    bmesh.ops.subdivide_edges(bm, edges=[e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > 1e-4], cuts=5, use_grid_fill=True)
    for v in bm.verts:
        if v.co.y < -d * 0.6:                                    # finger ridges on the front face
            v.co.y -= 0.0018 * (0.5 + 0.5 * math.cos(-v.co.z / h * 3 * 2 * math.pi))
    return bm


# ================================================================ scanner (left hand)
S = "Scanner_"
obj(S + "Body_Body", box(0.020, 0.058, 0.017, taper=0.86), (0, -0.012, 0.0), sub=1, bevel=0.006)
obj(S + "Nose_Dark", cyl(0.0215, 0.022, r2=0.019), (0, -0.078, 0.001), bevel=0.003, seg=2)
obj(S + "Lens_Glow", cyl(0.0125, 0.004), (0, -0.0895, 0.001), bevel=0.0015, seg=2)
obj(S + "Ring_Glow", arc_ring(0.0175, 0.0024, 0, 100, 9) , (0, -0.0905, 0.001))
ring_b = obj(S + "RingB_Glow", arc_ring(0.0175, 0.0024, 120, 220, 9), (0, -0.0905, 0.001))
ring_c = obj(S + "RingC_Glow", arc_ring(0.0175, 0.0024, 240, 340, 9), (0, -0.0905, 0.001))
for k in range(3):
    pts, tipp = claw(0.0205, 0.0185, -0.072, -0.097, 0.0035, 90 + 120 * k)
    obj(S + f"Prong{k}_Dark", tube(pts, 0.0026, 0.0012, seg=8), smooth=True)
    obj(S + f"Bead{k}_Glow", bead(0.0021), tuple(tipp))
g = obj(S + "Grip_Dark", grip(0.056, 0.0125, 0.0155), (0, 0.020, -0.012), sub=1)
g.rotation_euler = (math.radians(17), 0, 0)
obj(S + "GripLight_Glow", box(0.0016, 0.0012, 0.019), (0, 0.0442, -0.036), bevel=0.0007, seg=1)
bpy.data.objects[S + "GripLight_Glow"].rotation_euler = (math.radians(17), 0, 0)
obj(S + "Trigger_Dark", box(0.004, 0.006, 0.012), (0, -0.006, -0.028), sub=1, bevel=0.002)
obj(S + "Screen_Dark", box(0.016, 0.026, 0.002), (0, 0.010, 0.0185), sub=1, bevel=0.0015)
bpy.data.objects[S + "Screen_Dark"].rotation_euler = (math.radians(-12), 0, 0)
for s in (1, -1):
    obj(S + f"Strip{'L' if s > 0 else 'R'}_Glow", box(0.0012, 0.034, 0.0022), (s * 0.0195, -0.03, 0.004), sub=0, bevel=0.0008, seg=1)
obj(S + "Cap_Dark", cyl(0.016, 0.01, r2=0.012), (0, 0.048, 0.0), bevel=0.003, seg=2)
obj(S + "BackRing_Glow", arc_ring(0.0118, 0.0011, 0, 360, 28, 6), (0, 0.0535, 0.0))
obj(S + "Led_Glow", cyl(0.0022, 0.002, axis='Z'), (0, 0.032, 0.0175))

# ================================================================ BioTool (right hand)
B = "BioTool_"
obj(B + "Body_Body", cyl(0.0175, 0.075, seg=18, r2=0.0165), (0, 0.012, 0.0), bevel=0.004, seg=3)
obj(B + "Cap_Dark", cyl(0.0185, 0.012, seg=18, r2=0.015), (0, 0.054, 0.0), bevel=0.003, seg=2)
obj(B + "BackRing_Glow", arc_ring(0.0112, 0.0011, 0, 360, 28, 6), (0, 0.0605, 0.0))
obj(B + "Band_Dark", cyl(0.0182, 0.008, seg=18), (0, -0.018, 0.0), bevel=0.002, seg=2)
obj(B + "Glass_Glass", cyl(0.0145, 0.034, seg=18, cap=False), (0, -0.039, 0.0))
obj(B + "Core_Glow", cyl(0.0055, 0.03, seg=12), (0, -0.039, 0.0), bevel=0.002, seg=2)
obj(B + "Collar_Dark", cyl(0.0185, 0.009, seg=18, r2=0.016), (0, -0.0605, 0.0), bevel=0.0025, seg=2)
obj(B + "CoilA_Glow", arc_ring(0.0195, 0.0022, 0, 270, 20), (0, -0.031, 0.0))
obj(B + "CoilB_Glow", arc_ring(0.0195, 0.0022, 30, 300, 20), (0, -0.047, 0.0))
obj(B + "Emitter_Glow", octa(0.0085, 0.017), (0, -0.078, 0.0), smooth=False)
for k in range(3):
    pts, tipp = claw(0.0175, 0.0075, -0.063, -0.088, 0.006, 90 + 120 * k)
    obj(B + f"Prong{k}_Dark", tube(pts, 0.0022, 0.0011, seg=8), smooth=True)
    obj(B + f"Bead{k}_Glow", bead(0.0018), tuple(tipp))
g = obj(B + "Grip_Dark", grip(0.056, 0.0125, 0.0155), (0, 0.028, -0.012), sub=1)
g.rotation_euler = (math.radians(17), 0, 0)
obj(B + "GripLight_Glow", box(0.0016, 0.0012, 0.019), (0, 0.0522, -0.036), bevel=0.0007, seg=1)
bpy.data.objects[B + "GripLight_Glow"].rotation_euler = (math.radians(17), 0, 0)
obj(B + "Trigger_Dark", box(0.004, 0.006, 0.012), (0, 0.004, -0.026), sub=1, bevel=0.002)
obj(B + "Screen_Dark", box(0.014, 0.022, 0.002), (0, 0.022, 0.0185), sub=1, bevel=0.0015)
for s in (1, -1):
    obj(B + f"Strip{'L' if s > 0 else 'R'}_Glow", box(0.0012, 0.026, 0.0022), (s * 0.017, 0.018, 0.004), bevel=0.0008, seg=1)

for o in sc.objects:
    o.select_set(False)
here = os.path.dirname(os.path.abspath(__file__))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(here, "tools.blend"))
bpy.ops.export_scene.fbx(filepath=os.path.join(here, "Tools.fbx"), use_selection=False, object_types={'MESH'}, apply_unit_scale=True,
                         apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
tot = 0
for o in sc.objects:
    t = sum(len(p.vertices) - 2 for p in o.data.polygons); tot += t
print("parts", len(sc.objects), "tris", tot)
