# Area Taxonomy

Recognised `ambito` values for stub file naming.

| Area | Description | Source content indicators |
|------|-------------|--------------------------|
| `database` | Data model, schema, migrations, indexes | table, entity, column, foreign key, migration, ERD |
| `backend` | API, services, business logic, server-side | endpoint, controller, service, handler, route, HTTP |
| `frontend` | UI components, pages, client-side logic | component, form, page, Angular, React, CSS, UI |
| `testing` | Test plan, test cases, acceptance/QA | test, acceptance criteria, scenario, QA, validation |
| `documentation` | Technical or functional docs | README, guide, manual, changelog, how to |
| `infrastructure` | Deploy, CI/CD, cloud, configuration | Docker, Kubernetes, pipeline, deploy, env, config |
| `security` | Auth, authorization, tokens, encryption | auth, login, token, permission, role, OAuth |
| `integration` | Third-party APIs, external services | webhook, external API, connector, sync, event |

## Custom Areas

If none of the above match, define a custom lowercase area name (e.g., `reporting`, `notifications`). Present it to the user before generating the stub.
