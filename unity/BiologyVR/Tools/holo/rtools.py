import bpy, math, os, mathutils
bpy.ops.wm.open_mainfile(filepath=os.path.abspath("tools.blend"))
sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=16; sc.cycles.use_denoising=False
w=bpy.data.worlds.new("W"); sc.world=w; w.use_nodes=True; w.node_tree.nodes["Background"].inputs[0].default_value=(0.35,0.38,0.42,1)
l=bpy.data.lights.new("S","SUN"); l.energy=3; lo=bpy.data.objects.new("S",l); sc.collection.objects.link(lo); lo.rotation_euler=(math.radians(40),0,math.radians(30))
def mat(name,col,emit=0,rough=0.4,alpha=1):
    m=bpy.data.materials.new(name); m.use_nodes=True; b=m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value=(*col,1); b.inputs["Roughness"].default_value=rough
    if emit: b.inputs["Emission Color"].default_value=(*col,1); b.inputs["Emission Strength"].default_value=emit
    if alpha<1: b.inputs["Alpha"].default_value=alpha
    return m
M={"Body":mat("b",(0.82,0.86,0.9)),"Dark":mat("d",(0.12,0.14,0.17)),"Glow":mat("g",(0.4,0.9,1.0),5),"Glass":mat("gl",(0.6,0.9,1.0),0.5,0.1,0.3)}
for o in bpy.data.objects:
    if o.type=='MESH': o.data.materials.append(M[o.name.split('_')[-1]])
    if o.name.startswith("BioTool"): o.location.x += 0.09
cam=bpy.data.cameras.new("C"); cam.type='ORTHO'; cam.ortho_scale=0.26; co=bpy.data.objects.new("C",cam); sc.collection.objects.link(co); sc.camera=co
sc.render.resolution_x=640; sc.render.resolution_y=420
from PIL import Image
ims=[]
for i,d in enumerate(((1,0.3,0.4),(0.6,-1,0.5),(-0.7,0.6,0.6))):
    d=mathutils.Vector(d).normalized(); co.location=mathutils.Vector((0.045,-0.02,0))+d*2; co.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=f"/tmp/claude-0/tool{i}.png"; bpy.ops.render.render(write_still=True); ims.append(Image.open(sc.render.filepath).convert('RGB'))
W=Image.new('RGB',(640*3,420)); [W.paste(im,(i*640,0)) for i,im in enumerate(ims)]; W.save('/tmp/claude-0/tools.jpg',quality=88)
