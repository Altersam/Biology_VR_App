// Start the input actor immediately after Play, before study cells drift far away.
const {execFileSync}=require('child_process');
const path=require('path');
const root=path.resolve(__dirname,'..');
const helper=path.join(__dirname,'unity_ws_call.cjs');
const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
function call(method,file){return JSON.parse(execFileSync(process.execPath,[helper,method,...(file?[path.join(__dirname,file)]:[])],{cwd:root,encoding:'utf8',timeout:100000}));}
(async()=>{
  call('set_play_mode_status','stop_ws.json');
  for(let i=0;i<20;i++){await delay(750);if(!call('get_play_mode_status').result.isPlaying)break;}
  try{call('set_play_mode_status','play_ws.json');}catch(error){/* Domain reload interrupts the transport. */}
  let ready=false;
  for(let i=0;i<40;i++){
    await delay(750);
    try{const state=call('get_play_mode_status').result;if(state.isPlaying){if(state.isPaused)call('set_play_mode_status','play_ws.json');ready=true;break;}}catch(error){}
  }
  if(!ready)throw new Error('Unity did not enter Play Mode');
  await delay(500);
  console.log(JSON.stringify(call('execute_menu_item',process.argv[2]||'cursor_pc_test_ws.json'),null,2));
})().catch(error=>{console.error(error.message);process.exitCode=1;});
