#!/bin/bash
set -ev

cd Resources
rm *.dll
cd ../docs
rm -r gh-pages
rm -r build
cd ../