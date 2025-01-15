import os
import subprocess
import winreg
import ctypes
from ctypes import wintypes
import sys
from datetime import datetime, timedelta

def is_admin():
    try:
        return ctypes.windll.shell32.IsUserAnAdmin()
    except:
        return False
        
def run_commands_as_task():
    start_time = (datetime.now() + timedelta(seconds=10)).strftime("%H:%M")

    task_name1 = "RunUnionFileSubstituteModeM"
    task_name2 = "RunUnionFileSubstituteModeU"

    subprocess.run(f'schtasks /create /tn {task_name1} /tr "C:\\SZTSProgramInstaller\\SZTSProgram\\unionFileSubstituteSystem.exe --mode m" /sc once /st {start_time} /rl limited /f', shell=True)
    subprocess.run(f'schtasks /create /tn {task_name2} /tr "C:\\SZTSProgramInstaller\\SZTSProgram\\unionFileSubstituteSystem.exe --mode u" /sc once /st {start_time} /rl limited /f', shell=True)

    #subprocess.run(f'schtasks /run /tn {task_name1}', shell=True)
    #subprocess.run(f'schtasks /run /tn {task_name2}', shell=True)

    #subprocess.run(f'schtasks /delete /tn {task_name1} /f', shell=True)
    #subprocess.run(f'schtasks /delete /tn {task_name2} /f', shell=True)

def add_task(task_name, app_path):
    schedule_time = "18:00"
    try:
        delete_command = f'schtasks /delete /tn "{task_name}" /f'
        subprocess.run(delete_command, shell=True)
        
    except subprocess.CalledProcessError:
        pass
    
    command = f'schtasks /create /tn "{task_name}" /tr "{app_path}" /sc DAILY /mo 3 /st {schedule_time} /RL HIGHEST'
    subprocess.run(command, shell=True)

def add_log_task(task_name, app_path, arguments):
    try:
        delete_command = f'schtasks /delete /tn "{task_name}" /f'
        subprocess.run(delete_command, shell=True, check=True)
    except subprocess.CalledProcessError:
        pass 

    command = f'schtasks /create /tn "{task_name}" /tr "{app_path} {arguments}" /sc ONCE /ST 00:00 /RL HIGHEST /F'
    try:
        subprocess.run(command, shell=True, check=True)
        return
    except subprocess.CalledProcessError as e:
        return

def add_to_system_path(new_paths):
    reg_path = r'SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
    with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, reg_path, 0, winreg.KEY_READ | winreg.KEY_WRITE) as key:
        value, _ = winreg.QueryValueEx(key, 'Path')
        paths = value.split(';')
        updated = False
        for new_path in new_paths:
            if new_path not in paths:
                paths.append(new_path)
                updated = True
        if updated:
            new_value = ';'.join(paths)
            winreg.SetValueEx(key, 'Path', 0, winreg.REG_EXPAND_SZ, new_value)

def add_to_registry(app_name, app_path):
    reg_path = r"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"
    try:
        reg_key = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, reg_path, 0, winreg.KEY_READ | winreg.KEY_SET_VALUE)
        try:
            existing_value, reg_type = winreg.QueryValueEx(reg_key, app_name)
            if existing_value != app_path:
                winreg.SetValueEx(reg_key, app_name, 0, winreg.REG_SZ, app_path)
        except FileNotFoundError:
            winreg.SetValueEx(reg_key, app_name, 0, winreg.REG_SZ, app_path)
        finally:
            winreg.CloseKey(reg_key)
    except PermissionError:
        return
    except Exception as e:
        return

def add_environment_variable(name, value):
    reg_path = r'SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
    with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, reg_path, 0, winreg.KEY_READ | winreg.KEY_WRITE) as key:
        winreg.SetValueEx(key, name, 0, winreg.REG_EXPAND_SZ, value)

def broadcast_environment_change():
    HWND_BROADCAST = 0xFFFF
    WM_SETTINGCHANGE = 0x001A
    SMTO_ABORTIFHUNG = 0x0002
    result = ctypes.c_long()
    SendMessageTimeout = ctypes.windll.user32.SendMessageTimeoutW
    SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, 0, "Environment", SMTO_ABORTIFHUNG, 5000, ctypes.byref(result))

if not is_admin():
    # Re-run the script with admin privileges
    ctypes.windll.shell32.ShellExecuteW(None, "runas", sys.executable, ' '.join(sys.argv), None, 1)
else:
    new_paths = [
        r"C:\SZTSProgramInstaller\SZTSProgram", 
        r"C:\SZTSProgramInstaller\SZTSProgram\cygwin64\bin", 
        r"C:\SZTSProgramInstaller\SZTSProgram\WinFsp\bin",
        #r"C:\SZTSProgramInstaller\SZTSProgram\dotnet",
        r"C:\SZTSProgramInstaller\SZTSProgram\VisualCppRedistributable"
    ]
    add_to_system_path(new_paths)
    #add_environment_variable('DOTNET_ROOT', r'C:\SZTSProgramInstaller\SZTSProgram\dotnet')
    broadcast_environment_change()
    
    app1_name = "SZTSProgramGUI"
    app1_path = r'C:\SZTSProgramInstaller\SZTSProgram\FilterGUI.exe --minimized'
    app2_name = "SZTSProgramFS"
    app2_path = r'C:\SZTSProgramInstaller\SZTSProgram\unionFileSubstituteSystem.exe --mode m'
    add_to_registry(app1_name, app1_path)
    add_to_registry(app2_name, app2_path)
    
    task_name = "SZTSProgramUpdate"
    app3_path = r"C:\SZTSProgramInstaller\SZTSProgram\AtUpdate.exe"
    add_task(task_name, app3_path)
