import os
import subprocess
import time
import datetime
import logging
import psutil
import ctypes
from ctypes import wintypes
import json

logging_enabled = True
log_file_path = "C:/SZTSProgramInstaller/SZTSConfig/processmon_logs.txt"

if logging_enabled:
    logging.basicConfig(filename=log_file_path, level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
else:
    logging.basicConfig(level=logging.CRITICAL)

seconds_num = 60
prefix = "C:/SZTSProgramInstaller/SZTSProgram/"

def terminate_procmon():
    myargs2 = [prefix + 'run-hidden.exe', 'C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe', prefix + 'Procmon64.exe', '/Terminate']
    try:
        subprocess.run(myargs2)
        time.sleep(5)
    except Exception as e:
        logging.error(f"Error terminating Procmon: {e}")

    for proc in psutil.process_iter(['pid', 'name']):
        if proc.info['name'] in ['Procmon.exe', 'Procmon64.exe']:
            try:
                proc.terminate()
                proc.wait(timeout=5)
            except psutil.NoSuchProcess:
                logging.warning(f"Process {proc.info['name']} with PID {proc.info['pid']} does not exist")
            except psutil.TimeoutExpired:
                proc.kill()
                logging.warning(f"Killed leftover process {proc.info['name']} with PID {proc.info['pid']} after timeout")

def wait_and_delete(file_path, check_interval=1, timeout=60):
    start_time = time.time()
    while time.time() - start_time < timeout:
        try:
            if os.path.exists(file_path):
                os.remove(file_path)
                return True
            else:
                return True
        except PermissionError:
            time.sleep(check_interval)
    
    logging.error(f"Failed to delete {file_path} within the timeout period.")
    return False

def delete_pml_json_files():
    pml_dir = prefix + 'mypmlfile/'
    for pml_file in os.listdir(pml_dir):
        pml_file_path = pml_dir + pml_file
        wait_and_delete(pml_file_path)
        logging.info(f"wait_and_delete pml_file_path : {pml_file_path}")

if __name__ == '__main__':
    pmlpath = prefix + "mypmlfile/"
    while True:
        mytime = datetime.datetime.now()
        if ((mytime.minute >= 0) and (mytime.minute <= 1)) or ((mytime.minute >= 30) and (mytime.minute <= 31)):
            delete_pml_json_files()
    
        mytime = datetime.datetime.now().strftime("%Y-%m-%d+%H-%M-%S") + 'log'
        pmc = prefix + "pc.pmc"
        name = "-ArgumentList " + "'/Quiet /LoadConfig " + pmc + " /BackingFile " + pmlpath + mytime + ".pml /AcceptEula'"

        myargs = [prefix + 'run-hidden.exe', 'C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe', 'Start-Process', '-WindowStyle hidden',
                  '-FilePath ' + prefix + 'Procmon.exe', name]

        try:
            subprocess.run(myargs)
        except Exception as e:
            logging.error(f"Error starting Procmon: {e}, pml name : {name}")

        time.sleep(seconds_num)

        try:
            terminate_procmon()
        except Exception as e:
            logging.error(f"Error terminating Procmon: {e}, pml name : {name}")

        time.sleep(1)
