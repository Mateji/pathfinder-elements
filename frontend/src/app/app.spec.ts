import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { App } from './app';
import { appConfig } from './app.config';
import { type Combatant } from './models/combatant.model';

describe('App language selector', () => {
    const storageKey = 'pathfinder-elements.language';

    beforeEach(() => {
        localStorage.setItem(storageKey, 'en');
    });

    afterEach(() => {
        localStorage.removeItem(storageKey);
        document.documentElement.removeAttribute('lang');
        document.title = '';
    });

    it('translates the tracker in place and keeps the active combatant', async () => {
        TestBed.configureTestingModule({
            imports: [App],
            providers: [...appConfig.providers, provideHttpClientTesting()]
        });
        const fixture = TestBed.createComponent(App);
        fixture.detectChanges();
        await TestBed.inject(Router).navigateByUrl('/');
        fixture.detectChanges();

        const page: HTMLElement = fixture.nativeElement;
        expect(page.querySelector('main p')?.textContent).toBe('No combatants available.');
        const combatants: Combatant[] = [
            {
                id: 'pc-1',
                name: 'Aria',
                side: 'PC',
                initiative: 12,
                defense: { ac: 18 }
            },
            {
                id: 'npc-1',
                name: 'Goblin A',
                side: 'NPC',
                initiative: 8,
                defense: { ac: 16 }
            }
        ];
        const http = TestBed.inject(HttpTestingController);
        http.expectOne('assets/encounters/goblin-ambush.json').flush(combatants);
        fixture.detectChanges();

        const selector = page.querySelector('select')!;
        const [previousTurn, nextTurn] = page.querySelectorAll('button');
        expect(selector.value).toBe('en');
        expect(document.documentElement.lang).toBe('en');
        expect(document.title).toBe('Initiative Tracker | Pathfinder Elements');
        expect(nextTurn.textContent).toBe('Next turn');

        nextTurn.click();
        fixture.detectChanges();
        const activeRow = page.querySelector('.active-combatant');
        expect(activeRow?.textContent).toContain('Goblin A');
        expect(activeRow?.querySelector('svg')?.getAttribute('aria-label')).toBe('Armor Class 16');

        selector.value = 'de';
        selector.dispatchEvent(new Event('change'));
        fixture.detectChanges();

        expect(page.querySelector('label')?.textContent).toBe('Sprache');
        expect(page.querySelector('h1')?.textContent).toBe('Initiative-Tracker');
        expect(document.documentElement.lang).toBe('de');
        expect(document.title).toBe('Initiative-Tracker | Pathfinder Elements');
        expect(previousTurn.textContent).toBe('Vorheriger Zug');
        expect(nextTurn.textContent).toBe('Nächster Zug');
        expect(localStorage.getItem(storageKey)).toBe('de');
        expect(page.querySelector('.active-combatant')).toBe(activeRow);
        expect(activeRow?.textContent).toContain('Goblin A');
        expect(activeRow?.querySelector('svg')?.getAttribute('aria-label')).toBe('Rüstungsklasse 16');

        previousTurn.click();
        fixture.detectChanges();
        expect(page.querySelector('.active-combatant')?.textContent).toContain('Aria');
        http.verify();
    });
});
