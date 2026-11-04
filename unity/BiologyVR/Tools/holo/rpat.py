import bpy, math, os, mathutils
bpy.ops.wm.open_mainfile(filepath=os.path.abspath("patient.blend"))
sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=12; sc.cycles.use_denoising=False
w=bpy.data.worlds.new("W"); sc.world=w; w.use_nodes=True; w.node_tree.nodes["Background"].inputs[0].default_value=(0.03,0.04,0.06,1)
def emis(name, col, strength, alpha=1.0):
    m=bpy.data.materials.new(name); m.use_nodes=True; nt=m.node_tree; nt.nodes.clear()
    o=nt.nodes.new('ShaderNodeOutputMaterial'); e=nt.nodes.new('ShaderNodeEmission'); e.inputs[0].default_value=(*col,1); e.inputs[1].default_value=strength
    if alpha<1:
        t=nt.nodes.new('ShaderNodeBsdfTransparent'); mx=nt.nodes.new('ShaderNodeMixShader'); mx.inputs[0].default_value=alpha
        lw=nt.nodes.new('ShaderNodeLayerWeight'); lw.inputs[0].default_value=0.25
        nt.links.new(lw.outputs['Facing'], mx.inputs[0]); nt.links.new(t.outputs[0], mx.inputs[1]); nt.links.new(e.outputs[0], mx.inputs[2]); nt.links.new(mx.outputs[0], o.inputs[0])
    else: nt.links.new(e.outputs[0], o.inputs[0])
    return m
O=bpy.data.objects
O["PatientBody"].data.materials.append(emis("b",(0.3,0.8,1.0),2.0,0.5))
O["PatientArteries"].data.materials.append(emis("a",(1,0.2,0.25),3))
O["PatientVeins"].data.materials.append(emis("v",(0.25,0.45,1),3))
O["PatientHeart"].data.materials.append(emis("h",(1,0.25,0.3),4))
O["PatientLungs"].data.materials.append(emis("l",(0.5,0.8,1.0),1.0,0.3))
cam=bpy.data.cameras.new("C"); cam.type='ORTHO'; cam.ortho_scale=1.95; co=bpy.data.objects.new("C",cam); sc.collection.objects.link(co); sc.camera=co
sc.render.resolution_x=520; sc.render.resolution_y=900
from PIL import Image
ims=[]
for i,d in enumerate(((0,-1,0.05),(1,-0.6,0.05),(1,0,0.05))):
    d=mathutils.Vector(d).normalized(); co.location=mathutils.Vector((0,0,0.9))+d*4; co.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=f"/tmp/claude-0/pat{i}.png"; bpy.ops.render.render(write_still=True); ims.append(Image.open(sc.render.filepath).convert('RGB'))
W=Image.new('RGB',(520*3,900)); [W.paste(im,(i*520,0)) for i,im in enumerate(ims)]; W.save('/tmp/claude-0/pat.jpg',quality=85)
