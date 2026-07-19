---
name: task-tracking
description: "Multi-step task plan implementation with progressive tracking and change records. Guides systematic execution of plans in .copilot-tracking/ with quality standards and completion criteria. USE FOR: task plan implementation, progressive tracking, change record, copilot-tracking workflow, implement plan file, track implementation progress, release changes, plan execution, implementation workflow. DO NOT USE FOR: specific coding standards (use language instructions), architecture design (use architecture-design skill), compliance review (use compliance-enforcement skill)."
license: MIT
metadata:
  author: GHC AI Accelerators
  version: "1.0.0"
---

# Task Tracking Skill

Implements task plans located in `.copilot-tracking/plans/**` and `.copilot-tracking/details/**` with progressive tracking via change records in `.copilot-tracking/changes/**`.

## When to Activate

- "implement the task plan"
- "track implementation progress"
- "update the change record"
- Working with `.copilot-tracking/` files
- Plan execution and implementation workflows
- "release changes documentation"

## When NOT to Use

- Writing code to language standards (use language instructions)
- Architecture design (use `architecture-design` skill)
- Compliance review (use `compliance-enforcement` skill)
- Deployment execution (use `microsoft-engineering` skill)

## Core Implementation Process

### 1. Plan Analysis and Preparation

**MUST complete before starting implementation:**

- Read and fully understand the complete plan file (scope, objectives, all phases, checklists)
- Read and fully understand the corresponding changes file — re-read entire file if any parts are missing
- Identify all referenced files and examine them for context
- Understand current project structure and conventions

### 2. Systematic Implementation Process

For each unchecked task in the plan:

1. **Read** the entire details section from `.copilot-tracking/details/**`
2. **Fully understand** all implementation requirements
3. **Implement** with working code following workspace patterns
4. **Validate** implementation meets task requirements
5. **Mark complete** `[x]` in plan file
6. **Update changes file** — append to Added, Modified, or Removed sections
7. **Call out divergences** from plan/details with specific reasons

### 3. Implementation Quality Standards

Every implementation MUST:

- Follow existing workspace patterns and conventions (check `copilot/` folder)
- Implement complete, working functionality
- Include appropriate error handling and validation
- Use consistent naming conventions from the workspace
- Add necessary documentation for complex logic
- Ensure compatibility with existing systems

### 4. Continuous Progress and Validation

After each task:

1. Validate changes against task requirements
2. Fix any problems before moving to next task
3. Mark task `[x]` in plan file
4. Update changes file immediately
5. Continue to next unchecked task

### 5. Completion Criteria

Implementation is complete when:

- All plan tasks are marked `[x]`
- All specified files exist with working code
- All success criteria verified
- No implementation errors remain
- Changes file is fully updated with release summary

## Changes File Template

Create in `.copilot-tracking/changes/` with filename: `YYYYMMDD-task-description-changes.md`

```markdown
<!-- markdownlint-disable-file -->

# Release Changes: {{task name}}

**Related Plan**: {{plan-file-name}}
**Implementation Date**: {{YYYY-MM-DD}}

## Summary

{{Brief description of overall changes}}

## Changes

### Added

- {{relative-file-path}} - {{one sentence summary}}

### Modified

- {{relative-file-path}} - {{one sentence summary}}

### Removed

- {{relative-file-path}} - {{one sentence summary}}

## Release Summary

**Total Files Affected**: {{number}}

### Files Created ({{count}})

- {{file-path}} - {{purpose}}

### Files Modified ({{count}})

- {{file-path}} - {{changes-made}}

### Files Removed ({{count}})

- {{file-path}} - {{reason}}

### Dependencies & Infrastructure

- **New Dependencies**: {{list}}
- **Updated Dependencies**: {{list}}
- **Infrastructure Changes**: {{updates}}
- **Configuration Updates**: {{changes}}

### Deployment Notes

{{Any specific deployment considerations}}
```

## Implementation Workflow Summary

```
1. Read and understand plan file completely
2. Read and understand changes file completely
3. For each unchecked task:
   a. Read details from details markdown file
   b. Understand all requirements
   c. Implement with working code
   d. Validate against requirements
   e. Mark [x] in plan file
   f. Update changes file
   g. Note any divergences with reasons
4. Repeat until all tasks complete
5. Add final Release Summary to changes file
```

## Problem Resolution

When encountering issues:

- Document the specific problem clearly
- Try alternative approaches
- Use workspace patterns as fallback
- Continue with available information
- Note unresolved issues in the plan file

## Reference Gathering

- Focus on practical implementation examples
- Validate external sources contain usable patterns
- Adapt external patterns to match workspace conventions
- Follow workspace patterns first, external patterns second
