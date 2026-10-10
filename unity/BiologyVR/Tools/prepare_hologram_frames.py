"""Retain the supplied GLB rims, removing the baked solid display surface for real holographic UI."""
import io
import json
import shutil
from pathlib import Path
import numpy as np
from PIL import Image, ImageEnhance
from prepare_active_instruments import load, accessor, transform

ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'Assets/BiologyVR/ArteryJourney/Generated/HolographicScreens'
SRC=ROOT.parent/'Экраны'
if not OUT.parent.is_dir():raise SystemExit('Generated parent is missing')
OUT.mkdir(exist_ok=True)
for kind,filename in [('Vitals','futuristic+display+panel+3d+model.glb'),('Theory','futuristic+portal+frame+3d+model.glb')]:
    folder=OUT/kind;folder.mkdir(exist_ok=True)
    path=SRC/filename;shutil.copy2(path,folder/filename)
    doc,binary=load(path);node=doc['nodes'][0];matrix=transform(node)
    primitive=doc['meshes'][node['mesh']]['primitives'][0];attrs=primitive['attributes']
    vertices=accessor(doc,binary,attrs['POSITION'])@matrix[:3,:3].T+matrix[:3,3]
    low,high=vertices.min(axis=0),vertices.max(axis=0);center=(low+high)/2
    vertices=(vertices-center)/(high[0]-low[0]);vertices[:,2]*=-1
    normals=accessor(doc,binary,attrs['NORMAL'])@np.linalg.inv(matrix[:3,:3]);normals[:,2]*=-1
    normals/=np.maximum(np.linalg.norm(normals,axis=1)[:,None],1e-8)
    uv=accessor(doc,binary,attrs['TEXCOORD_0']);uv[:,1]=1-uv[:,1]
    indices=accessor(doc,binary,primitive['indices']).reshape(-1,3)[:,[0,2,1]]
    centroids=vertices[indices].mean(axis=1)
    # Both generated sources have opaque central faces. Keep only their actual
    # peripheral mesh; replace the interior with a transparent, scrollable Canvas.
    half_height=(high[1]-low[1])/(high[0]-low[0])/2
    keep=(np.abs(centroids[:,0])>.43)|(np.abs(centroids[:,1])>half_height*.88)
    rim=indices[keep]
    data={'vertices':vertices.tolist(),'normals':normals.tolist(),'uv':uv.tolist(),'triangles':rim.reshape(-1).tolist(),'aspectHeight':float(half_height*2),'sourceTriangles':len(indices),'frameTriangles':len(rim)}
    (folder/'FrameGeometry.json').write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
    tex=doc['materials'][primitive.get('material',0)]['pbrMetallicRoughness']['baseColorTexture']['index']
    image=doc['images'][doc['textures'][tex]['source']];view=doc['bufferViews'][image['bufferView']]
    atlas=Image.open(io.BytesIO(binary[view.get('byteOffset',0):view.get('byteOffset',0)+view['byteLength']])).convert('RGB')
    atlas=ImageEnhance.Brightness(atlas).enhance(1.18);atlas.save(folder/'FrameAtlas.png')
    print(json.dumps({'kind':kind,'sourceTriangles':len(indices),'frameTriangles':len(rim),'aspectHeight':data['aspectHeight']}))
