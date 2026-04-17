# Sip & Sync — .NET App Modernization with GitHub Copilot Agents

## Video Details

| Field | Value |
|---|---|
| **Show** | Sip & Sync |
| **Title** | "From Legacy .NET to Microservices in Minutes — with GitHub Copilot Agents" |
| **Duration** | ~10 minutes |
| **Hosts** | **Priyanka** (Host / Interviewer) · **Krystal** (Guest / Demo Lead) |
| **Channel** | Microsoft Developer (YouTube) |
| **Format** | Conversational two-person — casual energy, drinks on desk, screen-share demo segments |
| **Demo App** | eShopModernizing (.NET Framework 4.7.2 WebForms → .NET 10 microservices) |

---

## Cold Open — Hook (0:00–0:30)

**[ON CAMERA — Both at desk with drinks, relaxed energy]**

**PRIYANKA:**
> Hey everyone, welcome back to Sip & Sync! I'm Priyanka, and today I have Krystal with me. Krystal, you told me you can take an old .NET Framework app — like, 2017 old — and modernize it into microservices with ONE click. I need to see this.

**KRYSTAL:**
> *(laughs)* One click to START. But yeah — we're going from a .NET Framework 4.7.2 WebForms app with business logic hiding inside SQL Server triggers to a full microservice architecture on .NET 10 with Kotlin and React BFF layers, all orchestrated by .NET Aspire. And GitHub Copilot agents do the heavy lifting.

**PRIYANKA:**
> Okay, I have my coffee, I'm ready. Show me.

---

## Segment 1 — The Problem (0:30–2:00)

**[SCREEN SHARE — VS Code with legacy workspace open]**

**KRYSTAL:**
> So here's what we're starting with. This is eShopModernizing — it's a Microsoft reference app, but it mirrors what we see in real enterprise shops. .NET Framework 4.7.2, ASP.NET WebForms, Entity Framework 6, Autofac for dependency injection, log4net for logging, and 44 NuGet packages.

**[Opens `Global.asax.cs`]**

**KRYSTAL:**
> Look at this — Autofac wired through HTTP modules, route config, bundle config. This is the ceremony that WebForms developers lived with for years.

**PRIYANKA:**
> And let me guess — there's business logic hiding in the database too?

**KRYSTAL:**
> Exactly.

**[Opens `Triggers.sql`]**

**KRYSTAL:**
> Three SQL Server triggers. This one logs every catalog change. This one fires a stock alert when inventory drops. This one tracks price history. The APPLICATION doesn't even know these exist. They fire silently on every write.

**PRIYANKA:**
> So you can't test them, you can't observe them, and if something breaks…

**KRYSTAL:**
> You find out in production. And that's before we talk about security — there's a high-severity CVE in Newtonsoft.Json, connection strings sitting in plaintext in `Web.config`, and zero authorization. Anyone with the URL can access anything.

**PRIYANKA:**
> Okay, this is painful. How do you even start fixing this?

---

## Segment 2 — One-Click Modernization (2:00–3:30)

**KRYSTAL:**
> This is where it gets fun. I have three reusable assets in my `.github` folder — a skill file that defines an 8-phase modernization pipeline, an agent that reads the skill and has access to assessment and security tools, and a one-click prompt.

**[Opens Copilot Chat, types the prompt]**

**KRYSTAL:**
> I type one command — `/dotnet10.modernize.agent-upgrades-v1` — point it at the legacy solution path, and hit enter.

**[Shows the agent starting work]**

**KRYSTAL:**
> Behind the scenes, the agent installs Microsoft's AppCAT assessment tool, auto-detects the framework version from the project files — we never hardcode that — and starts running Phase 1 through Phase 8.

**PRIYANKA:**
> Wait — it detects the version automatically?

**KRYSTAL:**
> Yeah. It reads the csproj, packages.config, and solution metadata. Could be .NET Framework 3.5, could be 4.8 — it adapts. And the target is always .NET 10.

**PRIYANKA:**
> And this same prompt works on ANY .NET Framework app?

**KRYSTAL:**
> Same skill, same agent, same prompt. Point it at a different solution folder and the pipeline runs the same way. The findings are specific to the codebase, but the process is identical.

---

## Segment 3 — Assessment + Security Baseline (3:30–5:00)

**[SCREEN SHARE — Generated docs]**

**KRYSTAL:**
> So the pipeline already ran ahead of time — let me walk you through what it found. Phase 1, Legacy Assessment —

**[Opens `01-legacy-assessment.md`]**

**KRYSTAL:**
> AppCAT detected the WebForms project, flagged compatibility issues, and estimated the migration effort in story points. It found WebForms-specific blockers — ASPX pages, ViewState, Session, code-behind — plus file-based logging and machine-name dependencies that won't work in containers.

**PRIYANKA:**
> So you get REAL numbers, not guesswork.

**KRYSTAL:**
> Exactly — data-driven planning. Now Phase 2, Security Baseline —

**[Opens `02-security-baseline.md`]**

**KRYSTAL:**
> 44 packages scanned. Newtonsoft.Json 12.0.1 — HIGH severity CVE for denial of service. Autofac.Web — no upgrade path at all, the entire DI model has to change. Application Insights 2.x — deprecated, needs to move to OpenTelemetry. Plus the connection strings in plaintext and those database triggers running with elevated permissions.

**PRIYANKA:**
> So you have a clear "before" picture — security baseline BEFORE any code changes.

**KRYSTAL:**
> That's the point. We measure before, we modernize, then we measure again. The delta tells the story.

---

## Segment 4 — The Star Moment: Triggers → Domain Events (5:00–7:30)

**[SCREEN SHARE — Side by side: Triggers.sql vs modernized code]**

**PRIYANKA:**
> Okay, this is the part I'm most curious about. Those silent SQL triggers — what happens to them?

**KRYSTAL:**
> Every trigger becomes a domain event in .NET 10.

**[Shows the mapping table]**

**KRYSTAL:**
> The audit log trigger becomes an EF Core `SaveChangesInterceptor` — it intercepts every entity change and writes structured audit logs. No more hidden side-effects.

> The stock alert trigger becomes a `StockBelowThresholdEvent` — a proper domain event published through MediatR to Azure Service Bus. Downstream services subscribe and react.

> And the price history trigger becomes a `PriceChangedEvent` streamed through Azure Event Hubs to a reporting service for analytics.

**PRIYANKA:**
> So instead of invisible database magic, you have explicit, observable events flowing through messaging infrastructure.

**KRYSTAL:**
> And testable! In the legacy app, you could NOT test the stock alert trigger without a live database. Now —

**[Opens test file]**

**KRYSTAL:**
> — you have unit tests like `AdjustStock_StockDropsBelowThreshold_SetsOnReorderTrue`. That's the business rule from the trigger, now tested with zero infrastructure.

**PRIYANKA:**
> How many tests did the pipeline generate?

**KRYSTAL:**
> 29 unit tests covering all the stored procedure and trigger business rules. All passing.

**PRIYANKA:**
> That's the moment enterprise developers lean forward, right?

**KRYSTAL:**
> Every time. *(laughs)* "You mean I can actually test this now?" — yes, yes you can.

---

## Segment 5 — The Full Architecture (7:30–9:00)

**[SCREEN SHARE — Architecture diagram from Phase 7]**

**KRYSTAL:**
> So here's the big picture. We didn't just upgrade the .NET code — we decomposed the monolith into four tracks.

> **Track A** — Database: SQL Server triggers migrated to domain events, with a PostgreSQL option using dual-provider EF Core.

> **Track B** — .NET 10 Microservices: four bounded-context services — Catalog, Inventory, Image, and Reporting — with three layers of authorization: Azure RBAC, ASP.NET Core policies, and OPA policy enforcement through a sidecar.

> **Track C** — A Kotlin BFF built with Ktor for mobile and partner APIs, with circuit breakers and coroutine-based async.

> **Track D** — A React BFF using Next.js 15 Server Components. The server-side rendering IS the BFF layer — no separate API gateway needed.

**[Opens Aspire orchestrator Program.cs]**

**KRYSTAL:**
> And .NET Aspire wires it all together. One file — PostgreSQL, Redis, Service Bus, Event Hubs, four microservices, two BFFs, and an OPA sidecar. You run `dotnet run` and the Aspire dashboard shows everything.

**PRIYANKA:**
> And the security after all this?

**KRYSTAL:**
> Phase 5 — security re-scan. Zero known CVEs in the modernized stack. Connection strings eliminated from config files. Three layers of authorization where there was none before. CSP headers on every response from the React BFF.

**PRIYANKA:**
> From one high-severity CVE and zero auth to zero CVEs and three layers of security. That's a story that sells itself.

---

## Segment 6 — Wrap-Up + CTA (9:00–10:00)

**[ON CAMERA — Both back on screen]**

**PRIYANKA:**
> So let me recap — you took a 2017-era WebForms app with hidden database triggers, ran one prompt, and got a full microservice architecture with .NET 10, Kotlin and React BFF layers, event-driven messaging, Aspire orchestration, and zero CVEs. All with auditable evidence at every step.

**KRYSTAL:**
> And the best part — it's three reusable assets. The skill, the agent, and the prompt. Point them at your next .NET Framework app and the pipeline runs the same way. Same 8 phases, same quality gates, different findings.

**PRIYANKA:**
> If you have .NET Framework apps that need modernizing — and let's be honest, who doesn't — check out the links in the description. We have the full workspace, the templates, and the skill file you can use today.

**KRYSTAL:**
> And if your team has business logic buried in stored procedures or triggers — that's the most painful legacy pattern to deal with manually. The agent handles it and gives you testable code on the other side.

**PRIYANKA:**
> Thanks for sipping and syncing with us! Drop a comment below if you want to see us go deeper on any of the four tracks — database migration, .NET microservices, Kotlin BFF, or the React Server Components piece. And don't forget to subscribe. See you next time!

**KRYSTAL:**
> See you next time! *(waves)*

**[END CARD — Links to repo, skill file, Microsoft Developer channel]**

---

## Production Notes

### B-Roll / Screen Share Segments Needed
| Timestamp | Visual |
|---|---|
| 0:30–2:00 | VS Code: `Global.asax.cs`, `Triggers.sql`, `Web.config`, `packages.config` |
| 2:00–3:30 | Copilot Chat: one-click prompt execution, agent working |
| 3:30–5:00 | Generated docs: `01-legacy-assessment.md`, `02-security-baseline.md` |
| 5:00–7:30 | Side-by-side: `Triggers.sql` ↔ domain event code, test file, test results |
| 7:30–9:00 | Architecture diagram (Mermaid), Aspire `Program.cs`, security comparison table |

### Description Box Links
- GitHub repo: `github.com/kfolkes/sbux-appmod-demo`
- Skill file: `.github/skills/dotnet10-modernization-customer/SKILL.md`
- .NET Aspire docs: `learn.microsoft.com/dotnet/aspire`
- GitHub Copilot: `github.com/features/copilot`

### Thumbnail Concept
- Split screen: left = old WebForms code (dark/red tint), right = Aspire dashboard (bright/green)
- Text overlay: "Legacy .NET → Microservices in 1 Click"
- Both hosts with coffee mugs

### Tags
`GitHub Copilot`, `.NET modernization`, `.NET 10`, `ASP.NET WebForms`, `microservices`, `Kotlin BFF`, `React Server Components`, `.NET Aspire`, `app modernization`, `legacy migration`, `domain events`, `EF Core`, `Azure`, `Sip and Sync`
