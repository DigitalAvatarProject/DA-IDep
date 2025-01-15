import os
import shutil
import subprocess
import getpass
import sys

def copy_files(src, dst):
    if not os.path.exists(dst):
        os.makedirs(dst)
    for root, dirs, files in os.walk(src):
        rel_path = os.path.relpath(root, src)
        dest_dir = os.path.join(dst, rel_path)
        if not os.path.exists(dest_dir):
            os.makedirs(dest_dir)
        for file in files:
            src_file = os.path.join(root, file)
            dst_file = os.path.join(dest_dir, file)
            shutil.copy(src_file, dst_file)

def set_permissions(dst):
    username = getpass.getuser()
    CREATE_NO_WINDOW = 0x08000000
    
    try:
        subprocess.run(
            ['icacls', dst, '/setowner', username, '/T', '/C', '/Q'],
            check=True,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            creationflags=CREATE_NO_WINDOW
        )
        
        subprocess.run(
            ['icacls', dst, '/inheritance:r'],
            check=True,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            creationflags=CREATE_NO_WINDOW
        )
        subprocess.run(
            ['icacls', dst, '/grant:r', f'{username}:(OI)(CI)F', '/T', '/C', '/Q'],
            check=True,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            creationflags=CREATE_NO_WINDOW
        )
        
    except subprocess.CalledProcessError as e:
        sys.exit(1)

def main():
    source_directory = r'C:\SZTSProgramInstaller\SZTSProgram\szts_encdir'
    destination_directory = r'C:\SZTSProgramInstaller\SZTSConfig\szts_encdir'
    destination_directory2 = r'C:\SZTSProgramInstaller\SZTSConfig\default_dir\szts_encdir'

    copy_files(source_directory, destination_directory)
    copy_files(source_directory, destination_directory2)

    set_permissions(destination_directory)
    set_permissions(destination_directory2)
    
    try:
        shutil.rmtree(source_directory)
    except Exception as e:
        sys.exit(1)

if __name__ == "__main__":
    main()
