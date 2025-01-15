import os
import json
import sys
import logging
import subprocess
import string
import random
import base64
from Crypto.PublicKey import RSA
from Crypto.Signature import pkcs1_15
from Crypto.Hash import SHA256

public_key_path = 'C:\\SZTSProgramInstaller\\SZTSConfig\\public_key.pem'
default_private_key_path = 'C:\\SZTSProgramInstaller\\SZTSConfig\\private_key.pem'
config_path = 'C:\\SZTSProgramInstaller\\SZTSConfig\\forms.json'
config_password_path = 'C:\\SZTSProgramInstaller\\SZTSConfig\\f_secure.json'
password_path = 'C:\\SZTSProgramInstaller\\SZTSConfig\\key_password.txt'
log_path = 'C:\\SZTSProgramInstaller\\SZTSConfig\\pydebug_log.txt'

logging.basicConfig(filename=log_path, level=logging.DEBUG, format='%(asctime)s %(levelname)s:%(message)s', filemode='a', encoding='utf-8')

def generate_password(length=6):
    if os.path.exists(config_password_path):
        with open(config_password_path, 'r', encoding='utf-8') as f:
            config_password = json.load(f)
            if 'User' in config_password:
                password = config_password['User']
            else:
                characters = string.ascii_letters + string.digits + string.punctuation
                raw_password = ''.join(random.choice(characters) for i in range(length))
                password = base64.b64encode(password.encode()).decode()
    return password

def generate_keys(public_key_path, private_key_path):
    encoded_password = generate_password()
    password = base64.b64decode(encoded_password).decode()
    
    key = RSA.generate(2048)
    private_key = key.export_key(passphrase=password, pkcs=8, protection="scryptAndAES128-CBC")
    public_key = key.publickey().export_key()

    with open(private_key_path, 'wb') as f:
        f.write(private_key)

    with open(public_key_path, 'wb') as f:
        f.write(public_key)
    
    logging.info(f"Keys generated with password saved to {password_path}")
    return password

def main():
    try:
        if os.path.exists(config_path):
            with open(config_path, 'r') as f:
                config = json.load(f)
                if 'privateKeyLocation' in config:
                    private_key_path = config['privateKeyLocation']
                else:
                    private_key_path = default_private_key_path
        else:
            private_key_path = default_private_key_path

        password = generate_keys(public_key_path, private_key_path)

        with open(public_key_path, 'r', encoding='utf-8') as f:
            public_key = RSA.import_key(f.read())

        with open(private_key_path, 'r', encoding='utf-8') as f:
            private_key = RSA.import_key(f.read(), passphrase=password)

        challenge = os.urandom(32).hex()
        logging.debug(f"Challenge: {challenge}")

        hash_obj = SHA256.new(challenge.encode())

        signature = pkcs1_15.new(private_key).sign(hash_obj)

        try:
            pkcs1_15.new(public_key).verify(hash_obj, signature)
            logging.info("The signature is valid.")
            print("The signature is valid.")
            sys.exit(0)
        except (ValueError, TypeError) as e:
            logging.error(f"The signature is invalid: {e}")
            print(f"The signature is invalid: {e}")
            sys.exit(1)
    
    except Exception as e:
        logging.critical(f"Unhandled exception: {e}")
        sys.exit(-1)

if __name__ == "__main__":
    main()
