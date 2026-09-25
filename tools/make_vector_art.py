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
def eye(x,y,r=15):
    return f'<ellipse cx="{x}" cy="{y}" rx="{r}" ry="{r*1.3}" fill="{CREAM}" stroke-width="5"/>'+circle(x+4,y+4,r*.55,INK,'none')+circle(x+7,y,r*.19,CREAM,'none')
def face(mood='smile'):
    s=eye(99,113)+eye(155,113)
    if mood=='smile': s+=path('M 94 151 Q 129 184 163 148 Q 145 165 112 153',INK)+path('M 107 156 Q 130 165 150 153',CREAM,'stroke="none"')
    else: s+=path('M 100 163 Q 128 143 157 163', 'none','stroke-width="7"')
    return s
# Twelve cosmetic planet palettes, with separate animated face and gun.
palettes=[('#F5A451','#CB704B'),('#A8D7BF','#558F8F'),('#D9AEDE','#A173A8'),('#F3C76B','#C58B4D'),('#93C6DF','#5E8EB6'),('#E78992','#B95A72'),('#B6C982','#879755'),('#B7ACE1','#8376B7'),('#E5AF87','#BC7E68'),('#AED4D7','#699FA6'),('#EAC8D7','#B287AD'),('#F4DB9B','#CFB76D')]
for i,(base,shade) in enumerate(palettes,1):
    body=circle(129,135,98,INK,'none')+circle(125,122,98,base)
    body+=path('M 42 154 Q 112 222 211 147 Q 189 219 121 219 Q 65 212 42 154',shade,'stroke="none"')
    body+=path('M 49 99 Q 63 48 116 40','none',f'stroke="{CREAM}" stroke-width="12" opacity=".6"')
    body+=circle(76,134,16,shade,'none')+circle(174,74,11,shade,'none')+circle(174,171,20,shade,'none')
    svg(f'planet_{i}',body)
svg('face',face()+path('M 87 77 Q 100 68 110 77')+path('M 144 76 Q 155 68 167 79'))
svg('face_blink',path('M 86 115 Q 99 123 111 113')+path('M 143 113 Q 155 123 168 114')+path('M 97 151 Q 129 181 162 149'))
svg('face_happy',path('M 83 118 Q 98 91 114 115')+path('M 142 115 Q 156 91 172 118')+path('M 94 150 Q 128 199 164 150 Z',INK)+path('M 116 172 Q 132 157 146 171',BERRY,'stroke="none"'))
# Blaster faces right. Broad orange barrel, berry casing, cream muzzle.
gun=path('M 40 100 L 57 83 L 174 83 Q 184 84 185 100 L 223 100 L 223 149 L 178 149 L 162 159 L 86 159 L 65 177 L 43 161 L 53 140 L 35 137 Z',BERRY)
gun+=path('M 83 87 L 172 87 L 172 153 L 83 153 Z',ORANGE)+path('M 183 95 L 226 95 L 226 154 L 183 154 Z',CREAM)
gun+=path('M 100 96 L 147 96','none',f'stroke="{CREAM}" stroke-width="7"')+circle(121,127,12,BERRY)+path('M 196 109 L 218 109 M 196 139 L 218 139','none','stroke-width="5"')
svg('blaster',gun)
# Distinct enemy silhouettes with clear faces and intentional surface detail.
drifter=path('M 37 88 L 72 42 L 145 28 L 207 59 L 229 126 L 203 192 L 139 222 L 64 204 L 26 147 Z','#BC7F83')
drifter+=path('M 47 158 Q 111 220 213 150 L 202 191 L 139 215 L 65 196 Z','#945768','stroke="none"')+circle(69,99,18,'#945768')+circle(174,166,24,'#945768')+eye(113,116,13)+eye(159,112,13)+path('M 113 158 Q 137 148 150 162')
svg('body_drifter',drifter)
shard=path('M 19 160 Q 8 119 38 85 L 109 34 Q 139 18 160 52 L 230 174 Q 242 205 204 216 L 67 215 Z','#EAA36F')+path('M 21 169 L 67 211 L 203 210 L 160 180 Z','#C46E68','stroke="none"')+eye(107,126,13)+eye(156,123,13)+path('M 105 163 Q 128 181 153 162')+path('M 53 89 L 75 70','none',f'stroke="{CREAM}"')
svg('body_shard',shard)
plan=circle(128,130,108,'#958BBC')+path('M 27 148 Q 135 222 228 138 Q 218 228 127 237 Q 45 220 27 148','#71668F','stroke="none"')+circle(64,84,20,'#71668F')+circle(188,184,22,'#71668F')+eye(104,119,16)+eye(161,119,16)+path('M 89 95 L 122 103 M 146 103 L 179 94')+path('M 98 169 L 160 169')
svg('body_planetoid',plan)
fract=path('M 32 85 L 91 30 L 173 36 L 228 99 L 220 170 L 163 225 L 75 214 L 25 153 Z','#88BFB7')+path('M 127 35 L 111 87 L 143 111 L 115 153 L 143 182 L 129 220','none','stroke-width="12"')+eye(79,116,13)+eye(177,116,13)+path('M 68 158 Q 80 174 94 157 M 163 157 Q 179 174 191 158')
svg('body_fracture',fract);svg('body_fracture_mini',fract)
sat=path('M 15 88 L 57 88 L 57 158 L 15 158 Z M 199 88 L 240 88 L 240 158 L 199 158 Z','#7AAAB9')+path('M 28 100 L 44 100 M 28 119 L 44 119 M 28 139 L 44 139 M 213 100 L 230 100 M 213 120 L 230 120 M 213 140 L 230 140','none','stroke-width="4"')+circle(128,126,74,'#D3ADBD')+path('M 116 51 L 105 21 M 145 51 L 155 21')+circle(103,19,9,ORANGE)+circle(157,19,9,ORANGE)+eye(106,117,13)+eye(150,117,13)+path('M 97 162 Q 128 185 161 162',BERRY)
svg('body_satellite',sat)
pts=[]
for i in range(24):
 a=math.tau*i/24;r=115 if i%2==0 else 88;pts.append(f'{128+math.cos(a)*r:.1f},{128+math.sin(a)*r:.1f}')
flare=f'<polygon points="{" ".join(pts)}" fill="#E7876F"/>'+circle(128,128,75,ORANGE)+eye(105,118,13)+eye(151,118,13)+path('M 102 160 Q 129 184 155 160')+path('M 65 121 Q 71 85 99 76','none',f'stroke="{CREAM}" stroke-width="7"')
svg('body_flare',flare)
bul=path('M 37 61 Q 127 5 222 63 L 215 165 Q 169 224 125 239 Q 77 226 36 165 Z','#769CB4')+path('M 132 26 L 218 67 L 210 162 Q 173 211 132 231 Z','#52758F','stroke="none"')+path('M 56 76 Q 130 40 200 78','none',f'stroke="{CREAM}" stroke-width="10"')+eye(99,120,13)+eye(154,120,13)+path('M 82 99 L 114 107 M 140 107 L 174 99')+path('M 105 171 L 151 171')
svg('body_bulwark',bul)
# Bosses use the same line work and faces, with layered appendages.
coil=''
for a in (0,90,180,270):
 coil+=f'<g transform="rotate({a} 128 128)">'+path('M 107 76 Q 51 15 34 46 Q 17 77 85 111',BERRY)+circle(42,53,12,ORANGE)+'</g>'
coil+=circle(128,128,71,'#C69AC9')+circle(128,128,51,'#F0CBE0')+eye(109,117,12)+eye(149,117,12)+path('M 107 156 Q 129 174 152 151')
svg('boss_coil',coil)
brood=''.join(circle(x,y,r,'#84B0A0') for x,y,r in [(49,77,30),(207,67,29),(211,191,28),(43,185,33)])+path('M 52 91 Q 50 36 126 29 Q 211 41 212 124 Q 224 207 136 230 Q 45 227 40 149 Z','#A8C596')+path('M 48 163 Q 128 218 206 155 Q 193 223 127 226 Q 69 219 48 163','#769F8C','stroke="none"')+eye(101,108,20)+eye(158,108,20)+path('M 81 163 Q 127 195 176 159',BERRY)+circle(63,132,11,'#769F8C','none')+circle(189,134,14,'#769F8C','none')
svg('boss_brood',brood)
hole=path('M 20 126 Q 33 45 123 35 Q 212 20 239 103 Q 222 182 141 216 Q 45 235 20 126',BERRY)+path('M 36 151 Q 80 220 176 178 Q 235 145 216 95 Q 179 45 101 66 Q 40 82 62 131',ORANGE,'stroke-width="12"')+circle(132,128,69,INK)+eye(107,110,15)+eye(153,110,15)+path('M 94 154 Q 132 203 173 149',BERRY)+path('M 105 160 L 116 174 L 125 164 M 142 165 L 154 172 L 161 157',CREAM,'stroke="none"')
svg('boss_black_hole',hole)
svg('moon',circle(128,128,99,'#E2D8D3')+circle(75,79,21,'#B1A1B2','none')+circle(179,161,25,'#B1A1B2','none')+eye(105,122,13)+eye(153,122,13)+path('M 113 158 Q 130 174 148 157'))
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
