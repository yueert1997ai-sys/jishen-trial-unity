"""Read a public Sword Impulse gameplay reference; no browser profile or login."""
import json, pathlib, urllib.request
OUT = pathlib.Path(__file__).resolve().parents[1] / 'AuditEvidence/motion-v2/reference'
OUT.mkdir(parents=True, exist_ok=True)
url='https://www.youtube.com/watch?v=rllR6FWAVgM'
req=urllib.request.Request(url,headers={'User-Agent':'Mozilla/5.0'})
with urllib.request.urlopen(req,timeout=25) as response: html=response.read().decode()
(OUT/'youtube-page.html').write_text(html,encoding='utf-8')
for key in ['var ytInitialPlayerResponse = ', 'ytInitialPlayerResponse = ']:
 if key in html:
  data=json.JSONDecoder().raw_decode(html.split(key,1)[1])[0]
  (OUT/'player.json').write_text(json.dumps(data),encoding='utf-8')
  print(json.dumps({k:data.get(k) for k in ['playabilityStatus','videoDetails','storyboards']},ensure_ascii=True)[:5000])
  break
else: print('Public page has no player metadata. bytes=',len(html))
