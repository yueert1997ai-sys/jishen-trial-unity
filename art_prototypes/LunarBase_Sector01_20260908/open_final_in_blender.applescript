set previousClipboard to missing value
try
    set previousClipboard to the clipboard
end try
set the clipboard to "bpy.ops.wm.open_mainfile(filepath='/Users/yuemarco/Documents/mech/jishen-trial-unity/art_prototypes/LunarBase_Sector01_20260908/MARE07_LUNAR_BASE_MASTER.blend')"
tell application "System Events" to tell process "Blender"
    set frontmost to true
    click at {420, 360}
    key code 36 using shift down
    delay 0.6
    keystroke "v" using command down
end tell
delay 1
if previousClipboard is not missing value then set the clipboard to previousClipboard
