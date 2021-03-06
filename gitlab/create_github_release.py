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

import argparse

parser = argparse.ArgumentParser()
parser.add_argument("--tag", "-t", help="tag name", required=True)
parser.add_argument("--name", "-n", help="release name", required=True)
parser.add_argument("--zip", "-z", help="zip path to upload", required=True)
args = parser.parse_args()

import os
import sys

access_token = os.environ.get("K8S_SECRET_GITHUB_ACCESS_TOKEN")
if access_token is None:
    sys.exit("Unable to retrieve GitHub access token from environment variables.")

repo_name = os.environ.get("GITHUB_MIRROR")
if repo_name is None:
    sys.exit("Unable to retrieve Github repository name from environment variables.")

from .get_release_description import get_release_description
message = get_release_description(args.tag)

from github import Github
github = Github(access_token)
repo = github.get_repo(repo_name)
release = repo.create_git_release(args.tag, args.name, message)
release.upload_asset(args.zip, label="", content_type="application/zip", name=f"VRtist_Win64_{args.tag}.zip")
