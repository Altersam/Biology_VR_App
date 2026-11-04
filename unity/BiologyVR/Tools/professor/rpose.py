"""Perspective renders around the professor at the player's eye height; optional head yaw/pitch about the
neck-base pivot (as Bvr2Professor does in the game). usage: blender-python rpose.py -- file tag yaw pitch"""
import bpy, math, mathutils, os, sys
a = sys.argv[sys.argv.index('--')+1:]; src, tag = a[0], a[1]; yaw = float(a[2]) if len(a) > 2 else 0; pitch = float(a[3]) if len(a) > 3 else 0
bpy.ops.wm.open_mainfile(filepath=os.path.abspath(src))
sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=14; sc.cycles.use_denoising=False
w=bpy.data.worlds.new("W"); sc.world=w; w.use_nodes=True; w.node_tree.nodes["Background"].inputs[0].default_value=(0.42,0.45,0.5,1)
l=bpy.data.lights.new("S","SUN"); l.energy=3.0; lo=bpy.data.objects.new("S",l); sc.collection.objects.link(lo); lo.rotation_euler=(math.radians(50),0,math.radians(-30))
l2=bpy.data.lights.new("F","SUN"); l2.energy=1.3; lo2=bpy.data.objects.new("F",l2); sc.collection.objects.link(lo2); lo2.rotation_euler=(math.radians(60),0,math.radians(160))
O=bpy.data.objects
for o in O:
    if o.type=='MESH' and o.data.shape_keys:
        for k in o.data.shape_keys.key_blocks:
            if k.name!='Basis': k.value=0
head=O["Head"]; M=head.matrix_world
minz=min((M@v.co).z for v in head.data.vertices)
pivot=mathutils.Vector((0,0.012,minz+0.012))
R=(mathutils.Matrix.Translation(pivot) @ mathutils.Matrix.Rotation(math.radians(yaw),4,'Z') @ mathutils.Matrix.Rotation(math.radians(pitch),4,'X') @ mathutils.Matrix.Translation(-pivot))
for n in ("Head","Hair","HairCap","Glasses","Beard","Mustache"):
    if n in O: O[n].matrix_world = R @ O[n].matrix_world
cam=bpy.data.cameras.new("C"); cam.lens=50; co=bpy.data.objects.new("C",cam); sc.collection.objects.link(co); sc.camera=co
sc.render.resolution_x, sc.render.resolution_y = 420, 520
target=mathutils.Vector((0,0,1.5))
for i,ang in enumerate([0,45,90,135,180,225,270,315]):
    t=math.radians(ang); d=mathutils.Vector((math.sin(t), -math.cos(t), 0))
    co.location=target+d*1.1+mathutils.Vector((0,0,0.15))
    co.rotation_euler=(target-co.location).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=os.path.abspath(f"renders/{tag}_a{ang:03d}.png"); bpy.ops.render.render(write_still=True)
