"""Offline, tileable art bake. No runtime texture generation or random skin noise."""
import argparse
import json
from pathlib import Path
import numpy as np
from PIL import Image

parser=argparse.ArgumentParser()
parser.add_argument('output')
parser.add_argument('--size',type=int,default=2048)
parser.add_argument('--cartoon',action='store_true',help='Muted soft-cartoon V3; leaves V2 bake unchanged')
parser.add_argument('--concept-bright',action='store_true',help='Warm brighter coral/red V4 inspired by the GitHub scene/preset images')
parser.add_argument('--large-cells',action='store_true',help='Larger readable endothelial cells for the clean retro-futuristic VR direction')
parser.add_argument('--organic-panels',action='store_true',help='V5 broad rounded organic panels with real shader relief and reduced repetition')
parser.add_argument('--smooth-artery',action='store_true',help='V6 flatter arterial endothelium, restrained coral/red palette and soft boundaries')
args=parser.parse_args()
if args.smooth_artery: args.organic_panels=True
if args.organic_panels: args.concept_bright=True;args.large_cells=True
if args.concept_bright: args.cartoon=True
out=Path(args.output)
if not out.is_dir(): raise SystemExit('Output directory must exist')
n=args.size
u,v=np.meshgrid(np.arange(n,dtype=np.float32)/n,np.arange(n,dtype=np.float32)/n)
columns,rows=(10,4) if args.large_cells else (12,5)
if args.organic_panels: columns,rows=7,3
# The existing vessel UV has physical axes: U = circumference/3.5m,
# V = flow arc/5.6m. Twelve by five cells yields elongated axial cells.
px=u*columns+.28*np.sin(v*2*np.pi)+.14*np.sin(v*6*np.pi+u*2*np.pi)
py=v*rows+.22*np.sin(u*4*np.pi)+.09*np.sin(v*2*np.pi-u*6*np.pi)
if args.smooth_artery:
    px=u*columns+.15*np.sin(v*2*np.pi)+.07*np.sin(v*6*np.pi+u*2*np.pi)
    py=v*rows+.12*np.sin(u*4*np.pi)+.05*np.sin(v*2*np.pi-u*6*np.pi)
ix=np.floor(px).astype(np.int32);iy=np.floor(py).astype(np.int32)
def hash2(x,y,k=0):
    z=np.sin((x%columns)*127.1+(y%rows)*311.7+k*73.3)*43758.5453
    return (z-np.floor(z)).astype(np.float32)
best=np.full((n,n),99,dtype=np.float32);second=best.copy()
dxbest=np.zeros_like(best);dybest=dxbest.copy();seed=dxbest.copy();rotbest=dxbest.copy()
for j in range(-2,3):
    for i in range(-2,3):
        x=ix+i;y=iy+j;k=hash2(x,y)
        dx=px-(x+.5+(k-.5)*.65)
        dy=py-(y+.5+.22*np.sin((x%columns)*2*np.pi/columns)+(hash2(x,y,1)-.5)*.65)
        angle=(hash2(x,y,2)-.5)*.20
        a=dx*np.cos(angle)-dy*np.sin(angle)
        b=dx*np.sin(angle)+dy*np.cos(angle)
        width=.84+.32*hash2(x,y,3);length=.83+.34*hash2(x,y,4)
        d=np.sqrt((a/width)**2+(b/length)**2)
        new=d<best
        second=np.where(new,best,np.minimum(second,d))
        best=np.where(new,d,best)
        dxbest=np.where(new,a/width,dxbest);dybest=np.where(new,b/length,dybest)
        seed=np.where(new,k,seed)
        rotbest=np.where(new,angle,rotbest)
def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)
gap=second-best
border=1-smooth(.003,.026,gap)
dome=np.exp(-((dxbest/.58)**2+(dybest/.65)**2)*1.35)
# A broad raised membrane, a soft shoulder and tiny stylized highlight sweeps.
shoulder=np.exp(-((best-.32)/.10)**2)*(.55+.45*np.sin(dybest*3+1))
nucleus=1-smooth(.6,1.45,((dxbest-(seed-.5)*.07)/.085)**2+((dybest+.045)/.16)**2)
nucleus*=smooth(.28,.45,seed)*.32
if args.cartoon:
    nucleus*=.78
palette=lambda h: np.array([int(h[i:i+2],16)/255 for i in (0,2,4)],dtype=np.float32)
base=palette('E97A72');mid=palette('F49A87');light=palette('FFC1A8');shadow=palette('B94E55')
if args.cartoon:
    base=palette('D07C76');mid=palette('DE9889');light=palette('EAB7A3');shadow=palette('AA6167')
if args.concept_bright:
    base=palette('E87567');mid=palette('F5947F');light=palette('FFBE9E');shadow=palette('AB4350')
if args.organic_panels:
    base=palette('EA7865');mid=palette('F6987D');light=palette('FFBF99');shadow=palette('AD4650')
if args.smooth_artery:
    base=palette('D96763');mid=palette('ED8173');light=palette('F39A7F');shadow=palette('A8444A')
color=np.broadcast_to(base,(n,n,3)).copy()
f=(dome*.36+(seed-.5)*.12)[...,None]
if args.cartoon:
    f=(smooth(.24,.62,dome)*.36+(seed-.5)*.10)[...,None]
color=color*(1-f)+mid*f
softshadow=(smooth(-.15,.4,dxbest)*(1-dome)*.24)[...,None]
if args.concept_bright:
    softshadow=(smooth(-.15,.4,dxbest)*(1-dome)*.34)[...,None]
if args.smooth_artery:
    softshadow=(smooth(-.15,.4,dxbest)*(1-dome)*.19)[...,None]
color=color*(1-softshadow)+shadow*softshadow
painted=np.exp(-((dxbest+.28)/.13)**2-((dybest+.12)/.45)**2)*(1-border)
highlight=(painted*.42+shoulder*.12+dome*.05)[...,None]
if args.cartoon:
    # Readable rounded illustration patches, instead of shiny membrane streaks.
    painted=1-smooth(.7,1.35,((dxbest+.23)/.16)**2+((dybest+.11)/.40)**2)
    highlight=(painted*.26+smooth(.58,.85,dome)*.06)[...,None]
    if args.concept_bright:
        highlight=(painted*.40+smooth(.52,.83,dome)*.10)[...,None]
    if args.smooth_artery:
        highlight=(painted*.25+smooth(.52,.83,dome)*.08)[...,None]
color=color*(1-highlight)+light*highlight
edge=border[...,None]*.20
color=color*(1-edge)+palette('9C505A')*edge
if args.cartoon:
    color=color*(1-border[...,None]*.09)+palette('92626A')*border[...,None]*.09
    if args.organic_panels:
        color=color*(1-border[...,None]*.14)+palette('A54750')*border[...,None]*.14
nuc=nucleus[...,None]
if args.smooth_artery:nuc*=.30
color=color*(1-nuc)+palette('98647E')*nuc
height=dome*.030+shoulder*.004-border*.0002+nucleus*.001
if args.cartoon:
    height=dome*.018+shoulder*.0015+nucleus*.0007
    if args.concept_bright: height=dome*.024+shoulder*.002+nucleus*.0007
    if args.organic_panels: height=dome*.044+shoulder*.0025+nucleus*.0003
    if args.smooth_artery: height=dome*.034+shoulder*.0015+nucleus*.0002
# Derivatives are scaled in physical chart units, not arbitrary per-pixel noise.
hx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*n/3.5
hy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*n/5.6
normal=np.stack((-hx*.60,hy*.60,np.ones_like(hx)),axis=-1)
normal/=np.linalg.norm(normal,axis=-1)[...,None]
ao=1-border*.08-(1-dome)*.025
detail=np.stack((ao,dome,nucleus),axis=-1)
version='V4' if args.concept_bright else 'V3' if args.cartoon else 'V2'
if args.organic_panels: version='V5'
if args.smooth_artery: version='V6'
for name,data in [('Endothelium_Base_'+version,color),('Endothelium_Normal_'+version,normal*.5+.5),('Endothelium_Detail_'+version,detail)]:
    Image.fromarray(np.round(np.clip(data,0,1)*255).astype(np.uint8)).save(out/(name+'.png'))
preview=Image.fromarray(np.round(np.clip(color,0,1)*255).astype(np.uint8));preview.resize((800,800)).save(out/'Endothelium_Art_Preview.jpg')
(out/'TextureBake.json').write_text(json.dumps({'size':n,'style':'warm concept-inspired cartoon' if args.concept_bright else 'muted soft cartoon' if args.cartoon else 'soft scientific illustration','version':version,'physical_chart_m':[3.5,5.6],'cells_per_tile':[columns,rows],'elongated_along':'V / flow','variation':'independent width/length +/- 16-17%, warped boundaries, small orientation jitter','seed':8,'maps':['sRGB Base','linear tangent Normal','linear AO/dome/nucleus detail'],'noise':'none'},indent=2),encoding='utf-8')
print('Baked 3 maps at',n,'into',out)
