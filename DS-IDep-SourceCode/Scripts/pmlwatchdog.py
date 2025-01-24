from watchdog.events import FileSystemEventHandler
from watchdog.observers import Observer

from procmon_parser import ProcmonLogsReader

import datetime
import threading
import json
import time
import sqlite3
import collections
import subprocess
import re
import os
import shutil
import hashlib
import platform
import argparse
import ctypes
from ctypes import wintypes
from sqlite3 import Connection
import logging

import pytz

logging_enabled = True
log_file_path = "C:/SZTSProgramInstaller/SZTSConfig/pmlwatchdog_logs.txt"

if logging_enabled:
    logging.basicConfig(filename=log_file_path, level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
else:
    logging.basicConfig(level=logging.CRITICAL)

class LRUCache:
  
    def __init__(self, capacity):
        self.capacity = capacity
        self.queue = collections.OrderedDict()

    def get(self, key):
        if key not in self.queue:
            return -1
        value = self.queue.pop(key)
        self.queue[key] = value
        return self.queue[key]

    def put(self, key, value):
        if key in self.queue:
            self.queue.pop(key)
        elif len(self.queue.items()) == self.capacity:
            self.queue.popitem(last=False)
        self.queue[key] = value

prefix="C:/SZTSProgramInstaller/SZTSProgram/"

dbname=''
pattern = r'[a-zA-Z]:\\(?:[\w\-\s\.~]+\\)*[\w\-\s\.~]+\.\w+'
file_lre=LRUCache(100)
sandbox_ip="192.168.0.100"
file_protected=[]

db_file = r'C:\SZTSProgramInstaller\SZTSProgram\test.db'
db_file_test0 = r'C:\SZTSProgramInstaller\SZTSProgram\test0.db'
backup_directory = r'C:\SZTSProgramInstaller\SZTSProgram\database_backups'

db_path = "C:\\SZTSProgramInstaller\\SZTSProgram\\database_backups"
icon_path  = "C:\\SZTSProgramInstaller\\SZTSProgram\\icon_pngs"
final_png_path = "C:\\SZTSProgramInstaller\\SZTSConfig\\pgraph.png"

if not os.path.exists(backup_directory):
    os.makedirs(backup_directory)

def comma_separated_values(value):
    return value.split(',')
    
def send_file_to_sandbox(path):
    curl_command = "curl -X POST -F \"file=@"+path+"\" http://"+sandbox_ip+":8088/breedbox/remote_submit_sample/"
    output = subprocess.check_output(curl_command, shell=True)
    output = output.decode('utf-8')

def move_file_to_folder(path):
    fpath,fname=os.path.split(path) 
    shutil.copy(path,prefix+'malicious_file/'+fname) 

def gen_hash(file_path,hash_type):
    with open(file_path, 'rb') as fp:
        data = fp.read()
    h=hash_type
    h.update(data)
    file_hash=h.hexdigest()
    return file_hash

def gen_description(file_path,integrity_level,warn_level):
    info=dict()
    info['path']=file_path
    info['md5']=gen_hash(file_path,hashlib.md5())
    info['sha256']=gen_hash(file_path,hashlib.sha256())
    info['size']=os.stat(file_path).st_size

    info['os']=platform.platform()
    info['warn_level']=warn_level
    info['intgrity_level']=integrity_level

    with open(prefix+'malicious_file/description.json','a+') as file:
        file.write(json.dumps(info))
        file.write("\n")

    return 

def create_folder():
    if not os.path.exists(prefix+"mypmlfile"):
        os.makedirs(prefix+"mypmlfile")

    if not os.path.exists(prefix+"myjsonfile"):
        os.makedirs(prefix+"myjsonfile")
        
    if not os.path.exists(prefix+"malicious_file"):
        os.makedirs(prefix+"malicious_file")

def create_db():
    con=sqlite3.connect(dbname)
    cur=con.cursor()
    
    cur.execute("CREATE TABLE IF NOT EXISTS network(id INTEGER PRIMARY KEY,\
                'Date & Time' TEXT, 'Process Name' TEXT, 'PID' TEXT, 'Operation' TEXT, 'Result' TEXT, \
                'Detail' TEXT, 'Sequence' TEXT, 'Company' TEXT, 'Description' TEXT, 'Command Line' TEXT, 'User' TEXT, 'Image Path' TEXT,\
                'Session' TEXT, 'Path' TEXT, 'TID' TEXT, 'Relative Time' TEXT, 'Duration' TEXT, 'Time of Day' TEXT, 'Version' TEXT, \
                'Event Class' TEXT, 'Authentication ID' TEXT, 'Virtualized' TEXT, 'Integrity' TEXT, 'Category' TEXT, 'Parent PID' TEXT, \
                'Architecture' TEXT, 'Completion Time' TEXT)")

    cur.execute("CREATE TABLE IF NOT EXISTS process(id INTEGER PRIMARY KEY,\
                'Date & Time' TEXT, 'Process Name' TEXT, 'PID' TEXT, 'Operation' TEXT, 'Result' TEXT, \
                'Detail' TEXT, 'Sequence' TEXT, 'Company' TEXT, 'Description' TEXT, 'Command Line' TEXT, 'User' TEXT, 'Image Path' TEXT,\
                'Session' TEXT, 'Path' TEXT, 'TID' TEXT, 'Relative Time' TEXT, 'Duration' TEXT, 'Time of Day' TEXT, 'Version' TEXT, \
                'Event Class' TEXT, 'Authentication ID' TEXT, 'Virtualized' TEXT, 'Integrity' TEXT, 'Category' TEXT, 'Parent PID' TEXT, \
                'Architecture' TEXT, 'Completion Time' TEXT)")
                
    cur.execute("CREATE TABLE IF NOT EXISTS filesystem(id INTEGER PRIMARY KEY,\
                'Date & Time' TEXT, 'Process Name' TEXT, 'PID' TEXT, 'Operation' TEXT, 'Result' TEXT, \
                'Detail' TEXT, 'Sequence' TEXT, 'Company' TEXT, 'Description' TEXT, 'Command Line' TEXT, 'User' TEXT, 'Image Path' TEXT,\
                'Session' TEXT, 'Path' TEXT, 'TID' TEXT, 'Relative Time' TEXT, 'Duration' TEXT, 'Time of Day' TEXT, 'Version' TEXT, \
                'Event Class' TEXT, 'Authentication ID' TEXT, 'Virtualized' TEXT, 'Integrity' TEXT, 'Category' TEXT, 'Parent PID' TEXT, \
                'Architecture' TEXT, 'Completion Time' TEXT)")
    
    cur.execute("CREATE TABLE IF NOT EXISTS malicious(id INTEGER PRIMARY KEY,\
                'Date & Time' TEXT, 'Process Name' TEXT, 'PID' TEXT, 'Operation' TEXT, 'Result' TEXT, \
                'Detail' TEXT, 'Sequence' TEXT, 'Company' TEXT, 'Description' TEXT, 'Command Line' TEXT, 'User' TEXT, 'Image Path' TEXT,\
                'Session' TEXT, 'Path' TEXT, 'TID' TEXT, 'Relative Time' TEXT, 'Duration' TEXT, 'Time of Day' TEXT, 'Version' TEXT, \
                'Event Class' TEXT, 'Authentication ID' TEXT, 'Virtualized' TEXT, 'Integrity' TEXT, 'Category' TEXT, 'Parent PID' TEXT, \
                'Architecture' TEXT, 'Completion Time' TEXT, 'Malicious File Path' TEXT)")
    
    con.commit()
    con.close()
    
def backup_db_file(db_file, backup_file_path):
    try:
        shutil.copy(db_file, backup_file_path)
    except FileNotFoundError as e:
        pass
    
def replace_db_with_test0():
    try:
        shutil.copy2(db_file_test0, db_file)
    except FileNotFoundError as e:
        pass
            
def wait_and_delete(file_path, check_interval=1, timeout=60):
    start_time = time.time()
    while time.time() - start_time < timeout:
        try:
            if os.path.exists(file_path):
                os.remove(file_path)
                logging.info(f"{file_path} has been deleted.")
                return True
            else:
                return True
        except PermissionError:
            logging.error(f"File is currently in use. Retrying in {check_interval} seconds...")
            time.sleep(check_interval)

    logging.error(f"Failed to delete {file_path} within the timeout period.")
    return False
    
def delete_pml_json_files():
    pml_dir = prefix + 'mypmlfile/'
    for pml_file in os.listdir(pml_dir):
        pml_file_path = pml_dir + pml_file
        wait_and_delete(pml_file_path)
        
def connect_db_with_retries(dbname, retries=1, delay=3):
    for attempt in range(retries):
        try:
            con = sqlite3.connect(dbname, check_same_thread=False)
            return con
        except sqlite3.OperationalError:
            time.sleep(delay)
    raise sqlite3.OperationalError("Failed to connect to the database after multiple retries")
    
def execute_with_retries(cur, value_str, params, retries=1, delay=3):
    for attempt in range(retries):
        try:
            cur.execute(value_str, params)
            return
        except sqlite3.OperationalError:
            time.sleep(delay)
    raise sqlite3.OperationalError("Failed to execute the query after multiple retries")

china_tz = pytz.timezone('Asia/Shanghai')
date_time_fmt = '%m/%d/%Y %I:%M:%S %p'
time_of_day_fmt = '%I:%M:%S.%f %p'
completion_time_fmt = '%I:%M:%S.%f %p'

def convert_to_china_time(utc_time_str, fmt, has_seven_microseconds=False):
    try:
        if has_seven_microseconds:
            time_parts = utc_time_str.split(' ')
            main_time = time_parts[0]
            am_pm = time_parts[1] if len(time_parts) > 1 else ''
            microsecond_start = main_time.rfind('.')
            if microsecond_start != -1:
                microseconds = main_time[microsecond_start+1:]
                if len(microseconds) == 7:
                    main_time = main_time[:microsecond_start+7]
                utc_time_str = f"{main_time} {am_pm}".strip()
        utc_time = datetime.datetime.strptime(utc_time_str, fmt)
        utc_time = pytz.utc.localize(utc_time)
        china_time = utc_time.astimezone(china_tz)
        return china_time.strftime(fmt)
    except ValueError as e:
        logging.error(f"Error parsing time string '{utc_time_str}' with format '{fmt}': {e}")
        return None
        
def process_time_field(info, field_name, fmt, has_seven_microseconds=False):
    if field_name in info:
        converted_time = convert_to_china_time(info[field_name], fmt, has_seven_microseconds)
        if converted_time is not None:
            info[field_name] = converted_time
        else:
            logging.error(f"Failed to convert time field '{field_name}' with value '{info[field_name]}'")

whitelist = ["数字替身资源防护.lnk", "微信.lnk", "FilterGUI.exe", "unionFileSubstituteSystem.exe", "encfs.exe", \
    "PopPwd.exe", "PopPwd2.exe", "privatekeyValidation.exe", "pmlwatchdog.exe", "processmon.exe", "Procmon.exe", \
    "Procmon64.exe", "control.exe", \
    "desktop.ini", "Downloads.lnk", "Documents.lnk", "Pictures.lnk", "Music.lnk", "Videos.lnk.lnk", "Pictures.lnk", "RecentPlaces.lnk"]

proc_whitelist = ["FilterGUI.exe", "unionFileSubstituteSystem.exe", "encfs.exe", \
    "PopPwd.exe", "PopPwd2.exe", "privatekeyValidation.exe", "pmlwatchdog.exe", "processmon.exe", "Procmon.exe", \
    "Procmon64.exe", "control.exe", "explorer.exe", "Explorer.EXE", "Explorer.exe" \
    ]

def pmlparser(lru,jsonpath,pmlpath):
    time.sleep(65)

    try:
        con=sqlite3.connect(dbname)
        cur=con.cursor()

        lru_pre=LRUCache(20)

        send_files=[]

        jsonname=jsonpath+datetime.datetime.now().strftime("%Y-%m-%d+%H-%M-%S")+".json"
        with open(jsonname,'a+') as jsonfile:
            with open(pmlpath,'rb') as f:
                pml=ProcmonLogsReader(f)
                for item in pml:
                    try:
                        info=item.get_compatible_csv_info()
                        process_time_field(info, 'Time of Day', time_of_day_fmt, has_seven_microseconds=True)
                        process_time_field(info, 'Date & Time', date_time_fmt)
                        process_time_field(info, 'Completion Time', completion_time_fmt, has_seven_microseconds=True)
                        
                        data_type=""
                        if info['Event Class']=='Network':
                            data_type='network'
                        elif info['Event Class']=='Process':
                            data_type='process'
                        elif info['Event Class']=='File System':
                            data_type='filesystem'
                        
                        if data_type=='filesystem':
                            flag=0
                            for file in file_protected:
                                if info['Path'].count(file)>0:
                                    flag=1
                                    break

                            if flag==0:
                                continue

                        if data_type=='filesystem' and lru.get(info['PID'])==-1:
                            lru.put(info['PID'],info['Process Name'])
                            lru.put(info['Parent PID'],"*")

                        if data_type=='network' :
                            if lru.get(info['PID'])!=-1 or lru.get(info['Parent PID'])!=-1:
                                lru.put(info['PID'],info['Process Name'])
                                lru.put(info['Parent PID'],"*")
                            else:
                                continue
                        
                        if data_type=='process':
                            if lru.get(info['PID'])!=-1 or lru.get(info['Parent PID'])!=-1:
                                lru.put(info['PID'],info['Process Name'])
                                lru.put(info['Parent PID'],"*")
                            else:
                                continue
                        
                        jsonfile.write(json.dumps(info))
                        jsonfile.write("\n")

                        coloumvalue=[]
                        for s in info.values():
                            coloumvalue.append(str(s))

                        value_str="INSERT INTO "+data_type+" VALUES (NULL,?, ?, ?, ?, ?, \
                            ?, ?, ?, ?, ?, ?, ?,\
                            ?, ?, ?, ?, ?, ?, ?, \
                            ?, ?, ?, ?, ?, ?, \
                            ?, ?)"
                    
                        cur.execute(value_str,(coloumvalue[0],coloumvalue[1],coloumvalue[2],coloumvalue[3],coloumvalue[4],coloumvalue[5],coloumvalue[6],coloumvalue[7],
                                            coloumvalue[8],coloumvalue[9],coloumvalue[10],coloumvalue[11],coloumvalue[12],coloumvalue[13],coloumvalue[14],coloumvalue[15],
                                            coloumvalue[16],coloumvalue[17],coloumvalue[18],coloumvalue[19],coloumvalue[20],coloumvalue[21],coloumvalue[22],coloumvalue[23],
                                            coloumvalue[24],coloumvalue[25],coloumvalue[26]))
                        
                        if data_type=='filesystem' and info['Operation'] != 'ReadFile':
                            res=re.findall(pattern,info['Command Line'])
                            malicious_files=""
                            for idx,ans in enumerate(res):
                                if idx==0 and info['Process Name'] in proc_whitelist:
                                    continue
                                    
                                if idx>=0:
                                    malicious_files+=ans+' '
                                    if file_lre.get(ans)==-1:
                                        file_lre.put(ans,"")
                                        send_files.append(ans)

                                        if os.path.exists(ans):
                                            gen_description(ans,info['Integrity'],'high')

                            if len(malicious_files)==0:
                                continue

                            value_str="INSERT INTO "+"malicious"+" VALUES (NULL,?, ?, ?, ?, ?, \
                                ?, ?, ?, ?, ?, ?, ?,\
                                ?, ?, ?, ?, ?, ?, ?, \
                                ?, ?, ?, ?, ?, ?, \
                                ?, ?, ?)"
                        
                            cur.execute(value_str,(coloumvalue[0],coloumvalue[1],coloumvalue[2],coloumvalue[3],coloumvalue[4],coloumvalue[5],coloumvalue[6],coloumvalue[7],
                                                coloumvalue[8],coloumvalue[9],coloumvalue[10],coloumvalue[11],coloumvalue[12],coloumvalue[13],coloumvalue[14],coloumvalue[15],
                                                coloumvalue[16],coloumvalue[17],coloumvalue[18],coloumvalue[19],coloumvalue[20],coloumvalue[21],coloumvalue[22],coloumvalue[23],
                                                coloumvalue[24],coloumvalue[25],coloumvalue[26],malicious_files))
                  
                    except Exception as e:
                        logging.error(f"Error processing pml item {item}, {pmlpath}:{e}")
                    
                cnt=0
                for i in range(len(pml)-1,0,-1):
                    if cnt>3:
                        break
                    info=pml[i].get_compatible_csv_info()
                    process_time_field(info, 'Time of Day', time_of_day_fmt, has_seven_microseconds=True)
                    process_time_field(info, 'Date & Time', date_time_fmt)
                    process_time_field(info, 'Completion Time', completion_time_fmt, has_seven_microseconds=True)
                    
                    if info['Operation']=='Process Create' and lru.get(info['PID'])!=-1:
                        lru_pre.put(info['PID'],"")
                        continue
                    
                    if lru_pre.get(info['PID'])!=-1 and info['Operation']=='ReadFile':
                        whiteflag = 0
                        for whiteitem in whitelist:
                            if info['Path'].find(whiteitem) >= 0:
                                whiteflag = 1
                                break
                        
                        if whiteflag == 0:
                            for whiteitem in proc_whitelist:
                                if info['Process Name'].find(whiteitem) >= 0:
                                    whiteflag = 1
                                    break
                        
                        if whiteflag != 1:
                            if file_lre.get(info['Path'])==-1:
                                cnt+=1
                                file_lre.put(info['Path'],"")
                                send_files.append(info['Path'])

                                mpath=info['Path']
                                if os.path.exists(mpath):
                                    gen_description(mpath,info['Integrity'],'middle')

                                coloumvalue=[]
                                for s in info.values():
                                    coloumvalue.append(str(s))

                            value_str="INSERT INTO "+"malicious"+" VALUES (NULL,?, ?, ?, ?, ?, \
                                ?, ?, ?, ?, ?, ?, ?,\
                                ?, ?, ?, ?, ?, ?, ?, \
                                ?, ?, ?, ?, ?, ?, \
                                ?, ?, ?)"
                        
                            cur.execute(value_str,(coloumvalue[0],coloumvalue[1],coloumvalue[2],coloumvalue[3],coloumvalue[4],coloumvalue[5],coloumvalue[6],coloumvalue[7],
                                                coloumvalue[8],coloumvalue[9],coloumvalue[10],coloumvalue[11],coloumvalue[12],coloumvalue[13],coloumvalue[14],coloumvalue[15],
                                                coloumvalue[16],coloumvalue[17],coloumvalue[18],coloumvalue[19],coloumvalue[20],coloumvalue[21],coloumvalue[22],coloumvalue[23],
                                                    coloumvalue[24],coloumvalue[25],coloumvalue[26],info['Path']))

        con.commit()
    
    except Exception as e:
        logging.error(f"Exception occurred {pmlpath}: {e}")
        
    finally:
        if con:
            con.close()
        for file in send_files:
            if os.path.exists(file):
                move_file_to_folder(file)
                
        try:
            wait_and_delete(pmlpath)
            logging.info(f"Delete PML file {pmlpath}")
        except Exception as e:
            logging.error(f"Failed to delete PML file {pmlpath}: {e}")

        current_time = datetime.datetime.now()
        if (current_time.hour == 8) and (current_time.minute >= 0) and (current_time.minute <= 1):
            current_day = current_time.strftime('%Y-%m-%d')
            backup_file_name = f'{current_day}-test.db'
            backup_file_path = os.path.join(backup_directory, backup_file_name)
            if os.path.exists(backup_file_path):
                logging.error(f"Backup db file today already exists! backup_file_path : {backup_file_path}")
        
            else:
                backup_db_file(db_file, backup_file_path)
                logging.info("backup db file success")
                replace_db_with_test0()
                logging.info("update db file success")
                delete_pml_json_files()
    
    return

class MyHandler(FileSystemEventHandler):
    
    def __init__(self):
        super().__init__()
        self.jsonpath=prefix+'myjsonfile/'
        self.lru=LRUCache(200)

    def on_created(self, event):
        if event.src_path.endswith('.pml'):
            thread=threading.Thread(target=pmlparser,args=(self.lru,self.jsonpath,event.src_path,))
            thread.start()

if __name__ == "__main__":

    cur_parser = argparse.ArgumentParser()
    cur_parser.add_argument("--sensitive", type=comma_separated_values)
    args = cur_parser.parse_args()

    if args.sensitive:
        for a in args.sensitive:
            file_protected.append(a)

    pmlpath=prefix+'mypmlfile/'

    create_folder()

    observer = Observer()
    event_handler = MyHandler()
    observer.schedule(event_handler, path=pmlpath, recursive=True)
    observer.start()

    try:
        while True:
            dbname=prefix+"test.db"
            if not os.path.exists(dbname):
                create_db()
            time.sleep(1)
    except KeyboardInterrupt:
        observer.stop()

    observer.join()


