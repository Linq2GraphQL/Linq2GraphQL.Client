This folder holds CHECKED-IN GENERATED OUTPUT. Do not edit Generated/ by hand.

To refresh it after a template or test-schema change, run from the repo root:

    ./scripts/regenerate-test-clients.ps1

The script boots both test servers over plain HTTP, runs the generator against
them with the flags that produced this output, and writes the result back here.
Review the diff and commit it.

The "Test clients up to date" CI job runs the same script with -Check and fails
if the committed output no longer matches what the generator produces.
