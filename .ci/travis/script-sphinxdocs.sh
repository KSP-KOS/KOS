#!/bin/bash
set -ev

cd doc
sphinx-build -b html -q -W -d build/doctrees source gh-pages
#make html
cd ../