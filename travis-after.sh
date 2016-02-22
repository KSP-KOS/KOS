#!/bin/bash
set -ev

cd Resources
rm *.dll
cd ../doc
rm -r gh-pages
rm -r build
cd ../