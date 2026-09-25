"""Original GRINFINITY arcade sounds; deterministic synthesis, no sampled assets."""
from pathlib import Path
import math, random, wave, array

RATE = 22050
OUT = Path(__file__).resolve().parent.parent / 'sounds'
rng = random.Random(7301)

def write(name, data):
    samples = array.array('h', (int(max(-0.85, min(0.85, x)) * 32767) for x in data))
    with wave.open(str(OUT / (name + '.wav')), 'wb') as f:
        f.setnchannels(1); f.setsampwidth(2); f.setframerate(RATE); f.writeframes(samples.tobytes())

def note(data, start, duration, midi, volume, kind='bell'):
    freq = 440 * 2 ** ((midi - 69) / 12)
    for i in range(int(duration * RATE)):
        at = int(start * RATE) + i
        if at >= len(data): break
        t = i / RATE
        env = min(t / .008, 1) * max(0, 1 - t / duration) ** 2
        phase = 2 * math.pi * freq * t
        tone = math.sin(phase) + (0.22 * math.sin(phase * 2) if kind == 'bell' else .1 * math.sin(phase * 3))
        data[at] += tone * env * volume

for name, pitches, duration in [
    ('upgrade_chirp', [72,76,79,84], .4), ('pickup_chirp',[76,83],.22),
    ('button_tap',[76,79],.12), ('button_tick',[79],.055),
    ('go_again',[79,76,72,76],.58), ('streak_chirp',[79,84,88],.3)]:
    data=[0.] * int((duration+.15)*RATE)
    for i,pitch in enumerate(pitches): note(data,i*duration/len(pitches),.18,pitch,.22)
    write(name,data)

for name,start,end,duration,noise in [('pew',1050,280,.11,.02),('pop',260,70,.16,.16),('shield_pop',680,180,.3,.06),('dash_swish',420,120,.18,.05),('nova_boom',150,40,.42,.12)]:
    data=[];phase=0
    for i in range(int(duration*RATE)):
        t=i/RATE; ratio=t/duration; phase+=2*math.pi*(start+(end-start)*ratio)/RATE
        env=min(t/.003,1)*(1-ratio)**2
        data.append((.3*math.sin(phase)+rng.uniform(-noise,noise))*env)
    write(name,data)

# 16 bars at 100 BPM: light bass pulse, warm chords and a sparse original melody.
beat=.6; bars=16; length=bars*4*beat
data=[0.]*int(length*RATE)
chords=[(48,60,64,67),(45,57,60,64),(53,60,65,69),(43,59,62,67)]
motifs=[[72,0,76,79,0,76,74,0],[69,0,72,76,0,72,0,71],[72,0,77,76,0,72,74,0],[71,0,74,79,0,74,72,0]]
for bar in range(bars):
    root,*chord=chords[bar%4]
    for step in range(4):
        note(data,(bar*4+step)*beat,.38,root if step%2==0 else root+7,.095,'bass')
    for pitch in chord: note(data,bar*4*beat,1.6,pitch,.035)
    for step,pitch in enumerate(motifs[bar%4]):
        if pitch and (bar%8 not in (3,7) or step%2==0): note(data,(bar*4+step*.5)*beat,.32,pitch + (12 if bar>=8 else 0),.07)
write('smile_forever',data)
print('Wrote original music loop and 11 effects.')
