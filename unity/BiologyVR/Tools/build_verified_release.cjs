// Sequential Unity MCP builds; verifies each artifact before preparing publication.
const fs=require('fs');
const path=require('path');
const {execFileSync}=require('child_process');
const root=path.resolve(__dirname,'..');
const reports=path.join(root,'Assets/BiologyVR/ArteryJourney/Reports');
const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
function read(name){return JSON.parse(fs.readFileSync(path.join(reports,name),'utf8').replace(/^\uFEFF/,''));}
function call(method,file){return JSON.parse(execFileSync(process.execPath,[path.join(__dirname,'unity_ws_call.cjs'),method,...(file?[path.join(__dirname,file)]:[])],{cwd:root,encoding:'utf8',stdio:['ignore','pipe','pipe'],timeout:100000}));}
function python(script){const output=execFileSync('python',[path.join(__dirname,script)],{cwd:root,encoding:'utf8',stdio:['ignore','pipe','pipe'],timeout:240000});console.log(output);}
async function ready(){for(let n=0;n<60;n++){try{const r=call('get_play_mode_status');if(r.result?.isPlaying)throw new Error('Exit Play Mode before building');if(r.result?.success)return;}catch(e){if(e.message==='Exit Play Mode before building')throw e;}await delay(2000);}throw new Error('Unity MCP is unavailable');}
async function build(menu,report){
  await ready();let before;try{before=read(report).utc;}catch{}
  const result=call('execute_menu_item',menu);if(!result.result?.success)throw new Error(JSON.stringify(result));
  console.log('Build started: '+menu);
  for(let n=0;n<180;n++){await delay(5000);let current;try{current=read(report);}catch{continue;}if(current.utc===before||current.result==='Queued')continue;if(current.result!=='Succeeded')throw new Error(JSON.stringify(current));console.log('Build succeeded: '+report);return;}
  throw new Error('Build deadline exceeded: '+report);
}
(async()=>{
  await ready();call('execute_menu_item','refresh_ws.json');await delay(12000);await ready();
  call('load_scene','reload_master_ws.json');call('execute_menu_item','offline_sdk_ws.json');
  await build('pico_build_ws.json','PicoBuildReport.json');python('verify_pico_apk.py');python('publish_local_pico_artifact.py');
  await build('quest_build_ws.json','MetaQuestBuildReport.json');python('verify_quest_apk.py');python('publish_local_quest_artifact.py');python('prepare_pico_release.py');
  await build('webgl_build_ws.json','WebGLBuildReport.json');python('webgl_browser_smoke.py');python('write_consistency_release_notes.py');python('update_app_repository.py');
  console.log('VERIFIED RELEASE PREPARATION COMPLETE');
})().catch(error=>{console.error(error.message);if(error.stderr)console.error(String(error.stderr));process.exitCode=1;});
