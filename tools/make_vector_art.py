"""Original vector artwork. No external assets or raster-image editing."""
from pathlib import Path
import math

OUT = Path(__file__).resolve().parents[1] / 'art' / 'cosmic'
OUT.mkdir(parents=True, exist_ok=True)
INK='#321E3D'; CREAM='#FFF0CE'; BERRY='#AB4564'; ORANGE='#F5A451'
def svg(name, body, size=256):
    (OUT/(name+'.svg')).write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="0 0 256 256"><g stroke="{INK}" stroke-width="8" stroke-linecap="round" stroke-linejoin="round">{body}</g></svg>',encoding='utf-8')
def circle(x,y,r,fill,stroke=None):
    return f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}"'+(f' stroke="{stroke}"' if stroke else '')+'/>'
def path(d,fill='none',extra=''):
    return f'<path d="{d}" fill="{fill}" {extra}/>'
# The planet bodies live in tools/refine_cosmic_art.py.
# The planet's faces live in tools/make_moods.py.
# Blaster faces right. Broad orange barrel, berry casing, cream muzzle.
gun=path('M 40 100 L 57 83 L 174 83 Q 184 84 185 100 L 223 100 L 223 149 L 178 149 L 162 159 L 86 159 L 65 177 L 43 161 L 53 140 L 35 137 Z',BERRY)
gun+=path('M 83 87 L 172 87 L 172 153 L 83 153 Z',ORANGE)+path('M 183 95 L 226 95 L 226 154 L 183 154 Z',CREAM)
gun+=path('M 100 96 L 147 96','none',f'stroke="{CREAM}" stroke-width="7"')+circle(121,127,12,BERRY)+path('M 196 109 L 218 109 M 196 139 L 218 139','none','stroke-width="5"')
svg('blaster',gun)
# Enemy and boss art, faces included, lives in tools/make_moods.py.
svg('shot',path('M 47 100 L 173 100 Q 220 100 224 128 Q 221 156 173 156 L 47 156 Q 30 128 47 100',ORANGE)+path('M 70 116 L 181 116','none',f'stroke="{CREAM}" stroke-width="12"'))
svg('hostile',path('M 128 20 Q 156 83 236 128 Q 154 167 128 236 Q 102 170 20 128 Q 103 89 128 20','#F28B7E')+circle(128,128,28,CREAM,'none'))
icons={
 'shield':path('M 58 57 Q 128 91 198 57 L 190 145 Q 166 191 128 216 Q 87 191 65 145 Z','#94CFC9')+path('M 92 124 L 117 148 L 166 99','none',f'stroke="{CREAM}" stroke-width="13"'),
 'dash':path('M 42 65 L 104 128 L 42 191 M 119 65 L 185 128 L 119 191','none',f'stroke="{ORANGE}" stroke-width="26"'),
 'rapid':path('M 141 28 L 60 142 L 119 142 L 103 226 L 199 99 L 139 99 Z',ORANGE),
 'nova':circle(128,128,65,ORANGE)+path('M 128 19 L 128 40 M 128 216 L 128 239 M 19 128 L 40 128 M 216 128 L 239 128 M 48 48 L 63 63 M 194 194 L 211 211 M 47 209 L 63 194 M 194 63 L 212 47','none',f'stroke="{CREAM}" stroke-width="12"'),
 'magnet':path('M 57 68 L 57 139 Q 57 213 128 213 Q 199 213 199 139 L 199 68 L 159 68 L 159 137 Q 159 166 128 166 Q 97 166 97 137 L 97 68 Z',BERRY)+path('M 57 69 L 97 69 M 159 69 L 199 69','none',f'stroke="{CREAM}" stroke-width="20"'),
 'freeze':path('M 128 26 L 128 230 M 39 77 L 217 179 M 39 179 L 217 77 M 106 48 L 128 71 L 151 48 M 106 207 L 128 184 L 151 207','none','stroke="#94CFC9" stroke-width="14"'),
 'spread':path('M 46 128 L 105 128 M 111 92 L 171 52 M 133 128 L 215 128 M 111 164 L 171 204','none',f'stroke="{ORANGE}" stroke-width="23"'),
 'pierce':path('M 33 128 L 207 128 M 170 86 L 212 128 L 170 170','none',f'stroke="{ORANGE}" stroke-width="17"')+path('M 90 56 L 90 201 M 133 56 L 133 201','none','stroke="#94CFC9" stroke-width="9"'),
}
for name,body in icons.items():svg('icon_'+name,body)
print(f'Created {len(list(OUT.glob("*.svg")))} original vector assets in {OUT}')
