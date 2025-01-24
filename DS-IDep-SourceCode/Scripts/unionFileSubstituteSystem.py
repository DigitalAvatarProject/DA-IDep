import os
import subprocess
import argparse
import shutil
import json
import base64
import ctypes
import sys

def copySubstituteDir(source_folder, target_folder):
    if not os.path.exists(target_folder):
        os.makedirs(target_folder)

    for root, dirs, files in os.walk(source_folder):
        rel_path = os.path.relpath(root, source_folder)
        target_path = os.path.join(target_folder, rel_path)

        if not os.path.exists(target_path):
            os.makedirs(target_path)

        for file in files:
            source_file = os.path.join(root, file)
            target_file = os.path.join(target_path, file)
            shutil.copy2(source_file, target_file)

def load_configurations(mode):
    forms_path = r'C:\SZTSProgramInstaller\SZTSConfig\forms.json'
    secure_path = r'C:\SZTSProgramInstaller\SZTSConfig\f_secure.json'

    if not os.path.exists(forms_path):
        raise FileNotFoundError(f"{forms_path} 文件未找到")

    with open(forms_path, 'r', encoding='utf-8') as f:
        forms_config = json.load(f)
    
    drive_letter = forms_config.get('driveLetter')
    enc_path = forms_config.get('drivePath') if mode == 'm' else None

    enc_password = None
    if mode == 'm':
        if not os.path.exists(secure_path):
            raise FileNotFoundError(f"{secure_path} 文件未找到")
        
        with open(secure_path, 'r', encoding='utf-8') as f:
            secure_config = json.load(f)
            encoded_password = secure_config.get('Drive')
            enc_password = base64.b64decode(encoded_password).decode('utf-8')

    return drive_letter, enc_path, enc_password

def mount_encfs(enc_password, enc_path, drive_letter, source_replacedir, target_replacedir, mode='m'):
    if mode == 'm':
        encfs_command = f'"C://SZTSProgramInstaller//SZTSProgram//encfs.exe" {enc_path} {drive_letter}'
    elif mode == 'u':
        encfs_command = f'"C://SZTSProgramInstaller//SZTSProgram//encfs.exe" -u {drive_letter}'
    else:
        raise ValueError("Invalid mode. Use 'm' for mount or 'u' for unmount.")

    if (mode == 'm' and os.path.exists(enc_path)) or mode == 'u':
        proc = subprocess.Popen(
            encfs_command,
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            creationflags=subprocess.CREATE_NO_WINDOW
        )
        stdout, stderr = proc.communicate(input=enc_password)
        if proc.returncode != 0:
            print(f'Error: {stderr}')
        else:
            print(f'Success: {stdout}')
            if mode == 'm':
                updated_target_replacedir = drive_letter + target_replacedir
                copySubstituteDir(source_replacedir, updated_target_replacedir)
    else:
        exit_code = os.system(f'start /wait cmd /C "{encfs_command}"')
        if exit_code != 0:
            print(f'NewDir Error: {exit_code}.')
        else:
            print('NewDir Success.')

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='文件替身系统挂载')
    parser.add_argument('--mode', default='m', choices=['m', 'u'], help='挂载模式：m - mount, u - unmount')
    parser.add_argument('--source_replacedir', default='C:/SZTSProgramInstaller/SZTSConfig/目录树模版/敏感文件资料/temp/替身文件模版/', help='替身文件源目录，默认值：C:/SZTSProgramInstaller/SZTSConfig/目录树模版/敏感文件资料/temp/替身文件模版/')
    parser.add_argument('--target_replacedir', default='/敏感文件资料/temp/替身文件模版/', help='替身文件目录，默认值：/敏感文件资料/temp/替身文件模版/')
    
    args = parser.parse_args()

    try:
        drive_letter, enc_path, enc_password = load_configurations(args.mode)
        mount_encfs(enc_password, enc_path, drive_letter, args.source_replacedir, args.target_replacedir, args.mode)
    
    except Exception as e:
        print(f"Error: {e}")
