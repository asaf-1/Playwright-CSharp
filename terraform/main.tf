# Playwright-CSharp's settings on GitHub, as code.
#
# This file is the record of how the repository is set up, and Terraform makes
# GitHub match it. A change to a setting or to the rule on main goes through a
# pull request, like a change to code.
#
# The repository itself is adopted with the import block in imports.tf, never
# created here. The ruleset on main is new: nothing protected main before this
# folder.
#
# Two things to fear in this folder. Terraform removes what the file doesn't
# mention: a ruleset left out is a ruleset deleted, and with it the rule that
# tests must pass before a merge. And `destroy` would delete the repository.
# prevent_destroy on both resources turns either into an error instead.

provider "github" {
  owner = var.owner
  token = var.pwcs_github_token
}

locals {
  # The jobs a pull request must pass before it can merge: the three jobs of
  # .github/workflows/pr-tests.yml, by their names. Rename a job there and here
  # together, or no pull request can merge.
  required_checks = ["NUnit tests", "xUnit tests", "Bug hunt"]

  # The app the checks must come from. 15368 is GitHub Actions: pinning it means
  # a status called "NUnit tests" posted by anything else doesn't count.
  github_actions_app = 15368
}

# The repository: description, topics, which merge buttons exist, and the
# housekeeping switches. Imported, never created here.
resource "github_repository" "this" {
  name        = var.repository
  description = "C# + Playwright for .NET tests for Playground Bank, on NUnit and xUnit, with CI on GitHub Actions"
  visibility  = "public"

  # The repository's tags on GitHub, so it shows up in searches for them.
  topics = [
    "csharp",
    "dotnet",
    "github-actions",
    "nunit",
    "playwright",
    "terraform",
    "test-automation",
    "xunit",
  ]

  has_issues      = true
  has_projects    = true
  has_wiki        = true
  has_discussions = false
  is_template     = false

  # All three merge buttons stay. Auto-merge lets a pull request merge itself
  # once its checks pass, and GitHub deletes the branch after a merge.
  allow_merge_commit     = true
  allow_squash_merge     = true
  allow_rebase_merge     = true
  allow_auto_merge       = true
  allow_update_branch    = false
  delete_branch_on_merge = true

  squash_merge_commit_title   = "COMMIT_OR_PR_TITLE"
  squash_merge_commit_message = "COMMIT_MESSAGES"
  merge_commit_title          = "MERGE_MESSAGE"
  merge_commit_message        = "PR_TITLE"

  web_commit_signoff_required = false

  # Belt and braces. prevent_destroy makes any plan that would destroy the
  # repository an error. If that line is ever removed, archive_on_destroy makes a
  # destroy archive the repository instead of deleting it.
  archive_on_destroy = true

  lifecycle {
    prevent_destroy = true
  }
}

# The rule on main. A change reaches main only through a pull request whose
# three test jobs passed, and main can't be deleted or force-pushed. No bypass
# actors: not even the owner skips it.
resource "github_repository_ruleset" "main" {
  name        = "Repo-Main"
  repository  = github_repository.this.name
  target      = "branch"
  enforcement = "active"

  conditions {
    ref_name {
      include = ["~DEFAULT_BRANCH"]
      exclude = []
    }
  }

  rules {
    deletion         = true
    non_fast_forward = true

    # Zero required approvals: one person works here, and the gate is the test
    # run, not a review.
    pull_request {
      required_approving_review_count   = 0
      dismiss_stale_reviews_on_push     = false
      require_code_owner_review         = false
      require_last_push_approval        = false
      required_review_thread_resolution = false
      allowed_merge_methods             = ["merge", "squash", "rebase"]
    }

    required_status_checks {
      # Not strict: a pull request needn't be updated with the newest main
      # before it merges.
      strict_required_status_checks_policy = false
      do_not_enforce_on_create             = false

      dynamic "required_check" {
        for_each = local.required_checks

        content {
          context        = required_check.value
          integration_id = local.github_actions_app
        }
      }
    }
  }

  lifecycle {
    prevent_destroy = true
  }
}
