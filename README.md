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
# config.toml

# The backend address of DGLAB
# DO NOT use lookback address like 127.0.0.1
ws_url = "ws://192.168.0.233:9999"


# Shock Mode
# Options：single / ramp
type = "single"

# Single mode settings (Only works with type = "single")
single_intensity = 100
single_channel = "A" # A/B or Both
single_ms = 500

# Ramp mode settings (Only works with type = "Ramp")
ramp_start_intensity = 50
ramp_step = 10
ramp_max_intensity = 150  
```

3. Start the game and scan the QRCode on your screen

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
