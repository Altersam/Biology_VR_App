import json
import struct
from pathlib import Path

import sys
path=Path(sys.argv[1]) if len(sys.argv)>1 else Path(r"Z:\YandexDisk\ПетПроекты\Biology_VR\3д\Unity\Биотул и сканнер\futuristic+circular+device+3d+model.glb")
data=path.read_bytes();magic,version,length=struct.unpack_from('<III',data)
offset=12;document=None
while offset<length:
 size,kind=struct.unpack_from('<II',data,offset);offset+=8
 if kind==0x4e4f534a:document=json.loads(data[offset:offset+size].decode('utf-8').rstrip('\0 '))
 offset+=size
report={'bytes':len(data),'version':version,'extensionsRequired':document.get('extensionsRequired',[]),
        'nodes':document.get('nodes',[]),'materials':document.get('materials',[]),'images':document.get('images',[]),
        'meshes':[{'index':i,'name':m.get('name'),'primitives':m['primitives']} for i,m in enumerate(document.get('meshes',[]))],
        'accessors':[{'index':i,**a} for i,a in enumerate(document.get('accessors',[]))]}
dest=Path(__file__).resolve().parent.parent/'INSTRUMENT_GLB_AUDIT.json'
dest.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
summary={'name':path.name,'bytes':len(data),'nodes':len(report['nodes']),'meshes':len(report['meshes']),'extensionsRequired':report['extensionsRequired'],
         'first_materials':report['materials'][:2],'image_count':len(report['images']),'first_image':report['images'][:1],
         'parts':[{'node':n['name'],'mesh':n.get('mesh'),'translation':n.get('translation'),
                   'bounds':[(document['accessors'][p['attributes']['POSITION']].get('min'),document['accessors'][p['attributes']['POSITION']].get('max')) for p in document['meshes'][n['mesh']]['primitives']]} for n in document['nodes'] if 'mesh' in n]}
print(json.dumps(summary,ensure_ascii=False,indent=2))
