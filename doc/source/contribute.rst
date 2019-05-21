.. _contribute:

Contribute
==========

How to Contribute to this Project
---------------------------------

Do you know or are willing to learn C# and the **KSP** public API? Great, we could use your help! The source code for **kOS** is kept on `github`_ under https://github.com/KSP-KOS/KOS.

If you are already quite familiar with git and Github, the usual Github project development path is used:

  - Tell github to fork the main repository to your own github clone of it.
  - Clone your fork to your local computer.
  - On your local computer, make a branch from ``develop`` (don't edit ``develop`` directly) and make your changes in your branch.
  - Commit your changes and push them up to the same branch name on your github fork.
  - Make a Pull Request on Github to merge the branch from your fork to the ``develop`` branch of the main repository.
  - Wait for a developer to notice the Pull Request and start examining it.  There should be at the very least a comment letting you know it's being looked at, within a short time.  KSP-KOS is quite actively developed and someone should notice it soon.

  - Your request is more likely to get merged quickly if you make sure the ``develop`` branch you start from is always up to date with the latest upstream develop when you first split your branch from it.  If it takes a long time to finish, it may be a good idea to check again before making the Pull Request to see if there's been any new upstream ``develop`` changes, and merge them into your branch yourself so the rest of the team has an easier time deciphering the git diff output.

If you do know how to program on large projects and would like to contribute, but just aren't familiar with how git and Github do repository management, contact one of the developers and ask for help on how to get started, or ask to be added to the Slack channel first.

.. _github: https://github.com/KSP-KOS

Slack Chat
----------

There is an active Slack chat channel where the developers often discuss complex ideas before even mentioning them in a github issue or pull request.  If you wish to be added to this channel, please contact one of the main developers to ask to be invited to the channel.

How to get credited in the next Release
---------------------------------------

After version 0.19.0, Only people who opt-in to being credited will be mentioned in the release notes.

When you contribute to the development of the mod, if you wish to be named a certain way in the next release notes, then add your edit to the ``### Contributors`` section of the CHANGELOG.md file in your pull request.
In past releases we have tried to scour the github history to find all authors and it's a bit of a pain to pull the data together.  In future releases we will simply rely on this opt-in technique.  If you don't edit the file, you won't be opted-in to the contributors section.  This also avoids the hassle of having to ask everyone's permission in the last days of putting a release out, and then waiting for people's responses.

How to Edit this Documentation
------------------------------

This documentation was written using `reStructuredText`_ and compiled into HTML using `Sphinx`_ and the `Read The Docs Theme`_.

.. _reStructuredText: http://docutils.sourceforge.net/rst.html
.. _Sphinx: http://sphinx-doc.org/
.. _Read The Docs Theme: https://github.com/snide/sphinx_rtd_theme

To re-build the documentation tree locally, get a local clone of the project, `cd` into the `doc/` directory, and do these two commands:

.. highlight:: none

::

    make clean
    make html

.. highlight:: kerboscript

Note, this requires you set up Sphinx and Read-the-Docs first, as described in the links above.

This documentation system was first set up for us by Johann Goetz, to whom we are grateful:

.. _Johann Goetz: http://github.com/theodoregoetz

