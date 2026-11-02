# The token is the only secret this folder touches, and it never reaches a file.
# Terraform reads it from the TF_VAR_pwcs_github_token environment variable, by
# name. Not TF_VAR_github_token: that name already holds Agents-Playground's own
# token, which its Terraform folder reads, so this repo must never overwrite it.
# `sensitive` keeps it off the screen; `ephemeral` keeps it out of the plan file
# and the state too.
variable "pwcs_github_token" {
  description = "Fine-grained personal access token for Playwright-CSharp only, with Administration read/write. Set TF_VAR_pwcs_github_token. Never write it in a file."
  type        = string
  sensitive   = true
  ephemeral   = true

  # Only a fine-grained token is accepted. A classic token (ghp_) or the gh
  # login token (gho_) can't be limited to one repository, so it would carry
  # power over every repository on the account.
  validation {
    condition     = startswith(var.pwcs_github_token, "github_pat_")
    error_message = "pwcs_github_token must be a fine-grained token (it starts with github_pat_), limited to Playwright-CSharp. Classic and gh login tokens are refused because they reach every repository."
  }
}

variable "owner" {
  description = "The GitHub account that owns the repository."
  type        = string
  default     = "asaf-1"
}

variable "repository" {
  description = "The repository whose settings this folder manages. It already exists and is imported, never created here."
  type        = string
  default     = "Playwright-CSharp"
}
