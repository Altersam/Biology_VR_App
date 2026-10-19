const {execFileSync}=require('child_process');
const path=require('path');
const root=path.resolve(__dirname,'..');
const reply=JSON.parse(execFileSync(process.execPath,[path.join(__dirname,'unity_ws_call.cjs'),'get_gameobject',path.join(__dirname,process.argv[2]||'world_snapshot_ws.json')],{cwd:root,encoding:'utf8',timeout:100000}));
const components=reply.result?.gameObject?.components;
if(!components)throw new Error(JSON.stringify(reply));
for(const c of components)if(c.type===(process.argv[3]||'JourneyPlaqueChallenge')){
  const p=c.properties;
  const selected=process.argv.slice(4);
  console.log(JSON.stringify(selected.length?Object.fromEntries(selected.map(k=>[k,p[k]])):p,null,2));
}
