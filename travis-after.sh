#!/bin/bash
set -ev

cd Resources
rm *.dll
cd ../doc
make html
rm -r gh-pages
rm -r build
cd ../