import { computed, Injectable, inject, signal } from '@angular/core';
import { Combatant } from '../models/combatant.model';
import { EncounterService } from './encounter-service';

interface EncounterState {
    combatants: Combatant[];
    activeIndex: number; // Index in initiative-sorted Liste
}

@Injectable({ providedIn: 'root' })
export class EncounterStore {
    private _encounterService = inject(EncounterService);

    private readonly state = signal<EncounterState>({
        combatants: [],
        activeIndex: 0
    });

    readonly sorted = computed(() => [...this.state().combatants].sort((a, b) => b.initiative - a.initiative));
    readonly active = computed(() => this.sorted()[this.state().activeIndex] ?? null);

    getEncounter(name: string): void {
        this._encounterService.getEncounter(name).subscribe((combatants) => {
            this.setCombatants(combatants);
        });
    }

    // commands
    setCombatants(list: Combatant[]) {
        this.state.update((state) => ({ ...state, combatants: list, activeIndex: 0 }));
    }

    next() {
        this.state.update((state) => {
            const length = this.sorted().length;
            if (!length) return state;
            return { ...state, activeIndex: (state.activeIndex + 1) % length };
        });
    }

    prev() {
        this.state.update((state) => {
            const length = this.sorted().length;
            if (!length) return state;
            return { ...state, activeIndex: (state.activeIndex - 1 + length) % length };
        });
    }

    jumpTo(id: string) {
        const index = this.sorted().findIndex((combatant) => combatant.id === id);
        if (index >= 0) this.state.update((state) => ({ ...state, activeIndex: index }));
    }
}
