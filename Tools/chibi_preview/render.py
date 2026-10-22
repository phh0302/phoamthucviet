import numpy as np, math, random
from PIL import Image, ImageDraw
class Kit:
    def __init__(s): s.v=[];s.n=[];s.c=[];s.t=[]
    def vert(s,p,n,c): s.v.append(np.array(p,float));s.n.append(np.array(n,float));s.c.append(c);return len(s.v)-1
    def tri(s,a,b,c):
        f=np.cross(s.v[b]-s.v[a],s.v[c]-s.v[a]); nn=s.n[a]+s.n[b]+s.n[c]
        s.t.append((a,c,b) if np.dot(f,nn)<0 else (a,b,c))
    def ell(s,ctr,r,col,ol=True,seg=16,rings=10):
        col=(col,1 if ol else 0); st=len(s.v)
        for i in range(rings+1):
            th=math.pi*i/rings;y=math.cos(th);sn=math.sin(th)
            for j in range(seg+1):
                ph=2*math.pi*j/seg;u=np.array([sn*math.cos(ph),y,sn*math.sin(ph)])
                n=u/np.array(r);n/=np.linalg.norm(n)+1e-9
                s.vert(np.array(ctr)+u*np.array(r),n,col)
        for i in range(rings):
            for j in range(seg):
                a=st+i*(seg+1)+j;b=a+seg+1
                if i>0:s.tri(a,a+1,b)
                if i<rings-1:s.tri(b,a+1,b+1)
        return s
    def sph(s,c,r,col,ol=True): return s.ell(c,(r,r,r),col,ol,10 if r<.08 else 16,6 if r<.08 else 10)
    def cap(s,c,r,nrm,col,seg):
        ci=s.vert(c,nrm,col);st=len(s.v)
        for j in range(seg+1):
            ph=2*math.pi*j/seg;s.vert(np.array(c)+np.array([math.cos(ph),0,math.sin(ph)])*r,nrm,col)
        for j in range(seg):s.tri(ci,st+j,st+j+1)
    def cyl(s,b,r,h,col,ol=True,seg=16,top=1):
        col=(col,1 if ol else 0);st=len(s.v);rt=r*top;b=np.array(b,float)
        for j in range(seg+1):
            ph=2*math.pi*j/seg;d=np.array([math.cos(ph),0,math.sin(ph)]);n=np.array([d[0]*h,r-rt,d[2]*h]);n/=np.linalg.norm(n)
            s.vert(b+d*r,n,col);s.vert(b+d*rt+[0,h,0],n,col)
        for j in range(seg):
            a=st+j*2;s.tri(a,a+1,a+2);s.tri(a+2,a+1,a+3)
        s.cap(b,r,[0,-1,0],col,seg)
        if rt>.001:s.cap(b+[0,h,0],rt,[0,1,0],col,seg)
        return s
    def cone(s,b,r,h,col,ol=True,seg=20):
        col=(col,1 if ol else 0);st=len(s.v);b=np.array(b,float)
        for j in range(seg+1):
            ph=2*math.pi*j/seg;d=np.array([math.cos(ph),0,math.sin(ph)]);n=np.array([d[0]*h,r,d[2]*h]);n/=np.linalg.norm(n)
            s.vert(b+d*r,n,col);s.vert(b+[0,h,0],n,col)
        for j in range(seg):a=st+j*2;s.tri(a,a+1,a+2)
        s.cap(b,r,[0,-1,0],col,seg);return s
C=lambda r,g,b:(r,g,b)
Ink=C(.12,.08,.07);Cheek=C(1,.58,.55);Mouth=C(.62,.22,.2);White=C(.97,.97,.95)
Skins=[C(1,.84,.7),C(.96,.78,.62),C(.88,.68,.52),C(1,.88,.76)]
Hairs=[C(.1,.08,.08),C(.22,.14,.1),C(.35,.22,.14),C(.55,.55,.58)]
Shirts=[C(.95,.55,.35),C(.4,.75,.45),C(.85,.45,.7),C(.95,.8,.3),C(.55,.55,.9),C(.3,.7,.75),C(.9,.3,.3),C(.98,.95,.88)]
Pants=[C(.25,.32,.55),C(.7,.6,.42),C(.22,.22,.25),C(.35,.45,.3)]
Straw=C(.95,.86,.58)
def look(kind,v):
    L=dict(hat=None,hair_style='Short',apron=False,vest=False,bow=False,scarf=False,must=False,buttons=False,glasses=False,hatc=White)
    if kind=='head': L.update(skin=Skins[1],hair=Hairs[1],shirt=White,pants=C(.25,.25,.28),shoes=Ink,hat='Tall',scarf=True,scarfc=C(.85,.15,.15),must=True,buttons=True)
    elif kind=='chef': L.update(skin=Skins[v%4],hair=Hairs[(v+1)%3],hair_style='Short' if v%2==0 else 'Spiky',shirt=White,pants=C(.28,.28,.32),shoes=Ink,hat='Toque',apron=True,apronc=C(.95,.62,.25),scarf=True,scarfc=C(.98,.75,.25),buttons=True)
    elif kind=='waiter': L.update(skin=Skins[(v+2)%4],hair=Hairs[v%3],hair_style='Short' if v%2==0 else 'Bun',shirt=White,pants=C(.16,.17,.22),shoes=Ink,vest=True,vestc=C(.18,.3,.62),bow=True,apron=True,apronc=C(.16,.17,.22))
    else:
        rng=random.Random(v); L.update(skin=Skins[rng.randrange(4)],hair=Hairs[rng.randrange(3)],shirt=Shirts[v%8],pants=Pants[rng.randrange(4)],shoes=C(.35,.25,.2))
        if v==0:L.update(hat='NonLa',hair_style='Long')
        if v==1:L.update(hat='Cap',hatc=C(.9,.3,.25))
        if v==2:L.update(hair_style='Bun')
        if v==3:L.update(hair_style='Long',glasses=True)
        if v==4:L.update(hair_style='Spiky')
        if v==5:L.update(hat='NonLa',hair=Hairs[3],must=True)
        if v==6:L.update(hat='Cap',hatc=C(.25,.45,.85),glasses=True)
    return L
HY=1.42;HR=.43
def mul(c,k):return tuple(x*k for x in c)
def torso(l):
    k=Kit()
    k.ell((0,.74,0),(.37,.36,.31),l['vestc'] if l['vest'] else l['shirt'])
    k.ell((0,.5,0),(.33,.15,.28),l['pants'])
    if l['vest']:
        k.ell((0,.86,.25),(.13,.17,.08),l['shirt'],False)
        k.sph((0,.66,.3),.03,C(.9,.85,.4),False);k.sph((0,.56,.29),.03,C(.9,.85,.4),False)
    if l['apron']:k.ell((0,.5 if l['vest'] else .62,.19),(.3,.2 if l['vest'] else .3,.14),l['apronc'])
    if l['buttons'] and not l['apron']:
        for i in range(2):
            for s in(-1,1):k.sph((.1*s,.82-i*.15,.29),.032,C(.35,.35,.4),False)
    if l['buttons'] and l['apron']:k.sph((0,.92,.29),.032,C(.35,.35,.4),False)
    if l['scarf']:k.ell((0,1.02,.02),(.29,.08,.27),l['scarfc']);k.ell((0,.92,.27),(.09,.11,.05),l['scarfc'])
    if l['bow']:
        red=C(.85,.15,.18);k.ell((-.07,1,.29),(.07,.045,.04),red);k.ell((.07,1,.29),(.07,.045,.04),red);k.sph((0,1,.31),.03,red,False)
    k.sph((0,HY,0),HR,l['skin']);k.sph((-.42,HY-.02,0),.08,l['skin']);k.sph((.42,HY-.02,0),.08,l['skin'])
    for s in(-1,1):
        k.ell((.15*s,HY+.02,.39),(.055,.08,.04),Ink,False);k.sph((.15*s-.015,HY+.05,.425),.02,(1,1,1),False)
        k.ell((.25*s,HY-.09,.34),(.07,.04,.04),Cheek,False)
    k.sph((0,HY-.05,.43),.035,mul(l['skin'],.95),False);k.ell((0,HY-.13,.4),(.055,.025,.03),Mouth,False)
    if l['must']:
        for s in(-1,1):k.ell((.07*s,HY-.09,.415),(.08,.032,.035),l['hair'],False)
    if l['glasses']:
        fr=C(.15,.12,.12)
        for s in(-1,1):
            k.ell((.15*s,HY+.02,.41),(.095,.085,.02),fr,False);k.ell((.15*s,HY+.02,.425),(.072,.064,.014),C(.8,.9,.98),False);k.ell((.15*s,HY+.02,.432),(.03,.045,.01),Ink,False)
        k.ell((0,HY+.04,.43),(.06,.014,.014),fr,False)
    if not(l['hat']=='Cap' and l['hair_style']=='Short'):
        k.ell((0,HY+.1,-.05),(.46,.36,.44),l['hair'])
        hs=l['hair_style']
        if hs=='Bun':k.sph((0,HY+.44,-.14),.15,l['hair'])
        if hs=='Long':k.ell((0,HY-.18,-.14),(.44,.38,.32),l['hair'])
        if hs=='Spiky':
            for i in(-1,0,1):k.ell((.15*i,HY+.38,.12-abs(i)*.05),(.12,.12,.14),l['hair'])
    h=l['hat']
    if h=='Toque':k.cyl((0,HY+.24,-.02),.36,.2,White);k.ell((0,HY+.56,-.02),(.42,.24,.42),White)
    if h=='Tall':k.cyl((0,HY+.24,-.02),.37,.36,White);k.ell((0,HY+.74,-.02),(.47,.3,.47),White);k.sph((-.2,HY+.8,.12),.2,White);k.sph((.2,HY+.8,.12),.2,White)
    if h=='NonLa':k.cone((0,HY+.26,0),.66,.48,Straw)
    if h=='Cap':k.ell((0,HY+.15,-.02),(.455,.32,.455),l['hatc']);k.ell((0,HY+.17,.36),(.3,.04,.24),l['hatc'])
    return k
def arm(l):k=Kit();k.ell((0,-.16,0),(.11,.2,.11),l['shirt']);k.sph((0,-.37,0),.1,l['skin']);return k
def leg(l):k=Kit();k.cyl((0,-.34,0),.115,.34,l['pants'],top=1.1);k.ell((0,-.35,.05),(.13,.08,.19),l['shoes']);return k
def rx(a):a=math.radians(a);c,s=math.cos(a),math.sin(a);return np.array([[1,0,0],[0,c,-s],[0,s,c]])
def ry(a):a=math.radians(a);c,s=math.cos(a),math.sin(a);return np.array([[c,0,s],[0,1,0],[-s,0,c]])
def rz(a):a=math.radians(a);c,s=math.cos(a),math.sin(a);return np.array([[c,-s,0],[s,c,0],[0,0,1]])
def render(kind,v,yaw=180,armA=0,legA=0,img=None,ox=0,scale=170,action=None):
    l=look(kind,v);parts=[]
    tilt=rx(-28);Y=ry(yaw)
    aL=aR=armA;lL=lR=legA
    if action=='cook':aL,aR=-60,-40
    if action=='carry':aL=aR=-80
    parts.append((torso(l),np.eye(3),np.zeros(3)))
    parts.append((arm(l),rx(aL)@rz(-10)*0+ (rz(-10)@rx(aL)),np.array([-.36,.95,0])))
    parts.append((arm(l),rz(10)@rx(aR),np.array([.36,.95,0])))
    parts.append((leg(l),rx(lL),np.array([-.16,.42,0])))
    parts.append((leg(l),rx(lR),np.array([.16,.42,0])))
    M=tilt@Y
    tris=[]
    for k,R,o in parts:
        V=np.array(k.v);N=np.array(k.n)
        W=(M@(R@V.T+o[:,None])).T;NW=(M@R@N.T).T
        for (a,b,c) in k.t:
            p=W[[a,b,c]];f=np.cross(p[1]-p[0],p[2]-p[0])
            col,ol=k.c[a];n=NW[[a,b,c]].mean(0)
            if f[2]<0:  # front facing (toward -z camera)
                n=n/np.linalg.norm(n);L=np.array([-.45,.75,-.55]);L/=np.linalg.norm(L)
                d=np.dot(n,L)*.5+.5;lit=.74+(.26 if d>.47 else 0)
                rim=max(0,1+n[2])**4*.12
                tris.append((p[:,2].mean(),p,tuple(min(1,cc*lit+rim) for cc in col),0))
            elif ol:
                q=p+NW[[a,b,c]]/np.linalg.norm(NW[[a,b,c]],axis=1)[:,None]*.033
                tris.append((q[:,2].mean()+0.02,q,(.2,.12,.1),1))
    tris.sort(key=lambda t:-t[0])
    d=ImageDraw.Draw(img)
    W,H=img.size
    for z,p,col,_ in tris:
        pts=[(ox+x*scale,H-60-y*scale) for x,y,_ in p]
        d.polygon(pts,fill=tuple(int(c*255) for c in col))
img=Image.new('RGB',(1900,560),(235,225,205))
items=[('head',0,180,None),('chef',0,180,'cook'),('chef',1,200,None),('waiter',0,180,'carry'),('waiter',1,160,None),('cust',0,180,None),('cust',1,200,None),('cust',2,180,None),('cust',3,160,None),('cust',5,180,None),('cust',6,200,None),('cust',4,180,None)]
for i,(k,v,yw,ac) in enumerate(items):
    render(k,v,yw,img=img,ox=90+i*155,scale=150,action=ac)
img.save('preview.png');print('ok')
