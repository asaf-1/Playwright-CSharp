output "repository_url" {
  description = "The repository on GitHub."
  value       = github_repository.this.html_url
}

output "ruleset_id" {
  description = "GitHub's ID for the Repo-Main ruleset on main."
  value       = github_repository_ruleset.main.ruleset_id
}

output "required_checks" {
  description = "The jobs a pull request must pass before it can merge."
  value       = local.required_checks
}
