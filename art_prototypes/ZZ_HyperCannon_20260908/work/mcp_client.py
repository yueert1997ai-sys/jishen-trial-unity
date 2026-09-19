import argparse, asyncio, base64, json, os, tomllib
from datetime import timedelta
from pathlib import Path
from mcp import ClientSession
from mcp.client.stdio import StdioServerParameters, stdio_client

async def main():
    p=argparse.ArgumentParser()
    p.add_argument('--tool',default='get_scene_info')
    p.add_argument('--code-file')
    p.add_argument('--code')
    p.add_argument('--image')
    p.add_argument('--out')
    p.add_argument('--args',default='{}')
    a=p.parse_args()
    config=tomllib.loads(Path(r'C:\Users\yue\.codex\config.toml').read_text(encoding='utf-8'))['mcp_servers']['blender']
    params=StdioServerParameters(command=config['command'],args=config.get('args',[]),env={**os.environ,**config['env']})
    args=json.loads(a.args)
    args['user_prompt']='我很喜欢这个加农炮，zz高达的，你能在blender里面搓出来不，然后给我的机甲用。右手持用。'
    if a.code_file:
        f=Path(a.code_file).resolve()
        args['code']='__file__='+repr(str(f))+'\n'+f.read_text(encoding='utf-8')
        compile(args['code'],str(f),'exec')
        a.tool='execute_blender_code'
    elif a.code:
        args['code']=a.code
        a.tool='execute_blender_code'
    log=Path(__file__).parent/'mcp-client.log'
    with log.open('a',encoding='utf-8') as err:
        async with stdio_client(params,errlog=err) as (read,write):
            async with ClientSession(read,write,read_timeout_seconds=timedelta(seconds=180)) as session:
                await session.initialize()
                r=await session.call_tool(a.tool,args)
                texts=[]
                for c in r.content:
                    if c.type=='text':texts.append(c.text)
                    elif c.type=='image' and a.image:
                        path=Path(a.image);path.parent.mkdir(parents=True,exist_ok=True)
                        path.write_bytes(base64.b64decode(c.data))
                        texts.append('IMAGE_SAVED '+str(path))
                text='\n'.join(texts)
                if a.out:Path(a.out).write_text(text,encoding='utf-8')
                print(text[:11000],flush=True)
                if r.isError or text.startswith(('Error executing code:','Error getting scene')):
                    raise RuntimeError('MCP call failed; see output')

asyncio.run(main())
