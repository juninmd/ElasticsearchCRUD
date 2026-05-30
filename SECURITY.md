# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | :white_check_mark: |

## Reporting a Vulnerability

Security issues should be reported privately via the repository's security advisory
tab at https://github.com/juninmd/ElasticsearchCRUD/security/advisories.

Please **do not** open public issues for security vulnerabilities.

## Security Best Practices

### Secrets Management
- Connection strings, API keys, and passwords must **never** be committed to the repository
- Use environment variables or secure secret stores (e.g. GitHub Secrets, Azure Key Vault)
- The `.gitignore` blocks common secret file patterns (`.env`, `*.key`, `*.pem`, `secrets/`, etc.)
- Config files with sample data are excluded from secret scanning via `.gitleaks.toml` allowlist

### Secret Scanning
This repository uses [Gitleaks](https://github.com/gitleaks/gitleaks) to scan for
accidentally committed secrets. The scan runs:
- On every push to `main`/`master`
- Weekly on Mondays via scheduled workflow

Any detected secrets must be treated as compromised and rotated immediately.

### Dependency Updates
Automated dependency updates are managed via Dependabot:
- **NuGet packages**: checked weekly
- **GitHub Actions**: checked weekly

Review and merge Dependabot PRs promptly, especially security-related ones.

### Supply Chain Security
- Pin dependency versions in `packages.config`
- Review all dependency changes before merging
- Enable GitHub Dependabot alerts in the repository settings

### Least Privilege
- CI/CD tokens use minimum required permissions
- Release workflow permissions are scoped per job
- Avoid using personal access tokens in automation

### Infrastructure
- Always use HTTPS for Elasticsearch connections
- Validate TLS certificates in production
- Do not expose Elasticsearch to the public internet without authentication
