// Authenticated direct transport to the already-running Unity MCP bridge.
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
const cache = path.join(root, 'Library/PackageCache');
const packageName = fs.readdirSync(cache).find(name => name.startsWith('com.gamelovers.mcp-unity@'));
if (!packageName) throw new Error('Unity MCP package is not restored; wait for Package Manager.');
const WebSocket = require(path.join(cache, packageName, 'Server~/node_modules/ws'));
const token = fs.readFileSync(path.join(root, 'Library/McpUnity/bridge-token'), 'utf8').trim();
const settings = JSON.parse(fs.readFileSync(path.join(root, 'ProjectSettings/McpUnitySettings.json'), 'utf8').replace(/^\uFEFF/, ''));
const socket = new WebSocket(`ws://127.0.0.1:${settings.Port || 8090}/McpUnity`, {
  headers: { Authorization: 'Basic ' + Buffer.from('mcp-unity:' + token).toString('base64') }
});
const args = process.argv[3] ? JSON.parse(fs.readFileSync(process.argv[3], 'utf8').replace(/^\uFEFF/, '')) : {};
const timeout = process.argv[2] === 'set_play_mode_status' ? 15000 : 90000;
const timer = setTimeout(() => { console.error('Unity MCP response interrupted; verify editor state before retrying'); socket.terminate(); process.exitCode = 2; }, timeout);
socket.on('open', () => socket.send(JSON.stringify({jsonrpc: '2.0', id: 'biology-art-pass', method: process.argv[2], params: args})));
socket.on('message', data => {
  const result = JSON.parse(data.toString());
  console.log(JSON.stringify(result, null, 2));
  clearTimeout(timer);socket.close();
  if(result.error || result.result?.error || result.result?.success === false) process.exitCode = 2;
});
socket.on('error', error => { clearTimeout(timer);console.error('Unity MCP transport failed: '+error.message);process.exitCode = 2; });
