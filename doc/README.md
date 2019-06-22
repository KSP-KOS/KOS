KOS_DOC
=======
The documents are generated using Sphinx restructured text, with the ReadTheDocs
theme.

# Getting started on Windows

(For this example, the KOS repository is assumed to be located at `C:\KOS`,
you should adjust your path based on your actual repository location)

1. If you don't already have Python installed, install the latest version in the
  2.7 series.  At the time of this writing, 2.7.11 was the most current version.
  You may download the installer from: https://www.python.org/downloads/

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

# Getting started on Linux
1. As with Windows above, install Python 2.7.  You may use your distribution's
  package manager system, or download from: https://www.python.org/downloads/

2. All other instructions are the same as above for windows, replacing the `\`
  path character with `/` and adapting paths to reference your Linux file system.

# Publishing

This section pertains only to what has to be done when a new release of
the documentation is being made public (usually to correspond to a new
release of kOS itself.)  These steps do not need to be (and should not be)
performed on every single documentation edit and every merged PR.

1. We recommend creating a second clone repository for managing Github pages
  publishing.  You should also add KSP-KOS/KOS as the upstream remote:
  ```
  C:\KOS>cd ..
  C:\>git clone https://github.com/[username]/KOS.git KOS-gh-pages
  C:\>cd KOS-gh-pages
  C:\KOS-gh-pages>git remote add upstream https://github.com/KSP-KOS/KOS.git
  ```

2. For those who have permission to publish to the KOS_DOC repository, you will
  also need to add it as a new remote:
  ```
  C:\KOS-gh-pages>git remote add KOS_DOC https://github.com/KSP-KOS/KOS_DOC.git
  ```

3. Checkout the gh-pages branch and update it to match the upstream version:
  ```
  C:\KOS-gh-pages>git checkout gh-pages
  C:\KOS-gh-pages>git pull --ff-only upstream gh-pages
  ```

  (You may delete other branches from this clone if you want, but make sure you
  do not delete the remote branch)

4. The previous sphinx output needs to be deleted.  Delete all files and folders
  within the `KOS-gh-pages` folder **except** files and folders with a leading `.`
  character (such as `.git`, `.gitattributes`, `.nojekyll`, and similar files).
  The file `.buildinfo` also needs to be deleted even though it starts with `.`:
  ```
  C:\KOS-gh-pages>git rm -r [!.]*
  C:\KOS-gh-pages>git rm .buildinfo
  ```

5. Copy the contents of th `KOS\doc\gh-pages` folder into `KOS-gh-pages`.

6. Add the updated files, commit, and push to your origin.  You should include a
  message that represents the reason for the update, such as "Update docs for
  v0.19.3"
  ```
  C:\KOS-gh-pages>git reset head
  C:\KOS-gh-pages>git add --all
  C:\KOS-gh-pages>git commit -m [message]
  C:\KOS-gh-pages>git push
  ```

  If any of your local editing tools added extra files not created by sphinx,
  be sure to unstage them **before** you commit.
  ```
  C:\KOS-gh-pages>git reset [path_to_file]
  ```

7. Submit a Pull Request with your changes against the `gh-pages` branch of
  the `KSP-KOS/KOS` fork, or developers with write permission may push to the
  upstream branch directly like this:
  ```
  C:\KOS-gh-pages>git push upstream gh-pages
  ```

8. You may test and review the documents on your own Github pages address:
  `https://[username].github.io/KOS`

9. Developers with write permission may push to the `KSP-KOS/KOS_DOC gh-pages`
  repository and branch to make the documentation publicly available at
  `https://KSP-KOS.github.io/KOS_DOC`.  This must be done from the command line,
  the repository is unable to accept pull requests from standard KOS
  repositories:
  ```
  C:\KOS-gh-pages>git push KOS_DOC gh-pages
  ```

# Generating Dash Docset

[Dash](https://kapeli.com/dash) is an offline API Documentation Browser and
Code Snippet Manager for macOS.

1. Install the [doc2dash][doc-2-dash] python package using `pipx` as
   recommended in the linked instructions.
2. Follow the instructions in "Getting started on Windows" adapting for macOS
   as necessary. (Basically the Linux instructions, but no need to install
   python).
3. After compiling the RTD docs, use doc2dash to create a docset. From the
   `doc/` folder run:
   ```
   pipx run doc2dash -n kOS -A gh-pages --icon=source/_images/kos_logo_konly.png
   ```
4. If this completes successfully and you have Dash installed, the docset will
   automatically open and be added to your docset library.

[doc-2-dash]: https://doc2dash.readthedocs.io/en/stable/installation.html
