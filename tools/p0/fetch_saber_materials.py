"""Fetch the author's public download form without a browser or audio playback."""
from pathlib import Path
import urllib.request,urllib.parse,http.cookiejar,re,json,html
out=Path(__file__).resolve().parents[2]/'AuditEvidence/p0-audio-v5/sources'
url='https://dova-s.jp/se/detail/1086/download'
opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
opener.addheaders=[('User-Agent','Mozilla/5.0')]
for track in [1,2,3]:
    page_url=f'https://dova-s.jp/se/detail/1086/track/{track}'
    page=opener.open(page_url,timeout=30).read().decode()
    audio_url=html.unescape(re.search(r'<audio[^>]+src="([^"]+)"',page).group(1))
    req=urllib.request.Request(audio_url,headers={'Referer':page_url})
    with opener.open(req,timeout=40) as response:
        body=response.read();kind=response.headers.get('Content-Type','')
        assert 'html' not in kind, 'Download form did not return audio'
        path=out/f'dova_saber3_track{track}.mp3';path.write_bytes(body)
        print(json.dumps(dict(track=track,bytes=len(body),content_type=kind,url=response.geturl())),flush=True)
