# 🧭 Team Charter & Code of Conduct

This document defines the collaboration rules and the code of conduct
for the Mystic River project team. It outlines responsibilities,
communication channels and development practices used during the project.

## Mystic River

A distributed 1v1 multiplayer strategy system with a turn-based combat mechanism.
The application consists of a desktop client (WPF) and a server-side backend based on ASP.NET Core (C#).

Two players compete against each other online and each control a creature with defined attributes
(e.g., health points, initiative, defense) as well as several abilities with different effects
such as damage, healing, or temporary status changes.

The central focus of the project is the development of a robust, deterministic, and fully server-side game engine.

The server is authoritative, meaning:

    - All game decisions are validated on the server
    - The complete combat logic is calculated exclusively in the backend
    - The client acts purely as a presentation and interaction layer

The battle engine is implemented as an independent, UI-agnostic domain component,
allowing it to be tested in isolation and executed without a client
(e.g., in simulation tests).

The desktop interface (WPF) is responsible only for visualizing:

    - Creature status
    - Health points
    - Turn selection
    - Battle log

The primary focus of the project is on software architecture, testability,
multiplayer synchronization, and DevOps — not on complex graphics or animations.

### Links

- [Repo](https://github.com/MunozRaul/MysticRiver)

### Team Members

| Name | Initals | E-Mail | Intnl.Prof. 🇺🇳 (yes/no) |
|---|---|---|---|
| Kevin Fust | fustkev1 | fustkev1@students.zhaw.ch | no |
| Tobias Marxer | marxetob | marxetob@students.zhaw.ch | no |
| Simon Leu | leusim02 | leusim02@students.zhaw.ch | yes |
| Raul Munoz Peña | munozrau | munozrau@students.zhaw.ch | no |
| Etienne Gündüz| guendeti | guendeti@students.zhaw.ch | no |
| Ramón Gallego| galleram | galleram@students.zhaw.ch | no |
| Rubén Mena | menarub1 | menarub1@students.zhaw.ch | no |
 

---

## 📆 Collaboration & Organization

### Regular Meetings

| Zeremonie | Ort | Format | Dauer |
|--------|----------|--------|-------|
| Sprint Planning | Discord | Online Meeting | 60-90 min |
| Daily / Sync | Discord / Whatsapp | Text messages / Short Meeting | 10-15 min |
| Review | Discord | Demo | 30-45 min |
| Retrospective | Discord | Moderated Meeting | 30-45 min |

---

### 📍 Working Setup 

- Primary working model: Hybrid (remote work + physical meetings every Thursday at ZHAW at 10:00)
- Expected availability per week (per person): approx. 2–4 hours of project work
- Core working hours: evenings between 18:00 – 21:00 or by individual agreement

#### Known Availability Constraints

The following recurring availability constraints are known within the team:

| Team Member | Constraint |
|-------------|-----------|
| Kevin Fust | Not available on Wednesday evenings |
| Tobias Marxer |  |
| Simon Leu | Not available on Saturday afternoons |
| Raul Munoz Peña |
| Etienne Gündüz|  |
| Ramón Gallego|  |
| Rubén Mena |  |

---

### 💬 Communication Channels


| Purpose | Tool |
|-------|------|
| Quick coordination | WhatsApp / Discord |
| Meetings / Calls | Discord |
| Official decisions | GitHub Issues / GitHub Discussions |
| Task tracking | GitHub Projects / Issues |
| Documentation | GitHub Wiki / Markdown in the repository |

**Rule:** Important decisions must be documented (e.g., in the repository or via ADRs).

---

## 🧑‍⚖️ Rollen & Verantwortlichkeiten

| Role | Person | Responsibility |
|------|--------|--------------|
| Product Owner | - | Management of the product backlog and feature prioritization |
| Scrum Master / Moderator | - | Facilitates meetings and ensures adherence to the Scrum process |
| Architecture Lead | Rubén Mena | Technical architecture and overall project structure |
| CI/CD Responsible | - | Build pipeline, GitHub Actions, and automation |

> Roles may rotate during the project.

---

## 🧪 Quality Standards

### Definition of Done

A task is considered complete when:

- [ ] Code is merged into the main branch
- [ ] Pull request has been reviewed
- [ ] Tests are implemented and completed sucessfully
- [ ] CI build is successful
- [ ] Documentation is up to date

---

### Git Rules

We follow a simplified GitFlow strategy.

#### Branching Strategy

- `main` → stable version
- `staging` → integration branch
- `feature/<feature-name>` → new features
- `bugfix/<bug-name>` → bug fixes
- `docs/<docs-name>` → documentation

#### Workflow

- Create a feature branch
- Implement the feature
- Create a pull request
- Perform code review
- Merge into `staging`
- Merge `staging` into `main`

#### Commit Message Convention

We follow the **Conventional Commits** standard.

Examples:

- `feat: implement turn-based combat engine`
- `fix: prevent negative HP after damage`
- `docs: update architecture diagram`
- `refactor: improve game state handling`
- `test: add unit tests for damage effects`


##### Pull Request Rules

- Pull request description is required
- At least one code review is required
- All tests must pass
- No direct commits to `main`

#### Who may merge?

Merging may be performed by:

- Any team member after a successful review


---

## ⚖️ Code of Conduct

We commit to:

- Respectful communication
- Active participation
- Reliability
- Transparency when problems occur
- Constructive feedback

We do not accept:

- Personal attacks
- Discrimination
- Ghosting or lack of response
- Shifting work unfairly to others


---

## 🚨 Conflict Resolution

If conflicts arise:

1. Address the issue directly within the team
2. If unresolved → escalate to the moderator / Scrum Master
3. If still unresolved → inform the lecturer


---

## 🔄 Feedback & Improvement

We conduct regular retrospectives.

Improvement suggestions are:

- documented
- prioritized
- implemented

The goal is continuous improvement of collaboration and the development process.

---

## 📝 Signatures

By providing our (digital) signatures, we confirm that we accept these rules.

| Name | Date | Signature |
|------|-------|-------------|
| Kevin Fust | 12.03 | kefu |
| Tobias Marxer |  |  |
| Simon Leu | 14.03 | leusim02 |
| Raul Munoz Peña | 12.03 | munozrau |
| Etienne Gündüz| 15.03 | guendeti |
| Ramón Gallego| 12.03 | galleram |
| Rubén Mena | 15.03 | menarub1 |
 
