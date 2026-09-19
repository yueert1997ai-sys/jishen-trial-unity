@echo off
setlocal
set "MECH_EQUIPMENT_PROFILE=%~dp0handoff\GameplayLoop_V1\AuditEvidence\p0-combat\local-demo-profile.json"
cd /d "%~dp0handoff\GameplayLoop_V1"
start "" "Builds\P0_10_1_NoCannon\MECH_TRIAL_P0.exe" -p0Demo -screen-fullscreen 0 -screen-width 1600 -screen-height 900
