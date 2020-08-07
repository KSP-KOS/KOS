### PLEASE TAKE NOTE IF YOU EVER EDIT kOS.netkan

### IN A GIT MERGE OR PULL REQUEST.

### THIS IS IMPORTANT.

Do Not Ignore This Warning...

Unlike how every other source code file in a project typically
gets managed, THIS file (kOS.netkan) has a special rule that's
different and would normally be bad practice (but we have to
do it this way for CKAN to work right):

**If you don't follow what this README says, CKAN's crawler bot
will be populated with wrong information the CKAN people will
have to manually fix.**

#### For people making a pull request that edits kOS.netkan:

This warning is for people who are thinking along these lines:

*"I am writing a Pull Request that makes calls into the API of this
other mod.  So, I think that means I should make this PR contain
an edit to kOS.netkan to describe that mod dependency, right?"*.

**Do Not DO THAT**.

*Do Not Put The Edit Of kOS.netkan In That Same Pull Request, even
though that would normally seem to be correct good practice.*

**Put it in a separate pull request of its own, and name
that branch something matching the pattern "netkan_issue_NNNN".**

You can make note of it in the original pull request, but keep it
separate so it can be merged independantly.

#### For people who are reviewing a pull request that edits kOS.netkan:

Read the above section first about how to make a pull request like this.

Make sure you never merge an edit to kOS.version that's in the same PR
as other code edits.  That should be grounds for rejecting a pull request
during review and asking it to be changed to follow this rule (move
the kOS.version edit to its own separate PR.)

**NEVER MERGE THE kOS.version PR's until much later, as part of making
the next release.  (Do it AFTER the ZIP is uploaded to the releases page.)**

The order of events NEEDS to be this or else CKAN's crawler bot will
get things wrong:

FIRST, we make a new release and gets it on the github releases page,
with the ZIP file uploaded for the release.

SECOND, we edit the kOS.version file in the master branch, only AFTER
that new release ZIP exists on the releases page.

### Why?

Because CKAN's crawler bot wakes up every half hour and *assumes* the
kOS.version file it sees in the master branch goes with the most recent
release ZIP file it sees in the releases page.  If you update the
file on the master branch FIRST, when you copy develop over to master,
Before you've cut the new release, then it falsely assumes the settings
in that kOS version file go with the previous kOS release.  (i.e. if you
add a new dependency, it will falsely associate that dependency with the
previous release of kOS, even though that's incorrect.)

The only sure way to prevent this is to make sure you didn't update
kOS.version in master until after a release ZIP was made.  That's
why any PRs to change the kOS.version must wait until after the release
ZIP got put there for the CKAN bot to see.

