"""Local MCP client for the user's running Unity Kitwright server.
Reads the authenticated URL from this project's existing config; never prints it.
Usage: python Tools/kitwright_call.py <tool> [arguments.json]
"""
import json
import os
import sys
import urllib.request
from pathlib import Path

root=Path(__file__).resolve().parent.parent
config=json.loads((root/'opencode.json').read_text(encoding='utf-8-sig'))
url=config['mcp']['kitwright']['url']
session=None

def request(method, params, identifier=None):
    global session
    payload={'jsonrpc':'2.0','method':method,'params':params}
    if identifier is not None: payload['id']=identifier
    headers={'Content-Type':'application/json','Accept':'application/json, text/event-stream'}
    if session: headers['Mcp-Session-Id']=session
    req=urllib.request.Request(url,json.dumps(payload,ensure_ascii=False).encode('utf-8'),headers)
    with urllib.request.urlopen(req,timeout=float(os.environ.get('BIOLOGY_MCP_TIMEOUT','240'))) as response:
        if response.headers.get('Mcp-Session-Id'): session=response.headers['Mcp-Session-Id']
        data=response.read().decode('utf-8')
        if not data: return {}
        if data.startswith('event:') or data.startswith('data:'):
            values=[line[5:].strip() for line in data.splitlines() if line.startswith('data:')]
            data=values[-1]
        return json.loads(data)

request('initialize',{'protocolVersion':'2025-03-26','capabilities':{},'clientInfo':{'name':'BiologyVR Unity Transfer','version':'1.0'}},1)
request('notifications/initialized',{})
tool=sys.argv[1]
args=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig')) if len(sys.argv)>2 else {}
result=request('tools/call',{'name':tool,'arguments':args},2)
texts=[]
for part in result.get('result',{}).get('content',[]):
    if part.get('type')=='text':
        try: texts.append(json.loads(part['text']))
        except ValueError: texts.append(part['text'])
print(json.dumps(texts or result,ensure_ascii=False,indent=2))
if result.get('error') or result.get('result',{}).get('isError'): sys.exit(2)
