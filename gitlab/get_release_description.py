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

def get_release_description(version):
    if version.startswith("v"):
        version = version[1:]

    release_description = ""
    found = False
    with open("CHANGELOG.md", "r") as f:
        for line in f.readlines():
            if not found and line.strip() == f"# {version}":
                found = True
            elif found and line.startswith("# "):
                break
            if found:
                release_description += f"{line}"
    return release_description


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Extract description of a release from CHANGELOG.md")
    parser.add_argument("version", help="Version number")
    args = parser.parse_args()

    print(get_release_description(args.version))
