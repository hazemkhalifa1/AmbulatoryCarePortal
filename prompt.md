# Claude CLI Project Debug Prompt

I have a project with multiple errors. I want you to analyze the entire project, discover all issues, and suggest practical solutions for each error.

## Required steps:

1. **Explore** the project folder and tell me its structure (languages, frameworks, main files).

2. **Read error logs** from the terminal if you can run commands like `npm run build` or `python manage.py check` depending on the project type. If not, ask me to send you the exact error text.

3. **Categorize errors** into:
   - Dependency errors
   - Syntax errors
   - Logic errors
   - Configuration errors

4. For each error, explain the **root cause**, then provide specific code or configuration to fix it.

5. **Prioritize solutions**: Fix errors that prevent execution first, then the rest.

6. After proposing solutions, give me specific terminal commands to apply the fixes in one go if possible.

7. Finally, **summarize** the situation: how many errors were resolved, and which errors may need additional manual intervention.

## Notes:
- If you need me to run any command and paste its output, tell me exactly what to type in the terminal.
- Do not assume any files or environment variables exist unless confirmed.
- Use clear English, and write code snippets in their original language.

## How to use this prompt:
1. Copy the entire text above.
2. Paste it to Claude CLI inside your project folder.
3. Follow any instructions from Claude (e.g., running diagnostic commands, sharing file contents).