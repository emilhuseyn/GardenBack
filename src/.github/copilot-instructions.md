# Copilot Instructions

## Project Guidelines
- User expects responses and code changes to strictly match the exact requested scope (e.g., add specific field in Child model) without unrelated modifications.
- User expects child parent-related schema changes to include exact requested fields (e.g., second parent phone number) and not different fields.
- User wants payment receipt 'Dövr' to be clearly dynamic and show exact start-end date range for the specific paid period.
- When a child leaves, the workflow should use deactivation (not deletion) and must record the leave date.
- Project should track historical group activity via persistent logs (child added/removed, group edited) with timestamps because hard delete is used.