// Uses the installed Godot MCP server with an explicitly selected .NET engine.
// Usage: node tools/godot-mcp-smoke.mjs <server-entry.js> <godot.exe> <project>
import { createRequire } from 'node:module';
const [server, godot, project] = process.argv.slice(2);
const require = createRequire(server);
const { Client } = require('@modelcontextprotocol/sdk/client/index.js');
const { StdioClientTransport } = require('@modelcontextprotocol/sdk/client/stdio.js');
const client = new Client({name:'grinfinity-release-qa', version:'1.0.0'}, {capabilities:{}});
await client.connect(new StdioClientTransport({command:process.execPath,args:[server],env:{...process.env,GODOT_PATH:godot}}));
try {
  for (const scene of ['res://scenes/menu.tscn', 'res://scenes/game.tscn']) {
    console.log(await client.callTool({name:'run_project',arguments:{projectPath:project,scene}}));
    await new Promise(resolve=>setTimeout(resolve,7000));
    console.log(JSON.stringify(await client.callTool({name:'get_debug_output',arguments:{}})));
    console.log(JSON.stringify(await client.callTool({name:'stop_project',arguments:{}})));
  }
} finally { await client.close(); }
