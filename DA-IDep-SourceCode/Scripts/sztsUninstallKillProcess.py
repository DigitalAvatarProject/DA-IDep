import psutil
import os
import subprocess
import winreg
import ctypes
from ctypes import wintypes
import json
import sys

def is_admin():
    try:
        return ctypes.windll.shell32.IsUserAnAdmin()
    except:
        return False

def kill_encfs():
    for proc in psutil.process_iter(['pid', 'name']):
        try:
            if proc.info['name'].lower() in ['encfs.exe', 'filtergui.exe']:
            #if proc.info['name'].lower() in ['encfs.exe', 'filtergui.exe', 'pmlwatchdog.exe', 'processmon.exe', 'Procmon.exe', 'Procmon64.exe']:
                proc.terminate()
        except (psutil.NoSuchProcess, psutil.AccessDenied, psutil.ZombieProcess):
            pass
            
def delete_task(task_name):
    try:
        check_command = f'schtasks /query /tn "{task_name}"'
        subprocess.run(check_command, shell=True, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        delete_command = f'schtasks /delete /tn "{task_name}" /f'
        subprocess.run(delete_command, shell=True)
    except subprocess.CalledProcessError:
        pass

def delete_environment_variable(name):
    reg_path = r'SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
    try:
        with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, reg_path, 0, winreg.KEY_READ | winreg.KEY_WRITE) as key:
            winreg.DeleteValue(key, name)
    except FileNotFoundError:
        return
    except Exception as e:
        return

def remove_paths_from_system_path(paths_to_remove):
    reg_path = r'SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
    with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, reg_path, 0, winreg.KEY_READ | winreg.KEY_WRITE) as key:
        value, _ = winreg.QueryValueEx(key, 'Path')
        paths = value.split(';')
        updated = False
        for path_to_remove in paths_to_remove:
            if path_to_remove in paths:
                paths.remove(path_to_remove)
                updated = True
        if updated:
            new_value = ';'.join(paths)
            winreg.SetValueEx(key, 'Path', 0, winreg.REG_EXPAND_SZ, new_value)
        else:
            return

def broadcast_environment_change():
    HWND_BROADCAST = 0xFFFF
    WM_SETTINGCHANGE = 0x001A
    SMTO_ABORTIFHUNG = 0x0002
    result = ctypes.c_long()
    SendMessageTimeout = ctypes.windll.user32.SendMessageTimeoutW

if __name__ == "__main__":
    kill_encfs()
    delete_task("SZTSProgramUpdate")
    
    if not is_admin():
        ctypes.windll.shell32.ShellExecuteW(None, "runas", sys.executable, ' '.join(sys.argv), None, 1)
    else:
        new_paths = [
            r"C:\SZTSProgramInstaller\SZTSProgram", 
            r"C:\SZTSProgramInstaller\SZTSProgram\cygwin64\bin", 
            r"C:\SZTSProgramInstaller\SZTSProgram\WinFsp\bin",
            r"C:\SZTSProgramInstaller\SZTSProgram\dotnet",
            r"C:\SZTSProgramInstaller\SZTSProgram\VisualCppRedistributable"
        ]
        
        delete_environment_variable('DOTNET_ROOT')
        remove_paths_from_system_path(new_paths)
        broadcast_environment_change()