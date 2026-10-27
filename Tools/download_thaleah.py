"""Download the unmodified original font from Tiny Worlds' own distribution."""
import io, json, re, urllib.request, urllib.parse, zipfile, http.cookiejar
from pathlib import Path

base = 'https://tinyworlds.itch.io/free-pixel-font-thaleah'
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
page = opener.open(base, timeout=40).read().decode()
csrf = re.search(r'<meta[^>]+name="csrf_token"[^>]+(?:content|value)="([^"]+)', page)
if not csrf:
    csrf = re.search(r'"csrf_token":"([^"]+)', page)
if not csrf:
    raise RuntimeError('Download form not found')
data = urllib.parse.urlencode({'csrf_token': urllib.parse.unquote(csrf[1]), 'upload_id': '1281104'}).encode()
request = urllib.request.Request(base + '/file/1281104', data=data, headers={'Referer': base, 'X-Requested-With':'XMLHttpRequest'})
response = json.loads(opener.open(request, timeout=40).read())
archive = zipfile.ZipFile(io.BytesIO(opener.open(response['url'], timeout=40).read()))
out = Path('Assets/_Trapple/UI/Storybook/Fonts'); out.mkdir(parents=True, exist_ok=True)
for name in archive.namelist():
    if name.lower().endswith(('.ttf','.txt')):
        (out/Path(name).name).write_bytes(archive.read(name))
print('Original Thaleah archive extracted:', ', '.join(archive.namelist()))
