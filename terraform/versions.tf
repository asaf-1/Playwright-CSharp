terraform {
  required_version = "~> 1.16"

  # The record of what Terraform owns (the state) lives in HCP Terraform, not on
  # this laptop: the settings this folder manages exist once and protect main,
  # so the record has to outlive any one machine. HCP keeps it with a version
  # history and a lock.
  #
  # The workspace runs in local execution mode: Terraform runs on this PC, with
  # the GitHub token from this PC, and only the state is stored in HCP. Nothing
  # in the state is secret; the token is an ephemeral variable, so it is never
  # written to the state or to a plan file.
  cloud {
    organization = "Asaf-Platform"

    workspaces {
      name = "playwright-csharp-github"
    }
  }

  required_providers {
    # Manages the repository's settings and the ruleset on main. The exact build
    # is pinned in .terraform.lock.hcl for Windows and Linux.
    github = {
      source  = "integrations/github"
      version = "~> 6.13"
    }
  }
}
