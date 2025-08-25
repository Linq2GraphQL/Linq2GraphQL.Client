# Pattern Detector for Cursor Rules Auto-Improvement
# This script analyzes the codebase for recurring patterns and suggests rule improvements

param(
    [string]$ProjectRoot = ".",
    [int]$PatternThreshold = 3,
    [string]$OutputFile = "pattern_analysis.json"
)

# Initialize pattern storage
$patterns = @{}
$filePatterns = @{}

# Common pattern types to detect
$patternTypes = @{
    "MassTransit" = @(
        "AddMassTransit",
        "UsingRabbitMq",
        "ConfigureEndpoints"
    )
    "HealthChecks" = @(
        "AddHealthChecks",
        "AddDbContextCheck",
        "MapHealthChecks"
    )
    "Authentication" = @(
        "AddAuthentication",
        "AddAuthorization",
        "UseAuthentication"
    )
    "DependencyInjection" = @(
        "AddScoped",
        "AddSingleton",
        "AddTransient",
        "services\.Add"
    )
    "ErrorHandling" = @(
        "try\s*\{",
        "catch\s*\(Exception",
        "LogError",
        "StatusCode\(500"
    )
    "Logging" = @(
        "_logger\.Log",
        "LogInformation",
        "LogWarning",
        "LogError"
    )
    "Database" = @(
        "UseNpgsql",
        "UseSqlServer",
        "AddDbContext",
        "DbContext"
    )
    "GraphQL" = @(
        "AddGraphQLServer",
        "AddQueryType",
        "AddMutationType",
        "AddType"
    )
}

# Function to detect patterns in a file
function Detect-PatternsInFile {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) { return }
    
    try {
        $content = Get-Content $FilePath -Raw -ErrorAction SilentlyContinue
        if (-not $content) { return }
        
        $filePatterns[$FilePath] = @{}
        
        foreach ($patternType in $patternTypes.Keys) {
            $patterns = $patternTypes[$patternType]
            $filePatterns[$FilePath][$patternType] = @{}
            
            foreach ($pattern in $patterns) {
                $matches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                if ($matches.Count -gt 0) {
                    $filePatterns[$FilePath][$patternType][$pattern] = $matches.Count
                    
                    # Store global pattern count
                    if (-not $patterns.ContainsKey($patternType)) {
                        $patterns[$patternType] = @{}
                    }
                    if (-not $patterns[$patternType].ContainsKey($pattern)) {
                        $patterns[$patternType][$pattern] = @{}
                    }
                    if (-not $patterns[$patternType][$pattern].ContainsKey($FilePath)) {
                        $patterns[$patternType][$pattern][$FilePath] = 0
                    }
                    $patterns[$patternType][$pattern][$FilePath] += $matches.Count
                }
            }
        }
    }
    catch {
        Write-Warning "Error processing file: $FilePath - $($_.Exception.Message)"
    }
}

# Function to analyze patterns and suggest improvements
function Analyze-Patterns {
    $suggestions = @()
    
    foreach ($patternType in $patterns.Keys) {
        foreach ($pattern in $patterns[$patternType].Keys) {
            $fileCount = $patterns[$patternType][$pattern].Keys.Count
            $totalOccurrences = ($patterns[$patternType][$pattern].Values | Measure-Object -Sum).Sum
            
            if ($fileCount -ge $PatternThreshold) {
                $suggestion = @{
                    Type = "Pattern Detected"
                    PatternType = $patternType
                    Pattern = $pattern
                    FileCount = $fileCount
                    TotalOccurrences = $totalOccurrences
                    Files = $patterns[$patternType][$pattern].Keys
                    Recommendation = "Consider creating/updating rule for $patternType patterns"
                    Priority = if ($fileCount -ge 5) { "High" } elseif ($fileCount -ge 3) { "Medium" } else { "Low" }
                }
                $suggestions += $suggestion
            }
        }
    }
    
    return $suggestions
}

# Function to generate rule suggestions
function Generate-RuleSuggestions {
    param([array]$Suggestions)
    
    $ruleSuggestions = @()
    
    foreach ($suggestion in $Suggestions) {
        $ruleSuggestion = @{
            RuleName = "$($suggestion.PatternType)_best_practices.mdc"
            Description = "Best practices for $($suggestion.PatternType) patterns"
            Trigger = "Pattern detected in $($suggestion.FileCount) files"
            Examples = $suggestion.Files | Select-Object -First 3
            Content = @"
---
description: Best practices for $($suggestion.PatternType) patterns based on detected usage across $($suggestion.FileCount) files.
globs: **/*.cs, **/*.ps1, **/*.yml, **/*.yaml, **/*.json
alwaysApply: false
---

# $($suggestion.PatternType) Best Practices

## Pattern Detection
This rule was automatically generated based on the detection of `$($suggestion.Pattern)` pattern in $($suggestion.FileCount) files:
$($suggestion.Files -join ", ")

## Standardized Implementation
- **Use consistent $($suggestion.PatternType) patterns across all services**
- **Follow established conventions from existing implementations**
- **Ensure proper error handling and validation**

## Examples
Based on detected usage in:
$($suggestion.Files | ForEach-Object { "- $_" })

## References
- Follow [cursor_rules.mdc](mdc:.cursor/rules/cursor_rules.mdc) for rule structure
- Follow [auto_improvement.mdc](mdc:.cursor/rules/auto_improvement.mdc) for pattern detection
"@
        }
        $ruleSuggestions += $ruleSuggestion
    }
    
    return $ruleSuggestions
}

# Main execution
Write-Host "Pattern Detector for Cursor Rules Auto-Improvement" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Green
Write-Host ""

# Get all relevant files
$fileExtensions = @("*.cs", "*.ps1", "*.yml", "*.yaml", "*.json", "*.md")
$files = @()

foreach ($ext in $fileExtensions) {
    $files += Get-ChildItem -Path $ProjectRoot -Recurse -Filter $ext -ErrorAction SilentlyContinue | 
              Where-Object { $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\node_modules\*" }
}

Write-Host "Found $($files.Count) files to analyze..." -ForegroundColor Yellow

# Process each file
$processedCount = 0
foreach ($file in $files) {
    $processedCount++
    if ($processedCount % 50 -eq 0) {
        Write-Progress -Activity "Analyzing files" -Status "Processed $processedCount of $($files.Count)" -PercentComplete (($processedCount / $files.Count) * 100)
    }
    
    Detect-PatternsInFile -FilePath $file.FullName
}

Write-Progress -Activity "Analyzing files" -Completed

# Analyze patterns
Write-Host "Analyzing patterns..." -ForegroundColor Yellow
$suggestions = Analyze-Patterns

# Generate rule suggestions
Write-Host "Generating rule suggestions..." -ForegroundColor Yellow
$ruleSuggestions = Generate-RuleSuggestions -Suggestions $suggestions

# Create output
$output = @{
    AnalysisDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    ProjectRoot = (Resolve-Path $ProjectRoot).Path
    PatternThreshold = $PatternThreshold
    TotalFilesAnalyzed = $files.Count
    PatternsDetected = $patterns
    Suggestions = $suggestions
    RuleSuggestions = $ruleSuggestions
}

# Save to file
$output | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutputFile -Encoding UTF8

# Display summary
Write-Host ""
Write-Host "Analysis Complete!" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green
Write-Host "Files analyzed: $($files.Count)" -ForegroundColor White
Write-Host "Patterns detected: $($suggestions.Count)" -ForegroundColor White
Write-Host "Rule suggestions: $($ruleSuggestions.Count)" -ForegroundColor White
Write-Host "Results saved to: $OutputFile" -ForegroundColor White

# Display high-priority suggestions
$highPrioritySuggestions = $suggestions | Where-Object { $_.Priority -eq "High" }
if ($highPrioritySuggestions.Count -gt 0) {
    Write-Host ""
    Write-Host "High Priority Suggestions:" -ForegroundColor Red
    foreach ($suggestion in $highPrioritySuggestions) {
        Write-Host "  • $($suggestion.PatternType): $($suggestion.Pattern) (found in $($suggestion.FileCount) files)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Review the generated suggestions in $OutputFile" -ForegroundColor White
Write-Host "2. Implement high-priority rule improvements" -ForegroundColor White
Write-Host "3. Run this script regularly to maintain rule quality" -ForegroundColor White
Write-Host "4. Use the auto-improvement system in [auto_improvement.mdc](mdc:.cursor/rules/auto_improvement.mdc)" -ForegroundColor White
