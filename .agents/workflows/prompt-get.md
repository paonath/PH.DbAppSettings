---
description: Redefine prompt inserted by the human
---

1. If prompt is empty prompt stop immediatly.
2. Understand and disambiguate the user-entered prompt: activate and use the prompt-clarifier skill, if necessary, launch QA with the user.
3. Redefine and rewrite the prompt in English following these rules:
    - fewest words possible;
    - if you need to use a path, make sure it is ALWAYS relative and not absolute;
    - human-ready and AI-ready;
    - enclose output in backticks for easy copy/pasting.