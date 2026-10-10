import io
import json
import math
import shutil
import struct
from pathlib import Path
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'Assets/BiologyVR/ArteryJourney/Generated/ActiveInstruments'
SRC=ROOT.parent/'Биотул и сканнер'

def load(path):
    data=path.read_bytes();offset=12;doc=None;binary=None
    while offset<len(data):
        size,kind=struct.unpack_from('<II',data,offset);offset+=8
        if kind==0x4e4f534a:doc=json.loads(data[offset:offset+size].decode('utf-8').rstrip('\0 '))
        if kind==0x004e4942:binary=data[offset:offset+size]
        offset+=size
    if doc.get('extensionsRequired'):raise ValueError('Compressed GLB needs a different decoder')
    return doc,binary

def accessor(doc,binary,index):
    a=doc['accessors'][index];v=doc['bufferViews'][a['bufferView']]
    width={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
    kind={5121:'u1',5123:'<u2',5125:'<u4',5126:'<f4'}[a['componentType']]
    size=np.dtype(kind).itemsize;offset=v.get('byteOffset',0)+a.get('byteOffset',0)
    result=np.ndarray((a['count'],width),dtype=kind,buffer=binary,offset=offset,strides=(v.get('byteStride',width*size),size)).copy()
    return result

def transform(node):
    if 'matrix' in node:return np.array(node['matrix']).reshape(4,4).T
    x,y,z,w=node.get('rotation',[0,0,0,1]);m=np.eye(4)
    m[:3,:3]=[[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
              [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],
              [2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]]
    m[:3,:3]=m[:3,:3]@np.diag(node.get('scale',[1,1,1]));m[:3,3]=node.get('translation',[0,0,0])
    return m

def prepare(kind,filename):
    folder=OUT/kind;folder.mkdir(parents=True,exist_ok=True);(OUT/'Sources').mkdir(exist_ok=True)
    source=SRC/filename;shutil.copy2(source,OUT/'Sources'/filename)
    doc,binary=load(source);nodes=doc['nodes'];parent={child:i for i,n in enumerate(nodes) for child in n.get('children',[])}
    matrices={}
    def world(i):
        if i not in matrices:matrices[i]=(world(parent[i]) if i in parent else np.eye(4))@transform(nodes[i])
        return matrices[i]
    count=len(doc['materials']);grid=math.ceil(math.sqrt(count));tile=128;padding=4;size=grid*tile
    atlas=Image.new('RGB',(size,size),(18,28,36))
    for i,mat in enumerate(doc['materials']):
        pbr=mat.get('pbrMetallicRoughness',{});texture=pbr.get('baseColorTexture')
        if texture:
            image=doc['images'][doc['textures'][texture['index']]['source']];view=doc['bufferViews'][image['bufferView']]
            image=Image.open(io.BytesIO(binary[view.get('byteOffset',0):view.get('byteOffset',0)+view['byteLength']])).convert('RGB')
        else:image=Image.new('RGB',(8,8),tuple(int(c*255) for c in pbr.get('baseColorFactor',[1,1,1,1])[:3]))
        image=image.resize((tile-2*padding,tile-2*padding))
        patch=Image.new('RGB',(tile,tile));patch.paste(image,(padding,padding))
        patch.paste(image.crop((0,0,1,image.height)).resize((padding,image.height)),(0,padding))
        patch.paste(image.crop((image.width-1,0,image.width,image.height)).resize((padding,image.height)),(tile-padding,padding))
        patch.paste(patch.crop((0,padding,tile,padding+1)).resize((tile,padding)),(0,0))
        patch.paste(patch.crop((0,tile-padding-1,tile,tile-padding)).resize((tile,padding)),(0,tile-padding))
        atlas.paste(patch,((i%grid)*tile,(i//grid)*tile))
    # A clean medical palette replaces noisy photographic mottling. Preserve
    # source UV details and paint luminous cyan / small amber signals separately.
    rgb=np.asarray(atlas,dtype=np.float32)/255
    luminance=rgb@np.array([.2126,.7152,.0722])
    cyan=(rgb[:,:,1]>rgb[:,:,0]*1.18)&(rgb[:,:,2]>rgb[:,:,0]*1.18)&(luminance>.18)
    if kind=='Scanner':cyan|=(rgb[:,:,1]>rgb[:,:,0]*1.22)&(rgb[:,:,1]>rgb[:,:,2]*1.10)&(luminance>.16)
    amber=(rgb[:,:,0]>.48)&(rgb[:,:,1]>.30)&(rgb[:,:,2]<rgb[:,:,1]*.58)&~cyan
    dark=luminance<.34
    cream=np.array([.86,.87,.83]);graphite=np.array([.12,.15,.18])
    paint=np.where(dark[:,:,None],graphite,cream)
    # Classify physical parts, not individual shadow pixels: that gives the
    # housing clean cream surfaces instead of baked black photographic blotches.
    for node_index,node in enumerate(nodes):
        if 'mesh' not in node:continue
        matrix=world(node_index);name=node.get('name','')
        for primitive in doc['meshes'][node['mesh']]['primitives']:
            positions=accessor(doc,binary,primitive['attributes']['POSITION'])
            centre=(positions@matrix[:3,:3].T+matrix[:3,3]).mean(axis=0)
            graphite_part=(centre[1]<.54) or (kind=='BioTool' and centre[0]<-.18 and name!='tripo_part_0') or (kind=='Scanner' and name=='tripo_part_0')
            material=primitive.get('material',0);tx=(material%grid)*tile;ty=(material//grid)*tile
            region=np.s_[ty:ty+tile,tx:tx+tile]
            base=graphite if graphite_part else cream
            paint[region]=base*(.97+luminance[region][...,None]*.03)+rgb[region]*.012
    paint[cyan]=np.array([92/255,235/255,1])*(.78+luminance[cyan,None]*.18)
    paint[amber]=np.array([243/255,181/255,74/255])*(.82+luminance[amber,None]*.14)
    emission=np.zeros_like(rgb);emission[cyan]=[92/255,235/255,1];emission[amber]=[243/255,181/255,74/255]
    atlas=Image.fromarray((np.clip(paint,0,1)*255).astype('uint8'))
    atlas.save(folder/'DeviceAtlas.png')
    Image.fromarray((emission*255).astype('uint8')).save(folder/'DeviceEmission.png')
    # glTF right-handed to Unity, then align muzzle forward with +Z.
    reflect=np.diag([1,1,-1])
    rotation=np.array([[-1,0,0],[0,1,0],[0,0,-1]]) if kind=='Scanner' else np.array([[0,0,1],[0,1,0],[-1,0,0]])
    convert=rotation@reflect;scale=.32
    muzzle=np.array([0,.74,.31]) if kind=='Scanner' else np.array([-.48,.75,0])
    def point(value):return ((np.asarray(value)-muzzle)@convert.T)*scale
    groups={}
    def group_for(name,center):
        if kind=='Scanner':return 'Rotor' if name=='tripo_part_0' else 'Body'
        if center[0]<-.35 and name!='tripo_part_0':
            y,z=center[1]-.75,center[2]
            if abs(y)>abs(z):return 'Prong0' if y>0 else 'Prong2'
            return 'Prong1' if z<0 else 'Prong3'
        return 'Body'
    parts=[];triangles=0
    for i,node in enumerate(nodes):
        if 'mesh' not in node:continue
        matrix=world(i);center=matrix[:3,3];name=node.get('name',str(i));group=group_for(name,center)
        for primitive in doc['meshes'][node['mesh']]['primitives']:
            attrs=primitive['attributes'];positions=accessor(doc,binary,attrs['POSITION'])
            global_positions=positions@matrix[:3,:3].T+matrix[:3,3]
            vertices=point(global_positions)
            normals=accessor(doc,binary,attrs['NORMAL'])@np.linalg.inv(matrix[:3,:3])
            normals=normals@convert.T;normals/=np.maximum(np.linalg.norm(normals,axis=1)[:,None],1e-8)
            uv=accessor(doc,binary,attrs['TEXCOORD_0']);material=primitive.get('material',0)
            tx=(material%grid)*tile;ty=(material//grid)*tile
            uv=np.column_stack(((tx+padding+np.clip(uv[:,0],0,1)*(tile-2*padding))/size,
                                1-(ty+padding+np.clip(uv[:,1],0,1)*(tile-2*padding))/size))
            indices=accessor(doc,binary,primitive['indices']).reshape(-1,3)[:,[0,2,1]]
            item=groups.setdefault(group,{'vertices':[],'normals':[],'uv':[],'triangles':[],'sourceParts':[]})
            start=len(item['vertices']);item['vertices'].extend(vertices.tolist());item['normals'].extend(normals.tolist());item['uv'].extend(uv.tolist())
            item['triangles'].extend((indices+start).reshape(-1).tolist());item['sourceParts'].append(name);triangles+=len(indices)
        parts.append({'name':name,'group':group})
    pivots={'Rotor':point([-.0015,.739,.2057]).tolist()} if kind=='Scanner' else {f'Prong{i}':point(value).tolist() for i,value in enumerate([[-.35,.865,0],[-.35,.75,-.135],[-.35,.635,0],[-.35,.75,.135]])}
    for name,item in groups.items():
        pivot=np.array(pivots.get(name,[0,0,0]));item['vertices']=(np.asarray(item['vertices'])-pivot).tolist();item['pivot']=pivot.tolist()
    result={'kind':kind,'source':filename,'groups':groups,'parts':parts,'triangles':triangles,'atlasSize':size,'normalizedScale':scale,'forward':'+Z'}
    (folder/'Geometry.json').write_text(json.dumps(result,separators=(',',':')),encoding='utf-8')
    print(json.dumps({'kind':kind,'groups':{k:v['sourceParts'] for k,v in groups.items()},'triangles':triangles,'atlasSize':size},indent=2))

if __name__=='__main__':
    if not OUT.is_dir():raise SystemExit('Output parent must exist')
    prepare('Scanner','futuristic+circular+device+3d+model.glb')
    prepare('BioTool','futuristic+gun+3d+model.glb')
