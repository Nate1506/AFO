# Say It Again — Quick Research (2026-10-24)

## Environment check (evidence)
- Editor: Unity 6000.3.8f1 at `D:/New folder/6000.3.8f1/Editor/Unity.exe` (read via MCP `EditorApplication.applicationPath`).
- Project `AFO`: `ProjectVersion.txt` = 6000.3.8f1, URP 17.3.0 active (`PC_RPAsset`), build target StandaloneWindows64.
- Unity Cloud: project id `384dfe55-290c-4178-abab-6e1b18ec4440`, `projectBound=True`, user signed in.
- Live Editor reachable through the AI Game Developer MCP (`rql-query`, `script-execute`). Scripts compile (`scriptCompilationFailed=False`).
- Batch mode on the live project is blocked while the Editor is open ("Multiple Unity instances cannot open the same project"). Compile checks go through the live Editor instead.

## Packages compatible with 6000.3.8f1 (from Package Manager `Client.Search` in the live Editor)
| Package | Version | Use |
|---|---|---|
| com.unity.services.multiplayer | 2.4.1 | Sessions: create/join by code, Relay |
| com.unity.netcode.gameobjects | 2.13.3 | State sync (roles, order cards, clock) |
| com.unity.services.vivox | 16.12.1 | Voice, channels, audio taps |
| com.unity.cinemachine | 3.1.7 | Cutscene cameras |
| com.unity.timeline | 1.8.10 (installed) | Cutscene |
| com.unity.ai.navigation | 2.0.10 installed (2.0.16 available) | Villager NavMesh |
| com.unity.inputsystem | 1.18.0 installed (recommended) | FP controller |
| com.unity.localization | 1.5.13 | Optional; a simple CSV table is lighter |
| TextMeshPro | bundled in com.unity.ugui 2.0.0 | Thai font asset |

## Multiplayer stack — proposal
**Netcode for GameObjects + Multiplayer Services (Sessions with Relay).** One Unity-native stack, join-by-code built in, works behind school NAT via Relay, and the project is already linked to Unity Cloud. Mirror/Photon need extra accounts or self-hosting.
- Relay + NGO: https://docs.unity.com/relay/relay-and-ngo.html
- Sessions building block: https://docs.unity3d.com/kr/current/Manual/building-blocks-multiplayer-sessions.html

## Voice (Vivox) and radio distortion
- Vivox package manual: https://docs.unity3d.com/Manual/com.unity.services.vivox.html
- Release notes: https://docs.unity.com/vivox-unity/release-notes
- **Audio Taps**: a Participant Tap gives one remote player's voice as an AudioSource. Route it to an AudioMixer group (low/high-pass, distortion, noise) per player. Taps work only on the receiving side and only while in a channel; you cannot tap yourself.
  - https://docs.unity.com/en-us/vivox-unity/developer-guide/audio-taps/use-audio-taps
  - https://docs.unity.com/en-us/vivox-unity/developer-guide/audio-taps/audio-tap-components
- Plan: each client applies distortion locally on received voice, driven by WeatherConfig (distance falloff, noise, dropout = volume ducking bursts + static SFX, thunder masking).
- Fallback if Vivox fails: quick-chat preset phrases over NGO RPCs (already in scope), plus solo bot mode with text + radio SFX.
- Forum notes (anecdotal): sender-side effects not supported; pitch on taps is limited.
  - https://discussions.unity.com/t/adding-effects-to-microphone-data-before-being-sent-enabling-doppler-effect/1530575

## First-person + binoculars (URP)
- Input System FP controller with CharacterController; binoculars = lerp camera FOV (e.g. 60 to 15) + vignette overlay. No extra camera needed. Respect reduce-motion (instant zoom).

## Comparable games (design reference only)
- Keep Talking and Nobody Explodes — asymmetric info, only talk solves it.
- Overcooked — co-op pressure from a clock.
- Lethal Company — proximity/walkie voice as gameplay; distance and static matter.
- Our twist: a relay chain (HQ -> van -> tent -> village) with distortion and rumors; reveal shows where the message changed.

## Disaster communication and rumor facts (cite carefully)
- Systematic review (JMIR Infodemiology 2025): false messages often outpaced official communication during natural disasters. https://infodemiology.jmir.org/2025/1/e70413/PDF
- Univ. at Buffalo / NSF-funded study (Zhuang, Yang): the public struggles to tell disaster truth from rumor and rarely corrects after debunking. https://engineering.buffalo.edu/industrial-systems/news-events.host.html/content/shared/engineering/home/articles/2018-media-coverage/how-rumors-spread-on-social-media-during-weather-disasters.detail.html
- Information voids during 2024 European floods / Hurricane Helene: filling information gaps early limits misinformation. (Same JMIR source family; verify before quoting.)
- #19S Mexico earthquake misinformation case study: https://arxiv.org/pdf/2007.05848
- Framing for pitch: the game practices read-back and source verification. It does not claim to fix misinformation.

## Free assets (to verify per item before use)
- Quaternius (CC0 characters with animations): https://quaternius.com
- Kenney (CC0 models, UI, audio): https://kenney.nl/assets
- Poly Haven (CC0 textures/HDRIs): https://polyhaven.com
- Freesound (filter CC0 / CC-BY): https://freesound.org
- Google Fonts Noto Sans Thai (OFL): https://fonts.google.com/noto/specimen/Noto+Sans+Thai
- Already in project (licenses still to check): JC_StylizedNature_Lite, Pine Forest_LowPoly, Medieval Village Pack Vol.1, OldVillage, PBR_Tiled, Roadside Tales Bridges, Low Poly Boats & Raft, Oswald font (OFL.txt present).
