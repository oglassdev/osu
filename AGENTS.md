# Agent Guidelines

## Keep it simple

- Prefer the smallest change that solves the problem.
- Do not add abstractions, helpers, or error handling for unlikely edge cases.
- Match the complexity level of the surrounding code.

## Follow existing style

Before writing new code, read nearby files and understand local conventions:

- Check `.editorconfig` and any linter/analyzer configuration.
- Match naming, formatting, import order, and patterns used in the same module.
- Reuse existing utilities and abstractions instead of reimplementing them.

## Format and lint

After making changes, run the project's formatter and static analysis. Use `osu.Desktop.slnf` — the repo has multiple solution files.

Only format files you changed — do not run the formatter across the whole solution.

```bash
dotnet tool restore
dotnet format osu.Desktop.slnf --include path/to/changed/file.cs
dotnet build -c Debug -warnaserror osu.Desktop.slnf -p:EnforceCodeStyleInBuild=true
dotnet CodeFileSanity
```

For the full static-analysis pass (build first, then run):

```bash
dotnet build -c Debug -warnaserror osu.Desktop.slnf
./InspectCode.sh
```

Fix any warnings or errors before finishing.

## Build and test

Before considering a feature or fix complete:

```bash
dotnet build -c Debug -warnaserror osu.Desktop.slnf
dotnet test osu.Game.Tests   # or the test project matching your change
```

Target a specific test when iterating:

```bash
dotnet test osu.Game.Tests --filter "FullyQualifiedName~YourTestName"
```

Add or update tests when the change warrants coverage.

## Git commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/) style:

```
<type>: <short description>
```

Common types:

- `feat:` — new feature
- `fix:` — bug fix
- `refactor:` — code change that neither fixes a bug nor adds a feature
- `test:` — adding or updating tests
- `chore:` — maintenance (deps, tooling, etc.)
- `docs:` — documentation only

Keep the subject line concise and in the imperative mood. Add a body only when the why is not obvious from the subject.
