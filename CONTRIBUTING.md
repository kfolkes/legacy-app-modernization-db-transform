# Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

---

## Local development

```bash
git clone https://github.com/Azure-Samples/app-modernization-lab
cd app-modernization-lab
code .            # Reopen in devcontainer
```

The devcontainer installs .NET 10, Java 21, Docker, Azure CLI, AppCAT, and OpenRewrite — everything required for both flows.

## Repo structure

See the [README.md](README.md) for the full layout. The most important folders for contributors:

- `.github/skills/` — single source of truth for the modernization flows
- `.github/prompts/` — one-click `/dotnet.modernize` and `/java.modernize` entry points
- `.github/agents/` — agent definitions and tool access
- `scripts/` — headless helpers for CI and BYO mode

## Adding a new flow

1. Add a new skill under `.github/skills/<flow-name>-flow/SKILL.md` mirroring the 8-phase contract.
2. Add a one-click prompt under `.github/prompts/<flow-name>.modernize.prompt.md`.
3. Add evidence-doc folder `docs/<flow-name>/`.
4. Add CI smoke test in `.github/workflows/`.
5. Update the README table.

## Testing

```bash
# .NET
bash scripts/dotnet/build.sh
bash scripts/dotnet/test.sh

# Java
bash scripts/java/build.sh
bash scripts/java/test.sh
```
