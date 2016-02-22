#!/bin/bash

cd doc
sphinx-build -b html -q -d build/doctrees source gh-pages
echo $?
#sphinx-build -b html -q -W -d build/doctrees source gh-pages
#make html
cd ../