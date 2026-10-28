import bpy, math, mathutils, os, sys
args = sys.argv[sys.argv.index('--')+1:]
src, tag = args[0], args[1]; sets = args[2:]
bpy.ops.wm.open_mainfile(filepath=os.path.abspath(src))
sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=16; sc.cycles.use_denoising=False
w=bpy.data.worlds.new("W"); sc.world=w; w.use_nodes=True; w.node_tree.nodes["Background"].inputs[0].default_value=(0.45,0.47,0.5,1)
l=bpy.data.lights.new("S","SUN"); l.energy=3.0; lo=bpy.data.objects.new("S",l); sc.collection.objects.link(lo); lo.rotation_euler=(math.radians(50),0,math.radians(-30))
cam=bpy.data.cameras.new("C"); cam.type='ORTHO'; co=bpy.data.objects.new("C",cam); sc.collection.objects.link(co); sc.camera=co
sc.render.resolution_x=sc.render.resolution_y=300
for i,st in enumerate(sets):
    for o in bpy.data.objects:
        if o.type=='MESH' and o.data.shape_keys:
            for k in o.data.shape_keys.key_blocks: k.value=0
    for kv in st.split(','):
        if not kv: continue
        n,v=kv.split('=')
        for o in bpy.data.objects:
            if o.type=='MESH' and o.data.shape_keys and n in o.data.shape_keys.key_blocks: o.data.shape_keys.key_blocks[n].value=float(v)
    for view,d,size,c in (("f",(0,-1,0),0.16,(0,0,1.585)),("s",(0.75,-0.65,0),0.2,(0,0,1.6))):
        cam.ortho_scale=size; co.location=mathutils.Vector(c)+mathutils.Vector(d).normalized()*5
        co.rotation_euler=(mathutils.Vector(d)*-1).to_track_quat('-Z','Y').to_euler()
        sc.render.filepath=os.path.abspath(f"renders/{tag}_{i}{view}.png"); bpy.ops.render.render(write_still=True)
