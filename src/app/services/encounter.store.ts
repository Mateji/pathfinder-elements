import { computed, Injectable, inject, signal } from '@angular/core';
import { Combatant } from '../models/combatant.model';
import { EncounterService } from './encounter-service';

interface EncounterState {
    combatants: Combatant[];
    activeIndex: number;
}

@Injectable({ providedIn: 'root' })
export class EncounterStore {
    private _encounterService = inject(EncounterService);

    private readonly state = signal<EncounterState>({
        combatants: [],
        activeIndex: 0
    });

    readonly sorted = computed(() => {
        const combatants = this.state().combatants;

        return [...combatants].sort((leftCombatant, rightCombatant) => {
            if (leftCombatant.initiative !== rightCombatant.initiative) {
                return rightCombatant.initiative - leftCombatant.initiative;
            }

            return leftCombatant.name.localeCompare(rightCombatant.name);
        });
    });

    readonly active = computed(() => {
        const activeIndex = this.state().activeIndex;
        const sortedCombatants = this.sorted();

        return sortedCombatants[activeIndex] ?? null;
    });

    loadEncounter(encounterName: string): void {
        this._encounterService.getEncounter(encounterName).subscribe({
            next: (combatants) => {
                this.setCombatants(combatants);
            },
            error: () => {
                this.setCombatants([]);
            }
        });
    }

    getEncounter(encounterName: string): void {
        this.loadEncounter(encounterName);
    }

    setCombatants(combatants: Combatant[]): void {
        this.state.update((currentState) => {
            return {
                ...currentState,
                combatants,
                activeIndex: 0
            };
        });
    }

    nextTurn(): void {
        this.state.update((currentState) => {
            const combatantCount = this.sorted().length;

            if (combatantCount === 0) {
                return currentState;
            }

            return {
                ...currentState,
                activeIndex: (currentState.activeIndex + 1) % combatantCount
            };
        });
    }

    previousTurn(): void {
        this.state.update((currentState) => {
            const combatantCount = this.sorted().length;

            if (combatantCount === 0) {
                return currentState;
            }

            return {
                ...currentState,
                activeIndex: (currentState.activeIndex - 1 + combatantCount) % combatantCount
            };
        });
    }

    jumpToCombatant(combatantId: string): void {
        const sortedCombatants = this.sorted();
        const nextActiveIndex = sortedCombatants.findIndex((combatant) => combatant.id === combatantId);

        if (nextActiveIndex >= 0) {
            this.state.update((currentState) => {
                return {
                    ...currentState,
                    activeIndex: nextActiveIndex
                };
            });
        }
    }

    next(): void {
        this.nextTurn();
    }

    prev(): void {
        this.previousTurn();
    }

    jumpTo(combatantId: string): void {
        this.jumpToCombatant(combatantId);
    }
}
