# Project Context

This is an Angular 21 frontend project for Pathfinder 1st Edition GM tools.
The first feature is an Initiative Tracker for combat encounters.

The project should be treated as a learning project for modern Angular features:
- Signals
- computed
- inject()
- standalone components
- modern Angular template control flow with @if and @for

# Coding Style

Use clear and readable TypeScript.

Prefer:
- block statements over terse one-liners
- descriptive variable names
- explicit method names
- small focused functions
- strong typing where useful

Avoid:
- short variable names like idx, len, s, c
- unnecessary abstractions
- large rewrites
- adding external libraries unless explicitly requested
- moving files around without asking
- mixing multiple architectural changes in one task

# Angular Preferences

Use:
- inject() for dependency injection
- signals and computed for state and derived state
- standalone components
- @if and @for in templates
- track expressions in @for loops, preferably track combatant.id

Avoid:
- unnecessary RxJS complexity in components
- business logic in templates
- side effects inside computed()
- direct mutation of signal state

# Architecture

Components should focus on UI and user interaction.

Stores should manage feature state and expose:
- state signals
- computed values
- feature actions like nextTurn, previousTurn, jumpToCombatant, loadEncounter

Services should handle data access:
- loading JSON from assets for now
- later replacing this with backend API calls

The Initiative Tracker currently uses:
- Combatant model
- EncounterStore
- EncounterService
- InitiativeTracker component

# Pathfinder 1E Domain

Combatants can be PCs or NPCs.
Important visible values:
- name
- side
- initiative
- AC
- touch AC
- flat-footed AC
- saves if available
- attacks for NPCs, especially when active

The active combatant should be visually highlighted.