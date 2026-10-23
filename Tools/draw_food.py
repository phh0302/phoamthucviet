# Vẽ hình món ăn (256x256, nền trong suốt) cùng phong cách icon trang bị.
# Tên file = tên món bỏ dấu nối gạch ngang (giống SpriteLibrary.Slug).
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import draw_gear as G
from draw_gear import Art, D, rgba, lit, dk, curve, finish
from PIL import Image, ImageChops

OUT = sys.argv[1] if len(sys.argv) > 1 else "food_out"
os.makedirs(OUT, exist_ok=True)
G.OUT = OUT


def H(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def rot(pts, cx, cy, ang):
    a = math.radians(ang)
    c, s = math.cos(a), math.sin(a)
    return [(cx + x * c - y * s, cy + x * s + y * c) for x, y in pts]


def ellpts(cx, cy, rx, ry, n=32, a0=0, a1=360):
    return [(cx + rx * math.cos(math.radians(a0 + (a1 - a0) * i / n)), cy + ry * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


# ---------------- nguyên liệu nhỏ (vẽ bằng D) ----------------

def leaf(d, cx, cy, ln, ang, col=H("4caf50"), vein=True):
    pts = curve((-ln / 2, 0), (0, -ln * 0.38), (ln / 2, 0), 8) + curve((ln / 2, 0), (0, ln * 0.38), (-ln / 2, 0), 8)
    d.poly(rot(pts, cx, cy, ang), rgba(col))
    if vein:
        d.line(rot([(-ln * 0.4, 0), (ln * 0.4, 0)], cx, cy, ang), rgba(lit(col, 0.35)), 1.5)


def herbs(d, cx, cy, n, spread, seed, cols=(H("43a047"), H("66bb6a"), H("2e7d32"))):
    r = random.Random(seed)
    for i in range(n):
        leaf(d, cx + r.uniform(-spread, spread), cy + r.uniform(-spread * 0.4, spread * 0.4), r.uniform(14, 22), r.uniform(0, 180), cols[i % len(cols)])


def scallion(d, cx, cy, n, spread, seed):
    r = random.Random(seed)
    for i in range(n):
        x, y = cx + r.uniform(-spread, spread), cy + r.uniform(-spread * 0.4, spread * 0.4)
        d.circ(x, y, 4.5, rgba(H("7cb342")))
        d.circ(x, y, 2.2, rgba(H("dcedc8")))


def chili(d, cx, cy, n, spread, seed):
    r = random.Random(seed)
    for i in range(n):
        x, y = cx + r.uniform(-spread, spread), cy + r.uniform(-spread * 0.4, spread * 0.4)
        d.circ(x, y, 4, rgba(H("e53935")))
        d.circ(x, y, 1.8, rgba(H("ffcc80")))


def lime(d, cx, cy, r=14, ang=0):
    pts = [(0, 0)] + ellpts(0, 0, r, r * 0.8, 12, 180, 360)
    d.poly(rot(pts, cx, cy, ang), rgba(H("7cb342")))
    pts = [(0, -1)] + ellpts(0, -1, r * 0.78, r * 0.6, 12, 180, 360)
    d.poly(rot(pts, cx, cy, ang), rgba(H("dce775")))


def noodles(d, cx, cy, w, h, col=H("fbf6e9"), n=10, seed=1, thick=3.2, amp=4):
    r = random.Random(seed)
    for i in range(n):
        y0 = cy - h / 2 + h * i / max(1, n - 1)
        ph = r.uniform(0, 6)
        pts = [(cx - w / 2 + w * t / 20, y0 + amp * math.sin(ph + t * 0.9)) for t in range(21)]
        d.line(pts, rgba(dk(col, 0.12)), thick + 1.5)
        d.line(pts, rgba(col), thick)


def beef_slices(d, cx, cy, n, spread, seed, col=H("8d5a4b")):
    r = random.Random(seed)
    for i in range(n):
        x, y = cx + r.uniform(-spread, spread), cy + r.uniform(-spread * 0.35, spread * 0.35)
        pts = ellpts(0, 0, 17, 9, 14)
        d.poly(rot(pts, x, y, r.uniform(-30, 30)), rgba(col))
        d.poly(rot(ellpts(-3, -2, 9, 4, 10), x, y, r.uniform(-30, 30)), rgba(lit(col, 0.18)))


def shrimp(d, cx, cy, s=1.0, ang=0, col=H("ff8a65")):
    pts = []
    for i in range(13):
        a = math.radians(200 + i * 13)
        pts.append((math.cos(a) * 16 * s, math.sin(a) * 16 * s))
    inner = []
    for i in range(13):
        a = math.radians(200 + (12 - i) * 13)
        inner.append((math.cos(a) * 7 * s, math.sin(a) * 7 * s))
    d.poly(rot(pts + inner, cx, cy, ang), rgba(col))
    for i in range(3):
        a = math.radians(240 + i * 40)
        p = rot([(math.cos(a) * 7 * s, math.sin(a) * 7 * s), (math.cos(a) * 16 * s, math.sin(a) * 16 * s)], cx, cy, ang)
        d.line(p, rgba(dk(col, 0.2)), 1.6)
    tail = rot([(math.cos(math.radians(356)) * 12 * s, math.sin(math.radians(356)) * 12 * s), (20 * s, 4 * s), (18 * s, -8 * s)], cx, cy, ang)
    d.poly(tail, rgba(H("ff7043")))


def egg_half(d, cx, cy, r=14):
    d.ell(cx - r, cy - r * 0.8, cx + r, cy + r * 0.8, rgba(H("fffdf5")))
    d.circ(cx, cy, r * 0.5, rgba(H("ffb300")))


def rice_grains(d, cx, cy, w, h, seed, col=H("ffffff")):
    r = random.Random(seed)
    for i in range(int(w * h / 60)):
        x, y = cx + r.uniform(-w / 2, w / 2), cy + r.uniform(-h / 2, h / 2)
        if ((x - cx) / (w / 2)) ** 2 + ((y - cy) / (h / 2)) ** 2 > 1:
            continue
        d.ell(x - 3, y - 1.5, x + 3, y + 1.5, rgba(dk(col, 0.08)))


def grill_marks(d, cx, cy, w, n=3, ang=-25):
    for i in range(n):
        off = (i - (n - 1) / 2) * 10
        d.line(rot([(-w / 2, off), (w / 2, off)], cx, cy, ang), rgba(H("4e342e"), 200), 3)


def snail(d, cx, cy, r=13, col=H("bcaaa4")):
    d.circ(cx, cy, r, rgba(dk(col, 0.25)))
    d.circ(cx, cy, r - 2.5, rgba(col))
    pts = [(cx + (r - 3) * (1 - t / 3.2) * math.cos(t * 2.2), cy + (r - 3) * (1 - t / 3.2) * math.sin(t * 2.2)) for t in [i / 8 for i in range(0, 26)]]
    d.line(pts, rgba(dk(col, 0.35)), 1.8)


def ice(d, cx, cy, s=16, ang=0):
    pts = rot([(-s / 2, -s / 2), (s / 2, -s / 2), (s / 2, s / 2), (-s / 2, s / 2)], cx, cy, ang)
    d.poly(pts, (240, 250, 255, 120))
    d.line([pts[0], pts[1]], (255, 255, 255, 170), 2)


def dots(d, cx, cy, w, h, n, col, r=2.2, seed=3):
    rr = random.Random(seed)
    for i in range(n):
        x, y = cx + rr.uniform(-w / 2, w / 2), cy + rr.uniform(-h / 2, h / 2)
        d.circ(x, y, r, rgba(col))


# ---------------- vật đựng (Art) ----------------
BOWL_RIM = (128, 112, 104, 30)   # cx, cy, rx, ry của miệng tô


def bowl(a, broth, contents=None, body=H("f5f1e8"), band=H("3f6fb5"), deep=False):
    cx, cy, rx, ry = BOWL_RIM
    bot = 218 if deep else 212
    a.piece(lambda d, f: d.ell(92, bot - 14, 164, bot + 6, f), dk(body, 0.08))
    body_pts = [(cx - rx, cy)] + curve((cx - rx, cy), (cx - rx + 6, bot - 4), (cx, bot)) + curve((cx, bot), (cx + rx - 6, bot - 4), (cx + rx, cy))
    a.piece(lambda d, f: d.poly(body_pts, f), body,
            deco=(lambda d: (d.line([(cx - rx + 8, cy + 30)] + curve((cx - rx + 8, cy + 30), (cx, cy + 62), (cx + rx - 8, cy + 30))[1:], rgba(band), 4),
                             d.line([(cx - rx + 18, cy + 46)] + curve((cx - rx + 18, cy + 46), (cx, cy + 74), (cx + rx - 18, cy + 46))[1:], rgba(band), 2.5)))
            if band else None, light=0.4)
    a.piece(lambda d, f: d.ell(cx - rx, cy - ry, cx + rx, cy + ry, f), lit(body, 0.3))
    a.piece(lambda d, f: d.ell(cx - rx + 9, cy - ry + 6, cx + rx - 9, cy + ry - 4, f), broth, deco=contents, shade=0.15, light=0.0)


def plate(a, contents=None, col=H("fbf8f2"), rim_col=None, cy=160, rx=114, ry=50, edge=None):
    rim_col = rim_col or dk(col, 0.06)
    a.piece(lambda d, f: d.ell(128 - rx, cy - ry + 8, 128 + rx, cy + ry + 8, f), dk(col, 0.18), shade=0, light=0)
    a.piece(lambda d, f: d.ell(128 - rx, cy - ry, 128 + rx, cy + ry, f), col,
            deco=lambda d: (d.ell(128 - rx * 0.78, cy - ry * 0.72, 128 + rx * 0.78, cy + ry * 0.72, rgba(dk(col, 0.04))),
                            d.arc(128 - rx + 5, cy - ry + 4, 128 + rx - 5, cy + ry - 4, 0, 360, rgba(edge), 3) if edge else None))
    if contents:
        a.draw(contents)


def leaf_plate(a, contents=None, cy=162):
    pts = curve((14, cy), (128, cy - 92), (242, cy)) + curve((242, cy), (128, cy + 92), (14, cy))
    a.piece(lambda d, f: d.poly(pts, f), H("5d9b3a"),
            deco=lambda d: (d.line([(20, cy), (236, cy)], rgba(H("8bc34a")), 3),
                            [d.line([(40 + i * 30, cy), (60 + i * 30, cy - 30)], rgba(H("7cb342")), 2) for i in range(6)],
                            [d.line([(40 + i * 30, cy), (60 + i * 30, cy + 30)], rgba(H("7cb342")), 2) for i in range(6)]))
    if contents:
        a.draw(contents)


def glass(a, liquid, top=56, bottom=224, wtop=60, wbot=44, fill=0.82, ice_n=0, straw=None, garnish=None, layers=None,
          foam=None, cx=128, extra=None):
    h = bottom - top
    lt = top + h * (1 - fill)
    def lerpw(y):
        t = (y - top) / h
        return wtop + (wbot - wtop) * t
    body = [(cx - wtop, top), (cx + wtop, top), (cx + wbot, bottom), (cx - wbot, bottom)]
    if straw:
        a.piece(lambda d, f: d.line([(cx + 18, top + 40), (cx + 44, top - 34)], f, 9), straw, shade=0, light=0.2)
    a.piece(lambda d, f: d.poly(body, f), (226, 242, 250), light=0.0, shade=0.1)
    def liq(d):
        if layers:
            ys = [lt] + [lt + (bottom - lt) * t for t in [l[0] for l in layers[1:]]] + [bottom]
            for i, (frac, col) in enumerate(layers):
                y0, y1 = ys[i], ys[i + 1]
                d.poly([(cx - lerpw(y0) + 5, y0), (cx + lerpw(y0) - 5, y0), (cx + lerpw(y1) - 5, y1), (cx - lerpw(y1) + 5, y1)], rgba(col))
        else:
            d.poly([(cx - lerpw(lt) + 5, lt), (cx + lerpw(lt) - 5, lt), (cx + wbot - 5, bottom - 5), (cx - wbot + 5, bottom - 5)], rgba(liquid))
        for i in range(ice_n):
            ice(d, cx - 22 + (i % 3) * 22, lt + 14 + (i // 3) * 26 + (i % 2) * 6, 22, 12 * (i % 3 - 1))
        if foam:
            d.poly([(cx - lerpw(lt) + 5, lt - 2), (cx + lerpw(lt) - 5, lt - 2), (cx + lerpw(lt + 20) - 5, lt + 20), (cx - lerpw(lt + 20) + 5, lt + 20)], rgba(foam))
            for i in range(5):
                d.circ(cx - lerpw(lt) + 14 + i * (2 * lerpw(lt) - 28) / 4, lt - 4, 12, rgba(foam))
        if extra:
            extra(d, lt)
        # ánh sáng thành ly
        d.poly([(cx - wtop + 10, top + 8), (cx - wtop + 22, top + 8), (cx - wbot + 18, bottom - 10), (cx - wbot + 8, bottom - 10)], (255, 255, 255, 110))
    a.piece(lambda d, f: d.poly(body, f), (235, 248, 255), deco=liq, light=0.0, shade=0.08)
    a.draw(lambda d: d.ell(cx - wtop, top - 8, cx + wtop, top + 8, (255, 255, 255, 120)))
    if garnish:
        garnish(a)


def cup(a, liquid, col=H("f5f1e8"), band=H("3f6fb5"), saucer=True, topping=None):
    if saucer:
        a.piece(lambda d, f: d.ell(30, 170, 226, 222, f), lit(col, 0.1))
    a.piece(lambda d, f: d.arc(158, 112, 214, 176, 270, 90 + 360, f, 11), col, shade=0, light=0)
    body = [(52, 110)] + curve((52, 110), (56, 196), (128, 200)) + curve((128, 200), (200, 196), (204, 110))
    a.piece(lambda d, f: d.poly(body, f), col, deco=(lambda d: d.line(curve((60, 150), (128, 176), (196, 150)), rgba(band), 4)) if band else None, light=0.4)
    a.piece(lambda d, f: d.ell(50, 94, 206, 128, f), lit(col, 0.3))
    a.piece(lambda d, f: d.ell(60, 99, 196, 124, f), liquid, deco=topping, shade=0.1, light=0)


def mug(a, liquid, foam=None, col=(226, 242, 250), topping=None):
    a.piece(lambda d, f: d.arc(150, 92, 226, 186, 270, 90 + 360, f, 13), col, shade=0, light=0.2)
    glass(a, liquid, top=60, bottom=222, wtop=58, wbot=54, fill=0.86, foam=foam, cx=112, extra=topping)


def clay_pot(a, contents, col=H("8d5a3b"), broth=H("6d4c41")):
    a.piece(lambda d, f: (d.rect(18, 112, 46, 130, f, 8), d.rect(210, 112, 238, 130, f, 8)), dk(col, 0.1))
    body = [(36, 112)] + curve((36, 112), (34, 214), (128, 216)) + curve((128, 216), (222, 214), (220, 112))
    a.piece(lambda d, f: d.poly(body, f), col, light=0.35)
    a.piece(lambda d, f: d.ell(30, 92, 226, 132, f), lit(col, 0.15))
    a.piece(lambda d, f: d.ell(40, 98, 216, 128, f), broth, deco=contents, shade=0.12, light=0)


def hotpot(a, contents, broth, col=H("b0bec5")):
    a.piece(lambda d, f: d.poly([(60, 196), (196, 196), (180, 228), (76, 228)], f), H("455a64"))
    a.draw(lambda d: (d.ell(96, 204, 160, 222, rgba(H("ff7043"))), d.ell(110, 208, 146, 220, rgba(H("ffd54f")))))
    clay_pot(a, contents, col=col, broth=broth)


def pan(a, contents, col=H("3a3a40")):
    a.piece(lambda d, f: d.poly([(196, 140), (248, 112), (254, 126), (204, 156)], f), H("5d4037"))
    a.piece(lambda d, f: d.ell(14, 92, 214, 214, f), col)
    a.piece(lambda d, f: d.ell(26, 100, 202, 200, f), dk(col, 0.35), deco=contents, shade=0.1, light=0)


def skewer_stick(d, x0, y0, x1, y1):
    d.line([(x0, y0), (x1, y1)], rgba(H("d7b98e")), 4)


def steam(a, xs=(100, 128, 156), y=70):
    for x in xs:
        a.draw(lambda d, x=x: d.line(curve((x, y), (x - 12, y - 22), (x + 4, y - 44)), (255, 255, 255, 150), 6))


# ======================== CÁC MÓN ========================
R = {}


def dish(name):
    def wrap(fn):
        R[name] = fn
        return fn
    return wrap


def pho(a, broth, meat, seed, extra=None, noodle=H("fbf6e9")):
    def c(d):
        noodles(d, 128, 112, 150, 30, noodle, 6, seed)
        meat(d)
        herbs(d, 92, 104, 5, 22, seed + 1)
        scallion(d, 150, 118, 8, 30, seed + 2)
        chili(d, 172, 104, 3, 10, seed + 3)
        if extra:
            extra(d)
    bowl(a, broth, c)
    a.draw(lambda d: lime(d, 206, 100, 15, -20))
    steam(a, y=72)


@dish("pho-bo")
def _(a):
    pho(a, H("e8c38a"), lambda d: beef_slices(d, 132, 108, 5, 34, 7), 1)


@dish("pho-bo-tai-lan")
def _(a):
    def m(d):
        beef_slices(d, 128, 108, 7, 40, 9, H("6d4030"))
        dots(d, 128, 108, 70, 20, 14, H("fff3c4"), 2.6, 4)
    pho(a, H("d9a86a"), m, 2)


@dish("pho-ga")
def _(a):
    def m(d):
        r = random.Random(5)
        for i in range(7):
            x, y = 128 + r.uniform(-40, 40), 108 + r.uniform(-12, 12)
            d.poly(rot(ellpts(0, 0, 16, 7, 10), x, y, r.uniform(-40, 40)), rgba(H("fff1d6")))
            d.poly(rot(ellpts(0, -3, 14, 3, 8), x, y, r.uniform(-40, 40)), rgba(H("f4c27a")))
        leaf(d, 80, 112, 20, 30, H("9ccc65"))
    pho(a, H("f3dfae"), m, 3)


@dish("bun-bo-hue")
def _(a):
    def m(d):
        beef_slices(d, 120, 108, 4, 30, 11)
        d.circ(160, 106, 15, rgba(H("f8bbd0")))
        d.circ(160, 106, 8, rgba(H("fff8e1")))
        d.ell(80, 100, 104, 114, rgba(H("6d4c41")))  # tiết
        dots(d, 128, 104, 120, 24, 16, H("e65100"), 3, 6)
    pho(a, H("e2702e"), m, 4, noodle=H("fffaf0"))


@dish("bun-bo-gio-heo")
def _(a):
    def m(d):
        d.circ(118, 106, 22, rgba(H("f3c6a5")))
        d.circ(118, 106, 12, rgba(H("fff3e0")))
        d.circ(118, 106, 5, rgba(H("e0c3a0")))
        beef_slices(d, 166, 110, 2, 12, 3)
        dots(d, 128, 104, 120, 24, 16, H("e65100"), 3, 7)
    pho(a, H("dd6a2a"), m, 5)


@dish("bun-cha")
def _(a):
    def c(d):
        for (x, y) in ((96, 110), (130, 104), (160, 114)):
            d.ell(x - 18, y - 10, x + 18, y + 10, rgba(H("8d4f2a")))
            grill_marks(d, x, y, 26, 2)
        for (x, y) in ((110, 122), (146, 96)):
            d.poly(rot(ellpts(0, 0, 14, 6), x, y, 15), rgba(H("ffcc80")))  # đu đủ
        chili(d, 176, 104, 3, 8, 2)
    bowl(a, H("d9a35f"), c, body=H("f0ebe0"), band=None)
    a.draw(lambda d: herbs(d, 56, 196, 6, 18, 9))


@dish("nem-ran")
def _(a):
    def c(d):
        for i, (x, y, ang) in enumerate(((92, 150, -12), (130, 140, -12), (168, 152, -12), (110, 172, -12), (150, 170, -12))):
            pts = rot([(-28, -11), (28, -11), (28, 11), (-28, 11)], x, y, ang)
            d.poly(pts, rgba(H("d9822b")))
            d.ell(x - 28, y - 11, x + 28, y + 11, rgba(H("e89a3c")))
            dots(d, x, y, 46, 14, 10, H("f6c26b"), 2, i)
        herbs(d, 60, 160, 4, 10, 2)
        d.circ(206, 150, 18, rgba(H("ffcc80")))
        d.circ(206, 150, 14, rgba(H("ffab40")))
    plate(a, c)


@dish("cha-ca-la-vong")
def _(a):
    def c(d):
        for (x, y) in ((86, 140), (122, 128), (156, 146), (110, 162), (148, 170)):
            d.poly(rot(ellpts(0, 0, 22, 12), x, y, 20), rgba(H("f4a62a")))
            d.poly(rot(ellpts(-4, -3, 12, 5), x, y, 20), rgba(H("ffd54f")))
        herbs(d, 120, 150, 10, 50, 4, (H("9ccc65"), H("7cb342")))
        scallion(d, 100, 150, 6, 40, 6)
    pan(a, c)
    steam(a, (80, 120, 160), 86)


@dish("tra-da")
def _(a):
    glass(a, H("e9d58a"), ice_n=5, fill=0.8)


@dish("tra-chanh")
def _(a):
    glass(a, H("e8a33a"), top=40, ice_n=6, fill=0.85, straw=H("4caf50"),
          garnish=lambda a: a.draw(lambda d: (d.circ(178, 52, 22, rgba(H("7cb342"))), d.circ(178, 52, 17, rgba(H("e6ee9c"))),
                                              [d.line([(178, 52), (178 + 15 * math.cos(math.radians(k * 60)), 52 + 15 * math.sin(math.radians(k * 60)))], rgba(H("c5e1a5")), 2) for k in range(6)])))


@dish("tra-sen")
def _(a):
    cup(a, H("d4b36a"), band=H("2e7d32"), topping=lambda d: [leaf(d, 128 + 22 * math.cos(math.radians(k * 72)), 112 + 9 * math.sin(math.radians(k * 72)), 26, k * 72, H("f8bbd0"), False) for k in range(5)] and d.circ(128, 112, 7, rgba(H("ffeb3b"))))


@dish("quay-nong")
def _(a):
    def c(d):
        for i, (x, y, ang) in enumerate(((100, 150, -30), (150, 150, -30), (126, 170, 20))):
            for off in (-9, 9):
                pts = rot([(-50, -9), (50, -9), (50, 9), (-50, 9)], x + off * 0.6, y + off, ang)
                d.poly(pts, rgba(H("d08a35")))
                d.poly(rot(ellpts(0, 0, 50, 9), x + off * 0.6, y + off, ang), rgba(H("e5a54a")))
                d.line(rot([(-42, -3), (42, -3)], x + off * 0.6, y + off, ang), rgba(H("f6cf7c")), 2.5)
    plate(a, c)


@dish("muc-nuong")
def _(a):
    def c(d):
        for i, x in enumerate((90, 128, 166)):
            skewer_stick(d, x - 10, 214, x + 12, 70)
            d.poly([(x - 16, 110)] + curve((x - 16, 110), (x, 70), (x + 18, 110)) + [(x + 14, 176), (x - 12, 176)], rgba(H("c97a44")))
            for k in range(5):
                d.line([(x - 10 + k * 6, 176), (x - 14 + k * 7, 200)], rgba(H("b0612d")), 4)
            d.line([(x - 10, 130), (x + 12, 126)], rgba(H("7a3b17")), 2.5)
            d.line([(x - 10, 150), (x + 12, 146)], rgba(H("7a3b17")), 2.5)
    plate(a, c, cy=176, ry=44)


@dish("bia-hoi")
def _(a):
    mug(a, H("f2b437"), foam=H("fffdf3"))


@dish("oc-luoc")
def _(a):
    def c(d):
        r = random.Random(3)
        for i in range(9):
            snail(d, 80 + (i % 5) * 24 + r.uniform(-4, 4), 102 + (i // 5) * 16 + r.uniform(-3, 3), 13, H("a1887f"))
        d.line([(70, 96), (190, 120)], rgba(H("c5e1a5")), 4)  # sả
    bowl(a, H("cfc2a8"), c, body=H("e8e2d6"), band=None)
    a.draw(lambda d: (d.circ(212, 196, 20, rgba(H("ffe0b2"))), d.circ(212, 196, 15, rgba(H("ff8a65")))))


@dish("cha-muc")
def _(a):
    def c(d):
        for (x, y) in ((88, 150), (132, 140), (174, 152), (110, 172), (154, 174)):
            d.ell(x - 26, y - 15, x + 26, y + 15, rgba(H("c98a3a")))
            d.ell(x - 22, y - 13, x + 18, y + 9, rgba(H("e7b057")))
            dots(d, x, y, 34, 16, 5, H("f8d488"), 2.2, x)
        herbs(d, 210, 140, 3, 8, 5)
    plate(a, c)


@dish("sa-sung")
def _(a):
    def c(d):
        r = random.Random(7)
        for i in range(14):
            x, y = 128 + r.uniform(-70, 70), 156 + r.uniform(-24, 22)
            d.poly(rot(ellpts(0, 0, 14, 5), x, y, r.uniform(0, 180)), rgba(H("b3794c")))
        dots(d, 128, 156, 140, 40, 18, H("fff3c4"), 3, 2)
        scallion(d, 128, 156, 8, 60, 4)
        chili(d, 128, 156, 5, 60, 5)
    plate(a, c)


@dish("ngan-hap")
def _(a):
    def c(d):
        r = random.Random(2)
        for i in range(8):
            x, y = 70 + (i % 4) * 38 + r.uniform(-4, 4), 146 + (i // 4) * 26
            d.ell(x - 18, y - 12, x + 18, y + 12, rgba(H("8d7a66")))
            d.ell(x - 14, y - 9, x + 14, y + 7, rgba(H("efe2c6")))
            d.ell(x - 7, y - 5, x + 7, y + 3, rgba(H("e0a96d")))
        d.line([(60, 130), (200, 186)], rgba(H("c5e1a5")), 4)
        herbs(d, 196, 140, 3, 8, 3)
    plate(a, c)


@dish("tom-hap")
def _(a):
    def c(d):
        for i, (x, y, ang) in enumerate(((78, 146, 0), (128, 136, 20), (176, 148, -10), (100, 176, 40), (152, 178, -30))):
            shrimp(d, x, y, 1.6, ang)
        lime(d, 204, 168, 14, 20)
    plate(a, c)


@dish("tom-hum")
def _(a):
    def c(d):
        body = H("e8542d")
        for k in range(5):
            d.ell(108 + k * 18 - 16, 140 - 14 + k * 3, 108 + k * 18 + 16, 140 + 14 + k * 3, rgba(dk(body, 0.08 * (k % 2))))
        d.poly([(196, 154), (226, 140), (230, 172)], rgba(body))
        d.ell(70, 124, 112, 160, rgba(body))
        for s in (-1, 1):
            d.line([(78, 142), (56, 142 + 28 * s), (36, 142 + 26 * s)], rgba(body), 8)
            d.ell(14, 142 + 26 * s - 12, 46, 142 + 26 * s + 12, rgba(dk(body, 0.08)))
            d.line([(76, 136), (30, 110 + s * 8)], rgba(H("ff8a65")), 2)
        d.ell(108, 136, 196, 152, rgba(H("fff3e0")))  # thịt nướng
        dots(d, 150, 144, 80, 10, 10, H("ffcc80"), 3, 2)
        scallion(d, 150, 144, 6, 40, 3)
    plate(a, c, cy=158)


def crab(d, cx, cy, col=H("e5502e"), s=1.0, spots=False):
    for side in (-1, 1):
        for k in range(3):
            d.line([(cx + side * 40 * s, cy + (k - 1) * 12 * s), (cx + side * 66 * s, cy + (k - 1) * 18 * s + 10 * s), (cx + side * 74 * s, cy + (k - 1) * 20 * s + 22 * s)], rgba(dk(col, 0.1)), 6 * s)
        d.line([(cx + side * 34 * s, cy - 16 * s), (cx + side * 54 * s, cy - 42 * s)], rgba(col), 9 * s)
        d.ell(cx + side * 54 * s - 16 * s, cy - 58 * s, cx + side * 54 * s + 16 * s, cy - 34 * s, rgba(col))
        d.poly([(cx + side * 50 * s, cy - 56 * s), (cx + side * 62 * s, cy - 66 * s), (cx + side * 60 * s, cy - 52 * s)], rgba(H("3e2723")))
    d.ell(cx - 46 * s, cy - 30 * s, cx + 46 * s, cy + 30 * s, rgba(col))
    d.ell(cx - 38 * s, cy - 26 * s, cx + 30 * s, cy + 10 * s, rgba(lit(col, 0.15)))
    if spots:
        dots(d, cx, cy, 70 * s, 34 * s, 10, H("fff3e0"), 3.5 * s, 9)
    for side in (-1, 1):
        d.circ(cx + side * 10 * s, cy - 26 * s, 4.5 * s, rgba(H("212121")))


@dish("cua-be")
def _(a):
    plate(a, lambda d: (crab(d, 128, 160, H("e5502e")), herbs(d, 210, 186, 3, 8, 2)))


@dish("cua-hap")
def _(a):
    plate(a, lambda d: (crab(d, 120, 158, H("d84315"), 1.05), lime(d, 208, 186, 14, -10), d.line([(40, 190), (90, 200)], rgba(H("c5e1a5")), 4)))


@dish("ghe-hap")
def _(a):
    plate(a, lambda d: (crab(d, 128, 160, H("ff7043"), 0.95, spots=True), lime(d, 212, 184, 13, 10)))


# ---- Huế ----
@dish("nuoc-sam")
def _(a):
    glass(a, H("8d7a45"), ice_n=4, fill=0.82, garnish=lambda a: a.draw(lambda d: leaf(d, 166, 50, 34, -30, H("66bb6a"))))


@dish("banh-beo")
def _(a):
    def c(d):
        for i in range(7):
            x = 66 + (i % 4) * 42 + (21 if i >= 4 else 0)
            y = 136 + (i // 4) * 36
            d.ell(x - 22, y - 13, x + 22, y + 13, rgba(H("3f6fb5")))
            d.ell(x - 18, y - 11, x + 18, y + 9, rgba(H("fbf7ee")))
            d.ell(x - 11, y - 7, x + 11, y + 5, rgba(H("ffffff")))
            dots(d, x, y - 1, 16, 8, 6, H("ff7043"), 2.6, i)
            d.circ(x + 6, y - 4, 3, rgba(H("ffcc80")))
    plate(a, c, col=H("efe6d2"))


@dish("che-hue")
def _(a):
    glass(a, None, top=48, fill=0.9, layers=[(0, H("f8f3e6")), (0.25, H("7cb342")), (0.45, H("f3d36b")), (0.7, H("8d4f2a"))], ice_n=2,
          straw=H("e57373"))


@dish("com-hen")
def _(a):
    def c(d):
        rice_grains(d, 128, 112, 170, 40, 3)
        r = random.Random(8)
        for i in range(16):
            x, y = 128 + r.uniform(-70, 70), 112 + r.uniform(-16, 16)
            d.ell(x - 5, y - 3, x + 5, y + 3, rgba(H("b98a52")))
        herbs(d, 100, 106, 5, 30, 4)
        dots(d, 128, 112, 140, 30, 18, H("e0a24a"), 3, 7)  # da heo chiên, đậu phộng
        chili(d, 166, 114, 3, 10, 2)
    bowl(a, H("f5efe0"), c)


@dish("banh-khoai")
def _(a):
    def c(d):
        d.ell(36, 116, 220, 196, rgba(H("e6a22d")))
        d.ell(46, 120, 206, 184, rgba(H("f2c04e")))
        for (x, ang) in ((90, 20), (140, -20)):
            shrimp(d, x, 150, 1.0, ang)
        d.ell(160, 140, 196, 164, rgba(H("f4a7a7")))  # thịt
        herbs(d, 110, 168, 5, 40, 3, (H("c5e1a5"), H("fffde7")))  # giá
    plate(a, c)


@dish("tra-cung-dinh")
def _(a):
    cup(a, H("c98a2e"), col=H("fff7e3"), band=H("c79a2e"),
        topping=lambda d: (leaf(d, 110, 110, 26, 20, H("ffd54f"), False), leaf(d, 146, 112, 22, -30, H("ffca28"), False), d.circ(128, 112, 5, rgba(H("ff8f00")))))


def skewers(d, cx, cy, n, piece, stick_ang=-20):
    for i in range(n):
        x = cx + (i - (n - 1) / 2) * 36
        p = rot([(-80, 0), (80, 0)], x, cy + (i - (n - 1) / 2) * 6, stick_ang)
        d.line(p, rgba(H("d7b98e")), 4)
        for k in range(3):
            t = 0.3 + k * 0.22
            px, py = p[0][0] + (p[1][0] - p[0][0]) * t, p[0][1] + (p[1][1] - p[0][1]) * t
            piece(d, px, py)


@dish("nem-lui")
def _(a):
    def c(d):
        for i in range(4):
            x = 70 + i * 38
            d.line([(x - 20, 196), (x + 30, 100)], rgba(H("aed581")), 6)  # que sả
            d.poly(rot([(-12, -34), (12, -34), (12, 34), (-12, 34)], x + 5, 148, 28), rgba(H("a0522d")))
            d.poly(rot(ellpts(0, 0, 12, 34), x + 5, 148, 28), rgba(H("b8693a")))
            grill_marks(d, x + 5, 148, 20, 3, 28 + 90)
        herbs(d, 206, 176, 4, 10, 2)
    plate(a, c)


# ---- Hội An ----
@dish("banh-mi")
def _(a):
    def c(d):
        ang = -18
        crust = rot([(-100, 0)] + [(x, -26 * math.sqrt(max(0, 1 - (x / 100) ** 2)) - 4) for x in range(-96, 97, 8)] + [(100, 0)]
                    + [(x, 24 * math.sqrt(max(0, 1 - (x / 100) ** 2))) for x in range(96, -97, -8)], 128, 150, ang)
        d.poly(crust, rgba(H("d9913a")))
        d.poly(rot([(x, -22 * math.sqrt(max(0, 1 - (x / 92) ** 2)) - 4) for x in range(-88, 89, 8)] + [(80, -6), (-80, -6)], 128, 150, ang), rgba(H("eaae55")))
        for k in range(5):
            d.line(rot([(-60 + k * 30, -22), (-46 + k * 30, -12)], 128, 150, ang), rgba(H("c2701f")), 3)
        # nhân lộ ra ở đường xẻ
        d.poly(rot([(-84, -2), (84, -2), (78, 8), (-78, 8)], 128, 150, ang), rgba(H("f8e1b0")))
        for k in range(6):
            d.poly(rot(ellpts(-70 + k * 28, 2, 14, 6), 128, 150, ang), rgba(H("e57373")))
        for k in range(7):
            leaf(d, *rot([(-76 + k * 25, -6)], 128, 150, ang)[0], 16, ang - 60 + (k % 2) * 120, H("43a047"))
        dots(d, *rot([(0, 4)], 128, 150, ang)[0], 120, 8, 8, H("ff8a65"), 3, 5)
    plate(a, c, col=H("efe6d2"), cy=164, ry=48)

@dish("nuoc-mot")
def _(a):
    glass(a, H("c6d98a"), ice_n=4, fill=0.84,
          garnish=lambda a: a.draw(lambda d: (leaf(d, 150, 54, 28, -20, H("43a047")), leaf(d, 172, 46, 24, 40, H("66bb6a")),
                                              d.line([(98, 30), (114, 120)], rgba(H("c5e1a5")), 6),
                                              d.circ(186, 62, 16, rgba(H("7cb342"))), d.circ(186, 62, 12, rgba(H("e6ee9c"))))))


@dish("banh-bao-banh-vac")
def _(a):
    def c(d):
        for i, (x, y) in enumerate(((80, 144), (128, 136), (176, 144), (102, 172), (152, 172))):
            for k in range(5):
                ang = math.radians(k * 72 - 90)
                d.ell(x - 14 + 9 * math.cos(ang), y - 10 + 6 * math.sin(ang), x + 14 + 9 * math.cos(ang), y + 10 + 6 * math.sin(ang), rgba(H("e3ddd3")))
            for k in range(5):
                ang = math.radians(k * 72 - 90)
                d.ell(x - 12 + 8 * math.cos(ang), y - 9 + 5 * math.sin(ang), x + 11 + 8 * math.cos(ang), y + 7 + 5 * math.sin(ang), rgba(H("fbf8f3")))
            d.ell(x - 10, y - 7, x + 10, y + 6, rgba(H("ff8a65")))
            dots(d, x, y - 1, 16, 8, 6, H("c58b3a"), 2.4, i)
    plate(a, c, col=H("3f6fb5"), edge=H("ffffff"))

@dish("cao-lau")
def _(a):
    def c(d):
        noodles(d, 128, 112, 150, 28, H("f0c27a"), 6, 3, thick=5.5)
        beef_slices(d, 140, 106, 4, 30, 4, H("b5674a"))
        herbs(d, 86, 108, 6, 18, 5)
        for (x, y) in ((168, 112), (100, 120)):
            d.poly(rot([(-12, -8), (12, -8), (12, 8), (-12, 8)], x, y, 20), rgba(H("f2c46d")))
    bowl(a, H("9a5a32"), c, body=H("efe6d2"), band=H("6d4c41"))


@dish("mi-quang")
def _(a):
    def c(d):
        noodles(d, 128, 112, 150, 28, H("f7d46b"), 6, 4, thick=5)
        shrimp(d, 100, 106, 0.9, 20)
        shrimp(d, 150, 104, 0.9, -20)
        egg_half(d, 126, 116, 11)
        herbs(d, 170, 116, 4, 12, 6)
        dots(d, 128, 112, 130, 26, 12, H("c58b3a"), 3, 9)
    bowl(a, H("e8a33a"), c)
    a.draw(lambda d: (d.circ(206, 196, 26, rgba(H("f5deb3"))), dots(d, 206, 196, 30, 30, 14, H("3e2723"), 2.2, 1)))  # bánh tráng mè


@dish("com-ga")
def _(a):
    def c(d):
        d.ell(56, 120, 200, 186, rgba(H("f2c84b")))
        rice_grains(d, 128, 153, 140, 60, 5, H("f8d76a"))
        r = random.Random(3)
        for i in range(10):
            x, y = 128 + r.uniform(-40, 40), 140 + r.uniform(-12, 12)
            d.line([(x - 12, y), (x + 12, y + r.uniform(-4, 4))], rgba(H("fff1d6")), 5)
        herbs(d, 128, 136, 5, 30, 4)
        d.poly(rot(ellpts(0, 0, 16, 6), 206, 170, 10), rgba(H("ffab91")))
    plate(a, c)


@dish("che-bap")
def _(a):
    def c(d):
        dots(d, 128, 112, 170, 40, 40, H("ffd54f"), 4.5, 3)
        d.ell(96, 102, 170, 122, rgba(H("fffdf5")))
        dots(d, 130, 112, 60, 14, 10, H("f5f5f5"), 2, 5)
    bowl(a, H("f6dc7a"), c, body=H("fffaf0"), band=H("e0a24a"))


@dish("hoanh-thanh-chien")
def _(a):
    def c(d):
        for i, (x, y) in enumerate(((84, 150), (128, 140), (172, 152), (106, 174), (152, 176))):
            d.poly([(x - 30, y + 10), (x - 10, y - 18), (x + 10, y - 22), (x + 30, y + 8), (x, y + 18)], rgba(H("e8a33a")))
            d.poly([(x - 18, y + 4), (x - 4, y - 12), (x + 16, y + 2)], rgba(H("f6c86b")))
        d.ell(96, 136, 164, 166, rgba(H("ef6c3a"), 220))  # sốt
        shrimp(d, 116, 150, 0.7, 10)
        dots(d, 140, 150, 30, 12, 6, H("8bc34a"), 3, 2)
    plate(a, c)


# ---- Đà Nẵng ----
@dish("banh-trang-cuon-thit-heo")
def _(a):
    def c(d):
        for k in range(4):
            x = 70 + k * 36
            d.poly(rot(ellpts(0, 0, 30, 9), x, 148 + (k % 2) * 6, 70), rgba(H("fde7d8")))
            d.poly(rot(ellpts(0, 0, 26, 4), x, 148 + (k % 2) * 6, 70), rgba(H("e4b49a")))
        herbs(d, 196, 138, 6, 16, 2)
        d.circ(196, 178, 16, rgba(H("ffe0b2")))
        d.circ(196, 178, 12, rgba(H("8d6e63")))
    plate(a, c)


@dish("nuoc-mia")
def _(a):
    glass(a, H("dce38a"), top=40, ice_n=6, fill=0.85, straw=H("e53935"), foam=H("f4f6d0"),
          garnish=lambda a: a.draw(lambda d: (d.circ(84, 46, 14, rgba(H("ffca28"))), d.circ(84, 46, 10, rgba(H("fff59d"))))))


def xeo(d, cx, cy, s=1.0):
    pts = ellpts(cx, cy, 92 * s, 52 * s, 20, 180, 360) + [(cx + 92 * s, cy)]
    d.poly(pts + [(cx - 92 * s, cy)], rgba(H("eab12f")))
    d.poly([(cx - 92 * s, cy), (cx + 92 * s, cy), (cx + 80 * s, cy + 12 * s), (cx - 80 * s, cy + 12 * s)], rgba(H("d89520")))
    for k in range(3):
        shrimp(d, cx - 46 * s + k * 44 * s, cy - 20 * s, 0.8 * s, 10)
    for k in range(6):
        d.line([(cx - 60 * s + k * 22 * s, cy - 2), (cx - 54 * s + k * 22 * s, cy + 16 * s)], rgba(H("f1f8e9")), 3)
    dots(d, cx, cy - 30 * s, 120 * s, 20 * s, 10, H("c77a1c"), 3, 4)


@dish("banh-xeo")
def _(a):
    plate(a, lambda d: (xeo(d, 128, 156), herbs(d, 206, 182, 5, 12, 2)))


@dish("banh-xeo-mien-nam")
def _(a):
    plate(a, lambda d: (xeo(d, 124, 156, 1.1), herbs(d, 40, 180, 6, 14, 3), herbs(d, 210, 186, 6, 14, 5)), rx=118)


@dish("bun-cha-ca")
def _(a):
    def c(d):
        noodles(d, 128, 112, 150, 28, H("fbf6e9"), 6, 2)
        for (x, y) in ((96, 106), (148, 102)):
            d.poly([(x - 18, y - 10), (x + 18, y - 10), (x + 18, y + 10), (x - 18, y + 10)], rgba(H("f0d9b0")))
            d.line([(x - 18, y - 10), (x + 18, y - 10)], rgba(H("c98a3a")), 3)
        d.ell(120, 112, 144, 126, rgba(H("e53935")))  # cà chua
        d.ell(160, 112, 184, 124, rgba(H("f5deb3")))  # thơm
        scallion(d, 128, 112, 8, 40, 3)
        herbs(d, 76, 116, 3, 10, 7)
    bowl(a, H("efb366"), c)


@dish("nuoc-dua")
def _(a):
    a.piece(lambda d, f: d.ell(48, 70, 208, 220, f), H("7cb342"), light=0.45)
    a.piece(lambda d, f: d.poly([(80, 92), (176, 92), (160, 64), (96, 64)], f), H("c5e1a5"))
    a.piece(lambda d, f: d.ell(96, 56, 160, 76, f), H("f1f8e9"))
    a.piece(lambda d, f: d.line([(132, 70), (170, 10)], f, 9), H("ff7043"), shade=0)
    a.draw(lambda d: d.ell(70, 100, 98, 150, (255, 255, 255, 70)))


@dish("oc-hut")
def _(a):
    def c(d):
        r = random.Random(5)
        for i in range(10):
            x, y = 70 + (i % 5) * 30 + r.uniform(-4, 4), 140 + (i // 5) * 26 + r.uniform(-3, 3)
            d.poly(rot([(-8, -6), (22, -2), (22, 2), (-8, 6)], x, y, r.uniform(-30, 30)), rgba(H("8d6e63")))
            d.circ(x - 6, y, 9, rgba(H("a1887f")))
        d.ell(70, 130, 190, 186, rgba(H("ff8f3a"), 160))
        herbs(d, 128, 150, 6, 50, 9)
        chili(d, 128, 156, 5, 50, 3)
    plate(a, c)


@dish("tom-nuong")
def _(a):
    plate(a, lambda d: (skewers(d, 128, 150, 3, lambda d, x, y: shrimp(d, x, y, 0.9, 30, H("f4511e"))), lime(d, 208, 182, 13)))


# ---- Nha Trang ----
@dish("nem-nuong")
def _(a):
    def c(d):
        skewers(d, 116, 148, 3, lambda d, x, y: (d.ell(x - 13, y - 11, x + 13, y + 11, rgba(H("b5562e"))), d.ell(x - 9, y - 9, x + 7, y + 3, rgba(H("d07a4a")))))
        herbs(d, 210, 140, 4, 10, 3)
        d.circ(206, 184, 16, rgba(H("ffe0b2")))
        d.circ(206, 184, 12, rgba(H("ff9800")))
    plate(a, c)


@dish("banh-can")
def _(a):
    a.piece(lambda d, f: d.ell(20, 108, 236, 214, f), H("7a4b33"))
    def c(d):
        for i in range(7):
            x = 66 + (i % 4) * 42 + (21 if i >= 4 else 0)
            y = 144 + (i // 4) * 34
            d.ell(x - 20, y - 13, x + 20, y + 13, rgba(H("5d3a26")))
            d.ell(x - 16, y - 11, x + 16, y + 9, rgba(H("f3e2bd")))
            d.circ(x, y - 2, 6, rgba(H("ffb300")) if i % 2 == 0 else rgba(H("ff8a65")))
            scallion(d, x, y, 2, 6, i)
    a.draw(c)


@dish("bun-ca-sua")
def _(a):
    def c(d):
        noodles(d, 128, 112, 150, 28, H("fbf6e9"), 6, 4)
        for (x, y) in ((100, 106), (150, 104)):
            d.ell(x - 18, y - 10, x + 18, y + 10, rgba(H("e6d2a8")))
        for (x, y) in ((126, 116), (168, 116)):
            d.ell(x - 14, y - 8, x + 14, y + 8, rgba(H("e1f5fe"), 200))
            d.arc(x - 14, y - 8, x + 14, y + 8, 0, 360, rgba(H("b3e5fc")), 2)
        d.ell(74, 108, 94, 120, rgba(H("e53935")))
        scallion(d, 128, 110, 8, 40, 6)
    bowl(a, H("f0c68a"), c)


@dish("sinh-to-bo")
def _(a):
    glass(a, H("a5c85a"), top=44, fill=0.9, straw=H("ffffff"),
          extra=lambda d, lt: (d.ell(80, lt - 12, 176, lt + 14, rgba(H("f3f7d4"))), d.circ(128, lt - 8, 9, rgba(H("6d4c41")))))


@dish("muc-mot-nang")
def _(a):
    def c(d):
        for k, (x, ang) in enumerate(((92, -14), (164, 14))):
            def P(pts): return rot(pts, x, 146, ang)
            d.poly(P([(-24, -60), (24, -60), (30, -54), (28, 16), (-28, 16), (-30, -54)]), rgba(H("f0c39a")))
            d.poly(P([(0, -88), (34, -54), (-34, -54)]), rgba(H("eab486")))   # vây đầu nhọn
            for t in range(7):
                d.line(P([(-21 + t * 7, 14), (-26 + t * 8.6, 50 + (t % 2) * 6)]), rgba(H("e4a77a")), 5)
            d.circ(*P([(-12, 6)])[0], 4, rgba(H("3e2723")))
            d.circ(*P([(12, 6)])[0], 4, rgba(H("3e2723")))
            for gy in (-40, -20, -2):
                d.line(P([(-24, gy), (24, gy - 6)]), rgba(H("8d4f2a"), 200), 3)
        d.circ(128, 196, 14, rgba(H("ffe0b2")))
        d.circ(128, 196, 10, rgba(H("ef5350")))
    plate(a, c, cy=164)

@dish("tom-hum-nuong")
def _(a):
    R["tom-hum"](a)
    a.draw(lambda d: grill_marks(d, 150, 144, 70, 4, -10))


# ---- Đà Lạt ----
@dish("banh-trang-nuong")
def _(a):
    def c(d):
        d.ell(36, 106, 220, 200, rgba(H("e8b74f")))
        d.ell(44, 110, 212, 192, rgba(H("f6d37e")))
        d.ell(80, 128, 176, 172, rgba(H("ffb300"), 210))  # trứng
        scallion(d, 128, 150, 14, 70, 3)
        dots(d, 128, 150, 140, 60, 16, H("ff7043"), 3.5, 5)  # tôm khô
        d.line(curve((70, 140), (128, 120), (186, 160)), rgba(H("fff3e0")), 4)  # sốt mayo
        d.line([(128, 106), (128, 200)], rgba(H("c98a3a"), 120), 2)
        d.line([(36, 153), (220, 153)], rgba(H("c98a3a"), 120), 2)
    a.draw(c)
    a.piece(lambda d, f: d.ell(36, 106, 220, 200, f), H("e8b74f"), deco=c, shade=0.15, light=0.3)


@dish("sua-dau-nanh")
def _(a):
    a.piece(lambda d, f: (d.rect(84, 80, 172, 222, f, 26), d.rect(106, 36, 150, 92, f, 10)), (232, 244, 250), light=0.3,
            deco=lambda d: (d.rect(88, 96, 168, 218, rgba(H("f3e9cc")), 22), d.rect(90, 140, 166, 176, rgba(H("7cb342")))))
    a.piece(lambda d, f: d.rect(100, 22, 156, 42, f, 6), H("2e7d32"))


@dish("banh-uot-long-ga")
def _(a):
    def c(d):
        for k in range(3):
            y = 134 + k * 18
            d.ell(52, y - 12, 162, y + 12, rgba(H("f8f6f0")))
            d.ell(56, y - 9, 158, y + 4, rgba(H("ffffff")))
        for (x, y) in ((186, 140), (196, 166), (168, 172)):
            d.ell(x - 12, y - 9, x + 12, y + 9, rgba(H("a0522d")))
        d.ell(170, 126, 190, 142, rgba(H("ffb74d")))  # trứng non
        herbs(d, 110, 132, 5, 40, 4, (H("2e7d32"), H("43a047")))
        dots(d, 110, 150, 80, 30, 10, H("c58b3a"), 3, 6)
    plate(a, c)


@dish("lau-ga-la-e")
def _(a):
    def c(d):
        for (x, y) in ((96, 110), (140, 104), (170, 116)):
            d.ell(x - 18, y - 12, x + 18, y + 12, rgba(H("f5d9a8")))
            d.ell(x - 14, y - 10, x + 8, y + 2, rgba(H("ffe8c2")))
        herbs(d, 128, 112, 12, 60, 5, (H("33691e"), H("558b2f"), H("689f38")))
        chili(d, 120, 116, 4, 40, 3)
        d.ell(70, 104, 92, 120, rgba(H("ffd54f")))
    hotpot(a, c, H("e9c56d"))
    steam(a, (96, 128, 160), 76)


@dish("tra-atiso")
def _(a):
    cup(a, H("7b2d26"), col=H("fff7e3"), band=H("6d4c41"))


@dish("xien-nuong")
def _(a):
    def p(d, x, y):
        d.ell(x - 12, y - 12, x + 12, y + 12, rgba(H("b5562e")))
        d.circ(x - 3, y - 4, 4, rgba(H("e0915e")))
    def q(d, x, y):
        d.ell(x - 13, y - 9, x + 13, y + 9, rgba(H("f0a35a")))
    def c(d):
        skewers(d, 108, 150, 2, p)
        skewers(d, 186, 150, 1, q)
        chili(d, 70, 186, 3, 8, 4)
    plate(a, c)


@dish("dau-tay-dam")
def _(a):
    def straw(d, x, y, s=1.0):
        d.poly([(x - 14 * s, y - 8 * s)] + curve((x - 14 * s, y - 8 * s), (x, y + 30 * s), (x + 14 * s, y - 8 * s)), rgba(H("e53935")))
        d.ell(x - 14 * s, y - 14 * s, x + 14 * s, y - 2 * s, rgba(H("e53935")))
        dots(d, x, y, 18 * s, 18 * s, 6, H("ffeb3b"), 1.5 * s, int(x))
        leaf(d, x - 5 * s, y - 13 * s, 12 * s, 30, H("43a047"), False)
        leaf(d, x + 5 * s, y - 13 * s, 12 * s, -30, H("43a047"), False)
    glass(a, H("f8bbd0"), top=60, fill=0.75,
          extra=lambda d, lt: [straw(d, 98 + i * 30, lt + 14 + (i % 2) * 14) for i in range(3)])
    a.draw(lambda d: (straw(d, 112, 58, 1.2), straw(d, 150, 52, 1.2), d.ell(90, 40, 170, 64, rgba(H("fffdf5")))))


# ---- Sài Gòn ----
@dish("hu-tieu-go")
def _(a):
    def c(d):
        noodles(d, 128, 112, 150, 28, H("fdfbf3"), 7, 6, thick=2.6)
        beef_slices(d, 110, 106, 3, 18, 5, H("e4b49a"))
        for x in (148, 168):
            d.circ(x, 106, 10, rgba(H("d7ccc8")))
        d.ell(80, 112, 102, 124, rgba(H("8d6e63")))  # gan
        herbs(d, 128, 112, 5, 40, 8, (H("c5e1a5"), H("8bc34a")))
        scallion(d, 128, 112, 6, 40, 2)
    bowl(a, H("eedbb1"), c, body=H("f5f1e8"), band=H("e53935"))


@dish("ca-phe-sua-da")
def _(a):
    glass(a, None, top=50, fill=0.86, layers=[(0, H("6d3f1f")), (0.55, H("c99a6a")), (0.85, H("f3e1c0"))], ice_n=5, straw=H("1e88e5"))


@dish("banh-trang-tron")
def _(a):
    def c(d):
        r = random.Random(4)
        for i in range(40):
            x, y = 128 + r.uniform(-74, 74), 154 + r.uniform(-26, 24)
            d.line(rot([(-12, 0), (12, 0)], x, y, r.uniform(0, 180)), rgba(H("f0a35a")), 4)
        for (x, y) in ((96, 146), (150, 160), (126, 138)):
            d.circ(x, y, 10, rgba(H("fffdf5")))
            d.circ(x, y, 5, rgba(H("ffb300")))
        herbs(d, 128, 152, 8, 60, 3)
        dots(d, 128, 156, 140, 40, 20, H("6d4c41"), 3, 8)
        dots(d, 128, 156, 140, 40, 14, H("ff7043"), 3, 9)
    plate(a, c)


def com_tam(d, chop=True):
    d.ell(56, 124, 150, 186, rgba(H("fffdf7")))
    rice_grains(d, 103, 155, 90, 56, 7)
    if chop:
        d.poly(rot(ellpts(0, 0, 40, 22, 16), 176, 146, -20), rgba(H("a0522d")))
        d.poly(rot(ellpts(-4, -4, 30, 14, 16), 176, 146, -20), rgba(H("c2703f")))
        grill_marks(d, 176, 146, 46, 3, 70)
    d.ell(150, 166, 196, 192, rgba(H("fffdf5")))
    d.circ(170, 178, 9, rgba(H("ffb300")))
    dots(d, 70, 186, 30, 10, 6, H("ff8a65"), 3, 2)
    d.poly(rot(ellpts(0, 0, 14, 5), 66, 140, 30), rgba(H("81c784")))


@dish("com-tam")
def _(a):
    plate(a, lambda d: (com_tam(d), scallion(d, 103, 140, 6, 30, 4)))


@dish("com-tam-suon-bi-cha")
def _(a):
    def c(d):
        com_tam(d)
        r = random.Random(2)
        for i in range(10):
            x, y = 110 + r.uniform(-20, 20), 182 + r.uniform(-6, 6)
            d.line([(x - 10, y), (x + 10, y + r.uniform(-3, 3))], rgba(H("fff3e0")), 3)  # bì
        d.poly([(30, 150), (60, 140), (66, 168), (36, 176)], rgba(H("ffcc80")))  # chả trứng
        d.poly([(34, 154), (58, 146), (62, 164), (38, 170)], rgba(H("ffb74d")))
    plate(a, c, rx=118)


@dish("hu-tieu-nam-vang")
def _(a):
    def c(d):
        noodles(d, 128, 112, 150, 28, H("fdfbf3"), 7, 9, thick=2.6)
        shrimp(d, 96, 104, 0.9, 10)
        shrimp(d, 160, 106, 0.9, -15)
        egg_half(d, 128, 104, 10)
        d.ell(70, 112, 92, 124, rgba(H("8d6e63")))
        herbs(d, 128, 116, 5, 40, 3)
        dots(d, 128, 112, 140, 26, 10, H("fff59d"), 3, 4)
    bowl(a, H("efdcb5"), c)


@dish("ca-phe-muoi")
def _(a):
    cup(a, H("6d3f1f"), col=H("fff7e3"), band=None,
        topping=lambda d: (d.ell(84, 103, 172, 120, rgba(H("efe0c2"))), d.line(curve((86, 112), (128, 100), (170, 114)), rgba(H("a1887f")), 3),
                           dots(d, 128, 110, 60, 10, 6, H("ffffff"), 2, 3)))


# ---- Cần Thơ ----
@dish("lau-mam")
def _(a):
    def c(d):
        shrimp(d, 96, 108, 0.9, 10)
        d.ell(130, 100, 170, 120, rgba(H("efe6d2")))  # cá
        d.ell(150, 110, 176, 124, rgba(H("7e57c2")))  # cà tím
        herbs(d, 112, 116, 8, 50, 2, (H("33691e"), H("8bc34a")))
        chili(d, 140, 110, 4, 40, 5)
    hotpot(a, c, H("6e4a2a"))
    steam(a, (96, 128, 160), 76)
    a.draw(lambda d: herbs(d, 40, 210, 5, 14, 9))


@dish("banh-cong")
def _(a):
    def c(d):
        for i, (x, y) in enumerate(((84, 148), (132, 140), (180, 150), (110, 176), (158, 178))):
            d.ell(x - 24, y - 16, x + 24, y + 16, rgba(H("c27a2a")))
            d.ell(x - 20, y - 14, x + 18, y + 10, rgba(H("e39a3f")))
            shrimp(d, x, y - 6, 0.6, 10)
        herbs(d, 210, 120, 4, 10, 6)
    plate(a, c)


@dish("bun-rieu")
def _(a):
    def c(d):
        noodles(d, 128, 112, 150, 26, H("fbf6e9"), 6, 8)
        for (x, y) in ((96, 104), (152, 100)):
            d.ell(x - 22, y - 10, x + 22, y + 10, rgba(H("c97a44")))  # gạch cua
            dots(d, x, y, 30, 12, 8, H("8d4f2a"), 2.5, x)
        for (x, y) in ((126, 116), (176, 112)):
            d.ell(x - 12, y - 8, x + 12, y + 8, rgba(H("e53935")))
            d.ell(x - 8, y - 5, x + 8, y + 5, rgba(H("ef9a9a")))
        d.poly([(66, 110), (88, 110), (88, 126), (66, 126)], rgba(H("f0b75a")))  # đậu
        scallion(d, 128, 110, 8, 40, 3)
    bowl(a, H("e66a2c"), c)


@dish("trai-cay-dia")
def _(a):
    def c(d):
        d.poly([(40, 160)] + curve((40, 160), (80, 110), (120, 160)), rgba(H("e53935")))  # dưa hấu
        d.poly([(40, 160), (120, 160), (114, 168), (46, 168)], rgba(H("7cb342")))
        dots(d, 80, 146, 40, 14, 6, H("212121"), 2.2, 2)
        d.ell(118, 120, 176, 170, rgba(H("ffca28")))  # xoài
        d.ell(124, 126, 168, 160, rgba(H("ffd54f")))
        d.ell(166, 136, 220, 186, rgba(H("ec407a")))  # thanh long
        d.ell(172, 142, 214, 180, rgba(H("fafafa")))
        dots(d, 193, 161, 34, 28, 14, H("212121"), 1.6, 5)
        d.circ(100, 184, 13, rgba(H("8e24aa")))  # nho / chôm chôm
        d.circ(124, 190, 13, rgba(H("8e24aa")))
    plate(a, c)


def whole_fish(d, cx, cy, col, s=1.0, charred=False):
    body = ellpts(cx, cy, 80 * s, 26 * s, 24)
    d.poly(body, rgba(col))
    d.poly([(cx + 70 * s, cy), (cx + 108 * s, cy - 26 * s), (cx + 104 * s, cy + 26 * s)], rgba(dk(col, 0.1)))
    d.circ(cx - 60 * s, cy - 6 * s, 5 * s, rgba(H("fafafa")))
    d.circ(cx - 60 * s, cy - 6 * s, 2.5 * s, rgba(H("212121")))
    d.line([(cx - 44 * s, cy - 20 * s), (cx - 40 * s, cy + 20 * s)], rgba(dk(col, 0.25)), 2.5)
    if charred:
        dots(d, cx, cy, 130 * s, 36 * s, 30, H("3e2723"), 4 * s, 7)


@dish("ca-loc-nuong-trui")
def _(a):
    def c(d):
        d.line([(20, 176), (236, 128)], rgba(H("c8a46e")), 6)
        whole_fish(d, 120, 152, H("6d4c41"), 1.0, charred=True)
        d.poly(rot(ellpts(0, 0, 30, 9), 130, 150, -12), rgba(H("fff3e0")))  # thịt cá lộ ra
        herbs(d, 200, 186, 5, 12, 3)
    leaf_plate(a, c)


@dish("vit-nau-chao")
def _(a):
    def c(d):
        for (x, y) in ((96, 106), (140, 102), (168, 116)):
            d.ell(x - 20, y - 12, x + 20, y + 12, rgba(H("c08050")))
            d.ell(x - 16, y - 10, x + 12, y + 2, rgba(H("e0a777")))
        for (x, y) in ((120, 118), (74, 116)):
            d.poly([(x - 12, y - 9), (x + 12, y - 9), (x + 12, y + 9), (x - 12, y + 9)], rgba(H("d1c4e9")))  # khoai môn
        herbs(d, 128, 110, 4, 40, 2)
    clay_pot(a, c, broth=H("e6b98a"))


@dish("goi-ca-trich")
def _(a):
    def c(d):
        for k in range(6):
            x = 66 + k * 24
            d.poly(rot(ellpts(0, 0, 22, 8), x, 150 + (k % 2) * 10, 60), rgba(H("f3e5f5")))
            d.poly(rot(ellpts(0, -4, 18, 3), x, 150 + (k % 2) * 10, 60), rgba(H("b0bec5")))
        dots(d, 120, 150, 120, 30, 16, H("ffe0b2"), 3, 2)  # dừa nạo
        herbs(d, 128, 160, 6, 60, 4)
        scallion(d, 128, 150, 6, 50, 5)
        d.circ(200, 182, 18, rgba(H("efe6d2")))  # bánh tráng
    plate(a, c)


@dish("banh-canh-ghe")
def _(a):
    def c(d):
        noodles(d, 128, 112, 150, 28, H("fff8e1"), 6, 10, thick=6)
        d.ell(70, 96, 120, 124, rgba(H("ef6c00")))  # càng ghẹ
        d.poly([(110, 100), (130, 92), (132, 108)], rgba(H("ef6c00")))
        d.ell(140, 100, 178, 120, rgba(H("fff3e0")))  # thịt ghẹ
        dots(d, 128, 110, 130, 24, 14, H("ff7043"), 2.6, 4)
        scallion(d, 128, 112, 8, 40, 3)
    bowl(a, H("f08a3a"), c)


@dish("bun-ken")
def _(a):
    def c(d):
        noodles(d, 128, 112, 150, 28, H("fbf6e9"), 6, 11)
        d.ell(60, 98, 196, 126, rgba(H("fff3d6"), 180))  # nước cốt dừa
        dots(d, 128, 108, 120, 20, 22, H("d7a86e"), 3.5, 5)  # cá xé
        herbs(d, 150, 110, 6, 30, 7, (H("43a047"), H("8bc34a"), H("ab47bc")))
        dots(d, 100, 114, 50, 10, 8, H("6d4c41"), 3, 7)
    bowl(a, H("eec56a"), c)


@dish("ruou-sim")
def _(a):
    a.piece(lambda d, f: (d.rect(52, 96, 128, 224, f, 18), d.rect(76, 30, 104, 104, f, 8)), H("5e2750"), light=0.4,
            deco=lambda d: (d.rect(56, 140, 124, 190, rgba(H("fff3e0"))), d.circ(90, 165, 14, rgba(H("7b1fa2")))))
    a.piece(lambda d, f: d.rect(72, 18, 108, 36, f, 5), H("8d6e63"))
    glass(a, H("7b1f5a"), top=150, bottom=226, wtop=36, wbot=26, fill=0.6, cx=178)


@dish("nhum-nuong-mo-hanh")
def _(a):
    def c(d):
        for i, (x, y) in enumerate(((84, 148), (140, 140), (110, 178), (170, 176))):
            for k in range(14):
                ang = math.radians(k * 360 / 14)
                d.line([(x, y), (x + 34 * math.cos(ang), y + 22 * math.sin(ang))], rgba(H("4a148c")), 3)
            d.ell(x - 26, y - 17, x + 26, y + 17, rgba(H("6a1b9a")))
            d.ell(x - 20, y - 12, x + 20, y + 12, rgba(H("ffb74d")))
            scallion(d, x, y, 5, 14, i)
            dots(d, x, y, 24, 12, 5, H("c58b3a"), 2.2, i + 3)
    plate(a, c)


# ---- Sự kiện ----
@dish("banh-chung")
def _(a):
    a.piece(lambda d, f: d.poly([(128, 52), (232, 112), (128, 172), (24, 112)], f), H("4c8a2e"),
            deco=lambda d: [d.line([(76 + k * 30, 82 + k * 17), (-28 + k * 30 + 52 + 24, 142 + k * 17 - 60 + 30)], rgba(H("66a843")), 2) for k in range(1)])
    a.piece(lambda d, f: d.poly([(24, 112), (128, 172), (128, 222), (24, 162)], f), H("3e7526"))
    a.piece(lambda d, f: d.poly([(232, 112), (128, 172), (128, 222), (232, 162)], f), H("336420"))
    def strings(d):
        for t in (0.33, 0.66):
            p0 = (24 + 104 * t, 112 - 60 * t)
            p1 = (128 + 104 * t, 172 - 60 * t)
            d.line([p0, p1, (p1[0], p1[1] + 50)], rgba(H("f0e2b6")), 3.5)
            q0 = (128 - 104 * t, 52 + 60 * t + 0)
            q0 = (24 + 104 * t, 112 + 60 * t)
            q1 = (128 + 104 * t, 52 + 60 * t)
            d.line([q0, q1], rgba(H("f0e2b6")), 3.5)
            d.line([q0, (q0[0], q0[1] + 50)], rgba(H("f0e2b6")), 3.5)
    a.draw(strings)


@dish("gio-lua")
def _(a):
    def c(d):
        d.ell(30, 110, 150, 210, rgba(H("5d9b3a")))
        d.ell(40, 116, 140, 202, rgba(H("efe0d0")))
        d.ell(52, 124, 128, 194, rgba(H("f3e7da")))
        for k in range(3):
            x = 160 + k * 22
            d.poly([(x - 14, 128), (x + 10, 120), (x + 14, 196), (x - 10, 204)], rgba(H("eadbc8")))
            d.poly([(x - 14, 128), (x - 10, 204), (x - 16, 204), (x - 20, 128)], rgba(H("6aa045")))
        herbs(d, 96, 150, 2, 6, 1, (H("4caf50"),))
    plate(a, c, col=H("f5f0e6"))


@dish("thit-kho-trung")
def _(a):
    def c(d):
        for (x, y) in ((90, 108), (128, 102), (168, 112), (110, 120)):
            d.rect(x - 16, y - 12, x + 16, y + 12, rgba(H("8d4a22")), 5)
            d.rect(x - 16, y - 12, x + 16, y - 4, rgba(H("f3c58a")), 4)
        for (x, y) in ((150, 120), (72, 116)):
            d.ell(x - 14, y - 10, x + 14, y + 10, rgba(H("b8742f")))
            d.ell(x - 8, y - 6, x + 8, y + 6, rgba(H("f9a825")))
        chili(d, 128, 110, 3, 40, 4)
    clay_pot(a, c, broth=H("8a4b1e"))


@dish("canh-mang")
def _(a):
    def c(d):
        r = random.Random(4)
        for i in range(10):
            x, y = 128 + r.uniform(-60, 60), 112 + r.uniform(-14, 14)
            d.poly(rot([(-14, -4), (14, -4), (14, 4), (-14, 4)], x, y, r.uniform(-40, 40)), rgba(H("f0e2a8")))
        beef_slices(d, 120, 108, 3, 30, 6, H("e4b49a"))
        scallion(d, 128, 110, 8, 50, 2)
        herbs(d, 160, 116, 3, 10, 3)
    bowl(a, H("efe0b0"), c)


@dish("mut-tet")
def _(a):
    a.piece(lambda d, f: d.poly([(128, 70)] + [(128 + 108 * math.cos(math.radians(-90 + k * 45)), 150 + 72 * math.sin(math.radians(-90 + k * 45))) for k in range(9)], f),
            H("c62828"), light=0.3)
    def c(d):
        cols = [H("ffb300"), H("8bc34a"), H("f06292"), H("fff59d"), H("ff7043"), H("a1887f")]
        for k in range(6):
            ang = math.radians(-90 + k * 60)
            x, y = 128 + 60 * math.cos(ang), 150 + 38 * math.sin(ang)
            d.ell(x - 26, y - 16, x + 26, y + 16, rgba(H("ffd54f")))
            dots(d, x, y, 34, 18, 9, cols[k], 5, k)
        d.ell(102, 132, 154, 168, rgba(H("ffd54f")))
        dots(d, 128, 150, 36, 22, 8, H("e53935"), 4.5, 9)
    a.draw(c)


def mooncake(d, cx, cy, r, col, pattern):
    d.circ(cx, cy, r, rgba(dk(col, 0.1)))
    for k in range(12):
        ang = math.radians(k * 30)
        d.circ(cx + r * math.cos(ang), cy + r * 0.9 * math.sin(ang), r * 0.18, rgba(dk(col, 0.1)))
    d.circ(cx, cy, r * 0.82, rgba(col))
    d.arc(cx - r * 0.6, cy - r * 0.6, cx + r * 0.6, cy + r * 0.6, 0, 360, rgba(pattern), 3)
    d.star(cx, cy, r * 0.35, rgba(pattern), n=6, inner=0.5)


@dish("banh-nuong")
def _(a):
    plate(a, lambda d: (mooncake(d, 128, 140, 58, H("c98a3a"), H("8a5220")),
                        d.poly([(150, 176), (210, 160), (214, 196), (160, 206)], rgba(H("c98a3a"))),
                        d.poly([(156, 180), (206, 168), (208, 192), (164, 200)], rgba(H("f3e1b0"))),
                        d.circ(184, 186, 8, rgba(H("ffb300")))))


@dish("banh-deo")
def _(a):
    plate(a, lambda d: (mooncake(d, 128, 142, 58, H("f7f3ea"), H("cfc6b6")), leaf(d, 196, 186, 30, -20, H("8bc34a"))))


@dish("che-hat-sen")
def _(a):
    def c(d):
        dots(d, 128, 112, 160, 36, 16, H("fff3d6"), 9, 6)
        dots(d, 128, 112, 160, 36, 16, H("f5e3b5"), 4, 6)
        d.ell(100, 104, 160, 120, rgba(H("ffffff"), 120))
    bowl(a, H("f2e6bf"), c, body=H("fffaf0"), band=H("43a047"))
    a.draw(lambda d: leaf(d, 210, 90, 34, -30, H("f8bbd0"), False))


@dish("xoi-gac")
def _(a):
    def c(d):
        d.poly([(48, 172)] + curve((48, 172), (64, 92), (128, 88)) + curve((128, 88), (192, 92), (208, 172)), rgba(H("d84315")))
        rice_grains(d, 128, 136, 150, 80, 3, H("ef6c3a"))
        d.ell(90, 102, 150, 128, rgba(H("ff8a65"), 120))
    leaf_plate(a, c, cy=168)


@dish("goi-cuon")
def _(a):
    def c(d):
        for k in range(3):
            x = 78 + k * 46
            pts = rot([(-17, -44), (17, -44), (17, 44), (-17, 44)], x, 150, 12)
            d.poly(rot(ellpts(0, 0, 18, 46), x, 150, 12), rgba(H("f2f0ea"), 235))
            d.poly(rot(ellpts(0, 0, 12, 36), x, 150, 12), rgba(H("c5e1a5"), 200))
            shrimp(d, x, 136, 0.65, 100)
            d.poly(rot(ellpts(0, 10, 10, 16), x, 150, 12), rgba(H("fffdf5"), 220))
        d.circ(212, 180, 18, rgba(H("ffe0b2")))
        d.circ(212, 180, 14, rgba(H("8d6e63")))
    plate(a, c)


@dish("banh-quy-gung")
def _(a):
    def man(d, cx, cy, s, ang):
        col = H("b56a2c")
        pts = []
        d.poly(rot(ellpts(0, -34 * s, 16 * s, 16 * s), cx, cy, ang), rgba(col))
        d.poly(rot([(-16 * s, -20 * s), (16 * s, -20 * s), (18 * s, 16 * s), (-18 * s, 16 * s)], cx, cy, ang), rgba(col))
        for sx in (-1, 1):
            d.line(rot([(sx * 14 * s, -14 * s), (sx * 34 * s, -2 * s)], cx, cy, ang), rgba(col), 13 * s)
            d.line(rot([(sx * 10 * s, 14 * s), (sx * 18 * s, 38 * s)], cx, cy, ang), rgba(col), 14 * s)
        for k in range(3):
            d.circ(*rot([(0, -12 * s + k * 10 * s)], cx, cy, ang)[0], 3 * s, rgba(H("e53935")))
        for sx in (-1, 1):
            d.circ(*rot([(sx * 6 * s, -38 * s)], cx, cy, ang)[0], 2.5 * s, rgba(H("ffffff")))
        d.arc(*[v for p in rot([(-7 * s, -36 * s)], cx, cy, 0) for v in p], *[v for p in rot([(7 * s, -26 * s)], cx, cy, 0) for v in p], 20, 160, rgba(H("ffffff")), 2)
    plate(a, lambda d: (man(d, 96, 150, 1.0, -12), man(d, 162, 152, 1.0, 14)))


@dish("ga-quay")
def _(a):
    def c(d):
        d.ell(52, 104, 204, 196, rgba(H("b8621f")))
        d.ell(62, 108, 190, 180, rgba(H("d9862f")))
        d.ell(80, 112, 150, 150, rgba(H("efb05a")))
        for sx, x in ((-1, 70), (1, 186)):
            d.line([(x, 160), (x + sx * 26, 120)], rgba(H("c2701f")), 22)
            d.circ(x + sx * 30, 114, 8, rgba(H("fffdf5")))
        herbs(d, 40, 180, 4, 10, 2)
        herbs(d, 220, 182, 4, 10, 6)
        d.circ(206, 196, 9, rgba(H("e53935")))
    plate(a, c, cy=164)


@dish("banh-bong-lan")
def _(a):
    a.piece(lambda d, f: d.poly([(40, 150), (128, 196), (128, 222), (40, 176)], f), H("f2c24a"))
    a.piece(lambda d, f: d.poly([(128, 196), (226, 128), (226, 154), (128, 222)], f), H("e8b13a"))
    a.piece(lambda d, f: d.poly([(40, 150), (138, 82), (226, 128), (128, 196)], f), H("fff3d0"),
            deco=lambda d: (d.poly([(40, 150), (138, 82), (148, 88), (50, 156)], rgba(H("c98a3a"))), dots(d, 140, 140, 120, 60, 12, H("f06292"), 4, 2)))
    a.draw(lambda d: (d.ell(118, 100, 158, 122, rgba(H("fffdf5"))), d.circ(138, 104, 9, rgba(H("e53935")))))


@dish("sup-kem")
def _(a):
    def c(d):
        d.line(curve((80, 112), (128, 92), (176, 116)), rgba(H("fffdf7")), 5)
        herbs(d, 128, 110, 5, 20, 3, (H("2e7d32"),))
        dots(d, 128, 112, 100, 20, 10, H("ffd54f"), 4, 5)
        dots(d, 128, 112, 100, 20, 8, H("bcaaa4"), 2, 7)
    bowl(a, H("f7e7c4"), c, body=H("fffaf0"), band=H("c62828"))
    a.draw(lambda d: (d.rect(196, 160, 238, 196, rgba(H("e0a24a")), 6), d.rect(198, 162, 236, 172, rgba(H("f3c46b")), 4)))


@dish("so-co-la-nong")
def _(a):
    mug(a, H("6d3f1f"), col=H("c62828"),
        topping=lambda d, lt: (d.ell(64, lt - 16, 160, lt + 16, rgba(H("fffdf5"))), d.rect(86, lt - 22, 104, lt - 6, rgba(H("f8bbd0")), 4),
                               d.rect(116, lt - 26, 134, lt - 10, rgba(H("ffffff")), 4), dots(d, 112, lt, 70, 12, 8, H("5d4037"), 2.4, 1)))



# ---- Món mới của quán sự kiện (8 món mỗi sự kiện) ----
@dish("banh-tet")
def _(a):
    def c(d):
        for k, x in enumerate((92, 164)):
            d.ell(x - 34, 120, x + 34, 196, rgba(H("3e7526")))
            d.ell(x - 26, 128, x + 26, 188, rgba(H("f3ead2")))
            d.ell(x - 18, 138, x + 18, 178, rgba(H("e2c06b")))
            d.ell(x - 9, 148, x + 9, 168, rgba(H("b06b3c")))
        d.line([(60, 110), (196, 110)], rgba(H("f0e2b6")), 3)
    plate(a, c)


@dish("ga-luoc-la-chanh")
def _(a):
    def c(d):
        d.ell(52, 112, 200, 196, rgba(H("e9b65a")))
        d.ell(62, 116, 186, 180, rgba(H("f6d27a")))
        for x in (70, 186):
            d.line([(x, 160), (x + (-26 if x < 128 else 26), 124)], rgba(H("e9b65a")), 20)
        r = random.Random(3)
        for i in range(12):
            d.line(rot([(-8, 0), (8, 0)], 128 + r.uniform(-50, 50), 150 + r.uniform(-24, 18), r.uniform(0, 180)), rgba(H("4caf50")), 2.5)
        chili(d, 128, 150, 4, 40, 2)
        d.circ(212, 190, 14, rgba(H("ffe0b2")))
        d.circ(212, 190, 10, rgba(H("ffffff")))
    plate(a, c, cy=164)


@dish("nem-chua")
def _(a):
    def c(d):
        for i, (x, y) in enumerate(((80, 150), (124, 140), (168, 150), (100, 176), (148, 178))):
            d.rect(x - 18, y - 16, x + 18, y + 16, rgba(H("5d9b3a")), 4)
            d.rect(x - 13, y - 11, x + 13, y + 11, rgba(H("ef9a9a")), 3)
            dots(d, x, y, 18, 14, 4, H("fff3e0"), 2, i)
            d.circ(x + 5, y - 4, 3, rgba(H("e53935")))
        herbs(d, 210, 140, 3, 8, 5)
    plate(a, c)


@dish("banh-pia")
def _(a):
    def c(d):
        d.circ(110, 146, 56, rgba(H("e6c58a")))
        d.circ(110, 146, 48, rgba(H("f3dcae")))
        for k in range(3):
            d.arc(110 - 40 + k * 6, 146 - 40 + k * 6, 110 + 40 - k * 6, 146 + 40 - k * 6, 200, 340, rgba(H("e2c89a")), 2)
        d.circ(110, 140, 8, rgba(H("e53935")))
        d.poly([(150, 176), (210, 160), (214, 196), (160, 206)], rgba(H("e6c58a")))
        d.poly([(156, 180), (206, 168), (208, 192), (164, 200)], rgba(H("c9a24a")))
        d.circ(186, 186, 9, rgba(H("ffb300")))
    plate(a, c)


@dish("che-troi-nuoc")
def _(a):
    def c(d):
        for (x, y) in ((96, 108), (130, 102), (162, 110), (114, 118), (148, 120)):
            d.circ(x, y, 15, rgba(H("fff8ee")))
            d.circ(x - 4, y - 5, 5, rgba(H("ffffff")))
        dots(d, 128, 110, 140, 30, 18, H("fff3c4"), 2.2, 3)  # mè
        d.line(curve((70, 120), (128, 96), (186, 122)), rgba(H("fffdf5")), 5)
        d.line([(84, 98), (100, 92)], rgba(H("f3c46b")), 3)  # gừng
    bowl(a, H("b8752f"), c, body=H("fffaf0"), band=H("8d4f2a"))


@dish("buoi-nam-roi")
def _(a):
    def c(d):
        d.ell(54, 108, 154, 200, rgba(H("9ccc65")))
        d.ell(62, 112, 146, 190, rgba(H("aed581")))
        d.circ(104, 112, 6, rgba(H("6d4c41")))
        leaf(d, 118, 104, 30, -30, H("43a047"))
        for k in range(4):
            x = 150 + k * 18
            d.poly([(x, 196), (x + 14, 140), (x + 28, 196)], rgba(H("f8bbd0")))
            d.poly([(x + 4, 192), (x + 14, 152), (x + 24, 192)], rgba(H("f48fb1")))
    plate(a, c)


@dish("xoi-xeo")
def _(a):
    def c(d):
        d.poly([(48, 170)] + curve((48, 170), (64, 96), (128, 92)) + curve((128, 92), (192, 96), (208, 170)), rgba(H("f6c544")))
        rice_grains(d, 128, 140, 150, 70, 4, H("f2b631"))
        d.ell(80, 104, 176, 140, rgba(H("ffe082")))  # đậu xanh
        dots(d, 128, 112, 90, 26, 18, H("8d5a2b"), 3.2, 6)  # hành phi
    leaf_plate(a, c, cy=168)


@dish("banh-cuon")
def _(a):
    def c(d):
        for k in range(4):
            y = 128 + k * 16
            d.ell(46, y - 12, 176, y + 12, rgba(H("f6f3ec")))
            d.ell(52, y - 9, 170, y + 4, rgba(H("ffffff")))
        dots(d, 110, 150, 100, 50, 22, H("8d5a2b"), 3, 2)  # hành phi
        for (x, y) in ((190, 136), (204, 160)):
            d.ell(x - 18, y - 9, x + 18, y + 9, rgba(H("f2e1c5")))  # chả lụa
        herbs(d, 196, 186, 3, 8, 3)
    plate(a, c)


@dish("che-ba-mau")
def _(a):
    glass(a, None, top=48, fill=0.9, layers=[(0, H("fdfaf2")), (0.22, H("66bb6a")), (0.48, H("ffd54f")), (0.72, H("c62828"))], ice_n=3,
          straw=H("ffca28"))


@dish("banh-khuc-cay")
def _(a):
    def c(d):
        d.rect(44, 122, 196, 182, rgba(H("6d4c41")), 26)
        for k in range(5):
            d.line([(60 + k * 26, 128), (70 + k * 26, 176)], rgba(H("5d4037")), 3)
        d.ell(176, 120, 216, 184, rgba(H("8d6e63")))
        d.arc(182, 128, 210, 176, 0, 360, rgba(H("fff3e0")), 3)
        d.arc(190, 138, 202, 166, 0, 360, rgba(H("fff3e0")), 2)
        dots(d, 120, 124, 120, 6, 10, H("ffffff"), 4, 3)  # đường bột
        leaf(d, 92, 118, 22, -20, H("2e7d32"))
        d.circ(104, 116, 6, rgba(H("e53935")))
        d.circ(112, 120, 6, rgba(H("e53935")))
    plate(a, c)


@dish("keo-gay")
def _(a):
    def cane(d, x, ang):
        pts = [(x, 210), (x, 90)] + [(x + 22 - 22 * math.cos(math.radians(t)), 90 - 22 * math.sin(math.radians(t))) for t in range(0, 181, 15)]
        pts = rot([(px - x, py - 150) for px, py in pts], x, 150, ang)
        d.line(pts, rgba(H("fafafa")), 16)
        for k in range(len(pts) - 1):
            if k % 2 == 0:
                d.line([pts[k], pts[k + 1]], rgba(H("e53935")), 16)
    def c(d):
        cane(d, 96, -12)
        cane(d, 150, 10)
        d.ell(110, 186, 150, 204, rgba(H("2e7d32")))
        d.circ(130, 192, 7, rgba(H("e53935")))
    a.draw(c)
    a.piece(lambda d, f: d.ell(-10, -10, -5, -5, f), H("ffffff"), shade=0, light=0, edge=False)


@dish("banh-tao")
def _(a):
    def c(d):
        d.ell(42, 108, 214, 196, rgba(H("d9913a")))
        d.ell(52, 114, 204, 188, rgba(H("efb75a")))
        for k in range(6):
            x = 66 + k * 24
            d.line([(x, 120), (x + 18, 184)], rgba(H("d9913a")), 6)
        for k in range(4):
            y = 128 + k * 18
            d.line([(54, y), (202, y)], rgba(H("d9913a")), 5)
        d.circ(186, 108, 14, rgba(H("e53935")))
        leaf(d, 196, 96, 16, -40, H("43a047"))
    plate(a, c, cy=158)

ORDER = list(R.keys())

if __name__ == "__main__":
    only = sys.argv[2:]
    for name in (only or ORDER):
        art = Art()
        R[name](art)
        finish(art, name)
    print("done", len(only or ORDER))
