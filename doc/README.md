KOS_DOC
=======
The documents are generated using Sphinx restructured text, with the ReadTheDocs
theme.

#Getting started on Windows

(For this example, the KOS repository is assumed to be located at `C:\KOS`,
you should adjust your path based on your actual repository location)

1. If you don't already have Python installed, install the latest version in the
  2.7 series.  At the time of this writing, 2.7.11 was the most current version.

2. You can verify the installation and version of Python (and pip) by issuing
  the following commands from the command line:
  ```
  C:\>python --version
  Python 2.7.11
  C:\>pip --version
  pip
  ```

3. Install the Sphinx engine
  ```
  C:\>pip install sphinx
  ```

4. Ensure that the read the docs template is installed.
  ```
  C:\>pip install sphinx_rtd_theme
  ```

5. Switch to the docs directory and run the make batch file:
  ```
  C:\>cd KOS\docs
  C:\KOS\docs>make clean
  C:\KOS\docs>make html
  ```

6. Review the output for errors and warnings.  In the above example you would
  find the compiled html files at `C:\KOS\docs\gh-pages`

7. (Optional) You may browse the generated html using file urls, or by using
  Python's included SimpleHTTPServer:
  ```
  C:\KOS\docs>cd gh-pages
  C:\KOS\docs\gh-pages>python -m SimpleHTTPServer 8000
  Serving HTTP on 0.0.0.0 port 8000...
  ```

  At which point you can point your browser to `http://localhost:8000`

#Getting started on Linux
1. TODO: WRITE LINUX INSTRUCTIONS WITH APT-GET AND PIP

#Publishing
1. Clone KSP-KOS/KOS gh-pages branch if not done already, pull the most recent
  version from the upstream repository if you already have a clone.
2. The clone should either be in a different folder from the primary
  repository or you need to copy the rendered `doc/gh-pages` contents to a
  temporary folder
3. Delete the contents of the repository root, except the `.git` folder,
  `.gitatributes` file, and `.nojekyll` file.
4. Copy the contents of `doc/gh-pages` into the `gh-pages` repository root folder
5. Commit all of the changed files
6. Push the gh-pages branch to your own kOS github repository
7. You can test the documents at `https://[username].github.io/KOS`
8. Make a pull request against the KSP-KOS/KOS gh-pages branch
