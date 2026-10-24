# Say It Again — Team Decisions (2026-10-24)

Deadline: **2026-10-25 12:00** (Bangkok). Feature freeze at 2026-10-25 09:00.

| Topic | Decision |
|---|---|
| Engine | Unity 6000.3.8f1, URP 17.3, 3D first-person |
| Platform | Windows 64-bit only |
| Net stack | Netcode for GameObjects 2.13.x + Multiplayer Services 2.4.1 (Session + Relay, join by room code) |
| Voice | Vivox 16.12.1, distortion applied on receive via Participant Audio Taps + AudioMixer |
| Players | 3: Van (HQ order), Tent (relay + map + limited "Ask HQ to confirm"), Village (leads villagers) |
| HQ order | 3 fixed slots: Route / Avoid / Landmark (from OrderData). "What I understood" card = pick 3 slots |
| Movement | Van: fixed at open tailgate (look only). Tent: walk inside tent. Village: free roam. Roles never see each other's avatars |
| Controls | Keyboard + mouse only. WASD move, mouse look, V push-to-talk, E interact, RMB binoculars, Tab order card, Q quick-chat, Esc pause |
| Binoculars | Zoom + landmark name labels when held on a landmark |
| Rumor shouts | Text speech bubbles + generic CC0 shout/crowd sounds. No AI/TTS |
| Cutscene | Captions only, 5 shots (~20 s): wide village + lightning, rising water, van tailgate, tent, high hill. Flood level continues into gameplay as visual storm clock |
| Animations | CC0 only (Quaternius etc.). No Mixamo |
| Palette | Navy #0E1A2B, storm grey #3A4552, warm yellow #FFC93C, alert red #E63946 |
| Flashlight | None |
| Pitch recording | OBS Studio |

## Workflow
- The live Editor (with the AI Game Developer MCP) is open on the main checkout. Code is written in the Claude worktree, committed, then fast-forwarded into `main` so the Editor picks it up.
- Batch mode cannot open the project while the Editor is open. Compile checks run through the live Editor.
