# Code Review Staged Files

Review all currently staged script files for issues and improvements.

## Steps

1. Run `!git diff --cached --name-only` to identify staged files
2. Run `!git diff --cached` to see the actual changes
3. Read the full content of each staged script file with relevant context

## Review Checklist

### Bugs & Logic Gaps
- Identify any bugs, logic errors, or edge cases not handled
- Look for regressions — does this change break anything elsewhere?
- Check for null/uninitialized references, off-by-one errors, or unsafe assumptions

### Code Standards
- Review the project's CLAUDE.md for coding standards and conventions
- Flag any violations of the standards defined there if the code file is not part of a plugin.

### Comments & Clarity
- Suggest inline comments where intent is not obvious
- Point out complex blocks that would benefit from a brief explanation
- Avoid suggesting comments for self-explanatory code

## Output Format

For each file, report:
- **File**: filename
- **Bugs / Gaps**: list issues found, or "None"
- **Standards violations**: list violations, or "None"
- **Comment suggestions**: show the line(s) and the suggested comment

If no issues are found in a file, say so clearly.
After all files, give a short overall summary.