# Cursor Rules - Optimized Structure

This directory contains optimized Cursor rules that follow the latest specification and best practices, including an **auto-improvement system** that automatically detects patterns and suggests rule enhancements.

## Rule Structure

### Core Rules
- **[cursor_rules.mdc](mdc:.cursor/rules/cursor_rules.mdc)** - Main specification and formatting guidelines
- **[self_improve.mdc](mdc:.cursor/rules/self_improve.mdc)** - Continuous improvement and pattern recognition
- **[auto_improvement.mdc](mdc:.cursor/rules/auto_improvement.mdc)** - **NEW**: Automatic rule improvement system

### Domain-Specific Rules
- **[authentication_best_practices.mdc](mdc:.cursor/rules/authentication/authentication_best_practices.mdc)** - Security and authentication guidelines
- **[hotchocolate_best_practices.mdc](mdc:.cursor/rules/hotchocolate/hotchocolate_best_practices.mdc)** - GraphQL and HotChocolate best practices

### Workflow Rules
- **[development_workflow.mdc](mdc:.cursor/rules/development_workflow.mdc)** - Development process and project organization

### Automation Tools
- **[pattern_detector.ps1](mdc:.cursor/rules/pattern_detector.ps1)** - **NEW**: PowerShell script for automatic pattern detection

## 🚀 Auto-Improvement System

### What It Does
The auto-improvement system automatically detects when the same patterns appear multiple times in your codebase and suggests rule improvements:

- **Pattern Detection**: Identifies recurring code patterns across 3+ files
- **Rule Suggestions**: Automatically generates new rule suggestions
- **Quality Metrics**: Assesses rule completeness and effectiveness
- **Cross-Referencing**: Maintains rule relationships and dependencies

### How It Works
1. **Automatic Detection**: Monitors code for recurring patterns
2. **Threshold Triggers**: Suggests improvements when patterns appear 3+ times
3. **Rule Generation**: Creates comprehensive rule suggestions
4. **Quality Assessment**: Evaluates rule effectiveness and completeness

### Pattern Categories Detected
- **Architectural Patterns**: Service registration, DI, middleware
- **Code Quality**: Error handling, logging, validation
- **Performance**: Database queries, caching, async patterns
- **Security**: Authentication, authorization, input validation

### Usage
```powershell
# Run pattern detection (PowerShell)
.\pattern_detector.ps1 -ProjectRoot . -PatternThreshold 3

# Customize detection
.\pattern_detector.ps1 -PatternThreshold 5 -OutputFile "custom_analysis.json"
```

## Rule Format

All rules follow this standardized format:

```markdown
---
description: Clear, one-line description of what the rule enforces
globs: path/to/files/*.ext, other/path/**/*
alwaysApply: boolean
---

# Rule Title

## Section

- **Key Point in Bold**
  - Sub-points with details
  - Examples and explanations
```

## Usage

1. **Always Apply Rules**: These rules are automatically applied to all relevant files
2. **Domain Rules**: Apply to specific file types or directories
3. **Cross-References**: Rules reference each other for consistency
4. **Auto-Improvement**: Rules automatically improve based on detected patterns

## Optimization Benefits

- **Eliminated Duplication**: Consolidated similar rules into comprehensive files
- **Standardized Format**: All rules follow the same structure and metadata
- **Proper Cross-Referencing**: Rules link to each other for maintainability
- **Focused Scope**: Each rule file has a clear, specific purpose
- **Latest Specification**: Follows current Cursor rules best practices
- **🚀 Auto-Improvement**: Rules automatically evolve based on codebase patterns

## Maintenance

- **Automatic**: The auto-improvement system detects and suggests updates
- **Manual**: Update rules when new patterns emerge
- **Examples**: Add examples from actual codebase
- **Cross-Reference**: Rules automatically maintain references
- **Quality**: Follow the self-improvement guidelines in [self_improve.mdc](mdc:.cursor/rules/self_improve.mdc)

## Auto-Improvement Workflow

1. **Pattern Detection**: System monitors code for recurring patterns
2. **Threshold Trigger**: When pattern appears 3+ times, system suggests improvement
3. **Rule Suggestion**: Generates comprehensive rule or update suggestion
4. **Quality Assessment**: Evaluates rule completeness and effectiveness
5. **Implementation**: Apply approved rule improvements
6. **Continuous Monitoring**: System continues to detect new patterns

## Backup

A complete backup of all rules is available in `cursor-rules-backup-YYYYMMDD-HHMMSS.zip` in the project root.

## Next Steps

1. **Run Pattern Detection**: Execute `.\pattern_detector.ps1` to analyze your codebase
2. **Review Suggestions**: Check generated rule suggestions in `pattern_analysis.json`
3. **Implement Improvements**: Apply high-priority rule enhancements
4. **Monitor Quality**: Use the auto-improvement system for continuous enhancement
