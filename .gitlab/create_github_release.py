import argparse

parser = argparse.ArgumentParser()
parser.add_argument("--tag", "-t", help="tag name", required=True)
parser.add_argument("--name", "-n", help="release name", required=True)
parser.add_argument("--zip", "-z", help="zip path to upload", required=True)
parser.add_argument("--message", "-m", help="Release message")
args = parser.parse_args()

import os
import sys
from github import Github

access_token = os.environ.get("K8S_SECRET_GITHUB_ACCESS_TOKEN", None)
if access_token is None:
    sys.exit("Unable to retrieve GitHub access token from environment variables.")

github = Github(access_token)
repo = github.get_repo("ubisoft/vrtist")
release = repo.create_git_release(args.tag, args.name, args.message)
if release is None:
    sys.exit("Failed to create release")

asset = release.upload_asset(args.zip, label="", content_type="application/zip", name=f"VRtist_Win64_{args.tag}.zip")
if asset is None:
    sys.exit("Failed to upload asset")
