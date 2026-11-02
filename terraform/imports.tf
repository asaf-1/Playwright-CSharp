# Adoption, not creation. The repository existed before this folder (gh created
# it in Phase 5), so the first apply takes ownership of it instead of trying to
# make a second one. The first plan reads "1 to import" for it, and the
# repository enters the state with no change from the import itself.
#
# Once the repository is in the state this block does nothing, so it stays as
# the record of where it came from. If the state were ever lost, a plan with it
# adopts the repository again instead of trying to create it.
#
# The ruleset has no import block: nothing protected main before, so Terraform
# creates it.

import {
  to = github_repository.this
  id = var.repository
}
