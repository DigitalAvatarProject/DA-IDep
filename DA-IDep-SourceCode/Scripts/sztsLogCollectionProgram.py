import os
import argparse
import time
import json
import subprocess
import zipfile

def comma_separated_values(value):
    return value.split(',')

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
    return False

def delete_pml_json_files():
    pml_dir = "C:\\SZTSProgramInstaller\\SZTSProgram\\mypmlfile\\"
    for pml_file in os.listdir(pml_dir):
        pml_file_path = pml_dir + pml_file
        wait_and_delete(pml_file_path)

def read_sensitive_from_config():
    json_path = "C:\\SZTSProgramInstaller\\SZTSConfig\\forms.json"
    if os.path.exists(json_path):
        try:
            with open(json_path, "r", encoding="utf-8") as json_file:
                json_data = json.load(json_file)
                return json_data.get("logKeywords", "敏感文件资料")
        except Exception as e:
            return "敏感文件资料"
    return "敏感文件资料"

if __name__ == "__main__":

    parser = argparse.ArgumentParser()
    parser.add_argument("--start", action='store_true', help='start exe')
    parser.add_argument("--end", action="store_true", help="terminate exe")
    parser.add_argument("--submit", action="store_true", help="submit malicious file")
    args = parser.parse_args()

    if args.start:
        sensitive_value = read_sensitive_from_config()

        cmd = [
            "C:\\SZTSProgramInstaller\\SZTSProgram\\pmlwatchdog.exe",
            "--sensitive", sensitive_value
        ]
        subprocess.Popen(cmd, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

        cmd2 = ["C:\\SZTSProgramInstaller\\SZTSProgram\\processmon.exe"]
        subprocess.Popen(cmd2, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    if args.end:
        pro1 = ['run-hidden.exe','C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe','taskkill /f /im processmon.exe']
        subprocess.run(pro1)

        myargs2=['run-hidden.exe','C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe','./Procmon64.exe','/Terminate']
        subprocess.run(myargs2)
        
        myargs3=['run-hidden.exe','C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe','./Procmon.exe','/Terminate']
        subprocess.run(myargs3)
        
        pro4 = ['run-hidden.exe','C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe','taskkill /f /im Procmon64.exe']
        subprocess.run(pro4)
        
        pro5 = ['run-hidden.exe','C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe','taskkill /f /im Procmon.exe']
        subprocess.run(pro5)
        
        pro6 = ['run-hidden.exe','C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe','taskkill /f /im pmlwatchdog.exe']
        subprocess.run(pro6)
        
        pml_dir = "C:\\SZTSProgramInstaller\\SZTSProgram\\mypmlfile\\"
        for pml_file in os.listdir(pml_dir):
            pml_file_path = pml_dir + pml_file
            wait_and_delete(pml_file_path)
            
        json_dir = "C:\\SZTSProgramInstaller\\SZTSProgram\\myjsonfile\\"
        for json_file in os.listdir(json_dir):
            json_file_path = json_dir + json_file
            wait_and_delete(json_file_path)

    if args.submit:
        path='C:\SZTSProgramInstaller\SZTSProgram\malicious_file'
        zip = zipfile.ZipFile('C:\SZTSProgramInstaller\SZTSProgram\malicious_file.zip', 'w', zipfile.ZIP_DEFLATED)
        for file in os.listdir(path):
            zip.write(path+'/'+file)
        zip.close()

        filepath="C:\SZTSProgramInstaller\SZTSProgram\malicious_file.zip"
        sandbox_ip="192.168.0.100"
        curl_command = "curl --connect-timeout 3 -X POST -F \"file=@"+filepath+"\" http://"+sandbox_ip+":8088/breedbox/remote_submit_sample/"
        try:
            output = subprocess.check_output(curl_command, shell=True)
        except:
            print()
