import { Component, computed, inject } from '@angular/core';
import { EncounterStore } from '../../services/encounter.store';

@Component({
  selector: 'app-initiative-tracker',
  imports: [],
  templateUrl: './initiative-tracker.html',
  styleUrl: './initiative-tracker.css',
})
export class InitiativeTracker {
  private readonly encounterStore = inject(EncounterStore);

  readonly sortedCombatants = this.encounterStore.sorted;

  readonly activeCombatant = this.encounterStore.active;

  readonly hasCombatants = computed(() => {
    this.sortedCombatants.length > 0;
  });

  readonly activeCombatantId = computed(() => {
    const activeCombatant = this.activeCombatant();
    if (!activeCombatant) {
      return null;
    }
    return activeCombatant.id;
  })
}
