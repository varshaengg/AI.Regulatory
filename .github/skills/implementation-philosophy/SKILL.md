---
name: implementation-philosophy
description: "Zen-like minimalism guiding implementation decisions: ruthless simplicity, library-vs-custom choices, surgical editing, assumption surfacing, and goal-driven verification. USE FOR: implementation approach, simplicity vs complexity, vertical slices, editing existing code, verification loops. DO NOT USE FOR: language coding standards, architecture design, deployment procedures."
license: MIT
metadata:
  author: GHC AI Accelerators
  version: "1.0.0"
---

# Implementation Philosophy

This skill outlines the core implementation philosophy and guidelines for software development projects. It serves as a central reference for decision-making and development approach.

## When to Activate

- "should I use a library or custom code?"
- "how simple should this be?"
- "implementation approach"
- "design philosophy"
- "vertical slice implementation"
- "when to add complexity?"
- Making implementation trade-off decisions
- Starting a new feature or module

## When NOT to Use

- Enforcing specific language coding standards (use language instructions)
- High-level architecture design (use `architecture-design` skill)
- Compliance/security reviews (use `compliance-enforcement` skill)
- Deployment execution (use `microsoft-engineering` skill)

## Core Philosophy

Embodies a Zen-like minimalism that values simplicity and clarity above all:

- **Wabi-sabi philosophy**: Embracing simplicity and the essential. Each line serves a clear purpose without unnecessary embellishment.
- **Occam's Razor thinking**: The solution should be as simple as possible, but no simpler.
- **Trust in emergence**: Complex systems work best when built from simple, well-defined components that do one thing well.
- **Present-moment focus**: Handle what's needed now rather than anticipating every possible future scenario.
- **Pragmatic trust**: Trust external systems enough to interact directly, handling failures as they occur.

## Assumptions & Uncertainty

Don't assume. Don't hide confusion. Surface tradeoffs.

- **State assumptions explicitly.** Before implementing, name what you're assuming. If uncertain, ask.
- **Present multiple interpretations.** If a request has more than one valid reading, present the options — don't pick silently.
- **Push back when warranted.** If a simpler approach exists, say so. If the request is overcomplicated, flag it.
- **Stop on confusion.** If something is unclear, name what's confusing and ask before writing code.

## Core Design Principles

### 1. Ruthless Simplicity

- **KISS principle taken to heart**: Keep everything as simple as possible, but no simpler
- **Minimize abstractions**: Every layer of abstraction must justify its existence
- **Start minimal, grow as needed**: Begin with the simplest implementation that meets current needs
- **Avoid future-proofing**: Don't build for hypothetical future requirements
- **Question everything**: Regularly challenge complexity in the codebase

### 2. Architectural Integrity with Minimal Implementation

- **Preserve key architectural patterns**: MCP for service communication, SSE for events, separate I/O channels
- **Simplify implementations**: Maintain pattern benefits with dramatically simpler code
- **Scrappy but structured**: Lightweight implementations of solid architectural foundations
- **End-to-end thinking**: Focus on complete flows rather than perfect components

### 3. Library vs Custom Code

#### The Evolution Pattern

- **Start simple**: Custom code for basic needs (20 lines handles it)
- **Growing complexity**: Switch to a library when requirements expand
- **Hitting limits**: Back to custom when you outgrow the library's capabilities

This isn't failure — it's natural evolution.

#### When Custom Code Makes Sense

- The need is simple and well-understood
- You want code perfectly tuned to your exact requirements
- Libraries would require significant "hacking" or workarounds
- The problem is unique to your domain

#### When Libraries Make Sense

- They solve complex problems you'd rather not tackle (auth, crypto, video encoding)
- They align well with your needs without major modifications
- The problem is well-solved with mature, battle-tested solutions
- Configuration alone can adapt them to your requirements

#### Making the Judgment Call

Ask yourself:

- How well does this library align with our actual needs?
- Are we fighting the library or working with it?
- Is the integration clean or does it require workarounds?
- Is the problem complex enough to justify the dependency?

#### Stay Flexible

Keep library integration points minimal and isolated so you can switch approaches when needed.

## Technical Implementation Guidelines

### API Layer

- Implement only essential endpoints
- Minimal middleware with focused validation
- Clear error responses with useful messages
- Consistent patterns across endpoints

### Database & Storage

- Simple schema focused on current needs
- Use TEXT/JSON fields to avoid excessive normalization early
- Add indexes only when needed for performance

### MCP Implementation

- Streamlined MCP client with minimal error handling
- Utilize FastMCP when possible
- Focus on core functionality without elaborate state management

### SSE & Real-time Updates

- Basic SSE connection management
- Simple resource-based subscriptions
- Direct event delivery without complex routing

### Event System

- Simple topic-based publisher/subscriber
- Direct event delivery without complex pattern matching
- Clear, minimal event payloads

### LLM Integration

- Direct integration with PydanticAI
- Minimal transformation of responses
- Handle common error cases only

### Editing Existing Code

Touch only what you must. Clean up only your own mess.

- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it — don't delete it.

When your changes create orphans:

- Remove imports, variables, and functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: every changed line should trace directly to the user's request.

## Development Approach

### Vertical Slices

- Implement complete end-to-end functionality slices
- Start with core user journeys
- Get data flowing through all layers early
- Add features horizontally only after core flows work

### Iterative Implementation

- 80/20 principle: Focus on high-value, low-effort features first
- One working feature > multiple partial features
- Validate with real usage before enhancing
- Be willing to refactor early work as patterns emerge

### Testing Strategy

- Emphasis on integration and end-to-end tests
- Manual testability as a design goal
- Focus on critical path testing initially
- Testing pyramid: 60% unit, 30% integration, 10% end-to-end

### Verification Protocol

Define success criteria. Loop until verified.

Transform tasks into verifiable goals:

- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:

```text
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

### Error Handling

- Handle common errors robustly
- Log detailed information for debugging
- Provide clear error messages to users
- Fail fast and visibly during development
- On unexpected failure: stop, report context, and let the caller decide — do not silently continue
- On missing dependency or file: STOP and report path and reason — do not work around it
- On partial success: save all completed work, report what was skipped, and add `## Partial Results Notice`

## Decision-Making Framework

When faced with implementation decisions, ask:

1. **Necessity**: "Do we actually need this right now?"
2. **Simplicity**: "What's the simplest way to solve this problem?"
3. **Directness**: "Can we solve this more directly?"
4. **Value**: "Does the complexity add proportional value?"
5. **Maintenance**: "How easy will this be to understand and change later?"

## Areas to Embrace Complexity

1. **Security**: Never compromise on security fundamentals
2. **Data integrity**: Ensure data consistency and reliability
3. **Core user experience**: Make the primary user flows smooth and reliable
4. **Error visibility**: Make problems obvious and diagnosable

## Areas to Aggressively Simplify

1. **Internal abstractions**: Minimize layers between components
2. **Generic "future-proof" code**: Resist solving non-existent problems
3. **Edge case handling**: Handle the common cases well first
4. **Framework usage**: Use only what you need from frameworks
5. **State management**: Keep state simple and explicit

## Remember

- It's easier to add complexity later than to remove it
- Code you don't write has no bugs
- Favor clarity over cleverness
- The best code is often the simplest

## Attribution

The Assumptions & Uncertainty, Editing Existing Code, and Verification Protocol sections are derived from [Andrej Karpathy's observations on LLM coding pitfalls](https://x.com/karpathy/status/2015883857489522876).

## Composability — How This Skill Connects

This skill is a **cross-cutting philosophy** consumed by implementation agents. It does not produce artifacts itself — it shapes how other agents make decisions.

### Consumed by

| Agent / Recipe                  | How it uses this skill                                                |
| ------------------------------- | --------------------------------------------------------------------- |
| `ceai-task-planner`             | Guides plan structure toward vertical slices and minimal abstractions |
| `ceai-task-researcher`          | Frames research toward simplest viable approach                       |
| `ceai-wisdom-miner`             | Provides vocabulary for principles and patterns extraction            |
| `ceai-database-architect`       | Aligns with "start simple, add structure as patterns emerge"          |
| `ceai-technical-architect`      | Simplicity-first design, library-vs-custom decisions                  |
| `ceai-specification`            | Ruthless simplicity to avoid over-specification                       |
| `ceai-adr`                      | Occam's Razor thinking and assumption surfacing in options analysis   |
| `recipe-spec-to-ship`           | Governs implementation phases with simplicity and verification loops  |
| `recipe-spec-to-ship-ship-only` | Surgical editing and verification loops during implementation         |
| `recipe-workflow-forge`         | Minimalism and vertical slices when building operational tools        |
| `recipe-arch-decision`          | Simplicity-first analysis and assumption surfacing                    |
| `recipe-idea-to-prototype`      | Start-simple principles for Experience Preview generation             |

### Related skills (handoff targets)

| Need                                                 | Route to                                             |
| ---------------------------------------------------- | ---------------------------------------------------- |
| Architecture-level decisions (HLD, options analysis) | `architecture-design` skill                          |
| Language-specific code conventions                   | Language instruction files (`.github/instructions/`) |
| Compliance, SDL, privacy gates                       | `compliance-enforcement` skill                       |
| Deployment patterns, OneBranch, Ev2                  | `microsoft-engineering` skill                        |
| Task plan execution and tracking                     | `task-tracking` skill                                |

### Shared conventions

- Artifact paths: all implementation artifacts go to `.copilot-tracking/` directories
- Decision escalation: when simplicity and security conflict, security wins (see "Areas to Embrace Complexity")
- Error escalation: fail fast, report context, let caller decide — never silently degrade
