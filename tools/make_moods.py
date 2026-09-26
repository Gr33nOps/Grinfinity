"""Faces for everything in the arena. Original vector artwork, no external assets.

Only the planet is happy, and each of the twelve planets has its own happy
face. Every enemy wears its own bad mood, chosen to suit
how it behaves, and the most common kinds come in a few versions so a crowd of
them never looks cloned:

  Drifter    sad        (built from parts: three rock shapes, three colours,
                         eight unhappy moods, plus scared and shocked reactions)
  Shard      angry      (two versions: gritted teeth, snarl)
  Planetoid  grumpy     (two versions: half-shut eyes, eye-roll)
  Fracture   worried    raised brows, a wobbly mouth, a sweat drop
  Splinter   scared     wide eyes, tiny pupils, a little "o" mouth
  Flare      furious    shouting, with a cross-vein
  Bulwark    stubborn   heavy flat brows and a pout

  Coil        sneering
  Brood       wailing
  Black Hole  menacing

Run from the project root: python tools/make_moods.py
"""
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / 'art' / 'cosmic'
INK = '#321E3D'; CREAM = '#FFF0CE'; BERRY = '#AB4564'; ORANGE = '#F5A451'
TEAR = '#9AD7E8'; BLUSH = '#F08A8A'


def svg(name, body):
    (OUT / (name + '.svg')).write_text(
        f'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 256 256">'
        f'<g stroke="{INK}" stroke-width="8" stroke-linecap="round" stroke-linejoin="round">{body}</g></svg>',
        encoding='utf-8')


def circle(x, y, r, fill, stroke=None, extra=''):
    return f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}"' + (f' stroke="{stroke}"' if stroke else '') + f' {extra}/>'


def path(d, fill='none', extra=''):
    return f'<path d="{d}" fill="{fill}" {extra}/>'


# --- Eyes --------------------------------------------------------------------

def eye(x, y, r=13, look=(3, 4), pupil=0.55, shine=True):
    """A cartoon eye: cream white, ink pupil pushed toward `look`, one glint."""
    px, py = x + look[0], y + look[1]
    s = f'<ellipse cx="{x}" cy="{y}" rx="{r}" ry="{r * 1.3:.1f}" fill="{CREAM}" stroke-width="5"/>'
    s += circle(px, py, round(r * pupil, 1), INK, 'none')
    if shine:
        s += circle(round(px + r * 0.22, 1), round(py - r * 0.28, 1), round(r * 0.19, 1), CREAM, 'none')
    return s


def lid(x, y, r, left, right, skin, line=6):
    """Covers the top of an eye down to a slanted edge. `left`/`right` are how far
    below the eye's centre the edge sits at each side (negative is higher).
    Inner corners low reads angry; outer corners low reads sad."""
    top = y - r * 1.3 - 7
    x0, x1 = x - r - 5, x + r + 5
    s = path(f'M {x0} {top:.1f} L {x1} {top:.1f} L {x1} {y + right} L {x0} {y + left} Z', skin, 'stroke="none"')
    s += path(f'M {x - r - 3} {y + left - 1} L {x + r + 3} {y + right - 1}', 'none', f'stroke-width="{line}"')
    return s


def brow(x0, y0, x1, y1, bend=0, width=6):
    mx, my = (x0 + x1) / 2, (y0 + y1) / 2 - bend
    return path(f'M {x0} {y0} Q {mx} {my} {x1} {y1}', 'none', f'stroke-width="{width}"')


def squeezed(x, y, r, facing):
    """An eye screwed shut: a sideways chevron pointing at the nose (> <)."""
    d = r * 0.9
    tip = x + d * facing
    back = x - d * facing
    return path(f'M {back} {y - d * 0.8} L {tip} {y} L {back} {y + d * 0.8}', 'none', 'stroke-width="7"')


def tear(x, y, size=1.0):
    h, w = 20 * size, 9 * size
    return path(f'M {x} {y} C {x + w} {y + h * 0.6} {x + w} {y + h} {x} {y + h} C {x - w} {y + h} {x - w} {y + h * 0.6} {x} {y} Z',
                TEAR, 'stroke-width="4"')


def sweat(x, y):
    return tear(x, y, 0.9) + path(f'M {x - 2} {y + 9} L {x - 2} {y + 13}', 'none', f'stroke="{CREAM}" stroke-width="3"')


def vein(x, y, s=1.0):
    """The cartoon cross-shaped anger mark."""
    k = 9 * s
    arcs = ''
    for dx, dy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
        cx, cy = x + dx * k, y + dy * k
        arcs += path(f'M {cx - dx * k * 0.2} {cy + dy * k * 0.9} Q {cx - dx * k * 0.1} {cy - dy * 0.1} {cx + dx * k * 0.9} {cy - dy * k * 0.2}',
                     'none', f'stroke="{BERRY}" stroke-width="5"')
    return arcs


# --- Mouths ------------------------------------------------------------------

def frown(mx, my, w, depth=12, width=7):
    return path(f'M {mx - w / 2} {my + depth / 2} Q {mx} {my - depth} {mx + w / 2} {my + depth / 2}', 'none', f'stroke-width="{width}"')


def wobble(mx, my, w, amp=5):
    step = w / 4
    x = mx - w / 2
    d = f'M {x} {my + amp}'
    for i in range(4):
        d += f' Q {x + step * (i + 0.5)} {my + (-amp if i % 2 == 0 else amp) * 1.6} {x + step * (i + 1)} {my + (amp if i % 2 == 0 else -amp) * 0.2}'
    return path(d, 'none', 'stroke-width="6"')


def gritted(mx, my, w, h=20):
    """Clenched teeth: an ink slot with a row of cream teeth."""
    x0, y0 = mx - w / 2, my - h / 2
    s = f'<rect x="{x0}" y="{y0}" width="{w}" height="{h}" rx="{h / 2}" fill="{CREAM}" stroke-width="6"/>'
    s += path(f'M {x0 + 3} {my} L {x0 + w - 3} {my}', 'none', 'stroke-width="4"')
    for i in range(1, 4):
        tx = x0 + w * i / 4
        s += path(f'M {tx} {y0 + 2} L {tx} {y0 + h - 2}', 'none', 'stroke-width="4"')
    return s


def snarl(mx, my, w):
    """Lip curled up on one side, one fang showing."""
    s = path(f'M {mx - w / 2} {my + 6} Q {mx - w * 0.1} {my - 4} {mx + w / 2} {my - 10}', 'none', 'stroke-width="7"')
    s += path(f'M {mx + w * 0.2} {my - 5} L {mx + w * 0.28} {my + 7} L {mx + w * 0.36} {my - 7} Z', CREAM, 'stroke-width="4"')
    return s


def shout(mx, my, w, h):
    """Mouth wide open, yelling: wider at the bottom, tongue showing."""
    s = path(f'M {mx - w * 0.38} {my - h / 2} L {mx + w * 0.38} {my - h / 2} Q {mx + w / 2} {my + h / 2} {mx} {my + h / 2} Q {mx - w / 2} {my + h / 2} {mx - w * 0.38} {my - h / 2} Z', INK)
    s += path(f'M {mx - w * 0.3} {my - h / 2 + 3} L {mx + w * 0.3} {my - h / 2 + 3} L {mx + w * 0.26} {my - h / 2 + 10} L {mx - w * 0.26} {my - h / 2 + 10} Z', CREAM, 'stroke="none"')
    s += f'<ellipse cx="{mx}" cy="{my + h * 0.3}" rx="{w * 0.2}" ry="{h * 0.15}" fill="{BERRY}" stroke="none"/>'
    return s


def wail(mx, my, w, h):
    """A big crying mouth: open, turned down at the corners."""
    s = path(f'M {mx - w / 2} {my + h * 0.35} Q {mx} {my - h * 0.75} {mx + w / 2} {my + h * 0.35} Q {mx} {my + h * 0.55} {mx - w / 2} {my + h * 0.35} Z', INK)
    s += f'<ellipse cx="{mx}" cy="{my + h * 0.2}" rx="{w * 0.18}" ry="{h * 0.14}" fill="{BERRY}" stroke="none"/>'
    return s


def grimace(mx, my, w, h):
    """Teeth bared in a frown: a band of teeth that turns down at both corners,
    so it can never be mistaken for a grin."""
    def quad(t, a, c):
        return (1 - t) ** 2 * a + 2 * t * (1 - t) * c + t ** 2 * a
    corner = my + h * 0.45
    top, bottom, middle = my - h * 0.95, my + h * 0.35, my - h * 0.3
    x0, x1 = mx - w / 2, mx + w / 2
    s = path(f'M {x0} {corner} Q {mx} {top} {x1} {corner} Q {mx} {bottom} {x0} {corner} Z', CREAM, 'stroke-width="6"')
    s += path(f'M {x0 + 4} {corner - 1} Q {mx} {middle} {x1 - 4} {corner - 1}', 'none', 'stroke-width="3.5"')
    for dx in (-0.2, 0, 0.2):
        t = 0.5 + dx
        tx = mx + dx * w
        s += path(f'M {tx:.1f} {quad(t, corner, top) + 2:.1f} L {tx:.1f} {quad(t, corner, bottom) - 2:.1f}', 'none', 'stroke-width="3.5"')
    return s


def o_mouth(mx, my, r):
    return f'<ellipse cx="{mx}" cy="{my}" rx="{r * 0.8}" ry="{r}" fill="{INK}" stroke-width="5"/>' + \
        f'<ellipse cx="{mx}" cy="{my + r * 0.35}" rx="{r * 0.45}" ry="{r * 0.35}" fill="{BERRY}" stroke="none"/>'


def pout(mx, my, w):
    s = frown(mx, my, w, 14, 8)
    s += path(f'M {mx - w * 0.24} {my + 11} Q {mx} {my + 21} {mx + w * 0.24} {my + 11}', 'none', 'stroke-width="6"')
    return s


# --- Enemies -----------------------------------------------------------------

DRIFT = '#BC7F83'
drifter = path('M 37 88 L 72 42 L 145 28 L 207 59 L 229 126 L 203 192 L 139 222 L 64 204 L 26 147 Z', DRIFT)
drifter += path('M 47 158 Q 111 220 213 150 L 202 191 L 139 215 L 64 196 Z', '#945768', 'stroke="none"')


def craters(spots, colour):
    return ''.join(circle(x, y, r, colour) for x, y, r in spots)


# Glum: eyes drooping at the outer corners, looking down, a plain frown.
svg('body_drifter', drifter + craters([(69, 99, 18), (190, 76, 12)], '#945768')
    + eye(113, 118, 13, (0, 6)) + lid(113, 118, 13, 2, -8, DRIFT)
    + eye(159, 114, 13, (0, 6)) + lid(159, 114, 13, -8, 2, DRIFT)
    + brow(98, 90, 122, 84, -2) + brow(150, 80, 174, 86, -2)
    + frown(136, 160, 34, 12))
# Teary: big wet eyes, a tear rolling, wobbly mouth.
svg('body_drifter_2', drifter + craters([(194, 84, 14), (62, 146, 11)], '#945768')
    + eye(113, 116, 14, (1, 5), 0.6) + eye(159, 112, 14, (1, 5), 0.6)
    + brow(97, 88, 121, 80, -3) + brow(151, 76, 175, 84, -3)
    + tear(101, 136) + wobble(136, 160, 34, 4))
# Sulking: half-shut eyes turned away, mouth pushed to one side.
svg('body_drifter_3', drifter + craters([(92, 60, 11), (200, 132, 16), (58, 124, 9)], '#945768')
    + eye(113, 116, 13, (-5, 5)) + lid(113, 116, 13, -1, -1, DRIFT)
    + eye(159, 112, 13, (-5, 5)) + lid(159, 112, 13, -1, -1, DRIFT)
    + path('M 120 162 Q 138 154 156 164', 'none', 'stroke-width="7"'))

SHARD = '#EAA36F'
shard = path('M 19 160 Q 8 119 38 85 L 109 34 Q 139 18 160 52 L 230 174 Q 242 205 204 216 L 67 215 Z', SHARD)
shard += path('M 21 169 L 67 211 L 203 210 L 160 180 Z', '#C46E68', 'stroke="none"')
shard += path('M 53 89 L 75 70', 'none', f'stroke="{CREAM}"')
angry_eyes = (eye(107, 126, 13, (2, 5)) + lid(107, 126, 13, -9, 1, SHARD)
              + eye(156, 123, 13, (-2, 5)) + lid(156, 123, 13, 1, -9, SHARD))
svg('body_shard', shard + angry_eyes + gritted(131, 166, 40, 18))
svg('body_shard_2', shard + angry_eyes + snarl(131, 166, 40))

PLAN = '#958BBC'
plan = circle(128, 130, 108, PLAN)
plan += path('M 27 148 Q 135 222 228 138 Q 218 228 127 237 Q 45 220 27 148', '#71668F', 'stroke="none"')
# Grumpy: heavy lids half down, a long flat mouth that dips at one end.
svg('body_planetoid', plan + craters([(64, 84, 20), (188, 184, 22)], '#71668F')
    + eye(104, 121, 16, (1, 7)) + lid(104, 121, 16, 1, 3, PLAN, 7)
    + eye(161, 121, 16, (-1, 7)) + lid(161, 121, 16, 3, 1, PLAN, 7)
    + brow(92, 94, 122, 99, 0, 7) + brow(143, 99, 173, 94, 0, 7)
    + path('M 102 170 L 144 169 Q 151 169 154 176', 'none', 'stroke-width="7"'))
# Fed up: rolling its eyes, one brow cocked, a short tight "hmph" of a mouth.
svg('body_planetoid_2', plan + craters([(192, 74, 16), (56, 150, 17), (150, 208, 10)], '#71668F')
    + eye(104, 121, 16, (5, -9)) + eye(161, 121, 16, (5, -9))
    + brow(88, 94, 120, 92, 0, 7) + brow(145, 88, 177, 80, 6, 7)
    + path('M 112 172 L 146 170', 'none', 'stroke-width="7"')
    + path('M 150 164 Q 158 170 150 176', 'none', 'stroke-width="5"'))

FRACT = '#88BFB7'
fract = path('M 32 85 L 91 30 L 173 36 L 228 99 L 220 170 L 163 225 L 75 214 L 25 153 Z', FRACT)
fract += path('M 168 36 L 160 74 L 186 96 L 172 126 L 204 146 L 223 156', 'none', 'stroke-width="12"')
# Worried: brows up in the middle, one wobbly mouth, sweating.
svg('body_fracture', fract
    + eye(88, 114, 14, (1, 2), 0.5) + eye(136, 114, 14, (-1, 2), 0.5)
    + brow(72, 86, 100, 78, 4) + brow(124, 78, 152, 86, 4)
    + wobble(112, 160, 36, 4)
    + sweat(56, 84))
# Scared (the Splinters): wide eyes, pin-prick pupils, a little "o" mouth.
svg('body_fracture_mini', fract
    + eye(88, 112, 16, (0, 0), 0.3) + eye(136, 112, 16, (0, 0), 0.3)
    + brow(72, 80, 102, 72, 6) + brow(122, 72, 152, 80, 6)
    + o_mouth(112, 162, 12))
# Bawling (a Splinter): eyes squeezed shut, tears, a big wailing mouth.
svg('body_fracture_mini_2', fract
    + squeezed(88, 114, 16, 1) + squeezed(136, 114, 16, -1)
    + brow(72, 86, 102, 78, 4) + brow(122, 78, 152, 86, 4)
    + wail(112, 166, 46, 30)
    + tear(68, 128) + tear(156, 128))
# Yikes (a Splinter): eyes darting sideways, teeth gritted, sweating.
svg('body_fracture_mini_3', fract
    + eye(88, 110, 16, (7, 0), 0.4) + eye(136, 110, 16, (7, 0), 0.4)
    + brow(72, 82, 102, 75, 5) + brow(122, 75, 152, 82, 5)
    + grimace(112, 162, 50, 22)
    + sweat(60, 70))

pts = []
import math
for i in range(24):
    a = math.tau * i / 24
    r = 115 if i % 2 == 0 else 88
    pts.append(f'{128 + math.cos(a) * r:.1f},{128 + math.sin(a) * r:.1f}')
flare = f'<polygon points="{" ".join(pts)}" fill="#E7876F"/>' + circle(128, 128, 75, ORANGE)
flare += path('M 65 121 Q 71 85 99 76', 'none', f'stroke="{CREAM}" stroke-width="7"')
# Furious: glaring down hard, yelling, with an anger mark.
svg('body_flare', flare
    + eye(105, 118, 13, (3, 5)) + lid(105, 118, 13, -10, 3, ORANGE, 7)
    + eye(151, 118, 13, (-3, 5)) + lid(151, 118, 13, 3, -10, ORANGE, 7)
    + shout(128, 163, 42, 26) + vein(170, 82, 0.9))

BUL = '#769CB4'; BUL_SHADE = '#52758F'
bul = path('M 37 61 Q 127 5 222 63 L 215 165 Q 169 224 125 239 Q 77 226 36 165 Z', BUL)
bul += path('M 132 26 L 218 67 L 210 162 Q 173 211 132 231 Z', BUL_SHADE, 'stroke="none"')
bul += path('M 56 76 Q 130 40 200 78', 'none', f'stroke="{CREAM}" stroke-width="10"')
# Stubborn: thick flat brows pressed right down, pouting.
svg('body_bulwark', bul
    + eye(99, 122, 13, (2, 4)) + lid(99, 122, 13, -3, -3, BUL, 9)
    + eye(154, 122, 13, (-2, 4)) + lid(154, 122, 13, -3, -3, BUL_SHADE, 9)
    + pout(127, 170, 40))

# --- Bosses ------------------------------------------------------------------

coil = ''
for a in (0, 90, 180, 270):
    coil += f'<g transform="rotate({a} 128 128)">' + path('M 107 76 Q 51 15 34 46 Q 17 77 85 111', BERRY) + circle(42, 53, 12, ORANGE) + '</g>'
COIL = '#F0CBE0'
coil += circle(128, 128, 71, '#C69AC9') + circle(128, 128, 51, COIL)
# Sneering: one eye narrowed, one brow cocked up, lip curled with a fang.
svg('boss_coil', coil
    + eye(109, 118, 12, (3, 3)) + lid(109, 118, 12, -2, -2, COIL)
    + eye(149, 116, 12, (-2, 3)) + brow(137, 94, 161, 92, 8)
    + snarl(129, 154, 34))

BROOD = '#A8C596'
brood = ''.join(circle(x, y, r, '#84B0A0') for x, y, r in [(49, 77, 30), (207, 67, 29), (211, 191, 28), (43, 185, 33)])
brood += path('M 52 91 Q 50 36 126 29 Q 211 41 212 124 Q 224 207 136 230 Q 45 227 40 149 Z', BROOD)
brood += path('M 48 163 Q 128 218 206 155 Q 193 223 127 226 Q 69 219 48 163', '#769F8C', 'stroke="none"')
brood += circle(63, 132, 11, '#769F8C', 'none') + circle(189, 134, 14, '#769F8C', 'none')
# Wailing: eyes screwed shut, tears pouring, mouth wide open.
svg('boss_brood', brood
    + squeezed(101, 110, 18, 1) + squeezed(158, 110, 18, -1)
    + brow(80, 84, 116, 78, 5) + brow(142, 78, 178, 84, 5)
    + tear(86, 128, 1.2) + tear(173, 128, 1.2)
    + wail(129, 172, 64, 40))

# The Black Hole keeps its eclipse look; the face sits on the dark core, glaring.
hole = f'<g transform="rotate(-18 128 128)">'
hole += f'<ellipse cx="128" cy="134" rx="119" ry="44" fill="{BERRY}" stroke-width="7"/>'
hole += f'<ellipse cx="128" cy="128" rx="108" ry="32" fill="{ORANGE}" stroke="none"/>'
hole += circle(128, 117, 69, '#100f21', None, 'stroke-width="9"')
hole += path('M 67 107A63 63 0 0 1 188 96', 'none', f'stroke="{CREAM}" stroke-width="9"')
hole += path(f'M 16 131Q55 179 128 173Q209 171 240 132Q200 151 127 153Q53 155 16 131Z', ORANGE, 'stroke-width="5"')
hole += path('M 32 139Q129 181 224 139', 'none', f'stroke="{CREAM}" stroke-width="8"')
hole += path('M 66 193L92 199M184 49L202 57', 'none', f'stroke="{BERRY}" stroke-width="8"')
# Glowing slit eyes, slanted down toward the middle, and a jagged grimace.
hole += path('M 86 104 Q 104 94 122 112 Q 102 118 86 104 Z', ORANGE, 'stroke="none"')
hole += path('M 170 104 Q 152 94 134 112 Q 154 118 170 104 Z', ORANGE, 'stroke="none"')
hole += circle(108, 108, 4, CREAM, 'none') + circle(148, 108, 4, CREAM, 'none')
hole += path('M 98 146 L 108 136 L 118 145 L 128 135 L 138 145 L 148 136 L 158 146 Q 128 132 98 146 Z', CREAM, 'stroke="none"')
hole += '</g>'
svg('boss_black_hole', hole)

# --- The planet: the only happy face out here ------------------------------------

p_eyes = ''
for x in (99, 155):
    p_eyes += f'<ellipse cx="{x}" cy="112" rx="18" ry="23" fill="{CREAM}" stroke-width="5"/>'
    p_eyes += f'<ellipse cx="{x + 4}" cy="115" rx="9.5" ry="12.5" fill="{INK}" stroke="none"/>'
    p_eyes += circle(x + 8, 108, 4, 'white', 'none')
# Brows lifted and relaxed, cheeks pink, and a proper open smile with a tongue.
p_brows = path('M 84 80 Q 98 72 112 79', 'none', 'stroke-width="5"') + path('M 142 79 Q 156 72 170 80', 'none', 'stroke-width="5"')
p_cheeks = (f'<ellipse cx="72" cy="146" rx="15" ry="9" fill="{BLUSH}" stroke="none" opacity=".55"/>'
            f'<ellipse cx="182" cy="146" rx="15" ry="9" fill="{BLUSH}" stroke="none" opacity=".55"/>')
p_smile = path('M 97 146 Q 127 153 157 146 Q 153 178 127 179 Q 101 178 97 146 Z', INK, 'stroke-width="5"')
p_smile += path('M 111 170 Q 127 160 143 170 Q 136 177 127 177 Q 118 177 111 170 Z', BERRY, 'stroke="none"')
p_smile += path('M 104 149 Q 127 154 150 149', 'none', f'stroke="{CREAM}" stroke-width="4"')
svg('face', p_cheeks + p_eyes + p_brows + p_smile)
p_closed = path('M 83 114 Q 99 124 115 114', 'none', 'stroke-width="6"') + path('M 139 114 Q 155 124 171 114', 'none', 'stroke-width="6"')
svg('face_blink', p_cheeks + p_closed + p_brows + p_smile)
p_joy = path('M 83 118 Q 99 96 115 118', 'none', 'stroke-width="7"') + path('M 139 118 Q 155 96 171 118', 'none', 'stroke-width="7"')
p_big = path('M 92 142 Q 127 152 162 142 Q 158 186 127 187 Q 96 186 92 142 Z', INK, 'stroke-width="5"')
p_big += path('M 108 174 Q 127 162 146 174 Q 138 184 127 184 Q 116 184 108 174 Z', BERRY, 'stroke="none"')
p_big += path('M 100 146 Q 127 152 154 146', 'none', f'stroke="{CREAM}" stroke-width="4"')
svg('face_happy', p_cheeks + p_joy + p_big)


# --- One happy face per planet ------------------------------------------------
# Embertide wears the face above. Every other planet has its own good mood,
# picked to suit it, each with a blink (eyes shut, everything else the same).

GOLD = '#FFD76A'; TONGUE = '#E0677F'
EYES = (99, 155)


def p_eye(x, y=112, look=(4, 3), size=1.0, glints=1):
    """The planet's eye: big cream oval, big ink pupil, bright glints."""
    px, py = x + look[0] * size, y + look[1] * size
    e = f'<ellipse cx="{x}" cy="{y}" rx="{18 * size:.1f}" ry="{23 * size:.1f}" fill="{CREAM}" stroke-width="5"/>'
    e += f'<ellipse cx="{px:.1f}" cy="{py:.1f}" rx="{9.5 * size:.1f}" ry="{12.5 * size:.1f}" fill="{INK}" stroke="none"/>'
    e += circle(round(px + 4 * size, 1), round(py - 7 * size, 1), round(4 * size, 1), 'white', 'none')
    if glints > 1:
        e += circle(round(px - 3 * size, 1), round(py + 5 * size, 1), round(2.2 * size, 1), 'white', 'none')
    return e


def half_eye(x, y=114, look=0):
    """A relaxed, half-shut eye: only the lower half shows, under a flat lid line."""
    e = path(f'M {x - 18} {y} A 18 21 0 0 0 {x + 18} {y} Q {x} {y - 5} {x - 18} {y} Z', CREAM, 'stroke-width="5"')
    e += circle(x + look, y + 8, 8, INK, 'none') + circle(x + look + 3, y + 5, 2.6, 'white', 'none')
    e += path(f'M {x - 21} {y} Q {x} {y - 6} {x + 21} {y}', 'none', 'stroke-width="6"')
    return e


def star(cx, cy, outer, inner, points=5, fill=GOLD, extra='stroke-width="5"'):
    import math
    pts = []
    for i in range(points * 2):
        r = outer if i % 2 == 0 else inner
        a = -math.pi / 2 + i * math.pi / points
        pts.append(f'{cx + r * math.cos(a):.1f} {cy + r * math.sin(a):.1f}')
    return path('M ' + ' L '.join(pts) + ' Z', fill, extra)


shut = p_closed                                   # content, eyes closed
arcs = p_joy                                      # ^ ^
brows_up = path('M 82 74 Q 98 64 114 72', 'none', 'stroke-width="5"') + path('M 140 72 Q 156 64 172 74', 'none', 'stroke-width="5"')
open_smile = p_smile
small_smile = path('M 108 152 Q 127 166 146 152', 'none', 'stroke-width="6"')

faces = {}

# 2 Driftlight: calm and content, eyes closed, a small soft smile.
faces[2] = (p_cheeks + shut + p_brows + small_smile, None)

# 3 Palefrost: shy, glancing away, cheeks glowing.
shy_cheeks = ''
for cx in (70, 184):
    shy_cheeks += f'<ellipse cx="{cx}" cy="146" rx="20" ry="11" fill="{BLUSH}" stroke="none" opacity=".75"/>'
    for dx in (-8, 0, 8):
        shy_cheeks += path(f'M {cx + dx + 3} {140} L {cx + dx - 3} {151}', 'none', f'stroke="{BERRY}" stroke-width="2.5" opacity=".7"')
shy_mouth = path('M 108 155 Q 121 164 136 154', 'none', 'stroke-width="5"')
shy_brows = path('M 86 82 Q 98 76 111 80', 'none', 'stroke-width="5"') + path('M 143 80 Q 156 76 168 82', 'none', 'stroke-width="5"')
faces[3] = (shy_cheeks + p_eye(99, look=(-6, 5)) + p_eye(155, look=(-6, 5)) + shy_brows + shy_mouth,
            shy_cheeks + shut + shy_brows + shy_mouth)

# 4 Cinderbloom: proud, a wide toothy grin.
grin = path('M 88 144 Q 127 156 166 144 Q 160 180 127 182 Q 94 180 88 144 Z', INK, 'stroke-width="5"')
grin += path('M 95 149 Q 127 159 159 149 L 156 162 Q 127 170 98 162 Z', CREAM, 'stroke="none"')
grin += path('M 112 155 L 112 166 M 127 157 L 127 169 M 142 155 L 142 166', 'none', 'stroke-width="3"')
faces[4] = (p_cheeks + p_eye(99) + p_eye(155) + brows_up + grin, p_cheeks + shut + brows_up + grin)

# 5 Hollowmere: mischievous, eyes half shut and sliding sideways, a sly grin.
sly_brows = path('M 84 90 L 113 87', 'none', 'stroke-width="5"') + path('M 141 80 Q 156 68 171 77', 'none', 'stroke-width="5"')
sly = path('M 96 148 Q 132 160 164 140 Q 152 178 120 174 Q 102 168 96 148 Z', INK, 'stroke-width="5"')
sly += path('M 104 152 Q 132 160 156 147', 'none', f'stroke="{CREAM}" stroke-width="4"')
sly_shut = path('M 80 116 Q 99 124 118 116', 'none', 'stroke-width="6"') + path('M 136 116 Q 155 124 174 116', 'none', 'stroke-width="6"')
faces[5] = (p_cheeks + half_eye(99, look=7) + half_eye(155, look=7) + sly_brows + sly, p_cheeks + sly_shut + sly_brows + sly)

# 6 Duskwarden: confident and ready, firm brows and a broad closed smile.
firm_brows = path('M 83 80 L 113 80', 'none', 'stroke-width="7"') + path('M 141 80 L 171 80', 'none', 'stroke-width="7"')
broad = path('M 90 148 Q 127 176 164 148', 'none', 'stroke-width="7"')
broad += path('M 85 143 Q 87 149 93 152 M 169 143 Q 167 149 161 152', 'none', 'stroke-width="4"')
faces[6] = (p_cheeks + p_eye(99, look=(0, 3)) + p_eye(155, look=(0, 3)) + firm_brows + broad, p_cheeks + shut + firm_brows + broad)

# 7 Verdant Halo: starry-eyed.
starry = star(99, 113, 24, 11) + star(155, 113, 24, 11)
faces[7] = (p_cheeks + starry + brows_up + open_smile, p_cheeks + shut + brows_up + open_smile)

# 8 Ashen Coil: cool, in sunglasses, with a smirk.
shades = path('M 74 100 L 124 100 Q 124 132 100 132 Q 76 132 74 100 Z', '#1B1026', 'stroke-width="5"')
shades += path('M 130 100 L 180 100 Q 178 132 154 132 Q 130 132 130 100 Z', '#1B1026', 'stroke-width="5"')
shades += path('M 124 104 Q 127 100 130 104', 'none', 'stroke-width="5"')
shades += path('M 86 108 L 96 108 M 142 108 L 152 108', 'none', f'stroke="{CREAM}" stroke-width="4" opacity=".8"')
smirk = path('M 104 156 Q 132 166 156 146', 'none', 'stroke-width="6"') + path('M 152 142 Q 158 146 158 152', 'none', 'stroke-width="4"')
faces[8] = (p_cheeks + shades + smirk, None)

# 9 Glasswake: delighted, huge shining eyes and a small open smile.
ooh = path('M 110 150 Q 127 157 144 150 Q 142 174 127 174 Q 112 174 110 150 Z', INK, 'stroke-width="5"')
ooh += path('M 119 168 Q 127 162 135 168 Q 131 172 127 172 Q 123 172 119 168 Z', BERRY, 'stroke="none"')
faces[9] = (p_cheeks + p_eye(99, y=110, look=(1, 2), size=1.15, glints=2) + p_eye(155, y=110, look=(1, 2), size=1.15, glints=2) + brows_up + ooh,
            p_cheeks + shut + brows_up + ooh)

# 10 Moltencrown: a wink and a big grin.
wink = path('M 139 116 Q 155 102 171 116', 'none', 'stroke-width="7"')
wink_brows = path('M 84 80 Q 98 72 112 79', 'none', 'stroke-width="5"') + path('M 142 86 Q 156 80 170 86', 'none', 'stroke-width="5"')
faces[10] = (p_cheeks + p_eye(99) + wink + wink_brows + p_big, p_cheeks + shut + wink_brows + p_big)

# 11 Voidkin: playful, eyes squeezed shut, tongue out.
squint = path('M 84 104 L 112 114 L 84 124', 'none', 'stroke-width="7"') + path('M 170 104 L 142 114 L 170 124', 'none', 'stroke-width="7"')
tongue = path('M 98 148 Q 127 162 156 148', 'none', 'stroke-width="6"')
tongue += path('M 114 155 Q 114 182 128 182 Q 142 182 142 155 Q 128 161 114 155 Z', TONGUE, 'stroke-width="5"')
tongue += path('M 128 162 L 128 173', 'none', 'stroke-width="3"')
faces[11] = (p_cheeks + squint + tongue, None)

# 12 Starforged: beaming, shining eyes, a huge grin and a few sparkles.
sparkles = star(56, 88, 13, 4, 4, CREAM, 'stroke-width="3"') + star(204, 80, 10, 3, 4, CREAM, 'stroke-width="3"') + star(208, 180, 11, 3.5, 4, CREAM, 'stroke-width="3"')
beam = path('M 86 140 Q 127 154 168 140 Q 164 190 127 191 Q 90 190 86 140 Z', INK, 'stroke-width="5"')
beam += path('M 93 145 Q 127 157 161 145 L 159 155 Q 127 165 95 155 Z', CREAM, 'stroke="none"')
beam += path('M 106 178 Q 127 164 148 178 Q 138 188 127 188 Q 116 188 106 178 Z', BERRY, 'stroke="none"')
faces[12] = (p_cheeks + sparkles + p_eye(99, look=(2, 2), size=1.08, glints=2) + p_eye(155, look=(2, 2), size=1.08, glints=2) + brows_up + beam,
             p_cheeks + sparkles + shut + brows_up + beam)

for n, (look, blink) in faces.items():
    svg(f'face_{n}', look)
    svg(f'face_{n}_blink', blink or look)


# --- Drifters, built from parts ----------------------------------------------------
# The most common enemy by far, so a crowd of them must not look stamped out. In
# play a Drifter is a body (three shapes, three rock colours) with a face laid on
# top (eight unhappy moods, each with a blink), plus two reactions: scared when it
# gets close to the planet, and shocked when a neighbour pops. The parts mix
# freely, so no two Drifters in a crowd need to match. The eyelids are drawn in
# the rock's own colour, so each colour has its own set of faces.

ROCKS = {'rose': ('#BC7F83', '#945768'), 'stone': ('#A6979B', '#7A6A70'), 'clay': ('#C9A07E', '#9C7458')}
SHAPES = {
    'jagged': ('M 37 88 L 72 42 L 145 28 L 207 59 L 229 126 L 203 192 L 139 222 L 64 204 L 26 147 Z',
               [(69, 99, 18), (190, 76, 12)]),
    'pebble': ('M 24 128 Q 22 70 84 48 Q 140 30 196 52 Q 238 74 236 130 Q 234 186 178 208 Q 124 226 70 206 Q 26 186 24 128 Z',
               [(58, 112, 14), (204, 150, 11), (182, 66, 8)]),
    'lumpy': ('M 46 82 Q 44 48 80 44 Q 102 22 136 34 Q 170 22 194 48 Q 232 58 226 100 Q 244 136 222 166 Q 216 208 176 212 '
              'Q 144 234 108 220 Q 66 222 50 190 Q 20 164 32 128 Q 24 100 46 82 Z',
              [(72, 178, 12), (198, 86, 13), (56, 112, 8)]),
}
for shape, (outline, spots) in SHAPES.items():
    for rock, (base, shade) in ROCKS.items():
        clip = f'<clipPath id="c"><path d="{outline}"/></clipPath>'
        body = clip + path(outline, base)
        # The lower part of the rock in shadow, clipped to its outline.
        body += f'<g clip-path="url(#c)"><ellipse cx="140" cy="240" rx="150" ry="78" fill="{shade}" stroke="none"/></g>'
        body += path(outline, 'none')
        body += craters(spots, shade)
        svg(f'drifter_body_{shape}_{rock}', body)

worried = brow(98, 90, 122, 81, -2) + brow(150, 78, 174, 87, -2)
shut_sad = path('M 101 121 Q 113 127 125 121', 'none', 'stroke-width="6"') + path('M 147 117 Q 159 123 171 117', 'none', 'stroke-width="6"')


def moods(skin):
    """Each mood as (eyes, everything else), so the blink can swap just the eyes."""
    return {
        'glum': (eye(113, 118, 13, (0, 6)) + lid(113, 118, 13, 2, -8, skin) + eye(159, 114, 13, (0, 6)) + lid(159, 114, 13, -8, 2, skin),
                 brow(98, 90, 122, 84, -2) + brow(150, 80, 174, 86, -2) + frown(136, 160, 34, 12)),
        'teary': (eye(113, 116, 14, (1, 5), 0.6) + eye(159, 112, 14, (1, 5), 0.6),
                  brow(97, 88, 121, 80, -3) + brow(151, 76, 175, 84, -3) + tear(101, 136) + wobble(136, 160, 34, 4)),
        'sulking': (eye(113, 116, 13, (-5, 5)) + lid(113, 116, 13, -1, -1, skin) + eye(159, 112, 13, (-5, 5)) + lid(159, 112, 13, -1, -1, skin),
                    path('M 120 162 Q 138 154 156 164', 'none', 'stroke-width="7"')),
        # Fed up and tired: heavy lids, bags under the eyes, a flat line of a mouth.
        'sleepy': (eye(113, 118, 13, (0, 7)) + lid(113, 118, 13, 4, 4, skin) + eye(159, 114, 13, (0, 7)) + lid(159, 114, 13, 4, 4, skin),
                   path('M 101 141 Q 113 147 125 141', 'none', 'stroke-width="3"') + path('M 147 137 Q 159 143 171 137', 'none', 'stroke-width="3"')
                   + path('M 124 165 L 150 163', 'none', 'stroke-width="6"')),
        # Nervous: eyes darting sideways, brows up, a sweat drop, a shaky mouth.
        'nervous': (eye(113, 116, 13, (6, 2), 0.5) + eye(159, 112, 13, (6, 2), 0.5),
                    worried + sweat(188, 86) + wobble(136, 162, 28, 3)),
        # Pouting: big pleading eyes looking up, bottom lip pushed out.
        'pouty': (eye(113, 116, 14, (0, -3), 0.62) + eye(159, 112, 14, (0, -3), 0.62),
                  worried + pout(136, 158, 30)),
        # Sniffly: droopy eyes, a pink nose and a drip.
        'sniffly': (eye(113, 118, 13, (0, 5)) + lid(113, 118, 13, 0, -6, skin) + eye(159, 114, 13, (0, 5)) + lid(159, 114, 13, -6, 0, skin),
                    f'<ellipse cx="136" cy="140" rx="10" ry="7" fill="{BLUSH}" stroke="none"/>'
                    + tear(143, 145, 0.55) + frown(134, 168, 24, 8, 6)),
        # Grumbling: flat heavy brows, half-shut eyes, a wavy muttering mouth.
        'grumbling': (eye(113, 118, 13, (0, 4)) + lid(113, 118, 13, -3, -3, skin) + eye(159, 114, 13, (0, 4)) + lid(159, 114, 13, -3, -3, skin),
                      path('M 97 96 L 126 97', 'none', 'stroke-width="8"') + path('M 146 93 L 175 92', 'none', 'stroke-width="8"')
                      + wobble(136, 163, 42, 5)),
    }


for rock, (base, _) in ROCKS.items():
    for mood, (eyes, rest) in moods(base).items():
        svg(f'drifter_face_{rock}_{mood}', eyes + rest)
        svg(f'drifter_face_{rock}_{mood}_blink', shut_sad + rest)

# Reactions. Each Drifter has its own fear face (its mood's) and one shocked face of
# four, so a crowd reacting at once still reacts in different ways.
high_worry = brow(96, 84, 122, 76, -3) + brow(150, 74, 176, 82, -3)


def exclaim(x, y):
    """A cartoon "!" floating by the head, ink-rimmed so it reads on any rock."""
    bar = f'M {x + 3} {y} L {x} {y + 26}'
    return (path(bar, 'none', 'stroke-width="13"') + path(bar, 'none', f'stroke="{CREAM}" stroke-width="6"')
            + circle(x - 1, y + 40, 6.5, CREAM, None, 'stroke-width="4"'))


def jolt(lines):
    """Short strokes flying off the head: the flinch of a sudden fright."""
    return ''.join(path(d, 'none', 'stroke-width="5"') for d in lines)


def spiral(x, y, r):
    """A dizzy spiral eye."""
    import math
    points = []
    for k in range(40):
        t = k / 39 * 3 * math.pi
        rr = r * (0.12 + 0.88 * k / 39)
        points.append(f'{x + rr * math.cos(t):.1f} {y + rr * math.sin(t):.1f}')
    return (f'<circle cx="{x}" cy="{y}" r="{r + 2}" fill="{CREAM}" stroke-width="5"/>'
            + path('M ' + ' L '.join(points), 'none', 'stroke-width="4"'))


def shiny(x, y, r):
    """A huge pleading eye: big pupil, two glints."""
    return (eye(x, y, r, (0, -2), 0.7)
            + circle(round(x - r * 0.25, 1), round(y + r * 0.3, 1), round(r * 0.14, 1), CREAM, 'none'))


# Fear, close to the planet: each mood panics in its own way, so a crowd bunched
# around you never wears one face. Drawn to differ in outline at play size, not
# just in small details.
scared_faces = {
    # Glum dreads it: wide wet eyes, a big wavering frown.
    'fear_glum': eye(113, 114, 15, (0, 3), 0.3) + eye(159, 110, 15, (0, 3), 0.3) + high_worry
                 + f'<ellipse cx="113" cy="131" rx="11" ry="3.5" fill="{TEAR}" stroke="none"/>'
                 + f'<ellipse cx="159" cy="127" rx="11" ry="3.5" fill="{TEAR}" stroke="none"/>'
                 + path('M 108 174 Q 136 142 164 174 Q 150 164 136 166 Q 122 164 108 174 Z', INK, 'stroke-width="5"'),
    # Teary bawls: eyes screwed shut, a huge wail, tears everywhere.
    'fear_teary': squeezed(113, 116, 14, 1) + squeezed(159, 112, 14, -1) + high_worry
                  + wail(136, 168, 50, 34) + tear(92, 128, 1.2) + tear(182, 124, 1.2) + tear(86, 156, 0.8),
    # Sulking grimaces: eyes narrowed under low brows, teeth bared in a frown, sweating.
    'fear_sulking': eye(113, 118, 12, (0, 1), 0.42) + eye(159, 114, 12, (0, 1), 0.42)
                    + brow(97, 100, 126, 93, -2) + brow(146, 91, 175, 97, -2)
                    + grimace(140, 164, 50, 22) + sweat(190, 90),
    # Sleepy is jolted wide awake: enormous eyes, a tiny o, shock lines.
    'fear_sleepy': eye(113, 112, 18, (0, 0), 0.2) + eye(159, 108, 18, (0, 0), 0.2)
                   + brow(96, 78, 124, 70, 6) + brow(146, 68, 174, 74, 6) + o_mouth(136, 170, 7)
                   + jolt(['M 118 36 L 122 52', 'M 142 32 L 142 48', 'M 166 36 L 162 52']),
    # Nervous goes to pieces: spiral eyes, a wobbling mouth, sweat both sides.
    'fear_nervous': spiral(113, 114, 15) + spiral(159, 110, 15)
                    + wobble(136, 164, 46, 6) + sweat(192, 84) + sweat(78, 96),
    # Pouty begs: huge shiny eyes, a trembling lip, one tear.
    'fear_pouty': shiny(113, 114, 17) + shiny(159, 110, 17) + high_worry + pout(136, 162, 30)
                  + tear(98, 136, 0.8) + jolt(['M 112 186 L 116 182', 'M 160 186 L 156 182']),
    # Sniffly screams: wide eyes with pin-prick pupils, a howling mouth, a runny tear.
    'fear_sniffly': eye(113, 114, 15, (0, 0), 0.22) + eye(159, 110, 15, (0, 0), 0.22) + high_worry
                    + wail(136, 170, 46, 40) + tear(94, 132, 0.9)
                    + jolt(['M 118 38 L 122 54', 'M 154 34 L 152 50']),
    # Grumbling clenches: brows down hard, eyes wide, teeth bared in a frown, fuming.
    'fear_grumbling': eye(113, 116, 14, (0, 0), 0.35) + eye(159, 112, 14, (0, 0), 0.35)
                      + path('M 96 94 L 126 100', 'none', 'stroke-width="8"') + path('M 146 98 L 176 90', 'none', 'stroke-width="8"')
                      + grimace(136, 166, 58, 26) + vein(190, 70, 0.8),
}
shocked_faces = {
    # Gasp: brows flying up, round eyes, mouth a big O.
    'shocked': eye(113, 116, 15, (0, 0), 0.34) + eye(159, 112, 15, (0, 0), 0.34)
               + brow(98, 80, 124, 74, 6) + brow(148, 72, 174, 78, 6) + o_mouth(136, 164, 13),
    # Jaw drop: pin-prick pupils and a long open mouth.
    'shocked_2': eye(113, 112, 14, (0, 0), 0.2) + eye(159, 108, 14, (0, 0), 0.2)
                 + brow(98, 76, 124, 70, 8) + brow(148, 68, 174, 74, 8)
                 + f'<ellipse cx="136" cy="168" rx="11" ry="20" fill="{INK}" stroke-width="5"/>'
                 + f'<ellipse cx="136" cy="178" rx="6" ry="6" fill="{BERRY}" stroke="none"/>',
    # "!": one eye popping, a small gasp, an exclamation mark by the head.
    'shocked_3': eye(113, 116, 17, (0, -2), 0.3) + eye(159, 113, 11, (0, -2), 0.35)
                 + brow(94, 82, 124, 70, 6) + brow(150, 88, 172, 86, 0)
                 + o_mouth(130, 164, 9) + exclaim(204, 26),
    # Flinch: eyes clamped shut and drooping, brows pinched up, a wobbling frown, jolt lines.
    'shocked_4': path('M 100 122 L 126 113', 'none', 'stroke-width="7"') + path('M 146 109 L 172 118', 'none', 'stroke-width="7"')
                 + high_worry + wobble(136, 166, 38, 5)
                 + jolt(['M 62 58 L 72 72', 'M 46 84 L 62 90', 'M 86 38 L 90 54']),
}
for name, face in {**scared_faces, **shocked_faces}.items():
    svg(f'drifter_face_{name}', face)

print('Wrote 14 planet faces, 12 enemy faces, 3 boss faces, 9 Drifter bodies and 60 Drifter faces')
