import { Routes } from '@angular/router';
import { InitiativeTracker } from './components/initiative-tracker/initiative-tracker';

export const routes: Routes = [
    { path: '', component: InitiativeTracker },
    { path: 'library', loadComponent: () => import('./library/library-page').then((module) => module.LibraryPage) },
    { path: 'encounters', loadComponent: () => import('./encounters/encounter-page').then((module) => module.EncounterPage) }
];
