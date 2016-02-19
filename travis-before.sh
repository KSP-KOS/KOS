#!/bin/bash
set -ev

if [ "${TRAVIS_PULL_REQUEST}" = "false" ]; then
	echo "Build from within the home repository, encryption enabled."
	wget --quiet https://github.com/KSP-KOS/KSP_LIB/blob/master/kos-1.0.5.tar.enc?raw=true -O kos-1.0.5.tar.enc
	openssl aes-256-cbc -K $encrypted_6287ee711a27_key -iv $encrypted_6287ee711a27_iv -in kos-1.0.5.tar.enc -out kos-1.0.5.tar -d
else
	echo "Build from pull request outside of the home repository, encryption disabled."
	wget --quiet https://github.com/KSP-KOS/KSP_LIB/blob/master/kos-1.0.5.tar?raw=true -O kos-1.0.5.tar
fi

mkdir -p Resources
tar -xvf kos-1.0.5.tar -C Resources/

python --version
pip --version
# workaround for not being able to use pip outside of a python project
export PATH=$HOME/.local/bin:$PATH
pip install --user $USER sphinx_rtd_theme
cd ../doc
make html
cd ../