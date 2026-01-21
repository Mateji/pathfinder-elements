import { Component, computed, inject, OnInit } from '@angular/core';
import { EncounterStore } from '../../services/encounter.store';
import { Icon } from "../icon/icon";

@Component({
    selector: 'app-initiative-tracker',
    imports: [Icon],
    templateUrl: './initiative-tracker.html',
    styleUrl: './initiative-tracker.css'
})
export class InitiativeTracker implements OnInit {
    private readonly encounterStore = inject(EncounterStore);

    readonly sortedCombatants = this.encounterStore.sorted;

    readonly activeCombatant = this.encounterStore.active;

    readonly hasCombatants = computed(() => this.sortedCombatants().length > 0);

    readonly activeCombatantId = computed(() => {
        const activeCombatant = this.activeCombatant();
        if (!activeCombatant) {
            return null;
        }
        return activeCombatant.id;
    });

    ngOnInit(): void {
        // Initial load of encounter data
        this.encounterStore.getEncounter('goblin-ambush');
    }

    onNextTurn() {
        this.encounterStore.next();
    }

    onPreviousTurn() {
        this.encounterStore.prev();
    }
}
