"""Faces for everything in the arena. Original vector artwork, no external assets.

Only the planet is happy. Every enemy wears its own bad mood, chosen to suit
how it behaves, and the most common kinds come in a few versions so a crowd of
them never looks cloned:

  Drifter    sad        (three versions: glum, teary, sulking)
  Shard      angry      (two versions: gritted teeth, snarl)
  Planetoid  grumpy     (two versions: half-shut eyes, eye-roll)
  Fracture   worried    raised brows, a wobbly mouth, a sweat drop
  Splinter   scared     wide eyes, tiny pupils, a little "o" mouth
  Satellite  suspicious one eye narrowed, looking sideways
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

SAT = '#D3ADBD'
sat = path('M 15 88 L 57 88 L 57 158 L 15 158 Z M 199 88 L 240 88 L 240 158 L 199 158 Z', '#7AAAB9')
sat += path('M 28 100 L 44 100 M 28 119 L 44 119 M 28 139 L 44 139 M 213 100 L 230 100 M 213 120 L 230 120 M 213 140 L 230 140', 'none', 'stroke-width="4"')
sat += circle(128, 126, 74, SAT) + path('M 116 51 L 105 21 M 145 51 L 155 21') + circle(103, 19, 9, ORANGE) + circle(157, 19, 9, ORANGE)
# Suspicious: one eye narrowed to a slit, both looking sideways, mouth pulled aside.
svg('body_satellite', sat
    + eye(106, 118, 13, (6, 2)) + lid(106, 118, 13, 2, 2, SAT)
    + eye(150, 116, 14, (6, 1))
    + brow(92, 98, 118, 101, 0) + brow(137, 90, 163, 86, 5)
    + path('M 110 166 Q 130 160 150 158', 'none', 'stroke-width="7"'))

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

print('Wrote 3 planet faces, 12 enemy faces and 3 boss faces')
