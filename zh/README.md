# DG-Sinmai-Shock-Mod

这是一个基于 **MelonLoader** 的给八按键音游机使用的Mod

大体功能就是当在游戏里Miss，会自动发送指定的Json内容到某个Websocket服务器

配置自己填写，保证电击强度（？）


## 使用

1. 去Release下载构建好的zip覆盖你的游戏文件

2. 修改在 `sinmai.exe` 目录下的 `DGShockMod.toml` 文件，示例如下：


```toml
# WebSocket 连接地址
ws_url = "ws://192.168.1.100:60536/v1"

# Miss 发生时发送的 JSON 内容
send_content = '''
{
  "cmd": "set_pattern",
  "pattern_name": "经典",
  "intensity": 100,
  "ticks": 1
}
'''
```
Json内容参考[open-DGLAB-controller](https://github.com/open-toys-controller/open-DGLAB-controller)


## Build
你需要使用websocket-sharp.dll，请自行编译

---

## 注意事项

- **WebSocket 依赖**：该 Mod 使用 [websocket-sharp](https://github.com/sta/websocket-sharp)，必须在构建中正确引用 `websocket-sharp.dll`。
- **配置文件**：如果配置文件缺失，Mod 会警告但不崩溃；但不会连接 WebSocket，也不会发送消息。
- **重连机制**：如果 WebSocket 连接断开，Mod 会自动尝试重新连接。
