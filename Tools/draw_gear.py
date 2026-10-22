# Vẽ icon trang bị đầu bếp (256x256, nền trong suốt) theo phong cách hoạt hình viền nâu của nhân vật chibi.
import math, os, sys
from PIL import Image, ImageDraw, ImageFilter, ImageChops

S = 4
N = 256 * S
OUTLINE = (92, 69, 61)
OUT = sys.argv[1] if len(sys.argv) > 1 else "out"
os.makedirs(OUT, exist_ok=True)


def rgba(c, a=255):
    return (int(c[0]), int(c[1]), int(c[2]), a)


def lit(c, k):
    return tuple(int(c[i] + (255 - c[i]) * k) for i in range(3))


def dk(c, k):
    return tuple(int(c[i] * (1 - k)) for i in range(3))


class D:
    """Vẽ theo toạ độ 256 (tự nhân S)."""

    def __init__(self, img):
        self.d = ImageDraw.Draw(img)

    def ell(self, x0, y0, x1, y1, f):
        self.d.ellipse([x0 * S, y0 * S, x1 * S, y1 * S], fill=f)

    def circ(self, cx, cy, r, f):
        self.ell(cx - r, cy - r, cx + r, cy + r, f)

    def poly(self, pts, f):
        self.d.polygon([(x * S, y * S) for x, y in pts], fill=f)

    def rect(self, x0, y0, x1, y1, f, r=0):
        if r > 0:
            self.d.rounded_rectangle([x0 * S, y0 * S, x1 * S, y1 * S], radius=r * S, fill=f)
        else:
            self.d.rectangle([x0 * S, y0 * S, x1 * S, y1 * S], fill=f)

    def line(self, pts, f, w):
        p = [(x * S, y * S) for x, y in pts]
        self.d.line(p, fill=f, width=max(1, int(w * S)), joint="curve")
        for x, y in (p[0], p[-1]):
            r = w * S / 2
            self.d.ellipse([x - r, y - r, x + r, y + r], fill=f)

    def arc(self, x0, y0, x1, y1, a0, a1, f, w):
        self.d.arc([x0 * S, y0 * S, x1 * S, y1 * S], a0, a1, fill=f, width=max(1, int(w * S)))

    def star(self, cx, cy, r, f, n=5, inner=0.45, rot=-90):
        pts = []
        for i in range(n * 2):
            rr = r if i % 2 == 0 else r * inner
            a = math.radians(rot + i * 180 / n)
            pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
        self.poly(pts, f)

    def sparkle(self, cx, cy, r, f):
        self.star(cx, cy, r, f, n=4, inner=0.22, rot=-90)


def shift(m, dx, dy):
    n = Image.new("L", m.size, 0)
    n.paste(m, (int(dx), int(dy)))
    return n


def curve(p0, p1, p2, n=16):
    """Đường cong bậc 2 từ p0 qua điều khiển p1 tới p2."""
    out = []
    for i in range(n + 1):
        t = i / n
        x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0]
        y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]
        out.append((x, y))
    return out


class Art:
    def __init__(self):
        self.img = Image.new("RGBA", (N, N), (0, 0, 0, 0))

    def piece(self, shape, color, deco=None, shade=0.24, light=0.30, edge=True):
        m = Image.new("L", (N, N), 0)
        shape(D(m), 255)
        base = Image.new("RGBA", (N, N), (0, 0, 0, 0))
        base.paste(Image.new("RGBA", (N, N), rgba(color)), (0, 0), m)
        if deco:
            dl = Image.new("RGBA", (N, N), (0, 0, 0, 0))
            deco(D(dl))
            dl.putalpha(ImageChops.multiply(dl.split()[3], m))
            base.alpha_composite(dl)
        if shade:
            sh = ImageChops.subtract(m, shift(m, -6 * S, -6 * S)).filter(ImageFilter.GaussianBlur(2 * S))
            sh = ImageChops.multiply(sh, m).point(lambda v: int(v * shade))
            lay = Image.new("RGBA", (N, N), rgba((40, 20, 10)))
            lay.putalpha(sh)
            base.alpha_composite(lay)
        if light:
            hl = ImageChops.subtract(shift(m, 2 * S, 2 * S), shift(m, 7 * S, 7 * S))
            hl = ImageChops.multiply(hl.filter(ImageFilter.GaussianBlur(2 * S)), m).point(lambda v: int(v * light))
            lay = Image.new("RGBA", (N, N), (255, 255, 255, 255))
            lay.putalpha(hl)
            base.alpha_composite(lay)
        if edge:
            e = ImageChops.subtract(m, m.filter(ImageFilter.MinFilter(7)))
            lay = Image.new("RGBA", (N, N), rgba(OUTLINE))
            lay.putalpha(e.point(lambda v: int(v * 0.85)))
            base.alpha_composite(lay)
        self.img.alpha_composite(base)
        return self

    def draw(self, fn):
        """Chi tiết vẽ thẳng lên (không bóng, không viền)."""
        lay = Image.new("RGBA", (N, N), (0, 0, 0, 0))
        fn(D(lay))
        self.img.alpha_composite(lay)
        return self

    def paste(self, other, angle=0, scale=1.0, dx=0, dy=0):
        im = other.img if isinstance(other, Art) else other
        if angle:
            im = im.rotate(angle, resample=Image.BICUBIC, center=(N / 2, N / 2))
        if scale != 1.0:
            sz = int(N * scale)
            im = im.resize((sz, sz), Image.LANCZOS)
            canvas = Image.new("RGBA", (N, N), (0, 0, 0, 0))
            off = (N - sz) // 2
            canvas.alpha_composite(im, (off + int(dx * S), off + int(dy * S))) if off >= 0 else None
            im = canvas
        elif dx or dy:
            canvas = Image.new("RGBA", (N, N), (0, 0, 0, 0))
            canvas.alpha_composite(im, (int(dx * S), int(dy * S)))
            im = canvas
        self.img.alpha_composite(im)
        return self


def finish(art, name, glow=None, sparkles=None):
    a = art.img.split()[3]
    dil = a.filter(ImageFilter.MaxFilter(15))
    out = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    if glow:
        g = Image.new("L", (N, N), 0)
        D(g).circ(128, 132, 104, 255)
        g = g.filter(ImageFilter.GaussianBlur(26 * S))
        lay = Image.new("RGBA", (N, N), rgba(glow))
        lay.putalpha(g.point(lambda v: int(v * 0.85)))
        out.alpha_composite(lay)
    ol = Image.new("RGBA", (N, N), rgba(OUTLINE))
    ol.putalpha(dil)
    out.alpha_composite(ol)
    out.alpha_composite(art.img)
    if sparkles:
        col, pts = sparkles
        lay = Image.new("RGBA", (N, N), (0, 0, 0, 0))
        d = D(lay)
        for (x, y, r) in pts:
            d.sparkle(x, y, r + 3, rgba(OUTLINE, 200))
            d.sparkle(x, y, r, rgba(col))
            d.circ(x, y, r * 0.25, (255, 255, 255, 255))
        out.alpha_composite(lay)
    out = out.resize((256, 256), Image.LANCZOS)
    out.save(os.path.join(OUT, name + ".png"))
    return out


LEG = ((255, 200, 90), [(36, 48, 9), (214, 206, 7), (206, 40, 6)])
ULT = ((255, 225, 120), [(34, 44, 11), (220, 210, 9), (210, 36, 8), (40, 214, 6)])
ULT_GLOW = (255, 70, 60)

# ======================= DAO =======================
STEEL = (206, 212, 222)


def knife(kind="chef", blade=STEEL, handle=(139, 90, 52), bolster=(120, 120, 130), blade_deco=None,
          handle_deco=None, rivets=True, edge_col=None):
    a = Art()
    hx0, hx1 = 113, 143
    # cán
    def hdeco(d):
        if rivets:
            for y in (176, 204):
                d.circ(128, y, 4.5, rgba((235, 215, 160)))
        if handle_deco:
            handle_deco(d)
    a.piece(lambda d, f: d.rect(hx0, 152, hx1, 230, f, 12), handle, hdeco)
    # lưỡi
    if kind == "chef":
        edge = [(152, 150)] + [(152 - 38 * t ** 2, 112 - 90 * t) for t in [i / 16 for i in range(1, 17)]]
        pts = [(108, 150)] + edge[::-1][:0] + edge + [(108, 40)]
        pts = [(108, 150)] + edge + [(108, 44)]
        # sắp lại: sống dao bên trái, lưỡi bên phải
        pts = [(108, 44), (108, 150)] + edge
    elif kind == "cleaver":
        pts = [(94, 44), (162, 36), (162, 150), (94, 150)]
    elif kind == "willow":
        edge = [(140, 150)] + [(140 - 18 * t ** 1.6, 124 - 104 * t) for t in [i / 16 for i in range(1, 17)]]
        pts = [(118, 44), (118, 150)] + edge
    elif kind == "crescent":
        outer = [(108 + 60 * math.sin(math.radians(t)) * 0.75, 150 - 120 * (t / 180)) for t in range(0, 181, 10)]
        inner = [(108 + 30 * math.sin(math.radians(t)) * 0.75, 150 - 120 * (t / 180)) for t in range(180, -1, -10)]
        pts = [(108, 150)] + outer + inner
        pts = [(x, y) for x, y in outer] + [(x - 0, y) for x, y in inner]
        pts = [(106, 150), (150, 150)] + [(150 - 40 * t ** 2 + 10 * math.sin(t * math.pi), 150 - 128 * t) for t in [i / 20 for i in range(1, 21)]] \
              + [(110 + 6 * math.sin(t * math.pi), 22 + 128 * t) for t in [i / 20 for i in range(1, 20)]]
    else:
        raise ValueError(kind)

    def bdeco(d):
        # ánh sáng dọc sống dao
        if kind != "crescent":
            d.poly([(112, 52), (122, 50), (122, 146), (112, 146)], rgba(lit(blade, 0.45), 150))
        if edge_col:
            ep = [p for p in pts if p[0] > 128 or kind == "cleaver"]
        if blade_deco:
            blade_deco(d)
    a.piece(lambda d, f: d.poly(pts, f), blade, bdeco, light=0.45)
    if edge_col:
        a.draw(lambda d: d.line(pts[2:] if kind != "cleaver" else [(162, 38), (162, 150)], rgba(edge_col, 230), 5))
    # chắn tay
    a.piece(lambda d, f: d.rect(104, 146, 152, 162, f, 5), bolster)
    return a


def knife_icon(name, glow=None, sparkles=None, angle=-38, **kw):
    k = knife(**kw)
    art = Art().paste(k, angle=angle, dy=4)
    return finish(art, name, glow, sparkles)


def hammered(d):
    for (x, y) in [(116, 70), (126, 86), (118, 104), (130, 118), (116, 132), (134, 64), (140, 100)]:
        d.circ(x, y, 5, rgba((110, 116, 128), 170))


def wrap(color):
    def f(d):
        for y in range(160, 228, 12):
            d.line([(113, y), (143, y + 6)], rgba(color), 3)
    return f


def gold_swirl(d):
    d.arc(112, 70, 148, 106, 200, 470, rgba((150, 90, 20), 230), 3)
    d.arc(120, 96, 144, 120, 0, 270, rgba((150, 90, 20), 230), 3)


def dragon_scales(d):
    for row in range(6):
        for col in range(3):
            x = 114 + col * 12 + (6 if row % 2 else 0)
            y = 60 + row * 15
            d.arc(x - 7, y - 7, x + 7, y + 7, 0, 180, rgba((190, 40, 30), 230), 3)


def leaf_engrave(d):
    d.line([(124, 140), (128, 52)], rgba((140, 150, 165), 220), 2)
    for i, y in enumerate(range(130, 60, -16)):
        d.line([(126, y), (134, y - 10)], rgba((140, 150, 165), 220), 2)


def jade_veins(d):
    d.line([(116, 140), (130, 110), (122, 80), (134, 56)], rgba((200, 245, 220), 200), 3)
    d.line([(130, 110), (146, 98)], rgba((200, 245, 220), 160), 2)


def snowflake_at(cx, cy, r, col):
    def f(d):
        for k in range(6):
            a = math.radians(k * 60 + 90)
            x, y = cx + r * math.cos(a), cy + r * math.sin(a)
            d.line([(cx, cy), (x, y)], col, 3)
            for s in (0.55,):
                bx, by = cx + r * s * math.cos(a), cy + r * s * math.sin(a)
                for da in (-40, 40):
                    b = a + math.radians(da)
                    d.line([(bx, by), (bx + r * 0.3 * math.cos(b), by + r * 0.3 * math.sin(b))], col, 2.5)
        d.circ(cx, cy, 3.5, col)
    return f


def draw_knives():
    knife_icon("dao-thai-thit", kind="chef")
    knife_icon("dao-da-sy", kind="chef", blade=(160, 168, 182), handle=(98, 60, 36), bolster=(196, 150, 70), blade_deco=hammered)
    knife_icon("dao-pho-gia-truyen", kind="cleaver", handle=(196, 62, 50), handle_deco=wrap((120, 30, 25)), rivets=False,
               blade_deco=lambda d: d.circ(146, 54, 6, rgba((90, 80, 80))))
    knife_icon("dao-vang-ngu-thien", LEG and None, LEG, kind="chef", blade=(246, 200, 76), handle=(170, 36, 40),
               bolster=(250, 220, 120), blade_deco=gold_swirl)
    # Bộ ba dao xếp song song chéo
    art = Art()
    for off, kind in ((-52, "cleaver"), (0, "chef"), (52, "willow")):
        k = knife(kind=kind, handle=(160, 40, 40), bolster=(240, 200, 90))
        art.paste(k, angle=-38, scale=0.8, dx=off * 0.8 + 6, dy=off * 0.62 - 2)
    finish(art, "dao-ngu-thien-phong", None, LEG)
    knife_icon("dao-than-bep", None, LEG, kind="chef", blade=(78, 80, 100), handle=(40, 36, 44), bolster=(230, 120, 40),
               edge_col=(255, 150, 40), blade_deco=lambda d: d.line([(116, 140), (118, 60)], rgba((255, 120, 40), 160), 3))
    knife_icon("dao-ngoc-bich", None, LEG, kind="chef", blade=(96, 196, 146), handle=(240, 236, 222), bolster=(232, 190, 80),
               blade_deco=jade_veins)
    knife_icon("dao-lieu-diep", None, LEG, kind="willow", blade=(220, 226, 236), handle=(40, 40, 46), bolster=(200, 200, 210),
               blade_deco=leaf_engrave)
    # Tối thượng
    knife_icon("ult-tet-0", ULT_GLOW, ULT, kind="chef", blade=(250, 206, 80), handle=(200, 30, 36), bolster=(250, 226, 130),
               blade_deco=dragon_scales, edge_col=(255, 240, 180))
    knife_icon("ult-trungthu-0", ULT_GLOW, ULT, kind="crescent", blade=(226, 232, 255), handle=(150, 120, 200), bolster=(250, 220, 120),
               blade_deco=lambda d: d.star(128, 104, 11, rgba((255, 214, 90))))
    knife_icon("ult-quockhanh-0", ULT_GLOW, ULT, kind="cleaver", blade=(214, 42, 40), handle=(110, 30, 26), bolster=(250, 210, 80),
               blade_deco=lambda d: d.star(128, 96, 26, rgba((255, 222, 60))), rivets=True)
    knife_icon("ult-noel-0", ULT_GLOW, ULT, kind="chef", blade=(170, 220, 250), handle=(236, 244, 255), bolster=(120, 170, 220),
               blade_deco=snowflake_at(126, 100, 18, rgba((255, 255, 255))), edge_col=(230, 250, 255))


# ======================= NỒI =======================

def pot(body, lid=None, rim=None, knob=None, handles=None, body_deco=None, lid_deco=None, tall=False, knob_shape=None,
        handle_shape=None, steam=False):
    a = Art()
    rim = rim or lit(body, 0.15)
    lid = lid or body
    knob = knob or (60, 50, 50)
    handles = handles or (60, 50, 50)
    top = 92 if tall else 108
    if steam:
        for x in (96, 128, 160):
            a.draw(lambda d, x=x: d.line(curve((x, top - 30), (x - 14, top - 52), (x + 6, top - 80)), (255, 255, 255, 170), 7))
    # quai hai bên
    if handle_shape:
        handle_shape(a, handles, top)
    else:
        a.piece(lambda d, f: (d.rect(26, top + 18, 64, top + 38, f, 9), d.rect(192, top + 18, 230, top + 38, f, 9)), handles)
    # thân
    a.piece(lambda d, f: (d.rect(48, top + 6, 208, 216, f, 30), d.rect(48, top + 6, 208, 170, f, 6)), body, body_deco, light=0.35)
    # miệng nồi
    a.piece(lambda d, f: d.ell(40, top - 6, 216, top + 24, f), rim)
    # nắp
    a.piece(lambda d, f: (d.ell(56, top - 46, 200, top + 18, f), d.rect(56, top - 14, 200, top + 4, f)), lid, lid_deco, light=0.4)
    if knob_shape:
        knob_shape(a, knob, top)
    else:
        a.piece(lambda d, f: (d.rect(116, top - 60, 140, top - 40, f, 4), d.ell(108, top - 70, 148, top - 50, f)), knob)
    return a


def band(col, y0, y1):
    return lambda d: d.rect(40, y0, 216, y1, rgba(col))


def meander(col, y):
    def f(d):
        d.rect(40, y - 12, 216, y + 12, rgba(dk(col, 0.25)))
        for x in range(46, 210, 22):
            d.line([(x, y + 6), (x, y - 6), (x + 12, y - 6), (x + 12, y + 2), (x + 6, y + 2)], rgba(col), 3)
    return f


def coin(d, cx, cy, r, col=(255, 214, 80)):
    d.circ(cx, cy, r, rgba(dk(col, 0.25)))
    d.circ(cx, cy, r - 3, rgba(col))
    d.rect(cx - r * 0.3, cy - r * 0.3, cx + r * 0.3, cy + r * 0.3, rgba(dk(col, 0.35)))


def flames(a, cx, base, w, h, cols=((255, 90, 40), (255, 170, 50), (255, 236, 140))):
    for i, col in enumerate(cols):
        k = 1 - i * 0.28
        ww, hh = w * k, h * k
        pts = [(cx - ww / 2, base)] + curve((cx - ww / 2, base), (cx - ww * 0.6, base - hh * 0.6), (cx - ww * 0.15, base - hh)) \
              + curve((cx - ww * 0.15, base - hh), (cx, base - hh * 0.5), (cx + ww * 0.12, base - hh * 0.85)) \
              + curve((cx + ww * 0.12, base - hh * 0.85), (cx + ww * 0.7, base - hh * 0.55), (cx + ww / 2, base))
        a.piece(lambda d, f, pts=pts: d.poly(pts, f), col, shade=0, light=0, edge=(i == 0))


def draw_pots():
    a = pot((200, 206, 214), knob=(50, 46, 46), handles=(50, 46, 46))
    finish(a, "noi-nhom")

    # Chảo gang
    a = Art()
    a.piece(lambda d, f: d.poly([(160, 124), (236, 70), (248, 86), (176, 142)], f), (60, 44, 36))
    a.piece(lambda d, f: d.ell(18, 96, 190, 200, f), (52, 52, 58))
    a.piece(lambda d, f: d.ell(32, 104, 176, 184, f), (34, 34, 40), light=0.12,
            deco=lambda d: d.arc(46, 112, 140, 170, 160, 250, rgba((120, 120, 130), 200), 4))
    a.draw(lambda d: d.circ(236, 80, 4, rgba((20, 16, 14))))
    finish(a, "chao-gang")

    a = pot((214, 220, 228), tall=True, steam=False, knob=(40, 40, 40), handles=(40, 40, 40),
            body_deco=lambda d: (d.rect(40, 150, 216, 156, rgba((160, 166, 176))), d.rect(40, 186, 216, 192, rgba((160, 166, 176)))))
    finish(a, "noi-nuoc-dung")

    copper = (206, 112, 62)
    a = pot(copper, lid=(214, 124, 70), rim=(232, 176, 90), knob=(246, 206, 90), handles=(222, 170, 80),
            body_deco=meander((246, 206, 90), 160))
    finish(a, "noi-dong-cung-dinh", None, LEG)

    # Bếp lò Hoàng cung: lò đất nung + lửa + viền vàng
    a = Art()
    flames(a, 128, 82, 90, 70)
    a.piece(lambda d, f: d.poly([(54, 96), (202, 96), (190, 214), (66, 214)], f), (190, 92, 64),
            deco=lambda d: (d.rect(40, 96, 216, 110, rgba((246, 206, 90))), d.rect(40, 198, 216, 214, rgba((246, 206, 90)))))
    a.piece(lambda d, f: d.ell(46, 82, 210, 112, f), (150, 66, 46))
    a.draw(lambda d: d.ell(70, 88, 186, 106, rgba((60, 24, 18))))
    a.piece(lambda d, f: (d.rect(96, 150, 160, 200, f), d.ell(96, 126, 160, 176, f)), (50, 22, 18), light=0)
    flames(a, 128, 198, 46, 56)
    finish(a, "bep-lo-hoang-cung", None, LEG)

    # Nồi áp suất: quai dài trên nắp, đồng hồ đỏ, tia sét
    def ps_knob(a, col, top):
        a.piece(lambda d, f: d.rect(58, top - 58, 222, top - 40, f, 9), (40, 40, 48))
        a.piece(lambda d, f: d.circ(96, top - 40, 15, f), (240, 240, 240),
                deco=lambda d: (d.line([(96, top - 40), (104, top - 49)], rgba((220, 40, 40)), 3), d.circ(96, top - 40, 3, rgba((40, 40, 40)))))
    a = pot((176, 196, 220), lid=(150, 170, 196), knob_shape=ps_knob, handles=(40, 40, 48),
            body_deco=lambda d: d.poly([(140, 128), (114, 170), (130, 170), (118, 206), (152, 156), (134, 156), (150, 128)], rgba((255, 214, 50))))
    finish(a, "noi-ap-suat-than-toc", None, LEG)

    # Chảo lửa Rồng
    a = Art()
    flames(a, 110, 120, 150, 100, ((220, 40, 30), (255, 120, 40), (255, 210, 90)))
    a.piece(lambda d, f: d.poly([(170, 140), (238, 110), (246, 126), (178, 156)], f), (40, 30, 26))
    a.piece(lambda d, f: (d.ell(20, 104, 200, 204, f),), (46, 46, 54))
    a.piece(lambda d, f: d.ell(30, 108, 190, 156, f), (28, 28, 34), light=0.1)
    flames(a, 108, 140, 100, 64, ((230, 50, 30), (255, 150, 50), (255, 230, 120)))
    finish(a, "chao-lua-rong", None, LEG)

    # Tối thượng
    gold = (238, 182, 70)
    a = pot(gold, lid=(244, 196, 84), rim=(250, 220, 120), knob=(214, 40, 40), handles=(214, 40, 40),
            body_deco=lambda d: (d.rect(40, 140, 216, 196, rgba((206, 34, 38))),
                                 [coin(d, x, 168, 13) for x in (68, 98, 128, 158, 188)]))
    finish(a, "ult-tet-1", ULT_GLOW, ULT)

    def moon_knob(a, col, top):
        a.piece(lambda d, f: d.circ(128, top - 58, 20, f), (255, 226, 120))
        a.draw(lambda d: d.circ(138, top - 64, 17, (0, 0, 0, 0)))
    def moon_knob2(a, col, top):
        m = Art()
        m.piece(lambda d, f: d.circ(128, top - 58, 20, f), (255, 226, 120))
        cut = Image.new("L", (N, N), 0)
        D(cut).circ(140, top - 66, 17, 255)
        al = ImageChops.subtract(m.img.split()[3], cut)
        m.img.putalpha(al)
        a.img.alpha_composite(m.img)
    def clouds(d):
        for (x, y) in ((76, 170), (176, 182), (128, 196)):
            d.circ(x, y, 10, rgba((200, 186, 240)))
            d.circ(x + 12, y - 4, 12, rgba((200, 186, 240)))
            d.circ(x + 24, y, 9, rgba((200, 186, 240)))
    a = pot((244, 240, 255), lid=(226, 220, 250), rim=(214, 200, 246), knob_shape=moon_knob2, handles=(176, 150, 230),
            body_deco=clouds)
    finish(a, "ult-trungthu-1", ULT_GLOW, ULT)

    a = pot((214, 40, 40), lid=(226, 56, 50), rim=(246, 206, 80), knob=(250, 214, 70), handles=(250, 214, 70),
            body_deco=lambda d: d.star(128, 166, 34, rgba((255, 222, 60))))
    finish(a, "ult-quockhanh-1", ULT_GLOW, ULT)

    def antlers(a, col, top):
        for s in (-1, 1):
            x0 = 128 + s * 78
            pts = [(x0, top + 26), (x0 + s * 24, top - 20), (x0 + s * 6, top - 50)]
            a.piece(lambda d, f, pts=pts, x0=x0: (d.line(pts, f, 11), d.line([(x0 + s * 18, top - 8), (x0 + s * 42, top - 28)], f, 9),
                                                  d.line([(x0 + s * 12, top - 30), (x0 + s * 30, top - 58)], f, 8)), (150, 104, 60))
    def nose(a, col, top):
        a.piece(lambda d, f: d.circ(128, top - 56, 15, f), (226, 40, 40), light=0.6)
    a = pot((150, 96, 60), lid=(170, 116, 74), rim=(240, 236, 226), handle_shape=antlers, knob_shape=nose,
            body_deco=lambda d: (d.circ(100, 160, 8, rgba((40, 30, 26))), d.circ(156, 160, 8, rgba((40, 30, 26))),
                                 d.circ(103, 157, 3, rgba((255, 255, 255))), d.circ(159, 157, 3, rgba((255, 255, 255))),
                                 d.circ(84, 182, 9, rgba((240, 140, 130), 160)), d.circ(172, 182, 9, rgba((240, 140, 130), 160)),
                                 d.rect(40, 200, 216, 216, rgba((60, 130, 70)))))
    finish(a, "ult-noel-1", ULT_GLOW, ULT)


# ======================= TẠP DỀ =======================

APRON = [(92, 50), (164, 50), (166, 104), (194, 116), (204, 206)] + curve((204, 206), (204, 232), (178, 232)) \
        + [(78, 232)] + curve((78, 232), (52, 232), (52, 206)) + [(62, 116), (90, 104)]


def apron(color, deco=None, pocket=None, strap=None, ties=None, pocket_deco=None):
    a = Art()
    strap = strap or dk(color, 0.2)
    ties = ties or dk(color, 0.2)
    a.draw(lambda d: None)
    a.piece(lambda d, f: (d.arc(84, 6, 172, 96, 180, 360, f, 9),), strap, shade=0, light=0)
    a.piece(lambda d, f: (d.line([(62, 120), (30, 138), (22, 168)], f, 9), d.line([(194, 120), (226, 138), (234, 168)], f, 9)), ties)
    a.piece(lambda d, f: d.poly(APRON, f), color, deco, light=0.32)
    if pocket:
        a.piece(lambda d, f: d.rect(98, 150, 158, 196, f, 8), pocket, pocket_deco)
    return a


def stitches(col):
    def f(d):
        for i in range(0, 30):
            t = i / 30
        pts = APRON
        for i in range(len(pts) - 1):
            (x0, y0), (x1, y1) = pts[i], pts[i + 1]
            L = math.hypot(x1 - x0, y1 - y0)
            n = int(L / 9)
            cx, cy = 128, 140
            for k in range(n):
                t0, t1 = k / n, (k + 0.5) / n
                p0 = (x0 + (x1 - x0) * t0, y0 + (y1 - y0) * t0)
                p1 = (x0 + (x1 - x0) * t1, y0 + (y1 - y0) * t1)
                # đẩy vào trong 7 px
                def inset(p):
                    vx, vy = cx - p[0], cy - p[1]
                    l = math.hypot(vx, vy) or 1
                    return (p[0] + vx / l * 8, p[1] + vy / l * 8)
                d.line([inset(p0), inset(p1)], rgba(col, 200), 2)
    return f


def flower5(d, cx, cy, r, petal, center):
    for k in range(5):
        a = math.radians(k * 72 - 90)
        d.circ(cx + r * 0.62 * math.cos(a), cy + r * 0.62 * math.sin(a), r * 0.5, rgba(petal))
    d.circ(cx, cy, r * 0.33, rgba(center))


def draw_aprons():
    finish(apron((234, 218, 184), stitches((170, 140, 100)), pocket=(220, 200, 160)), "tap-de-vai")
    finish(apron((74, 112, 170), stitches((230, 190, 110)), pocket=(64, 98, 152),
                 pocket_deco=lambda d: (d.circ(104, 156, 3, rgba((230, 190, 110))), d.circ(152, 156, 3, rgba((230, 190, 110))))),
           "tap-de-bo")
    finish(apron((238, 158, 196), lambda d: (d.poly([(70, 120), (110, 104), (190, 220), (150, 232)], rgba((255, 220, 236), 120)),
                                              flower5(d, 92, 170, 16, (255, 250, 252), (250, 210, 120)),
                                              flower5(d, 164, 140, 12, (255, 250, 252), (250, 210, 120)),
                                              flower5(d, 150, 206, 10, (255, 250, 252), (250, 210, 120))),
                 strap=(214, 120, 170), ties=(214, 120, 170)), "tap-de-lua-ha-dong")

    def dragon(col):
        def f(d):
            pts = []
            for i in range(60):
                t = i / 59
                pts.append((80 + 96 * t, 210 - 110 * t + 18 * math.sin(t * math.pi * 3)))
            d.line(pts, rgba(col), 7)
            d.circ(pts[-1][0], pts[-1][1], 9, rgba(col))
            for i in range(4, 56, 6):
                x, y = pts[i]
                d.line([(x, y), (x - 4, y - 10)], rgba(col), 3)
            d.circ(pts[-1][0] + 2, pts[-1][1] - 2, 3, rgba((180, 20, 20)))
        return f
    finish(apron((200, 40, 40), dragon((250, 206, 80)), strap=(250, 206, 80), ties=(250, 206, 80)), "tap-de-theu-rong", None, LEG)

    def brocade(d):
        for y in range(56, 240, 26):
            for x in range(52, 210, 26):
                ox = 13 if (y // 26) % 2 else 0
                cx = x + ox
                d.poly([(cx, y - 8), (cx + 8, y), (cx, y + 8), (cx - 8, y)], rgba((200, 130, 30), 220))
                d.circ(cx, y, 2.5, rgba((255, 236, 160)))
    finish(apron((244, 194, 64), brocade, strap=(190, 40, 40), ties=(190, 40, 40)), "tap-de-gam-hoang-gia", None, LEG)

    def vanphuc(d):
        for (x, y) in ((92, 130), (164, 130), (128, 172), (92, 212), (164, 212), (128, 84)):
            d.circ(x, y, 15, rgba((150, 220, 220), 230))
            d.circ(x, y, 10, rgba((60, 150, 160)))
            d.rect(x - 6, y - 1.5, x + 6, y + 1.5, rgba((150, 220, 220)))
            d.rect(x - 1.5, y - 6, x + 1.5, y + 6, rgba((150, 220, 220)))
    finish(apron((54, 150, 162), vanphuc, strap=(40, 110, 120), ties=(40, 110, 120)), "tap-de-lua-van-phuc", None, LEG)

    def thocam(d):
        cols = [(214, 50, 50), (250, 196, 60), (60, 150, 90), (60, 110, 200), (240, 240, 230)]
        for i, y in enumerate(range(70, 240, 28)):
            c = cols[i % len(cols)]
            d.rect(40, y - 9, 216, y + 9, rgba(dk(c, 0.15)))
            for x in range(44, 216, 16):
                d.poly([(x, y), (x + 8, y - 7), (x + 16, y), (x + 8, y + 7)], rgba(lit(c, 0.25)))
    finish(apron((40, 34, 40), thocam, strap=(214, 50, 50), ties=(214, 50, 50)), "tap-de-tho-cam", None, LEG)

    # Tối thượng
    def mai(d):
        d.line(curve((60, 226), (120, 170), (190, 92)), rgba((110, 60, 40)), 5)
        d.line(curve((120, 172), (150, 180), (176, 200)), rgba((110, 60, 40)), 4)
        for (x, y, r) in ((96, 196, 14), (138, 150, 16), (176, 104, 13), (172, 198, 12), (112, 116, 11)):
            flower5(d, x, y, r, (255, 216, 50), (230, 120, 30))
    finish(apron((206, 30, 40), mai, strap=(250, 206, 60), ties=(250, 206, 60)), "ult-tet-2", ULT_GLOW, ULT)

    def ongsao(d):
        d.line([(128, 60), (128, 96)], rgba((250, 220, 120)), 3)
        d.star(128, 150, 52, rgba((255, 210, 60)))
        d.star(128, 150, 34, rgba((240, 70, 50)))
        d.circ(128, 150, 10, rgba((255, 240, 160)))
        d.line([(128, 196), (122, 228)], rgba((240, 70, 50)), 4)
        d.line([(128, 196), (134, 228)], rgba((255, 210, 60)), 4)
        for (x, y) in ((74, 92), (190, 196), (70, 210), (186, 110)):
            d.circ(x, y, 3, rgba((255, 250, 200)))
    finish(apron((40, 46, 104), ongsao, strap=(250, 206, 60), ties=(250, 206, 60)), "ult-trungthu-2", ULT_GLOW, ULT)

    finish(apron((220, 36, 40), lambda d: d.star(128, 160, 50, rgba((255, 222, 50))), strap=(255, 210, 60), ties=(255, 210, 60)),
           "ult-quockhanh-2", ULT_GLOW, ULT)

    def santa(d):
        d.rect(40, 200, 216, 240, rgba((250, 250, 250)))
        for x in range(44, 216, 14):
            d.circ(x, 200, 8, rgba((250, 250, 250)))
        d.rect(40, 130, 216, 150, rgba((40, 34, 34)))
        d.rect(110, 124, 146, 156, rgba((250, 210, 60)))
        d.rect(118, 132, 138, 148, rgba((40, 34, 34)))
        d.rect(118, 56, 138, 124, rgba((250, 250, 250), 0))
    finish(apron((214, 38, 44), santa, strap=(250, 250, 250), ties=(250, 250, 250)), "ult-noel-2", ULT_GLOW, ULT)


# ======================= MŨ =======================

def toque(a, color, top=40, band_col=None, band_deco=None, puff_deco=None):
    band_col = band_col or lit(color, 0.0)
    a.piece(lambda d, f: (d.circ(86, top + 66, 40, f), d.circ(128, top + 46, 48, f), d.circ(170, top + 66, 40, f),
                          d.rect(78, top + 66, 178, 186, f, 10)), color, puff_deco, light=0.5, shade=0.18)
    a.draw(lambda d: (d.line(curve((104, top + 90), (100, top + 120), (104, 176)), rgba(dk(color, 0.18)), 4),
                      d.line(curve((152, top + 90), (156, top + 120), (152, 176)), rgba(dk(color, 0.18)), 4)))
    a.piece(lambda d, f: d.rect(70, 168, 186, 210, f, 10), band_col, band_deco)


def draw_hats():
    # Mũ vải (mũ lưỡi trai)
    a = Art()
    a.piece(lambda d, f: d.ell(110, 160, 238, 200, f), (196, 100, 70))
    a.piece(lambda d, f: (d.ell(30, 70, 196, 230, f),), (226, 120, 84),
            deco=lambda d: (d.rect(0, 180, 256, 256, (0, 0, 0, 0)), d.line(curve((113, 72), (100, 120), (110, 176)), rgba((180, 90, 60)), 3),
                            d.circ(113, 74, 7, rgba((200, 100, 70)))))
    # cắt nửa dưới: vẽ lại vùng trong suốt
    m = Image.new("L", (N, N), 255)
    D(m).rect(0, 172, 256, 256, 0)
    D(m).ell(110, 160, 238, 200, 255)
    a.img.putalpha(ImageChops.multiply(a.img.split()[3], m))
    a.piece(lambda d, f: d.rect(30, 162, 196, 176, f, 6), (200, 100, 70))
    finish(a, "mu-vai")

    # Nón lá
    a = Art()
    def nonla_deco(d):
        for k in range(1, 6):
            y = 48 + k * 22
            w = (y - 48) / 120 * 108
            d.line([(128 - w, y), (128 + w, y)], rgba((196, 166, 100), 200), 2.5)
        d.poly([(128, 48), (60, 168), (90, 168)], rgba((255, 248, 220), 90))
    a.piece(lambda d, f: d.line(curve((50, 170), (128, 250), (206, 170)), f, 5), (214, 60, 70), shade=0, light=0)
    a.piece(lambda d, f: (d.poly([(14, 168), (128, 44), (242, 168)], f), d.ell(14, 154, 242, 186, f)), (238, 214, 150), nonla_deco)
    finish(a, "non-la")

    a = Art()
    toque(a, (250, 250, 248), top=34, band_col=(238, 238, 234))
    finish(a, "mu-dau-bep-trang")

    # Khăn xếp ngự thiện
    a = Art()
    gold = (240, 190, 60)
    a.piece(lambda d, f: (d.ell(46, 70, 210, 190, f), d.rect(46, 130, 210, 196, f, 20)), gold,
            deco=lambda d: ([d.line([(40 + k * 22, 200), (90 + k * 22, 70)], rgba((200, 140, 30), 230), 5) for k in range(-2, 8)],
                            [d.line([(216 - k * 22, 200), (166 - k * 22, 70)], rgba((255, 222, 120), 200), 4) for k in range(-2, 8)]),
            light=0.4)
    a.piece(lambda d, f: d.rect(42, 180, 214, 204, f, 10), (200, 40, 40))
    a.draw(lambda d: d.circ(128, 120, 12, rgba((210, 40, 50))))
    a.draw(lambda d: d.circ(128, 120, 6, rgba((255, 200, 210))))
    finish(a, "khan-xep-ngu-thien", None, LEG)

    a = Art()
    toque(a, (252, 252, 250), top=14, band_col=(232, 180, 60),
          band_deco=lambda d: d.star(128, 189, 15, rgba((255, 246, 200))))
    finish(a, "mu-tong-bep-truong", None, LEG)

    # Nón quai thao
    a = Art()
    for s in (-1, 1):
        x = 128 + s * 96
        a.piece(lambda d, f, x=x: [d.line([(x + k * 4, 142), (x + k * 5, 206)], f, 3.5) for k in range(-3, 4)], (40, 34, 34), shade=0, light=0)
    a.piece(lambda d, f: d.ell(12, 106, 244, 166, f), (226, 196, 120),
            deco=lambda d: ([d.ell(12 + k * 14, 106 + k * 3.8, 244 - k * 14, 166 - k * 3.8, (0, 0, 0, 0)) for k in range(1)],
                            [d.arc(12 + k * 18, 106 + k * 4.6, 244 - k * 18, 166 - k * 4.6, 0, 360, rgba((190, 150, 80), 200), 2.5) for k in range(1, 6)]))
    a.piece(lambda d, f: (d.rect(92, 98, 164, 134, f, 6), d.ell(92, 86, 164, 110, f)), (214, 180, 104))
    a.piece(lambda d, f: d.rect(90, 120, 166, 134, f, 4), (40, 34, 34))
    finish(a, "non-quai-thao", None, LEG)

    # Mũ Phượng Hoàng
    a = Art()
    for ang, col in ((-50, (240, 90, 40)), (-25, (250, 150, 40)), (0, (250, 200, 60)), (25, (250, 150, 40)), (50, (240, 90, 40))):
        r = math.radians(ang - 90)
        tip = (128 + 104 * math.cos(r), 170 + 120 * math.sin(r))
        base = (128 + 14 * math.cos(r), 170 + 14 * math.sin(r))
        nx, ny = -math.sin(r), math.cos(r)
        mid = ((tip[0] + base[0]) / 2, (tip[1] + base[1]) / 2)
        pts = curve(base, (mid[0] + nx * 30, mid[1] + ny * 30), tip) + curve(tip, (mid[0] - nx * 30, mid[1] - ny * 30), base)
        a.piece(lambda d, f, pts=pts, mid=mid: d.poly(pts, f), col,
                deco=lambda d, tip=tip, base=base: d.line([base, tip], rgba((200, 40, 30), 200), 3), light=0.4)
    a.piece(lambda d, f: d.rect(60, 150, 196, 206, f, 12), (246, 200, 70),
            deco=lambda d: [d.poly([(x, 150), (x + 10, 162), (x, 174), (x - 10, 162)], rgba((210, 150, 40))) for x in range(72, 196, 24)])
    a.piece(lambda d, f: d.circ(128, 178, 15, f), (220, 30, 50), light=0.6)
    finish(a, "mu-phuong-hoang", None, LEG)

    # Mũ Thần Tài (mũ cánh chuồn)
    a = Art()
    for s in (-1, 1):
        cx = 128 + s * 82
        a.piece(lambda d, f, cx=cx: d.ell(cx - 44, 92, cx + 44, 124, f),
                (30, 26, 30), deco=lambda d, cx=cx: d.ell(cx - 34, 98, cx + 34, 118, rgba((214, 40, 40))))
    a.piece(lambda d, f: (d.ell(64, 50, 192, 170, f), d.rect(64, 110, 192, 200, f, 14)), (214, 34, 40), light=0.4)
    a.piece(lambda d, f: d.rect(58, 170, 198, 206, f, 10), (30, 26, 30),
            deco=lambda d: d.rect(58, 182, 198, 192, rgba((246, 200, 70))))
    a.draw(lambda d: coin(d, 128, 120, 22))
    finish(a, "ult-tet-3", ULT_GLOW, ULT)

    # Mũ Chú Cuội: mũ rơm vành rộng + dải lá đa + trăng khuyết
    a = Art()
    straw = (236, 200, 110)
    a.piece(lambda d, f: d.ell(10, 128, 246, 196, f), straw,
            deco=lambda d: [d.arc(10 + k * 16, 128 + k * 4.6, 246 - k * 16, 196 - k * 4.6, 0, 360, rgba((200, 160, 70), 200), 2.5) for k in range(1, 7)])
    a.piece(lambda d, f: (d.rect(70, 80, 186, 160, f, 10), d.ell(70, 56, 186, 108, f)), (242, 210, 120),
            deco=lambda d: [d.line([(70, y), (186, y)], rgba((210, 170, 80), 200), 2.5) for y in range(80, 160, 12)])
    a.piece(lambda d, f: d.rect(66, 130, 190, 154, f, 6), (70, 150, 80))
    for (x, y, ang) in ((88, 140, -30), (112, 136, 10), (150, 138, -20)):
        a.piece(lambda d, f, x=x, y=y: d.poly(curve((x, y + 12), (x - 14, y - 6), (x + 6, y - 22)) + curve((x + 6, y - 22), (x + 18, y - 2), (x, y + 12)), f),
                (90, 180, 90))
    m = Art()
    m.piece(lambda d, f: d.circ(170, 102, 20, f), (255, 230, 120), light=0.5)
    cut = Image.new("L", (N, N), 0)
    D(cut).circ(180, 94, 17, 255)
    m.img.putalpha(ImageChops.subtract(m.img.split()[3], cut))
    a.img.alpha_composite(m.img)
    finish(a, "ult-trungthu-3", ULT_GLOW, ULT)

    # Mũ Hòa Bình (mũ cối + sao vàng + cành ô liu)
    a = Art()
    green = (110, 140, 80)
    a.piece(lambda d, f: d.ell(16, 144, 240, 200, f), dk(green, 0.1))
    a.piece(lambda d, f: (d.ell(48, 54, 208, 190, f),), green,
            deco=lambda d: d.rect(0, 160, 256, 256, (0, 0, 0, 0)), light=0.4)
    m = Image.new("L", (N, N), 255)
    D(m).rect(0, 172, 256, 256, 0)
    D(m).ell(16, 144, 240, 200, 255)
    a.img.putalpha(ImageChops.multiply(a.img.split()[3], m))
    a.piece(lambda d, f: d.circ(128, 120, 24, f), (214, 40, 40), deco=lambda d: d.star(128, 121, 18, rgba((255, 222, 50))))
    def olive(d):
        d.line(curve((70, 164), (100, 150), (110, 140)), rgba((90, 120, 50)), 3)
        for (x, y) in ((80, 156), (92, 150), (102, 144)):
            d.ell(x - 8, y - 4, x + 4, y + 4, rgba((150, 190, 90)))
    a.draw(olive)
    a.draw(lambda d: (d.ell(150, 140, 186, 160, rgba((255, 255, 255))), d.poly([(164, 146), (196, 126), (176, 152)], rgba((240, 240, 240))),
                      d.circ(184, 146, 2, rgba((40, 40, 40)))))
    finish(a, "ult-quockhanh-3", ULT_GLOW, ULT)

    # Mũ Giáng Sinh
    a = Art()
    a.piece(lambda d, f: d.poly([(54, 168)] + curve((54, 168), (90, 30), (180, 42)) + curve((180, 42), (220, 60), (222, 104))
                                + curve((222, 104), (196, 80), (178, 76)) + curve((178, 76), (196, 120), (202, 168)), f), (214, 38, 44), light=0.4)
    a.piece(lambda d, f: d.circ(222, 108, 20, f), (250, 250, 250), light=0.2)
    a.piece(lambda d, f: d.rect(38, 158, 218, 204, f, 22), (250, 250, 250), light=0.2,
            deco=lambda d: [d.circ(x, 181, 9, rgba((232, 232, 236))) for x in range(54, 210, 22)])
    finish(a, "ult-noel-3", ULT_GLOW, ULT)


if __name__ == "__main__":
    draw_knives()
    draw_pots()
    draw_aprons()
    draw_hats()
    print("done", len(os.listdir(OUT)))
