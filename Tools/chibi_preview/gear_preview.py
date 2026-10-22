import math, numpy as np
from PIL import Image, ImageDraw
import os
HERE=os.path.dirname(os.path.abspath(__file__))
src=open(os.path.join(HERE,'render.py'),encoding='utf-8').read()
exec(src.split("img=Image.new('RGB',(1900")[0])
# --- mở rộng Kit: ma trận M, Box, Prism ---
def _vert(s,p,n,c,o=None):
    M=getattr(s,'M',None)
    p=np.array(p,float);n=np.array(n,float)
    if M is not None:
        p=M[:3,:3]@p+M[:3,3]; n=M[:3,:3]@n; n=n/(np.linalg.norm(n)+1e-9)
    s.v.append(p);s.n.append(n);s.c.append(c);return len(s.v)-1
Kit.vert=_vert
def box(s,b,size,col,ol=True):
    col=(col,1 if ol else 0);b=np.array(b,float);size=np.array(size,float);c=b+[0,size[1]/2,0];h=size/2
    for n in ([1,0,0],[-1,0,0],[0,1,0],[0,-1,0],[0,0,1],[0,0,-1]):
        n=np.array(n,float);u=np.array([1,0,0.]) if abs(n[1])>.5 else np.array([0,1,0.]);v=np.cross(n,u);st=len(s.v)
        for k in range(4):
            su=-1 if k in(0,3) else 1;sv=-1 if k<2 else 1;loc=n+u*su+v*sv;s.vert(c+loc*h,n,col)
        s.tri(st,st+1,st+2);s.tri(st,st+2,st+3)
    return s
def prism(s,ctr,pts,depth,col,ol=False):
    col=(col,1 if ol else 0);ctr=np.array(ctr,float)
    for side in(-1,1):
        n=[0,0,side];off=np.array([0,0,side*depth/2]);c=s.vert(ctr+off,n,col);st=len(s.v)
        for p in pts:s.vert(ctr+off+[p[0],p[1],0],n,col)
        for i in range(len(pts)):s.tri(c,st+i,st+(i+1)%len(pts))
    for i in range(len(pts)):
        a=np.array(pts[i]);b=np.array(pts[(i+1)%len(pts)]);e=b-a;n=np.array([e[1],-e[0],0.]);n/=np.linalg.norm(n)
        if np.dot(n[:2],(a+b)/2)<0:n=-n
        s0=s.vert(ctr+[a[0],a[1],-depth/2],n,col);s.vert(ctr+[b[0],b[1],-depth/2],n,col);s.vert(ctr+[b[0],b[1],depth/2],n,col);s.vert(ctr+[a[0],a[1],depth/2],n,col)
        s.tri(s0,s0+1,s0+2);s.tri(s0,s0+2,s0+3)
    return s
Kit.box=box;Kit.prism=prism
def star(r,n=5,inner=.45):return [(math.cos(math.pi/2+i*math.pi/n)*(r if i%2==0 else r*inner),math.sin(math.pi/2+i*math.pi/n)*(r if i%2==0 else r*inner)) for i in range(2*n)]
def TRS(p,ex=0,ey=0,ez=0):
    M=np.eye(4);M[:3,:3]=ry(ey)@rx(ex)@rz(ez);M[:3,3]=p;return M
def Cc(r,g,b):return (r/255,g/255,b/255)
W_=(.97,.97,.95);Gold=Cc(246,200,76);Red=Cc(214,38,44);Black=Cc(40,34,36);StrawG=Cc(240,214,140);Steel=Cc(206,212,222)
def m(c,k):return tuple(x*k for x in c)
y=HY
def tall(k,y,sc=1):
    k.cyl((0,y+.24,-.02),.37,.36*sc,W_);top=y+.24+.36*sc+.14
    k.ell((0,top,-.02),(.47,.3,.47),W_);k.sph((-.2,top+.06,.12),.2,W_);k.sph((.2,top+.06,.12),.2,W_)
def hat(id):
    k=Kit();k.M=None
    if id=='mu-vai':
        c=Cc(226,120,84);k.ell((0,y+.15,-.02),(.455,.32,.455),c);k.ell((0,y+.17,.36),(.3,.04,.24),m(c,.88));k.sph((0,y+.47,-.02),.05,m(c,.85))
    elif id=='non-la':k.cone((0,y+.26,0),.66,.48,StrawG);k.cyl((0,y+.26,0),.665,.02,Cc(214,186,110),False)
    elif id=='mu-dau-bep-trang':k.cyl((0,y+.24,-.02),.37,.22,W_);k.ell((0,y+.58,-.02),(.44,.24,.44),W_)
    elif id=='khan-xep-ngu-thien':
        k.ell((0,y+.22,-.02),(.49,.29,.49),Cc(240,190,60))
        for i in(-1,0,1):k.ell((.14*i,y+.26+.02*abs(i),.4-.06*abs(i)),(.09,.16,.06),Cc(214,150,40),False)
        k.cyl((0,y+.09,-.02),.455,.09,Cc(200,40,40));k.sph((0,y+.3,.47),.06,Cc(220,40,60))
    elif id=='mu-tong-bep-truong':
        tall(k,y,1.35);k.cyl((0,y+.23,-.02),.385,.11,Gold);k.prism((0,y+.285,.37),star(.075),.03,Cc(255,246,200))
    elif id=='non-quai-thao':
        k.cyl((0,y+.24,0),.36,.2,Cc(214,180,104));k.cyl((0,y+.3,0),.8,.05,StrawG);k.cyl((0,y+.35,0),.3,.06,Cc(214,180,104))
        for s in(-1,1):
            for j in range(-2,3):k.cyl((s*.72,y-.02,j*.06),.022,.32,Black,False,seg=6)
    elif id=='mu-phuong-hoang':
        f=[Cc(240,90,40),Cc(250,150,40),Cc(250,200,60),Cc(250,150,40),Cc(240,90,40)]
        for i in range(-2,3):
            k.M=TRS((i*.12,y+.36,-.06),ez=-i*22);k.ell((0,.28,0),(.09,.3,.05),f[i+2]);k.M=None
        k.ell((0,y+.38,-.02),(.43,.1,.43),Gold);k.cyl((0,y+.18,-.02),.455,.22,Gold);k.sph((0,y+.29,.46),.07,Cc(220,30,50))
    elif id=='ult-tet-3':
        k.ell((0,y+.22,-.02),(.48,.34,.48),Cc(214,34,40));k.cyl((0,y+.08,-.02),.47,.11,Black);k.cyl((0,y+.115,-.02),.48,.035,Gold,False)
        for s in(-1,1):
            k.ell((s*.74,y+.36,-.12),(.3,.09,.035),Black);k.ell((s*.74,y+.36,-.09),(.22,.055,.02),Cc(214,34,40),False);k.cyl((s*.4,y+.3,-.12),.035,.1,Black,False,seg=8)
        k.ell((0,y+.33,.45),(.12,.12,.03),Gold);k.box((0,y+.29,.47),(.07,.07,.02),Cc(170,110,30),False)
    elif id=='ult-trungthu-3':
        k.cyl((0,y+.2,0),.74,.04,StrawG);k.cyl((0,y+.2,0),.38,.26,Cc(242,210,120));k.ell((0,y+.46,0),(.38,.07,.38),Cc(242,210,120))
        k.cyl((0,y+.24,0),.39,.08,Cc(70,150,80))
        for i in(-1,0,1):k.ell((.16*i,y+.3,.37-.05*abs(i)),(.06,.1,.03),Cc(110,190,100))
        k.sph((.24,y+.4,.3),.08,Cc(255,230,120))
    elif id=='ult-quockhanh-3':
        g=Cc(110,140,80);k.cyl((0,y+.13,-.02),.62,.035,m(g,.88));k.ell((0,y+.22,-.02),(.48,.32,.48),g);k.sph((0,y+.55,-.02),.05,m(g,.85))
        k.ell((0,y+.3,.44),(.11,.11,.03),Red);k.prism((0,y+.3,.47),star(.08),.02,Cc(255,222,50))
    elif id=='ult-noel-3':
        k.M=TRS((0,y+.28,-.02),ez=-28);k.cone((0,0,0),.42,.66,Red);M=k.M;tip=M[:3,:3]@np.array([0,.66,0])+M[:3,3];k.M=None
        k.sph(tip,.12,W_);k.cyl((0,y+.16,-.02),.49,.15,W_)
    else:tall(k,y)
    return k
AY,AZ,ARx,ARy,ARz=.62,.19,.3,.3,.14
def on(x,yy,lift=.006):
    u=x/ARx;v=(yy-AY)/ARy;return (x,yy,AZ+ARz*math.sqrt(max(0,1-u*u-v*v))+lift)
def dot(k,x,yy,r,c):k.ell(on(x,yy),(r,r,.02),c,False,10,6)
def flower(k,x,yy,r,p,c):
    for i in range(5):
        a=math.pi/2+i*math.pi*2/5;dot(k,x+math.cos(a)*r*.6,yy+math.sin(a)*r*.6,r*.45,p)
    k.ell(on(x,yy,.012),(r*.3,r*.3,.02),c,False,8,5)
def apron(id):
    k=Kit();k.M=None;dec=[]
    if id=='tap-de-vai':main=Cc(234,218,184);dec=[lambda:k.ell(on(0,.5,-.01),(.12,.07,.03),Cc(214,194,150))]
    elif id=='tap-de-bo':main=Cc(74,112,170);dec=[lambda:k.ell(on(0,.5,-.01),(.12,.07,.03),Cc(64,98,152))]
    elif id=='tap-de-lua-ha-dong':main=Cc(238,158,196);dec=[lambda:flower(k,-.12,.55,.07,Cc(255,250,252),Cc(250,210,120)),lambda:flower(k,.13,.7,.055,Cc(255,250,252),Cc(250,210,120))]
    elif id=='tap-de-theu-rong':
        main=Cc(200,40,40)
        def d():
            for i in range(14):
                t=i/13;dot(k,-.18+.34*t,.42+.38*t+.05*math.sin(t*math.pi*3),.028,Gold)
        dec=[d]
    elif id=='ult-tet-2':
        main=Cc(206,30,40);dec=[lambda:[flower(k,*a,Cc(255,216,50),Cc(230,120,30)) for a in((-.13,.48,.07),(.1,.62,.08),(-.06,.78,.06),(.16,.44,.05))]]
    elif id=='ult-quockhanh-2':main=Cc(220,36,40);dec=[lambda:k.prism(on(0,.6,.008),star(.17),.02,Cc(255,222,50))]
    elif id=='ult-trungthu-2':main=Cc(40,46,104);dec=[lambda:(k.prism(on(0,.6,.008),star(.17),.02,Cc(255,210,60)),k.prism(on(0,.6,.022),star(.1),.02,Cc(240,70,50)))]
    elif id=='ult-noel-2':
        main=Cc(214,38,44);dec=[lambda:(k.ell((0,.4,AZ),(.27,.07,.13),W_),k.ell((0,.64,AZ+.005),(.305,.045,.145),Black,False),k.box(on(0,.62,0),(.1,.08,.03),Gold,False))]
    elif id=='tap-de-tho-cam':
        main=Cc(40,34,40);cols=[Cc(214,50,50),Cc(250,196,60),Cc(60,150,90),Cc(60,110,200),Cc(240,240,230)]
        def d():
            for i in range(5):
                yy=.4+i*.095;k.ell(on(0,yy,0),(.29*math.sqrt(max(.1,1-((yy-AY)/ARy)**2)),.026,.04),cols[i],False,12,6)
        dec=[d]
    else:main=W_
    k.ell((0,AY,AZ),(ARx,ARy,ARz),main)
    for s in(-1,1):k.ell((.12*s,.93,.25),(.035,.1,.04),m(main,.85),False)
    for f in dec:f()
    return k
def knife(id):
    k=Kit();k.M=None;shape='chef';blade=Steel;handle=Cc(139,90,52);bol=Cc(150,150,160);dec=[]
    if id=='dao-pho-gia-truyen':shape='cleaver';handle=Cc(196,62,50)
    if id=='dao-vang-ngu-thien':blade=Cc(246,200,76);handle=Cc(170,36,40);bol=Cc(250,220,120)
    if id=='dao-lieu-diep':shape='willow';blade=Cc(220,226,236);handle=Cc(40,40,46)
    if id=='ult-quockhanh-0':shape='cleaver';blade=Cc(214,42,40);handle=Cc(110,30,26);bol=Cc(250,210,80);dec=[('star',-.68,.07,Cc(255,222,60))]
    if id=='ult-tet-0':blade=Cc(250,206,80);handle=Cc(200,30,36);bol=Cc(250,226,130)
    if id=='ult-noel-0':blade=Cc(170,220,250);handle=Cc(236,244,255);bol=Cc(120,170,220);dec=[('star',-.68,.05,(1,1,1))]
    if id=='ult-trungthu-0':shape='willow';blade=Cc(226,232,255);handle=Cc(150,120,200);dec=[('star',-.7,.04,Cc(255,214,90))]
    k.cyl((0,-.5,0),.042,.22,handle,seg=10)
    w=.2 if shape=='cleaver' else .075 if shape=='willow' else .13;ln=.3 if shape=='cleaver' else .36
    k.box((0,-.54,w*.5-.03),(.06,.04,w+.03),bol);k.box((0,-.54-ln,w*.5-.02),(.025,ln,w),blade)
    if shape!='cleaver':
        k.M=TRS((0,-.54-ln,w*.5-.02),ey=90);g=np.array([w/6,-.3*w])
        k.prism((g[0],g[1],0),[tuple(np.array(p)-g) for p in((-w/2,0),(w/2,0),(w/2,-w*.9))],.025,blade,True);k.M=None
    for (_,yy,r,c) in dec:
        for s in(-1,1):
            k.M=TRS((.016*s,yy,.05),ey=90*s);k.prism((0,0,0),star(r),.006,c);k.M=None
    return k
def ware(id):
    k=Kit();k.M=None;shape='pot';body=Steel;rim=Cc(226,230,236);lid=Cc(196,202,212);knob=Black;fire=False
    if id=='chao-gang':shape='pan';body=Cc(52,52,58);rim=Cc(70,70,78)
    if id=='chao-lua-rong':shape='pan';body=Cc(46,46,54);rim=Cc(200,50,30);fire=True
    if id=='ult-tet-1':body=Cc(238,182,70);rim=Cc(206,34,38);lid=Cc(244,196,84);knob=Cc(214,40,40)
    if id=='noi-dong-cung-dinh':body=Cc(206,112,62);rim=Cc(232,176,90);lid=Cc(214,124,70);knob=Gold
    k.cyl((0,-.62,0),.035,.28,Black,seg=8)
    r=.26 if shape=='pan' else .21
    k.M=TRS((0,-.62-r*.9,-.05),ex=60)
    if shape=='pan':
        k.cyl((0,0,0),r,.07,body,top=1.15);k.cyl((0,.068,0),r*1.15,.015,rim,False);k.ell((0,.085,0),(r*.7,.03,r*.7),Cc(240,190,90),False)
    else:
        h=.2;k.cyl((0,0,0),r,h,body);k.cyl((0,h-.02,0),r*1.08,.04,rim);k.ell((0,h+.02,0),(r*.98,.08,r*.98),lid);k.sph((0,h+.11,0),.04,knob)
    if fire:k.ell((0,-.06,0),(r*.7,.06,r*.7),Cc(255,120,40),False);k.ell((0,-.035,0),(r*.45,.05,r*.45),Cc(255,214,90),False)
    k.M=None
    return k
def render2(gear,yaw=180,action=None,img=None,ox=0,scale=150):
    l=look('head',0);l['hat']=None
    tilt=rx(-28);Y=ry(yaw);M=tilt@Y
    aL=aR=0
    if action=='cook':aL,aR=-50,-72
    parts=[(torso(l),np.eye(3),np.zeros(3))]
    hz,az,kz,wz=gear
    parts.append((hat(hz),np.eye(3),np.zeros(3)))
    if az:parts.append((apron(az),np.eye(3),np.zeros(3)))
    RL=rx(aL)@rz(-10);RR=rx(aR)@rz(10)
    parts.append((arm(l),RL,np.array([-.36,.95,0])));parts.append((arm(l),RR,np.array([.36,.95,0])))
    if kz:parts.append((knife(kz),RR@ry(-55)*1.2,np.array([.36,.95,0])))
    if wz and action=='cook':parts.append((ware(wz),RL,np.array([-.36,.95,0])))
    parts.append((leg(l),np.eye(3),np.array([-.16,.42,0])));parts.append((leg(l),np.eye(3),np.array([.16,.42,0])))
    tris=[]
    for k,R,o in parts:
        if not k.v:continue
        V=np.array(k.v);N=np.array(k.n)
        Wp=(M@(R@V.T+o[:,None])).T;NW=(M@R@N.T).T
        for (a,b,c) in k.t:
            p=Wp[[a,b,c]];f=np.cross(p[1]-p[0],p[2]-p[0]);col,ol=k.c[a];n=NW[[a,b,c]].mean(0)
            if f[2]<0:
                n=n/(np.linalg.norm(n)+1e-9);L=np.array([-.45,.75,-.55]);L/=np.linalg.norm(L)
                d=np.dot(n,L)*.5+.5;lit=.84+(.16 if d>.47 else 0)
                tris.append((p[:,2].mean(),p,tuple(min(1,cc*lit) for cc in col)))
            elif ol:
                q=p+NW[[a,b,c]]/(np.linalg.norm(NW[[a,b,c]],axis=1)[:,None]+1e-9)*.025
                tris.append((q[:,2].mean()+0.02,q,(.36,.27,.24)))
    tris.sort(key=lambda t:-t[0])
    d=ImageDraw.Draw(img);H=img.size[1]
    for z,p,col in tris:
        d.polygon([(ox+x*scale,H-40-y_*scale) for x,y_,_ in p],fill=tuple(int(c*255) for c in col))
sets=[('mặc định',(None,None,None,None),None),
('Thường',('mu-vai','tap-de-vai','dao-thai-thit','noi-nhom'),'cook'),
('Hiếm',('non-la','tap-de-bo','dao-pho-gia-truyen','chao-gang'),'cook'),
('Sử thi',('mu-dau-bep-trang','tap-de-lua-ha-dong','dao-lieu-diep',None),None),
('HT1',('khan-xep-ngu-thien','tap-de-theu-rong','dao-vang-ngu-thien','noi-dong-cung-dinh'),'cook'),
('HT2',('mu-phuong-hoang','tap-de-tho-cam','dao-vang-ngu-thien','chao-lua-rong'),'cook'),
('HT3',('non-quai-thao','tap-de-tho-cam',None,None),None),
('HT4',('mu-tong-bep-truong','tap-de-theu-rong','dao-lieu-diep',None),None),
('Tết',('ult-tet-3','ult-tet-2','ult-tet-0','ult-tet-1'),'cook'),
('Trung thu',('ult-trungthu-3','ult-trungthu-2','ult-trungthu-0',None),None),
('Quốc khánh',('ult-quockhanh-3','ult-quockhanh-2','ult-quockhanh-0',None),None),
('Noel',('ult-noel-3','ult-noel-2','ult-noel-0',None),None)]
img=Image.new('RGB',(6*230,2*480),(235,225,205))
for i,(nm,g,ac) in enumerate(sets):
    sub=Image.new('RGB',(230,480),(235,225,205))
    render2(g,yaw=180 if i%2==0 else 200,action=ac,img=sub,ox=115,scale=140)
    img.paste(sub,((i%6)*230,(i//6)*480))
img.save(os.path.join(HERE,'gear_preview.png'));print('ok')
