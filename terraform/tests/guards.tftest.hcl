# Tests for terraform/, run by `terraform test` (on this PC and in CI).
#
# Why these exist: this folder's job is to keep a few settings exactly so, and a
# real plan needs the GitHub token and the HCP login, which CI doesn't have.
# These pin the settings the rest of the repo depends on, so an edit that
# loosens one has to come here and say so.
#
# Nothing here talks to GitHub. The provider is mocked, every run is a plan, and
# the token is a made-up value with the right prefix.

mock_provider "github" {}

# imports.tf adopts the repository, and a mocked provider can't import. This
# tells the test to treat it as already there; every argument written in
# main.tf keeps its real value, which is all the assertions read.
override_resource {
  target = github_repository.this
}

variables {
  pwcs_github_token = "github_pat_made_up_for_tests_only"
}

run "main_stays_protected" {
  command = plan

  assert {
    condition     = github_repository_ruleset.main.enforcement == "active"
    error_message = "The Repo-Main ruleset is no longer active. \"evaluate\" and \"disabled\" protect nothing."
  }

  assert {
    condition     = github_repository_ruleset.main.conditions[0].ref_name[0].include == tolist(["~DEFAULT_BRANCH"])
    error_message = "The ruleset no longer targets the default branch."
  }

  assert {
    condition     = github_repository_ruleset.main.rules[0].deletion == true && github_repository_ruleset.main.rules[0].non_fast_forward == true
    error_message = "The ruleset must keep blocking deletion and force-pushes on main."
  }

  assert {
    condition     = length(github_repository_ruleset.main.rules[0].pull_request) == 1
    error_message = "The ruleset must keep requiring a pull request to change main."
  }

  assert {
    condition     = length(github_repository_ruleset.main.bypass_actors) == 0
    error_message = "The ruleset gained a bypass actor. Nobody skips the tests here, not even the owner."
  }
}

run "all_three_test_jobs_stay_required" {
  command = plan

  assert {
    condition     = toset([for c in github_repository_ruleset.main.rules[0].required_status_checks[0].required_check : c.context]) == toset(["NUnit tests", "xUnit tests", "Bug hunt"])
    error_message = "The required checks must be exactly NUnit tests, xUnit tests and Bug hunt, the three jobs of pr-tests.yml."
  }

  assert {
    condition     = alltrue([for c in github_repository_ruleset.main.rules[0].required_status_checks[0].required_check : c.integration_id == 15368])
    error_message = "A required check lost its GitHub Actions app ID (15368). Without it, any status with the same name would satisfy the rule."
  }
}

run "the_repository_stays_public_and_tidy" {
  command = plan

  assert {
    condition     = github_repository.this.visibility == "public"
    error_message = "The repository is no longer public. Rulesets on a private repository need a paid plan."
  }

  assert {
    condition     = github_repository.this.delete_branch_on_merge == true
    error_message = "delete_branch_on_merge is off, so merged branches would pile up on GitHub."
  }
}

# A token that reaches every repository must be refused before anything is
# planned.
run "a_classic_token_is_refused" {
  command = plan

  variables {
    pwcs_github_token = "ghp_classic_token_made_up_for_tests"
  }

  expect_failures = [
    var.pwcs_github_token,
  ]
}

run "the_gh_login_token_is_refused" {
  command = plan

  variables {
    pwcs_github_token = "gho_login_token_made_up_for_tests"
  }

  expect_failures = [
    var.pwcs_github_token,
  ]
}
