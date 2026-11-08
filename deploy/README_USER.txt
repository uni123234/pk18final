========================================================================
  MODULAR RPG PLATFORM & CHARACTER CATALOG - USER DEPLOYMENT GUIDE
========================================================================

SYSTEM REQUIREMENTS:
  - Operating System: Windows 10 (version 1809+) or Windows 11 (64-bit).
  - Runtime: .NET 8.0 Windows Desktop Runtime (only for Portable package;
    the Standalone package bundles its own runtime).
  - Storage: ~100 MB free disk space for cache and save files.

QUICK START:
  1. Automatic Installation:
     Double-click `install.bat`. It will create the desktop shortcut and
     initialize storage folders in `%LOCALAPPDATA%\ModularRpgPlatform`.

  2. Launch Desktop GUI:
     Double-click `run.bat` (or use the Desktop Shortcut).

  3. Launch Terminal CLI Console:
     Double-click `run_cli.bat` to run keyboard-driven commands.

BUILDING FROM SOURCE:
  If building on a development workstation with .NET 8 SDK:
    - Build Release:   `build.bat`
    - Package Release: `publish.bat`

UNINSTALLATION:
  Run `uninstall.bat` to safely remove desktop shortcuts and user data.
========================================================================
