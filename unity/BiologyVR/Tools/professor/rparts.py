import bpy, math, mathutils, os, sys
a = sys.argv[sys.argv.index('--')+1:]; src, tag = a[0], a[1]
hide = os.environ.get("HIDE","").split(",")
bpy.ops.wm.open_mainfile(filepath=os.path.abspath(src))
sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=12; sc.cycles.use_denoising=False
w=bpy.data.worlds.new("W"); sc.world=w; w.use_nodes=True; w.node_tree.nodes["Background"].inputs[0].default_value=(0.42,0.45,0.5,1)
l=bpy.data.lights.new("S","SUN"); l.energy=3.0; lo=bpy.data.objects.new("S",l); sc.collection.objects.link(lo); lo.rotation_euler=(math.radians(50),0,math.radians(-30))
for n in hide:
    if n in bpy.data.objects: bpy.data.objects[n].hide_render=True
cam=bpy.data.cameras.new("C"); cam.type='ORTHO'; cam.ortho_scale=0.45; co=bpy.data.objects.new("C",cam); sc.collection.objects.link(co); sc.camera=co
sc.render.resolution_x=sc.render.resolution_y=380
c=mathutils.Vector((0,0.01,1.64))
for nm,d in (("back",(0,1,0.25)),("side",(1,0.2,0.1)),("q34b",(0.7,0.7,0.2))):
    d=mathutils.Vector(d).normalized(); co.location=c+d*3; co.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=os.path.abspath(f"renders/{tag}_{nm}.png"); bpy.ops.render.render(write_still=True)
