# MIT License
#
# Copyright (c) 2021 Ubisoft
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in all
# copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.

import requests
import os
import argparse
import logging
import pprint
from .get_release_description import get_release_description

GITLAB_API_TOKEN = os.environ["GITLAB_API_TOKEN"]
CI_API_V4_URL = os.environ["CI_API_V4_URL"]
CI_SERVER_URL = os.environ["CI_SERVER_URL"]
CI_PROJECT_ID = os.environ["CI_PROJECT_ID"]
CI_COMMIT_REF_NAME = os.environ["CI_COMMIT_REF_NAME"]
UPLOAD_URL = f"{CI_API_V4_URL}/projects/{CI_PROJECT_ID}/uploads"
RELEASE_URL = f"{CI_API_V4_URL}/projects/{CI_PROJECT_ID}/releases"
VERSION_TAG = f"{CI_COMMIT_REF_NAME}"

logging.basicConfig(format="%(asctime)s - %(name)s - %(levelname)s - %(message)s", level=logging.INFO)

parser = argparse.ArgumentParser(description="Upload zip archive passed as argument and create a gitlab Release")
parser.add_argument("zip_file", help="Path to the zip file to upload.")
args = parser.parse_args()

release_description = get_release_description(VERSION_TAG[1:])
if release_description == "":
    logging.error(f"No release description found for version {args.version} in CHANGELOG.md")
    exit(1)

zip_file = args.zip_file

r = requests.post(UPLOAD_URL, headers={"PRIVATE-TOKEN": GITLAB_API_TOKEN}, files={"file": open(zip_file, "rb")})
try:
    r.raise_for_status()
except Exception as e:
    logging.error(e)
    logging.error(r.text)
    exit(1)

upload_result = r.json()
logging.info(upload_result)

data = {
    "name": f"Version {VERSION_TAG[1:]}",
    "tag_name": VERSION_TAG,
    "description": release_description,
    "assets": {
        "links": [
            {
                "name": upload_result["alt"],
                "url": f"{CI_SERVER_URL}{upload_result['full_path']}",
                "link_type": "other",
            }
        ]
    },
}

r = requests.post(RELEASE_URL, headers={"PRIVATE-TOKEN": GITLAB_API_TOKEN}, json=data)
try:
    r.raise_for_status()
except Exception as e:
    logging.error(e)
    logging.error(r.text)
    exit(1)

pprint.pprint(r.json())
