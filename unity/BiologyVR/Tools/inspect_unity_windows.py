"""Read visible Unity window captions/buttons to diagnose a modal build blocker."""
import ctypes
import json
import sys
from ctypes import wintypes

pid = int(sys.argv[1])
user = ctypes.WinDLL('user32', use_last_error=True)
callback = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
user.GetWindowTextW.argtypes = [wintypes.HWND, wintypes.LPWSTR, ctypes.c_int]
user.GetClassNameW.argtypes = [wintypes.HWND, wintypes.LPWSTR, ctypes.c_int]
user.GetWindowThreadProcessId.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)]
user.IsWindowVisible.argtypes = [wintypes.HWND]
user.EnumWindows.argtypes = [callback, wintypes.LPARAM]
user.EnumChildWindows.argtypes = [wintypes.HWND, callback, wintypes.LPARAM]
user.SendMessageW.argtypes = [wintypes.HWND, wintypes.UINT, wintypes.WPARAM, wintypes.LPARAM]
rows = []

def info(handle):
    title, kind = ctypes.create_unicode_buffer(2048), ctypes.create_unicode_buffer(256)
    user.GetClassNameW(handle, kind, len(kind))
    # Never inspect editable/password fields.
    if kind.value.lower() != 'edit':
        user.GetWindowTextW(handle, title, len(title))
    return {'handle': int(handle), 'class': kind.value, 'caption': title.value}

@callback
def top(handle, unused):
    owner = wintypes.DWORD()
    user.GetWindowThreadProcessId(handle, ctypes.byref(owner))
    if owner.value != pid or not user.IsWindowVisible(handle):
        return True
    row = info(handle)
    children = []
    @callback
    def child(child_handle, unused):
        if user.IsWindowVisible(child_handle):
            item = info(child_handle)
            if item['caption']:
                children.append(item)
        return True
    user.EnumChildWindows(handle, child, 0)
    row['children'] = children
    rows.append(row)
    return True

user.EnumWindows(top, 0)
print(json.dumps(rows, ensure_ascii=False, indent=2))
if '--dismiss-invalid-path' in sys.argv:
    for row in rows:
        if row['class'] == '#32770' and row['caption'] == 'Invalid project path':
            for child in row['children']:
                if child['class'] == 'Button' and child['caption'] == '&OK':
                    user.SendMessageW(child['handle'], 0x00F5, 0, 0)
                    print('Dismissed the specific Invalid project path / OK dialog')
if '--cancel-sdk-detection' in sys.argv:
    for row in rows:
        if row['class'] == '#32770' and row['caption'].startswith('Detecting Android SDK'):
            for child in row['children']:
                if child['class'] == 'Button' and child['caption'] == 'Cancel':
                    user.SendMessageW(child['handle'], 0x00F5, 0, 0)
                    print('Cancelled the specific stalled Android SDK detection')
if '--dismiss-build-failure' in sys.argv:
    for row in rows:
        if row['class'] == '#32770' and row['caption'] == 'Build failure':
            for child in row['children']:
                if child['class'] == 'Button' and child['caption'] == '&OK':
                    user.SendMessageW(child['handle'], 0x00F5, 0, 0)
                    print('Acknowledged the build-failure result')
