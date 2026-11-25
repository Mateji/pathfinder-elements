import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { I18nService } from './i18n.service';

describe('I18nService', () => {
    const storageKey = 'pathfinder-elements.language';

    afterEach(() => {
        vi.restoreAllMocks();
        localStorage.removeItem(storageKey);
    });

    it('prefers a saved selection over the browser languages', () => {
        vi.spyOn(navigator, 'languages', 'get').mockReturnValue(['en-US']);
        localStorage.setItem(storageKey, 'de');

        expect(TestBed.inject(I18nService).language()).toBe('de');
    });

    it('uses the first supported browser language if the stored value is invalid', () => {
        vi.spyOn(navigator, 'languages', 'get').mockReturnValue(['fr-FR', 'de-AT', 'en-GB']);
        localStorage.setItem(storageKey, 'invalid');

        expect(TestBed.inject(I18nService).language()).toBe('de');
    });

    it('defaults to English and can switch even when storage is blocked', () => {
        vi.spyOn(navigator, 'languages', 'get').mockReturnValue(['fr-FR']);
        vi.spyOn(window, 'localStorage', 'get').mockImplementation(() => {
            throw new DOMException('Storage disabled', 'SecurityError');
        });

        const i18n = TestBed.inject(I18nService);
        expect(i18n.language()).toBe('en');
        i18n.setLanguage('fr');
        expect(i18n.language()).toBe('en');
        i18n.setLanguage('de');
        expect(i18n.messages().noCombatants).toBe('Keine Kampfteilnehmer verfügbar.');
    });
});
