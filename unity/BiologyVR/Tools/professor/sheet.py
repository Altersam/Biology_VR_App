import sys
from PIL import Image
tag=sys.argv[1]; out=sys.argv[2]
angs=[0,45,90,135,180,225,270,315]
ims=[Image.open(f"renders/{tag}_a{a:03d}.png").convert('RGB') for a in angs]
W=Image.new('RGB',(420*4,520*2))
for i,im in enumerate(ims): W.paste(im,((i%4)*420,(i//4)*520))
W.save(out,quality=85)
