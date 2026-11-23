# Advanced Accessories Menu

## Maker visibility (2026-11-23)

The user confirmed the F1 hiding fix in KKS. BrowserFolders checks whether the Maker interface is actually visible. `MakerAPI.InsideAndLoaded` and `NativeMenu.Shown` can both stay true while another screen covers Maker. AAM now also checks `MakerAPI.IsInterfaceVisible()` before drawing its Maker window and Advanced button or handling the Maker shortcut. For Configuration Manager, `ConfigManagerBridge` reads the live `DisplayingWindow` property through a cached getter. The previous implementation tracked calls to the setter.

The first KKS retest still showed the old behavior because the build used `-p:CopyToGame=false`. The installed DLL had a different SHA-256 hash from the fixed build. After the fixed DLL was copied to `G:\A\sunshine\BepInEx\plugins\KKS_AdvancedAccessoriesMenu.dll` and the hashes matched, the user confirmed it worked. For a similar report, compare the build and installed DLL with `Get-FileHash` and restart KKS before retesting. All four game targets compiled. The in-game confirmation was for KKS.
