# DG-Sinmai-Shock-Mod

English | [简体中文](https://github.com/Cainongw/dg-sinmai-shock-mod/tree/main/zh)

This is a **MelonLoader**-based mod designed for an 8-key rhythm game cabinet.

The main function is: when a Miss happens in the game, it will automatically send a specified JSON message to a given WebSocket server.

The configuration is up to you — ensure the shock intensity (or whatever else) is set properly!

---

## Usage

1. Download the built zip from the Releases page and overwrite your game files.

2. Edit the `DGShockMod.toml` file located in the same directory as `sinmai.exe`, for example:

```toml
# WebSocket connection URL
ws_url = "ws://192.168.1.100:60536/v1"

# JSON content to send when a Miss happens
send_content = '''
{
  "cmd": "set_pattern",
  "pattern_name": "classic",
  "intensity": 100,
  "ticks": 1
}
'''
```
For reference on JSON content, see [open-DGLAB-controller](https://github.com/open-toys-controller/open-DGLAB-controller).

---

## Build

You will need to include `websocket-sharp.dll` as a dependency — compile it yourself as needed.

---

## Notes

- **WebSocket Dependency**: This mod uses [websocket-sharp](https://github.com/sta/websocket-sharp). Be sure to properly reference `websocket-sharp.dll` when building.
- **Config File**: If the config file is missing, the mod will warn you but not crash. It will not connect to the WebSocket or send messages.
- **Reconnect**: If the WebSocket connection is lost, the mod will automatically try to reconnect.

---

Happy modding! 🍻
