set previousClipboard to missing value
try
    set previousClipboard to the clipboard
end try
set the clipboard to "p = '/Users/yuemarco/Documents/mech/jishen-trial-unity/art_prototypes/LunarBase_Sector01_20260908/build_lunar_base.py'; exec(compile(open(p).read(), p, 'exec'), dict(__file__=p))"
tell application "System Events" to tell process "Blender"
    set frontmost to true
    keystroke "v" using command down
    delay 0.4
    key code 36
end tell
delay 0.3
if previousClipboard is not missing value then set the clipboard to previousClipboard
